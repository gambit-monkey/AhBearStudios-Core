namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Types of performance thresholds.
    /// </summary>
    public enum PerformanceThresholdType
    {
        /// <summary>
        /// Slow execution time threshold.
        /// </summary>
        SlowExecution,

        /// <summary>
        /// High failure rate threshold.
        /// </summary>
        HighFailureRate,

        /// <summary>
        /// Memory usage threshold.
        /// </summary>
        HighMemoryUsage,

        /// <summary>
        /// CPU usage threshold.
        /// </summary>
        HighCpuUsage
    }
}