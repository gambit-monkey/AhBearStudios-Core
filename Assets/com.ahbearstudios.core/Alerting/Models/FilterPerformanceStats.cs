using System;
using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Performance statistics for individual alert filters.
    /// </summary>
    public readonly partial record struct FilterPerformanceStats
    {
        /// <summary>
        /// Gets the filter name.
        /// </summary>
        public FixedString64Bytes FilterName { get; init; }

        /// <summary>
        /// Gets whether the filter is currently enabled.
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets the total alerts evaluated by this filter.
        /// </summary>
        public long TotalEvaluations { get; init; }

        /// <summary>
        /// Gets the number of alerts suppressed.
        /// </summary>
        public long SuppressedCount { get; init; }

        /// <summary>
        /// Gets the average evaluation time in milliseconds.
        /// </summary>
        public double AverageEvaluationTimeMs { get; init; }

        /// <summary>
        /// Gets the last evaluation timestamp.
        /// </summary>
        public DateTime? LastEvaluation { get; init; }

        /// <summary>
        /// Gets the suppression rate as percentage (0-100).
        /// </summary>
        public double SuppressionRate => TotalEvaluations > 0 
            ? (double)SuppressedCount / TotalEvaluations * 100 
            : 0;
    }
}