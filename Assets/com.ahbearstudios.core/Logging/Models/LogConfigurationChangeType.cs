namespace AhBearStudios.Core.Logging.Models
{
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
        ConfigurationReloaded = 13,

        /// <summary>
        /// The default channel was changed.
        /// </summary>
        DefaultChannelChanged = 14
    }
}