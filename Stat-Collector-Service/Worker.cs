using Stat_Collector_Service.Models;
using Stat_Collector_Service.StatCollectProviders.Interfaces;

namespace Stat_Collector_Service;

public class Worker(
    ILogger<Worker> logger,
    ISystemStatsProvider statProvider,
    IConfiguration configuration)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = configuration.GetValue("ServerStatisticsConfig:SamplingIntervalSeconds", 60);
        var serverIdentifier = configuration.GetValue<string>("ServerStatisticsConfig:ServerIdentifier", "linux1");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            var serverStatistics = new ServerStatistics()
            {
                AvailableMemory = statProvider.GetAvailableMemory(),
                CpuUsage = statProvider.GetCpuUsage(),
                MemoryUsage = statProvider.GetMemoryUsage(),
                Timestamp =  DateTime.UtcNow
            };
            
            logger.LogInformation(
                "[{Server}] Stats collected at {Time}: CPU: {Cpu}%, Mem Usage: {Mem}MB", 
                serverIdentifier, 
                serverStatistics.Timestamp, 
                Math.Round(serverStatistics.CpuUsage, 2), 
                Math.Round(serverStatistics.MemoryUsage, 2));
            
            // TODO: Connect with message queue abstract
            
            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }
}