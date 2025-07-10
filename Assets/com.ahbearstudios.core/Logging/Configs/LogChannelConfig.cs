using System.Collections.Generic;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Configs
{
    /// <summary>
    /// Configuration record for log channels.
    /// Channels provide domain-specific log categorization for better organization.
    /// </summary>
    public sealed record LogChannelConfig
    {
        /// <summary>
        /// Gets or sets the unique name of the log channel.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of what this channel is used for.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the minimum log level for this channel.
        /// </summary>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

        /// <summary>
        /// Gets or sets whether this channel is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the color associated with this channel for console output.
        /// </summary>
        public string Color { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the prefix to be added to messages from this channel.
        /// </summary>
        public string Prefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets channel-specific configuration properties.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets whether this channel should include timestamps in its messages.
        /// </summary>
        public bool IncludeTimestamps { get; set; } = true;

        /// <summary>
        /// Gets or sets whether this channel should include correlation IDs in its messages.
        /// </summary>
        public bool IncludeCorrelationId { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of target names this channel should output to.
        /// If empty, the channel will output to all targets.
        /// </summary>
        public List<string> TargetNames { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the maximum number of messages per second for this channel (rate limiting).
        /// Zero means no rate limiting.
        /// </summary>
        public int MaxMessagesPerSecond { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether this channel should use structured logging.
        /// </summary>
        public bool UseStructuredLogging { get; set; } = true;

        /// <summary>
        /// Validates the channel configuration and returns any validation errors.
        /// </summary>
        /// <returns>A list of validation errors, empty if configuration is valid</returns>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Channel name cannot be null or empty.");

            if (MaxMessagesPerSecond < 0)
                errors.Add("Max messages per second cannot be negative.");

            return errors.AsReadOnly();
        }

        /// <summary>
        /// Creates a copy of this configuration with the specified modifications.
        /// </summary>
        /// <param name="modifications">Action to apply modifications to the copy</param>
        /// <returns>A new LogChannelConfig instance with the modifications applied</returns>
        public LogChannelConfig WithModifications(Action<LogChannelConfig> modifications)
        {
            var copy = this with { };
            modifications(copy);
            return copy;
        }
    }
}