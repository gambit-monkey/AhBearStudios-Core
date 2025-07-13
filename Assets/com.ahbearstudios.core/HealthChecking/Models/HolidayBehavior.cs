namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Behavior on holidays
/// </summary>
public enum HolidayBehavior
{
    /// <summary>
    /// Normal schedule
    /// </summary>
    Normal,

    /// <summary>
    /// Skip health checks
    /// </summary>
    Skip,

    /// <summary>
    /// Reduced frequency
    /// </summary>
    ReducedFrequency,

    /// <summary>
    /// Essential checks only
    /// </summary>
    EssentialOnly
}