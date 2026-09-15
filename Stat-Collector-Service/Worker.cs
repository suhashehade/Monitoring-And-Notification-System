using Messaging;
using Shared.Models;
using Stat_Collector_Service.StatCollectProviders.Interfaces;

namespace Stat_Collector_Service;

public class Worker(
    ILogger<Worker> logger,
    ISystemStatsProvider statProvider,
    IConfiguration configuration, 
    IMessagePublisher messagePublisher)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = configuration.GetValue("ServerStatisticsConfig:SamplingIntervalSeconds", 60);
        var serverIdentifier = configuration.GetValue<string>("ServerStatisticsConfig:ServerIdentifier", "linux1");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                var serverStatistics = new ServerStatistics()
                {
                    ServerIdentifier =  serverIdentifier,
                    AvailableMemory = statProvider.GetAvailableMemory(),
                    CpuUsage = statProvider.GetCpuUsage(),
                    MemoryUsage = statProvider.GetMemoryUsage(),
                    Timestamp = DateTime.UtcNow
                };
                await messagePublisher.PublishAsync($"ServerStatistics.{serverIdentifier}", serverStatistics);
            }
            catch (Exception e)
            {
                logger.LogError(e, "An exception occurred");
            }
            finally
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
        }
    }
}