using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Configs
{
    /// <summary>
    /// Configuration record for the logging system.
    /// Contains all settings required to configure the high-performance logging infrastructure.
    /// </summary>
    public sealed record LoggingConfig
    {
        /// <summary>
        /// Gets or sets the global minimum log level. Messages below this level will be filtered out.
        /// </summary>
        public LogLevel GlobalMinimumLevel { get; set; } = LogLevel.Info;

        /// <summary>
        /// Gets or sets whether logging is enabled globally.
        /// </summary>
        public bool IsLoggingEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of log messages to queue when batching is enabled.
        /// </summary>
        public int MaxQueueSize { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the interval at which batched log messages are flushed.
        /// </summary>
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets whether high-performance mode is enabled for zero-allocation logging.
        /// </summary>
        public bool HighPerformanceMode { get; set; } = false;

        /// <summary>
        /// Gets or sets whether Burst compilation compatibility is enabled for native job system integration.
        /// </summary>
        public bool BurstCompatibility { get; set; } = false;

        /// <summary>
        /// Gets or sets whether structured logging is enabled for rich contextual data.
        /// </summary>
        public bool StructuredLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets the format string for correlation IDs used to track operations across system boundaries.
        /// </summary>
        public string CorrelationIdFormat { get; set; } = "{0:N}";

        /// <summary>
        /// Gets or sets whether automatic correlation ID generation is enabled.
        /// </summary>
        public bool AutoCorrelationId { get; set; } = true;

        /// <summary>
        /// Gets or sets the message format template for log output.
        /// </summary>
        public string MessageFormat { get; set; } = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}";

        /// <summary>
        /// Gets or sets whether timestamps are included in log messages.
        /// </summary>
        public bool IncludeTimestamps { get; set; } = true;

        /// <summary>
        /// Gets or sets the format string for timestamps in log messages.
        /// </summary>
        public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";

        /// <summary>
        /// Gets or sets the collection of log target configurations.
        /// </summary>
        public IReadOnlyList<LogTargetConfig> TargetConfigs { get; set; } = new List<LogTargetConfig>().AsReadOnly();

        /// <summary>
        /// Gets or sets the collection of log channel configurations.
        /// </summary>
        public IReadOnlyList<LogChannelConfig> ChannelConfigs { get; set; } = new List<LogChannelConfig>().AsReadOnly();

        /// <summary>
        /// Validates the configuration and returns any validation errors.
        /// </summary>
        /// <returns>A list of validation errors, empty if configuration is valid</returns>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            if (MaxQueueSize <= 0)
                errors.Add("Max queue size must be greater than zero.");

            if (FlushInterval <= TimeSpan.Zero)
                errors.Add("Flush interval must be greater than zero.");

            if (string.IsNullOrWhiteSpace(CorrelationIdFormat))
                errors.Add("Correlation ID format cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(MessageFormat))
                errors.Add("Message format template cannot be null or empty.");

            if (IncludeTimestamps && string.IsNullOrWhiteSpace(TimestampFormat))
                errors.Add("Timestamp format cannot be null or empty when timestamps are enabled.");

            if (TargetConfigs.Count == 0)
                errors.Add("At least one log target must be configured.");

            if (ChannelConfigs.Count == 0)
                errors.Add("At least one log channel must be configured.");

            // Validate nested configurations
            foreach (var targetConfig in TargetConfigs)
            {
                errors.AddRange(targetConfig.Validate());
            }

            foreach (var channelConfig in ChannelConfigs)
            {
                errors.AddRange(channelConfig.Validate());
            }

            return errors.AsReadOnly();
        }

        /// <summary>
        /// Creates a copy of this configuration with the specified modifications.
        /// </summary>
        /// <param name="modifications">Action to apply modifications to the copy</param>
        /// <returns>A new LoggingConfig instance with the modifications applied</returns>
        public LoggingConfig WithModifications(Action<LoggingConfig> modifications)
        {
            var copy = this with { };
            modifications(copy);
            return copy;
        }
    }
}