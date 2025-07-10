using System.Collections.Generic;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Targets;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Primary interface for the logging service.
    /// Provides centralized logging with multiple targets and advanced features.
    /// Follows the AhBearStudios Core Architecture foundation system pattern.
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Logs a debug message with optional structured data.
        /// </summary>
        /// <param name="message">The message to log</param>
        void LogDebug(string message);

        /// <summary>
        /// Logs a debug message with structured properties.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="properties">Additional structured properties</param>
        void LogDebug(string message, IReadOnlyDictionary<string, object> properties);

        /// <summary>
        /// Logs a debug message with correlation ID and source context.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="correlationId">The correlation ID for tracking</param>
        /// <param name="sourceContext">The source context (typically class name)</param>
        void LogDebug(string message, string correlationId, string sourceContext = null);

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log</param>
        void LogInfo(string message);

        /// <summary>
        /// Logs an informational message with structured properties.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="properties">Additional structured properties</param>
        void LogInfo(string message, IReadOnlyDictionary<string, object> properties);

        /// <summary>
        /// Logs an informational message with correlation ID and source context.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="correlationId">The correlation ID for tracking</param>
        /// <param name="sourceContext">The source context (typically class name)</param>
        void LogInfo(string message, string correlationId, string sourceContext = null);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log</param>
        void LogWarning(string message);

        /// <summary>
        /// Logs a warning message with structured properties.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="properties">Additional structured properties</param>
        void LogWarning(string message, IReadOnlyDictionary<string, object> properties);

        /// <summary>
        /// Logs a warning message with correlation ID and source context.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="correlationId">The correlation ID for tracking</param>
        /// <param name="sourceContext">The source context (typically class name)</param>
        void LogWarning(string message, string correlationId, string sourceContext = null);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log</param>
        void LogError(string message);

        /// <summary>
        /// Logs an error message with structured properties.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="properties">Additional structured properties</param>
        void LogError(string message, IReadOnlyDictionary<string, object> properties);

        /// <summary>
        /// Logs an error message with correlation ID and source context.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="correlationId">The correlation ID for tracking</param>
        /// <param name="sourceContext">The source context (typically class name)</param>
        void LogError(string message, string correlationId, string sourceContext = null);

        /// <summary>
        /// Logs a critical message.
        /// </summary>
        /// <param name="message">The message to log</param>
        void LogCritical(string message);

        /// <summary>
        /// Logs a critical message with structured properties.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="properties">Additional structured properties</param>
        void LogCritical(string message, IReadOnlyDictionary<string, object> properties);

        /// <summary>
        /// Logs a critical message with correlation ID and source context.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="correlationId">The correlation ID for tracking</param>
        /// <param name="sourceContext">The source context (typically class name)</param>
        void LogCritical(string message, string correlationId, string sourceContext = null);

        /// <summary>
        /// Logs an exception with context information.
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="context">Additional context information</param>
        void LogException(Exception exception, string context);

        /// <summary>
        /// Logs an exception with context and correlation ID.
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="context">Additional context information</param>
        /// <param name="correlationId">The correlation ID for tracking</param>
        /// <param name="sourceContext">The source context (typically class name)</param>
        void LogException(Exception exception, string context, string correlationId, string sourceContext = null);

        /// <summary>
        /// Logs a message with the specified level and channel.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="channel">The channel name</param>
        /// <param name="message">The message to log</param>
        /// <param name="exception">The associated exception, if any</param>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="properties">Additional structured properties</param>
        /// <param name="sourceContext">The source context</param>
        void Log(LogLevel level, string channel, string message, Exception exception = null, 
                string correlationId = null, IReadOnlyDictionary<string, object> properties = null, 
                string sourceContext = null);

        /// <summary>
        /// Registers a log target with the service.
        /// </summary>
        /// <param name="target">The log target to register</param>
        /// <exception cref="ArgumentNullException">Thrown when target is null</exception>
        void RegisterTarget(ILogTarget target);

        /// <summary>
        /// Unregisters a log target from the service.
        /// </summary>
        /// <param name="target">The log target to unregister</param>
        /// <returns>True if the target was unregistered, false if it was not found</returns>
        bool UnregisterTarget(ILogTarget target);

        /// <summary>
        /// Unregisters a log target by name.
        /// </summary>
        /// <param name="targetName">The name of the target to unregister</param>
        /// <returns>True if the target was unregistered, false if it was not found</returns>
        bool UnregisterTarget(string targetName);

        /// <summary>
        /// Gets all registered log targets.
        /// </summary>
        /// <returns>A read-only list of registered targets</returns>
        IReadOnlyList<ILogTarget> GetRegisteredTargets();

        /// <summary>
        /// Gets a registered log target by name.
        /// </summary>
        /// <param name="targetName">The name of the target to retrieve</param>
        /// <returns>The log target if found, null otherwise</returns>
        ILogTarget GetTarget(string targetName);

        /// <summary>
        /// Determines whether a log target is registered.
        /// </summary>
        /// <param name="targetName">The name of the target to check</param>
        /// <returns>True if the target is registered, false otherwise</returns>
        bool HasTarget(string targetName);

        /// <summary>
        /// Sets the minimum log level for all targets.
        /// </summary>
        /// <param name="minimumLevel">The minimum log level</param>
        void SetMinimumLevel(LogLevel minimumLevel);

        /// <summary>
        /// Sets the minimum log level for a specific target.
        /// </summary>
        /// <param name="targetName">The name of the target</param>
        /// <param name="minimumLevel">The minimum log level</param>
        /// <returns>True if the target was found and updated, false otherwise</returns>
        bool SetMinimumLevel(string targetName, LogLevel minimumLevel);

        /// <summary>
        /// Enables or disables all log targets.
        /// </summary>
        /// <param name="enabled">True to enable all targets, false to disable</param>
        void SetEnabled(bool enabled);

        /// <summary>
        /// Enables or disables a specific log target.
        /// </summary>
        /// <param name="targetName">The name of the target</param>
        /// <param name="enabled">True to enable the target, false to disable</param>
        /// <returns>True if the target was found and updated, false otherwise</returns>
        bool SetEnabled(string targetName, bool enabled);

        /// <summary>
        /// Forces all log targets to flush their buffers.
        /// </summary>
        void Flush();

        /// <summary>
        /// Forces a specific log target to flush its buffer.
        /// </summary>
        /// <param name="targetName">The name of the target to flush</param>
        /// <returns>True if the target was found and flushed, false otherwise</returns>
        bool Flush(string targetName);

        /// <summary>
        /// Performs a health check on all registered targets.
        /// </summary>
        /// <returns>True if all targets are healthy, false if any target is unhealthy</returns>
        bool PerformHealthCheck();

        /// <summary>
        /// Gets health status information for all registered targets.
        /// </summary>
        /// <returns>A dictionary containing health status for each target</returns>
        IReadOnlyDictionary<string, bool> GetHealthStatus();
    }
}