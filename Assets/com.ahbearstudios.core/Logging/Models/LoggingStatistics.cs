using System.Linq;

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

        /// <summary>
        /// Creates a new LoggingStatistics instance with the specified values.
        /// </summary>
        /// <param name="messagesProcessed">Total number of messages processed</param>
        /// <param name="errorCount">Total number of errors encountered</param>
        /// <param name="activeTargets">Number of active targets</param>
        /// <param name="healthyTargets">Number of healthy targets</param>
        /// <param name="uptimeSeconds">Service uptime in seconds</param>
        /// <param name="lastHealthCheck">Timestamp of last health check</param>
        /// <param name="currentQueueSize">Current queue size</param>
        /// <param name="averageProcessingTimeMs">Average processing time in milliseconds</param>
        /// <param name="peakMemoryUsageBytes">Peak memory usage in bytes</param>
        /// <returns>A new LoggingStatistics instance</returns>
        public static LoggingStatistics Create(
            long messagesProcessed,
            long errorCount,
            int activeTargets,
            int healthyTargets,
            double uptimeSeconds,
            DateTime lastHealthCheck,
            int currentQueueSize = 0,
            double averageProcessingTimeMs = 0.0,
            long peakMemoryUsageBytes = 0)
        {
            var errorRate = messagesProcessed > 0 ? (double)errorCount / messagesProcessed : 0.0;
            
            return new LoggingStatistics
            {
                MessagesProcessed = messagesProcessed,
                ErrorCount = errorCount,
                ErrorRate = errorRate,
                ActiveTargets = activeTargets,
                HealthyTargets = healthyTargets,
                UptimeSeconds = uptimeSeconds,
                LastHealthCheck = lastHealthCheck,
                CurrentQueueSize = currentQueueSize,
                AverageProcessingTimeMs = averageProcessingTimeMs,
                PeakMemoryUsageBytes = peakMemoryUsageBytes
            };
        }

        /// <summary>
        /// Creates LoggingStatistics for health check scenarios.
        /// </summary>
        /// <param name="service">The logging service to extract statistics from</param>
        /// <returns>A new LoggingStatistics instance optimized for health checks</returns>
        public static LoggingStatistics ForHealthCheck(ILoggingService service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            var targets = service.GetTargets();
            var healthyTargets = targets.Count(t => t.PerformHealthCheck());
            var existingStats = service.GetStatistics();
            var uptimeSeconds = existingStats.UptimeSeconds;
            
            return new LoggingStatistics
            {
                MessagesProcessed = 0, // Health check doesn't track message count
                ErrorCount = 0,
                ErrorRate = 0.0,
                ActiveTargets = targets.Count,
                HealthyTargets = healthyTargets,
                UptimeSeconds = uptimeSeconds,
                LastHealthCheck = DateTime.UtcNow,
                CurrentQueueSize = 0,
                AverageProcessingTimeMs = 0.0,
                PeakMemoryUsageBytes = GC.GetTotalMemory(false)
            };
        }

        /// <summary>
        /// Creates LoggingStatistics for performance monitoring scenarios.
        /// </summary>
        /// <param name="service">The logging service to extract statistics from</param>
        /// <param name="messagesProcessed">Number of messages processed</param>
        /// <param name="errorCount">Number of errors encountered</param>
        /// <param name="averageProcessingTime">Average processing time in milliseconds</param>
        /// <returns>A new LoggingStatistics instance optimized for performance monitoring</returns>
        public static LoggingStatistics ForPerformanceMonitoring(
            ILoggingService service,
            long messagesProcessed,
            long errorCount,
            double averageProcessingTime)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            var targets = service.GetTargets();
            var healthyTargets = targets.Count(t => t.PerformHealthCheck());
            var existingStats = service.GetStatistics();
            var uptimeSeconds = existingStats.UptimeSeconds;
            var errorRate = messagesProcessed > 0 ? (double)errorCount / messagesProcessed : 0.0;
            
            return new LoggingStatistics
            {
                MessagesProcessed = messagesProcessed,
                ErrorCount = errorCount,
                ErrorRate = errorRate,
                ActiveTargets = targets.Count,
                HealthyTargets = healthyTargets,
                UptimeSeconds = uptimeSeconds,
                LastHealthCheck = DateTime.UtcNow,
                CurrentQueueSize = 0, // Would need to be provided by service
                AverageProcessingTimeMs = averageProcessingTime,
                PeakMemoryUsageBytes = GC.GetTotalMemory(false)
            };
        }

        /// <summary>
        /// Creates an empty LoggingStatistics instance.
        /// </summary>
        /// <returns>A new LoggingStatistics instance with default values</returns>
        public static LoggingStatistics Empty()
        {
            return new LoggingStatistics
            {
                MessagesProcessed = 0,
                ErrorCount = 0,
                ErrorRate = 0.0,
                ActiveTargets = 0,
                HealthyTargets = 0,
                UptimeSeconds = 0.0,
                LastHealthCheck = DateTime.MinValue,
                CurrentQueueSize = 0,
                AverageProcessingTimeMs = 0.0,
                PeakMemoryUsageBytes = 0
            };
        }
    }
}