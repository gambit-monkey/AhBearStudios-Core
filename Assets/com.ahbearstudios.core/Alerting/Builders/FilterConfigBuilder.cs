using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Pooling;
using ZLinq;

namespace AhBearStudios.Core.Alerting.Builders
{
    /// <summary>
    /// Implementation of filter configuration builder.
    /// Uses ZLinq for collection operations and pooling for temporary allocations.
    /// Provides fluent interface for building comprehensive filter configurations.
    /// </summary>
    internal sealed class FilterConfigBuilder : IFilterConfigBuilder
    {
        private readonly List<FilterConfiguration> _filters;
        private readonly IPoolingService _poolingService;
        private readonly Dictionary<string, FilterConfiguration> _filtersByName;

        /// <summary>
        /// Initializes a new instance of the FilterConfigBuilder class.
        /// </summary>
        /// <param name="poolingService">Pooling service for temporary allocations</param>
        public FilterConfigBuilder(IPoolingService poolingService = null)
        {
            _filters = new List<FilterConfiguration>();
            _poolingService = poolingService;
            _filtersByName = new Dictionary<string, FilterConfiguration>();
        }

        #region Specific Filter Methods

        /// <summary>
        /// Adds a severity filter that filters alerts based on severity levels.
        /// </summary>
        public IFilterConfigBuilder AddSeverityFilter(string name, AlertSeverity minimumSeverity, bool allowCriticalAlways = true, int priority = 10)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Filter name cannot be null or whitespace", nameof(name));

            var settings = new Dictionary<string, object>
            {
                ["MinimumSeverity"] = minimumSeverity,
                ["AllowCriticalAlways"] = allowCriticalAlways
            };

            var config = new FilterConfiguration
            {
                Name = name,
                FilterType = FilterType.Severity,
                Type = "Severity",
                IsEnabled = true,
                Priority = priority,
                Settings = settings
            };

            return AddFilterInternal(config);
        }

        /// <summary>
        /// Adds a rate limiting filter that limits alerts per time window.
        /// </summary>
        public IFilterConfigBuilder AddRateLimitFilter(string name, int maxAlertsPerMinute, string sourcePattern = "*", int burstSize = 10, int priority = 30)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Filter name cannot be null or whitespace", nameof(name));
            if (maxAlertsPerMinute <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxAlertsPerMinute), "Must be greater than 0");

            var settings = new Dictionary<string, object>
            {
                ["MaxAlertsPerMinute"] = maxAlertsPerMinute,
                ["SourcePattern"] = sourcePattern ?? "*",
                ["BurstSize"] = burstSize,
                ["WindowSize"] = 60 // seconds
            };

            var config = new FilterConfiguration
            {
                Name = name,
                FilterType = FilterType.RateLimit,
                Type = "RateLimit",
                IsEnabled = true,
                Priority = priority,
                Settings = settings
            };

            return AddFilterInternal(config);
        }

        /// <summary>
        /// Adds a source filter that filters alerts based on source patterns.
        /// </summary>
        public IFilterConfigBuilder AddSourceFilter(string name, IEnumerable<string> sources, bool useWhitelist = true, bool caseSensitive = false, bool useRegex = false, int priority = 20)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Filter name cannot be null or whitespace", nameof(name));
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            var sourcesList = sources.AsValueEnumerable().ToList();
            if (sourcesList.Count == 0)
                throw new ArgumentException("At least one source pattern must be provided", nameof(sources));

            var settings = new Dictionary<string, object>
            {
                [useWhitelist ? "AllowedSources" : "BlockedSources"] = sourcesList,
                ["UseWhitelist"] = useWhitelist,
                ["CaseSensitive"] = caseSensitive,
                ["UseRegex"] = useRegex
            };

            var config = new FilterConfiguration
            {
                Name = name,
                FilterType = FilterType.Source,
                Type = "Source",
                IsEnabled = true,
                Priority = priority,
                Settings = settings
            };

            return AddFilterInternal(config);
        }

        /// <summary>
        /// Adds a content filter that filters alerts based on message content.
        /// </summary>
        public IFilterConfigBuilder AddContentFilter(string name, IEnumerable<string> patterns, FilterAction action = FilterAction.Suppress, bool caseSensitive = false, bool useRegex = false, int priority = 40)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Filter name cannot be null or whitespace", nameof(name));
            if (patterns == null)
                throw new ArgumentNullException(nameof(patterns));

            var patternsList = patterns.AsValueEnumerable().ToList();
            if (patternsList.Count == 0)
                throw new ArgumentException("At least one content pattern must be provided", nameof(patterns));

            var settings = new Dictionary<string, object>
            {
                ["Patterns"] = patternsList,
                ["Action"] = action,
                ["CaseSensitive"] = caseSensitive,
                ["UseRegex"] = useRegex
            };

            var config = new FilterConfiguration
            {
                Name = name,
                FilterType = FilterType.Content,
                Type = "Content",
                IsEnabled = true,
                Priority = priority,
                Settings = settings
            };

            return AddFilterInternal(config);
        }

        /// <summary>
        /// Adds a time-based filter that filters alerts based on time ranges.
        /// </summary>
        public IFilterConfigBuilder AddTimeBasedFilter(string name, IEnumerable<TimeRange> timeRanges, TimeZoneInfo timezone = null, int priority = 50)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Filter name cannot be null or whitespace", nameof(name));
            if (timeRanges == null)
                throw new ArgumentNullException(nameof(timeRanges));

            var rangesList = timeRanges.AsValueEnumerable().ToList();
            if (rangesList.Count == 0)
                throw new ArgumentException("At least one time range must be provided", nameof(timeRanges));

            var settings = new Dictionary<string, object>
            {
                ["TimeRanges"] = rangesList,
                ["Timezone"] = timezone?.Id ?? TimeZoneInfo.Utc.Id
            };

            var config = new FilterConfiguration
            {
                Name = name,
                FilterType = FilterType.TimeBased,
                Type = "TimeBased",
                IsEnabled = true,
                Priority = priority,
                Settings = settings
            };

            return AddFilterInternal(config);
        }

        /// <summary>
        /// Adds a composite filter that combines multiple child filters.
        /// </summary>
        public IFilterConfigBuilder AddCompositeFilter(string name, Action<IFilterConfigBuilder> childBuilder, LogicalOperator logicalOperator = LogicalOperator.And, int priority = 60)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Filter name cannot be null or whitespace", nameof(name));
            if (childBuilder == null)
                throw new ArgumentNullException(nameof(childBuilder));

            // Create child builder and build child filters
            var childFilterBuilder = new FilterConfigBuilder(_poolingService);
            childBuilder(childFilterBuilder);
            var childFilters = childFilterBuilder.Build();

            if (childFilters.Count == 0)
                throw new InvalidOperationException("Composite filter must have at least one child filter");

            var settings = new Dictionary<string, object>
            {
                ["ChildFilters"] = childFilters,
                ["LogicalOperator"] = logicalOperator
            };

            var config = new FilterConfiguration
            {
                Name = name,
                FilterType = FilterType.Composite,
                Type = "Composite",
                IsEnabled = true,
                Priority = priority,
                Settings = settings
            };

            return AddFilterInternal(config);
        }

        /// <summary>
        /// Adds a tag-based filter that filters alerts based on tags.
        /// </summary>
        public IFilterConfigBuilder AddTagFilter(string name, IEnumerable<string> requiredTags = null, IEnumerable<string> excludedTags = null, bool requireAllTags = false, int priority = 25)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Filter name cannot be null or whitespace", nameof(name));

            var settings = new Dictionary<string, object>
            {
                ["RequiredTags"] = requiredTags?.AsValueEnumerable().ToList() ?? new List<string>(),
                ["ExcludedTags"] = excludedTags?.AsValueEnumerable().ToList() ?? new List<string>(),
                ["RequireAllTags"] = requireAllTags
            };

            var config = new FilterConfiguration
            {
                Name = name,
                FilterType = FilterType.Tag,
                Type = "Tag",
                IsEnabled = true,
                Priority = priority,
                Settings = settings
            };

            return AddFilterInternal(config);
        }

        /// <summary>
        /// Adds a correlation filter that filters based on correlation patterns.
        /// </summary>
        public IFilterConfigBuilder AddCorrelationFilter(string name, IEnumerable<string> correlationPatterns, TimeSpan timeWindow = default, int priority = 35)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Filter name cannot be null or whitespace", nameof(name));
            if (correlationPatterns == null)
                throw new ArgumentNullException(nameof(correlationPatterns));

            var patternsList = correlationPatterns.AsValueEnumerable().ToList();
            if (patternsList.Count == 0)
                throw new ArgumentException("At least one correlation pattern must be provided", nameof(correlationPatterns));

            var settings = new Dictionary<string, object>
            {
                ["CorrelationPatterns"] = patternsList,
                ["TimeWindow"] = timeWindow == default ? TimeSpan.FromMinutes(5) : timeWindow
            };

            var config = new FilterConfiguration
            {
                Name = name,
                FilterType = FilterType.Correlation,
                Type = "Correlation",
                IsEnabled = true,
                Priority = priority,
                Settings = settings
            };

            return AddFilterInternal(config);
        }

        /// <summary>
        /// Adds a pass-through filter that allows all alerts.
        /// </summary>
        public IFilterConfigBuilder AddPassThroughFilter(string name, int priority = 100)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Filter name cannot be null or whitespace", nameof(name));

            var config = new FilterConfiguration
            {
                Name = name,
                FilterType = FilterType.PassThrough,
                Type = "PassThrough",
                IsEnabled = true,
                Priority = priority,
                Settings = new Dictionary<string, object>()
            };

            return AddFilterInternal(config);
        }

        /// <summary>
        /// Adds a block filter that suppresses all alerts.
        /// </summary>
        public IFilterConfigBuilder AddBlockFilter(string name, string reason = "Blocked by configuration", int priority = 1)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Filter name cannot be null or whitespace", nameof(name));

            var settings = new Dictionary<string, object>
            {
                ["BlockReason"] = reason ?? "Blocked by configuration"
            };

            var config = new FilterConfiguration
            {
                Name = name,
                FilterType = FilterType.Block,
                Type = "Block",
                IsEnabled = true,
                Priority = priority,
                Settings = settings
            };

            return AddFilterInternal(config);
        }

        #endregion

        #region Generic Filter Configuration

        /// <summary>
        /// Adds a filter using a pre-built FilterConfiguration.
        /// </summary>
        public IFilterConfigBuilder AddFilter(FilterConfiguration filterConfiguration)
        {
            if (filterConfiguration == null)
                throw new ArgumentNullException(nameof(filterConfiguration));

            return AddFilterInternal(filterConfiguration);
        }

        /// <summary>
        /// Adds a custom filter with specific type and settings.
        /// </summary>
        public IFilterConfigBuilder AddCustomFilter(string name, FilterType filterType, Dictionary<string, object> settings, int priority = 50, bool enabled = true)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Filter name cannot be null or whitespace", nameof(name));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var config = new FilterConfiguration
            {
                Name = name,
                FilterType = filterType,
                Type = filterType.ToString(),
                IsEnabled = enabled,
                Priority = priority,
                Settings = settings
            };

            return AddFilterInternal(config);
        }

        #endregion

        #region Filter Management

        /// <summary>
        /// Removes a filter by name.
        /// </summary>
        public IFilterConfigBuilder RemoveFilter(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return this;

            if (_filtersByName.TryGetValue(name, out var config))
            {
                _filters.Remove(config);
                _filtersByName.Remove(name);
            }

            return this;
        }

        /// <summary>
        /// Removes all filters of a specific type.
        /// </summary>
        public IFilterConfigBuilder RemoveFiltersOfType(FilterType filterType)
        {
            var toRemove = _filters.AsValueEnumerable().Where(f => f.FilterType == filterType).ToList();
            
            foreach (var filter in toRemove)
            {
                _filters.Remove(filter);
                _filtersByName.Remove(filter.Name);
            }

            return this;
        }

        /// <summary>
        /// Clears all configured filters.
        /// </summary>
        public IFilterConfigBuilder ClearFilters()
        {
            _filters.Clear();
            _filtersByName.Clear();
            return this;
        }

        /// <summary>
        /// Sets the priority of an existing filter.
        /// </summary>
        public IFilterConfigBuilder SetFilterPriority(string name, int priority)
        {
            if (string.IsNullOrWhiteSpace(name))
                return this;

            if (_filtersByName.TryGetValue(name, out var config))
            {
                var updatedConfig = config with { Priority = priority };
                UpdateFilter(config, updatedConfig);
            }

            return this;
        }

        /// <summary>
        /// Enables or disables a filter by name.
        /// </summary>
        public IFilterConfigBuilder SetFilterEnabled(string name, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(name))
                return this;

            if (_filtersByName.TryGetValue(name, out var config))
            {
                var updatedConfig = config with { IsEnabled = enabled };
                UpdateFilter(config, updatedConfig);
            }

            return this;
        }

        #endregion

        #region Environment Presets

        /// <summary>
        /// Configures filters optimized for development environments.
        /// </summary>
        public IFilterConfigBuilder ForDevelopment()
        {
            return AddSeverityFilter("DevSeverityFilter", AlertSeverity.Debug, true, 10)
                   .AddRateLimitFilter("DevRateLimitFilter", 1000, "*", 50, 90);
        }

        /// <summary>
        /// Configures filters optimized for production environments.
        /// </summary>
        public IFilterConfigBuilder ForProduction()
        {
            return AddSeverityFilter("ProdSeverityFilter", AlertSeverity.Warning, true, 10)
                   .AddRateLimitFilter("ProdRateLimitFilter", 100, "*", 20, 30)
                   .AddContentFilter("ProdNoiseFilter", new[] { "*test*", "*debug*", "*trace*" }, FilterAction.Suppress, false, false, 20);
        }

        /// <summary>
        /// Configures filters optimized for testing scenarios.
        /// </summary>
        public IFilterConfigBuilder ForTesting()
        {
            return AddPassThroughFilter("TestPassThroughFilter", 1);
        }

        /// <summary>
        /// Configures emergency filters that suppress most alerts.
        /// </summary>
        public IFilterConfigBuilder ForEmergency()
        {
            return ClearFilters()
                   .AddSeverityFilter("EmergencyFilter", AlertSeverity.Critical, true, 1);
        }

        #endregion

        #region Validation and Build

        /// <summary>
        /// Validates all configured filters without building.
        /// </summary>
        public Dictionary<string, FilterValidationResult> ValidateFilters()
        {
            var results = new Dictionary<string, FilterValidationResult>();

            foreach (var filter in _filters)
            {
                try
                {
                    filter.Validate();
                    results[filter.Name] = FilterValidationResult.Valid();
                }
                catch (InvalidOperationException ex)
                {
                    results[filter.Name] = FilterValidationResult.Invalid(new[] { ex.Message });
                }
            }

            return results;
        }

        /// <summary>
        /// Gets the current filter count.
        /// </summary>
        public int GetFilterCount()
        {
            return _filters.Count;
        }

        /// <summary>
        /// Gets the names of all configured filters.
        /// </summary>
        public IReadOnlyList<string> GetFilterNames()
        {
            return _filters.AsValueEnumerable().Select(f => f.Name.ToString()).ToList();
        }

        /// <summary>
        /// Checks if a filter with the specified name exists.
        /// </summary>
        public bool HasFilter(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && _filtersByName.ContainsKey(name);
        }

        /// <summary>
        /// Builds the final list of filter configurations.
        /// </summary>
        public IReadOnlyList<FilterConfiguration> Build()
        {
            // Validate all filters
            var validationResults = ValidateFilters();
            var invalidFilters = validationResults
                .AsValueEnumerable()
                .Where(kvp => !kvp.Value.IsValid)
                .ToList();

            if (invalidFilters.Any())
            {
                var errorMessages = invalidFilters
                    .AsValueEnumerable()
                    .SelectMany(kvp => kvp.Value.Errors.AsValueEnumerable().Select(e => $"Filter '{kvp.Key}': {e}"))
                    .ToList();
                
                throw new InvalidOperationException($"Filter validation failed: {string.Join(", ", errorMessages)}");
            }

            // Sort by priority (lower priority number = higher precedence)
            return _filters
                .AsValueEnumerable()
                .OrderBy(f => f.Priority)
                .ThenBy(f => f.Name.ToString())
                .ToList();
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Adds a filter configuration to the internal collections.
        /// </summary>
        private IFilterConfigBuilder AddFilterInternal(FilterConfiguration config)
        {
            if (_filtersByName.ContainsKey(config.Name))
            {
                throw new InvalidOperationException($"A filter with name '{config.Name}' already exists");
            }

            _filters.Add(config);
            _filtersByName[config.Name] = config;
            
            return this;
        }

        /// <summary>
        /// Updates an existing filter configuration.
        /// </summary>
        private void UpdateFilter(FilterConfiguration oldConfig, FilterConfiguration newConfig)
        {
            var index = _filters.IndexOf(oldConfig);
            if (index >= 0)
            {
                _filters[index] = newConfig;
                _filtersByName[newConfig.Name] = newConfig;
            }
        }

        #endregion
    }
}