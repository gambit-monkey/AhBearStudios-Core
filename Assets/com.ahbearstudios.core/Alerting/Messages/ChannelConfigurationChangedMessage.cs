using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Alerting.Configs;

namespace AhBearStudios.Core.Alerting.Messages
{
    /// <summary>
    /// Message published when an alert channel's configuration changes.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct ChannelConfigurationChangedMessage : IMessage
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

        // Channel configuration-specific properties
        /// <summary>
        /// Gets the name of the channel whose configuration changed.
        /// </summary>
        public FixedString64Bytes ChannelName { get; init; }

        /// <summary>
        /// Gets the previous channel configuration.
        /// </summary>
        public ChannelConfig PreviousConfiguration { get; init; }

        /// <summary>
        /// Gets the new channel configuration.
        /// </summary>
        public ChannelConfig NewConfiguration { get; init; }

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new ChannelConfigurationChangedMessage.
        /// </summary>
        /// <param name="channelName">Name of the channel</param>
        /// <param name="previousConfig">Previous configuration</param>
        /// <param name="newConfig">New configuration</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New ChannelConfigurationChangedMessage instance</returns>
        public static ChannelConfigurationChangedMessage Create(
            FixedString64Bytes channelName,
            ChannelConfig previousConfig,
            ChannelConfig newConfig,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new ChannelConfigurationChangedMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.ChannelConfigurationChanged,
                Source = source.IsEmpty ? "AlertChannelService" : source,
                Priority = MessagePriority.Low,
                CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
                ChannelName = channelName,
                PreviousConfiguration = previousConfig,
                NewConfiguration = newConfig
            };
        }

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Configuration change message string representation</returns>
        public override string ToString()
        {
            return $"ChannelConfigurationChanged: {ChannelName} configuration updated - enabled: {NewConfiguration?.IsEnabled}, severity: {NewConfiguration?.MinimumSeverity}";
        }
    }
}