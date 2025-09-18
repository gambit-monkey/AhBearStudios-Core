using System;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Message published when health check statistics are reset.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct HealthCheckStatisticsResetMessage : IMessage
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
        /// Gets the name of the health check whose statistics were reset.
        /// Empty if all statistics were reset.
        /// </summary>
        public FixedString64Bytes HealthCheckName { get; init; }

        /// <summary>
        /// Gets the reason for the statistics reset.
        /// </summary>
        public FixedString512Bytes Reason { get; init; }

        /// <summary>
        /// Gets the number of data points that were cleared.
        /// </summary>
        public int DataPointsCleared { get; init; }

        /// <summary>
        /// Gets the time span of data that was cleared (in seconds).
        /// </summary>
        public double DataTimeSpanSeconds { get; init; }

        /// <summary>
        /// Gets whether this was a full reset (all statistics).
        /// </summary>
        public bool IsFullReset { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new HealthCheckStatisticsResetMessage following CLAUDE.md patterns.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check (empty for full reset)</param>
        /// <param name="reason">Reason for the reset</param>
        /// <param name="dataPointsCleared">Number of data points cleared</param>
        /// <param name="dataTimeSpanSeconds">Time span of cleared data in seconds</param>
        /// <param name="isFullReset">Whether this was a full reset</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>New HealthCheckStatisticsResetMessage instance</returns>
        public static HealthCheckStatisticsResetMessage Create(
            string healthCheckName = null,
            string reason = "Manual reset",
            int dataPointsCleared = 0,
            double dataTimeSpanSeconds = 0,
            bool isFullReset = false,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "HealthStatisticsCollector" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckStatisticsResetMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("StatisticsReset", healthCheckName ?? "FullReset")
                : correlationId;

            return new HealthCheckStatisticsResetMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckStatisticsResetMessage,
                Source = source.IsEmpty ? "HealthStatisticsCollector" : source,
                Priority = MessagePriority.Low,
                CorrelationId = finalCorrelationId,
                HealthCheckName = healthCheckName?.Length <= 64 ? healthCheckName : healthCheckName?[..64] ?? string.Empty,
                Reason = reason?.Length <= 512 ? reason : reason?[..512] ?? "Manual reset",
                DataPointsCleared = dataPointsCleared,
                DataTimeSpanSeconds = dataTimeSpanSeconds,
                IsFullReset = isFullReset
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Statistics reset message string representation</returns>
        public override string ToString()
        {
            var target = IsFullReset ? "All statistics" :
                        HealthCheckName.IsEmpty ? "Unknown" : HealthCheckName.ToString();
            return $"StatisticsReset: {target} - {DataPointsCleared} points cleared ({DataTimeSpanSeconds:F1}s) - {Reason}";
        }

        #endregion
    }
}