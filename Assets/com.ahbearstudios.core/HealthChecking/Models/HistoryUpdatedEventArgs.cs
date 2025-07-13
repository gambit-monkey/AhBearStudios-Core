namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Event arguments for history updates
/// </summary>
public sealed class HistoryUpdatedEventArgs : EventArgs
{
    public string CheckName { get; init; }
    public HealthCheckResult Result { get; init; }
    public DateTime Timestamp { get; init; }
}