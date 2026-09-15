using Messaging;
using Messaging.RabbitMQ;
using Shared.Models;

namespace AnomalyDetectionService;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<IMessageConsumer, RabbitMqConsumer>();
        builder.Services.AddSingleton<IStatisticsRepository, MongoDbRepository>();
        
        var app = builder.Build();
       
        var messageConsumer = app.Services.GetRequiredService<IMessageConsumer>();
        messageConsumer.Subscribe("ServerStatistics.*", stats =>
        {
            Console.WriteLine($"Received from {stats.ServerIdentifier}: CPU {stats.CpuUsage}%, Mem {stats.MemoryUsage}MB");
            
            var ServerStatistics = new ServerStatistics()
            {
                CpuUsage = stats.CpuUsage,
                MemoryUsage = stats.MemoryUsage,
                ServerIdentifier = stats.ServerIdentifier,
                AvailableMemory = stats.AvailableMemory,
                Timestamp =  stats.Timestamp,
            };
            
            
            // TODO: لاحقاً هون رح نضيف: تخزين بالـ Mongo + فحص anomaly + بعث SignalR
        });
        app.MapGet("/", () => { });

        app.Run();
    }
}