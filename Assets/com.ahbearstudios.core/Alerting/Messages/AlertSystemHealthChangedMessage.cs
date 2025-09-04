using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Alerting.Messages
{
    /// <summary>
    /// Message published when the alert system health status changes.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct AlertSystemHealthChangedMessage : IMessage
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
        /// Gets the previous health status.
        /// </summary>
        public bool PreviousHealthStatus { get; init; }

        /// <summary>
        /// Gets the current health status.
        /// </summary>
        public bool CurrentHealthStatus { get; init; }

        /// <summary>
        /// Gets a description of what caused the health change.
        /// </summary>
        public FixedString512Bytes HealthChangeReason { get; init; }

        /// <summary>
        /// Gets the number of healthy channels.
        /// </summary>
        public int HealthyChannelsCount { get; init; }

        /// <summary>
        /// Gets the total number of channels.
        /// </summary>
        public int TotalChannelsCount { get; init; }

        /// <summary>
        /// Gets the number of active alerts.
        /// </summary>
        public int ActiveAlertsCount { get; init; }

        /// <summary>
        /// Gets the system uptime in seconds.
        /// </summary>
        public long UptimeSeconds { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the uptime as a TimeSpan.
        /// </summary>
        public TimeSpan Uptime => TimeSpan.FromSeconds(UptimeSeconds);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new AlertSystemHealthChangedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="previousHealth">Previous health status</param>
        /// <param name="currentHealth">Current health status</param>
        /// <param name="reason">Reason for the health change</param>
        /// <param name="healthyChannels">Number of healthy channels</param>
        /// <param name="totalChannels">Total number of channels</param>
        /// <param name="activeAlerts">Number of active alerts</param>
        /// <param name="uptimeSeconds">System uptime in seconds</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New AlertSystemHealthChangedMessage instance</returns>
        public static AlertSystemHealthChangedMessage Create(
            bool previousHealth,
            bool currentHealth,
            string reason,
            int healthyChannels,
            int totalChannels,
            int activeAlerts,
            long uptimeSeconds,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            // Input validation
            if (healthyChannels < 0 || totalChannels < 0 || activeAlerts < 0 || uptimeSeconds < 0)
                throw new ArgumentException("Count values cannot be negative");

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "AlertService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("AlertSystemHealthChangedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("AlertSystemHealthChange", $"{previousHealth}-{currentHealth}")
                : correlationId;

            return new AlertSystemHealthChangedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.AlertSystemHealthChangedMessage,
                Source = source.IsEmpty ? "AlertService" : source,
                Priority = currentHealth ? MessagePriority.Normal : MessagePriority.High,
                CorrelationId = finalCorrelationId,
                PreviousHealthStatus = previousHealth,
                CurrentHealthStatus = currentHealth,
                HealthChangeReason = reason?.Length <= 512 ? reason : reason?[..512] ?? "Unknown",
                HealthyChannelsCount = healthyChannels,
                TotalChannelsCount = totalChannels,
                ActiveAlertsCount = activeAlerts,
                UptimeSeconds = uptimeSeconds
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Health change message string representation</returns>
        public override string ToString()
        {
            var healthStatus = CurrentHealthStatus ? "Healthy" : "Unhealthy";
            var reasonText = HealthChangeReason.IsEmpty ? "No reason specified" : HealthChangeReason.ToString();
            return $"AlertSystemHealthChanged: {healthStatus} ({HealthyChannelsCount}/{TotalChannelsCount} channels) - {reasonText}";
        }

        #endregion
    }
}