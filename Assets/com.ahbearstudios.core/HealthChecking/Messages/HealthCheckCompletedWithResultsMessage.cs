using System;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Message published when a health check is completed with detailed results.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// Provides comprehensive health check completion information with status details.
    /// </summary>
    public readonly record struct HealthCheckCompletedWithResultsMessage : IMessage
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

        #region Health Check Properties

        /// <summary>
        /// Gets the name of the completed health check.
        /// </summary>
        public FixedString64Bytes HealthCheckName { get; init; }

        /// <summary>
        /// Gets the type of health check that was executed.
        /// </summary>
        public FixedString128Bytes HealthCheckType { get; init; }

        /// <summary>
        /// Gets the health status result.
        /// </summary>
        public HealthStatus Status { get; init; }

        /// <summary>
        /// Gets the health check result message.
        /// </summary>
        public FixedString512Bytes Message { get; init; }

        /// <summary>
        /// Gets the duration of the health check in milliseconds.
        /// </summary>
        public double DurationMs { get; init; }

        /// <summary>
        /// Gets whether the health check has issues.
        /// </summary>
        public bool HasIssues { get; init; }

        /// <summary>
        /// Gets whether the health check has warnings.
        /// </summary>
        public bool HasWarnings { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the TimeSpan representation of the health check duration.
        /// </summary>
        public TimeSpan Duration => TimeSpan.FromMilliseconds(DurationMs);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new HealthCheckCompletedWithResultsMessage with comprehensive result details.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="healthCheckType">Type of health check executed</param>
        /// <param name="status">Health status result</param>
        /// <param name="message">Health check result message</param>
        /// <param name="durationMs">Duration of the health check in milliseconds</param>
        /// <param name="hasIssues">Whether the health check detected issues</param>
        /// <param name="hasWarnings">Whether the health check detected warnings</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <param name="priority">Message priority level</param>
        /// <returns>New HealthCheckCompletedWithResultsMessage instance</returns>
        public static HealthCheckCompletedWithResultsMessage Create(
            string healthCheckName,
            string healthCheckType,
            HealthStatus status,
            string message,
            double durationMs,
            bool hasIssues = false,
            bool hasWarnings = false,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            MessagePriority priority = MessagePriority.Normal)
        {
            // Input validation
            if (string.IsNullOrEmpty(healthCheckName))
                throw new ArgumentException("Health check name cannot be null or empty", nameof(healthCheckName));

            if (string.IsNullOrEmpty(healthCheckType))
                throw new ArgumentException("Health check type cannot be null or empty", nameof(healthCheckType));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "HealthCheckSystem" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckCompletedWithResultsMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("HealthCheckCompletion", healthCheckName)
                : correlationId;

            // Determine priority based on status if not explicitly set
            var finalPriority = priority == MessagePriority.Normal && status == HealthStatus.Healthy 
                ? MessagePriority.Low 
                : priority;

            return new HealthCheckCompletedWithResultsMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckCompletedWithResultsMessage,
                Source = source.IsEmpty ? "HealthCheckSystem" : source,
                Priority = finalPriority,
                CorrelationId = finalCorrelationId,
                HealthCheckName = healthCheckName?.Length <= 64 ? healthCheckName : healthCheckName?[..64] ?? "Unknown",
                HealthCheckType = healthCheckType?.Length <= 128 ? healthCheckType : healthCheckType?[..128] ?? "Unknown",
                Status = status,
                Message = message?.Length <= 512 ? message : message?[..512] ?? string.Empty,
                DurationMs = durationMs,
                HasIssues = hasIssues,
                HasWarnings = hasWarnings
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Health check completed with results message string representation</returns>
        public override string ToString()
        {
            var issueWarningText = (HasIssues, HasWarnings) switch
            {
                (true, true) => " (Issues & Warnings)",
                (true, false) => " (Issues)",
                (false, true) => " (Warnings)",
                (false, false) => ""
            };

            return $"HealthCheckCompletedWithResults: {HealthCheckName} ({HealthCheckType}) - {Status} in {DurationMs:F1}ms{issueWarningText}";
        }

        #endregion
    }
}