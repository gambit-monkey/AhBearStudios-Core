namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Cache invalidation strategies
/// </summary>
public enum CacheInvalidationStrategy
{
    TimeBasedOnly,
    EventBased,
    Aggressive,
    Custom
}
