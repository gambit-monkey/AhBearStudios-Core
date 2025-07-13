using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Recovery tracking information
/// </summary>
internal sealed class RecoveryTracker
{
    public FixedString64Bytes SystemName { get; set; }
    public DateTime StartTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsAutomatic { get; set; }
    public int SuccessfulChecks { get; set; }
    public int TotalChecks { get; set; }
}