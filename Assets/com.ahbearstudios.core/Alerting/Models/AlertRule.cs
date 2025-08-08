using System;
using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Comprehensive alert rule configuration for filtering, suppression, and monitoring.
    /// Supports complex rule evaluation with Unity.Collections for high-performance operations.
    /// Serialization is handled through ISerializationService.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public record AlertRule
    {
        /// <summary>
        /// Gets or sets the unique rule identifier.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the rule name using FixedString for performance.
        /// </summary>
        public FixedString64Bytes Name { get; set; }

        /// <summary>
        /// Gets or sets the rule description.
        /// </summary>
        public FixedString512Bytes Description { get; set; }

        /// <summary>
        /// Gets or sets the rule type determining its behavior.
        /// </summary>
        public AlertRuleType RuleType { get; set; }

        /// <summary>
        /// Gets or sets whether this rule is currently enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the rule priority (lower numbers = higher priority).
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// Gets or sets the alert severity this rule applies to.
        /// </summary>
        public AlertSeverity? TargetSeverity { get; set; }

        /// <summary>
        /// Gets or sets the source pattern this rule matches against.
        /// </summary>
        public FixedString64Bytes SourcePattern { get; set; }

        /// <summary>
        /// Gets or sets the tag pattern this rule matches against.
        /// </summary>
        public FixedString32Bytes TagPattern { get; set; }

        /// <summary>
        /// Gets or sets the message pattern for text matching.
        /// </summary>
        public FixedString512Bytes MessagePattern { get; set; }

        /// <summary>
        /// Gets or sets the threshold value for numeric comparisons.
        /// </summary>
        public double Threshold { get; set; }

        /// <summary>
        /// Gets or sets the comparison operator for threshold evaluation.
        /// </summary>
        public ComparisonOperator ThresholdOperator { get; set; } = ComparisonOperator.GreaterThan;

        /// <summary>
        /// Gets or sets the time window for rate-based rules (in seconds).
        /// </summary>
        public int TimeWindowSeconds { get; set; } = 60;

        /// <summary>
        /// Gets or sets the maximum occurrences within the time window.
        /// </summary>
        public int MaxOccurrences { get; set; } = 1;

        /// <summary>
        /// Gets or sets the suppression duration in seconds.
        /// </summary>
        public int SuppressionDurationSeconds { get; set; } = 300; // 5 minutes

        /// <summary>
        /// Gets or sets custom rule parameters.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// Gets or sets the rule conditions for complex evaluation.
        /// </summary>
        public List<AlertRuleCondition> Conditions { get; set; } = new();

        /// <summary>
        /// Gets or sets the rule actions to take when matched.
        /// </summary>
        public List<AlertRuleAction> Actions { get; set; } = new();

        /// <summary>
        /// Gets or sets when this rule was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets when this rule was last modified.
        /// </summary>
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets who created this rule.
        /// </summary>
        public FixedString64Bytes CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets statistics for this rule.
        /// </summary>
        public AlertRuleStatistics Statistics { get; set; } = AlertRuleStatistics.Empty;

        /// <summary>
        /// Creates a basic suppression rule.
        /// </summary>
        /// <param name="name">Rule name</param>
        /// <param name="sourcePattern">Source to match</param>
        /// <param name="suppressionDuration">Suppression duration in seconds</param>
        /// <returns>Suppression rule</returns>
        public static AlertRule CreateSuppressionRule(
            string name,
            string sourcePattern,
            int suppressionDuration = 300)
        {
            return new AlertRule
            {
                Name = name,
                Description = $"Suppress alerts from {sourcePattern}",
                RuleType = AlertRuleType.Suppression,
                SourcePattern = sourcePattern,
                SuppressionDurationSeconds = suppressionDuration
            };
        }

        /// <summary>
        /// Creates a rate limiting rule.
        /// </summary>
        /// <param name="name">Rule name</param>
        /// <param name="sourcePattern">Source to match</param>
        /// <param name="maxOccurrences">Max alerts in time window</param>
        /// <param name="timeWindowSeconds">Time window in seconds</param>
        /// <returns>Rate limiting rule</returns>
        public static AlertRule CreateRateLimitRule(
            string name,
            string sourcePattern,
            int maxOccurrences,
            int timeWindowSeconds = 60)
        {
            return new AlertRule
            {
                Name = name,
                Description = $"Limit {sourcePattern} to {maxOccurrences} alerts per {timeWindowSeconds}s",
                RuleType = AlertRuleType.RateLimit,
                SourcePattern = sourcePattern,
                MaxOccurrences = maxOccurrences,
                TimeWindowSeconds = timeWindowSeconds
            };
        }

        /// <summary>
        /// Creates a threshold-based rule.
        /// </summary>
        /// <param name="name">Rule name</param>
        /// <param name="threshold">Threshold value</param>
        /// <param name="operator">Comparison operator</param>
        /// <param name="severity">Target severity</param>
        /// <returns>Threshold rule</returns>
        public static AlertRule CreateThresholdRule(
            string name,
            double threshold,
            ComparisonOperator @operator,
            AlertSeverity severity)
        {
            return new AlertRule
            {
                Name = name,
                Description = $"Alert when value {GetOperatorSymbol(@operator)} {threshold}",
                RuleType = AlertRuleType.Threshold,
                Threshold = threshold,
                ThresholdOperator = @operator,
                TargetSeverity = severity
            };
        }

        /// <summary>
        /// Evaluates if this rule matches the given alert.
        /// </summary>
        /// <param name="alert">Alert to evaluate</param>
        /// <param name="context">Evaluation context</param>
        /// <returns>True if rule matches</returns>
        public bool Matches(Alert alert, Dictionary<string, object> context = null)
        {
            if (!IsEnabled) return false;

            // Check severity match
            if (TargetSeverity.HasValue && alert.Severity != TargetSeverity.Value)
                return false;

            // Check source pattern
            if (!SourcePattern.IsEmpty && !MatchesPattern(alert.Source.ToString(), SourcePattern.ToString()))
                return false;

            // Check tag pattern
            if (!TagPattern.IsEmpty && !MatchesPattern(alert.Tag.ToString(), TagPattern.ToString()))
                return false;

            // Check message pattern
            if (!MessagePattern.IsEmpty && !MatchesPattern(alert.Message.ToString(), MessagePattern.ToString()))
                return false;

            // Evaluate conditions
            foreach (var condition in Conditions)
            {
                if (!condition.Evaluate(alert, context))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Applies the rule actions to an alert.
        /// </summary>
        /// <param name="alert">Alert to apply actions to</param>
        /// <param name="context">Action context</param>
        /// <returns>Modified alert or null if suppressed</returns>
        public Alert ApplyActions(Alert alert, Dictionary<string, object> context = null)
        {
            var result = alert;
            
            foreach (var action in Actions)
            {
                result = action.Apply(result, context);
                if (result == null) break; // Alert was suppressed
            }

            // Update statistics
            Statistics = Statistics.IncrementApplied();
            LastModifiedAt = DateTime.UtcNow;

            return result;
        }

        /// <summary>
        /// Simple pattern matching with wildcards.
        /// </summary>
        /// <param name="text">Text to match</param>
        /// <param name="pattern">Pattern with * wildcards</param>
        /// <returns>True if pattern matches</returns>
        private static bool MatchesPattern(string text, string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return true;
            if (pattern == "*") return true;
            
            // Simple wildcard matching - could be enhanced with regex
            if (pattern.Contains("*"))
            {
                var parts = pattern.Split('*', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return true;
                
                var currentIndex = 0;
                foreach (var part in parts)
                {
                    var index = text.IndexOf(part, currentIndex, StringComparison.OrdinalIgnoreCase);
                    if (index == -1) return false;
                    currentIndex = index + part.Length;
                }
                return true;
            }
            
            return string.Equals(text, pattern, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets operator symbol for display.
        /// </summary>
        /// <param name="op">Comparison operator</param>
        /// <returns>Symbol string</returns>
        private static string GetOperatorSymbol(ComparisonOperator op)
        {
            return op switch
            {
                ComparisonOperator.Equal => "==",
                ComparisonOperator.NotEqual => "!=",
                ComparisonOperator.GreaterThan => ">",
                ComparisonOperator.GreaterThanOrEqual => ">=",
                ComparisonOperator.LessThan => "<",
                ComparisonOperator.LessThanOrEqual => "<=",
                _ => "?"
            };
        }

        /// <summary>
        /// Disposes rule resources.
        /// </summary>
        public void Dispose()
        {
            Parameters?.Clear();
            Conditions?.Clear();
            Actions?.Clear();
        }

        /// <summary>
        /// Returns string representation of the rule.
        /// </summary>
        /// <returns>Rule description</returns>
        public override string ToString()
        {
            return $"{Name} ({RuleType}) - {(IsEnabled ? "Enabled" : "Disabled")}";
        }
    }






}