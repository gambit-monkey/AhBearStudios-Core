using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Real-time aggregation metrics
/// </summary>
public sealed record AggregationMetrics
{
    public DateTime Timestamp { get; init; }
    public int TotalHealthChecks { get; init; }
    public double OverallHealthScore { get; init; }
    public Dictionary<HealthCheckCategory, double> CategoryScores { get; init; } = new();
    public HealthStatus OverallStatus { get; init; }
    public DateTime LastStatusChange { get; init; }
    public DegradationLevel DegradationLevel { get; init; }
}