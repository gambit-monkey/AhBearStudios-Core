using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using ZLinq;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Comprehensive health report containing multiple check results and system status.
    /// Provides aggregated health information and analytics for system monitoring.
    /// </summary>
    public sealed record HealthReport
    {
        /// <summary>
        /// Gets the overall health status of the system.
        /// </summary>
        public HealthStatus Status { get; init; }

        /// <summary>
        /// Gets all health check results included in this report.
        /// </summary>
        public IReadOnlyList<HealthCheckResult> Results { get; init; } = new List<HealthCheckResult>();

        /// <summary>
        /// Gets the total time taken to execute all health checks.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Gets the unique correlation ID for tracking this health report.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets additional metadata for this health report.
        /// </summary>
        public IReadOnlyDictionary<string, object> Data { get; init; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the timestamp when this report was generated.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the current system degradation level.
        /// </summary>
        public DegradationLevel CurrentDegradationLevel { get; init; }

        /// <summary>
        /// Gets health check results that passed.
        /// </summary>
        public IEnumerable<HealthCheckResult> HealthyChecks => 
            Results.AsValueEnumerable().Where(r => r.Status == HealthStatus.Healthy).ToList();

        /// <summary>
        /// Gets health check results with warnings.
        /// </summary>
        public IEnumerable<HealthCheckResult> WarningChecks => 
            Results.AsValueEnumerable().Where(r => r.Status == HealthStatus.Warning).ToList();

        /// <summary>
        /// Gets health check results that are degraded.
        /// </summary>
        public IEnumerable<HealthCheckResult> DegradedChecks => 
            Results.AsValueEnumerable().Where(r => r.Status == HealthStatus.Degraded).ToList();

        /// <summary>
        /// Gets health check results that failed.
        /// </summary>
        public IEnumerable<HealthCheckResult> UnhealthyChecks => 
            Results.AsValueEnumerable().Where(r => r.Status == HealthStatus.Unhealthy).ToList();

        /// <summary>
        /// Gets the total number of health checks in this report.
        /// </summary>
        public int TotalChecks => Results.Count;

        /// <summary>
        /// Gets the number of healthy checks.
        /// </summary>
        public int HealthyCount => HealthyChecks.Count();

        /// <summary>
        /// Gets the number of checks with warnings.
        /// </summary>
        public int WarningCount => WarningChecks.Count();

        /// <summary>
        /// Gets the number of degraded checks.
        /// </summary>
        public int DegradedCount => DegradedChecks.Count();

        /// <summary>
        /// Gets the number of unhealthy checks.
        /// </summary>
        public int UnhealthyCount => UnhealthyChecks.Count();

        /// <summary>
        /// Gets health check results filtered by category.
        /// </summary>
        /// <param name="category">The category to filter by</param>
        /// <returns>Health check results for the specified category</returns>
        public IEnumerable<HealthCheckResult> GetChecksByCategory(HealthCheckCategory category)
        {
            return Results.AsValueEnumerable().Where(r => r.Category == category).ToList();
        }

        /// <summary>
        /// Gets health check results filtered by status.
        /// </summary>
        /// <param name="status">The status to filter by</param>
        /// <returns>Health check results with the specified status</returns>
        public IEnumerable<HealthCheckResult> GetChecksByStatus(HealthStatus status)
        {
            return Results.AsValueEnumerable().Where(r => r.Status == status).ToList();
        }

        /// <summary>
        /// Gets the names of failed dependencies from unhealthy checks.
        /// </summary>
        /// <returns>Collection of failed dependency names</returns>
        public IEnumerable<FixedString64Bytes> GetFailedDependencies()
        {
            return UnhealthyChecks
                .AsValueEnumerable()
                .Select(r => (FixedString64Bytes)r.Name)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Determines if there are any critical failures that require immediate attention.
        /// </summary>
        /// <returns>True if critical failures are present</returns>
        public bool HasCriticalFailures()
        {
            return Results.AsValueEnumerable()
                .Any(r => r.Status == HealthStatus.Unhealthy || r.Status == HealthStatus.Critical);
        }

        /// <summary>
        /// Gets the average execution time across all health checks.
        /// </summary>
        /// <returns>Average execution time</returns>
        public TimeSpan GetAverageExecutionTime()
        {
            if (!Results.Any())
                return TimeSpan.Zero;

            var totalTicks = Results.AsValueEnumerable()
                .Select(r => r.Duration.Ticks)
                .Sum();

            return new TimeSpan(totalTicks / Results.Count);
        }

        /// <summary>
        /// Gets the health check with the longest execution time.
        /// </summary>
        /// <returns>Slowest health check result, or null if no results</returns>
        public HealthCheckResult GetSlowestCheck()
        {
            return Results.AsValueEnumerable()
                .OrderByDescending(r => r.Duration.Ticks)
                .FirstOrDefault();
        }

        /// <summary>
        /// Creates a new HealthReport with the specified parameters.
        /// </summary>
        /// <param name="status">Overall health status</param>
        /// <param name="results">Collection of health check results</param>
        /// <param name="duration">Total execution duration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="data">Additional metadata</param>
        /// <param name="degradationLevel">Current degradation level</param>
        /// <returns>New HealthReport instance</returns>
        public static HealthReport Create(
            HealthStatus status,
            IEnumerable<HealthCheckResult> results,
            TimeSpan duration,
            Guid correlationId,
            Dictionary<string, object> data = null,
            DegradationLevel degradationLevel = DegradationLevel.None)
        {
            var resultsList = results?.ToList() ?? new List<HealthCheckResult>();
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("HealthReport", "HealthCheckService")
                : correlationId;

            return new HealthReport
            {
                Status = status,
                Results = resultsList.AsReadOnly(),
                Duration = duration,
                CorrelationId = finalCorrelationId,
                Data = data != null ? new ReadOnlyDictionary<string, object>(data) : new ReadOnlyDictionary<string, object>(new Dictionary<string, object>()),
                CurrentDegradationLevel = degradationLevel,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Returns a string representation of this health report.
        /// </summary>
        /// <returns>Health report summary string</returns>
        public override string ToString()
        {
            return $"HealthReport: {Status} ({HealthyCount}/{TotalChecks} healthy) - Duration: {Duration.TotalMilliseconds:F1}ms";
        }
    }
}