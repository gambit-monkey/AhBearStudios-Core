using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Messages
{
    /// <summary>
    /// Message published when an alert channel is registered with the service.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct AlertChannelRegisteredMessage : IMessage
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

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the channel type from the configuration.
        /// </summary>
        public AlertChannelType ChannelType => Configuration?.ChannelType ?? AlertChannelType.Log;

        #endregion

        #region Static Factory Methods


        /// <summary>
        /// Creates a new AlertChannelRegisteredMessage with proper validation and defaults.
        /// </summary>
        /// <param name="channelName">Name of the registered channel</param>
        /// <param name="configuration">Channel configuration</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="priority">Message priority level</param>
        /// <returns>New AlertChannelRegisteredMessage instance</returns>
        public static AlertChannelRegisteredMessage Create(
            FixedString64Bytes channelName,
            ChannelConfig configuration,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            MessagePriority priority = MessagePriority.Low)
        {
            // Input validation
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "AlertChannelService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("AlertChannelRegisteredMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("AlertChannelRegistration", channelName.ToString())
                : correlationId;

            return new AlertChannelRegisteredMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.AlertChannelRegisteredMessage,
                Source = source.IsEmpty ? "AlertChannelService" : source,
                Priority = priority,
                CorrelationId = finalCorrelationId,
                ChannelName = channelName,
                Configuration = configuration
            };
        }

        #endregion
    }
}