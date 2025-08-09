using System;
using System.Collections.Generic;
using ZLinq;
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
        public int Priority { get; init; } = 500;

        /// <summary>
        /// Gets the type of suppression rule this configuration represents.
        /// Determines which suppression algorithm and parameters are used.
        /// </summary>
        public SuppressionType SuppressionType { get; init; }

        /// <summary>
        /// Gets the time window for duplicate detection and rate limiting.
        /// Alerts within this window are evaluated for suppression based on rule type.
        /// </summary>
        public TimeSpan SuppressionWindow { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets the maximum number of alerts allowed within the suppression window.
        /// Applicable to rate limiting and threshold-based suppression rules.
        /// </summary>
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
            if (ApplicableSources.Count > 0 && !ApplicableSources.AsValueEnumerable().Contains(alert.Source))
                return false;

            // Check tag applicability
            if (ApplicableTags.Count > 0 && !ApplicableTags.AsValueEnumerable().Contains(alert.Tag))
                return false;

            // Check severity applicability
            if (ApplicableSeverities.Count > 0 && !ApplicableSeverities.AsValueEnumerable().Contains(alert.Severity))
                return false;

            // Evaluate custom filter expressionsdsa
            if (FilterExpressions.Count > 0)
            {
                return FilterExpressions.AsValueEnumerable().Any(expr => EvaluateFilterExpression(expr, alert));
            }

            return true;
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





}