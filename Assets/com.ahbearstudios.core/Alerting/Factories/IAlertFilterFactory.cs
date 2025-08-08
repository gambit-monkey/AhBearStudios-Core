using System;
using System.Collections.Generic;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.Alerting.Factories
{
    /// <summary>
    /// Factory interface for creating and configuring alert filter instances.
    /// Provides abstraction for dependency injection and testing scenarios.
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
        /// Validates a filter configuration before creation.
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateFilterConfiguration(FilterConfiguration configuration);

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

    /// <summary>
    /// Enumeration of supported filter types.
    /// </summary>
    public enum FilterType
    {
        /// <summary>
        /// Filter based on alert severity level.
        /// </summary>
        Severity = 0,

        /// <summary>
        /// Filter based on alert source.
        /// </summary>
        Source = 1,

        /// <summary>
        /// Filter based on rate limiting.
        /// </summary>
        RateLimit = 2,

        /// <summary>
        /// Filter based on alert content/message.
        /// </summary>
        Content = 3,

        /// <summary>
        /// Filter based on time of day/week.
        /// </summary>
        TimeBased = 4,

        /// <summary>
        /// Filter that combines multiple child filters.
        /// </summary>
        Composite = 5,

        /// <summary>
        /// Filter based on alert tags.
        /// </summary>
        Tag = 6,

        /// <summary>
        /// Filter based on correlation IDs.
        /// </summary>
        Correlation = 7,

        /// <summary>
        /// Filter that allows all alerts (pass-through).
        /// </summary>
        PassThrough = 8,

        /// <summary>
        /// Filter that blocks all alerts.
        /// </summary>
        Block = 9
    }

    /// <summary>
    /// Action to take when a filter matches an alert.
    /// </summary>
    public enum FilterAction
    {
        /// <summary>
        /// Allow the alert to pass through.
        /// </summary>
        Allow = 0,

        /// <summary>
        /// Suppress the alert (block it).
        /// </summary>
        Suppress = 1,

        /// <summary>
        /// Modify the alert and pass it through.
        /// </summary>
        Modify = 2,

        /// <summary>
        /// Defer the alert for later processing.
        /// </summary>
        Defer = 3
    }

    /// <summary>
    /// Logical operator for combining filter results.
    /// </summary>
    public enum LogicalOperator
    {
        /// <summary>
        /// All filters must pass (logical AND).
        /// </summary>
        And = 0,

        /// <summary>
        /// At least one filter must pass (logical OR).
        /// </summary>
        Or = 1,

        /// <summary>
        /// Exactly one filter must pass (logical XOR).
        /// </summary>
        Xor = 2,

        /// <summary>
        /// No filters must pass (logical NOT).
        /// </summary>
        Not = 3
    }

    /// <summary>
    /// Time range for time-based filtering.
    /// </summary>
    public readonly record struct TimeRange
    {
        /// <summary>
        /// Start time of the range.
        /// </summary>
        public TimeOnly StartTime { get; init; }

        /// <summary>
        /// End time of the range.
        /// </summary>
        public TimeOnly EndTime { get; init; }

        /// <summary>
        /// Days of the week this range applies to.
        /// </summary>
        public DayOfWeek[] DaysOfWeek { get; init; }

        /// <summary>
        /// Whether this range spans midnight.
        /// </summary>
        public bool SpansMidnight => EndTime < StartTime;

        /// <summary>
        /// Creates a new time range.
        /// </summary>
        /// <param name="startTime">Start time</param>
        /// <param name="endTime">End time</param>
        /// <param name="daysOfWeek">Applicable days of week</param>
        public TimeRange(TimeOnly startTime, TimeOnly endTime, params DayOfWeek[] daysOfWeek)
        {
            StartTime = startTime;
            EndTime = endTime;
            DaysOfWeek = daysOfWeek ?? Array.Empty<DayOfWeek>();
        }

        /// <summary>
        /// Creates a 24/7 time range (always active).
        /// </summary>
        /// <returns>Always-active time range</returns>
        public static TimeRange Always()
        {
            return new TimeRange(
                TimeOnly.MinValue,
                TimeOnly.MaxValue,
                Enum.GetValues<DayOfWeek>());
        }

        /// <summary>
        /// Creates a business hours time range (9 AM to 5 PM, Monday to Friday).
        /// </summary>
        /// <returns>Business hours time range</returns>
        public static TimeRange BusinessHours()
        {
            return new TimeRange(
                new TimeOnly(9, 0),
                new TimeOnly(17, 0),
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday);
        }

        /// <summary>
        /// Creates a weekend time range.
        /// </summary>
        /// <returns>Weekend time range</returns>
        public static TimeRange Weekend()
        {
            return new TimeRange(
                TimeOnly.MinValue,
                TimeOnly.MaxValue,
                DayOfWeek.Saturday,
                DayOfWeek.Sunday);
        }

        /// <summary>
        /// Checks if a given DateTime falls within this time range.
        /// </summary>
        /// <param name="dateTime">DateTime to check</param>
        /// <returns>True if within range, false otherwise</returns>
        public bool Contains(DateTime dateTime)
        {
            var timeOnly = TimeOnly.FromDateTime(dateTime);
            var dayOfWeek = dateTime.DayOfWeek;

            // Check if day matches
            if (DaysOfWeek.Length > 0 && !DaysOfWeek.Contains(dayOfWeek))
                return false;

            // Check if time matches
            if (SpansMidnight)
            {
                return timeOnly >= StartTime || timeOnly <= EndTime;
            }
            else
            {
                return timeOnly >= StartTime && timeOnly <= EndTime;
            }
        }
    }

    /// <summary>
    /// Extended configuration class for filter creation with additional factory-specific settings.
    /// </summary>
    public sealed class ExtendedFilterConfiguration : FilterConfiguration
    {
        /// <summary>
        /// Gets or sets the filter type enum.
        /// </summary>
        public FilterType FilterType { get; set; }

        /// <summary>
        /// Gets or sets whether to auto-enable the filter after creation.
        /// </summary>
        public bool AutoEnable { get; set; } = true;

        /// <summary>
        /// Gets or sets performance monitoring settings.
        /// </summary>
        public FilterPerformanceSettings PerformanceSettings { get; set; } = new FilterPerformanceSettings();

        /// <summary>
        /// Gets or sets error handling settings.
        /// </summary>
        public FilterErrorHandling ErrorHandling { get; set; } = new FilterErrorHandling();

        /// <summary>
        /// Gets or sets additional initialization parameters.
        /// </summary>
        public Dictionary<string, object> InitializationParameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates an extended configuration from a base configuration.
        /// </summary>
        /// <param name="baseConfig">Base filter configuration</param>
        /// <param name="filterType">Filter type</param>
        /// <returns>Extended filter configuration</returns>
        public static ExtendedFilterConfiguration FromBase(FilterConfiguration baseConfig, FilterType filterType)
        {
            return new ExtendedFilterConfiguration
            {
                Type = baseConfig.Type,
                Name = baseConfig.Name,
                IsEnabled = baseConfig.IsEnabled,
                Priority = baseConfig.Priority,
                Settings = new Dictionary<string, object>(baseConfig.Settings),
                FilterType = filterType
            };
        }

        /// <summary>
        /// Creates default configuration for a filter type.
        /// </summary>
        /// <param name="filterType">Filter type</param>
        /// <param name="name">Filter name</param>
        /// <param name="priority">Filter priority</param>
        /// <returns>Default extended configuration</returns>
        public static ExtendedFilterConfiguration Default(FilterType filterType, FixedString64Bytes name, int priority = 100)
        {
            var config = new ExtendedFilterConfiguration
            {
                Type = filterType.ToString(),
                Name = name,
                FilterType = filterType,
                IsEnabled = true,
                Priority = priority
            };

            // Set type-specific defaults
            switch (filterType)
            {
                case FilterType.Severity:
                    config.Settings = new Dictionary<string, object>
                    {
                        ["MinimumSeverity"] = AlertSeverity.Information,
                        ["AllowCriticalAlways"] = true
                    };
                    break;

                case FilterType.Source:
                    config.Settings = new Dictionary<string, object>
                    {
                        ["AllowedSources"] = new List<string> { "*" },
                        ["UseWhitelist"] = true,
                        ["CaseSensitive"] = false
                    };
                    break;

                case FilterType.RateLimit:
                    config.Settings = new Dictionary<string, object>
                    {
                        ["MaxAlertsPerMinute"] = 60,
                        ["SourcePattern"] = "*",
                        ["WindowSize"] = 60
                    };
                    break;

                case FilterType.Content:
                    config.Settings = new Dictionary<string, object>
                    {
                        ["Patterns"] = new List<string>(),
                        ["Action"] = FilterAction.Suppress,
                        ["CaseSensitive"] = false,
                        ["UseRegex"] = false
                    };
                    break;

                case FilterType.TimeBased:
                    config.Settings = new Dictionary<string, object>
                    {
                        ["TimeRanges"] = new[] { TimeRange.Always() },
                        ["Timezone"] = "UTC"
                    };
                    break;

                case FilterType.Tag:
                    config.Settings = new Dictionary<string, object>
                    {
                        ["RequiredTags"] = new List<string>(),
                        ["ForbiddenTags"] = new List<string>(),
                        ["MatchMode"] = "Any"
                    };
                    break;

                case FilterType.PassThrough:
                case FilterType.Block:
                    config.Settings = new Dictionary<string, object>();
                    break;
            }

            return config;
        }
    }

    /// <summary>
    /// Performance settings for filters.
    /// </summary>
    public sealed class FilterPerformanceSettings
    {
        /// <summary>
        /// Maximum processing time before alert is raised.
        /// </summary>
        public TimeSpan MaxProcessingTime { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Whether to enable performance monitoring.
        /// </summary>
        public bool EnableMonitoring { get; set; } = true;

        /// <summary>
        /// Sample rate for performance tracking (0.0 to 1.0).
        /// </summary>
        public double SampleRate { get; set; } = 0.1;
    }

    /// <summary>
    /// Error handling settings for filters.
    /// </summary>
    public sealed class FilterErrorHandling
    {
        /// <summary>
        /// How to handle filter errors.
        /// </summary>
        public ErrorHandlingMode ErrorMode { get; set; } = ErrorHandlingMode.LogAndContinue;

        /// <summary>
        /// Maximum consecutive errors before disabling filter.
        /// </summary>
        public int MaxConsecutiveErrors { get; set; } = 5;

        /// <summary>
        /// Whether to retry failed operations.
        /// </summary>
        public bool EnableRetry { get; set; } = true;

        /// <summary>
        /// Retry delay for failed operations.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// Error handling modes for filters.
    /// </summary>
    public enum ErrorHandlingMode
    {
        /// <summary>
        /// Allow all alerts on error.
        /// </summary>
        AllowOnError = 0,

        /// <summary>
        /// Suppress all alerts on error.
        /// </summary>
        SuppressOnError = 1,

        /// <summary>
        /// Log error and continue processing.
        /// </summary>
        LogAndContinue = 2,

        /// <summary>
        /// Disable filter on error.
        /// </summary>
        DisableOnError = 3
    }

    /// <summary>
    /// Result of filter creation operation.
    /// </summary>
    public sealed class FilterCreationResult
    {
        /// <summary>
        /// Gets or sets whether the creation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the created filter instance.
        /// </summary>
        public IAlertFilter Filter { get; set; }

        /// <summary>
        /// Gets or sets any error that occurred during creation.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the configuration used for creation.
        /// </summary>
        public FilterConfiguration Configuration { get; set; }

        /// <summary>
        /// Gets or sets the time taken for creation.
        /// </summary>
        public TimeSpan CreationTime { get; set; }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <param name="filter">Created filter</param>
        /// <param name="configuration">Configuration used</param>
        /// <param name="creationTime">Time taken</param>
        /// <returns>Successful creation result</returns>
        public static FilterCreationResult Success(IAlertFilter filter, FilterConfiguration configuration, TimeSpan creationTime)
        {
            return new FilterCreationResult
            {
                Success = true,
                Filter = filter,
                Configuration = configuration,
                CreationTime = creationTime
            };
        }

        /// <summary>
        /// Creates a failure result.
        /// </summary>
        /// <param name="error">Error message</param>
        /// <param name="configuration">Configuration that failed</param>
        /// <param name="creationTime">Time taken before failure</param>
        /// <returns>Failed creation result</returns>
        public static FilterCreationResult Failure(string error, FilterConfiguration configuration = null, TimeSpan creationTime = default)
        {
            return new FilterCreationResult
            {
                Success = false,
                Error = error,
                Configuration = configuration,
                CreationTime = creationTime
            };
        }
    }
}