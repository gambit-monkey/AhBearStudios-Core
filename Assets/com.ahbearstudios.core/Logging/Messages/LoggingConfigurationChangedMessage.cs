using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Logging.Messages
{
    /// <summary>
    /// Message published when logging configuration changes.
    /// Replaces direct EventHandler usage for loose coupling through IMessageBus.
    /// </summary>
    public readonly record struct LoggingConfigurationChangedMessage : IMessage
    {
        #region IMessage Implementation

        /// <summary>
        /// Gets the unique identifier for this message.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets the timestamp when this message was created.
        /// </summary>
        public long TimestampTicks { get; init; }

        /// <summary>
        /// Gets the type code for this message type.
        /// </summary>
        public ushort TypeCode { get; init; }

        /// <summary>
        /// Gets the source system that published this message.
        /// </summary>
        public FixedString64Bytes Source { get; init; }

        /// <summary>
        /// Gets the priority level for message processing.
        /// </summary>
        public MessagePriority Priority { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; init; }

        #endregion

        #region Message-Specific Properties

        /// <summary>
        /// Gets the type of configuration change that occurred.
        /// </summary>
        public LogConfigurationChangeType ChangeType { get; init; }

        /// <summary>
        /// Gets the component that was changed.
        /// </summary>
        public FixedString64Bytes ComponentName { get; init; }

        /// <summary>
        /// Gets the property or setting that was modified.
        /// </summary>
        public FixedString128Bytes PropertyName { get; init; }

        /// <summary>
        /// Gets the previous value (if applicable).
        /// </summary>
        public FixedString512Bytes PreviousValue { get; init; }

        /// <summary>
        /// Gets the new value.
        /// </summary>
        public FixedString512Bytes NewValue { get; init; }

        /// <summary>
        /// Gets the user or system that made the change.
        /// </summary>
        public FixedString64Bytes ChangedBy { get; init; }

        /// <summary>
        /// Gets the reason for the change.
        /// </summary>
        public FixedString128Bytes ChangeReason { get; init; }

        /// <summary>
        /// Gets whether the change requires a restart to take effect.
        /// </summary>
        public bool RequiresRestart { get; init; }

        /// <summary>
        /// Gets whether the configuration change was applied successfully.
        /// </summary>
        public bool AppliedSuccessfully { get; init; }

        /// <summary>
        /// Gets the configuration correlation ID for tracking related changes.
        /// </summary>
        public FixedString64Bytes ConfigurationCorrelationId { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new LoggingConfigurationChangedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="changeType">The type of change</param>
        /// <param name="componentName">The component that was changed</param>
        /// <param name="propertyName">The property that was modified</param>
        /// <param name="previousValue">The previous value</param>
        /// <param name="newValue">The new value</param>
        /// <param name="changedBy">Who made the change</param>
        /// <param name="changeReason">The reason for the change</param>
        /// <param name="requiresRestart">Whether a restart is required</param>
        /// <param name="appliedSuccessfully">Whether the change was applied successfully</param>
        /// <param name="configurationCorrelationId">Configuration correlation ID</param>
        /// <param name="correlationId">Message correlation ID</param>
        /// <param name="source">Source component creating this message</param>
        /// <returns>New LoggingConfigurationChangedMessage instance</returns>
        public static LoggingConfigurationChangedMessage CreateFromFixedStrings(
            LogConfigurationChangeType changeType,
            FixedString64Bytes componentName,
            FixedString128Bytes propertyName,
            FixedString512Bytes previousValue,
            FixedString512Bytes newValue,
            FixedString64Bytes changedBy = default,
            FixedString128Bytes changeReason = default,
            bool requiresRestart = false,
            bool appliedSuccessfully = true,
            FixedString64Bytes configurationCorrelationId = default,
            Guid correlationId = default,
            FixedString64Bytes source = default)
        {
            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "LoggingSystem" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("LoggingConfigurationChangedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("LoggingConfigChange", $"{changeType}-{componentName}")
                : correlationId;
            
            return new LoggingConfigurationChangedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.LoggingConfigurationChangedMessage,
                Source = source.IsEmpty ? "LoggingSystem" : source,
                Priority = !appliedSuccessfully ? MessagePriority.High : MessagePriority.Normal,
                CorrelationId = finalCorrelationId,
                
                ChangeType = changeType,
                ComponentName = componentName,
                PropertyName = propertyName,
                PreviousValue = previousValue,
                NewValue = newValue,
                ChangedBy = changedBy.IsEmpty ? "System" : changedBy,
                ChangeReason = changeReason,
                RequiresRestart = requiresRestart,
                AppliedSuccessfully = appliedSuccessfully,
                ConfigurationCorrelationId = configurationCorrelationId
            };
        }

        /// <summary>
        /// Creates a LoggingConfigurationChangedMessage from string parameters for convenience.
        /// </summary>
        /// <param name="changeType">The type of change</param>
        /// <param name="componentName">The component name</param>
        /// <param name="propertyName">The property name</param>
        /// <param name="previousValue">The previous value</param>
        /// <param name="newValue">The new value</param>
        /// <param name="changedBy">Who made the change</param>
        /// <param name="changeReason">The reason for the change</param>
        /// <param name="requiresRestart">Whether a restart is required</param>
        /// <param name="appliedSuccessfully">Whether the change was applied successfully</param>
        /// <param name="configurationCorrelationId">Configuration correlation ID</param>
        /// <param name="correlationId">Message correlation ID</param>
        /// <param name="source">Source component creating this message</param>
        /// <returns>A new LoggingConfigurationChangedMessage</returns>
        public static LoggingConfigurationChangedMessage Create(
            LogConfigurationChangeType changeType,
            string componentName,
            string propertyName,
            string previousValue = null,
            string newValue = null,
            string changedBy = null,
            string changeReason = null,
            bool requiresRestart = false,
            bool appliedSuccessfully = true,
            string configurationCorrelationId = null,
            Guid correlationId = default,
            string source = null)
        {
            return CreateFromFixedStrings(
                changeType,
                new FixedString64Bytes(componentName ?? "Unknown"),
                new FixedString128Bytes(propertyName ?? "Unknown"),
                new FixedString512Bytes(previousValue ?? string.Empty),
                new FixedString512Bytes(newValue ?? string.Empty),
                new FixedString64Bytes(changedBy ?? "System"),
                new FixedString128Bytes(changeReason ?? string.Empty),
                requiresRestart,
                appliedSuccessfully,
                new FixedString64Bytes(configurationCorrelationId ?? string.Empty),
                correlationId,
                new FixedString64Bytes(source ?? "LoggingSystem"));
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message.
        /// </summary>
        /// <returns>A formatted string</returns>
        public override string ToString()
        {
            var status = AppliedSuccessfully ? "Applied" : "Failed";
            var restart = RequiresRestart ? " (restart required)" : "";
            var componentName = ComponentName.IsEmpty ? "Unknown" : ComponentName.ToString();
            var propertyName = PropertyName.IsEmpty ? "Unknown" : PropertyName.ToString();
            var newValue = NewValue.IsEmpty ? string.Empty : NewValue.ToString();
            var previousValue = PreviousValue.IsEmpty ? string.Empty : PreviousValue.ToString();
            var changedBy = ChangedBy.IsEmpty ? "System" : ChangedBy.ToString();
            
            return $"LogConfigurationChanged: {ChangeType} - {componentName}.{propertyName} = '{newValue}' " +
                   $"(was '{previousValue}') by {changedBy} - {status}{restart}";
        }

        #endregion
    }
}