using System;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Statistics for alert rule performance.
    /// </summary>
    public readonly partial record struct AlertRuleStatistics
    {
        /// <summary>
        /// Gets the number of times this rule was matched.
        /// </summary>
        public long MatchCount { get; init; }

        /// <summary>
        /// Gets the number of times this rule was applied.
        /// </summary>
        public long AppliedCount { get; init; }

        /// <summary>
        /// Gets the last time this rule was matched.
        /// </summary>
        public DateTime? LastMatched { get; init; }

        /// <summary>
        /// Gets the last time this rule was applied.
        /// </summary>
        public DateTime? LastApplied { get; init; }

        /// <summary>
        /// Creates empty statistics.
        /// </summary>
        public static AlertRuleStatistics Empty => new();

        /// <summary>
        /// Increments the match count.
        /// </summary>
        /// <returns>Updated statistics</returns>
        public AlertRuleStatistics IncrementMatched()
        {
            return this with
            {
                MatchCount = MatchCount + 1,
                LastMatched = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Increments the applied count.
        /// </summary>
        /// <returns>Updated statistics</returns>
        public AlertRuleStatistics IncrementApplied()
        {
            return this with
            {
                AppliedCount = AppliedCount + 1,
                LastApplied = DateTime.UtcNow
            };
        }
    }
}