namespace Stat_Collector_Service.StatCollectProviders.Interfaces;

public interface ISystemStatsProvider
{
    double GetMemoryUsage();
    double GetAvailableMemory();
    double GetCpuUsage();
}