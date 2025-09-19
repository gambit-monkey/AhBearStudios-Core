using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        Models.HealthCheckTrendAnalysis AnalyzeTrends(
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
}