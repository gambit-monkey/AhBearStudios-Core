using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Detailed system degradation information
/// </summary>
public sealed record SystemDegradationInfo
{
    public string SystemName { get; init; } = string.Empty;
    public DegradationLevel CurrentLevel { get; init; }
    public DegradationLevel PreviousLevel { get; init; }
    public DateTime? DegradationStartTime { get; init; }
    public DateTime LastLevelChange { get; init; }
    public string Reason { get; init; } = string.Empty;
    public bool IsAutomatic { get; init; }
    public List<string> DisabledFeatures { get; init; } = new();
    public List<string> DegradedServices { get; init; } = new();
    public bool RecoveryInProgress { get; init; }
}