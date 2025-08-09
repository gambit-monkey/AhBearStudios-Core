using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Logging.Models;
using FilterMode = AhBearStudios.Core.Logging.Models.FilterMode;

namespace AhBearStudios.Core.Logging.Configs
{
    /// <summary>
    /// Configuration record for log filters with comprehensive filtering options.
    /// Provides immutable configuration for filter behavior and conditional processing.
    /// </summary>
    public sealed record FilterConfig
    {
        /// <summary>
        /// Gets the filter name for identification.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Gets whether the filter is enabled.
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets the filter type identifier.
        /// </summary>
        public string FilterType { get; init; }

        /// <summary>
        /// Gets the filter priority for execution order (higher = earlier execution).
        /// </summary>
        public int Priority { get; init; }

        /// <summary>
        /// Gets the filter mode (Include or Exclude).
        /// </summary>
        public FilterMode Mode { get; init; }

        /// <summary>
        /// Gets the minimum log level for this filter.
        /// </summary>
        public LogLevel MinimumLevel { get; init; }

        /// <summary>
        /// Gets the maximum log level for this filter.
        /// </summary>
        public LogLevel MaximumLevel { get; init; }

        /// <summary>
        /// Gets the channels that this filter applies to.
        /// Empty means all channels.
        /// </summary>
        public IReadOnlyList<string> Channels { get; init; }

        /// <summary>
        /// Gets the source contexts that this filter applies to.
        /// Empty means all contexts.
        /// </summary>
        public IReadOnlyList<string> SourceContexts { get; init; }

        /// <summary>
        /// Gets the source systems that this filter applies to.
        /// Empty means all sources.
        /// </summary>
        public IReadOnlyList<string> Sources { get; init; }

        /// <summary>
        /// Gets the message patterns (regex) that this filter applies to.
        /// Empty means all messages.
        /// </summary>
        public IReadOnlyList<string> MessagePatterns { get; init; }

        /// <summary>
        /// Gets the correlation ID patterns that this filter applies to.
        /// Empty means all correlation IDs.
        /// </summary>
        public IReadOnlyList<string> CorrelationIdPatterns { get; init; }

        /// <summary>
        /// Gets the thread IDs that this filter applies to.
        /// Empty means all threads.
        /// </summary>
        public IReadOnlyList<int> ThreadIds { get; init; }

        /// <summary>
        /// Gets the user IDs that this filter applies to.
        /// Empty means all users.
        /// </summary>
        public IReadOnlyList<string> UserIds { get; init; }

        /// <summary>
        /// Gets the session IDs that this filter applies to.
        /// Empty means all sessions.
        /// </summary>
        public IReadOnlyList<string> SessionIds { get; init; }

        /// <summary>
        /// Gets the machine names that this filter applies to.
        /// Empty means all machines.
        /// </summary>
        public IReadOnlyList<string> MachineNames { get; init; }

        /// <summary>
        /// Gets the time range for this filter.
        /// </summary>
        public TimeRange TimeRange { get; init; }

        /// <summary>
        /// Gets the sampling rate (0.0 to 1.0) for this filter.
        /// 1.0 means all messages, 0.5 means 50% of messages.
        /// </summary>
        public double SamplingRate { get; init; }

        /// <summary>
        /// Gets the rate limit for this filter (messages per second).
        /// 0 means no rate limiting.
        /// </summary>
        public int RateLimit { get; init; }

        /// <summary>
        /// Gets the rate limit window in seconds.
        /// </summary>
        public int RateLimitWindow { get; init; }

        /// <summary>
        /// Gets whether to use case-sensitive matching for string comparisons.
        /// </summary>
        public bool CaseSensitive { get; init; }

        /// <summary>
        /// Gets whether to use regex matching for patterns.
        /// </summary>
        public bool UseRegex { get; init; }

        /// <summary>
        /// Gets whether to invert the filter logic.
        /// </summary>
        public bool Invert { get; init; }

        /// <summary>
        /// Gets the custom filter expressions.
        /// </summary>
        public IReadOnlyList<string> CustomExpressions { get; init; }

        /// <summary>
        /// Gets additional filter-specific properties.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; init; }

        /// <summary>
        /// Gets the tags for filter categorization.
        /// </summary>
        public IReadOnlyList<string> Tags { get; init; }

        /// <summary>
        /// Gets the filter description.
        /// </summary>
        public string Description { get; init; }

        /// <summary>
        /// Initializes a new instance of the FilterConfig record.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="isEnabled">Whether the filter is enabled</param>
        /// <param name="filterType">The filter type identifier</param>
        /// <param name="priority">The filter priority</param>
        /// <param name="mode">The filter mode</param>
        /// <param name="minimumLevel">The minimum log level</param>
        /// <param name="maximumLevel">The maximum log level</param>
        /// <param name="channels">The channels to apply to</param>
        /// <param name="sourceContexts">The source contexts to apply to</param>
        /// <param name="sources">The sources to apply to</param>
        /// <param name="messagePatterns">The message patterns to match</param>
        /// <param name="correlationIdPatterns">The correlation ID patterns to match</param>
        /// <param name="threadIds">The thread IDs to apply to</param>
        /// <param name="userIds">The user IDs to apply to</param>
        /// <param name="sessionIds">The session IDs to apply to</param>
        /// <param name="machineNames">The machine names to apply to</param>
        /// <param name="timeRange">The time range to apply</param>
        /// <param name="samplingRate">The sampling rate</param>
        /// <param name="rateLimit">The rate limit</param>
        /// <param name="rateLimitWindow">The rate limit window</param>
        /// <param name="caseSensitive">Whether to use case-sensitive matching</param>
        /// <param name="useRegex">Whether to use regex matching</param>
        /// <param name="invert">Whether to invert the filter logic</param>
        /// <param name="customExpressions">Custom filter expressions</param>
        /// <param name="properties">Additional properties</param>
        /// <param name="tags">Filter tags</param>
        /// <param name="description">Filter description</param>
        public FilterConfig(
            string name,
            bool isEnabled = true,
            string filterType = "Generic",
            int priority = 0,
            FilterMode mode = FilterMode.Include,
            LogLevel minimumLevel = LogLevel.Debug,
            LogLevel maximumLevel = LogLevel.Critical,
            IReadOnlyList<string> channels = null,
            IReadOnlyList<string> sourceContexts = null,
            IReadOnlyList<string> sources = null,
            IReadOnlyList<string> messagePatterns = null,
            IReadOnlyList<string> correlationIdPatterns = null,
            IReadOnlyList<int> threadIds = null,
            IReadOnlyList<string> userIds = null,
            IReadOnlyList<string> sessionIds = null,
            IReadOnlyList<string> machineNames = null,
            TimeRange timeRange = default,
            double samplingRate = 1.0,
            int rateLimit = 0,
            int rateLimitWindow = 60,
            bool caseSensitive = false,
            bool useRegex = false,
            bool invert = false,
            IReadOnlyList<string> customExpressions = null,
            IReadOnlyDictionary<string, object> properties = null,
            IReadOnlyList<string> tags = null,
            string description = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            IsEnabled = isEnabled;
            FilterType = filterType ?? "Generic";
            Priority = priority;
            Mode = mode;
            MinimumLevel = minimumLevel;
            MaximumLevel = maximumLevel;
            Channels = channels ?? Array.Empty<string>();
            SourceContexts = sourceContexts ?? Array.Empty<string>();
            Sources = sources ?? Array.Empty<string>();
            MessagePatterns = messagePatterns ?? Array.Empty<string>();
            CorrelationIdPatterns = correlationIdPatterns ?? Array.Empty<string>();
            ThreadIds = threadIds ?? Array.Empty<int>();
            UserIds = userIds ?? Array.Empty<string>();
            SessionIds = sessionIds ?? Array.Empty<string>();
            MachineNames = machineNames ?? Array.Empty<string>();
            TimeRange = timeRange;
            SamplingRate = Math.Max(0.0, Math.Min(1.0, samplingRate));
            RateLimit = Math.Max(0, rateLimit);
            RateLimitWindow = Math.Max(1, rateLimitWindow);
            CaseSensitive = caseSensitive;
            UseRegex = useRegex;
            Invert = invert;
            CustomExpressions = customExpressions ?? Array.Empty<string>();
            Properties = properties ?? new Dictionary<string, object>();
            Tags = tags ?? Array.Empty<string>();
            Description = description ?? string.Empty;
        }

        /// <summary>
        /// Creates a filter configuration for level-based filtering.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="minimumLevel">The minimum log level</param>
        /// <param name="maximumLevel">The maximum log level</param>
        /// <param name="mode">The filter mode</param>
        /// <returns>A level-based FilterConfig</returns>
        public static FilterConfig ForLevel(
            string name,
            LogLevel minimumLevel,
            LogLevel maximumLevel = LogLevel.Critical,
            FilterMode mode = FilterMode.Include)
        {
            return new FilterConfig(
                name: name,
                filterType: "Level",
                mode: mode,
                minimumLevel: minimumLevel,
                maximumLevel: maximumLevel,
                priority: 1000,
                description: $"Filter for log levels {minimumLevel} to {maximumLevel}");
        }

        /// <summary>
        /// Creates a filter configuration for source-based filtering.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="sources">The sources to filter</param>
        /// <param name="mode">The filter mode</param>
        /// <returns>A source-based FilterConfig</returns>
        public static FilterConfig ForSource(
            string name,
            IReadOnlyList<string> sources,
            FilterMode mode = FilterMode.Include)
        {
            return new FilterConfig(
                name: name,
                filterType: "Source",
                mode: mode,
                sources: sources,
                priority: 900,
                description: $"Filter for sources: {string.Join(", ", sources)}");
        }

        /// <summary>
        /// Creates a filter configuration for channel-based filtering.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="channels">The channels to filter</param>
        /// <param name="mode">The filter mode</param>
        /// <returns>A channel-based FilterConfig</returns>
        public static FilterConfig ForChannel(
            string name,
            IReadOnlyList<string> channels,
            FilterMode mode = FilterMode.Include)
        {
            return new FilterConfig(
                name: name,
                filterType: "Channel",
                mode: mode,
                channels: channels,
                priority: 800,
                description: $"Filter for channels: {string.Join(", ", channels)}");
        }

        /// <summary>
        /// Creates a filter configuration for message pattern filtering.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="patterns">The message patterns to filter</param>
        /// <param name="useRegex">Whether to use regex matching</param>
        /// <param name="mode">The filter mode</param>
        /// <returns>A pattern-based FilterConfig</returns>
        public static FilterConfig ForPattern(
            string name,
            IReadOnlyList<string> patterns,
            bool useRegex = false,
            FilterMode mode = FilterMode.Include)
        {
            return new FilterConfig(
                name: name,
                filterType: "Pattern",
                mode: mode,
                messagePatterns: patterns,
                useRegex: useRegex,
                priority: 700,
                description: $"Filter for message patterns: {string.Join(", ", patterns)}");
        }

        /// <summary>
        /// Creates a filter configuration for sampling.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="samplingRate">The sampling rate (0.0 to 1.0)</param>
        /// <returns>A sampling FilterConfig</returns>
        public static FilterConfig ForSampling(
            string name,
            double samplingRate)
        {
            return new FilterConfig(
                name: name,
                filterType: "Sampling",
                mode: FilterMode.Include,
                samplingRate: samplingRate,
                priority: 100,
                description: $"Sampling filter with rate {samplingRate:P1}");
        }

        /// <summary>
        /// Creates a filter configuration for rate limiting.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="rateLimit">The rate limit (messages per second)</param>
        /// <param name="window">The rate limit window in seconds</param>
        /// <returns>A rate-limiting FilterConfig</returns>
        public static FilterConfig ForRateLimit(
            string name,
            int rateLimit,
            int window = 60)
        {
            return new FilterConfig(
                name: name,
                filterType: "RateLimit",
                mode: FilterMode.Include,
                rateLimit: rateLimit,
                rateLimitWindow: window,
                priority: 50,
                description: $"Rate limit filter: {rateLimit} messages per {window} seconds");
        }

        /// <summary>
        /// Creates a filter configuration for time-based filtering.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="startTime">The start time</param>
        /// <param name="endTime">The end time</param>
        /// <param name="mode">The filter mode</param>
        /// <returns>A time-based FilterConfig</returns>
        public static FilterConfig ForTimeRange(
            string name,
            DateTime startTime,
            DateTime endTime,
            FilterMode mode = FilterMode.Include)
        {
            return new FilterConfig(
                name: name,
                filterType: "TimeRange",
                mode: mode,
                timeRange: new TimeRange(startTime, endTime),
                priority: 600,
                description: $"Time range filter: {startTime:yyyy-MM-dd HH:mm:ss} to {endTime:yyyy-MM-dd HH:mm:ss}");
        }

        /// <summary>
        /// Validates the filter configuration.
        /// </summary>
        /// <returns>A validation result</returns>
        public ValidationResult Validate()
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add(new ValidationError("Filter name cannot be empty", nameof(Name)));

            if (SamplingRate < 0.0 || SamplingRate > 1.0)
                errors.Add(new ValidationError("Sampling rate must be between 0.0 and 1.0", nameof(SamplingRate)));

            if (RateLimit < 0)
                errors.Add(new ValidationError("Rate limit cannot be negative", nameof(RateLimit)));

            if (RateLimitWindow < 1)
                errors.Add(new ValidationError("Rate limit window must be at least 1 second", nameof(RateLimitWindow)));

            if (MinimumLevel > MaximumLevel)
                errors.Add(new ValidationError("Minimum level cannot be greater than maximum level", nameof(MinimumLevel)));

            if (UseRegex && MessagePatterns.Count > 0)
            {
                foreach (var pattern in MessagePatterns)
                {
                    try
                    {
                        _ = new System.Text.RegularExpressions.Regex(pattern);
                    }
                    catch (ArgumentException)
                    {
                        errors.Add(new ValidationError($"Invalid regex pattern: {pattern}", nameof(MessagePatterns)));
                    }
                }
            }

            if (SamplingRate < 1.0)
                warnings.Add(new ValidationWarning($"Sampling rate {SamplingRate:P1} will drop some messages", nameof(SamplingRate)));

            if (RateLimit > 0 && RateLimit < 10)
                warnings.Add(new ValidationWarning("Low rate limit may cause significant message drops", nameof(RateLimit)));

            return errors.Count > 0 
                ? ValidationResult.Failure(errors, Name, warnings)
                : ValidationResult.Success(Name, warnings);
        }
    }
}