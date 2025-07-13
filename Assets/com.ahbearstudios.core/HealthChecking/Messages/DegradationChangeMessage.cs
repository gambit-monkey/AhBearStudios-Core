using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Messages;

/// <summary>
/// Message bus message for degradation changes
/// </summary>
public sealed record DegradationChangeMessage
{
    public string SystemName { get; init; } = string.Empty;
    public DegradationLevel OldLevel { get; init; }
    public DegradationLevel NewLevel { get; init; }
    public string Reason { get; init; } = string.Empty;
    public bool IsAutomatic { get; init; }
    public DateTime Timestamp { get; init; }
}