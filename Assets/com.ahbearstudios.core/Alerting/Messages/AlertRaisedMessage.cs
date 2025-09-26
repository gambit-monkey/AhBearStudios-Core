using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Messages
{
    /// <summary>
    /// Message published when an alert is raised in the system.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct AlertRaisedMessage : IMessage
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
        /// Gets the unique identifier of the raised alert.
        /// </summary>
        public Guid AlertId { get; init; }

        /// <summary>
        /// Gets the alert message content.
        /// </summary>
        public FixedString512Bytes AlertMessage { get; init; }

        /// <summary>
        /// Gets the severity level of the raised alert.
        /// </summary>
        public AlertSeverity AlertSeverity { get; init; }

        /// <summary>
        /// Gets the source system that raised the alert.
        /// </summary>
        public FixedString64Bytes AlertSource { get; init; }

        /// <summary>
        /// Gets the categorization tag for the alert.
        /// </summary>
        public FixedString32Bytes AlertTag { get; init; }

        /// <summary>
        /// Gets the operation ID associated with the alert.
        /// </summary>
        public Guid OperationId { get; init; }

        /// <summary>
        /// Gets the timestamp when the alert was created, in UTC ticks.
        /// </summary>
        public long AlertTimestampTicks { get; init; }

        /// <summary>
        /// Gets whether the alert has contextual information attached.
        /// </summary>
        public bool HasContext { get; init; }

        /// <summary>
        /// Gets the count for duplicate alert suppression.
        /// </summary>
        public int AlertCount { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the DateTime representation of the alert timestamp.
        /// </summary>
        public DateTime AlertTimestamp => new DateTime(AlertTimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new AlertRaisedMessage from an Alert instance with proper validation and defaults.
        /// </summary>
        /// <param name="alert">The alert that was raised</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New AlertRaisedMessage instance</returns>
        public static AlertRaisedMessage Create(
            Alert alert,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            // Input validation
            if (alert == null)
                throw new ArgumentNullException(nameof(alert));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "AlertService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("AlertRaisedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? (alert.CorrelationId != default ? alert.CorrelationId : DeterministicIdGenerator.GenerateCorrelationId("AlertRaised", alert.Id.ToString()))
                : correlationId;

            return new AlertRaisedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.AlertRaisedMessage,
                Source = source.IsEmpty ? "AlertService" : source,
                Priority = GetMessagePriority(alert.Severity),
                CorrelationId = finalCorrelationId,
                AlertId = alert.Id,
                AlertMessage = alert.Message,
                AlertSeverity = alert.Severity,
                AlertSource = alert.Source,
                AlertTag = alert.Tag,
                OperationId = alert.OperationId,
                AlertTimestampTicks = alert.TimestampTicks,
                HasContext = alert.Context != null,
                AlertCount = alert.Count
            };
        }

        /// <summary>
        /// Creates a new AlertRaisedMessage with explicit alert details and proper validation.
        /// </summary>
        /// <param name="alertId">Alert unique identifier</param>
        /// <param name="message">Alert message</param>
        /// <param name="severity">Alert severity</param>
        /// <param name="alertSource">Source that raised the alert</param>
        /// <param name="tag">Alert categorization tag</param>
        /// <param name="operationId">Operation ID</param>
        /// <param name="hasContext">Whether alert has context</param>
        /// <param name="count">Alert count for suppression</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New AlertRaisedMessage instance</returns>
        public static AlertRaisedMessage Create(
            Guid alertId,
            string message,
            AlertSeverity severity,
            string alertSource,
            string tag = null,
            Guid operationId = default,
            bool hasContext = false,
            int count = 1,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            // Input validation
            if (alertId == Guid.Empty)
                throw new ArgumentException("Alert ID cannot be empty", nameof(alertId));
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message cannot be null or empty", nameof(message));
            if (string.IsNullOrEmpty(alertSource))
                throw new ArgumentException("Alert source cannot be null or empty", nameof(alertSource));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "AlertService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("AlertRaisedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("AlertRaised", alertId.ToString())
                : correlationId;

            var now = DateTime.UtcNow.Ticks;
            return new AlertRaisedMessage
            {
                Id = messageId,
                TimestampTicks = now,
                TypeCode = MessageTypeCodes.AlertRaisedMessage,
                Source = source.IsEmpty ? "AlertService" : source,
                Priority = GetMessagePriority(severity),
                CorrelationId = finalCorrelationId,
                AlertId = alertId,
                AlertMessage = message?.Length <= 512 ? message : message?[..512] ?? string.Empty,
                AlertSeverity = severity,
                AlertSource = alertSource?.Length <= 64 ? alertSource : alertSource?[..64] ?? string.Empty,
                AlertTag = tag?.Length <= 32 ? tag : tag?[..32] ?? string.Empty,
                OperationId = operationId,
                AlertTimestampTicks = now,
                HasContext = hasContext,
                AlertCount = count
            };
        }

        /// <summary>
        /// Determines the message priority based on alert severity.
        /// </summary>
        /// <param name="severity">Alert severity level</param>
        /// <returns>Corresponding message priority</returns>
        private static MessagePriority GetMessagePriority(AlertSeverity severity)
        {
            return severity switch
            {
                AlertSeverity.Emergency => MessagePriority.Critical,   // Highest priority
                AlertSeverity.Critical => MessagePriority.Critical,    // Critical severity gets critical priority
                AlertSeverity.Error => MessagePriority.High,           // Error conditions need high attention
                AlertSeverity.High => MessagePriority.High,            // High severity gets high priority
                AlertSeverity.Warning => MessagePriority.Normal,       // Warning level is normal priority
                AlertSeverity.Medium => MessagePriority.Normal,        // Medium severity gets normal priority
                AlertSeverity.Low => MessagePriority.Low,              // Low severity gets low priority
                AlertSeverity.Info => MessagePriority.Low,             // Informational is low priority
                AlertSeverity.Debug => MessagePriority.VeryLow,        // Debug is lowest priority
                _ => MessagePriority.Normal
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Alert raised message string representation</returns>
        public override string ToString()
        {
            var sourceText = AlertSource.IsEmpty ? "Unknown" : AlertSource.ToString();
            var messageText = AlertMessage.IsEmpty ? "No message" : AlertMessage.ToString();
            return $"AlertRaised: [{AlertSeverity}] {sourceText} - {messageText} (ID: {AlertId}, Count: {AlertCount})";
        }

        #endregion
    }
}