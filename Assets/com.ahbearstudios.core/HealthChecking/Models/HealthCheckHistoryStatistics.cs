namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Statistics for health check history
/// </summary>
public sealed record HealthCheckHistoryStatistics
{
    public string CheckName { get; init; }
    public TimeSpan TimePeriod { get; init; }
    public int TotalExecutions { get; init; }
    public int HealthyCount { get; init; }
    public int DegradedCount { get; init; }
    public int UnhealthyCount { get; init; }
    public int UnknownCount { get; init; }
    public double HealthyPercentage { get; init; }
    public double DegradedPercentage { get; init; }
    public double UnhealthyPercentage { get; init; }
    public double UnknownPercentage { get; init; }
    public TimeSpan AverageExecutionTime { get; init; }
    public TimeSpan TotalExecutionTime { get; init; }
    public TimeSpan TotalUptime { get; init; }
    public double UptimePercentage { get; init; }
    public HealthCheckResult FirstResult { get; init; }
    public HealthCheckResult LastResult { get; init; }
}