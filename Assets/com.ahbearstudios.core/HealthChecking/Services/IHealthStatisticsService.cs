using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Interface for health statistics collection and aggregation.
    /// Manages metrics, analytics, and performance tracking for the health check system.
    /// Statistics events are published via IMessageBusService following CLAUDE.md patterns:
    /// - HealthCheckStatisticsResetMessage for statistics reset events
    /// - HealthCheckPerformanceThresholdExceededMessage for threshold violations
    /// </summary>
    public interface IHealthStatisticsService : IDisposable
    {

        /// <summary>
        /// Gets the timestamp when statistics collection started.
        /// </summary>
        DateTime CollectionStartTime { get; }

        /// <summary>
        /// Gets the total uptime of the statistics collector.
        /// </summary>
        TimeSpan Uptime { get; }

        /// <summary>
        /// Records the execution of a health check.
        /// </summary>
        /// <param name="result">Health check result to record</param>
        void RecordHealthCheckExecution(HealthCheckResult result);

        /// <summary>
        /// Records a health report for system-wide metrics.
        /// </summary>
        /// <param name="report">Health report to record</param>
        void RecordHealthReport(HealthReport report);

        /// <summary>
        /// Records a circuit breaker state change.
        /// </summary>
        /// <param name="name">Circuit breaker name</param>
        /// <param name="oldState">Previous state</param>
        /// <param name="newState">New state</param>
        /// <param name="reason">Reason for state change</param>
        void RecordCircuitBreakerStateChange(FixedString64Bytes name, CircuitBreakerState oldState, CircuitBreakerState newState, string reason);

        /// <summary>
        /// Records a degradation level change.
        /// </summary>
        /// <param name="oldLevel">Previous degradation level</param>
        /// <param name="newLevel">New degradation level</param>
        /// <param name="reason">Reason for change</param>
        void RecordDegradationLevelChange(DegradationLevel oldLevel, DegradationLevel newLevel, string reason);

        /// <summary>
        /// Gets comprehensive health statistics.
        /// </summary>
        /// <returns>Current health statistics</returns>
        HealthStatistics GetHealthStatistics();

        /// <summary>
        /// Gets statistics for a specific health check.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <returns>Individual health check statistics, or null if not found</returns>
        IndividualHealthCheckStatistics GetHealthCheckStatistics(FixedString64Bytes healthCheckName);

        /// <summary>
        /// Gets statistics for all registered health checks.
        /// </summary>
        /// <returns>Dictionary of health check statistics</returns>
        IReadOnlyDictionary<FixedString64Bytes, IndividualHealthCheckStatistics> GetAllHealthCheckStatistics();

        /// <summary>
        /// Gets circuit breaker statistics.
        /// </summary>
        /// <returns>Dictionary of circuit breaker statistics</returns>
        IReadOnlyDictionary<FixedString64Bytes, CircuitBreakerStatistics> GetCircuitBreakerStatistics();

        /// <summary>
        /// Gets performance metrics over a specified time period.
        /// </summary>
        /// <param name="period">Time period to analyze</param>
        /// <returns>Performance metrics for the period</returns>
        PerformanceMetrics GetPerformanceMetrics(TimeSpan period);

        /// <summary>
        /// Gets trend analysis for health check performance.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check to analyze</param>
        /// <param name="period">Time period for analysis</param>
        /// <returns>Trend analysis results</returns>
        HealthCheckTrendAnalysis GetTrendAnalysis(FixedString64Bytes healthCheckName, TimeSpan period);

        /// <summary>
        /// Gets system-wide trend analysis.
        /// </summary>
        /// <param name="period">Time period for analysis</param>
        /// <returns>System trend analysis</returns>
        SystemTrendAnalysis GetSystemTrendAnalysis(TimeSpan period);

        /// <summary>
        /// Resets all statistics to initial values.
        /// </summary>
        /// <param name="reason">Reason for the reset</param>
        void ResetStatistics(string reason = null);

        /// <summary>
        /// Resets statistics for a specific health check.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check to reset</param>
        /// <param name="reason">Reason for the reset</param>
        /// <returns>True if statistics were found and reset</returns>
        bool ResetHealthCheckStatistics(FixedString64Bytes healthCheckName, string reason = null);

        /// <summary>
        /// Sets performance thresholds for monitoring.
        /// </summary>
        /// <param name="slowExecutionThreshold">Threshold for slow execution alerts</param>
        /// <param name="highFailureRateThreshold">Threshold for high failure rate alerts</param>
        void SetPerformanceThresholds(TimeSpan slowExecutionThreshold, double highFailureRateThreshold);

        /// <summary>
        /// Enables or disables automatic cleanup of old statistics.
        /// </summary>
        /// <param name="enabled">Whether to enable automatic cleanup</param>
        /// <param name="retentionPeriod">How long to retain statistics</param>
        void SetAutomaticCleanup(bool enabled, TimeSpan retentionPeriod);

        /// <summary>
        /// Manually triggers cleanup of old statistics.
        /// </summary>
        /// <param name="cutoffTime">Remove statistics older than this time</param>
        /// <returns>Number of records cleaned up</returns>
        int CleanupOldStatistics(DateTime cutoffTime);
    }
}