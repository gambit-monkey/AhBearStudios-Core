namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Event arguments for recovery completion
/// </summary>
public sealed class RecoveryCompletedEventArgs : EventArgs
{
    public string SystemName { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public bool IsAutomatic { get; init; }
    public bool Successful { get; init; }
    public DateTime Timestamp { get; init; }
}