using System.Collections.Generic;
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
        /// Gets the default logging configuration for production use.
        /// </summary>
        public static LoggingConfig Default => new LoggingConfig
        {
            GlobalMinimumLevel = LogLevel.Info,
            IsLoggingEnabled = true,
            MaxQueueSize = 1000,
            FlushInterval = TimeSpan.FromMilliseconds(100),
            HighPerformanceMode = true,
            BurstCompatibility = true,
            StructuredLogging = true,
            BatchingEnabled = true,
            BatchSize = 100,
            CorrelationIdFormat = "{0:N}",
            AutoCorrelationId = true,
            MessageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}",
            IncludeTimestamps = true,
            TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff",
            CachingEnabled = true,
            MaxCacheSize = 1000,
            TargetConfigs = new List<LogTargetConfig>().AsReadOnly(),
            ChannelConfigs = new List<LogChannelConfig> 
            { 
                new LogChannelConfig 
                { 
                    Name = "Default", 
                    MinimumLevel = LogLevel.Debug, 
                    IsEnabled = true 
                } 
            }.AsReadOnly()
        };

        /// <summary>
        /// Gets or sets the global minimum log level. Messages below this level will be filtered out.
        /// </summary>
        public LogLevel GlobalMinimumLevel { get; init; } = LogLevel.Info;

        /// <summary>
        /// Gets or sets whether logging is enabled globally.
        /// </summary>
        public bool IsLoggingEnabled { get; init; } = true;

        /// <summary>
        /// Gets or sets the maximum number of log messages to queue when batching is enabled.
        /// </summary>
        public int MaxQueueSize { get; init; } = 1000;

        /// <summary>
        /// Gets or sets the interval at which batched log messages are flushed.
        /// </summary>
        public TimeSpan FlushInterval { get; init; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets whether high-performance mode is enabled for zero-allocation logging.
        /// When enabled, uses Unity.Collections v2 and object pooling for optimal performance.
        /// </summary>
        public bool HighPerformanceMode { get; init; } = true;

        /// <summary>
        /// Gets or sets whether Burst compilation compatibility is enabled for native job system integration.
        /// When enabled, uses native-compatible data structures and algorithms.
        /// </summary>
        public bool BurstCompatibility { get; init; } = true;

        /// <summary>
        /// Gets or sets whether structured logging is enabled for rich contextual data.
        /// </summary>
        public bool StructuredLogging { get; init; } = true;

        /// <summary>
        /// Gets or sets whether batching is enabled for high-throughput scenarios.
        /// </summary>
        public bool BatchingEnabled { get; init; } = true;

        /// <summary>
        /// Gets or sets the number of messages to batch before flushing.
        /// </summary>
        public int BatchSize { get; init; } = 100;

        /// <summary>
        /// Gets or sets the format string for correlation IDs used to track operations across system boundaries.
        /// </summary>
        public string CorrelationIdFormat { get; init; } = "{0:N}";

        /// <summary>
        /// Gets or sets whether automatic correlation ID generation is enabled.
        /// </summary>
        public bool AutoCorrelationId { get; init; } = true;

        /// <summary>
        /// Gets or sets the message format template for log output.
        /// </summary>
        public string MessageFormat { get; init; } = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}";

        /// <summary>
        /// Gets or sets whether timestamps are included in log messages.
        /// </summary>
        public bool IncludeTimestamps { get; init; } = true;

        /// <summary>
        /// Gets or sets the format string for timestamps in log messages.
        /// </summary>
        public string TimestampFormat { get; init; } = "yyyy-MM-dd HH:mm:ss.fff";

        /// <summary>
        /// Gets or sets whether message formatting caching is enabled for performance.
        /// </summary>
        public bool CachingEnabled { get; init; } = true;

        /// <summary>
        /// Gets or sets the maximum cache size for formatted messages.
        /// </summary>
        public int MaxCacheSize { get; init; } = 1000;

        /// <summary>
        /// Gets or sets the collection of log target configurations.
        /// </summary>
        public IReadOnlyList<LogTargetConfig> TargetConfigs { get; init; } = new List<LogTargetConfig>().AsReadOnly();

        /// <summary>
        /// Gets or sets the collection of log channel configurations.
        /// </summary>
        public IReadOnlyList<LogChannelConfig> ChannelConfigs { get; init; } = new List<LogChannelConfig>().AsReadOnly();

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

            if (BatchingEnabled && BatchSize <= 0)
                errors.Add("Batch size must be greater than zero when batching is enabled.");

            if (string.IsNullOrWhiteSpace(CorrelationIdFormat))
                errors.Add("Correlation ID format cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(MessageFormat))
                errors.Add("Message format template cannot be null or empty.");

            if (IncludeTimestamps && string.IsNullOrWhiteSpace(TimestampFormat))
                errors.Add("Timestamp format cannot be null or empty when timestamps are enabled.");

            if (CachingEnabled && MaxCacheSize <= 0)
                errors.Add("Max cache size must be greater than zero when caching is enabled.");

            // Validate nested configurations
            foreach (var targetConfig in TargetConfigs)
            {
                var targetErrors = targetConfig.Validate();
                errors.AddRange(targetErrors);
            }

            foreach (var channelConfig in ChannelConfigs)
            {
                var channelErrors = channelConfig.Validate();
                errors.AddRange(channelErrors);
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
            if (modifications == null)
                throw new ArgumentNullException(nameof(modifications));

            var copy = this with { };
            modifications(copy);
            return copy;
        }
    }
}