using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Overall statistics for all health checks
/// </summary>
public sealed record OverallHistoryStatistics
{
    public TimeSpan TimePeriod { get; init; }
    public DateTime GeneratedAt { get; init; }
    public int TotalHealthChecks { get; init; }
    public int TotalExecutions { get; init; }
    public TimeSpan AverageUptime { get; init; }
    public double OverallHealthyPercentage { get; init; }
    public double OverallDegradedPercentage { get; init; }
    public double OverallUnhealthyPercentage { get; init; }
    public Dictionary<string, HealthCheckHistoryStatistics> CheckStatistics { get; init; } = new();
}