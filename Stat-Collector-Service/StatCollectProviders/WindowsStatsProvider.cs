using System;
using System.Diagnostics;
using System.Threading;
using Stat_Collector_Service.Models;
using Stat_Collector_Service.StatCollectProviders.Interfaces;

namespace Stat_Collector_Service.StatCollectProviders;

public class WindowsStatsProvider : ISystemStatsProvider
{
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _availableMemoryCounter;

    public WindowsStatsProvider()
    {
        _availableMemoryCounter = new PerformanceCounter("Memory", "Available Bytes");
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _cpuCounter.NextValue();
    }
    
    public double GetAvailableMemory()
    {
        var availableBytes = _availableMemoryCounter.NextValue();
        return availableBytes / (1024.0 * 1024.0);
    }

    public double GetMemoryUsage()
    {
        var totalMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        var totalMemoryMb = totalMemoryBytes / (1024.0 * 1024.0);
    
        var availableMemoryMb = GetAvailableMemory();
    
        return totalMemoryMb - availableMemoryMb;
    }

    public double GetCpuUsage()
    {
        return _cpuCounter.NextValue();
    }
}