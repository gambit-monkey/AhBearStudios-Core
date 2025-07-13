namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Event arguments for degradation status changes
/// </summary>
public sealed class DegradationStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Previous degradation level
    /// </summary>
    public DegradationLevel OldLevel { get; init; }

    /// <summary>
    /// New degradation level
    /// </summary>
    public DegradationLevel NewLevel { get; init; }

    /// <summary>
    /// Reason for the level change
    /// </summary>
    public string Reason { get; init; }

    /// <summary>
    /// Timestamp when the level changed
    /// </summary>
    public DateTime Timestamp { get; init; }
}