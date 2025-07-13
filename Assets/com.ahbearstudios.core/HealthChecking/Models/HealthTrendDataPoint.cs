namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Data point for trend analysis
/// </summary>
internal sealed record HealthTrendDataPoint
{
    public DateTime Timestamp { get; init; }
    public HealthStatus Status { get; init; }
    public TimeSpan Duration { get; init; }
}