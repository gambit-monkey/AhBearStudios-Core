using System.Collections.Generic;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Configs
{
    /// <summary>
    /// Configuration record for individual log targets.
    /// Defines how and where log messages are output with game-optimized performance settings.
    /// </summary>
    public sealed record LogTargetConfig : ILogTargetConfig
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
        /// Gets the list of channels this target should listen to as read-only.
        /// </summary>
        IReadOnlyList<string> ILogTargetConfig.Channels => Channels;

        /// <summary>
        /// Gets target-specific configuration properties as read-only dictionary.
        /// </summary>
        IReadOnlyDictionary<string, object> ILogTargetConfig.Properties => Properties;

        /// <summary>
        /// Gets or sets whether this target should include stack traces in error messages.
        /// </summary>
        public bool IncludeStackTrace { get; set; } = true;

        /// <summary>
        /// Gets or sets whether this target should include correlation IDs in messages.
        /// </summary>
        public bool IncludeCorrelationId { get; set; } = true;

        // Game-specific performance monitoring configuration

        /// <summary>
        /// Gets or sets the error rate threshold (0.0 to 1.0) that triggers alerts.
        /// Default: 0.1 (10% error rate)
        /// </summary>
        public double ErrorRateThreshold { get; set; } = 0.1;

        /// <summary>
        /// Gets or sets the frame budget threshold in milliseconds per write operation.
        /// Operations exceeding this threshold will trigger performance alerts.
        /// Default: 0.5ms for 60 FPS games (16.67ms frame budget)
        /// </summary>
        public double FrameBudgetThresholdMs { get; set; } = 0.5;

        /// <summary>
        /// Gets or sets the alert suppression interval in minutes.
        /// Prevents alert spam by suppressing duplicate alerts within this timeframe.
        /// Default: 5 minutes
        /// </summary>
        public int AlertSuppressionIntervalMinutes { get; set; } = 5;

        /// <summary>
        /// Gets or sets the maximum concurrent async operations for this target.
        /// Limits memory usage and prevents thread pool exhaustion.
        /// Default: 10 concurrent operations
        /// </summary>
        public int MaxConcurrentAsyncOperations { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether Unity Profiler integration is enabled.
        /// When enabled, operations will be tracked in Unity Profiler.
        /// Default: true in development builds, false in production
        /// </summary>
        public bool EnableUnityProfilerIntegration { get; set; } = true;

        /// <summary>
        /// Gets or sets whether performance metrics should be tracked and reported.
        /// Default: true
        /// </summary>
        public bool EnablePerformanceMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets the health check interval in seconds.
        /// More frequent checks for game development scenarios.
        /// Default: 30 seconds
        /// </summary>
        public int HealthCheckIntervalSeconds { get; set; } = 30;

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

            // Validate Unity game-specific properties
            if (ErrorRateThreshold < 0.0 || ErrorRateThreshold > 1.0)
                errors.Add("Error rate threshold must be between 0.0 and 1.0.");

            if (FrameBudgetThresholdMs < 0.0)
                errors.Add("Frame budget threshold must be non-negative.");

            if (AlertSuppressionIntervalMinutes < 0)
                errors.Add("Alert suppression interval must be non-negative.");

            if (MaxConcurrentAsyncOperations <= 0)
                errors.Add("Max concurrent async operations must be greater than zero.");

            if (HealthCheckIntervalSeconds <= 0)
                errors.Add("Health check interval must be greater than zero.");

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