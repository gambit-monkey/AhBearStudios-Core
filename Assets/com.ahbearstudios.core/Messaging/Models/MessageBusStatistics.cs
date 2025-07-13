using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Models
{
    /// <summary>
    /// Comprehensive statistics for message bus performance and health monitoring.
    /// </summary>
    public readonly struct MessageBusStatistics
    {
        /// <summary>
        /// Gets the total number of messages published through this bus.
        /// </summary>
        public readonly long TotalMessagesPublished;

        /// <summary>
        /// Gets the total number of messages successfully processed.
        /// </summary>
        public readonly long TotalMessagesProcessed;

        /// <summary>
        /// Gets the total number of failed message processing attempts.
        /// </summary>
        public readonly long TotalMessagesFailed;

        /// <summary>
        /// Gets the current number of active subscribers.
        /// </summary>
        public readonly int ActiveSubscribers;

        /// <summary>
        /// Gets the current queue depth across all message types.
        /// </summary>
        public readonly int CurrentQueueDepth;

        /// <summary>
        /// Gets the average message processing time in milliseconds.
        /// </summary>
        public readonly double AverageProcessingTimeMs;

        /// <summary>
        /// Gets the peak processing time in milliseconds.
        /// </summary>
        public readonly double PeakProcessingTimeMs;

        /// <summary>
        /// Gets the current messages per second throughput.
        /// </summary>
        public readonly double MessagesPerSecond;

        /// <summary>
        /// Gets the timestamp when statistics were last updated.
        /// </summary>
        public readonly long LastUpdatedTicks;

        /// <summary>
        /// Gets the number of messages currently in retry state.
        /// </summary>
        public readonly int MessagesInRetry;

        /// <summary>
        /// Gets the number of messages in the dead letter queue.
        /// </summary>
        public readonly int DeadLetterQueueSize;

        /// <summary>
        /// Gets the total memory allocated for messaging operations in bytes.
        /// </summary>
        public readonly long TotalMemoryAllocated;

        /// <summary>
        /// Gets the instance name of this message bus.
        /// </summary>
        public readonly FixedString64Bytes InstanceName;

        /// <summary>
        /// Initializes a new instance of MessageBusStatistics.
        /// </summary>
        /// <param name="totalMessagesPublished">Total messages published</param>
        /// <param name="totalMessagesProcessed">Total messages processed</param>
        /// <param name="totalMessagesFailed">Total messages failed</param>
        /// <param name="activeSubscribers">Current active subscribers</param>
        /// <param name="currentQueueDepth">Current queue depth</param>
        /// <param name="averageProcessingTimeMs">Average processing time in milliseconds</param>
        /// <param name="peakProcessingTimeMs">Peak processing time in milliseconds</param>
        /// <param name="messagesPerSecond">Current throughput</param>
        /// <param name="lastUpdatedTicks">Last update timestamp</param>
        /// <param name="messagesInRetry">Messages currently in retry</param>
        /// <param name="deadLetterQueueSize">Dead letter queue size</param>
        /// <param name="totalMemoryAllocated">Total memory allocated</param>
        /// <param name="instanceName">Instance name</param>
        public MessageBusStatistics(
            long totalMessagesPublished,
            long totalMessagesProcessed,
            long totalMessagesFailed,
            int activeSubscribers,
            int currentQueueDepth,
            double averageProcessingTimeMs,
            double peakProcessingTimeMs,
            double messagesPerSecond,
            long lastUpdatedTicks,
            int messagesInRetry,
            int deadLetterQueueSize,
            long totalMemoryAllocated,
            FixedString64Bytes instanceName)
        {
            TotalMessagesPublished = totalMessagesPublished;
            TotalMessagesProcessed = totalMessagesProcessed;
            TotalMessagesFailed = totalMessagesFailed;
            ActiveSubscribers = activeSubscribers;
            CurrentQueueDepth = currentQueueDepth;
            AverageProcessingTimeMs = averageProcessingTimeMs;
            PeakProcessingTimeMs = peakProcessingTimeMs;
            MessagesPerSecond = messagesPerSecond;
            LastUpdatedTicks = lastUpdatedTicks;
            MessagesInRetry = messagesInRetry;
            DeadLetterQueueSize = deadLetterQueueSize;
            TotalMemoryAllocated = totalMemoryAllocated;
            InstanceName = instanceName;
        }

        /// <summary>
        /// Gets the DateTime representation of the last updated timestamp.
        /// </summary>
        public DateTime LastUpdated => new DateTime(LastUpdatedTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the success rate as a percentage (0.0 to 1.0).
        /// </summary>
        public double SuccessRate => TotalMessagesPublished > 0 
            ? (double)TotalMessagesProcessed / TotalMessagesPublished 
            : 1.0;

        /// <summary>
        /// Gets the failure rate as a percentage (0.0 to 1.0).
        /// </summary>
        public double FailureRate => TotalMessagesPublished > 0 
            ? (double)TotalMessagesFailed / TotalMessagesPublished 
            : 0.0;

        /// <summary>
        /// Gets whether the message bus is operating within healthy parameters.
        /// </summary>
        public bool IsHealthy => SuccessRate >= 0.95 && 
                                CurrentQueueDepth < 1000 && 
                                AverageProcessingTimeMs < 100;

        /// <summary>
        /// Gets whether the message bus is in a degraded state.
        /// </summary>
        public bool IsDegraded => SuccessRate >= 0.85 && SuccessRate < 0.95 ||
                                 CurrentQueueDepth >= 1000 && CurrentQueueDepth < 5000 ||
                                 AverageProcessingTimeMs >= 100 && AverageProcessingTimeMs < 500;

        /// <summary>
        /// Gets whether the message bus is in an unhealthy state.
        /// </summary>
        public bool IsUnhealthy => !IsHealthy && !IsDegraded;

        /// <summary>
        /// Creates an empty statistics instance.
        /// </summary>
        /// <param name="instanceName">The instance name</param>
        /// <returns>Empty statistics</returns>
        public static MessageBusStatistics Empty(FixedString64Bytes instanceName) => new(
            0, 0, 0, 0, 0, 0, 0, 0, DateTime.UtcNow.Ticks, 0, 0, 0, instanceName);

        /// <summary>
        /// Returns a string representation of the statistics.
        /// </summary>
        /// <returns>Formatted statistics string</returns>
        public override string ToString() =>
            $"MessageBus[{InstanceName}]: " +
            $"Published={TotalMessagesPublished}, " +
            $"Processed={TotalMessagesProcessed}, " +
            $"Failed={TotalMessagesFailed}, " +
            $"Subscribers={ActiveSubscribers}, " +
            $"QueueDepth={CurrentQueueDepth}, " +
            $"AvgTime={AverageProcessingTimeMs:F2}ms, " +
            $"Throughput={MessagesPerSecond:F2}/sec";
    }
}