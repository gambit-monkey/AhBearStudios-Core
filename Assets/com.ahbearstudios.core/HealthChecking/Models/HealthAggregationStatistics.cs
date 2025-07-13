using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Statistics for health aggregation calculations
/// </summary>
public sealed record HealthAggregationStatistics
{
    public TimeSpan TimeWindow { get; init; }
    public int TotalChecks { get; init; }
    public int HealthyCount { get; init; }
    public int DegradedCount { get; init; }
    public int UnhealthyCount { get; init; }
    public int UnknownCount { get; init; }
    public TimeSpan AverageExecutionTime { get; init; }
    public Dictionary<HealthCheckCategory, double> CategoryScores { get; init; } = new();
    public double OverallHealthScore { get; init; }
    public DateTime LastStatusChange { get; init; }
    public HealthStatus CurrentOverallStatus { get; init; }
}