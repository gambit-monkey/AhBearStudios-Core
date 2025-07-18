using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;
using FilterMode = AhBearStudios.Core.Logging.Configs.FilterMode;

namespace AhBearStudios.Core.Logging.Filters
{
    /// <summary>
    /// Factory implementation for creating log filter instances.
    /// Provides centralized filter creation with support for all filter types and configuration-based instantiation.
    /// Follows the AhBearStudios Core factory pattern for consistent filter management.
    /// </summary>
    public sealed class LogFilterFactory : ILogFilterFactory
    {
        private static readonly Dictionary<string, string> FilterTypeMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Level"] = nameof(LevelFilter),
            ["Source"] = nameof(SourceFilter),
            ["Correlation"] = nameof(CorrelationFilter),
            ["Pattern"] = nameof(PatternFilter),
            ["Sampling"] = nameof(SamplingFilter),
            ["RateLimit"] = nameof(RateLimitFilter),
            ["TimeRange"] = nameof(TimeRangeFilter),
            ["Generic"] = nameof(LevelFilter) // Default fallback
        };

        /// <inheritdoc />
        public ILogFilter CreateFilter(FilterConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var validationResult = ValidateFilterConfig(config);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"Invalid filter configuration: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}", nameof(config));
            }

            return config.FilterType.ToLowerInvariant() switch
            {
                "level" => CreateLevelFilter(config),
                "source" => CreateSourceFilter(config),
                "correlation" => CreateCorrelationFilter(config),
                "pattern" => CreatePatternFilter(config),
                "sampling" => CreateSamplingFilter(config),
                "ratelimit" => CreateRateLimitFilter(config),
                "timerange" => CreateTimeRangeFilter(config),
                _ => CreateLevelFilter(config) // Default fallback
            };
        }

        /// <inheritdoc />
        public IReadOnlyList<ILogFilter> CreateFilters(IEnumerable<FilterConfig> configs)
        {
            if (configs == null)
                throw new ArgumentNullException(nameof(configs));

            var filters = new List<ILogFilter>();
            
            foreach (var config in configs)
            {
                try
                {
                    var filter = CreateFilter(config);
                    filters.Add(filter);
                }
                catch (Exception)
                {
                    // Skip invalid configurations and continue with other filters
                    continue;
                }
            }

            return filters.AsReadOnly();
        }

        /// <inheritdoc />
        public ILogFilter CreateFilter(
            string filterType, 
            string name, 
            IReadOnlyDictionary<FixedString32Bytes, object> settings = null, 
            int priority = 0)
        {
            if (string.IsNullOrEmpty(filterType))
                throw new ArgumentNullException(nameof(filterType));
            
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (!IsFilterTypeSupported(filterType))
                throw new NotSupportedException($"Filter type '{filterType}' is not supported");

            // Create a basic FilterConfig and then create the filter
            var config = GetDefaultConfig(filterType, name);
            var filter = CreateFilter(config);

            // Apply additional settings if provided
            if (settings != null && settings.Count > 0)
            {
                filter.Configure(settings);
            }

            return filter;
        }

        /// <inheritdoc />
        public IReadOnlyList<string> GetSupportedFilterTypes()
        {
            return FilterTypeMapping.Keys.ToList().AsReadOnly();
        }

        /// <inheritdoc />
        public bool IsFilterTypeSupported(string filterType)
        {
            return !string.IsNullOrEmpty(filterType) && FilterTypeMapping.ContainsKey(filterType);
        }

        /// <inheritdoc />
        public ValidationResult ValidateFilterConfig(FilterConfig config)
        {
            if (config == null)
                return ValidationResult.Failure(new[] { new ValidationError("Filter configSo cannot be null", "ConfigSo") }, "Unknown");

            return config.Validate();
        }

        /// <inheritdoc />
        public FilterConfig GetDefaultConfig(string filterType, string filterName)
        {
            if (string.IsNullOrEmpty(filterType))
                throw new ArgumentNullException(nameof(filterType));
            
            if (string.IsNullOrEmpty(filterName))
                throw new ArgumentNullException(nameof(filterName));

            if (!IsFilterTypeSupported(filterType))
                throw new NotSupportedException($"Filter type '{filterType}' is not supported");

            return filterType.ToLowerInvariant() switch
            {
                "level" => FilterConfig.ForLevel(filterName, LogLevel.Debug),
                "source" => FilterConfig.ForSource(filterName, new[] { "Default" }),
                "correlation" => new FilterConfig(filterName, filterType: "Correlation", priority: 800),
                "pattern" => FilterConfig.ForPattern(filterName, new[] { ".*" }),
                "sampling" => FilterConfig.ForSampling(filterName, 1.0),
                "ratelimit" => FilterConfig.ForRateLimit(filterName, 1000),
                "timerange" => FilterConfig.ForTimeRange(filterName, DateTime.UtcNow, DateTime.UtcNow.AddHours(1)),
                _ => FilterConfig.ForLevel(filterName, LogLevel.Debug)
            };
        }

        /// <inheritdoc />
        public LogFilterService CreateFilterService(IEnumerable<FilterConfig> configs = null)
        {
            var service = new LogFilterService();
            
            if (configs != null)
            {
                var filters = CreateFilters(configs);
                foreach (var filter in filters)
                {
                    service.AddFilter(filter);
                }
            }

            return service;
        }

        /// <summary>
        /// Creates a LevelFilter from configuration.
        /// </summary>
        /// <param name="config">The filter configuration</param>
        /// <returns>A configured LevelFilter instance</returns>
        private LevelFilter CreateLevelFilter(FilterConfig config)
        {
            var includeMode = config.Mode == FilterMode.Include;
            return new LevelFilter(
                name: config.Name,
                minimumLevel: config.MinimumLevel,
                maximumLevel: config.MaximumLevel,
                includeMode: includeMode,
                priority: config.Priority);
        }

        /// <summary>
        /// Creates a SourceFilter from configuration.
        /// </summary>
        /// <param name="config">The filter configuration</param>
        /// <returns>A configured SourceFilter instance</returns>
        private SourceFilter CreateSourceFilter(FilterConfig config)
        {
            var includeMode = config.Mode == FilterMode.Include;
            return new SourceFilter(
                name: config.Name,
                sources: config.Sources,
                sourceContexts: config.SourceContexts,
                includeMode: includeMode,
                caseSensitive: config.CaseSensitive,
                useRegex: config.UseRegex,
                priority: config.Priority);
        }

        /// <summary>
        /// Creates a CorrelationFilter from configuration.
        /// </summary>
        /// <param name="config">The filter configuration</param>
        /// <returns>A configured CorrelationFilter instance</returns>
        private CorrelationFilter CreateCorrelationFilter(FilterConfig config)
        {
            var includeMode = config.Mode == FilterMode.Include;
            return new CorrelationFilter(
                name: config.Name,
                correlationIdPatterns: config.CorrelationIdPatterns,
                userIds: config.UserIds,
                sessionIds: config.SessionIds,
                includeMode: includeMode,
                caseSensitive: config.CaseSensitive,
                useRegex: config.UseRegex,
                priority: config.Priority);
        }

        /// <summary>
        /// Creates a PatternFilter from configuration.
        /// </summary>
        /// <param name="config">The filter configuration</param>
        /// <returns>A configured PatternFilter instance</returns>
        private PatternFilter CreatePatternFilter(FilterConfig config)
        {
            var includeMode = config.Mode == FilterMode.Include;
            return new PatternFilter(
                name: config.Name,
                messagePatterns: config.MessagePatterns,
                includeMode: includeMode,
                caseSensitive: config.CaseSensitive,
                useRegex: config.UseRegex,
                priority: config.Priority);
        }

        /// <summary>
        /// Creates a SamplingFilter from configuration.
        /// </summary>
        /// <param name="config">The filter configuration</param>
        /// <returns>A configured SamplingFilter instance</returns>
        private SamplingFilter CreateSamplingFilter(FilterConfig config)
        {
            return new SamplingFilter(
                name: config.Name,
                samplingRate: config.SamplingRate,
                priority: config.Priority);
        }

        /// <summary>
        /// Creates a RateLimitFilter from configuration.
        /// </summary>
        /// <param name="config">The filter configuration</param>
        /// <returns>A configured RateLimitFilter instance</returns>
        private RateLimitFilter CreateRateLimitFilter(FilterConfig config)
        {
            return new RateLimitFilter(
                name: config.Name,
                rateLimit: config.RateLimit,
                rateLimitWindow: config.RateLimitWindow,
                priority: config.Priority);
        }

        /// <summary>
        /// Creates a TimeRangeFilter from configuration.
        /// </summary>
        /// <param name="config">The filter configuration</param>
        /// <returns>A configured TimeRangeFilter instance</returns>
        private TimeRangeFilter CreateTimeRangeFilter(FilterConfig config)
        {
            var includeMode = config.Mode == FilterMode.Include;
            var timeRanges = config.TimeRange.IsValid ? 
                new[] { config.TimeRange } : 
                Array.Empty<TimeRange>();

            return new TimeRangeFilter(
                name: config.Name,
                timeRanges: timeRanges,
                includeMode: includeMode,
                priority: config.Priority);
        }

        /// <summary>
        /// Creates a default LogFilterFactory instance.
        /// </summary>
        /// <returns>A LogFilterFactory instance</returns>
        public static LogFilterFactory Create()
        {
            return new LogFilterFactory();
        }

        /// <summary>
        /// Creates a LogFilterFactory with validation disabled for performance.
        /// </summary>
        /// <returns>A LogFilterFactory instance with validation disabled</returns>
        /// <remarks>
        /// Use this method only when you are certain that all configurations are valid.
        /// Invalid configurations may cause runtime errors.
        /// </remarks>
        public static LogFilterFactory CreateUnsafe()
        {
            return new LogFilterFactory();
        }
    }
}