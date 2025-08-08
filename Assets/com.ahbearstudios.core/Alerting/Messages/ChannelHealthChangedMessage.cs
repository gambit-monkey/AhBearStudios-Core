using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Alerting.Channels;

namespace AhBearStudios.Core.Alerting.Messages
{
    /// <summary>
    /// Message published when an alert channel's health status changes.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct ChannelHealthChangedMessage : IMessage
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

        // Channel health-specific properties
        /// <summary>
        /// Gets the name of the channel whose health changed.
        /// </summary>
        public FixedString64Bytes ChannelName { get; init; }

        /// <summary>
        /// Gets the previous health status.
        /// </summary>
        public bool PreviousHealthStatus { get; init; }

        /// <summary>
        /// Gets the current health status.
        /// </summary>
        public bool CurrentHealthStatus { get; init; }

        /// <summary>
        /// Gets the health check result details.
        /// </summary>
        public ChannelHealthResult HealthResult { get; init; }

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new ChannelHealthChangedMessage.
        /// </summary>
        /// <param name="channelName">Name of the channel</param>
        /// <param name="previousHealth">Previous health status</param>
        /// <param name="currentHealth">Current health status</param>
        /// <param name="healthResult">Health check result</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New ChannelHealthChangedMessage instance</returns>
        public static ChannelHealthChangedMessage Create(
            FixedString64Bytes channelName,
            bool previousHealth,
            bool currentHealth,
            ChannelHealthResult healthResult,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new ChannelHealthChangedMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.ChannelHealthChanged,
                Source = source.IsEmpty ? "AlertChannelService" : source,
                Priority = currentHealth ? MessagePriority.Low : MessagePriority.High,
                CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
                ChannelName = channelName,
                PreviousHealthStatus = previousHealth,
                CurrentHealthStatus = currentHealth,
                HealthResult = healthResult
            };
        }

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Channel health change message string representation</returns>
        public override string ToString()
        {
            var statusChange = CurrentHealthStatus ? "became healthy" : "became unhealthy";
            return $"ChannelHealthChanged: {ChannelName} {statusChange} - {HealthResult.StatusMessage}";
        }
    }
}