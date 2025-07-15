using System.Collections.Generic;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Configs
{
    /// <summary>
    /// Interface for log target configuration with strongly-typed properties.
    /// Provides game-optimized configuration options for Unity development.
    /// </summary>
    public interface ILogTargetConfig
    {
        /// <summary>
        /// Gets the unique name of the log target.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the type of the log target (e.g., "Console", "File", "Network").
        /// </summary>
        string TargetType { get; }

        /// <summary>
        /// Gets the minimum log level for this target.
        /// </summary>
        LogLevel MinimumLevel { get; }

        /// <summary>
        /// Gets whether this target is enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets the maximum number of messages to buffer for this target.
        /// </summary>
        int BufferSize { get; }

        /// <summary>
        /// Gets the flush interval for this target.
        /// </summary>
        TimeSpan FlushInterval { get; }

        /// <summary>
        /// Gets whether this target should use asynchronous writing.
        /// </summary>
        bool UseAsyncWrite { get; }

        /// <summary>
        /// Gets target-specific configuration properties.
        /// </summary>
        IReadOnlyDictionary<string, object> Properties { get; }

        /// <summary>
        /// Gets the message format template specific to this target.
        /// If null or empty, the global message format will be used.
        /// </summary>
        string MessageFormat { get; }

        /// <summary>
        /// Gets the list of channels this target should listen to.
        /// If empty, the target will listen to all channels.
        /// </summary>
        IReadOnlyList<string> Channels { get; }

        /// <summary>
        /// Gets whether this target should include stack traces in error messages.
        /// </summary>
        bool IncludeStackTrace { get; }

        /// <summary>
        /// Gets whether this target should include correlation IDs in messages.
        /// </summary>
        bool IncludeCorrelationId { get; }

        // Game-specific performance monitoring configuration
        
        /// <summary>
        /// Gets the error rate threshold (0.0 to 1.0) that triggers alerts.
        /// Default: 0.1 (10% error rate)
        /// </summary>
        double ErrorRateThreshold { get; }

        /// <summary>
        /// Gets the frame budget threshold in milliseconds per write operation.
        /// Operations exceeding this threshold will trigger performance alerts.
        /// Default: 0.5ms for 60 FPS games (16.67ms frame budget)
        /// </summary>
        double FrameBudgetThresholdMs { get; }

        /// <summary>
        /// Gets the alert suppression interval in minutes.
        /// Prevents alert spam by suppressing duplicate alerts within this timeframe.
        /// Default: 5 minutes
        /// </summary>
        int AlertSuppressionIntervalMinutes { get; }

        /// <summary>
        /// Gets the maximum concurrent async operations for this target.
        /// Limits memory usage and prevents thread pool exhaustion.
        /// Default: 10 concurrent operations
        /// </summary>
        int MaxConcurrentAsyncOperations { get; }

        /// <summary>
        /// Gets whether Unity Profiler integration is enabled.
        /// When enabled, operations will be tracked in Unity Profiler.
        /// Default: true in development builds, false in production
        /// </summary>
        bool EnableUnityProfilerIntegration { get; }

        /// <summary>
        /// Gets whether performance metrics should be tracked and reported.
        /// Default: true
        /// </summary>
        bool EnablePerformanceMetrics { get; }

        /// <summary>
        /// Gets the health check interval in seconds.
        /// More frequent checks for game development scenarios.
        /// Default: 30 seconds
        /// </summary>
        int HealthCheckIntervalSeconds { get; }

        /// <summary>
        /// Validates the target configuration and returns any validation errors.
        /// </summary>
        /// <returns>A list of validation errors, empty if configuration is valid</returns>
        IReadOnlyList<string> Validate();
    }
}