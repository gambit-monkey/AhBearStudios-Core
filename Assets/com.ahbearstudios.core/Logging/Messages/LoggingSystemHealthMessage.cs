using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Logging.Messages
{
    /// <summary>
    /// Message published when the logging system health status changes.
    /// Replaces direct EventHandler usage for loose coupling through IMessageBus.
    /// </summary>
    public readonly struct LoggingSystemHealthMessage : IMessage
    {
        /// <summary>
        /// Gets the unique identifier for this message.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the timestamp when this message was created.
        /// </summary>
        public long TimestampTicks { get; }

        /// <summary>
        /// Gets the type code for this message type.
        /// </summary>
        public ushort TypeCode => MessageTypeCodes.LoggingSystemHealth;

        /// <summary>
        /// Gets the source system that published this message.
        /// </summary>
        public FixedString64Bytes Source { get; }

        /// <summary>
        /// Gets the priority level for message processing.
        /// </summary>
        public MessagePriority Priority { get; }

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Gets the overall health status of the logging system.
        /// </summary>
        public LoggingSystemHealthStatus HealthStatus { get; }

        /// <summary>
        /// Gets the number of healthy targets.
        /// </summary>
        public int HealthyTargetCount { get; }

        /// <summary>
        /// Gets the total number of targets.
        /// </summary>
        public int TotalTargetCount { get; }

        /// <summary>
        /// Gets the number of active channels.
        /// </summary>
        public int ActiveChannelCount { get; }

        /// <summary>
        /// Gets the messages processed per second (throughput).
        /// </summary>
        public float MessagesPerSecond { get; }

        /// <summary>
        /// Gets the current error rate as a percentage.
        /// </summary>
        public float ErrorRatePercent { get; }

        /// <summary>
        /// Gets the memory usage in bytes.
        /// </summary>
        public long MemoryUsageBytes { get; }

        /// <summary>
        /// Gets the uptime in ticks.
        /// </summary>
        public long UptimeTicks { get; }

        /// <summary>
        /// Gets the health check correlation ID.
        /// </summary>
        public FixedString64Bytes HealthCheckCorrelationId { get; }

        /// <summary>
        /// Gets additional health status details.
        /// </summary>
        public FixedString512Bytes StatusDetails { get; }

        /// <summary>
        /// Initializes a new instance of the LoggingSystemHealthMessage.
        /// </summary>
        /// <param name="healthStatus">The overall health status</param>
        /// <param name="healthyTargetCount">Number of healthy targets</param>
        /// <param name="totalTargetCount">Total number of targets</param>
        /// <param name="activeChannelCount">Number of active channels</param>
        /// <param name="messagesPerSecond">Messages processed per second</param>
        /// <param name="errorRatePercent">Error rate as percentage</param>
        /// <param name="memoryUsageBytes">Memory usage in bytes</param>
        /// <param name="uptime">System uptime</param>
        /// <param name="healthCheckCorrelationId">Health check correlation ID</param>
        /// <param name="statusDetails">Additional status details</param>
        /// <param name="correlationId">Message correlation ID</param>
        public LoggingSystemHealthMessage(
            LoggingSystemHealthStatus healthStatus,
            int healthyTargetCount,
            int totalTargetCount,
            int activeChannelCount,
            float messagesPerSecond,
            float errorRatePercent,
            long memoryUsageBytes,
            TimeSpan uptime,
            FixedString64Bytes healthCheckCorrelationId = default,
            FixedString512Bytes statusDetails = default,
            Guid correlationId = default)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            Source = new FixedString64Bytes("LoggingSystem");
            Priority = healthStatus == LoggingSystemHealthStatus.Critical ? MessagePriority.High : MessagePriority.Normal;
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId;
            
            HealthStatus = healthStatus;
            HealthyTargetCount = healthyTargetCount;
            TotalTargetCount = totalTargetCount;
            ActiveChannelCount = activeChannelCount;
            MessagesPerSecond = messagesPerSecond;
            ErrorRatePercent = errorRatePercent;
            MemoryUsageBytes = memoryUsageBytes;
            UptimeTicks = uptime.Ticks;
            HealthCheckCorrelationId = healthCheckCorrelationId;
            StatusDetails = statusDetails;
        }

        /// <summary>
        /// Gets the uptime as a TimeSpan.
        /// </summary>
        public TimeSpan Uptime => new TimeSpan(UptimeTicks);

        /// <summary>
        /// Gets the percentage of healthy targets.
        /// </summary>
        public float HealthyTargetPercentage => TotalTargetCount > 0 ? (float)HealthyTargetCount / TotalTargetCount * 100f : 0f;

        /// <summary>
        /// Creates a LoggingSystemHealthMessage from individual parameters for convenience.
        /// </summary>
        /// <param name="isHealthy">Whether the system is healthy</param>
        /// <param name="healthyTargets">Number of healthy targets</param>
        /// <param name="totalTargets">Total number of targets</param>
        /// <param name="activeChannels">Number of active channels</param>
        /// <param name="throughput">Messages per second</param>
        /// <param name="errorRate">Error rate percentage</param>
        /// <param name="memoryUsage">Memory usage in bytes</param>
        /// <param name="uptime">System uptime</param>
        /// <param name="details">Additional details</param>
        /// <param name="correlationId">Message correlation ID</param>
        /// <returns>A new LoggingSystemHealthMessage</returns>
        public static LoggingSystemHealthMessage Create(
            bool isHealthy,
            int healthyTargets = 0,
            int totalTargets = 0,
            int activeChannels = 0,
            float throughput = 0f,
            float errorRate = 0f,
            long memoryUsage = 0,
            TimeSpan? uptime = null,
            string details = null,
            Guid correlationId = default)
        {
            var status = isHealthy ? LoggingSystemHealthStatus.Healthy : LoggingSystemHealthStatus.Unhealthy;
            if (errorRate > 50f) status = LoggingSystemHealthStatus.Critical;
            else if (errorRate > 10f) status = LoggingSystemHealthStatus.Degraded;

            return new LoggingSystemHealthMessage(
                status,
                healthyTargets,
                totalTargets,
                activeChannels,
                throughput,
                errorRate,
                memoryUsage,
                uptime ?? TimeSpan.Zero,
                default,
                new FixedString512Bytes(details ?? string.Empty),
                correlationId);
        }

        /// <summary>
        /// Returns a string representation of this message.
        /// </summary>
        /// <returns>A formatted string</returns>
        public override string ToString()
        {
            return $"LoggingSystemHealth: {HealthStatus} - {HealthyTargetCount}/{TotalTargetCount} targets healthy, " +
                   $"{MessagesPerSecond:F1} msg/s, {ErrorRatePercent:F2}% errors, uptime: {Uptime.TotalHours:F1}h";
        }
    }

    /// <summary>
    /// Defines the health status levels for the logging system.
    /// </summary>
    public enum LoggingSystemHealthStatus : byte
    {
        /// <summary>
        /// System is fully operational with no issues.
        /// </summary>
        Healthy = 0,

        /// <summary>
        /// System is operational but with some performance degradation.
        /// </summary>
        Degraded = 1,

        /// <summary>
        /// System has significant issues but is still functional.
        /// </summary>
        Unhealthy = 2,

        /// <summary>
        /// System is in critical state and may not be functional.
        /// </summary>
        Critical = 3,

        /// <summary>
        /// System status is unknown or cannot be determined.
        /// </summary>
        Unknown = 4
    }
}