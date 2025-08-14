using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Builders
{
    /// <summary>
    /// Builder interface for fluent filter configuration.
    /// Integrates with AlertConfigBuilder for comprehensive alert setup.
    /// Supports all filter types with validation and priority management.
    /// </summary>
    public interface IFilterConfigBuilder
    {
        #region Specific Filter Methods

        /// <summary>
        /// Adds a severity filter that filters alerts based on severity levels.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="minimumSeverity">Minimum severity level to allow</param>
        /// <param name="allowCriticalAlways">Whether to always allow critical alerts</param>
        /// <param name="priority">Filter priority (lower = higher priority)</param>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder AddSeverityFilter(
            string name, 
            AlertSeverity minimumSeverity, 
            bool allowCriticalAlways = true, 
            int priority = 10);

        /// <summary>
        /// Adds a rate limiting filter that limits alerts per time window.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="maxAlertsPerMinute">Maximum alerts allowed per minute</param>
        /// <param name="sourcePattern">Source pattern to match (supports wildcards)</param>
        /// <param name="burstSize">Burst size allowed above normal rate</param>
        /// <param name="priority">Filter priority (lower = higher priority)</param>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder AddRateLimitFilter(
            string name, 
            int maxAlertsPerMinute, 
            string sourcePattern = "*", 
            int burstSize = 10, 
            int priority = 30);

        /// <summary>
        /// Adds a source filter that filters alerts based on source patterns.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="sources">Source patterns to match</param>
        /// <param name="useWhitelist">Whether to use whitelist (true) or blacklist (false) mode</param>
        /// <param name="caseSensitive">Whether matching is case-sensitive</param>
        /// <param name="useRegex">Whether to use regex pattern matching</param>
        /// <param name="priority">Filter priority (lower = higher priority)</param>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder AddSourceFilter(
            string name, 
            IEnumerable<string> sources, 
            bool useWhitelist = true, 
            bool caseSensitive = false, 
            bool useRegex = false, 
            int priority = 20);

        /// <summary>
        /// Adds a content filter that filters alerts based on message content.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="patterns">Content patterns to match</param>
        /// <param name="action">Action to take when pattern matches</param>
        /// <param name="caseSensitive">Whether matching is case-sensitive</param>
        /// <param name="useRegex">Whether to use regex pattern matching</param>
        /// <param name="priority">Filter priority (lower = higher priority)</param>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder AddContentFilter(
            string name, 
            IEnumerable<string> patterns, 
            FilterAction action = FilterAction.Suppress, 
            bool caseSensitive = false, 
            bool useRegex = false, 
            int priority = 40);

        /// <summary>
        /// Adds a time-based filter that filters alerts based on time ranges.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="timeRanges">Time ranges when alerts are allowed</param>
        /// <param name="timezone">Timezone for time comparisons</param>
        /// <param name="priority">Filter priority (lower = higher priority)</param>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder AddTimeBasedFilter(
            string name, 
            IEnumerable<TimeRange> timeRanges, 
            TimeZoneInfo timezone = null, 
            int priority = 50);

        /// <summary>
        /// Adds a composite filter that combines multiple child filters.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="childBuilder">Action to configure child filters</param>
        /// <param name="logicalOperator">How to combine filter results (And/Or)</param>
        /// <param name="priority">Filter priority (lower = higher priority)</param>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder AddCompositeFilter(
            string name, 
            Action<IFilterConfigBuilder> childBuilder, 
            LogicalOperator logicalOperator = LogicalOperator.And, 
            int priority = 60);

        /// <summary>
        /// Adds a tag-based filter that filters alerts based on tags.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="requiredTags">Tags that must be present</param>
        /// <param name="excludedTags">Tags that must not be present</param>
        /// <param name="requireAllTags">Whether all required tags must be present</param>
        /// <param name="priority">Filter priority (lower = higher priority)</param>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder AddTagFilter(
            string name, 
            IEnumerable<string> requiredTags = null, 
            IEnumerable<string> excludedTags = null, 
            bool requireAllTags = false, 
            int priority = 25);

        /// <summary>
        /// Adds a correlation filter that filters based on correlation patterns.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="correlationPatterns">Correlation ID patterns to match</param>
        /// <param name="timeWindow">Time window for correlation tracking</param>
        /// <param name="priority">Filter priority (lower = higher priority)</param>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder AddCorrelationFilter(
            string name, 
            IEnumerable<string> correlationPatterns, 
            TimeSpan timeWindow = default, 
            int priority = 35);

        /// <summary>
        /// Adds a pass-through filter that allows all alerts.
        /// Useful for testing or as a default filter.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="priority">Filter priority (lower = higher priority)</param>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder AddPassThroughFilter(
            string name, 
            int priority = 100);

        /// <summary>
        /// Adds a block filter that suppresses all alerts.
        /// Useful for emergency situations or maintenance.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="reason">Reason for blocking</param>
        /// <param name="priority">Filter priority (lower = higher priority)</param>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder AddBlockFilter(
            string name, 
            string reason = "Blocked by configuration", 
            int priority = 1);

        #endregion

        #region Generic Filter Configuration

        /// <summary>
        /// Adds a filter using a pre-built FilterConfiguration.
        /// </summary>
        /// <param name="filterConfiguration">Complete filter configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder AddFilter(FilterConfiguration filterConfiguration);

        /// <summary>
        /// Adds a custom filter with specific type and settings.
        /// </summary>
        /// <param name="name">Filter name</param>
        /// <param name="filterType">Filter type</param>
        /// <param name="settings">Filter-specific settings</param>
        /// <param name="priority">Filter priority (lower = higher priority)</param>
        /// <param name="enabled">Whether filter is enabled</param>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder AddCustomFilter(
            string name, 
            FilterType filterType, 
            Dictionary<string, object> settings, 
            int priority = 50, 
            bool enabled = true);

        #endregion

        #region Filter Management

        /// <summary>
        /// Removes a filter by name.
        /// </summary>
        /// <param name="name">Name of filter to remove</param>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder RemoveFilter(string name);

        /// <summary>
        /// Removes all filters of a specific type.
        /// </summary>
        /// <param name="filterType">Type of filters to remove</param>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder RemoveFiltersOfType(FilterType filterType);

        /// <summary>
        /// Clears all configured filters.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder ClearFilters();

        /// <summary>
        /// Sets the priority of an existing filter.
        /// </summary>
        /// <param name="name">Name of filter to modify</param>
        /// <param name="priority">New priority value</param>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder SetFilterPriority(string name, int priority);

        /// <summary>
        /// Enables or disables a filter by name.
        /// </summary>
        /// <param name="name">Name of filter to modify</param>
        /// <param name="enabled">Whether filter should be enabled</param>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder SetFilterEnabled(string name, bool enabled);

        #endregion

        #region Environment Presets

        /// <summary>
        /// Configures filters optimized for development environments.
        /// Includes debug-level filtering with generous rate limits.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder ForDevelopment();

        /// <summary>
        /// Configures filters optimized for production environments.
        /// Includes warning-level filtering with conservative rate limits.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder ForProduction();

        /// <summary>
        /// Configures filters optimized for testing scenarios.
        /// Includes pass-through filters for comprehensive testing.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder ForTesting();

        /// <summary>
        /// Configures emergency filters that suppress most alerts.
        /// Only critical and emergency alerts pass through.
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        IFilterConfigBuilder ForEmergency();

        #endregion

        #region Validation and Build

        /// <summary>
        /// Validates all configured filters without building.
        /// </summary>
        /// <returns>Validation results for all filters</returns>
        Dictionary<string, FilterValidationResult> ValidateFilters();

        /// <summary>
        /// Gets the current filter count.
        /// </summary>
        /// <returns>Number of configured filters</returns>
        int GetFilterCount();

        /// <summary>
        /// Gets the names of all configured filters.
        /// </summary>
        /// <returns>List of filter names</returns>
        IReadOnlyList<string> GetFilterNames();

        /// <summary>
        /// Checks if a filter with the specified name exists.
        /// </summary>
        /// <param name="name">Filter name to check</param>
        /// <returns>True if filter exists</returns>
        bool HasFilter(string name);

        /// <summary>
        /// Builds the final list of filter configurations.
        /// Sorts filters by priority and validates all configurations.
        /// </summary>
        /// <returns>Immutable list of filter configurations</returns>
        IReadOnlyList<FilterConfiguration> Build();

        #endregion
    }

    /// <summary>
    /// Filter validation result containing validation status and messages.
    /// </summary>
    public readonly record struct FilterValidationResult
    {
        /// <summary>
        /// Gets whether the filter configuration is valid.
        /// </summary>
        public bool IsValid { get; init; }

        /// <summary>
        /// Gets validation error messages.
        /// </summary>
        public IReadOnlyList<string> Errors { get; init; }

        /// <summary>
        /// Gets validation warning messages.
        /// </summary>
        public IReadOnlyList<string> Warnings { get; init; }

        /// <summary>
        /// Creates a valid result.
        /// </summary>
        public static FilterValidationResult Valid()
        {
            return new FilterValidationResult
            {
                IsValid = true,
                Errors = Array.Empty<string>(),
                Warnings = Array.Empty<string>()
            };
        }

        /// <summary>
        /// Creates an invalid result with errors.
        /// </summary>
        /// <param name="errors">Validation errors</param>
        /// <param name="warnings">Optional warnings</param>
        public static FilterValidationResult Invalid(IReadOnlyList<string> errors, IReadOnlyList<string> warnings = null)
        {
            return new FilterValidationResult
            {
                IsValid = false,
                Errors = errors ?? Array.Empty<string>(),
                Warnings = warnings ?? Array.Empty<string>()
            };
        }
    }
}