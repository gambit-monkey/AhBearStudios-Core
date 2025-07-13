using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Messages;

/// <summary>
/// Message bus message for health check execution
/// </summary>
public sealed record HealthCheckExecutionMessage
{
    public string HealthCheckName { get; init; } = string.Empty;
    public HealthStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public DateTime Timestamp { get; init; }
}