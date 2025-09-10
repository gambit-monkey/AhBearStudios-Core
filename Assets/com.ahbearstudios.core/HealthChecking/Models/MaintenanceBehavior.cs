namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Defines the behavior for health checks during maintenance windows.
/// Used to control how health checks respond to scheduled maintenance periods.
/// </summary>
public enum MaintenanceBehavior
{
    /// <summary>
    /// Skip health check execution during maintenance windows
    /// </summary>
    Skip = 0,

    /// <summary>
    /// Continue executing health checks normally during maintenance windows
    /// </summary>
    Continue = 1,

    /// <summary>
    /// Execute health checks but mark results as maintenance-affected
    /// </summary>
    MarkAsMaintenance = 2,

    /// <summary>
    /// Queue health checks to execute immediately after maintenance window ends
    /// </summary>
    QueueForAfterMaintenance = 3,

    /// <summary>
    /// Execute health checks with reduced sensitivity during maintenance
    /// </summary>
    ReducedSensitivity = 4,

    /// <summary>
    /// Only execute critical health checks during maintenance windows
    /// </summary>
    CriticalOnly = 5
}