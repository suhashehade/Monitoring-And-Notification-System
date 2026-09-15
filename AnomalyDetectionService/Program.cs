using System.Collections.Concurrent;
using Messaging;
using Messaging.RabbitMQ;
using Microsoft.AspNetCore.SignalR;
using Persistence.Abstractions;
using Persistence.MongoDB;
using Shared.Models;

namespace AnomalyDetectionService;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<IMessageConsumer, RabbitMqConsumer>();
        builder.Services.AddSingleton<IStatisticsRepository, MongoDbRepository>();
        builder.Services.AddSignalR();

        var app = builder.Build();

        app.MapHub<AlertsHub>("/alertsHub");

        var messageConsumer = app.Services.GetRequiredService<IMessageConsumer>();
        var persistence = app.Services.GetRequiredService<IStatisticsRepository>();
        var hubContext = app.Services.GetRequiredService<IHubContext<AlertsHub>>();
        var previousReadings = new ConcurrentDictionary<string, ServerStatistics>();

        var memoryAnomalyThreshold = builder.Configuration.GetValue<double>("AnomalyDetectionConfig:MemoryUsageAnomalyThresholdPercentage");
        var cpuAnomalyThreshold = builder.Configuration.GetValue<double>("AnomalyDetectionConfig:CpuUsageAnomalyThresholdPercentage");
        var memoryUsageThreshold = builder.Configuration.GetValue<double>("AnomalyDetectionConfig:MemoryUsageThresholdPercentage");
        var cpuUsageThreshold = builder.Configuration.GetValue<double>("AnomalyDetectionConfig:CpuUsageThresholdPercentage");

        messageConsumer.Subscribe("ServerStatistics.*", async void (stats) =>
        {
            try
            {
                Console.WriteLine($"Received from {stats.ServerIdentifier}: CPU {Math.Round(stats.CpuUsage, 2)}%, Mem {Math.Round(stats.MemoryUsage, 2)}MB");

                await persistence.SaveAsync(stats);

                // ---- Anomaly checks (need previous reading) ----
                if (previousReadings.TryGetValue(stats.ServerIdentifier, out var previous))
                {
                    if (stats.MemoryUsage > previous.MemoryUsage * (1 + memoryAnomalyThreshold))
                    {
                        Console.WriteLine($"[ANOMALY] Memory spike on {stats.ServerIdentifier}: {Math.Round(previous.MemoryUsage, 2)}MB → {Math.Round(stats.MemoryUsage, 2)}MB");
                        await hubContext.Clients.All.SendAsync("AnomalyAlert", new
                        {
                            ServerIdentifier = stats.ServerIdentifier,
                            Type = "Memory",
                            Previous = previous.MemoryUsage,
                            Current = stats.MemoryUsage
                        });
                    }

                    if (stats.CpuUsage > previous.CpuUsage * (1 + cpuAnomalyThreshold))
                    {
                        Console.WriteLine($"[ANOMALY] CPU spike on {stats.ServerIdentifier}: {Math.Round(previous.CpuUsage, 2)}% → {Math.Round(stats.CpuUsage, 2)}%");
                        await hubContext.Clients.All.SendAsync("AnomalyAlert", new
                        {
                            ServerIdentifier = stats.ServerIdentifier,
                            Type = "CPU",
                            Previous = previous.CpuUsage,
                            Current = stats.CpuUsage
                        });
                    }
                }

                // ---- High usage checks (current reading only) ----
                var memoryUsagePercent = stats.MemoryUsage / (stats.MemoryUsage + stats.AvailableMemory);
                if (memoryUsagePercent > memoryUsageThreshold)
                {
                    Console.WriteLine($"[HIGH USAGE] Memory on {stats.ServerIdentifier}: {memoryUsagePercent:P1}");
                    await hubContext.Clients.All.SendAsync("HighUsageAlert", new
                    {
                        ServerIdentifier = stats.ServerIdentifier,
                        Type = "Memory",
                        Current = stats.MemoryUsage
                    });
                }

                if (stats.CpuUsage > cpuUsageThreshold)
                {
                    Console.WriteLine($"[HIGH USAGE] CPU on {stats.ServerIdentifier}: {Math.Round(stats.CpuUsage, 2)}%");
                    await hubContext.Clients.All.SendAsync("HighUsageAlert", new
                    {
                        ServerIdentifier = stats.ServerIdentifier,
                        Type = "CPU",
                        Current = stats.CpuUsage
                    });
                }

                // آخر شي دايماً: حدّثي القراءة السابقة
                previousReadings[stats.ServerIdentifier] = stats;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error processing message: {e.Message}");
            }
        });

        app.MapGet("/", () => { });

        app.Run();
    }
}