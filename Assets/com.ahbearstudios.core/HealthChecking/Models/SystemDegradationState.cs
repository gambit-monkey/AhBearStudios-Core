using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// System degradation state information
/// </summary>
internal sealed class SystemDegradationState
{
    public FixedString64Bytes SystemName { get; set; }
    public DegradationLevel CurrentLevel { get; set; }
    public DegradationLevel PreviousLevel { get; set; }
    public DateTime? DegradationStartTime { get; set; }
    public DateTime LastLevelChange { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsAutomatic { get; set; }
    public HashSet<FixedString64Bytes> DisabledFeatures { get; set; } = new();
    public HashSet<FixedString64Bytes> DegradedServices { get; set; } = new();
}