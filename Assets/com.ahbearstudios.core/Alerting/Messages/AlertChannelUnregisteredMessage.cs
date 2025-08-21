using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Alerting.Messages
{
    /// <summary>
    /// Message published when an alert channel is unregistered from the service.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct AlertChannelUnregisteredMessage : IMessage
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
        public ushort TypeCode { get; init; } = MessageTypeCodes.AlertChannelUnregisteredMessage;

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

        #region Channel-specific Properties

        /// <summary>
        /// Gets the name of the unregistered channel.
        /// </summary>
        public FixedString64Bytes ChannelName { get; init; }

        /// <summary>
        /// Gets the reason for unregistration if available.
        /// </summary>
        public FixedString512Bytes Reason { get; init; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the AlertChannelUnregisteredMessage struct.
        /// </summary>
        public AlertChannelUnregisteredMessage()
        {
            Id = default;
            TimestampTicks = default;
            Source = default;
            Priority = default;
            CorrelationId = default;
            ChannelName = default;
            Reason = default;
        }

        #region Helper Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        /// <summary>
        /// Creates a new AlertChannelUnregisteredMessage.
        /// </summary>
        /// <param name="channelName">Name of the unregistered channel</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="reason">Reason for unregistration</param>
        /// <returns>New AlertChannelUnregisteredMessage instance</returns>
        public static AlertChannelUnregisteredMessage Create(
            FixedString64Bytes channelName,
            Guid correlationId = default,
            FixedString64Bytes source = default,
            FixedString512Bytes reason = default)
        {
            return new AlertChannelUnregisteredMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.AlertChannelUnregisteredMessage,
                Source = source.IsEmpty ? "AlertChannelService" : source,
                Priority = MessagePriority.Low,
                CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
                ChannelName = channelName,
                Reason = reason.IsEmpty ? "Unregistered" : reason
            };
        }
    }
}