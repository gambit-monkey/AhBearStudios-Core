using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
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
        /// Gets the name of the unregistered channel.
        /// </summary>
        public FixedString64Bytes ChannelName { get; init; }

        /// <summary>
        /// Gets the reason for unregistration if available.
        /// </summary>
        public FixedString512Bytes Reason { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods


        /// <summary>
        /// Creates a new AlertChannelUnregisteredMessage with proper validation and defaults.
        /// </summary>
        /// <param name="channelName">Name of the unregistered channel</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="reason">Reason for unregistration</param>
        /// <param name="priority">Message priority level</param>
        /// <returns>New AlertChannelUnregisteredMessage instance</returns>
        public static AlertChannelUnregisteredMessage Create(
            FixedString64Bytes channelName,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            FixedString512Bytes reason = default,
            MessagePriority priority = MessagePriority.Low)
        {
            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "AlertChannelService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("AlertChannelUnregisteredMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("AlertChannelUnregistration", channelName.ToString())
                : correlationId;

            return new AlertChannelUnregisteredMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.AlertChannelUnregisteredMessage,
                Source = source.IsEmpty ? "AlertChannelService" : source,
                Priority = priority,
                CorrelationId = finalCorrelationId,
                ChannelName = channelName,
                Reason = reason.IsEmpty ? "Unregistered" : reason
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Channel unregistered message string representation</returns>
        public override string ToString()
        {
            var channelText = ChannelName.IsEmpty ? "Unknown" : ChannelName.ToString();
            var reasonText = Reason.IsEmpty ? "No reason specified" : Reason.ToString();
            return $"AlertChannelUnregistered: {channelText} - {reasonText}";
        }

        #endregion
    }
}