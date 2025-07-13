namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Event arguments for health check execution start
/// </summary>
public sealed class HealthCheckExecutionStartedEventArgs : EventArgs
{
    public string HealthCheckName { get; init; } = string.Empty;
    public DateTime ScheduledTime { get; init; }
    public DateTime ActualStartTime { get; init; }
}