namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Event arguments for recovery initiation
/// </summary>
public sealed class RecoveryInitiatedEventArgs : EventArgs
{
    public string SystemName { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public bool IsAutomatic { get; init; }
    public DateTime Timestamp { get; init; }
}