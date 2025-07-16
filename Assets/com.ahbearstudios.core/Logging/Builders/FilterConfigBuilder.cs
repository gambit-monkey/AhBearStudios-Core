using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;
using FilterMode = AhBearStudios.Core.Logging.Configs.FilterMode;

namespace AhBearStudios.Core.Logging.Builders
{
    /// <summary>
    /// Builder for creating FilterConfig instances with fluent API and validation.
    /// Follows the established AhBearStudios Core builder pattern for complex configuration objects.
    /// </summary>
    public sealed class FilterConfigBuilder
    {
        private string _name;
        private bool _isEnabled = true;
        private string _filterType = "Generic";
        private int _priority = 0;
        private FilterMode _mode = FilterMode.Include;
        private LogLevel _minimumLevel = LogLevel.Debug;
        private LogLevel _maximumLevel = LogLevel.Critical;
        private readonly List<string> _channels = new();
        private readonly List<string> _sourceContexts = new();
        private readonly List<string> _sources = new();
        private readonly List<string> _messagePatterns = new();
        private readonly List<string> _correlationIdPatterns = new();
        private readonly List<int> _threadIds = new();
        private readonly List<string> _userIds = new();
        private readonly List<string> _sessionIds = new();
        private readonly List<string> _machineNames = new();
        private TimeRange _timeRange = default;
        private double _samplingRate = 1.0;
        private int _rateLimit = 0;
        private int _rateLimitWindow = 60;
        private bool _caseSensitive = false;
        private bool _useRegex = false;
        private bool _invert = false;
        private readonly List<string> _customExpressions = new();
        private readonly Dictionary<string, object> _properties = new();
        private readonly List<string> _tags = new();
        private string _description = string.Empty;

        /// <summary>
        /// Private constructor to enforce factory pattern.
        /// </summary>
        private FilterConfigBuilder(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Creates a new FilterConfigBuilder instance.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <returns>A new FilterConfigBuilder instance</returns>
        public static FilterConfigBuilder Create(string name)
        {
            return new FilterConfigBuilder(name);
        }

        /// <summary>
        /// Sets whether the filter is enabled.
        /// </summary>
        /// <param name="isEnabled">Whether the filter is enabled</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithEnabled(bool isEnabled)
        {
            _isEnabled = isEnabled;
            return this;
        }

        /// <summary>
        /// Sets the filter type.
        /// </summary>
        /// <param name="filterType">The filter type identifier</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithFilterType(string filterType)
        {
            _filterType = filterType ?? throw new ArgumentNullException(nameof(filterType));
            return this;
        }

        /// <summary>
        /// Sets the filter priority.
        /// </summary>
        /// <param name="priority">The filter priority (higher = earlier execution)</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithPriority(int priority)
        {
            _priority = priority;
            return this;
        }

        /// <summary>
        /// Sets the filter mode.
        /// </summary>
        /// <param name="mode">The filter mode (Include or Exclude)</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithMode(FilterMode mode)
        {
            _mode = mode;
            return this;
        }

        /// <summary>
        /// Sets the log level range for filtering.
        /// </summary>
        /// <param name="minimumLevel">The minimum log level</param>
        /// <param name="maximumLevel">The maximum log level</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithLevelRange(LogLevel minimumLevel, LogLevel maximumLevel = LogLevel.Critical)
        {
            _minimumLevel = minimumLevel;
            _maximumLevel = maximumLevel;
            return this;
        }

        /// <summary>
        /// Sets the minimum log level for filtering.
        /// </summary>
        /// <param name="minimumLevel">The minimum log level</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithMinimumLevel(LogLevel minimumLevel)
        {
            _minimumLevel = minimumLevel;
            return this;
        }

        /// <summary>
        /// Sets the maximum log level for filtering.
        /// </summary>
        /// <param name="maximumLevel">The maximum log level</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithMaximumLevel(LogLevel maximumLevel)
        {
            _maximumLevel = maximumLevel;
            return this;
        }

        /// <summary>
        /// Adds channels to filter.
        /// </summary>
        /// <param name="channels">The channels to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithChannels(params string[] channels)
        {
            if (channels != null)
            {
                _channels.AddRange(channels.Where(c => !string.IsNullOrEmpty(c)));
            }
            return this;
        }

        /// <summary>
        /// Adds channels to filter.
        /// </summary>
        /// <param name="channels">The channels to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithChannels(IEnumerable<string> channels)
        {
            if (channels != null)
            {
                _channels.AddRange(channels.Where(c => !string.IsNullOrEmpty(c)));
            }
            return this;
        }

        /// <summary>
        /// Adds a single channel to filter.
        /// </summary>
        /// <param name="channel">The channel to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithChannel(string channel)
        {
            if (!string.IsNullOrEmpty(channel))
            {
                _channels.Add(channel);
            }
            return this;
        }

        /// <summary>
        /// Adds source contexts to filter.
        /// </summary>
        /// <param name="sourceContexts">The source contexts to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithSourceContexts(params string[] sourceContexts)
        {
            if (sourceContexts != null)
            {
                _sourceContexts.AddRange(sourceContexts.Where(c => !string.IsNullOrEmpty(c)));
            }
            return this;
        }

        /// <summary>
        /// Adds sources to filter.
        /// </summary>
        /// <param name="sources">The sources to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithSources(params string[] sources)
        {
            if (sources != null)
            {
                _sources.AddRange(sources.Where(s => !string.IsNullOrEmpty(s)));
            }
            return this;
        }

        /// <summary>
        /// Adds message patterns to filter.
        /// </summary>
        /// <param name="patterns">The message patterns to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithMessagePatterns(params string[] patterns)
        {
            if (patterns != null)
            {
                _messagePatterns.AddRange(patterns.Where(p => !string.IsNullOrEmpty(p)));
            }
            return this;
        }

        /// <summary>
        /// Adds a single message pattern to filter.
        /// </summary>
        /// <param name="pattern">The message pattern to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithMessagePattern(string pattern)
        {
            if (!string.IsNullOrEmpty(pattern))
            {
                _messagePatterns.Add(pattern);
            }
            return this;
        }

        /// <summary>
        /// Adds correlation ID patterns to filter.
        /// </summary>
        /// <param name="patterns">The correlation ID patterns to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithCorrelationIdPatterns(params string[] patterns)
        {
            if (patterns != null)
            {
                _correlationIdPatterns.AddRange(patterns.Where(p => !string.IsNullOrEmpty(p)));
            }
            return this;
        }

        /// <summary>
        /// Adds thread IDs to filter.
        /// </summary>
        /// <param name="threadIds">The thread IDs to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithThreadIds(params int[] threadIds)
        {
            if (threadIds != null)
            {
                _threadIds.AddRange(threadIds);
            }
            return this;
        }

        /// <summary>
        /// Adds user IDs to filter.
        /// </summary>
        /// <param name="userIds">The user IDs to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithUserIds(params string[] userIds)
        {
            if (userIds != null)
            {
                _userIds.AddRange(userIds.Where(u => !string.IsNullOrEmpty(u)));
            }
            return this;
        }

        /// <summary>
        /// Adds session IDs to filter.
        /// </summary>
        /// <param name="sessionIds">The session IDs to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithSessionIds(params string[] sessionIds)
        {
            if (sessionIds != null)
            {
                _sessionIds.AddRange(sessionIds.Where(s => !string.IsNullOrEmpty(s)));
            }
            return this;
        }

        /// <summary>
        /// Adds machine names to filter.
        /// </summary>
        /// <param name="machineNames">The machine names to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithMachineNames(params string[] machineNames)
        {
            if (machineNames != null)
            {
                _machineNames.AddRange(machineNames.Where(m => !string.IsNullOrEmpty(m)));
            }
            return this;
        }

        /// <summary>
        /// Sets the time range for filtering.
        /// </summary>
        /// <param name="startTime">The start time</param>
        /// <param name="endTime">The end time</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithTimeRange(DateTime startTime, DateTime endTime)
        {
            _timeRange = new TimeRange(startTime, endTime);
            return this;
        }

        /// <summary>
        /// Sets the time range for filtering.
        /// </summary>
        /// <param name="timeRange">The time range</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithTimeRange(TimeRange timeRange)
        {
            _timeRange = timeRange;
            return this;
        }

        /// <summary>
        /// Sets the sampling rate for filtering.
        /// </summary>
        /// <param name="samplingRate">The sampling rate (0.0 to 1.0)</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithSampling(double samplingRate)
        {
            _samplingRate = Math.Max(0.0, Math.Min(1.0, samplingRate));
            return this;
        }

        /// <summary>
        /// Sets the rate limit for filtering.
        /// </summary>
        /// <param name="rateLimit">The rate limit (messages per second)</param>
        /// <param name="window">The rate limit window in seconds</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithRateLimit(int rateLimit, int window = 60)
        {
            _rateLimit = Math.Max(0, rateLimit);
            _rateLimitWindow = Math.Max(1, window);
            return this;
        }

        /// <summary>
        /// Sets whether to use case-sensitive matching.
        /// </summary>
        /// <param name="caseSensitive">Whether to use case-sensitive matching</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithCaseSensitive(bool caseSensitive = true)
        {
            _caseSensitive = caseSensitive;
            return this;
        }

        /// <summary>
        /// Sets whether to use regex matching for patterns.
        /// </summary>
        /// <param name="useRegex">Whether to use regex matching</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithRegex(bool useRegex = true)
        {
            _useRegex = useRegex;
            return this;
        }

        /// <summary>
        /// Sets whether to invert the filter logic.
        /// </summary>
        /// <param name="invert">Whether to invert the filter logic</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithInvert(bool invert = true)
        {
            _invert = invert;
            return this;
        }

        /// <summary>
        /// Adds custom expressions to the filter.
        /// </summary>
        /// <param name="expressions">The custom expressions to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithCustomExpressions(params string[] expressions)
        {
            if (expressions != null)
            {
                _customExpressions.AddRange(expressions.Where(e => !string.IsNullOrEmpty(e)));
            }
            return this;
        }

        /// <summary>
        /// Adds properties to the filter configuration.
        /// </summary>
        /// <param name="properties">The properties to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithProperties(IReadOnlyDictionary<string, object> properties)
        {
            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    _properties[kvp.Key] = kvp.Value;
                }
            }
            return this;
        }

        /// <summary>
        /// Adds a single property to the filter configuration.
        /// </summary>
        /// <param name="key">The property key</param>
        /// <param name="value">The property value</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithProperty(string key, object value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _properties[key] = value;
            }
            return this;
        }

        /// <summary>
        /// Adds tags to the filter.
        /// </summary>
        /// <param name="tags">The tags to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithTags(params string[] tags)
        {
            if (tags != null)
            {
                _tags.AddRange(tags.Where(t => !string.IsNullOrEmpty(t)));
            }
            return this;
        }

        /// <summary>
        /// Sets the filter description.
        /// </summary>
        /// <param name="description">The filter description</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder WithDescription(string description)
        {
            _description = description ?? string.Empty;
            return this;
        }

        /// <summary>
        /// Configures the filter for level-based filtering.
        /// </summary>
        /// <param name="minimumLevel">The minimum log level</param>
        /// <param name="maximumLevel">The maximum log level</param>
        /// <param name="mode">The filter mode</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder ForLevel(LogLevel minimumLevel, LogLevel maximumLevel = LogLevel.Critical, FilterMode mode = FilterMode.Include)
        {
            return WithFilterType("Level")
                .WithMode(mode)
                .WithLevelRange(minimumLevel, maximumLevel)
                .WithPriority(1000)
                .WithDescription($"Filter for log levels {minimumLevel} to {maximumLevel}");
        }

        /// <summary>
        /// Configures the filter for source-based filtering.
        /// </summary>
        /// <param name="sources">The sources to filter</param>
        /// <param name="mode">The filter mode</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder ForSource(string[] sources, FilterMode mode = FilterMode.Include)
        {
            return WithFilterType("Source")
                .WithMode(mode)
                .WithSources(sources)
                .WithPriority(900)
                .WithDescription($"Filter for sources: {string.Join(", ", sources)}");
        }

        /// <summary>
        /// Configures the filter for channel-based filtering.
        /// </summary>
        /// <param name="channels">The channels to filter</param>
        /// <param name="mode">The filter mode</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder ForChannel(string[] channels, FilterMode mode = FilterMode.Include)
        {
            return WithFilterType("Channel")
                .WithMode(mode)
                .WithChannels(channels)
                .WithPriority(800)
                .WithDescription($"Filter for channels: {string.Join(", ", channels)}");
        }

        /// <summary>
        /// Configures the filter for pattern-based filtering.
        /// </summary>
        /// <param name="patterns">The message patterns to filter</param>
        /// <param name="useRegex">Whether to use regex matching</param>
        /// <param name="mode">The filter mode</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder ForPattern(string[] patterns, bool useRegex = false, FilterMode mode = FilterMode.Include)
        {
            return WithFilterType("Pattern")
                .WithMode(mode)
                .WithMessagePatterns(patterns)
                .WithRegex(useRegex)
                .WithPriority(700)
                .WithDescription($"Filter for message patterns: {string.Join(", ", patterns)}");
        }

        /// <summary>
        /// Configures the filter for sampling.
        /// </summary>
        /// <param name="samplingRate">The sampling rate (0.0 to 1.0)</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder ForSampling(double samplingRate)
        {
            return WithFilterType("Sampling")
                .WithMode(FilterMode.Include)
                .WithSampling(samplingRate)
                .WithPriority(100)
                .WithDescription($"Sampling filter with rate {samplingRate:P1}");
        }

        /// <summary>
        /// Configures the filter for rate limiting.
        /// </summary>
        /// <param name="rateLimit">The rate limit (messages per second)</param>
        /// <param name="window">The rate limit window in seconds</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder ForRateLimit(int rateLimit, int window = 60)
        {
            return WithFilterType("RateLimit")
                .WithMode(FilterMode.Include)
                .WithRateLimit(rateLimit, window)
                .WithPriority(50)
                .WithDescription($"Rate limit filter: {rateLimit} messages per {window} seconds");
        }

        /// <summary>
        /// Configures the filter for time-based filtering.
        /// </summary>
        /// <param name="startTime">The start time</param>
        /// <param name="endTime">The end time</param>
        /// <param name="mode">The filter mode</param>
        /// <returns>This builder instance for method chaining</returns>
        public FilterConfigBuilder ForTimeRange(DateTime startTime, DateTime endTime, FilterMode mode = FilterMode.Include)
        {
            return WithFilterType("TimeRange")
                .WithMode(mode)
                .WithTimeRange(startTime, endTime)
                .WithPriority(600)
                .WithDescription($"Time range filter: {startTime:yyyy-MM-dd HH:mm:ss} to {endTime:yyyy-MM-dd HH:mm:ss}");
        }

        /// <summary>
        /// Validates the current configuration.
        /// </summary>
        /// <returns>A validation result</returns>
        public ValidationResult Validate()
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();

            if (string.IsNullOrWhiteSpace(_name))
                errors.Add(new ValidationError("Filter name cannot be empty", nameof(_name)));

            if (_samplingRate < 0.0 || _samplingRate > 1.0)
                errors.Add(new ValidationError("Sampling rate must be between 0.0 and 1.0", nameof(_samplingRate)));

            if (_rateLimit < 0)
                errors.Add(new ValidationError("Rate limit cannot be negative", nameof(_rateLimit)));

            if (_rateLimitWindow < 1)
                errors.Add(new ValidationError("Rate limit window must be at least 1 second", nameof(_rateLimitWindow)));

            if (_minimumLevel > _maximumLevel)
                errors.Add(new ValidationError("Minimum level cannot be greater than maximum level", nameof(_minimumLevel)));

            if (_useRegex && _messagePatterns.Count > 0)
            {
                foreach (var pattern in _messagePatterns)
                {
                    try
                    {
                        _ = new Regex(pattern);
                    }
                    catch (ArgumentException)
                    {
                        errors.Add(new ValidationError($"Invalid regex pattern: {pattern}", nameof(_messagePatterns)));
                    }
                }
            }

            if (_samplingRate < 1.0)
                warnings.Add(new ValidationWarning($"Sampling rate {_samplingRate:P1} will drop some messages", nameof(_samplingRate)));

            if (_rateLimit > 0 && _rateLimit < 10)
                warnings.Add(new ValidationWarning("Low rate limit may cause significant message drops", nameof(_rateLimit)));

            return errors.Count > 0 
                ? ValidationResult.Failure(errors, _name, warnings)
                : ValidationResult.Success(_name, warnings);
        }

        /// <summary>
        /// Builds the FilterConfig instance.
        /// </summary>
        /// <returns>A configured FilterConfig instance</returns>
        /// <exception cref="ValidationException">Thrown when validation fails</exception>
        public FilterConfig Build()
        {
            var validationResult = Validate();
            validationResult.ThrowIfInvalid();

            return new FilterConfig(
                name: _name,
                isEnabled: _isEnabled,
                filterType: _filterType,
                priority: _priority,
                mode: _mode,
                minimumLevel: _minimumLevel,
                maximumLevel: _maximumLevel,
                channels: _channels.AsReadOnly(),
                sourceContexts: _sourceContexts.AsReadOnly(),
                sources: _sources.AsReadOnly(),
                messagePatterns: _messagePatterns.AsReadOnly(),
                correlationIdPatterns: _correlationIdPatterns.AsReadOnly(),
                threadIds: _threadIds.AsReadOnly(),
                userIds: _userIds.AsReadOnly(),
                sessionIds: _sessionIds.AsReadOnly(),
                machineNames: _machineNames.AsReadOnly(),
                timeRange: _timeRange,
                samplingRate: _samplingRate,
                rateLimit: _rateLimit,
                rateLimitWindow: _rateLimitWindow,
                caseSensitive: _caseSensitive,
                useRegex: _useRegex,
                invert: _invert,
                customExpressions: _customExpressions.AsReadOnly(),
                properties: _properties,
                tags: _tags.AsReadOnly(),
                description: _description);
        }

        /// <summary>
        /// Builds the FilterConfig instance with validation bypass.
        /// </summary>
        /// <returns>A configured FilterConfig instance</returns>
        public FilterConfig BuildUnsafe()
        {
            return new FilterConfig(
                name: _name,
                isEnabled: _isEnabled,
                filterType: _filterType,
                priority: _priority,
                mode: _mode,
                minimumLevel: _minimumLevel,
                maximumLevel: _maximumLevel,
                channels: _channels.AsReadOnly(),
                sourceContexts: _sourceContexts.AsReadOnly(),
                sources: _sources.AsReadOnly(),
                messagePatterns: _messagePatterns.AsReadOnly(),
                correlationIdPatterns: _correlationIdPatterns.AsReadOnly(),
                threadIds: _threadIds.AsReadOnly(),
                userIds: _userIds.AsReadOnly(),
                sessionIds: _sessionIds.AsReadOnly(),
                machineNames: _machineNames.AsReadOnly(),
                timeRange: _timeRange,
                samplingRate: _samplingRate,
                rateLimit: _rateLimit,
                rateLimitWindow: _rateLimitWindow,
                caseSensitive: _caseSensitive,
                useRegex: _useRegex,
                invert: _invert,
                customExpressions: _customExpressions.AsReadOnly(),
                properties: _properties,
                tags: _tags.AsReadOnly(),
                description: _description);
        }
    }
}