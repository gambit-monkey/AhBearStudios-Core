using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Logging.Messages
{
    /// <summary>
    /// Message published when logging configuration changes.
    /// Replaces direct EventHandler usage for loose coupling through IMessageBus.
    /// </summary>
    public readonly struct LoggingConfigurationChangedMessage : IMessage
    {
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
        public ushort TypeCode { get; init; } = MessageTypeCodes.LoggingConfigurationChangedMessage;

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

        /// <summary>
        /// Initializes a new instance of the LoggingConfigurationChangedMessage struct.
        /// </summary>
        public LoggingConfigurationChangedMessage()
        {
            Id = default;
            TimestampTicks = default;
            Source = default;
            Priority = default;
            CorrelationId = default;
            ChangeType = default;
            ComponentName = default;
            PropertyName = default;
            PreviousValue = default;
            NewValue = default;
            ChangedBy = default;
            ChangeReason = default;
            RequiresRestart = default;
            AppliedSuccessfully = default;
            ConfigurationCorrelationId = default;
        }

        /// <summary>
        /// Initializes a new instance of the LoggingConfigurationChangedMessage with parameters.
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
        public LoggingConfigurationChangedMessage(
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
            Guid correlationId = default)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = MessageTypeCodes.LoggingConfigurationChangedMessage;
            Source = new FixedString64Bytes("LoggingSystem");
            Priority = !appliedSuccessfully ? MessagePriority.High : MessagePriority.Normal;
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId;
            
            ChangeType = changeType;
            ComponentName = componentName;
            PropertyName = propertyName;
            PreviousValue = previousValue;
            NewValue = newValue;
            ChangedBy = changedBy.IsEmpty ? new FixedString64Bytes("System") : changedBy;
            ChangeReason = changeReason;
            RequiresRestart = requiresRestart;
            AppliedSuccessfully = appliedSuccessfully;
            ConfigurationCorrelationId = configurationCorrelationId;
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
            Guid correlationId = default)
        {
            return new LoggingConfigurationChangedMessage(
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
                correlationId);
        }

        /// <summary>
        /// Returns a string representation of this message.
        /// </summary>
        /// <returns>A formatted string</returns>
        public override string ToString()
        {
            var status = AppliedSuccessfully ? "Applied" : "Failed";
            var restart = RequiresRestart ? " (restart required)" : "";
            return $"LogConfigurationChanged: {ChangeType} - {ComponentName}.{PropertyName} = '{NewValue}' " +
                   $"(was '{PreviousValue}') by {ChangedBy} - {status}{restart}";
        }
    }

    /// <summary>
    /// Defines the types of configuration changes.
    /// </summary>
    public enum LogConfigurationChangeType : byte
    {
        /// <summary>
        /// A target was added to the logging system.
        /// </summary>
        TargetAdded = 0,

        /// <summary>
        /// A target was removed from the logging system.
        /// </summary>
        TargetRemoved = 1,

        /// <summary>
        /// A target's configuration was modified.
        /// </summary>
        TargetModified = 2,

        /// <summary>
        /// A channel was added to the logging system.
        /// </summary>
        ChannelAdded = 3,

        /// <summary>
        /// A channel was removed from the logging system.
        /// </summary>
        ChannelRemoved = 4,

        /// <summary>
        /// A channel's configuration was modified.
        /// </summary>
        ChannelModified = 5,

        /// <summary>
        /// A filter was added to the logging system.
        /// </summary>
        FilterAdded = 6,

        /// <summary>
        /// A filter was removed from the logging system.
        /// </summary>
        FilterRemoved = 7,

        /// <summary>
        /// A filter's configuration was modified.
        /// </summary>
        FilterModified = 8,

        /// <summary>
        /// The global logging level was changed.
        /// </summary>
        GlobalLevelChanged = 9,

        /// <summary>
        /// The logging system was enabled or disabled.
        /// </summary>
        SystemEnabledChanged = 10,

        /// <summary>
        /// Logging performance settings were modified.
        /// </summary>
        PerformanceSettingsChanged = 11,

        /// <summary>
        /// Security or audit settings were modified.
        /// </summary>
        SecuritySettingsChanged = 12,

        /// <summary>
        /// A complete configuration reload was performed.
        /// </summary>
        ConfigurationReloaded = 13
    }
}