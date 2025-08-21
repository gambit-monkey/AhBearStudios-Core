using System;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Message published when a health check is completed.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct HealthCheckCompletedMessage : IMessage
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
        public ushort TypeCode { get; init; } = MessageTypeCodes.HealthCheckCompletedMessage;

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

        // Health check-specific properties
        /// <summary>
        /// Gets the name of the completed health check.
        /// </summary>
        public FixedString64Bytes HealthCheckName { get; init; }

        /// <summary>
        /// Gets the health status result.
        /// </summary>
        public HealthStatus Status { get; init; }

        /// <summary>
        /// Gets the health check message.
        /// </summary>
        public FixedString512Bytes Message { get; init; }

        /// <summary>
        /// Gets the duration of the health check in ticks.
        /// </summary>
        public long DurationTicks { get; init; }

        /// <summary>
        /// Initializes a new instance of the HealthCheckCompletedMessage struct.
        /// </summary>
        public HealthCheckCompletedMessage()
        {
            Id = default;
            TimestampTicks = default;
            Source = default;
            Priority = default;
            CorrelationId = default;
            HealthCheckName = default;
            Status = default;
            Message = default;
            DurationTicks = default;
        }

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the TimeSpan representation of the health check duration.
        /// </summary>
        public TimeSpan Duration => new TimeSpan(DurationTicks);

        /// <summary>
        /// Creates a new HealthCheckCompletedMessage.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="status">Health status result</param>
        /// <param name="message">Health check message</param>
        /// <param name="duration">Duration of the health check</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New HealthCheckCompletedMessage instance</returns>
        public static HealthCheckCompletedMessage Create(
            string healthCheckName,
            HealthStatus status,
            string message,
            TimeSpan duration,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new HealthCheckCompletedMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckCompletedMessage,
                Source = source.IsEmpty ? "HealthCheckService" : source,
                Priority = status == HealthStatus.Healthy ? MessagePriority.Low : MessagePriority.Normal,
                CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
                HealthCheckName = healthCheckName?.Length <= 64 ? healthCheckName : healthCheckName?[..64] ?? "Unknown",
                Status = status,
                Message = message?.Length <= 512 ? message : message?[..512] ?? string.Empty,
                DurationTicks = duration.Ticks
            };
        }

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Health check completed message string representation</returns>
        public override string ToString()
        {
            return $"HealthCheckCompleted: {HealthCheckName} - {Status} in {Duration.TotalMilliseconds:F1}ms";
        }
    }
}