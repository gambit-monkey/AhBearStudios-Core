using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Alerting.Configs;

namespace AhBearStudios.Core.Alerting.Messages
{
    /// <summary>
    /// Message published when an alert channel is registered with the service.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct ChannelRegisteredMessage : IMessage
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

        #region Channel-specific Properties

        /// <summary>
        /// Gets the name of the registered channel.
        /// </summary>
        public FixedString64Bytes ChannelName { get; init; }

        /// <summary>
        /// Gets the channel configuration.
        /// </summary>
        public ChannelConfig Configuration { get; init; }

        #endregion

        #region Helper Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        /// <summary>
        /// Creates a new ChannelRegisteredMessage.
        /// </summary>
        /// <param name="channelName">Name of the registered channel</param>
        /// <param name="configuration">Channel configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="source">Source component creating this message</param>
        /// <returns>New ChannelRegisteredMessage instance</returns>
        public static ChannelRegisteredMessage Create(
            FixedString64Bytes channelName,
            ChannelConfig configuration,
            Guid correlationId = default,
            FixedString64Bytes source = default)
        {
            return new ChannelRegisteredMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.ChannelRegistered,
                Source = source.IsEmpty ? "AlertChannelService" : source,
                Priority = MessagePriority.Low,
                CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
                ChannelName = channelName,
                Configuration = configuration
            };
        }
    }
}