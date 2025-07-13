namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Event arguments for degradation changes
/// </summary>
public sealed class DegradationChangedEventArgs : EventArgs
{
    public string SystemName { get; init; } = string.Empty;
    public DegradationLevel OldLevel { get; init; }
    public DegradationLevel NewLevel { get; init; }
    public string Reason { get; init; } = string.Empty;
    public bool IsAutomatic { get; init; }
    public DateTime Timestamp { get; init; }
}