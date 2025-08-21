using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

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
        public ushort TypeCode { get; init; } = MessageTypeCodes.HealthCheckStatusChangedMessage;

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
        /// Gets the scores for various health check categories.
        /// Note: Using simplified structure for Unity performance - consider pooled collections for complex scenarios.
        /// </summary>
        public Dictionary<HealthCheckCategory, double> CategoryScores { get; init; }

        /// <summary>
        /// Initializes a new instance of the HealthCheckStatusChangedMessage struct.
        /// </summary>
        public HealthCheckStatusChangedMessage()
        {
            Id = default;
            TimestampTicks = default;
            Source = default;
            Priority = default;
            CorrelationId = default;
            OldStatus = default;
            NewStatus = default;
            CategoryScores = new Dictionary<HealthCheckCategory, double>();
        }

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new HealthCheckStatusChangedMessage.
        /// </summary>
        /// <param name="oldStatus">Previous health status</param>
        /// <param name="newStatus">New health status</param>
        /// <param name="categoryScores">Optional category scores</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New HealthCheckStatusChangedMessage instance</returns>
        public static HealthCheckStatusChangedMessage Create(
            HealthStatus oldStatus,
            HealthStatus newStatus,
            Dictionary<HealthCheckCategory, double> categoryScores = null,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new HealthCheckStatusChangedMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckStatusChangedMessage,
                Source = source.IsEmpty ? "HealthCheckService" : source,
                Priority = newStatus == HealthStatus.Healthy ? MessagePriority.Low : MessagePriority.High,
                CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                CategoryScores = categoryScores ?? new Dictionary<HealthCheckCategory, double>()
            };
        }

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Health status change message string representation</returns>
        public override string ToString()
        {
            return $"HealthStatusChanged: {OldStatus} -> {NewStatus} ({CategoryScores?.Count ?? 0} categories)";
        }
    }
}