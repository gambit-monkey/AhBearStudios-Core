using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Message published when health status changes.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct HealthCheckStatusChangedMessage : IMessage
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

        // Health status change-specific properties
        /// <summary>
        /// Gets the previous health status before the change.
        /// </summary>
        public HealthStatus OldStatus { get; init; }

        /// <summary>
        /// Gets the new health status after the change.
        /// </summary>
        public HealthStatus NewStatus { get; init; }

        /// <summary>
        /// Gets the overall health score (0.0 to 1.0).
        /// Uses single score instead of Dictionary to maintain zero-allocation pattern.
        /// </summary>
        public double OverallHealthScore { get; init; }

        /// <summary>
        /// Gets the affected health check category that triggered the status change.
        /// </summary>
        public HealthCheckCategory AffectedCategory { get; init; }

        /// <summary>
        /// Gets the score for the affected category.
        /// </summary>
        public double AffectedCategoryScore { get; init; }


        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new HealthCheckStatusChangedMessage following CLAUDE.md patterns.
        /// </summary>
        /// <param name="oldStatus">Previous health status</param>
        /// <param name="newStatus">New health status</param>
        /// <param name="overallHealthScore">Overall system health score (0.0 to 1.0)</param>
        /// <param name="affectedCategory">Health check category that triggered the change</param>
        /// <param name="affectedCategoryScore">Score for the affected category</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New HealthCheckStatusChangedMessage instance</returns>
        public static HealthCheckStatusChangedMessage Create(
            HealthStatus oldStatus,
            HealthStatus newStatus,
            double overallHealthScore = 1.0,
            HealthCheckCategory affectedCategory = HealthCheckCategory.System,
            double affectedCategoryScore = 1.0,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            var sourceString = source.IsEmpty ? "HealthCheckService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckStatusChangedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("HealthStatusChange", $"{oldStatus}-{newStatus}")
                : correlationId;

            return new HealthCheckStatusChangedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckStatusChangedMessage,
                Source = source.IsEmpty ? "HealthCheckService" : source,
                Priority = newStatus == HealthStatus.Healthy ? MessagePriority.Low : MessagePriority.High,
                CorrelationId = finalCorrelationId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                OverallHealthScore = Math.Clamp(overallHealthScore, 0.0, 1.0),
                AffectedCategory = affectedCategory,
                AffectedCategoryScore = Math.Clamp(affectedCategoryScore, 0.0, 1.0)
            };
        }

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Health status change message string representation</returns>
        public override string ToString()
        {
            return $"HealthStatusChanged: {OldStatus} -> {NewStatus} (Score: {OverallHealthScore:F2}, Category: {AffectedCategory})";
        }
    }
}