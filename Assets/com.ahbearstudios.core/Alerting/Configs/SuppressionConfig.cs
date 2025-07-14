using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Configs
{
    /// <summary>
    /// Configuration class for alert suppression rules and intelligent filtering.
    /// Provides sophisticated suppression mechanisms including duplicate detection, rate limiting,
    /// business hours filtering, and custom rule-based suppression to prevent alert flooding.
    /// </summary>
    public record SuppressionConfig
    {
        /// <summary>
        /// Gets the unique name identifier for this suppression rule.
        /// Must be unique across all suppression rules in the alert system.
        /// </summary>
        [Required]
        [StringLength(64, MinimumLength = 1)]
        public FixedString64Bytes RuleName { get; init; }

        /// <summary>
        /// Gets whether this suppression rule is enabled.
        /// Disabled rules are skipped during alert processing but remain configured.
        /// </summary>
        public bool IsEnabled { get; init; } = true;

        /// <summary>
        /// Gets the priority of this suppression rule.
        /// Rules with lower numbers have higher priority and are evaluated first.
        /// </summary>
        [Range(1, 1000)]
        public int Priority { get; init; } = 500;

        /// <summary>
        /// Gets the type of suppression rule this configuration represents.
        /// Determines which suppression algorithm and parameters are used.
        /// </summary>
        [Required]
        public SuppressionType SuppressionType { get; init; }

        /// <summary>
        /// Gets the time window for duplicate detection and rate limiting.
        /// Alerts within this window are evaluated for suppression based on rule type.
        /// </summary>
        [Range(1, 86400)] // 1 second to 1 day
        public TimeSpan SuppressionWindow { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets the maximum number of alerts allowed within the suppression window.
        /// Applicable to rate limiting and threshold-based suppression rules.
        /// </summary>
        [Range(1, 10000)]
        public int MaxAlertsInWindow { get; init; } = 10;

        /// <summary>
        /// Gets the action to take when suppression criteria are met.
        /// Determines whether alerts are dropped, queued, aggregated, or escalated.
        /// </summary>
        public SuppressionAction Action { get; init; } = SuppressionAction.Suppress;

        /// <summary>
        /// Gets the collection of alert sources this rule applies to.
        /// Empty collection means the rule applies to all sources.
        /// </summary>
        public IReadOnlyList<FixedString64Bytes> ApplicableSources { get; init; } = Array.Empty<FixedString64Bytes>();

        /// <summary>
        /// Gets the collection of alert tags this rule applies to.
        /// Empty collection means the rule applies to all tags.
        /// </summary>
        public IReadOnlyList<FixedString64Bytes> ApplicableTags { get; init; } = Array.Empty<FixedString64Bytes>();

        /// <summary>
        /// Gets the severity levels this rule applies to.
        /// Empty collection means the rule applies to all severity levels.
        /// </summary>
        public IReadOnlyList<AlertSeverity> ApplicableSeverities { get; init; } = Array.Empty<AlertSeverity>();

        /// <summary>
        /// Gets the business hours configuration for time-based suppression.
        /// Only applicable when SuppressionType includes business hours logic.
        /// </summary>
        public BusinessHoursConfig BusinessHours { get; init; } = BusinessHoursConfig.Default;

        /// <summary>
        /// Gets the duplicate detection configuration for identifying similar alerts.
        /// Defines how alerts are compared to determine if they are duplicates.
        /// </summary>
        public DuplicateDetectionConfig DuplicateDetection { get; init; } = DuplicateDetectionConfig.Default;

        /// <summary>
        /// Gets the aggregation configuration for grouping related alerts.
        /// Used when suppression action is set to Aggregate.
        /// </summary>
        public AggregationConfig Aggregation { get; init; } = AggregationConfig.Default;

        /// <summary>
        /// Gets the escalation configuration for handling suppressed critical alerts.
        /// Defines conditions under which suppressed alerts should still be escalated.
        /// </summary>
        public EscalationConfig Escalation { get; init; } = EscalationConfig.Default;

        /// <summary>
        /// Gets custom filter expressions for advanced suppression logic.
        /// Supports simple expression syntax for complex filtering conditions.
        /// </summary>
        public IReadOnlyList<string> FilterExpressions { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets additional configuration parameters specific to the suppression type.
        /// Allows for extensible configuration without breaking changes.
        /// </summary>
        public IReadOnlyDictionary<string, string> CustomParameters { get; init; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets whether statistics collection is enabled for this suppression rule.
        /// When enabled, detailed metrics about suppression effectiveness are collected.
        /// </summary>
        public bool EnableStatistics { get; init; } = true;

        /// <summary>
        /// Gets the duration for which suppression statistics are retained.
        /// Older statistics are automatically purged to prevent unbounded memory growth.
        /// </summary>
        [Range(1, 168)] // 1 hour to 1 week
        public TimeSpan StatisticsRetention { get; init; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Validates the suppression configuration for correctness and consistency.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration validation fails.</exception>
        public void Validate()
        {
            if (RuleName.IsEmpty)
                throw new InvalidOperationException("Rule name cannot be empty.");

            if (SuppressionWindow <= TimeSpan.Zero)
                throw new InvalidOperationException("Suppression window must be greater than zero.");

            if (MaxAlertsInWindow <= 0)
                throw new InvalidOperationException("Max alerts in window must be greater than zero.");

            if (StatisticsRetention <= TimeSpan.Zero)
                throw new InvalidOperationException("Statistics retention must be greater than zero.");

            // Validate business hours if applicable
            if (SuppressionType == SuppressionType.BusinessHours || SuppressionType == SuppressionType.Composite)
            {
                BusinessHours.Validate();
            }

            // Validate duplicate detection configuration
            DuplicateDetection.Validate();

            // Validate aggregation configuration if action is Aggregate
            if (Action == SuppressionAction.Aggregate)
            {
                Aggregation.Validate();
            }

            // Validate escalation configuration
            Escalation.Validate();

            // Validate filter expressions
            ValidateFilterExpressions();
        }

        /// <summary>
        /// Determines whether this suppression rule applies to the specified alert.
        /// </summary>
        /// <param name="alert">The alert to evaluate.</param>
        /// <returns>True if the rule applies to the alert; otherwise, false.</returns>
        public bool AppliesTo(Alert alert)
        {
            if (!IsEnabled)
                return false;

            // Check source applicability
            if (ApplicableSources.Count > 0 && !ApplicableSources.Contains(alert.Source))
                return false;

            // Check tag applicability
            if (ApplicableTags.Count > 0 && !ApplicableTags.Contains(alert.Tag))
                return false;

            // Check severity applicability
            if (ApplicableSeverities.Count > 0 && !ApplicableSeverities.Contains(alert.Severity))
                return false;

            // Evaluate custom filter expressions
            if (FilterExpressions.Count > 0)
            {
                return FilterExpressions.Any(expr => EvaluateFilterExpression(expr, alert));
            }

            return true;
        }

        /// <summary>
        /// Creates a default duplicate filter configuration.
        /// Suppresses identical alerts within a 5-minute window.
        /// </summary>
        /// <param name="name">Optional custom name for the rule.</param>
        /// <returns>A configured duplicate filter rule.</returns>
        public static SuppressionConfig CreateDefaultDuplicateFilter(string name = "DuplicateFilter")
        {
            return new SuppressionConfig
            {
                RuleName = name,
                IsEnabled = true,
                Priority = 100,
                SuppressionType = SuppressionType.Duplicate,
                SuppressionWindow = TimeSpan.FromMinutes(5),
                MaxAlertsInWindow = 1,
                Action = SuppressionAction.Suppress,
                DuplicateDetection = new DuplicateDetectionConfig
                {
                    CompareSource = true,
                    CompareMessage = true,
                    CompareSeverity = true,
                    CompareTag = false,
                    MessageSimilarityThreshold = 0.95,
                    IgnoreTimestamps = true
                },
                EnableStatistics = true,
                StatisticsRetention = TimeSpan.FromHours(24)
            };
        }

        /// <summary>
        /// Creates a default rate limit configuration.
        /// Limits alerts to 10 per minute across all sources.
        /// </summary>
        /// <param name="name">Optional custom name for the rule.</param>
        /// <returns>A configured rate limit rule.</returns>
        public static SuppressionConfig CreateDefaultRateLimit(string name = "RateLimit")
        {
            return new SuppressionConfig
            {
                RuleName = name,
                IsEnabled = true,
                Priority = 200,
                SuppressionType = SuppressionType.RateLimit,
                SuppressionWindow = TimeSpan.FromMinutes(1),
                MaxAlertsInWindow = 10,
                Action = SuppressionAction.Queue,
                EnableStatistics = true,
                StatisticsRetention = TimeSpan.FromHours(24)
            };
        }

        /// <summary>
        /// Creates a business hours suppression configuration.
        /// Applies different severity thresholds during business hours vs. after hours.
        /// </summary>
        /// <param name="name">Optional custom name for the rule.</param>
        /// <param name="timeZone">The time zone for business hours calculation.</param>
        /// <returns>A configured business hours rule.</returns>
        public static SuppressionConfig CreateBusinessHoursFilter(string name = "BusinessHours", TimeZoneInfo timeZone = null)
        {
            return new SuppressionConfig
            {
                RuleName = name,
                IsEnabled = true,
                Priority = 300,
                SuppressionType = SuppressionType.BusinessHours,
                SuppressionWindow = TimeSpan.FromHours(1),
                MaxAlertsInWindow = int.MaxValue,
                Action = SuppressionAction.Suppress,
                BusinessHours = new BusinessHoursConfig
                {
                    TimeZone = timeZone ?? TimeZoneInfo.Local,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(17, 0),
                    WorkDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                    BusinessHoursMinimumSeverity = AlertSeverity.Warning,
                    AfterHoursMinimumSeverity = AlertSeverity.Critical
                },
                EnableStatistics = true,
                StatisticsRetention = TimeSpan.FromDays(7)
            };
        }

        /// <summary>
        /// Creates a threshold-based suppression rule for performance alerts.
        /// Aggregates performance alerts when they exceed specified thresholds.
        /// </summary>
        /// <param name="name">Optional custom name for the rule.</param>
        /// <param name="source">The performance monitoring source.</param>
        /// <returns>A configured threshold rule.</returns>
        public static SuppressionConfig CreatePerformanceThresholdFilter(string name = "PerformanceThreshold", string source = "ProfilerService")
        {
            return new SuppressionConfig
            {
                RuleName = name,
                IsEnabled = true,
                Priority = 400,
                SuppressionType = SuppressionType.Threshold,
                SuppressionWindow = TimeSpan.FromMinutes(5),
                MaxAlertsInWindow = 5,
                Action = SuppressionAction.Aggregate,
                ApplicableSources = new[] { (FixedString64Bytes)source },
                Aggregation = new AggregationConfig
                {
                    GroupBy = AggregationGroupBy.Source,
                    MaxGroupSize = 20,
                    FlushInterval = TimeSpan.FromMinutes(10),
                    IncludeStatistics = true
                },
                EnableStatistics = true,
                StatisticsRetention = TimeSpan.FromHours(48)
            };
        }

        private void ValidateFilterExpressions()
        {
            foreach (var expression in FilterExpressions)
            {
                if (string.IsNullOrWhiteSpace(expression))
                    throw new InvalidOperationException("Filter expressions cannot be null or whitespace.");

                // Basic syntax validation - in a real implementation, this would use a proper expression parser
                if (!expression.Contains("source") && !expression.Contains("severity") && 
                    !expression.Contains("message") && !expression.Contains("tag"))
                {
                    throw new InvalidOperationException($"Filter expression '{expression}' must reference at least one alert property.");
                }
            }
        }

        private bool EvaluateFilterExpression(string expression, Alert alert)
        {
            // Simplified expression evaluation - in production, use a proper expression engine
            // This is a placeholder implementation for demonstration
            
            if (expression.Contains("source"))
            {
                var sourceValue = alert.Source.ToString();
                return expression.Replace("source", $"\"{sourceValue}\"").Contains(sourceValue);
            }

            if (expression.Contains("severity"))
            {
                var severityValue = alert.Severity.ToString();
                return expression.Replace("severity", $"\"{severityValue}\"").Contains(severityValue);
            }

            // Additional expression evaluation logic would go here
            return true;
        }
    }

    /// <summary>
    /// Defines the types of suppression rules available in the system.
    /// Each type implements a different suppression algorithm and logic.
    /// </summary>
    public enum SuppressionType
    {
        /// <summary>
        /// Suppresses duplicate alerts based on content similarity.
        /// </summary>
        Duplicate,

        /// <summary>
        /// Limits the rate of alerts from specific sources or overall.
        /// </summary>
        RateLimit,

        /// <summary>
        /// Applies different suppression rules based on business hours.
        /// </summary>
        BusinessHours,

        /// <summary>
        /// Suppresses alerts based on threshold values or counts.
        /// </summary>
        Threshold,

        /// <summary>
        /// Combines multiple suppression types with complex logic.
        /// </summary>
        Composite,

        /// <summary>
        /// Custom suppression logic defined by filter expressions.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Defines actions to take when suppression criteria are met.
    /// </summary>
    public enum SuppressionAction
    {
        /// <summary>
        /// Completely suppress the alert - it will not be processed further.
        /// </summary>
        Suppress,

        /// <summary>
        /// Queue the alert for later processing when suppression window expires.
        /// </summary>
        Queue,

        /// <summary>
        /// Aggregate the alert with similar alerts into a summary.
        /// </summary>
        Aggregate,

        /// <summary>
        /// Escalate the alert to a higher priority channel despite suppression.
        /// </summary>
        Escalate,

        /// <summary>
        /// Modify the alert (typically reduce severity) but continue processing.
        /// </summary>
        Modify
    }

    /// <summary>
    /// Configuration for business hours-based suppression logic.
    /// Defines when business hours occur and different suppression rules for business vs. after hours.
    /// </summary>
    public sealed record BusinessHoursConfig
    {
        /// <summary>
        /// Gets the time zone used for business hours calculations.
        /// </summary>
        [Required]
        public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Local;

        /// <summary>
        /// Gets the start time for business hours (24-hour format).
        /// </summary>
        public TimeOnly StartTime { get; init; } = new TimeOnly(9, 0);

        /// <summary>
        /// Gets the end time for business hours (24-hour format).
        /// </summary>
        public TimeOnly EndTime { get; init; } = new TimeOnly(17, 0);

        /// <summary>
        /// Gets the days of the week considered business days.
        /// </summary>
        [Required]
        public IReadOnlyList<DayOfWeek> WorkDays { get; init; } = new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
        };

        /// <summary>
        /// Gets the minimum severity level for alerts during business hours.
        /// Alerts below this level are suppressed during business hours.
        /// </summary>
        public AlertSeverity BusinessHoursMinimumSeverity { get; init; } = AlertSeverity.Warning;

        /// <summary>
        /// Gets the minimum severity level for alerts after business hours.
        /// Typically set higher than business hours to reduce after-hours noise.
        /// </summary>
        public AlertSeverity AfterHoursMinimumSeverity { get; init; } = AlertSeverity.Critical;

        /// <summary>
        /// Gets the collection of holidays when business hours rules don't apply.
        /// Holidays are treated as non-business days regardless of day of week.
        /// </summary>
        public IReadOnlyList<DateOnly> Holidays { get; init; } = Array.Empty<DateOnly>();

        /// <summary>
        /// Determines whether the specified timestamp falls within business hours.
        /// </summary>
        /// <param name="timestamp">The timestamp to evaluate.</param>
        /// <returns>True if the timestamp is during business hours; otherwise, false.</returns>
        public bool IsBusinessHours(DateTime timestamp)
        {
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(timestamp, TimeZone);
            var dateOnly = DateOnly.FromDateTime(localTime);
            var timeOnly = TimeOnly.FromDateTime(localTime);

            // Check if it's a holiday
            if (Holidays.Contains(dateOnly))
                return false;

            // Check if it's a work day
            if (!WorkDays.Contains(localTime.DayOfWeek))
                return false;

            // Check if it's within business hours
            return timeOnly >= StartTime && timeOnly <= EndTime;
        }

        /// <summary>
        /// Gets the appropriate minimum severity for the specified timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp to evaluate.</param>
        /// <returns>The minimum severity level that should be applied.</returns>
        public AlertSeverity GetMinimumSeverity(DateTime timestamp)
        {
            return IsBusinessHours(timestamp) ? BusinessHoursMinimumSeverity : AfterHoursMinimumSeverity;
        }

        /// <summary>
        /// Validates the business hours configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (TimeZone == null)
                throw new InvalidOperationException("Time zone cannot be null.");

            if (EndTime <= StartTime)
                throw new InvalidOperationException("End time must be after start time.");

            if (WorkDays.Count == 0)
                throw new InvalidOperationException("At least one work day must be specified.");

            if (WorkDays.Distinct().Count() != WorkDays.Count)
                throw new InvalidOperationException("Work days cannot contain duplicates.");
        }

        /// <summary>
        /// Gets the default business hours configuration (9 AM - 5 PM, Monday-Friday, local time).
        /// </summary>
        public static BusinessHoursConfig Default => new()
        {
            TimeZone = TimeZoneInfo.Local,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            WorkDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            BusinessHoursMinimumSeverity = AlertSeverity.Warning,
            AfterHoursMinimumSeverity = AlertSeverity.Critical,
            Holidays = Array.Empty<DateOnly>()
        };
    }

    /// <summary>
    /// Configuration for duplicate alert detection algorithms.
    /// Defines how alerts are compared to determine if they are duplicates.
    /// </summary>
    public sealed record DuplicateDetectionConfig
    {
        /// <summary>
        /// Gets whether the alert source is included in duplicate comparison.
        /// </summary>
        public bool CompareSource { get; init; } = true;

        /// <summary>
        /// Gets whether the alert message is included in duplicate comparison.
        /// </summary>
        public bool CompareMessage { get; init; } = true;

        /// <summary>
        /// Gets whether the alert severity is included in duplicate comparison.
        /// </summary>
        public bool CompareSeverity { get; init; } = false;

        /// <summary>
        /// Gets whether the alert tag is included in duplicate comparison.
        /// </summary>
        public bool CompareTag { get; init; } = false;

        /// <summary>
        /// Gets the similarity threshold for message comparison (0.0 to 1.0).
        /// Messages with similarity above this threshold are considered duplicates.
        /// </summary>
        [Range(0.0, 1.0)]
        public double MessageSimilarityThreshold { get; init; } = 0.95;

        /// <summary>
        /// Gets whether timestamps are ignored in duplicate comparison.
        /// When true, alerts with identical content but different timestamps are considered duplicates.
        /// </summary>
        public bool IgnoreTimestamps { get; init; } = true;

        /// <summary>
        /// Gets whether case sensitivity is applied to text comparisons.
        /// </summary>
        public bool CaseSensitive { get; init; } = false;

        /// <summary>
        /// Gets the collection of message patterns to normalize before comparison.
        /// Useful for removing variable parts like timestamps or IDs from messages.
        /// </summary>
        public IReadOnlyList<string> NormalizationPatterns { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Validates the duplicate detection configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (!CompareSource && !CompareMessage && !CompareSeverity && !CompareTag)
                throw new InvalidOperationException("At least one comparison field must be enabled.");

            if (MessageSimilarityThreshold < 0.0 || MessageSimilarityThreshold > 1.0)
                throw new InvalidOperationException("Message similarity threshold must be between 0.0 and 1.0.");
        }

        /// <summary>
        /// Gets the default duplicate detection configuration.
        /// </summary>
        public static DuplicateDetectionConfig Default => new()
        {
            CompareSource = true,
            CompareMessage = true,
            CompareSeverity = false,
            CompareTag = false,
            MessageSimilarityThreshold = 0.95,
            IgnoreTimestamps = true,
            CaseSensitive = false,
            NormalizationPatterns = Array.Empty<string>()
        };
    }

    /// <summary>
    /// Configuration for alert aggregation when suppression action is set to Aggregate.
    /// Defines how alerts are grouped together and when aggregated alerts are dispatched.
    /// </summary>
    public sealed record AggregationConfig
    {
        /// <summary>
        /// Gets the field used for grouping alerts during aggregation.
        /// Alerts with the same group key are aggregated together.
        /// </summary>
        public AggregationGroupBy GroupBy { get; init; } = AggregationGroupBy.Source;

        /// <summary>
        /// Gets the maximum number of alerts that can be aggregated into a single group.
        /// When this limit is reached, the aggregated alert is immediately dispatched.
        /// </summary>
        [Range(2, 1000)]
        public int MaxGroupSize { get; init; } = 20;

        /// <summary>
        /// Gets the maximum time to wait before flushing a partial aggregation group.
        /// Ensures aggregated alerts are not delayed indefinitely.
        /// </summary>
        [Range(1, 3600)]
        public TimeSpan FlushInterval { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets whether statistical information is included in aggregated alerts.
        /// When enabled, aggregated alerts include count, frequency, and timing statistics.
        /// </summary>
        public bool IncludeStatistics { get; init; } = true;

        /// <summary>
        /// Gets whether the original alert details are preserved in the aggregated alert.
        /// When enabled, the first and last alerts in the group are included for context.
        /// </summary>
        public bool PreserveDetails { get; init; } = true;

        /// <summary>
        /// Gets the severity level assigned to aggregated alerts.
        /// Can be different from the original alerts to indicate aggregation.
        /// </summary>
        public AlertSeverity AggregatedSeverity { get; init; } = AlertSeverity.Warning;

        /// <summary>
        /// Gets whether aggregated alerts should use the highest severity from the group.
        /// When true, AggregatedSeverity is ignored and the highest severity is used.
        /// </summary>
        public bool UseHighestSeverity { get; init; } = true;

        /// <summary>
        /// Gets the message template for aggregated alerts.
        /// Supports placeholders: {Count}, {GroupKey}, {TimeSpan}, {FirstMessage}, {LastMessage}.
        /// </summary>
        [StringLength(512)]
        public string MessageTemplate { get; init; } = "Aggregated {Count} alerts from {GroupKey} over {TimeSpan}";

        /// <summary>
        /// Validates the aggregation configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (MaxGroupSize < 2)
                throw new InvalidOperationException("Max group size must be at least 2.");

            if (FlushInterval <= TimeSpan.Zero)
                throw new InvalidOperationException("Flush interval must be greater than zero.");

            if (string.IsNullOrWhiteSpace(MessageTemplate))
                throw new InvalidOperationException("Message template cannot be empty or whitespace.");
        }

        /// <summary>
        /// Gets the default aggregation configuration.
        /// </summary>
        public static AggregationConfig Default => new()
        {
            GroupBy = AggregationGroupBy.Source,
            MaxGroupSize = 20,
            FlushInterval = TimeSpan.FromMinutes(5),
            IncludeStatistics = true,
            PreserveDetails = true,
            AggregatedSeverity = AlertSeverity.Warning,
            UseHighestSeverity = true,
            MessageTemplate = "Aggregated {Count} alerts from {GroupKey} over {TimeSpan}"
        };
    }

    /// <summary>
    /// Defines the fields available for grouping alerts during aggregation.
    /// </summary>
    public enum AggregationGroupBy
    {
        /// <summary>
        /// Group alerts by their source system or component.
        /// </summary>
        Source,

        /// <summary>
        /// Group alerts by their tag or category.
        /// </summary>
        Tag,

        /// <summary>
        /// Group alerts by their severity level.
        /// </summary>
        Severity,

        /// <summary>
        /// Group alerts by both source and tag combination.
        /// </summary>
        SourceAndTag,

        /// <summary>
        /// Group alerts by custom expression evaluation.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Configuration for escalation behavior when suppressed alerts meet critical criteria.
    /// Defines conditions under which suppressed alerts should still be escalated for immediate attention.
    /// </summary>
    public sealed record EscalationConfig
    {
        /// <summary>
        /// Gets whether escalation is enabled for this suppression rule.
        /// When disabled, no escalation occurs regardless of other settings.
        /// </summary>
        public bool IsEnabled { get; init; } = false;

        /// <summary>
        /// Gets the severity threshold for automatic escalation.
        /// Suppressed alerts at or above this severity are automatically escalated.
        /// </summary>
        public AlertSeverity EscalationSeverity { get; init; } = AlertSeverity.Emergency;

        /// <summary>
        /// Gets the maximum number of alerts that can be suppressed before triggering escalation.
        /// When this threshold is reached, an escalation alert is generated.
        /// </summary>
        [Range(1, 10000)]
        public int SuppressionThreshold { get; init; } = 100;

        /// <summary>
        /// Gets the time window for evaluating suppression thresholds.
        /// Suppression counts are evaluated over this rolling window.
        /// </summary>
        [Range(1, 86400)]
        public TimeSpan EvaluationWindow { get; init; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Gets the escalation channel to use for escalated alerts.
        /// Should be a high-priority channel that bypasses normal suppression rules.
        /// </summary>
        [StringLength(64)]
        public FixedString64Bytes EscalationChannel { get; init; } = "Emergency";

        /// <summary>
        /// Gets the message template for escalation alerts.
        /// Supports placeholders: {SuppressedCount}, {TimeWindow}, {RuleName}, {Severity}.
        /// </summary>
        [StringLength(512)]
        public string EscalationMessageTemplate { get; init; } = "Escalation: {SuppressedCount} alerts suppressed by {RuleName} in {TimeWindow}";

        /// <summary>
        /// Gets whether escalation alerts should include details of suppressed alerts.
        /// When enabled, a summary of suppressed alerts is included in the escalation.
        /// </summary>
        public bool IncludeSuppressedDetails { get; init; } = true;

        /// <summary>
        /// Gets the minimum delay between escalation alerts for the same rule.
        /// Prevents rapid-fire escalations that could overwhelm the escalation channel.
        /// </summary>
        [Range(1, 3600)]
        public TimeSpan EscalationCooldown { get; init; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Validates the escalation configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (IsEnabled && EscalationChannel.IsEmpty)
                throw new InvalidOperationException("Escalation channel must be specified when escalation is enabled.");

            if (SuppressionThreshold <= 0)
                throw new InvalidOperationException("Suppression threshold must be greater than zero.");

            if (EvaluationWindow <= TimeSpan.Zero)
                throw new InvalidOperationException("Evaluation window must be greater than zero.");

            if (EscalationCooldown <= TimeSpan.Zero)
                throw new InvalidOperationException("Escalation cooldown must be greater than zero.");

            if (IsEnabled && string.IsNullOrWhiteSpace(EscalationMessageTemplate))
                throw new InvalidOperationException("Escalation message template cannot be empty when escalation is enabled.");
        }

        /// <summary>
        /// Gets the default escalation configuration with escalation disabled.
        /// </summary>
        public static EscalationConfig Default => new()
        {
            IsEnabled = false,
            EscalationSeverity = AlertSeverity.Emergency,
            SuppressionThreshold = 100,
            EvaluationWindow = TimeSpan.FromMinutes(15),
            EscalationChannel = "Emergency",
            EscalationMessageTemplate = "Escalation: {SuppressedCount} alerts suppressed by {RuleName} in {TimeWindow}",
            IncludeSuppressedDetails = true,
            EscalationCooldown = TimeSpan.FromMinutes(10)
        };

        /// <summary>
        /// Creates an escalation configuration for critical alert monitoring.
        /// Escalates when more than 50 critical alerts are suppressed in 10 minutes.
        /// </summary>
        /// <returns>A configured escalation rule for critical alerts.</returns>
        public static EscalationConfig CreateCriticalAlertEscalation()
        {
            return new EscalationConfig
            {
                IsEnabled = true,
                EscalationSeverity = AlertSeverity.Critical,
                SuppressionThreshold = 50,
                EvaluationWindow = TimeSpan.FromMinutes(10),
                EscalationChannel = "Emergency",
                EscalationMessageTemplate = "CRITICAL: {SuppressedCount} critical alerts suppressed by {RuleName} in {TimeWindow} - System may be experiencing severe issues",
                IncludeSuppressedDetails = true,
                EscalationCooldown = TimeSpan.FromMinutes(5)
            };
        }
    }
}