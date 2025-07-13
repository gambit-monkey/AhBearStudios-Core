namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Event arguments for data retention operations
/// </summary>
public sealed class DataRetentionEventArgs : EventArgs
{
    public int CleanedUpCount { get; init; }
    public DateTime Timestamp { get; init; }
}