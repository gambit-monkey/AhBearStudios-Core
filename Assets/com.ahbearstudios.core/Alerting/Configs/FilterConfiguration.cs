using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Alerting.Configs
{
    /// <summary>
    /// Configuration class for individual alert filters.
    /// Defines filter-specific settings including type, priority, conditions, and performance parameters.
    /// Supports multiple filter types: Severity, Source, RateLimit, Content, TimeBased, Composite, Tag, Correlation, PassThrough, Block.
    /// </summary>
    public record FilterConfiguration
    {
        /// <summary>
        /// Gets the unique name identifier for this filter.
        /// Must be unique across all configured filters in the alert system.
        /// </summary>
        public FixedString64Bytes Name { get; init; }

        /// <summary>
        /// Gets the filter type that determines the implementation to use.
        /// </summary>
        public FilterType FilterType { get; init; }

        /// <summary>
        /// Gets the string representation of the filter type for serialization compatibility.
        /// </summary>
        public string Type { get; init; }

        /// <summary>
        /// Gets whether this filter is enabled for alert processing.
        /// Disabled filters are skipped during alert evaluation but remain configured.
        /// </summary>
        public bool IsEnabled { get; init; } = true;

        /// <summary>
        /// Gets the priority level for this filter during alert processing.
        /// Lower priority values are processed first. Range: 1 (highest) to 1000 (lowest).
        /// </summary>
        public int Priority { get; init; } = 100;

        /// <summary>
        /// Gets the collection of filter-specific configuration settings.
        /// Settings are interpreted by the specific filter implementation.
        /// </summary>
        public IReadOnlyDictionary<string, object> Settings { get; init; } = new Dictionary<string, object>();


        /// <summary>
        /// Validates the filter configuration for correctness and consistency.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration validation fails.</exception>
        public void Validate()
        {
            if (Name.IsEmpty)
                throw new InvalidOperationException("Filter name cannot be empty.");

            if (FilterType == default && string.IsNullOrWhiteSpace(Type))
                throw new InvalidOperationException("Filter type must be specified either as enum or string.");

            if (Priority < 1 || Priority > 1000)
                throw new InvalidOperationException("Filter priority must be between 1 and 1000.");

            if (Settings == null)
                throw new InvalidOperationException("Filter settings cannot be null.");


            // Validate type-specific settings
            ValidateTypeSpecificSettings();
        }

        /// <summary>
        /// Validates settings specific to the filter type.
        /// </summary>
        private void ValidateTypeSpecificSettings()
        {
            switch (FilterType)
            {
                case FilterType.Severity:
                    ValidateSeveritySettings();
                    break;
                case FilterType.Source:
                    ValidateSourceSettings();
                    break;
                case FilterType.RateLimit:
                    ValidateRateLimitSettings();
                    break;
                case FilterType.Content:
                    ValidateContentSettings();
                    break;
                case FilterType.TimeBased:
                    ValidateTimeBasedSettings();
                    break;
                case FilterType.Composite:
                    ValidateCompositeSettings();
                    break;
                case FilterType.Correlation:
                    ValidateCorrelationSettings();
                    break;
            }
        }

        private void ValidateSeveritySettings()
        {
            if (!Settings.ContainsKey("MinimumSeverity"))
                throw new InvalidOperationException("Severity filter requires MinimumSeverity setting.");
        }

        private void ValidateSourceSettings()
        {
            if (!Settings.ContainsKey("AllowedSources") && !Settings.ContainsKey("BlockedSources"))
                throw new InvalidOperationException("Source filter requires either AllowedSources or BlockedSources setting.");
        }

        private void ValidateRateLimitSettings()
        {
            if (!Settings.ContainsKey("MaxAlertsPerMinute"))
                throw new InvalidOperationException("RateLimit filter requires MaxAlertsPerMinute setting.");

            if (Settings.TryGetValue("MaxAlertsPerMinute", out var value) && 
                value is int maxAlerts && maxAlerts <= 0)
                throw new InvalidOperationException("MaxAlertsPerMinute must be greater than zero.");
        }

        private void ValidateContentSettings()
        {
            if (!Settings.ContainsKey("Patterns"))
                throw new InvalidOperationException("Content filter requires Patterns setting.");
        }

        private void ValidateTimeBasedSettings()
        {
            if (!Settings.ContainsKey("TimeRanges"))
                throw new InvalidOperationException("TimeBased filter requires TimeRanges setting.");
        }

        private void ValidateCompositeSettings()
        {
            if (!Settings.ContainsKey("ChildFilters"))
                throw new InvalidOperationException("Composite filter requires ChildFilters setting.");
        }

        private void ValidateCorrelationSettings()
        {
            if (!Settings.ContainsKey("RequiredCorrelationIds"))
                throw new InvalidOperationException("Correlation filter requires RequiredCorrelationIds setting.");
        }

        /// <summary>
        /// Creates a default configuration for a specific filter type.
        /// </summary>
        /// <param name="filterType">Type of filter</param>
        /// <param name="name">Filter name</param>
        /// <param name="priority">Filter priority</param>
        /// <returns>Default filter configuration</returns>
        public static FilterConfiguration DefaultFor(FilterType filterType, FixedString64Bytes name, int priority = 100)
        {
            var settings = GetDefaultSettingsFor(filterType);

            return new FilterConfiguration
            {
                Name = name,
                FilterType = filterType,
                Type = filterType.ToString(),
                IsEnabled = true,
                Priority = priority,
                Settings = settings
            };
        }

        /// <summary>
        /// Gets default settings for a specific filter type.
        /// </summary>
        /// <param name="filterType">Filter type</param>
        /// <returns>Default settings dictionary</returns>
        private static Dictionary<string, object> GetDefaultSettingsFor(FilterType filterType)
        {
            return filterType switch
            {
                FilterType.Severity => new Dictionary<string, object>
                {
                    ["MinimumSeverity"] = AlertSeverity.Info,
                    ["AllowCriticalAlways"] = true
                },
                FilterType.Source => new Dictionary<string, object>
                {
                    ["AllowedSources"] = new List<string> { "*" },
                    ["UseWhitelist"] = true,
                    ["CaseSensitive"] = false
                },
                FilterType.RateLimit => new Dictionary<string, object>
                {
                    ["MaxAlertsPerMinute"] = 60,
                    ["SourcePattern"] = "*",
                    ["WindowSize"] = 60
                },
                FilterType.Content => new Dictionary<string, object>
                {
                    ["Patterns"] = new List<string>(),
                    ["Action"] = FilterAction.Suppress,
                    ["CaseSensitive"] = false,
                    ["UseRegex"] = false
                },
                FilterType.TimeBased => new Dictionary<string, object>
                {
                    ["TimeRanges"] = new[] { TimeRange.Always() },
                    ["Timezone"] = "UTC"
                },
                FilterType.Tag => new Dictionary<string, object>
                {
                    ["RequiredTags"] = new List<string>(),
                    ["ForbiddenTags"] = new List<string>(),
                    ["MatchMode"] = "Any"
                },
                FilterType.Composite => new Dictionary<string, object>
                {
                    ["ChildFilters"] = new List<FilterConfiguration>(),
                    ["LogicalOperator"] = LogicalOperator.And
                },
                FilterType.Correlation => new Dictionary<string, object>
                {
                    ["RequiredCorrelationIds"] = new List<string>(),
                    ["MatchMode"] = "Any",
                    ["IncludeOperationIds"] = false
                },
                FilterType.PassThrough => new Dictionary<string, object>(),
                FilterType.Block => new Dictionary<string, object>(),
                _ => new Dictionary<string, object>()
            };
        }
    }
}