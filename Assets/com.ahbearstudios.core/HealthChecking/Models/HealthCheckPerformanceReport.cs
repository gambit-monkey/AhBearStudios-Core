using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Performance report aggregated from IProfilerService metrics.
    /// Provides comprehensive performance analysis for health checks.
    /// </summary>
    public sealed record HealthCheckPerformanceReport
    {
        /// <summary>
        /// Gets the start time of the report period.
        /// </summary>
        public DateTime StartTime { get; init; }

        /// <summary>
        /// Gets the end time of the report period.
        /// </summary>
        public DateTime EndTime { get; init; }

        /// <summary>
        /// Gets the average execution times by health check name.
        /// </summary>
        public IReadOnlyDictionary<string, double> AverageExecutionTimes { get; init; } = new Dictionary<string, double>();

        /// <summary>
        /// Gets the execution counts by health check name.
        /// </summary>
        public IReadOnlyDictionary<string, long> ExecutionCounts { get; init; } = new Dictionary<string, long>();

        /// <summary>
        /// Gets the failure counts by health check name.
        /// </summary>
        public IReadOnlyDictionary<string, long> FailureCounts { get; init; } = new Dictionary<string, long>();

        /// <summary>
        /// Gets the success rates by health check name.
        /// </summary>
        public IReadOnlyDictionary<string, double> SuccessRates { get; init; } = new Dictionary<string, double>();

        /// <summary>
        /// Gets the overall average execution time across all health checks.
        /// </summary>
        public double OverallAverageTime { get; init; }

        /// <summary>
        /// Gets the name of the slowest health check.
        /// </summary>
        public string SlowestHealthCheck { get; init; } = string.Empty;

        /// <summary>
        /// Gets the name of the health check with the most failures.
        /// </summary>
        public string MostFailedHealthCheck { get; init; } = string.Empty;

        /// <summary>
        /// Creates a new HealthCheckPerformanceReport with the specified parameters.
        /// </summary>
        /// <param name="startTime">Start time of the report period</param>
        /// <param name="endTime">End time of the report period</param>
        /// <param name="averageExecutionTimes">Average execution times by health check</param>
        /// <param name="executionCounts">Execution counts by health check</param>
        /// <param name="failureCounts">Failure counts by health check</param>
        /// <param name="successRates">Success rates by health check</param>
        /// <param name="overallAverageTime">Overall average execution time</param>
        /// <param name="slowestHealthCheck">Name of slowest health check</param>
        /// <param name="mostFailedHealthCheck">Name of most failed health check</param>
        /// <returns>New HealthCheckPerformanceReport instance</returns>
        public static HealthCheckPerformanceReport Create(
            DateTime startTime,
            DateTime endTime,
            Dictionary<string, double> averageExecutionTimes = null,
            Dictionary<string, long> executionCounts = null,
            Dictionary<string, long> failureCounts = null,
            Dictionary<string, double> successRates = null,
            double overallAverageTime = 0.0,
            string slowestHealthCheck = "",
            string mostFailedHealthCheck = "")
        {
            return new HealthCheckPerformanceReport
            {
                StartTime = startTime,
                EndTime = endTime,
                AverageExecutionTimes = averageExecutionTimes ?? new Dictionary<string, double>(),
                ExecutionCounts = executionCounts ?? new Dictionary<string, long>(),
                FailureCounts = failureCounts ?? new Dictionary<string, long>(),
                SuccessRates = successRates ?? new Dictionary<string, double>(),
                OverallAverageTime = overallAverageTime,
                SlowestHealthCheck = slowestHealthCheck ?? string.Empty,
                MostFailedHealthCheck = mostFailedHealthCheck ?? string.Empty
            };
        }

        /// <summary>
        /// Returns a string representation of this performance report.
        /// </summary>
        /// <returns>Performance report summary</returns>
        public override string ToString()
        {
            var period = EndTime - StartTime;
            return $"HealthCheckPerformanceReport: {period.TotalHours:F1}h period, {ExecutionCounts.Count} checks, {OverallAverageTime:F2}ms avg";
        }
    }
}