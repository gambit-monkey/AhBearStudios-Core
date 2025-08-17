using System;
using System.Collections.Generic;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Common.Models;
using FilterAction = AhBearStudios.Core.Common.Models.FilterAction;

namespace AhBearStudios.Core.Alerting.Factories
{
    /// <summary>
    /// Simple factory interface for creating alert filter instances.
    /// Follows CLAUDE.md guidelines - creation only, no validation or lifecycle management.
    /// Supports various filter types including severity, source, rate limiting, and custom filters.
    /// </summary>
    public interface IAlertFilterFactory
    {
        /// <summary>
        /// Creates a new alert filter instance by type.
        /// </summary>
        /// <param name="filterType">Type of filter to create</param>
        /// <param name="name">Name for the filter instance</param>
        /// <param name="priority">Priority for filter ordering</param>
        /// <returns>UniTask with created filter instance</returns>
        UniTask<IAlertFilter> CreateFilterAsync(FilterType filterType, FixedString64Bytes name, int priority = 100);

        /// <summary>
        /// Creates a new alert filter instance by type name.
        /// </summary>
        /// <param name="filterTypeName">Name of the filter type</param>
        /// <param name="name">Name for the filter instance</param>
        /// <param name="priority">Priority for filter ordering</param>
        /// <returns>UniTask with created filter instance</returns>
        UniTask<IAlertFilter> CreateFilterAsync(string filterTypeName, FixedString64Bytes name, int priority = 100);

        /// <summary>
        /// Creates and configures a new alert filter instance.
        /// </summary>
        /// <param name="configuration">Filter configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with created and configured filter instance</returns>
        UniTask<IAlertFilter> CreateAndConfigureFilterAsync(FilterConfiguration configuration, Guid correlationId = default);

        /// <summary>
        /// Creates a severity filter with specific configuration.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="minimumSeverity">Minimum severity level to allow</param>
        /// <param name="allowCriticalAlways">Whether to always allow critical alerts</param>
        /// <param name="priority">Filter priority</param>
        /// <returns>UniTask with configured severity filter</returns>
        UniTask<IAlertFilter> CreateSeverityFilterAsync(
            FixedString64Bytes name,
            AlertSeverity minimumSeverity,
            bool allowCriticalAlways = true,
            int priority = 10);

        /// <summary>
        /// Creates a source filter with specific configuration.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="allowedSources">Collection of allowed source patterns</param>
        /// <param name="useWhitelist">Whether to use whitelist (true) or blacklist (false) mode</param>
        /// <param name="priority">Filter priority</param>
        /// <returns>UniTask with configured source filter</returns>
        UniTask<IAlertFilter> CreateSourceFilterAsync(
            FixedString64Bytes name,
            IEnumerable<string> allowedSources,
            bool useWhitelist = true,
            int priority = 20);

        /// <summary>
        /// Creates a rate limiting filter with specific configuration.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="maxAlertsPerMinute">Maximum alerts allowed per minute</param>
        /// <param name="sourcePattern">Source pattern to match (supports wildcards)</param>
        /// <param name="priority">Filter priority</param>
        /// <returns>UniTask with configured rate limit filter</returns>
        UniTask<IAlertFilter> CreateRateLimitFilterAsync(
            FixedString64Bytes name,
            int maxAlertsPerMinute,
            string sourcePattern = "*",
            int priority = 30);

        /// <summary>
        /// Creates a content filter with specific configuration.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="patterns">Message patterns to match</param>
        /// <param name="action">Action to take on match</param>
        /// <param name="priority">Filter priority</param>
        /// <returns>UniTask with configured content filter</returns>
        UniTask<IAlertFilter> CreateContentFilterAsync(
            FixedString64Bytes name,
            IEnumerable<string> patterns,
            FilterAction action = FilterAction.Suppress,
            int priority = 40);

        /// <summary>
        /// Creates a time-based filter with specific configuration.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="allowedTimeRanges">Time ranges when alerts are allowed</param>
        /// <param name="timezone">Timezone for time comparisons</param>
        /// <param name="priority">Filter priority</param>
        /// <returns>UniTask with configured time-based filter</returns>
        UniTask<IAlertFilter> CreateTimeBasedFilterAsync(
            FixedString64Bytes name,
            IEnumerable<TimeRange> allowedTimeRanges,
            TimeZoneInfo timezone = null,
            int priority = 50);

        /// <summary>
        /// Creates a composite filter that combines multiple filters.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="childFilters">Child filters to combine</param>
        /// <param name="logicalOperator">How to combine filter results</param>
        /// <param name="priority">Filter priority</param>
        /// <returns>UniTask with configured composite filter</returns>
        UniTask<IAlertFilter> CreateCompositeFilterAsync(
            FixedString64Bytes name,
            IEnumerable<IAlertFilter> childFilters,
            LogicalOperator logicalOperator = LogicalOperator.And,
            int priority = 60);

        /// <summary>
        /// Creates multiple filters from a collection of configurations.
        /// </summary>
        /// <param name="configurations">Collection of filter configurations</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with collection of created filters</returns>
        UniTask<IEnumerable<IAlertFilter>> CreateFiltersAsync(
            IEnumerable<FilterConfiguration> configurations,
            Guid correlationId = default);

        /// <summary>
        /// Creates filters optimized for development environments.
        /// </summary>
        /// <returns>UniTask with collection of development filters</returns>
        UniTask<IEnumerable<IAlertFilter>> CreateDevelopmentFiltersAsync();

        /// <summary>
        /// Creates filters optimized for production environments.
        /// </summary>
        /// <returns>UniTask with collection of production filters</returns>
        UniTask<IEnumerable<IAlertFilter>> CreateProductionFiltersAsync();

        /// <summary>
        /// Creates filters optimized for testing scenarios.
        /// </summary>
        /// <returns>UniTask with collection of test filters</returns>
        UniTask<IEnumerable<IAlertFilter>> CreateTestFiltersAsync();


        /// <summary>
        /// Gets the default configuration for a specific filter type.
        /// </summary>
        /// <param name="filterType">Type of filter</param>
        /// <returns>Default configuration for the filter type</returns>
        FilterConfiguration GetDefaultConfiguration(FilterType filterType);

        /// <summary>
        /// Gets all supported filter types.
        /// </summary>
        /// <returns>Collection of supported filter types</returns>
        IEnumerable<FilterType> GetSupportedFilterTypes();

        /// <summary>
        /// Checks if a filter type is supported by this factory.
        /// </summary>
        /// <param name="filterType">Filter type to check</param>
        /// <returns>True if supported, false otherwise</returns>
        bool IsFilterTypeSupported(FilterType filterType);

        /// <summary>
        /// Creates configuration from a dictionary of settings.
        /// </summary>
        /// <param name="filterType">Type of filter</param>
        /// <param name="name">Filter name</param>
        /// <param name="settings">Configuration settings</param>
        /// <returns>Filter configuration</returns>
        FilterConfiguration CreateConfigurationFromSettings(FilterType filterType, FixedString64Bytes name, Dictionary<string, object> settings);
    }
}