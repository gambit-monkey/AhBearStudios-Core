using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published periodically to report pool strategy health status.
    /// Provides health metrics and status information for monitoring systems.
    /// </summary>
    public readonly record struct PoolStrategyHealthStatusMessage : IMessage
    {
        /// <summary>
        /// Gets the unique identifier for this message instance.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets the timestamp when this message was created, in UTC ticks.
        /// </summary>
        public long TimestampTicks { get; init; }

        /// <summary>
        /// Gets the message type code for efficient routing and filtering.
        /// </summary>
        public ushort TypeCode { get; init; } = MessageTypeCodes.PoolStrategyHealthStatusMessage;

        /// <summary>
        /// Gets the source system or component that created this message.
        /// </summary>
        public FixedString64Bytes Source { get; init; }

        /// <summary>
        /// Gets the priority level for message processing.
        /// </summary>
        public MessagePriority Priority { get; init; }

        /// <summary>
        /// Gets optional correlation ID for message tracing across systems.
        /// </summary>
        public Guid CorrelationId { get; init; }

        // Strategy health-specific properties
        /// <summary>
        /// Gets the name of the strategy reporting health status.
        /// </summary>
        public FixedString64Bytes StrategyName { get; init; }

        /// <summary>
        /// Gets whether the strategy is currently healthy.
        /// </summary>
        public bool IsHealthy { get; init; }

        /// <summary>
        /// Gets the current error count for this strategy.
        /// </summary>
        public int ErrorCount { get; init; }

        /// <summary>
        /// Gets the timestamp of the last health check in UTC ticks.
        /// </summary>
        public long LastHealthCheckTicks { get; init; }

        /// <summary>
        /// Gets a status message describing the current health state.
        /// </summary>
        public FixedString512Bytes StatusMessage { get; init; }

        /// <summary>
        /// Gets the average operation duration in milliseconds (if available).
        /// </summary>
        public double AverageOperationDurationMs { get; init; }

        /// <summary>
        /// Gets the total number of operations processed by this strategy.
        /// </summary>
        public long TotalOperations { get; init; }

        /// <summary>
        /// Gets the success rate percentage (0-100).
        /// </summary>
        public double SuccessRatePercentage { get; init; }

        /// <summary>
        /// Initializes a new instance of the PoolStrategyHealthStatusMessage struct.
        /// </summary>
        public PoolStrategyHealthStatusMessage()
        {
            Id = default;
            TimestampTicks = default;
            Source = default;
            Priority = default;
            CorrelationId = default;
            StrategyName = default;
            IsHealthy = default;
            ErrorCount = default;
            LastHealthCheckTicks = default;
            StatusMessage = default;
            AverageOperationDurationMs = default;
            TotalOperations = default;
            SuccessRatePercentage = default;
        }

        /// <summary>
        /// Gets the timestamp when the health status was reported.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the timestamp of the last health check.
        /// </summary>
        public DateTime LastHealthCheck => new DateTime(LastHealthCheckTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new PoolStrategyHealthStatusMessage with the specified details.
        /// </summary>
        /// <param name="strategyName">Name of the strategy</param>
        /// <param name="isHealthy">Whether strategy is healthy</param>
        /// <param name="errorCount">Current error count</param>
        /// <param name="lastHealthCheck">Last health check timestamp</param>
        /// <param name="statusMessage">Health status description</param>
        /// <param name="averageOperationDurationMs">Average operation duration</param>
        /// <param name="totalOperations">Total operations processed</param>
        /// <param name="successRatePercentage">Success rate percentage</param>
        /// <param name="source">Source component</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New PoolStrategyHealthStatusMessage instance</returns>
        public static PoolStrategyHealthStatusMessage Create(
            string strategyName,
            bool isHealthy,
            int errorCount,
            DateTime lastHealthCheck,
            string statusMessage,
            double averageOperationDurationMs = 0.0,
            long totalOperations = 0,
            double successRatePercentage = 100.0,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new PoolStrategyHealthStatusMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.PoolStrategyHealthStatusMessage,
                Source = source.IsEmpty ? "PoolStrategy" : source,
                Priority = isHealthy ? MessagePriority.Low : MessagePriority.Normal,
                CorrelationId = correlationId,
                StrategyName = strategyName?.Length <= 64 ? strategyName : strategyName?[..64] ?? "Unknown",
                IsHealthy = isHealthy,
                ErrorCount = errorCount,
                LastHealthCheckTicks = lastHealthCheck.Ticks,
                StatusMessage = statusMessage?.Length <= 256 ? statusMessage : statusMessage?[..256] ?? "Unknown",
                AverageOperationDurationMs = averageOperationDurationMs,
                TotalOperations = totalOperations,
                SuccessRatePercentage = Math.Max(0, Math.Min(100, successRatePercentage))
            };
        }
    }
}