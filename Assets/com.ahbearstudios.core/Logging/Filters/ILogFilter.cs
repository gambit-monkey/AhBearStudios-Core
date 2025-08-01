using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Logging.Filters
{
    /// <summary>
    /// Interface for log filters that determine whether log entries should be processed.
    /// Provides standardized filtering capabilities with correlation support and performance monitoring.
    /// Follows the AhBearStudios Core Architecture filter patterns for extensibility and consistency.
    /// </summary>
    public interface ILogFilter
    {
        /// <summary>
        /// Gets the filter name for identification.
        /// </summary>
        FixedString64Bytes Name { get; }

        /// <summary>
        /// Gets or sets whether the filter is currently enabled.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets the priority for filter execution order.
        /// Higher values indicate higher priority (execute earlier).
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Determines if a log entry should be processed based on filter criteria.
        /// This is the core filtering logic that implementations must provide.
        /// </summary>
        /// <param name="entry">The log entry to evaluate</param>
        /// <param name="correlationId">Correlation ID for tracking filter operations</param>
        /// <returns>True if the entry should be processed, false if it should be filtered out</returns>
        bool ShouldProcess(LogEntry entry, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Validates the filter configuration and state.
        /// Used during filter setup and health checks.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking validation operations</param>
        /// <returns>Validation result indicating configuration validity</returns>
        Common.Models.ValidationResult Validate(FixedString64Bytes correlationId = default);

        /// <summary>
        /// Gets filter performance and operation statistics.
        /// Used for monitoring filter performance and effectiveness.
        /// </summary>
        /// <returns>Filter performance statistics</returns>
        FilterStatistics GetStatistics();

        /// <summary>
        /// Resets the filter's internal state and statistics.
        /// Used for maintenance and testing purposes.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking reset operations</param>
        void Reset(FixedString64Bytes correlationId = default);

        /// <summary>
        /// Configures the filter with specific settings.
        /// Allows runtime configuration changes without recreating the filter.
        /// </summary>
        /// <param name="settings">Filter-specific configuration settings</param>
        /// <param name="correlationId">Correlation ID for tracking configuration operations</param>
        void Configure(IReadOnlyDictionary<FixedString32Bytes, object> settings, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Gets the current configuration settings.
        /// Used for diagnostics and configuration inspection.
        /// </summary>
        /// <returns>Current filter settings</returns>
        IReadOnlyDictionary<FixedString32Bytes, object> GetSettings();
    }
}