namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Event arguments for health check scheduling
/// </summary>
public sealed class HealthCheckScheduledEventArgs : EventArgs
{
    public string HealthCheckName { get; init; } = string.Empty;
    public DateTime NextExecutionTime { get; init; }
    public DateTime Timestamp { get; init; }
}