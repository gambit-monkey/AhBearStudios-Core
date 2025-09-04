using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Messages
{
    /// <summary>
    /// Message published when an alert is acknowledged in the system.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct AlertAcknowledgedMessage : IMessage
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
        /// Gets the unique identifier of the acknowledged alert.
        /// </summary>
        public Guid AlertId { get; init; }

        /// <summary>
        /// Gets the alert message content for context.
        /// </summary>
        public FixedString512Bytes AlertMessage { get; init; }

        /// <summary>
        /// Gets the severity level of the acknowledged alert.
        /// </summary>
        public AlertSeverity AlertSeverity { get; init; }

        /// <summary>
        /// Gets the source system that originally raised the alert.
        /// </summary>
        public FixedString64Bytes AlertSource { get; init; }

        /// <summary>
        /// Gets the categorization tag for the alert.
        /// </summary>
        public FixedString32Bytes AlertTag { get; init; }

        /// <summary>
        /// Gets the operation ID associated with the original alert.
        /// </summary>
        public Guid OperationId { get; init; }

        /// <summary>
        /// Gets the user or system that acknowledged the alert.
        /// </summary>
        public FixedString64Bytes AcknowledgedBy { get; init; }

        /// <summary>
        /// Gets the timestamp when the alert was acknowledged, in UTC ticks.
        /// </summary>
        public long AcknowledgedTimestampTicks { get; init; }

        /// <summary>
        /// Gets the timestamp when the original alert was created, in UTC ticks.
        /// </summary>
        public long OriginalAlertTimestampTicks { get; init; }

        /// <summary>
        /// Gets the duration between alert creation and acknowledgment in ticks.
        /// </summary>
        public long AcknowledgmentDurationTicks { get; init; }

        /// <summary>
        /// Gets the count for the alert at time of acknowledgment.
        /// </summary>
        public int AlertCount { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the acknowledgment timestamp.
        /// </summary>
        public DateTime AcknowledgedTimestamp => new DateTime(AcknowledgedTimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the DateTime representation of the original alert timestamp.
        /// </summary>
        public DateTime OriginalAlertTimestamp => new DateTime(OriginalAlertTimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the acknowledgment duration as TimeSpan.
        /// </summary>
        public TimeSpan AcknowledgmentDuration => new TimeSpan(AcknowledgmentDurationTicks);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new AlertAcknowledgedMessage from an acknowledged Alert instance with proper validation and defaults.
        /// </summary>
        /// <param name="acknowledgedAlert">The acknowledged alert</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New AlertAcknowledgedMessage instance</returns>
        public static AlertAcknowledgedMessage Create(
            Alert acknowledgedAlert,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            // Input validation
            if (acknowledgedAlert == null)
                throw new ArgumentNullException(nameof(acknowledgedAlert));

            if (!acknowledgedAlert.IsAcknowledged)
                throw new ArgumentException("Alert must be acknowledged", nameof(acknowledgedAlert));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "AlertService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("AlertAcknowledgedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? acknowledgedAlert.CorrelationId 
                : correlationId;

            var acknowledgmentDuration = acknowledgedAlert.AcknowledgedTimestampTicks.HasValue
                ? acknowledgedAlert.AcknowledgedTimestampTicks.Value - acknowledgedAlert.TimestampTicks
                : 0;

            return new AlertAcknowledgedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.AlertAcknowledgedMessage,
                Source = source.IsEmpty ? "AlertService" : source,
                Priority = GetMessagePriority(acknowledgedAlert.Severity),
                CorrelationId = finalCorrelationId,
                AlertId = acknowledgedAlert.Id,
                AlertMessage = acknowledgedAlert.Message,
                AlertSeverity = acknowledgedAlert.Severity,
                AlertSource = acknowledgedAlert.Source,
                AlertTag = acknowledgedAlert.Tag,
                OperationId = acknowledgedAlert.OperationId,
                AcknowledgedBy = acknowledgedAlert.AcknowledgedBy,
                AcknowledgedTimestampTicks = acknowledgedAlert.AcknowledgedTimestampTicks ?? DateTime.UtcNow.Ticks,
                OriginalAlertTimestampTicks = acknowledgedAlert.TimestampTicks,
                AcknowledgmentDurationTicks = acknowledgmentDuration,
                AlertCount = acknowledgedAlert.Count
            };
        }

        /// <summary>
        /// Creates a new AlertAcknowledgedMessage with explicit alert details and proper validation.
        /// </summary>
        /// <param name="alertId">Alert unique identifier</param>
        /// <param name="message">Alert message</param>
        /// <param name="severity">Alert severity</param>
        /// <param name="alertSource">Source that raised the alert</param>
        /// <param name="acknowledgedBy">User or system acknowledging</param>
        /// <param name="originalAlertTimestamp">When the alert was originally created</param>
        /// <param name="tag">Alert categorization tag</param>
        /// <param name="operationId">Operation ID</param>
        /// <param name="count">Alert count</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New AlertAcknowledgedMessage instance</returns>
        public static AlertAcknowledgedMessage Create(
            Guid alertId,
            FixedString512Bytes message,
            AlertSeverity severity,
            FixedString64Bytes alertSource,
            FixedString64Bytes acknowledgedBy,
            DateTime originalAlertTimestamp,
            FixedString32Bytes tag = default,
            Guid operationId = default,
            int count = 1,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            // Input validation
            if (alertId == Guid.Empty)
                throw new ArgumentException("Alert ID cannot be empty", nameof(alertId));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "AlertService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("AlertAcknowledgedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("AlertAcknowledgment", alertId.ToString())
                : correlationId;

            var now = DateTime.UtcNow;
            var nowTicks = now.Ticks;
            var originalTicks = originalAlertTimestamp.Ticks;
            var durationTicks = nowTicks - originalTicks;

            return new AlertAcknowledgedMessage
            {
                Id = messageId,
                TimestampTicks = nowTicks,
                TypeCode = MessageTypeCodes.AlertAcknowledgedMessage,
                Source = source.IsEmpty ? "AlertService" : source,
                Priority = GetMessagePriority(severity),
                CorrelationId = finalCorrelationId,
                AlertId = alertId,
                AlertMessage = message,
                AlertSeverity = severity,
                AlertSource = alertSource,
                AlertTag = tag,
                OperationId = operationId,
                AcknowledgedBy = acknowledgedBy,
                AcknowledgedTimestampTicks = nowTicks,
                OriginalAlertTimestampTicks = originalTicks,
                AcknowledgmentDurationTicks = durationTicks,
                AlertCount = count
            };
        }

        /// <summary>
        /// Determines the message priority based on alert severity.
        /// Acknowledgment messages are typically lower priority than the original alert.
        /// </summary>
        /// <param name="severity">Alert severity level</param>
        /// <returns>Corresponding message priority</returns>
        private static MessagePriority GetMessagePriority(AlertSeverity severity)
        {
            return severity switch
            {
                AlertSeverity.Emergency => MessagePriority.Critical,   // Highest priority
                AlertSeverity.Critical => MessagePriority.High,        // Lower than original Critical
                AlertSeverity.High => MessagePriority.Normal,          // Lower than original High
                AlertSeverity.Warning => MessagePriority.Low,          // Lower than original Normal
                AlertSeverity.Medium => MessagePriority.Low,           // Lower priority
                AlertSeverity.Low => MessagePriority.VeryLow,          // Lowest priority
                AlertSeverity.Info => MessagePriority.VeryLow,
                AlertSeverity.Debug => MessagePriority.VeryLow,
                _ => MessagePriority.Low
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Alert acknowledged message string representation</returns>
        public override string ToString()
        {
            var sourceText = AlertSource.IsEmpty ? "Unknown" : AlertSource.ToString();
            var messageText = AlertMessage.IsEmpty ? "No message" : AlertMessage.ToString();
            var acknowledgedByText = AcknowledgedBy.IsEmpty ? "Unknown" : AcknowledgedBy.ToString();
            return $"AlertAcknowledged: [{AlertSeverity}] {sourceText} - {messageText} " +
                   $"(ID: {AlertId}, AckedBy: {acknowledgedByText}, Duration: {AcknowledgmentDuration:mm\\:ss})";
        }

        #endregion
    }
}