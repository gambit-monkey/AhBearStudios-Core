using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
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
    public readonly record struct AlertChannelHealthChangedMessage : IMessage
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

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new AlertChannelHealthChangedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="channelName">Name of the channel</param>
        /// <param name="previousHealth">Previous health status</param>
        /// <param name="currentHealth">Current health status</param>
        /// <param name="healthResult">Health check result</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="priority">Message priority level (defaults based on health status)</param>
        /// <returns>New AlertChannelHealthChangedMessage instance</returns>
        public static AlertChannelHealthChangedMessage Create(
            FixedString64Bytes channelName,
            bool previousHealth,
            bool currentHealth,
            ChannelHealthResult healthResult,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            MessagePriority? priority = null)
        {

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "AlertChannelService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("AlertChannelHealthChangedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("AlertChannelHealthChange", channelName.ToString())
                : correlationId;

            // Default priority based on health status (unhealthy = higher priority)
            var messagePriority = priority ?? (currentHealth ? MessagePriority.Low : MessagePriority.High);

            return new AlertChannelHealthChangedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.AlertChannelHealthChangedMessage,
                Source = source.IsEmpty ? "AlertChannelService" : source,
                Priority = messagePriority,
                CorrelationId = finalCorrelationId,
                ChannelName = channelName,
                PreviousHealthStatus = previousHealth,
                CurrentHealthStatus = currentHealth,
                HealthResult = healthResult
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Channel health change message string representation</returns>
        public override string ToString()
        {
            var channelText = ChannelName.IsEmpty ? "Unknown" : ChannelName.ToString();
            var statusChange = CurrentHealthStatus ? "became healthy" : "became unhealthy";
            var statusMessage = HealthResult.StatusMessage.IsEmpty ? "No status message" : HealthResult.StatusMessage.ToString();
            return $"ChannelHealthChanged: {channelText} {statusChange} - {statusMessage}";
        }

        #endregion
    }
}