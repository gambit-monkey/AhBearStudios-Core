namespace AhBearStudios.Core.Logging.Models;

/// <summary>
/// Performance optimization profiles for logging service creation.
/// </summary>
public enum PerformanceProfile
{
    /// <summary>
    /// Optimized for maximum message throughput with large batches and caching.
    /// </summary>
    MaximumThroughput,

    /// <summary>
    /// Optimized for lowest possible latency with immediate processing.
    /// </summary>
    LowLatency,

    /// <summary>
    /// Optimized for minimal memory usage with small buffers and minimal logging.
    /// </summary>
    MinimalMemory,

    /// <summary>
    /// Balanced configuration suitable for most general-purpose applications.
    /// </summary>
    Balanced
}