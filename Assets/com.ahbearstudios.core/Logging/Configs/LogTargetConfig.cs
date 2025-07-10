using System.Collections.Generic;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Configs
{
    /// <summary>
    /// Configuration record for individual log targets.
    /// Defines how and where log messages are output.
    /// </summary>
    public sealed record LogTargetConfig
    {
        /// <summary>
        /// Gets or sets the unique name of the log target.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the log target (e.g., "Console", "File", "Network").
        /// </summary>
        public string TargetType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the minimum log level for this target.
        /// </summary>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

        /// <summary>
        /// Gets or sets whether this target is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of messages to buffer for this target.
        /// </summary>
        public int BufferSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the flush interval for this target.
        /// </summary>
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets whether this target should use asynchronous writing.
        /// </summary>
        public bool UseAsyncWrite { get; set; } = true;

        /// <summary>
        /// Gets or sets target-specific configuration properties.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the message format template specific to this target.
        /// If null or empty, the global message format will be used.
        /// </summary>
        public string MessageFormat { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of channels this target should listen to.
        /// If empty, the target will listen to all channels.
        /// </summary>
        public List<string> Channels { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether this target should include stack traces in error messages.
        /// </summary>
        public bool IncludeStackTrace { get; set; } = true;

        /// <summary>
        /// Gets or sets whether this target should include correlation IDs in messages.
        /// </summary>
        public bool IncludeCorrelationId { get; set; } = true;

        /// <summary>
        /// Validates the target configuration and returns any validation errors.
        /// </summary>
        /// <returns>A list of validation errors, empty if configuration is valid</returns>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Target name cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(TargetType))
                errors.Add("Target type cannot be null or empty.");

            if (BufferSize <= 0)
                errors.Add("Buffer size must be greater than zero.");

            if (FlushInterval <= TimeSpan.Zero)
                errors.Add("Flush interval must be greater than zero.");

            return errors.AsReadOnly();
        }

        /// <summary>
        /// Creates a copy of this configuration with the specified modifications.
        /// </summary>
        /// <param name="modifications">Action to apply modifications to the copy</param>
        /// <returns>A new LogTargetConfig instance with the modifications applied</returns>
        public LogTargetConfig WithModifications(Action<LogTargetConfig> modifications)
        {
            var copy = this with { };
            modifications(copy);
            return copy;
        }
    }
}