using System;

namespace AhBearStudios.Core.Common.Models;

/// <summary>
/// System resource usage statistics for monitoring performance.
/// Provides comprehensive system resource tracking across multiple components.
/// </summary>
public readonly record struct SystemResourceStats
{
    /// <summary>
    /// Gets the current memory usage percentage (0-100).
    /// </summary>
    public double MemoryUsagePercent { get; init; }

    /// <summary>
    /// Gets the current CPU usage percentage (0-100).
    /// </summary>
    public double CpuUsagePercent { get; init; }

    /// <summary>
    /// Gets the number of active threads.
    /// </summary>
    public int ActiveThreads { get; init; }

    /// <summary>
    /// Gets the peak memory usage recorded in bytes.
    /// </summary>
    public long PeakMemoryUsageBytes { get; init; }

    /// <summary>
    /// Creates empty resource statistics.
    /// </summary>
    public static SystemResourceStats Empty => new();

    /// <summary>
    /// Merges with other resource statistics.
    /// </summary>
    /// <param name="other">Other statistics to merge</param>
    /// <returns>Merged statistics</returns>
    public SystemResourceStats Merge(SystemResourceStats other)
    {
        return new SystemResourceStats
        {
            MemoryUsagePercent = Math.Max(MemoryUsagePercent, other.MemoryUsagePercent),
            CpuUsagePercent = Math.Max(CpuUsagePercent, other.CpuUsagePercent),
            ActiveThreads = Math.Max(ActiveThreads, other.ActiveThreads),
            PeakMemoryUsageBytes = Math.Max(PeakMemoryUsageBytes, other.PeakMemoryUsageBytes)
        };
    }
}