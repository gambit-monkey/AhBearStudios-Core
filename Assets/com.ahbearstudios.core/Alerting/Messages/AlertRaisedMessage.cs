using System;
using Unity.Collections;
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

        // Alert-specific properties
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

        /// <summary>
        /// Gets the DateTime representation of the alert timestamp.
        /// </summary>
        public DateTime AlertTimestamp => new DateTime(AlertTimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new AlertRaisedMessage from an Alert instance.
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
            if (alert == null)
                throw new ArgumentNullException(nameof(alert));

            return new AlertRaisedMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.AlertRaised,
                Source = source.IsEmpty ? "AlertService" : source,
                Priority = GetMessagePriority(alert.Severity),
                CorrelationId = correlationId == default ? alert.CorrelationId : correlationId,
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
        /// Creates a new AlertRaisedMessage with explicit alert details.
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
            FixedString512Bytes message,
            AlertSeverity severity,
            FixedString64Bytes alertSource,
            FixedString32Bytes tag = default,
            Guid operationId = default,
            bool hasContext = false,
            int count = 1,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            var now = DateTime.UtcNow.Ticks;
            return new AlertRaisedMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = now,
                TypeCode = MessageTypeCodes.AlertRaised,
                Source = source.IsEmpty ? "AlertService" : source,
                Priority = GetMessagePriority(severity),
                CorrelationId = correlationId == Guid.Empty ? Guid.NewGuid() : correlationId,
                AlertId = alertId,
                AlertMessage = message,
                AlertSeverity = severity,
                AlertSource = alertSource,
                AlertTag = tag,
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
                AlertSeverity.Critical => MessagePriority.Critical,
                AlertSeverity.Error => MessagePriority.High,
                AlertSeverity.Warning => MessagePriority.Normal,
                AlertSeverity.Info => MessagePriority.Low,
                AlertSeverity.Debug => MessagePriority.VeryLow,
                _ => MessagePriority.Normal
            };
        }

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Alert raised message string representation</returns>
        public override string ToString()
        {
            return $"AlertRaised: [{AlertSeverity}] {AlertSource} - {AlertMessage} (ID: {AlertId}, Count: {AlertCount})";
        }
    }
}