using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Messages
{
    /// <summary>
    /// Message published when an alert is resolved in the system.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct AlertResolvedMessage : IMessage
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
        /// Gets the unique identifier of the resolved alert.
        /// </summary>
        public Guid AlertId { get; init; }

        /// <summary>
        /// Gets the alert message content for context.
        /// </summary>
        public FixedString512Bytes AlertMessage { get; init; }

        /// <summary>
        /// Gets the severity level of the resolved alert.
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
        /// Gets the user or system that resolved the alert.
        /// </summary>
        public FixedString64Bytes ResolvedBy { get; init; }

        /// <summary>
        /// Gets the user or system that acknowledged the alert (if different from resolver).
        /// </summary>
        public FixedString64Bytes AcknowledgedBy { get; init; }

        /// <summary>
        /// Gets the timestamp when the alert was resolved, in UTC ticks.
        /// </summary>
        public long ResolvedTimestampTicks { get; init; }

        /// <summary>
        /// Gets the timestamp when the alert was acknowledged, in UTC ticks (optional).
        /// </summary>
        public long? AcknowledgedTimestampTicks { get; init; }

        /// <summary>
        /// Gets the timestamp when the original alert was created, in UTC ticks.
        /// </summary>
        public long OriginalAlertTimestampTicks { get; init; }

        /// <summary>
        /// Gets the total duration between alert creation and resolution in ticks.
        /// </summary>
        public long ResolutionDurationTicks { get; init; }

        /// <summary>
        /// Gets the duration between acknowledgment and resolution in ticks (if acknowledged).
        /// </summary>
        public long? AcknowledgmentToResolutionDurationTicks { get; init; }

        /// <summary>
        /// Gets the final count for the alert at time of resolution.
        /// </summary>
        public int AlertCount { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets whether the alert was acknowledged before being resolved.
        /// </summary>
        public bool WasAcknowledged => AcknowledgedTimestampTicks.HasValue;

        /// <summary>
        /// Gets the DateTime representation of the resolution timestamp.
        /// </summary>
        public DateTime ResolvedTimestamp => new DateTime(ResolvedTimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the DateTime representation of the acknowledgment timestamp (if acknowledged).
        /// </summary>
        public DateTime? AcknowledgedTimestamp => AcknowledgedTimestampTicks.HasValue
            ? new DateTime(AcknowledgedTimestampTicks.Value, DateTimeKind.Utc)
            : null;

        /// <summary>
        /// Gets the DateTime representation of the original alert timestamp.
        /// </summary>
        public DateTime OriginalAlertTimestamp => new DateTime(OriginalAlertTimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the total resolution duration as TimeSpan.
        /// </summary>
        public TimeSpan ResolutionDuration => new TimeSpan(ResolutionDurationTicks);

        /// <summary>
        /// Gets the acknowledgment-to-resolution duration as TimeSpan (if acknowledged).
        /// </summary>
        public TimeSpan? AcknowledgmentToResolutionDuration => AcknowledgmentToResolutionDurationTicks.HasValue
            ? new TimeSpan(AcknowledgmentToResolutionDurationTicks.Value)
            : null;

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new AlertResolvedMessage from a resolved Alert instance with proper validation and defaults.
        /// </summary>
        /// <param name="resolvedAlert">The resolved alert</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New AlertResolvedMessage instance</returns>
        public static AlertResolvedMessage Create(
            Alert resolvedAlert,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            // Input validation
            if (resolvedAlert == null)
                throw new ArgumentNullException(nameof(resolvedAlert));

            if (!resolvedAlert.IsResolved)
                throw new ArgumentException("Alert must be resolved", nameof(resolvedAlert));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "AlertService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("AlertResolvedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? resolvedAlert.CorrelationId 
                : correlationId;

            var resolutionDuration = resolvedAlert.ResolvedTimestampTicks.HasValue
                ? resolvedAlert.ResolvedTimestampTicks.Value - resolvedAlert.TimestampTicks
                : 0;

            var ackToResolutionDuration = resolvedAlert.AcknowledgedTimestampTicks.HasValue && 
                                        resolvedAlert.ResolvedTimestampTicks.HasValue
                ? resolvedAlert.ResolvedTimestampTicks.Value - resolvedAlert.AcknowledgedTimestampTicks.Value
                : (long?)null;

            return new AlertResolvedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.AlertResolvedMessage,
                Source = source.IsEmpty ? "AlertService" : source,
                Priority = GetMessagePriority(resolvedAlert.Severity),
                CorrelationId = finalCorrelationId,
                AlertId = resolvedAlert.Id,
                AlertMessage = resolvedAlert.Message,
                AlertSeverity = resolvedAlert.Severity,
                AlertSource = resolvedAlert.Source,
                AlertTag = resolvedAlert.Tag,
                OperationId = resolvedAlert.OperationId,
                ResolvedBy = resolvedAlert.ResolvedBy,
                AcknowledgedBy = resolvedAlert.AcknowledgedBy,
                ResolvedTimestampTicks = resolvedAlert.ResolvedTimestampTicks ?? DateTime.UtcNow.Ticks,
                AcknowledgedTimestampTicks = resolvedAlert.AcknowledgedTimestampTicks,
                OriginalAlertTimestampTicks = resolvedAlert.TimestampTicks,
                ResolutionDurationTicks = resolutionDuration,
                AcknowledgmentToResolutionDurationTicks = ackToResolutionDuration,
                AlertCount = resolvedAlert.Count
            };
        }

        /// <summary>
        /// Creates a new AlertResolvedMessage with explicit alert details and proper validation.
        /// </summary>
        /// <param name="alertId">Alert unique identifier</param>
        /// <param name="message">Alert message</param>
        /// <param name="severity">Alert severity</param>
        /// <param name="alertSource">Source that raised the alert</param>
        /// <param name="resolvedBy">User or system resolving</param>
        /// <param name="originalAlertTimestamp">When the alert was originally created</param>
        /// <param name="acknowledgedBy">User or system that acknowledged (optional)</param>
        /// <param name="acknowledgedTimestamp">When the alert was acknowledged (optional)</param>
        /// <param name="tag">Alert categorization tag</param>
        /// <param name="operationId">Operation ID</param>
        /// <param name="count">Final alert count</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New AlertResolvedMessage instance</returns>
        public static AlertResolvedMessage Create(
            Guid alertId,
            FixedString512Bytes message,
            AlertSeverity severity,
            FixedString64Bytes alertSource,
            FixedString64Bytes resolvedBy,
            DateTime originalAlertTimestamp,
            FixedString64Bytes acknowledgedBy = default,
            DateTime? acknowledgedTimestamp = null,
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
            var messageId = DeterministicIdGenerator.GenerateMessageId("AlertResolvedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("AlertResolution", alertId.ToString())
                : correlationId;

            var now = DateTime.UtcNow;
            var nowTicks = now.Ticks;
            var originalTicks = originalAlertTimestamp.Ticks;
            var acknowledgedTicks = acknowledgedTimestamp?.Ticks;
            var resolutionDurationTicks = nowTicks - originalTicks;
            var ackToResolutionDurationTicks = acknowledgedTicks.HasValue
                ? nowTicks - acknowledgedTicks.Value
                : (long?)null;

            return new AlertResolvedMessage
            {
                Id = messageId,
                TimestampTicks = nowTicks,
                TypeCode = MessageTypeCodes.AlertResolvedMessage,
                Source = source.IsEmpty ? "AlertService" : source,
                Priority = GetMessagePriority(severity),
                CorrelationId = finalCorrelationId,
                AlertId = alertId,
                AlertMessage = message,
                AlertSeverity = severity,
                AlertSource = alertSource,
                AlertTag = tag,
                OperationId = operationId,
                ResolvedBy = resolvedBy,
                AcknowledgedBy = acknowledgedBy,
                ResolvedTimestampTicks = nowTicks,
                AcknowledgedTimestampTicks = acknowledgedTicks,
                OriginalAlertTimestampTicks = originalTicks,
                ResolutionDurationTicks = resolutionDurationTicks,
                AcknowledgmentToResolutionDurationTicks = ackToResolutionDurationTicks,
                AlertCount = count
            };
        }

        /// <summary>
        /// Determines the message priority based on alert severity.
        /// Resolution messages are typically lower priority than the original alert.
        /// </summary>
        /// <param name="severity">Alert severity level</param>
        /// <returns>Corresponding message priority</returns>
        private static MessagePriority GetMessagePriority(AlertSeverity severity)
        {
            return severity switch
            {
                AlertSeverity.Emergency => MessagePriority.Normal,     // Much lower than original Critical
                AlertSeverity.Critical => MessagePriority.Low,         // Lower than original High
                AlertSeverity.Error => MessagePriority.Low,            // Error resolutions get low priority
                AlertSeverity.High => MessagePriority.Low,             // Lower than original High
                AlertSeverity.Warning => MessagePriority.VeryLow,      // Lower than original Normal
                AlertSeverity.Medium => MessagePriority.VeryLow,       // Lower priority
                AlertSeverity.Low => MessagePriority.VeryLow,          // Lowest priority
                AlertSeverity.Info => MessagePriority.VeryLow,         // Informational resolution
                AlertSeverity.Debug => MessagePriority.VeryLow,        // Debug resolution
                _ => MessagePriority.VeryLow
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Alert resolved message string representation</returns>
        public override string ToString()
        {
            var sourceText = AlertSource.IsEmpty ? "Unknown" : AlertSource.ToString();
            var messageText = AlertMessage.IsEmpty ? "No message" : AlertMessage.ToString();
            var resolvedByText = ResolvedBy.IsEmpty ? "Unknown" : ResolvedBy.ToString();
            var acknowledgedByText = AcknowledgedBy.IsEmpty ? "Unknown" : AcknowledgedBy.ToString();
            var ackStatus = WasAcknowledged ? $" (Acked by {acknowledgedByText})" : " (Direct resolution)";
            return $"AlertResolved: [{AlertSeverity}] {sourceText} - {messageText} " +
                   $"(ID: {AlertId}, ResolvedBy: {resolvedByText}, Duration: {ResolutionDuration:mm\\:ss}{ackStatus})";
        }

        #endregion
    }
}