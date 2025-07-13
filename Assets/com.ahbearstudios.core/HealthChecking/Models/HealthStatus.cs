namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Defines the possible health statuses.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// The health status is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The component is healthy.
    /// </summary>
    Healthy = 1,

    /// <summary>
    /// The component is degraded but still functional.
    /// </summary>
    Degraded = 2,

    /// <summary>
    /// The component is unhealthy.
    /// </summary>
    Unhealthy = 3,

    /// <summary>
    /// The component has a warning status.
    /// </summary>
    Warning = 4,

    /// <summary>
    /// The component is in a critical state.
    /// </summary>
    Critical = 5,

    /// <summary>
    /// The component is offline.
    /// </summary>
    Offline = 6
}