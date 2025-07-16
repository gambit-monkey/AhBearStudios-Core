using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Filters
{
    /// <summary>
    /// Factory interface for creating log filter instances.
    /// Provides standardized filter creation with support for configuration-based instantiation.
    /// Follows the AhBearStudios Core factory pattern for consistent filter management.
    /// </summary>
    public interface ILogFilterFactory
    {
        /// <summary>
        /// Creates a filter instance from a FilterConfig.
        /// </summary>
        /// <param name="config">The filter configuration</param>
        /// <returns>A configured filter instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        /// <exception cref="ArgumentException">Thrown when config has invalid values</exception>
        /// <exception cref="NotSupportedException">Thrown when filter type is not supported</exception>
        ILogFilter CreateFilter(FilterConfig config);

        /// <summary>
        /// Creates multiple filter instances from FilterConfigs.
        /// </summary>
        /// <param name="configs">The filter configurations</param>
        /// <returns>A collection of configured filter instances</returns>
        /// <exception cref="ArgumentNullException">Thrown when configs is null</exception>
        IReadOnlyList<ILogFilter> CreateFilters(IEnumerable<FilterConfig> configs);

        /// <summary>
        /// Creates a filter instance by type name with settings.
        /// </summary>
        /// <param name="filterType">The filter type name</param>
        /// <param name="name">The filter name</param>
        /// <param name="settings">The filter settings</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured filter instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when filterType or name is null</exception>
        /// <exception cref="NotSupportedException">Thrown when filter type is not supported</exception>
        ILogFilter CreateFilter(
            string filterType, 
            string name, 
            IReadOnlyDictionary<FixedString32Bytes, object> settings = null, 
            int priority = 0);

        /// <summary>
        /// Gets all supported filter types.
        /// </summary>
        /// <returns>A collection of supported filter type names</returns>
        IReadOnlyList<string> GetSupportedFilterTypes();

        /// <summary>
        /// Checks if a filter type is supported.
        /// </summary>
        /// <param name="filterType">The filter type name</param>
        /// <returns>True if the filter type is supported</returns>
        bool IsFilterTypeSupported(string filterType);

        /// <summary>
        /// Validates a filter configuration.
        /// </summary>
        /// <param name="config">The filter configuration to validate</param>
        /// <returns>A validation result</returns>
        ValidationResult ValidateFilterConfig(FilterConfig config);

        /// <summary>
        /// Gets the default configuration for a filter type.
        /// </summary>
        /// <param name="filterType">The filter type name</param>
        /// <param name="filterName">The filter name</param>
        /// <returns>A default FilterConfig instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when filterType or filterName is null</exception>
        /// <exception cref="NotSupportedException">Thrown when filter type is not supported</exception>
        FilterConfig GetDefaultConfig(string filterType, string filterName);

        /// <summary>
        /// Creates a LogFilterService with filters from configurations.
        /// </summary>
        /// <param name="configs">The filter configurations</param>
        /// <returns>A configured LogFilterService instance</returns>
        LogFilterService CreateFilterService(IEnumerable<FilterConfig> configs = null);
    }
}