using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Targets;
using AhBearStudios.Core.Logging.Filters;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Primary logging service interface providing centralized logging
    /// with correlation tracking and comprehensive system integration.
    /// Follows the AhBearStudios Core Architecture foundation system pattern.
    /// Designed for Unity game development with Job System and Burst compatibility.
    /// </summary>
    public interface ILoggingService : IDisposable
    {
        // Configuration and runtime state properties
        /// <summary>
        /// Gets the current configuration of the logging service.
        /// </summary>
        LoggingConfig Configuration { get; }

        /// <summary>
        /// Gets whether the logging service is enabled.
        /// </summary>
        bool IsEnabled { get; }

        // Core logging methods with Unity.Collections v2 correlation tracking
        /// <summary>
        /// Logs a debug message with correlation tracking.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="sourceContext">Source context (typically class name)</param>
        /// <param name="properties">Additional structured properties</param>
        void LogDebug(string message, FixedString64Bytes correlationId = default, 
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

        /// <summary>
        /// Logs an informational message with correlation tracking.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="sourceContext">Source context (typically class name)</param>
        /// <param name="properties">Additional structured properties</param>
        void LogInfo(string message, FixedString64Bytes correlationId = default, 
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

        /// <summary>
        /// Logs a warning message with correlation tracking.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="sourceContext">Source context (typically class name)</param>
        /// <param name="properties">Additional structured properties</param>
        void LogWarning(string message, FixedString64Bytes correlationId = default, 
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

        /// <summary>
        /// Logs an error message with correlation tracking.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="sourceContext">Source context (typically class name)</param>
        /// <param name="properties">Additional structured properties</param>
        void LogError(string message, FixedString64Bytes correlationId = default, 
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

        /// <summary>
        /// Logs a critical message with correlation tracking and automatic alerting.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="sourceContext">Source context (typically class name)</param>
        /// <param name="properties">Additional structured properties</param>
        void LogCritical(string message, FixedString64Bytes correlationId = default, 
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

        // Unity Job System and Burst-compatible logging methods
        /// <summary>
        /// Logs a debug message with structured data using generic type constraints for Burst compatibility.
        /// Designed for use within Unity Job System contexts.
        /// </summary>
        /// <typeparam name="T">The type of structured data (must be unmanaged for Burst compatibility)</typeparam>
        /// <param name="message">The message to log</param>
        /// <param name="data">The structured data to log</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void LogDebug<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged;

        /// <summary>
        /// Logs an informational message with structured data using generic type constraints for Burst compatibility.
        /// Designed for use within Unity Job System contexts.
        /// </summary>
        /// <typeparam name="T">The type of structured data (must be unmanaged for Burst compatibility)</typeparam>
        /// <param name="message">The message to log</param>
        /// <param name="data">The structured data to log</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void LogInfo<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged;

        /// <summary>
        /// Logs a warning message with structured data using generic type constraints for Burst compatibility.
        /// Designed for use within Unity Job System contexts.
        /// </summary>
        /// <typeparam name="T">The type of structured data (must be unmanaged for Burst compatibility)</typeparam>
        /// <param name="message">The message to log</param>
        /// <param name="data">The structured data to log</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void LogWarning<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged;

        /// <summary>
        /// Logs an error message with structured data using generic type constraints for Burst compatibility.
        /// Designed for use within Unity Job System contexts.
        /// </summary>
        /// <typeparam name="T">The type of structured data (must be unmanaged for Burst compatibility)</typeparam>
        /// <param name="message">The message to log</param>
        /// <param name="data">The structured data to log</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void LogError<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged;

        /// <summary>
        /// Logs a critical message with structured data using generic type constraints for Burst compatibility.
        /// Designed for use within Unity Job System contexts.
        /// </summary>
        /// <typeparam name="T">The type of structured data (must be unmanaged for Burst compatibility)</typeparam>
        /// <param name="message">The message to log</param>
        /// <param name="data">The structured data to log</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void LogCritical<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged;

        /// <summary>
        /// Logs an exception with context and correlation tracking.
        /// </summary>
        /// <param name="message">Context message for the exception</param>
        /// <param name="exception">The exception to log</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="sourceContext">Source context (typically class name)</param>
        /// <param name="properties">Additional structured properties</param>
        void LogException(string message, Exception exception, FixedString64Bytes correlationId = default, 
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null);

        /// <summary>
        /// Logs a message with the specified level and full context.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="message">The message to log</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="sourceContext">Source context</param>
        /// <param name="exception">Associated exception (optional)</param>
        /// <param name="properties">Structured properties</param>
        /// <param name="channel">Specific channel for the log</param>
        void Log(LogLevel level, string message, FixedString64Bytes correlationId = default, 
            string sourceContext = null, Exception exception = null, 
            IReadOnlyDictionary<string, object> properties = null, string channel = null);

        // Hierarchical logging scopes with correlation tracking
        /// <summary>
        /// Begins a logging scope for hierarchical context tracking.
        /// </summary>
        /// <param name="scopeName">Name of the scope</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="sourceContext">Source context</param>
        /// <returns>Disposable logging scope</returns>
        ILogScope BeginScope(string scopeName, FixedString64Bytes correlationId = default, 
            string sourceContext = null);

        // Target management with correlation tracking
        /// <summary>
        /// Registers a log target with the service.
        /// </summary>
        /// <param name="target">The log target to register</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void RegisterTarget(ILogTarget target, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Unregisters a log target from the service.
        /// </summary>
        /// <param name="targetName">Name of the target to unregister</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if target was unregistered</returns>
        bool UnregisterTarget(string targetName, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Gets all registered log targets.
        /// </summary>
        /// <returns>Collection of registered targets</returns>
        IReadOnlyCollection<ILogTarget> GetTargets();

        /// <summary>
        /// Sets the minimum log level for filtering.
        /// </summary>
        /// <param name="level">Minimum log level</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void SetMinimumLevel(LogLevel level, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Adds a log filter for advanced filtering.
        /// </summary>
        /// <param name="filter">Log filter to add</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void AddFilter(ILogFilter filter, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Removes a log filter.
        /// </summary>
        /// <param name="filterName">Name of filter to remove</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if filter was removed</returns>
        bool RemoveFilter(string filterName, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Gets current logging statistics for monitoring.
        /// </summary>
        /// <returns>Current logging statistics</returns>
        LoggingStatistics GetStatistics();

        /// <summary>
        /// Flushes all buffered log entries to targets.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Task representing the flush operation</returns>
        Task FlushAsync(FixedString64Bytes correlationId = default);

        /// <summary>
        /// Validates logging configuration and targets.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default);

        /// <summary>
        /// Clears internal caches and performs maintenance.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void PerformMaintenance(FixedString64Bytes correlationId = default);

        // Channel management methods
        /// <summary>
        /// Registers a log channel with the service.
        /// </summary>
        /// <param name="channel">The log channel to register</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void RegisterChannel(ILogChannel channel, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Unregisters a log channel from the service.
        /// </summary>
        /// <param name="channelName">The name of the channel to unregister</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if the channel was unregistered, false if it was not found</returns>
        bool UnregisterChannel(string channelName, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Gets all registered log channels.
        /// </summary>
        /// <returns>A read-only collection of registered channels</returns>
        IReadOnlyCollection<ILogChannel> GetChannels();

        /// <summary>
        /// Gets a registered log channel by name.
        /// </summary>
        /// <param name="channelName">The name of the channel to retrieve</param>
        /// <returns>The log channel if found, null otherwise</returns>
        ILogChannel GetChannel(string channelName);

        /// <summary>
        /// Determines whether a log channel is registered.
        /// </summary>
        /// <param name="channelName">The name of the channel to check</param>
        /// <returns>True if the channel is registered, false otherwise</returns>
        bool HasChannel(string channelName);
    }
}