using System;
using AhBearStudios.Core.HealthChecking.Services;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Trend analysis for an individual health check.
    /// </summary>
    public sealed record HealthCheckTrendAnalysis
    {
        /// <summary>
        /// Gets the name of the health check.
        /// </summary>
        public FixedString64Bytes HealthCheckName { get; init; }

        /// <summary>
        /// Gets the analysis period.
        /// </summary>
        public TimeSpan AnalysisPeriod { get; init; }

        /// <summary>
        /// Gets whether the performance trend is improving.
        /// </summary>
        public bool IsImproving { get; init; }

        /// <summary>
        /// Gets whether the performance trend is degrading.
        /// </summary>
        public bool IsDegrading { get; init; }

        /// <summary>
        /// Gets the trend confidence level (0.0 to 1.0).
        /// </summary>
        public double ConfidenceLevel { get; init; }

        /// <summary>
        /// Gets the average change rate per day.
        /// </summary>
        public double DailyChangeRate { get; init; }

        /// <summary>
        /// Gets execution time trend.
        /// </summary>
        public TrendDirection ExecutionTimeTrend { get; init; }

        /// <summary>
        /// Gets success rate trend.
        /// </summary>
        public TrendDirection SuccessRateTrend { get; init; }

        /// <summary>
        /// Gets predicted performance in the next period.
        /// </summary>
        public PerformancePrediction Prediction { get; init; }
    }
}