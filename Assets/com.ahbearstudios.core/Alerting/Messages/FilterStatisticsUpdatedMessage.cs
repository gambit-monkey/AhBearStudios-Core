using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Alerting.Filters;

namespace AhBearStudios.Core.Alerting.Messages
{
    /// <summary>
    /// Message published when filter statistics are updated.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct FilterStatisticsUpdatedMessage : IMessage
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

        // Filter statistics-specific properties
        /// <summary>
        /// Gets the name of the filter whose statistics were updated.
        /// </summary>
        public FixedString64Bytes FilterName { get; init; }

        /// <summary>
        /// Gets the updated filter statistics.
        /// </summary>
        public FilterStatistics Statistics { get; init; }

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new FilterStatisticsUpdatedMessage.
        /// </summary>
        /// <param name="filterName">Name of the filter</param>
        /// <param name="statistics">Updated statistics</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New FilterStatisticsUpdatedMessage instance</returns>
        public static FilterStatisticsUpdatedMessage Create(
            FixedString64Bytes filterName,
            FilterStatistics statistics,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new FilterStatisticsUpdatedMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.FilterStatisticsUpdated,
                Source = source.IsEmpty ? "AlertFilterService" : source,
                Priority = MessagePriority.Low, // Statistics updates are low priority
                CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
                FilterName = filterName,
                Statistics = statistics
            };
        }

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Filter statistics update message string representation</returns>
        public override string ToString()
        {
            return $"FilterStatisticsUpdated: {FilterName} - {Statistics.TotalEvaluations} evaluations, {Statistics.SuppressionRate:F1}% suppression rate";
        }
    }
}