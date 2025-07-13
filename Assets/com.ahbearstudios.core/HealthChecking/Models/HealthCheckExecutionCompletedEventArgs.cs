namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Event arguments for health check execution completion
/// </summary>
public sealed class HealthCheckExecutionCompletedEventArgs : EventArgs
{
    public string HealthCheckName { get; init; } = string.Empty;
    public HealthCheckResult Result { get; init; }
    public TimeSpan Duration { get; init; }
    public DateTime CompletionTime { get; init; }
}