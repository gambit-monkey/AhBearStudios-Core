using System;
using System.Collections.Generic;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Message sent when health check alerts are raised or resolved.
    /// Integrates health monitoring with the alerting system for comprehensive system monitoring.
    /// </summary>
    public readonly record struct HealthCheckAlertMessage : IMessage
    {
        /// <summary>
        /// Gets the unique identifier for this message.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets the timestamp when this message was created (in UTC ticks).
        /// </summary>
        public long TimestampTicks { get; init; }

        /// <summary>
        /// Gets the message type code for health check alert messages.
        /// </summary>
        public ushort TypeCode { get; init; }

        /// <summary>
        /// Gets the source component that generated this message.
        /// </summary>
        public FixedString64Bytes Source { get; init; }

        /// <summary>
        /// Gets the message priority level.
        /// </summary>
        public MessagePriority Priority { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking related operations.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets the name of the health check that triggered the alert.
        /// </summary>
        public FixedString64Bytes HealthCheckName { get; init; }

        /// <summary>
        /// Gets the current health status that triggered the alert.
        /// </summary>
        public HealthStatus HealthStatus { get; init; }

        /// <summary>
        /// Gets the previous health status before the change.
        /// </summary>
        public HealthStatus PreviousHealthStatus { get; init; }

        /// <summary>
        /// Gets the alert severity level.
        /// </summary>
        public AlertSeverity AlertSeverity { get; init; }

        /// <summary>
        /// Gets the alert message describing the health condition.
        /// </summary>
        public FixedString512Bytes AlertMessage { get; init; }

        /// <summary>
        /// Gets the health check category.
        /// </summary>
        public HealthCheckCategory Category { get; init; }

        /// <summary>
        /// Gets whether this alert represents a resolved condition.
        /// </summary>
        public bool IsResolved { get; init; }

        /// <summary>
        /// Gets the duration of the health check execution that triggered the alert.
        /// </summary>
        public TimeSpan ExecutionDuration { get; init; }

        /// <summary>
        /// Gets the number of consecutive failures (if applicable).
        /// </summary>
        public int ConsecutiveFailures { get; init; }

        /// <summary>
        /// Gets additional context information for the alert.
        /// </summary>
        public FixedString512Bytes Context { get; init; }

        /// <summary>
        /// Creates a new health check alert message for a health status change.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="currentStatus">Current health status</param>
        /// <param name="previousStatus">Previous health status</param>
        /// <param name="alertSeverity">Alert severity level</param>
        /// <param name="alertMessage">Alert message</param>
        /// <param name="category">Health check category</param>
        /// <param name="executionDuration">Duration of health check execution</param>
        /// <param name="consecutiveFailures">Number of consecutive failures</param>
        /// <param name="context">Additional context information</param>
        /// <param name="source">Source component generating the message</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="priority">Message priority</param>
        /// <returns>A new health check alert message</returns>
        public static HealthCheckAlertMessage Create(
            FixedString64Bytes healthCheckName,
            HealthStatus currentStatus,
            HealthStatus previousStatus,
            AlertSeverity alertSeverity,
            string alertMessage,
            HealthCheckCategory category,
            TimeSpan executionDuration,
            int consecutiveFailures = 0,
            string context = "",
            string source = "HealthCheckService",
            Guid? correlationId = null,
            MessagePriority priority = MessagePriority.Normal)
        {
            var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckAlertMessage", source, correlationId: null);
            var finalCorrelationId = correlationId ?? DeterministicIdGenerator.GenerateCorrelationId("HealthCheckAlert", healthCheckName.ToString());

            return new HealthCheckAlertMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckAlertMessage,
                Source = source,
                Priority = priority,
                CorrelationId = finalCorrelationId,
                HealthCheckName = healthCheckName,
                HealthStatus = currentStatus,
                PreviousHealthStatus = previousStatus,
                AlertSeverity = alertSeverity,
                AlertMessage = alertMessage,
                Category = category,
                IsResolved = currentStatus == HealthStatus.Healthy && previousStatus != HealthStatus.Healthy,
                ExecutionDuration = executionDuration,
                ConsecutiveFailures = consecutiveFailures,
                Context = context ?? ""
            };
        }

        /// <summary>
        /// Creates a new health check alert message for a resolved condition.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="previousStatus">Previous unhealthy status</param>
        /// <param name="category">Health check category</param>
        /// <param name="executionDuration">Duration of health check execution</param>
        /// <param name="context">Additional context information</param>
        /// <param name="source">Source component generating the message</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>A new resolved health check alert message</returns>
        public static HealthCheckAlertMessage CreateResolved(
            FixedString64Bytes healthCheckName,
            HealthStatus previousStatus,
            HealthCheckCategory category,
            TimeSpan executionDuration,
            string context = "",
            string source = "HealthCheckService",
            Guid? correlationId = null)
        {
            return Create(
                healthCheckName: healthCheckName,
                currentStatus: HealthStatus.Healthy,
                previousStatus: previousStatus,
                alertSeverity: AlertSeverity.Info,
                alertMessage: $"Health check '{healthCheckName}' recovered from {previousStatus} status",
                category: category,
                executionDuration: executionDuration,
                consecutiveFailures: 0,
                context: context,
                source: source,
                correlationId: correlationId,
                priority: MessagePriority.Normal);
        }

        /// <summary>
        /// Creates a new health check alert message for a critical failure.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="previousStatus">Previous health status</param>
        /// <param name="category">Health check category</param>
        /// <param name="executionDuration">Duration of health check execution</param>
        /// <param name="consecutiveFailures">Number of consecutive failures</param>
        /// <param name="errorMessage">Error message describing the failure</param>
        /// <param name="context">Additional context information</param>
        /// <param name="source">Source component generating the message</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>A new critical health check alert message</returns>
        public static HealthCheckAlertMessage CreateCritical(
            FixedString64Bytes healthCheckName,
            HealthStatus previousStatus,
            HealthCheckCategory category,
            TimeSpan executionDuration,
            int consecutiveFailures,
            string errorMessage,
            string context = "",
            string source = "HealthCheckService",
            Guid? correlationId = null)
        {
            return Create(
                healthCheckName: healthCheckName,
                currentStatus: HealthStatus.Unhealthy,
                previousStatus: previousStatus,
                alertSeverity: AlertSeverity.Critical,
                alertMessage: $"Critical failure in health check '{healthCheckName}': {errorMessage}",
                category: category,
                executionDuration: executionDuration,
                consecutiveFailures: consecutiveFailures,
                context: context,
                source: source,
                correlationId: correlationId,
                priority: MessagePriority.High);
        }

        /// <summary>
        /// Returns a string representation of this health check alert message.
        /// </summary>
        /// <returns>A formatted string containing key message information</returns>
        public override string ToString()
        {
            var timestamp = new DateTime(TimestampTicks, DateTimeKind.Utc);
            var resolvedText = IsResolved ? " [RESOLVED]" : "";
            var failureText = ConsecutiveFailures > 0 ? $" (Failures: {ConsecutiveFailures})" : "";
            
            return $"HealthCheckAlert[{timestamp:HH:mm:ss.fff}] {HealthCheckName}: {PreviousHealthStatus} → {HealthStatus} " +
                   $"({AlertSeverity}){resolvedText}{failureText} | {AlertMessage} | Duration: {ExecutionDuration.TotalMilliseconds:F1}ms";
        }
    }
}