namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Comprehensive scheduling statistics
/// </summary>
public sealed record SchedulingStatistics
{
    public int TotalScheduledChecks { get; init; }
    public int EnabledChecks { get; init; }
    public int DisabledChecks { get; init; }
    public int CurrentlyRunningChecks { get; init; }
    public int QueuedExecutions { get; init; }
    public int TotalExecutions { get; init; }
    public int TotalFailures { get; init; }
    public double SuccessRate { get; init; }
    public TimeSpan AverageExecutionTime { get; init; }
    public double ConcurrencyUtilization { get; init; }
    public DateTime LastUpdateTime { get; init; }
}