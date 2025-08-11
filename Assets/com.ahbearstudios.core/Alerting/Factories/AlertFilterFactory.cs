using System;
using System.Collections.Generic;
using AhBearStudios.Core.Alerting.Configs;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using ZLinq;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization;

namespace AhBearStudios.Core.Alerting.Factories
{
    /// <summary>
    /// Concrete implementation of IAlertFilterFactory for creating and configuring alert filter instances.
    /// Supports various filter types including severity, source, rate limiting, content, and composite filters.
    /// Designed for Unity game development with comprehensive configuration and validation support.
    /// </summary>
    public sealed class AlertFilterFactory : IAlertFilterFactory
    {
        private readonly ILoggingService _loggingService;
        private readonly ISerializationService _serializationService;
        private readonly Dictionary<FilterType, Func<FilterConfiguration, UniTask<IAlertFilter>>> _filterCreators;

        /// <summary>
        /// Initializes a new instance of the AlertFilterFactory class.
        /// </summary>
        /// <param name="loggingService">Optional logging service for factory operations</param>
        /// <param name="serializationService">Optional serialization service for alert data serialization</param>
        public AlertFilterFactory(ILoggingService loggingService = null, ISerializationService serializationService = null)
        {
            _loggingService = loggingService;
            _serializationService = serializationService;
            _filterCreators = InitializeFilterCreators();
        }

        #region IAlertFilterFactory Implementation

        /// <summary>
        /// Creates a new alert filter instance by type.
        /// </summary>
        public async UniTask<IAlertFilter> CreateFilterAsync(FilterType filterType, FixedString64Bytes name, int priority = 100)
        {
            var configuration = FilterConfiguration.DefaultFor(filterType, name, priority);
            return await CreateAndConfigureFilterAsync(configuration);
        }

        /// <summary>
        /// Creates a new alert filter instance by type name.
        /// </summary>
        public async UniTask<IAlertFilter> CreateFilterAsync(string filterTypeName, FixedString64Bytes name, int priority = 100)
        {
            if (!Enum.TryParse<FilterType>(filterTypeName, true, out var filterType))
            {
                throw new ArgumentException($"Unsupported filter type: {filterTypeName}", nameof(filterTypeName));
            }

            return await CreateFilterAsync(filterType, name, priority);
        }

        /// <summary>
        /// Creates and configures a new alert filter instance.
        /// </summary>
        public async UniTask<IAlertFilter> CreateAndConfigureFilterAsync(FilterConfiguration configuration, Guid correlationId = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var validationResult = ValidateFilterConfiguration(configuration);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"Invalid filter configuration: {string.Join(", ", validationResult.Errors.AsValueEnumerable().Select(e => e.Message))}", nameof(configuration));
            }

            var startTime = DateTime.UtcNow;
            
            try
            {
                // Determine filter type
                var filterType = DetermineFilterType(configuration);
                
                // Create the filter
                IAlertFilter filter;
                if (_filterCreators.TryGetValue(filterType, out var creator))
                {
                    filter = await creator(configuration);
                }
                else
                {
                    throw new NotSupportedException($"Filter type '{filterType}' is not supported");
                }

                // Configure the filter
                var configResult = filter.Configure(configuration.Settings, correlationId);
                if (!configResult)
                {
                    throw new InvalidOperationException($"Failed to configure filter '{configuration.Name}'");
                }

                // Set priority
                filter.Priority = configuration.Priority;

                var creationTime = DateTime.UtcNow - startTime;
                LogInfo($"Created and configured filter '{configuration.Name}' of type '{filterType}' in {creationTime.TotalMilliseconds:F2}ms", correlationId);
                
                return filter;
            }
            catch (Exception ex)
            {
                var creationTime = DateTime.UtcNow - startTime;
                LogError($"Failed to create filter '{configuration.Name}': {ex.Message} (took {creationTime.TotalMilliseconds:F2}ms)", correlationId);
                throw;
            }
        }

        /// <summary>
        /// Creates a severity filter with specific configuration.
        /// </summary>
        public async UniTask<IAlertFilter> CreateSeverityFilterAsync(
            FixedString64Bytes name,
            AlertSeverity minimumSeverity,
            bool allowCriticalAlways = true,
            int priority = 10)
        {
            var configuration = new FilterConfiguration
            {
                Type = "Severity",
                Name = name,
                IsEnabled = true,
                Priority = priority,
                Settings = new Dictionary<string, object>
                {
                    ["MinimumSeverity"] = minimumSeverity,
                    ["AllowCriticalAlways"] = allowCriticalAlways
                }
            };

            return await CreateAndConfigureFilterAsync(configuration);
        }

        /// <summary>
        /// Creates a source filter with specific configuration.
        /// </summary>
        public async UniTask<IAlertFilter> CreateSourceFilterAsync(
            FixedString64Bytes name,
            IEnumerable<string> allowedSources,
            bool useWhitelist = true,
            int priority = 20)
        {
            if (allowedSources == null)
                throw new ArgumentNullException(nameof(allowedSources));

            var configuration = new FilterConfiguration
            {
                Type = "Source",
                Name = name,
                IsEnabled = true,
                Priority = priority,
                Settings = new Dictionary<string, object>
                {
                    ["AllowedSources"] = allowedSources.AsValueEnumerable().ToList(),
                    ["UseWhitelist"] = useWhitelist,
                    ["CaseSensitive"] = false
                }
            };

            return await CreateAndConfigureFilterAsync(configuration);
        }

        /// <summary>
        /// Creates a rate limiting filter with specific configuration.
        /// </summary>
        public async UniTask<IAlertFilter> CreateRateLimitFilterAsync(
            FixedString64Bytes name,
            int maxAlertsPerMinute,
            string sourcePattern = "*",
            int priority = 30)
        {
            if (maxAlertsPerMinute <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxAlertsPerMinute), "Must be greater than 0");

            var configuration = new FilterConfiguration
            {
                Type = "RateLimit",
                Name = name,
                IsEnabled = true,
                Priority = priority,
                Settings = new Dictionary<string, object>
                {
                    ["MaxAlertsPerMinute"] = maxAlertsPerMinute,
                    ["SourcePattern"] = sourcePattern ?? "*",
                    ["WindowSize"] = 60
                }
            };

            return await CreateAndConfigureFilterAsync(configuration);
        }

        /// <summary>
        /// Creates a content filter with specific configuration.
        /// </summary>
        public async UniTask<IAlertFilter> CreateContentFilterAsync(
            FixedString64Bytes name,
            IEnumerable<string> patterns,
            FilterAction action = FilterAction.Suppress,
            int priority = 40)
        {
            if (patterns == null)
                throw new ArgumentNullException(nameof(patterns));

            var configuration = new FilterConfiguration
            {
                Type = "Content",
                Name = name,
                IsEnabled = true,
                Priority = priority,
                Settings = new Dictionary<string, object>
                {
                    ["Patterns"] = patterns.AsValueEnumerable().ToList(),
                    ["Action"] = action,
                    ["CaseSensitive"] = false,
                    ["UseRegex"] = false
                }
            };

            return await CreateAndConfigureFilterAsync(configuration);
        }

        /// <summary>
        /// Creates a time-based filter with specific configuration.
        /// </summary>
        public async UniTask<IAlertFilter> CreateTimeBasedFilterAsync(
            FixedString64Bytes name,
            IEnumerable<TimeRange> allowedTimeRanges,
            TimeZoneInfo timezone = null,
            int priority = 50)
        {
            if (allowedTimeRanges == null)
                throw new ArgumentNullException(nameof(allowedTimeRanges));

            var configuration = new FilterConfiguration
            {
                Type = "TimeBased",
                Name = name,
                IsEnabled = true,
                Priority = priority,
                Settings = new Dictionary<string, object>
                {
                    ["TimeRanges"] = allowedTimeRanges.AsValueEnumerable().ToList(),
                    ["Timezone"] = timezone?.Id ?? TimeZoneInfo.Utc.Id
                }
            };

            return await CreateAndConfigureFilterAsync(configuration);
        }

        /// <summary>
        /// Creates a composite filter that combines multiple filters.
        /// </summary>
        public async UniTask<IAlertFilter> CreateCompositeFilterAsync(
            FixedString64Bytes name,
            IEnumerable<IAlertFilter> childFilters,
            LogicalOperator logicalOperator = LogicalOperator.And,
            int priority = 60)
        {
            if (childFilters == null)
                throw new ArgumentNullException(nameof(childFilters));

            var childFiltersList = childFilters.AsValueEnumerable().ToList();
            if (childFiltersList.Count == 0)
                throw new ArgumentException("At least one child filter is required", nameof(childFilters));

            var configuration = new FilterConfiguration
            {
                Type = "Composite",
                Name = name,
                IsEnabled = true,
                Priority = priority,
                Settings = new Dictionary<string, object>
                {
                    ["ChildFilters"] = childFiltersList,
                    ["LogicalOperator"] = logicalOperator
                }
            };

            return await CreateAndConfigureFilterAsync(configuration);
        }

        /// <summary>
        /// Creates multiple filters from a collection of configurations.
        /// </summary>
        public async UniTask<IEnumerable<IAlertFilter>> CreateFiltersAsync(
            IEnumerable<FilterConfiguration> configurations,
            Guid correlationId = default)
        {
            if (configurations == null)
                throw new ArgumentNullException(nameof(configurations));

            var filters = new List<IAlertFilter>();
            var tasks = configurations.AsValueEnumerable().Select(config => CreateFilterSafely(config, correlationId));
            var results = await UniTask.WhenAll(tasks);

            foreach (var result in results)
            {
                if (result.Success)
                {
                    filters.Add(result.Filter);
                }
                else
                {
                    LogError($"Failed to create filter: {result.Error}", correlationId);
                }
            }

            return filters;
        }

        /// <summary>
        /// Creates filters optimized for development environments.
        /// </summary>
        public async UniTask<IEnumerable<IAlertFilter>> CreateDevelopmentFiltersAsync()
        {
            var filters = new List<IAlertFilter>();

            // Development severity filter (allow debug and above)
            var severityFilter = await CreateSeverityFilterAsync(
                "DevelopmentSeverity",
                AlertSeverity.Debug,
                allowCriticalAlways: true,
                priority: 5);
            filters.Add(severityFilter);

            // Development rate limit filter (generous limits)
            var rateLimitFilter = await CreateRateLimitFilterAsync(
                "DevelopmentRateLimit",
                maxAlertsPerMinute: 1000,
                sourcePattern: "*",
                priority: 90);
            filters.Add(rateLimitFilter);

            return filters;
        }

        /// <summary>
        /// Creates filters optimized for production environments.
        /// </summary>
        public async UniTask<IEnumerable<IAlertFilter>> CreateProductionFiltersAsync()
        {
            var filters = new List<IAlertFilter>();

            // Production severity filter (warning and above)
            var severityFilter = await CreateSeverityFilterAsync(
                "ProductionSeverity",
                AlertSeverity.Warning,
                allowCriticalAlways: true,
                priority: 10);
            filters.Add(severityFilter);

            // Production rate limit filter (conservative limits)
            var rateLimitFilter = await CreateRateLimitFilterAsync(
                "ProductionRateLimit",
                maxAlertsPerMinute: 100,
                sourcePattern: "*",
                priority: 20);
            filters.Add(rateLimitFilter);

            // Content filter to suppress noise
            var contentFilter = await CreateContentFilterAsync(
                "ProductionContentFilter",
                new[] { "*test*", "*debug*", "*trace*" },
                FilterAction.Suppress,
                priority: 30);
            filters.Add(contentFilter);

            return filters;
        }

        /// <summary>
        /// Creates filters optimized for testing scenarios.
        /// </summary>
        public async UniTask<IEnumerable<IAlertFilter>> CreateTestFiltersAsync()
        {
            var filters = new List<IAlertFilter>();

            // Test pass-through filter (allows everything)
            var passFilter = await CreateFilterAsync(FilterType.PassThrough, "TestPassThrough", priority: 1);
            filters.Add(passFilter);

            return filters;
        }

        /// <summary>
        /// Validates a filter configuration before creation.
        /// </summary>
        public ValidationResult ValidateFilterConfiguration(FilterConfiguration configuration)
        {
            if (configuration == null)
                return ValidationResult.Failure(new[] { new ValidationError("Configuration cannot be null") }, "AlertFilterFactory");

            var errors = new List<ValidationError>();

            // Validate basic properties
            if (string.IsNullOrEmpty(configuration.Type))
                errors.Add(new ValidationError("Filter type cannot be null or empty"));

            if (configuration.Name.IsEmpty)
                errors.Add(new ValidationError("Filter name cannot be empty"));

            if (configuration.Priority < 0)
                errors.Add(new ValidationError("Filter priority cannot be negative"));

            // Validate type-specific settings
            var filterType = DetermineFilterType(configuration);
            switch (filterType)
            {
                case FilterType.Severity:
                    ValidateSeverityFilterSettings(configuration.Settings, errors);
                    break;

                case FilterType.Source:
                    ValidateSourceFilterSettings(configuration.Settings, errors);
                    break;

                case FilterType.RateLimit:
                    ValidateRateLimitFilterSettings(configuration.Settings, errors);
                    break;

                case FilterType.Content:
                    ValidateContentFilterSettings(configuration.Settings, errors);
                    break;

                case FilterType.TimeBased:
                    ValidateTimeBasedFilterSettings(configuration.Settings, errors);
                    break;

                case FilterType.Composite:
                    ValidateCompositeFilterSettings(configuration.Settings, errors);
                    break;
            }

            return errors.AsValueEnumerable().Any()
                ? ValidationResult.Failure(errors, "AlertFilterFactory")
                : ValidationResult.Success("AlertFilterFactory");
        }

        /// <summary>
        /// Gets the default configuration for a specific filter type.
        /// </summary>
        public FilterConfiguration GetDefaultConfiguration(FilterType filterType)
        {
            return FilterConfiguration.DefaultFor(filterType, $"Default{filterType}", 100);
        }

        /// <summary>
        /// Gets all supported filter types.
        /// </summary>
        public IEnumerable<FilterType> GetSupportedFilterTypes()
        {
            return _filterCreators.Keys;
        }

        /// <summary>
        /// Checks if a filter type is supported by this factory.
        /// </summary>
        public bool IsFilterTypeSupported(FilterType filterType)
        {
            return _filterCreators.ContainsKey(filterType);
        }

        /// <summary>
        /// Creates configuration from a dictionary of settings.
        /// </summary>
        public FilterConfiguration CreateConfigurationFromSettings(FilterType filterType, FixedString64Bytes name, Dictionary<string, object> settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var config = FilterConfiguration.DefaultFor(filterType, name);

            // Override with provided settings
            if (settings.TryGetValue("IsEnabled", out var enabledValue))
            {
                if (bool.TryParse(enabledValue.ToString(), out var enabled))
                    config.IsEnabled = enabled;
            }

            if (settings.TryGetValue("Priority", out var priorityValue))
            {
                if (int.TryParse(priorityValue.ToString(), out var priority))
                    config.Priority = priority;
            }

            // Merge filter-specific settings
            foreach (var setting in settings)
            {
                if (!config.Settings.ContainsKey(setting.Key))
                    config.Settings[setting.Key] = setting.Value;
                else
                    config.Settings[setting.Key] = setting.Value; // Override default
            }

            return config;
        }

        #endregion

        #region Private Helper Methods

        private Dictionary<FilterType, Func<FilterConfiguration, UniTask<IAlertFilter>>> InitializeFilterCreators()
        {
            return new Dictionary<FilterType, Func<FilterConfiguration, UniTask<IAlertFilter>>>
            {
                [FilterType.Severity] = CreateSeverityFilterInternal,
                [FilterType.Source] = CreateSourceFilterInternal,
                [FilterType.RateLimit] = CreateRateLimitFilterInternal,
                [FilterType.Content] = CreateContentFilterInternal,
                [FilterType.TimeBased] = CreateTimeBasedFilterInternal,
                [FilterType.Composite] = CreateCompositeFilterInternal,
                [FilterType.Tag] = CreateTagFilterInternal,
                [FilterType.Correlation] = CreateCorrelationFilterInternal,
                [FilterType.PassThrough] = CreatePassThroughFilterInternal,
                [FilterType.Block] = CreateBlockFilterInternal
            };
        }

        private async UniTask<IAlertFilter> CreateSeverityFilterInternal(FilterConfiguration config)
        {
            var minimumSeverity = config.Settings.TryGetValue("MinimumSeverity", out var severityValue)
                && severityValue is AlertSeverity severity
                ? severity
                : AlertSeverity.Info;

            await UniTask.CompletedTask;
            var allowCriticalAlways = config.Settings.TryGetValue("AllowCriticalAlways", out var criticalValue)
                && criticalValue is bool critical
                ? critical
                : true;

            return new SeverityAlertFilter(minimumSeverity, allowCriticalAlways);
        }

        private async UniTask<IAlertFilter> CreateSourceFilterInternal(FilterConfiguration config)
        {
            var allowedSources = config.Settings.TryGetValue("AllowedSources", out var sourcesValue)
                && sourcesValue is IEnumerable<string> sources
                ? sources.AsValueEnumerable().ToList()
                : new List<string> { "*" };

            await UniTask.CompletedTask;
            return new SourceAlertFilter(config.Name.ToString(), allowedSources);
        }

        private async UniTask<IAlertFilter> CreateRateLimitFilterInternal(FilterConfiguration config)
        {
            var maxAlertsPerMinute = config.Settings.TryGetValue("MaxAlertsPerMinute", out var rateValue)
                && int.TryParse(rateValue.ToString(), out var rate)
                ? rate
                : 60;

            await UniTask.CompletedTask;
            return new RateLimitAlertFilter(config.Name.ToString(), maxAlertsPerMinute);
        }

        private async UniTask<IAlertFilter> CreateContentFilterInternal(FilterConfiguration config)
        {
            var patterns = config.Settings.TryGetValue("Patterns", out var patternsValue)
                && patternsValue is IEnumerable<string> patternList
                ? patternList.AsValueEnumerable().ToList()
                : new List<string>();

            await UniTask.CompletedTask;
            return new ContentAlertFilter(config.Name.ToString(), patterns);
        }

        private async UniTask<IAlertFilter> CreateTimeBasedFilterInternal(FilterConfiguration config)
        {
            var timeRanges = config.Settings.TryGetValue("TimeRanges", out var rangesValue)
                && rangesValue is IEnumerable<TimeRange> ranges
                ? ranges.AsValueEnumerable().ToList()
                : new List<TimeRange> { TimeRange.Always() };

            await UniTask.CompletedTask;
            return new TimeBasedAlertFilter(config.Name.ToString(), timeRanges);
        }

        private async UniTask<IAlertFilter> CreateCompositeFilterInternal(FilterConfiguration config)
        {
            var childFilters = config.Settings.TryGetValue("ChildFilters", out var filtersValue)
                && filtersValue is IEnumerable<IAlertFilter> filters
                ? filters.AsValueEnumerable().ToList()
                : new List<IAlertFilter>();

            var logicalOperator = config.Settings.TryGetValue("LogicalOperator", out var operatorValue)
                && operatorValue is LogicalOperator op
                ? op
                : LogicalOperator.And;

            await UniTask.CompletedTask;
            return new CompositeAlertFilter(config.Name.ToString(), childFilters, logicalOperator);
        }

        private async UniTask<IAlertFilter> CreateTagFilterInternal(FilterConfiguration config)
        {
            await UniTask.CompletedTask;
            return new TagAlertFilter(config.Name.ToString());
        }

        private async UniTask<IAlertFilter> CreateCorrelationFilterInternal(FilterConfiguration config)
        {
            await UniTask.CompletedTask;
            return new CorrelationAlertFilter(config.Name.ToString());
        }

        private async UniTask<IAlertFilter> CreatePassThroughFilterInternal(FilterConfiguration config)
        {
            await UniTask.CompletedTask;
            return new PassThroughAlertFilter(config.Name.ToString());
        }

        private async UniTask<IAlertFilter> CreateBlockFilterInternal(FilterConfiguration config)
        {
            await UniTask.CompletedTask;
            return new BlockAlertFilter(config.Name.ToString());
        }

        private FilterType DetermineFilterType(FilterConfiguration configuration)
        {
            if (Enum.TryParse<FilterType>(configuration.Type, true, out var filterType))
                return filterType;

            // Fallback mapping for string types
            return configuration.Type.ToLowerInvariant() switch
            {
                "severity" => FilterType.Severity,
                "source" => FilterType.Source,
                "ratelimit" or "rate" => FilterType.RateLimit,
                "content" or "message" => FilterType.Content,
                "time" or "timebased" => FilterType.TimeBased,
                "composite" or "combined" => FilterType.Composite,
                "tag" => FilterType.Tag,
                "correlation" => FilterType.Correlation,
                "passthrough" or "allow" => FilterType.PassThrough,
                "block" or "suppress" => FilterType.Block,
                _ => throw new ArgumentException($"Unknown filter type: {configuration.Type}")
            };
        }

        private async UniTask<FilterCreationResult> CreateFilterSafely(FilterConfiguration configuration, Guid correlationId)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                var filter = await CreateAndConfigureFilterAsync(configuration, correlationId);
                var creationTime = DateTime.UtcNow - startTime;
                return FilterCreationResult.Success(filter, configuration, creationTime);
            }
            catch (Exception ex)
            {
                var creationTime = DateTime.UtcNow - startTime;
                return FilterCreationResult.Failure(ex.Message, configuration, creationTime);
            }
        }

        #region Validation Methods

        private void ValidateSeverityFilterSettings(Dictionary<string, object> settings, List<ValidationError> errors)
        {
            if (!settings.ContainsKey("MinimumSeverity"))
                errors.Add(new ValidationError("Severity filter requires MinimumSeverity setting"));
        }

        private void ValidateSourceFilterSettings(Dictionary<string, object> settings, List<ValidationError> errors)
        {
            if (!settings.ContainsKey("AllowedSources"))
                errors.Add(new ValidationError("Source filter requires AllowedSources setting"));
        }

        private void ValidateRateLimitFilterSettings(Dictionary<string, object> settings, List<ValidationError> errors)
        {
            if (settings.TryGetValue("MaxAlertsPerMinute", out var rateValue))
            {
                if (!int.TryParse(rateValue.ToString(), out var rate) || rate <= 0)
                    errors.Add(new ValidationError("RateLimit filter MaxAlertsPerMinute must be a positive integer"));
            }
            else
            {
                errors.Add(new ValidationError("RateLimit filter requires MaxAlertsPerMinute setting"));
            }
        }

        private void ValidateContentFilterSettings(Dictionary<string, object> settings, List<ValidationError> errors)
        {
            if (!settings.ContainsKey("Patterns"))
                errors.Add(new ValidationError("Content filter requires Patterns setting"));
        }

        private void ValidateTimeBasedFilterSettings(Dictionary<string, object> settings, List<ValidationError> errors)
        {
            if (!settings.ContainsKey("TimeRanges"))
                errors.Add(new ValidationError("TimeBased filter requires TimeRanges setting"));
        }

        private void ValidateCompositeFilterSettings(Dictionary<string, object> settings, List<ValidationError> errors)
        {
            if (!settings.ContainsKey("ChildFilters"))
                errors.Add(new ValidationError("Composite filter requires ChildFilters setting"));
            else if (settings["ChildFilters"] is IEnumerable<IAlertFilter> filters && !filters.AsValueEnumerable().Any())
                errors.Add(new ValidationError("Composite filter requires at least one child filter"));
        }

        #endregion

        private void LogInfo(string message, Guid correlationId = default)
        {
            _loggingService?.LogInfo($"[AlertFilterFactory] {message}", correlationId.ToString(), "AlertFilterFactory");
        }

        private void LogError(string message, Guid correlationId = default)
        {
            _loggingService?.LogError($"[AlertFilterFactory] {message}", correlationId.ToString(), "AlertFilterFactory");
        }

        #endregion
    }

}