using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Messages
{
    /// <summary>
    /// Message published when an alert fails to be delivered through a channel.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct AlertDeliveryFailedMessage : IMessage
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

        // Delivery failure-specific properties
        /// <summary>
        /// Gets the name of the channel where delivery failed.
        /// </summary>
        public FixedString64Bytes ChannelName { get; init; }

        /// <summary>
        /// Gets the unique identifier of the alert that failed to be delivered.
        /// </summary>
        public Guid AlertId { get; init; }

        /// <summary>
        /// Gets the alert message that failed.
        /// </summary>
        public FixedString512Bytes AlertMessage { get; init; }

        /// <summary>
        /// Gets the severity of the failed alert.
        /// </summary>
        public AlertSeverity AlertSeverity { get; init; }

        /// <summary>
        /// Gets the source of the failed alert.
        /// </summary>
        public FixedString64Bytes AlertSource { get; init; }

        /// <summary>
        /// Gets the exception message that caused the failure.
        /// </summary>
        public FixedString512Bytes ExceptionMessage { get; init; }

        /// <summary>
        /// Gets the exception type name.
        /// </summary>
        public FixedString128Bytes ExceptionType { get; init; }

        /// <summary>
        /// Gets the number of retry attempts made.
        /// </summary>
        public int RetryCount { get; init; }

        /// <summary>
        /// Gets whether this was a final failure (no more retries).
        /// </summary>
        public bool IsFinalFailure { get; init; }

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new AlertDeliveryFailedMessage.
        /// </summary>
        /// <param name="channelName">Name of the failed channel</param>
        /// <param name="alert">The alert that failed to be delivered</param>
        /// <param name="exception">Exception that caused the failure</param>
        /// <param name="retryCount">Number of retry attempts</param>
        /// <param name="isFinalFailure">Whether this is the final failure</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New AlertDeliveryFailedMessage instance</returns>
        public static AlertDeliveryFailedMessage Create(
            FixedString64Bytes channelName,
            Alert alert,
            Exception exception,
            int retryCount,
            bool isFinalFailure,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new AlertDeliveryFailedMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.AlertDeliveryFailed,
                Source = source.IsEmpty ? "AlertChannelService" : source,
                Priority = isFinalFailure ? MessagePriority.High : MessagePriority.Normal,
                CorrelationId = correlationId == Guid.Empty ? alert?.CorrelationId ?? Guid.NewGuid() : correlationId,
                ChannelName = channelName,
                AlertId = alert?.Id ?? Guid.Empty,
                AlertMessage = alert?.Message ?? "Unknown alert",
                AlertSeverity = alert?.Severity ?? AlertSeverity.Info,
                AlertSource = alert?.Source ?? "Unknown",
                ExceptionMessage = exception?.Message?.Length <= 256 ? exception?.Message : exception?.Message?[..256] ?? "Unknown error",
                ExceptionType = exception?.GetType().Name?.Length <= 128 ? exception?.GetType().Name : exception?.GetType().Name?[..128] ?? "Unknown",
                RetryCount = retryCount,
                IsFinalFailure = isFinalFailure
            };
        }

        /// <summary>
        /// Creates a new AlertDeliveryFailedMessage with explicit details.
        /// </summary>
        /// <param name="channelName">Name of the failed channel</param>
        /// <param name="alertId">Alert ID</param>
        /// <param name="alertMessage">Alert message</param>
        /// <param name="alertSeverity">Alert severity</param>
        /// <param name="alertSource">Alert source</param>
        /// <param name="errorMessage">Error message</param>
        /// <param name="retryCount">Number of retry attempts</param>
        /// <param name="isFinalFailure">Whether this is the final failure</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New AlertDeliveryFailedMessage instance</returns>
        public static AlertDeliveryFailedMessage Create(
            FixedString64Bytes channelName,
            Guid alertId,
            FixedString512Bytes alertMessage,
            AlertSeverity alertSeverity,
            FixedString64Bytes alertSource,
            string errorMessage,
            int retryCount,
            bool isFinalFailure,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new AlertDeliveryFailedMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.AlertDeliveryFailed,
                Source = source.IsEmpty ? "AlertChannelService" : source,
                Priority = isFinalFailure ? MessagePriority.High : MessagePriority.Normal,
                CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
                ChannelName = channelName,
                AlertId = alertId,
                AlertMessage = alertMessage,
                AlertSeverity = alertSeverity,
                AlertSource = alertSource,
                ExceptionMessage = errorMessage?.Length <= 256 ? errorMessage : errorMessage?[..256] ?? "Unknown error",
                ExceptionType = "Unknown",
                RetryCount = retryCount,
                IsFinalFailure = isFinalFailure
            };
        }

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Delivery failure message string representation</returns>
        public override string ToString()
        {
            var finalText = IsFinalFailure ? " (final failure)" : $" (attempt {RetryCount})";
            return $"AlertDeliveryFailed: {ChannelName} failed to deliver alert {AlertId}{finalText} - {ExceptionMessage}";
        }
    }
}