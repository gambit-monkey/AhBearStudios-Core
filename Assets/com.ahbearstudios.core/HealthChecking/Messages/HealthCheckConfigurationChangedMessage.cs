using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Message published when a health check configuration is modified at runtime.
    /// Follows CLAUDE.md patterns with factory methods and zero-allocation design.
    /// </summary>
    public readonly record struct HealthCheckConfigurationChangedMessage : IMessage
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

        /// <summary>
        /// Gets the name of the health check configuration that changed.
        /// </summary>
        public FixedString64Bytes ConfigurationName { get; init; }

        /// <summary>
        /// Gets the type of configuration change that occurred.
        /// </summary>
        public FixedString64Bytes ChangeType { get; init; }

        /// <summary>
        /// Gets the description of what changed.
        /// </summary>
        public FixedString512Bytes ChangeDescription { get; init; }

        /// <summary>
        /// Gets whether the configuration is now enabled.
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets the new interval value in ticks (if changed, 0 if not changed).
        /// </summary>
        public long NewIntervalTicks { get; init; }

        /// <summary>
        /// Gets the new timeout value in ticks (if changed, 0 if not changed).
        /// </summary>
        public long NewTimeoutTicks { get; init; }

        /// <summary>
        /// Gets the user or system that made the change.
        /// </summary>
        public FixedString128Bytes ChangedBy { get; init; }

        /// <summary>
        /// Gets additional metadata about the change.
        /// </summary>
        public FixedString512Bytes Metadata { get; init; }

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the TimeSpan representation of the new interval (if changed).
        /// </summary>
        public TimeSpan? NewInterval => NewIntervalTicks > 0 ? new TimeSpan(NewIntervalTicks) : null;

        /// <summary>
        /// Gets the TimeSpan representation of the new timeout (if changed).
        /// </summary>
        public TimeSpan? NewTimeout => NewTimeoutTicks > 0 ? new TimeSpan(NewTimeoutTicks) : null;

        /// <summary>
        /// Creates a new HealthCheckConfigurationChangedMessage following CLAUDE.md patterns.
        /// </summary>
        /// <param name="configurationName">Name of the health check configuration</param>
        /// <param name="changeType">Type of change (Updated, Enabled, Disabled, etc.)</param>
        /// <param name="changeDescription">Description of what changed</param>
        /// <param name="isEnabled">Whether the configuration is now enabled</param>
        /// <param name="newInterval">New interval value (if changed)</param>
        /// <param name="newTimeout">New timeout value (if changed)</param>
        /// <param name="changedBy">User or system that made the change</param>
        /// <param name="metadata">Additional metadata about the change</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="priority">Message priority level</param>
        /// <returns>New HealthCheckConfigurationChangedMessage instance</returns>
        public static HealthCheckConfigurationChangedMessage Create(
            FixedString64Bytes configurationName,
            FixedString64Bytes changeType,
            string changeDescription = "",
            bool isEnabled = true,
            TimeSpan? newInterval = null,
            TimeSpan? newTimeout = null,
            string changedBy = "System",
            string metadata = "",
            FixedString64Bytes source = default,
            Guid correlationId = default,
            MessagePriority priority = MessagePriority.Normal)
        {
            var sourceString = source.IsEmpty ? "HealthCheckSystem" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckConfigurationChangedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("HealthCheckConfigChange", configurationName.ToString())
                : correlationId;

            return new HealthCheckConfigurationChangedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckConfigurationChangedMessage,
                Source = source.IsEmpty ? "HealthCheckSystem" : source,
                Priority = priority,
                CorrelationId = finalCorrelationId,
                ConfigurationName = configurationName,
                ChangeType = changeType,
                ChangeDescription = changeDescription?.Length <= 512 ? changeDescription : changeDescription?[..512] ?? "",
                IsEnabled = isEnabled,
                NewIntervalTicks = newInterval?.Ticks ?? 0,
                NewTimeoutTicks = newTimeout?.Ticks ?? 0,
                ChangedBy = changedBy?.Length <= 128 ? changedBy : changedBy?[..128] ?? "System",
                Metadata = metadata?.Length <= 512 ? metadata : metadata?[..512] ?? ""
            };
        }

        /// <summary>
        /// Creates a new message for configuration enabled/disabled changes.
        /// </summary>
        /// <param name="configurationName">Name of the health check configuration</param>
        /// <param name="isEnabled">Whether the configuration is now enabled</param>
        /// <param name="changedBy">User or system that made the change</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>New HealthCheckConfigurationChangedMessage instance</returns>
        public static HealthCheckConfigurationChangedMessage CreateEnabledChange(
            FixedString64Bytes configurationName,
            bool isEnabled,
            string changedBy = "System",
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            var changeType = isEnabled ? "Enabled" : "Disabled";
            var description = $"Health check configuration '{configurationName}' was {changeType.ToLower()}";

            return Create(
                configurationName: configurationName,
                changeType: changeType,
                changeDescription: description,
                isEnabled: isEnabled,
                changedBy: changedBy,
                source: source,
                correlationId: correlationId,
                priority: MessagePriority.Normal);
        }

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Configuration change message string representation</returns>
        public override string ToString()
        {
            return $"HealthCheckConfigChanged: {ConfigurationName} - {ChangeType} (Enabled: {IsEnabled})";
        }
    }
}