using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Message published when the health check service configuration is modified.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct HealthCheckServiceConfigurationChangedMessage : IMessage
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
        /// Gets the type of service configuration change.
        /// </summary>
        public FixedString64Bytes ChangeType { get; init; }

        /// <summary>
        /// Gets the description of what changed in the service configuration.
        /// </summary>
        public FixedString512Bytes ChangeDescription { get; init; }

        /// <summary>
        /// Gets whether automatic health checks are enabled.
        /// </summary>
        public bool AutomaticChecksEnabled { get; init; }

        /// <summary>
        /// Gets the new automatic check interval (if changed).
        /// </summary>
        public TimeSpan? NewAutomaticCheckInterval { get; init; }

        /// <summary>
        /// Gets the maximum concurrent health checks allowed.
        /// </summary>
        public int MaxConcurrentHealthChecks { get; init; }

        /// <summary>
        /// Gets whether circuit breaker functionality is enabled.
        /// </summary>
        public bool CircuitBreakerEnabled { get; init; }

        /// <summary>
        /// Gets whether graceful degradation is enabled.
        /// </summary>
        public bool GracefulDegradationEnabled { get; init; }

        /// <summary>
        /// Gets the user or system that made the change.
        /// </summary>
        public FixedString64Bytes ChangedBy { get; init; }

        /// <summary>
        /// Gets the previous configuration version.
        /// </summary>
        public FixedString64Bytes PreviousVersion { get; init; }

        /// <summary>
        /// Gets the new configuration version.
        /// </summary>
        public FixedString64Bytes NewVersion { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new HealthCheckServiceConfigurationChangedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="changeType">Type of service configuration change</param>
        /// <param name="changeDescription">Description of what changed</param>
        /// <param name="automaticChecksEnabled">Whether automatic health checks are enabled</param>
        /// <param name="maxConcurrentHealthChecks">Maximum concurrent health checks allowed</param>
        /// <param name="circuitBreakerEnabled">Whether circuit breaker functionality is enabled</param>
        /// <param name="gracefulDegradationEnabled">Whether graceful degradation is enabled</param>
        /// <param name="newAutomaticCheckInterval">New automatic check interval (if changed)</param>
        /// <param name="changedBy">User or system that made the change</param>
        /// <param name="previousVersion">Previous configuration version</param>
        /// <param name="newVersion">New configuration version</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <param name="priority">Message priority level</param>
        /// <returns>New HealthCheckServiceConfigurationChangedMessage instance</returns>
        public static HealthCheckServiceConfigurationChangedMessage Create(
            string changeType,
            string changeDescription,
            bool automaticChecksEnabled,
            int maxConcurrentHealthChecks,
            bool circuitBreakerEnabled,
            bool gracefulDegradationEnabled,
            TimeSpan? newAutomaticCheckInterval = null,
            string changedBy = "System",
            string previousVersion = "",
            string newVersion = "",
            FixedString64Bytes source = default,
            Guid correlationId = default,
            MessagePriority priority = MessagePriority.Normal)
        {
            // Input validation
            if (string.IsNullOrEmpty(changeType))
                throw new ArgumentException("Change type cannot be null or empty", nameof(changeType));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "HealthCheckSystem" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckServiceConfigurationChangedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("HealthCheckServiceConfigChange", changeType)
                : correlationId;

            return new HealthCheckServiceConfigurationChangedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckServiceConfigurationChangedMessage,
                Source = source.IsEmpty ? "HealthCheckSystem" : source,
                Priority = priority,
                CorrelationId = finalCorrelationId,
                ChangeType = changeType?.Length <= 64 ? changeType : changeType?[..64] ?? string.Empty,
                ChangeDescription = changeDescription?.Length <= 512 ? changeDescription : changeDescription?[..512] ?? string.Empty,
                AutomaticChecksEnabled = automaticChecksEnabled,
                NewAutomaticCheckInterval = newAutomaticCheckInterval,
                MaxConcurrentHealthChecks = maxConcurrentHealthChecks,
                CircuitBreakerEnabled = circuitBreakerEnabled,
                GracefulDegradationEnabled = gracefulDegradationEnabled,
                ChangedBy = changedBy?.Length <= 64 ? changedBy : changedBy?[..64] ?? "System",
                PreviousVersion = previousVersion?.Length <= 64 ? previousVersion : previousVersion?[..64] ?? string.Empty,
                NewVersion = newVersion?.Length <= 64 ? newVersion : newVersion?[..64] ?? string.Empty
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Health check service configuration changed message string representation</returns>
        public override string ToString()
        {
            var changeTypeText = ChangeType.IsEmpty ? "Unknown" : ChangeType.ToString();
            var changedByText = ChangedBy.IsEmpty ? "System" : ChangedBy.ToString();
            return $"HealthCheckServiceConfigChanged: {changeTypeText} by {changedByText} (AutoChecks: {AutomaticChecksEnabled}, CB: {CircuitBreakerEnabled})";
        }

        #endregion
    }
}