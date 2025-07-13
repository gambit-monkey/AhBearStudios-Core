using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Event arguments for health status change events.
/// </summary>
public sealed class HealthStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous health status.
    /// </summary>
    public HealthStatus PreviousStatus { get; }

    /// <summary>
    /// Gets the current health status.
    /// </summary>
    public HealthStatus CurrentStatus { get; }

    /// <summary>
    /// Gets the reason for the status change.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets the timestamp when the status changed.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of HealthStatusChangedEventArgs.
    /// </summary>
    /// <param name="previousStatus">The previous status</param>
    /// <param name="currentStatus">The current status</param>
    /// <param name="reason">The reason for change</param>
    public HealthStatusChangedEventArgs(HealthStatus previousStatus, HealthStatus currentStatus, string reason)
    {
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
        Reason = reason ?? "Unknown";
        Timestamp = DateTime.UtcNow;
    }
}