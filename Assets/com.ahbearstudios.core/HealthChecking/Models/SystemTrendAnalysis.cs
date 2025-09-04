using System;
using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// System-wide trend analysis.
    /// </summary>
    public sealed record SystemTrendAnalysis
    {
        /// <summary>
        /// Gets the analysis period.
        /// </summary>
        public TimeSpan AnalysisPeriod { get; init; }

        /// <summary>
        /// Gets the overall system health trend.
        /// </summary>
        public TrendDirection OverallHealthTrend { get; init; }

        /// <summary>
        /// Gets the system performance trend.
        /// </summary>
        public TrendDirection PerformanceTrend { get; init; }

        /// <summary>
        /// Gets the availability trend.
        /// </summary>
        public TrendDirection AvailabilityTrend { get; init; }

        /// <summary>
        /// Gets individual health check trends.
        /// </summary>
        public IReadOnlyList<HealthCheckTrendAnalysis> HealthCheckTrends { get; init; }

        /// <summary>
        /// Gets health checks with concerning trends.
        /// </summary>
        public IReadOnlyList<FixedString64Bytes> ConcerningHealthChecks { get; init; }

        /// <summary>
        /// Gets health checks with improving trends.
        /// </summary>
        public IReadOnlyList<FixedString64Bytes> ImprovingHealthChecks { get; init; }

        /// <summary>
        /// Gets system-wide performance prediction.
        /// </summary>
        public PerformancePrediction SystemPrediction { get; init; }
    }
}