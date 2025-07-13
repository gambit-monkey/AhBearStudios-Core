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
    Unhealthy = 3
}