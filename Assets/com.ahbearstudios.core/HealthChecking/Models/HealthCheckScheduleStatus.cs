namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Health check schedule status information
/// </summary>
public sealed record HealthCheckScheduleStatus
{
    public string Name { get; init; } = string.Empty;
    public ScheduleType ScheduleType { get; init; }
    public DateTime NextExecutionTime { get; init; }
    public DateTime? LastExecutionTime { get; init; }
    public TimeSpan LastExecutionDuration { get; init; }
    public HealthStatus LastExecutionStatus { get; init; }
    public int ExecutionCount { get; init; }
    public int FailureCount { get; init; }
    public TimeSpan AverageExecutionTime { get; init; }
    public bool IsRunning { get; init; }
    public int Priority { get; init; }
    public bool Enabled { get; init; }
}