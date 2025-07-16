using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Formatters
{
    /// <summary>
    /// Interface for log message formatting with correlation support.
    /// Provides standardized formatting capabilities for different output targets.
    /// 
    /// Architecture Note: Performance data (CPU usage, memory usage, processing time) should be 
    /// obtained through optional IProfilerService injection rather than being embedded in LogEntry.
    /// This maintains separation of concerns between core logging data and performance metrics.
    /// 
    /// Implementation Guide:
    /// - Accept IProfilerService as optional constructor parameter
    /// - Query performance data using profiler service during formatting
    /// - Gracefully handle cases where profiler service is not available
    /// - Use ProfilerTag for consistent performance metric identification
    /// </summary>
    public interface ILogFormatter
    {
        /// <summary>
        /// Gets the formatter name for identification.
        /// </summary>
        FixedString64Bytes Name { get; }

        /// <summary>
        /// Gets the supported output format.
        /// </summary>
        LogFormat LogFormat { get; }

        /// <summary>
        /// Gets whether this formatter is enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Formats a log entry for output.
        /// </summary>
        /// <param name="entry">Log entry to format</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Formatted log message</returns>
        string Format(LogEntry entry, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Formats multiple log entries for batch output.
        /// </summary>
        /// <param name="entries">Log entries to format</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Formatted log messages</returns>
        IEnumerable<string> FormatBatch(IReadOnlyCollection<LogEntry> entries, 
            FixedString64Bytes correlationId = default);

        /// <summary>
        /// Validates the formatter configuration.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Validation result</returns>
        ValidationResult Validate(FixedString64Bytes correlationId = default);

        /// <summary>
        /// Configures the formatter with specific settings.
        /// </summary>
        /// <param name="settings">Formatter-specific configuration settings</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void Configure(IReadOnlyDictionary<FixedString32Bytes, object> settings, 
            FixedString64Bytes correlationId = default);

        /// <summary>
        /// Gets the current configuration settings.
        /// </summary>
        /// <returns>Current formatter settings</returns>
        IReadOnlyDictionary<FixedString32Bytes, object> GetSettings();

        /// <summary>
        /// Determines if the formatter can handle the specified log format.
        /// </summary>
        /// <param name="format">The log format to check</param>
        /// <returns>True if the formatter can handle the format</returns>
        bool CanHandle(LogFormat format);
    }
}