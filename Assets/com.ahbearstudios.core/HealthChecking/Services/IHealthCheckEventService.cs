using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Service for complex health check event coordination and reporting.
    /// DOES NOT wrap simple IMessageBusService calls - those are used directly.
    /// DOES NOT duplicate IProfilerService metrics - those are used directly.
    /// Only handles complex event scenarios that require coordination.
    /// </summary>
    public interface IHealthCheckEventService
    {
        /// <summary>
        /// Publishes coordinated lifecycle events for a health check execution.
        /// This is complex coordination, not simple message publishing.
        /// Combines multiple related events and metrics in a coordinated way.
        /// </summary>
        /// <param name="checkName">Name of the health check</param>
        /// <param name="result">Health check result</param>
        /// <param name="previousStatus">Previous health status for comparison</param>
        /// <param name="correlationId">Correlation ID for event tracking</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the coordinated event publishing</returns>
        UniTask PublishHealthCheckLifecycleEventsAsync(
            string checkName,
            HealthCheckResult result,
            HealthStatus previousStatus,
            Guid correlationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a comprehensive performance report using IProfilerService metrics.
        /// Aggregates and analyzes existing metrics, doesn't collect new ones.
        /// </summary>
        /// <param name="period">Time period for the report</param>
        /// <param name="healthCheckNames">Optional filter for specific health checks</param>
        /// <returns>Performance report aggregated from IProfilerService</returns>
        HealthCheckPerformanceReport GeneratePerformanceReport(
            TimeSpan period,
            IEnumerable<string> healthCheckNames = null);

        /// <summary>
        /// Gets correlated events across the health check system.
        /// Analyzes message patterns and relationships for troubleshooting.
        /// </summary>
        /// <param name="correlationId">Correlation ID to track</param>
        /// <param name="includeRelated">Whether to include related correlations</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Correlated event analysis</returns>
        UniTask<CorrelatedHealthEvents> GetCorrelatedEventsAsync(
            Guid correlationId,
            bool includeRelated = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates alert escalation based on health check patterns.
        /// Complex logic that goes beyond simple alert raising.
        /// </summary>
        /// <param name="healthReport">Current health report</param>
        /// <param name="escalationRules">Rules for alert escalation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the escalation coordination</returns>
        UniTask CoordinateAlertEscalationAsync(
            HealthReport healthReport,
            AlertEscalationRules escalationRules,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes health check trends over time using historical data.
        /// Complex analysis that combines multiple data sources.
        /// </summary>
        /// <param name="checkName">Name of the health check to analyze</param>
        /// <param name="period">Analysis period</param>
        /// <returns>Trend analysis report</returns>
        HealthCheckTrendAnalysis AnalyzeTrends(
            string checkName,
            TimeSpan period);

        /// <summary>
        /// Generates a summary of critical events for monitoring dashboards.
        /// Aggregates and prioritizes events for display.
        /// </summary>
        /// <param name="maxEvents">Maximum number of events to include</param>
        /// <param name="minSeverity">Minimum severity to include</param>
        /// <returns>Critical event summary</returns>
        CriticalEventSummary GetCriticalEventSummary(
            int maxEvents = 10,
            HealthStatus minSeverity = HealthStatus.Warning);

        /// <summary>
        /// Coordinates batch result processing with optimized event publishing.
        /// Handles complex scenarios like deduplication and prioritization.
        /// </summary>
        /// <param name="results">Batch of health check results</param>
        /// <param name="overallStatus">Overall system status</param>
        /// <param name="serviceId">Service instance ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the batch coordination</returns>
        UniTask CoordinateBatchResultsAsync(
            Dictionary<string, HealthCheckResult> results,
            OverallHealthStatus overallStatus,
            Guid serviceId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Performance report aggregated from IProfilerService metrics.
    /// </summary>
    public class HealthCheckPerformanceReport
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Dictionary<string, double> AverageExecutionTimes { get; set; }
        public Dictionary<string, long> ExecutionCounts { get; set; }
        public Dictionary<string, long> FailureCounts { get; set; }
        public Dictionary<string, double> SuccessRates { get; set; }
        public double OverallAverageTime { get; set; }
        public string SlowestHealthCheck { get; set; }
        public string MostFailedHealthCheck { get; set; }
    }

    /// <summary>
    /// Correlated events for troubleshooting.
    /// </summary>
    public class CorrelatedHealthEvents
    {
        public Guid CorrelationId { get; set; }
        public List<HealthCheckEvent> Events { get; set; }
        public Dictionary<string, List<HealthCheckEvent>> RelatedEvents { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public string RootCause { get; set; }
    }

    /// <summary>
    /// Individual health check event.
    /// </summary>
    public class HealthCheckEvent
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; }
        public string HealthCheckName { get; set; }
        public HealthStatus Status { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Alert escalation rules.
    /// </summary>
    public class AlertEscalationRules
    {
        public Dictionary<HealthStatus, TimeSpan> EscalationDelays { get; set; }
        public Dictionary<HealthStatus, int> RepeatThresholds { get; set; }
        public bool EscalateOnDegradation { get; set; }
        public List<FixedString64Bytes> EscalationTags { get; set; }
    }

    /// <summary>
    /// Health check trend analysis.
    /// </summary>
    public class HealthCheckTrendAnalysis
    {
        public string HealthCheckName { get; set; }
        public TimeSpan AnalysisPeriod { get; set; }
        public double SuccessRateTrend { get; set; }
        public double PerformanceTrend { get; set; }
        public List<TrendPoint> DataPoints { get; set; }
        public string TrendDirection { get; set; }
        public string Recommendation { get; set; }
    }

    /// <summary>
    /// Trend data point.
    /// </summary>
    public class TrendPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public HealthStatus Status { get; set; }
    }

    /// <summary>
    /// Critical event summary for dashboards.
    /// </summary>
    public class CriticalEventSummary
    {
        public DateTime GeneratedAt { get; set; }
        public List<CriticalEvent> Events { get; set; }
        public Dictionary<string, int> EventCountsByType { get; set; }
        public OverallHealthStatus CurrentStatus { get; set; }
    }

    /// <summary>
    /// Critical event information.
    /// </summary>
    public class CriticalEvent
    {
        public DateTime Timestamp { get; set; }
        public string HealthCheckName { get; set; }
        public HealthStatus Severity { get; set; }
        public string Description { get; set; }
        public string Impact { get; set; }
    }
}