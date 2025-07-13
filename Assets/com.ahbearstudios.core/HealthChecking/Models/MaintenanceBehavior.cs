namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Behavior during maintenance windows
/// </summary>
public enum MaintenanceBehavior
{
    /// <summary>
    /// Skip health checks during maintenance
    /// </summary>
    Skip,

    /// <summary>
    /// Continue with reduced frequency
    /// </summary>
    ReducedFrequency,

    /// <summary>
    /// Run essential checks only
    /// </summary>
    EssentialOnly,

    /// <summary>
    /// Continue normal operations
    /// </summary>
    Continue
}