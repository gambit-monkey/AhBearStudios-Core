namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Performance and operational statistics for the logging service.
    /// Provides comprehensive metrics for monitoring and alerting.
    /// </summary>
    public sealed record LoggingStatistics
    {
        /// <summary>
        /// Gets the total number of log messages processed since startup.
        /// </summary>
        public long MessagesProcessed { get; init; }

        /// <summary>
        /// Gets the total number of errors encountered during logging operations.
        /// </summary>
        public long ErrorCount { get; init; }

        /// <summary>
        /// Gets the error rate as a percentage (0.0 to 1.0).
        /// </summary>
        public double ErrorRate { get; init; }

        /// <summary>
        /// Gets the number of currently active log targets.
        /// </summary>
        public int ActiveTargets { get; init; }

        /// <summary>
        /// Gets the number of healthy log targets.
        /// </summary>
        public int HealthyTargets { get; init; }

        /// <summary>
        /// Gets the service uptime in seconds.
        /// </summary>
        public double UptimeSeconds { get; init; }

        /// <summary>
        /// Gets the timestamp of the last health check.
        /// </summary>
        public DateTime LastHealthCheck { get; init; }

        /// <summary>
        /// Gets the current queue size (for batched logging).
        /// </summary>
        public int CurrentQueueSize { get; init; }

        /// <summary>
        /// Gets the average message processing time in milliseconds.
        /// </summary>
        public double AverageProcessingTimeMs { get; init; }

        /// <summary>
        /// Gets the peak memory usage in bytes.
        /// </summary>
        public long PeakMemoryUsageBytes { get; init; }

        /// <summary>
        /// Gets whether the logging service is currently healthy.
        /// </summary>
        public bool IsHealthy => ErrorRate < 0.1 && HealthyTargets > 0; // 10% error threshold

        /// <summary>
        /// Gets the service uptime as a TimeSpan.
        /// </summary>
        public TimeSpan Uptime => TimeSpan.FromSeconds(UptimeSeconds);

        /// <summary>
        /// Gets a summary description of the current state.
        /// </summary>
        public string StatusSummary
        {
            get
            {
                if (!IsHealthy)
                    return $"Unhealthy - {ErrorCount} errors, {HealthyTargets}/{ActiveTargets} targets healthy";
                
                return $"Healthy - {MessagesProcessed} messages processed, {HealthyTargets}/{ActiveTargets} targets active";
            }
        }
    }
}