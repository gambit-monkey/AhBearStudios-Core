using System;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Message published when a health check exceeds performance thresholds.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct HealthCheckPerformanceThresholdExceededMessage : IMessage
    {
        #region IMessage Implementation

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
        public ushort TypeCode { get; init; }

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

        #endregion

        #region Message-Specific Properties

        /// <summary>
        /// Gets the name of the health check that exceeded thresholds.
        /// </summary>
        public FixedString64Bytes HealthCheckName { get; init; }

        /// <summary>
        /// Gets the type of threshold that was exceeded.
        /// </summary>
        public FixedString64Bytes ThresholdType { get; init; }

        /// <summary>
        /// Gets the actual execution time in milliseconds.
        /// </summary>
        public double ActualDurationMs { get; init; }

        /// <summary>
        /// Gets the threshold value that was exceeded.
        /// </summary>
        public double ThresholdMs { get; init; }

        /// <summary>
        /// Gets the percentage over the threshold.
        /// </summary>
        public double PercentageOverThreshold { get; init; }

        /// <summary>
        /// Gets the number of consecutive times the threshold has been exceeded.
        /// </summary>
        public int ConsecutiveExceeds { get; init; }

        /// <summary>
        /// Gets the health check category.
        /// </summary>
        public HealthCheckCategory Category { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new HealthCheckPerformanceThresholdExceededMessage following CLAUDE.md patterns.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="thresholdType">Type of threshold exceeded (e.g., "ExecutionTime", "MemoryUsage")</param>
        /// <param name="actualDurationMs">Actual execution time in milliseconds</param>
        /// <param name="thresholdMs">Threshold value in milliseconds</param>
        /// <param name="consecutiveExceeds">Number of consecutive threshold exceeds</param>
        /// <param name="category">Health check category</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>New HealthCheckPerformanceThresholdExceededMessage instance</returns>
        public static HealthCheckPerformanceThresholdExceededMessage Create(
            string healthCheckName,
            string thresholdType = "ExecutionTime",
            double actualDurationMs = 0,
            double thresholdMs = 0,
            int consecutiveExceeds = 1,
            HealthCheckCategory category = HealthCheckCategory.System,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            // Input validation
            if (string.IsNullOrEmpty(healthCheckName))
                throw new ArgumentException("Health check name cannot be null or empty", nameof(healthCheckName));

            // Calculate percentage over threshold
            var percentageOver = thresholdMs > 0
                ? ((actualDurationMs - thresholdMs) / thresholdMs) * 100.0
                : 0;

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "HealthStatisticsCollector" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckPerformanceThresholdExceededMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("PerformanceThreshold", $"{healthCheckName}-{thresholdType}")
                : correlationId;

            return new HealthCheckPerformanceThresholdExceededMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckPerformanceThresholdExceededMessage,
                Source = source.IsEmpty ? "HealthStatisticsCollector" : source,
                Priority = consecutiveExceeds > 5 ? MessagePriority.Critical : MessagePriority.High,
                CorrelationId = finalCorrelationId,
                HealthCheckName = healthCheckName.Length <= 64 ? healthCheckName : healthCheckName[..64],
                ThresholdType = thresholdType?.Length <= 64 ? thresholdType : thresholdType?[..64] ?? "ExecutionTime",
                ActualDurationMs = actualDurationMs,
                ThresholdMs = thresholdMs,
                PercentageOverThreshold = percentageOver,
                ConsecutiveExceeds = consecutiveExceeds,
                Category = category
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Performance threshold exceeded message string representation</returns>
        public override string ToString()
        {
            return $"PerformanceThresholdExceeded: {HealthCheckName} - {ThresholdType} {ActualDurationMs:F2}ms > {ThresholdMs:F2}ms ({PercentageOverThreshold:F1}% over, {ConsecutiveExceeds} consecutive)";
        }

        #endregion
    }
}