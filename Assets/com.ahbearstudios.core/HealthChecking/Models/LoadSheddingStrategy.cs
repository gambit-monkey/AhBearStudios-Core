namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Load shedding strategies
/// </summary>
public enum LoadSheddingStrategy
{
    /// <summary>
    /// Random load shedding
    /// </summary>
    Random,

    /// <summary>
    /// Priority-based load shedding
    /// </summary>
    Priority,

    /// <summary>
    /// User tier-based load shedding
    /// </summary>
    UserTier,

    /// <summary>
    /// Round-robin load shedding
    /// </summary>
    RoundRobin,

    /// <summary>
    /// Least recently used load shedding
    /// </summary>
    LeastRecentlyUsed
}