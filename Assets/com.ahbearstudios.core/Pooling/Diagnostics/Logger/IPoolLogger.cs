using System;
using System.Collections.Generic;

namespace AhBearStudios.Pooling.Diagnostics
{
    /// <summary>
    /// Interface that provides logging and diagnostic features for pool operations.
    /// </summary>
    public interface IPoolLogger
    {
        /// <summary>
        /// Gets or sets the log level for this logger
        /// </summary>
        PoolLogger.LogLevel Level { get; set; }

        /// <summary>
        /// Gets or sets the log target for this logger
        /// </summary>
        PoolLogger.LogTarget Target { get; set; }

        /// <summary>
        /// Gets or sets whether to include timestamps in log entries
        /// </summary>
        bool IncludeTimestamps { get; set; }

        /// <summary>
        /// Gets or sets whether to include stack traces for error log entries
        /// </summary>
        bool IncludeStackTraceForErrors { get; set; }

        /// <summary>
        /// Logs an error message from an instance
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object</param>
        void LogErrorInstance(string message, UnityEngine.Object context = null);

        /// <summary>
        /// Logs a warning message from an instance
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object</param>
        void LogWarningInstance(string message, UnityEngine.Object context = null);

        /// <summary>
        /// Logs an info message from an instance
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object</param>
        void LogInfoInstance(string message, UnityEngine.Object context = null);

        /// <summary>
        /// Logs a debug message from an instance
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object</param>
        void LogDebugInstance(string message, UnityEngine.Object context = null);

        /// <summary>
        /// Logs a verbose message from an instance
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object</param>
        void LogVerboseInstance(string message, UnityEngine.Object context = null);

        /// <summary>
        /// Logs pool metrics from an instance
        /// </summary>
        /// <param name="metrics">Dictionary of metrics</param>
        void LogMetrics(Dictionary<string, object> metrics);

        /// <summary>
        /// Logs an exception from an instance
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="context">Optional context object</param>
        void LogException(Exception exception, UnityEngine.Object context = null);

        /// <summary>
        /// Logs details about a pool operation from an instance
        /// </summary>
        /// <param name="operation">Name of the operation</param>
        /// <param name="details">Details about the operation</param>
        /// <param name="level">Log level for this entry</param>
        void LogOperation(string operation, string details, PoolLogger.LogLevel level = PoolLogger.LogLevel.Debug);
    }
}