namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Event arguments for health status changes
/// </summary>
public sealed class HealthStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Previous health status
    /// </summary>
    public HealthStatus OldStatus { get; init; }

    /// <summary>
    /// New health status
    /// </summary>
    public HealthStatus NewStatus { get; init; }

    /// <summary>
    /// Reason for the status change
    /// </summary>
    public string Reason { get; init; }

    /// <summary>
    /// Timestamp when the status changed
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Indicates the source responsible for the health status change.
    /// </summary>
    public string Source { get; init; }
}