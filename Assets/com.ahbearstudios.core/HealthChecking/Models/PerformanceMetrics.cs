using System;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Performance metrics over a time period.
    /// </summary>
    public sealed record PerformanceMetrics
    {
        /// <summary>
        /// Gets the time period these metrics cover.
        /// </summary>
        public TimeSpan Period { get; init; }

        /// <summary>
        /// Gets the start time of the period.
        /// </summary>
        public DateTime PeriodStart { get; init; }

        /// <summary>
        /// Gets the end time of the period.
        /// </summary>
        public DateTime PeriodEnd { get; init; }

        /// <summary>
        /// Gets the total number of health check executions in the period.
        /// </summary>
        public long TotalExecutions { get; init; }

        /// <summary>
        /// Gets the average executions per minute.
        /// </summary>
        public double ExecutionsPerMinute { get; init; }

        /// <summary>
        /// Gets the overall success rate during the period.
        /// </summary>
        public double OverallSuccessRate { get; init; }

        /// <summary>
        /// Gets the average execution time during the period.
        /// </summary>
        public TimeSpan AverageExecutionTime { get; init; }

        /// <summary>
        /// Gets the 95th percentile execution time.
        /// </summary>
        public TimeSpan P95ExecutionTime { get; init; }

        /// <summary>
        /// Gets the 99th percentile execution time.
        /// </summary>
        public TimeSpan P99ExecutionTime { get; init; }

        /// <summary>
        /// Gets the peak execution time during the period.
        /// </summary>
        public TimeSpan PeakExecutionTime { get; init; }

        /// <summary>
        /// Gets the number of performance threshold violations.
        /// </summary>
        public int ThresholdViolations { get; init; }
    }
}