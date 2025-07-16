using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Services
{
    /// <summary>
    /// Interface for log message formatting services.
    /// Provides standardized log formatting capabilities with support for custom formatters and structured logging.
    /// </summary>
    public interface ILogFormattingService
    {
        /// <summary>
        /// Gets the default message format template.
        /// </summary>
        string DefaultMessageFormat { get; }

        /// <summary>
        /// Gets the timestamp format string.
        /// </summary>
        string TimestampFormat { get; }

        /// <summary>
        /// Gets whether high-performance mode is enabled.
        /// </summary>
        bool HighPerformanceMode { get; }

        /// <summary>
        /// Gets whether caching is enabled for formatted messages.
        /// </summary>
        bool CachingEnabled { get; }

        /// <summary>
        /// Gets the maximum cache size for formatted messages.
        /// </summary>
        int MaxCacheSize { get; }

        /// <summary>
        /// Gets formatting performance metrics.
        /// </summary>
        FormattingMetrics Metrics { get; }

        /// <summary>
        /// Formats a log message using the default format template.
        /// </summary>
        /// <param name="logMessage">The log message to format</param>
        /// <returns>The formatted message string</returns>
        string FormatMessage(in LogMessage logMessage);

        /// <summary>
        /// Formats a log message using the specified format template.
        /// </summary>
        /// <param name="logMessage">The log message to format</param>
        /// <param name="format">The format template to use</param>
        /// <returns>The formatted message string</returns>
        string FormatMessage(in LogMessage logMessage, string format);

        /// <summary>
        /// Formats multiple log messages efficiently.
        /// </summary>
        /// <param name="logMessages">The log messages to format</param>
        /// <param name="format">The format template to use</param>
        /// <returns>An array of formatted message strings</returns>
        string[] FormatMessages(IReadOnlyList<LogMessage> logMessages, string format = null);

        /// <summary>
        /// Formats a log message for structured logging output.
        /// </summary>
        /// <param name="logMessage">The log message to format</param>
        /// <returns>A dictionary containing structured log data</returns>
        Dictionary<string, object> FormatStructured(in LogMessage logMessage);

        /// <summary>
        /// Registers a custom formatter for a specific placeholder.
        /// </summary>
        /// <param name="placeholder">The placeholder name (without braces)</param>
        /// <param name="formatter">The custom formatter</param>
        void RegisterFormatter(string placeholder, ILogFormatter formatter);

        /// <summary>
        /// Unregisters a custom formatter.
        /// </summary>
        /// <param name="placeholder">The placeholder name to unregister</param>
        /// <returns>True if the formatter was removed, false if it wasn't found</returns>
        bool UnregisterFormatter(string placeholder);

        /// <summary>
        /// Gets all registered custom formatters.
        /// </summary>
        /// <returns>A dictionary of placeholder names and their formatters</returns>
        IReadOnlyDictionary<string, ILogFormatter> GetRegisteredFormatters();

        /// <summary>
        /// Clears the formatting cache.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Gets the current formatting performance metrics.
        /// </summary>
        /// <returns>A snapshot of current metrics</returns>
        FormattingMetrics GetMetrics();

        /// <summary>
        /// Resets the formatting performance metrics.
        /// </summary>
        void ResetMetrics();
    }
}