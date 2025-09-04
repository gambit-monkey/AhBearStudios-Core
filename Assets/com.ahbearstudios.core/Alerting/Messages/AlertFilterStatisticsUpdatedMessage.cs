using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
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
    public readonly record struct AlertFilterStatisticsUpdatedMessage : IMessage
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
        /// Gets the name of the filter whose statistics were updated.
        /// </summary>
        public FixedString64Bytes FilterName { get; init; }

        /// <summary>
        /// Gets the updated filter statistics.
        /// </summary>
        public FilterStatistics Statistics { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new AlertFilterStatisticsUpdatedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="filterName">Name of the filter</param>
        /// <param name="statistics">Updated statistics</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="priority">Message priority level</param>
        /// <returns>New AlertFilterStatisticsUpdatedMessage instance</returns>
        public static AlertFilterStatisticsUpdatedMessage Create(
            FixedString64Bytes filterName,
            FilterStatistics statistics,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            MessagePriority priority = MessagePriority.Low)
        {
            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "AlertFilterService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("AlertFilterStatisticsUpdatedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("AlertFilterStatsUpdate", filterName.ToString())
                : correlationId;

            return new AlertFilterStatisticsUpdatedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.AlertFilterStatisticsUpdatedMessage,
                Source = source.IsEmpty ? "AlertFilterService" : source,
                Priority = priority, // Statistics updates are typically low priority
                CorrelationId = finalCorrelationId,
                FilterName = filterName,
                Statistics = statistics
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Filter statistics update message string representation</returns>
        public override string ToString()
        {
            var filterText = FilterName.IsEmpty ? "Unknown" : FilterName.ToString();
            return $"FilterStatisticsUpdated: {filterText} - {Statistics.TotalEvaluations} evaluations, {Statistics.SuppressionRate:F1}% suppression rate";
        }

        #endregion
    }
}