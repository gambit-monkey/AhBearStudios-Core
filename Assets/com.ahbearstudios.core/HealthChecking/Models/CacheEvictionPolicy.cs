namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Cache eviction policies
/// </summary>
public enum CacheEvictionPolicy
{
    LeastRecentlyUsed,
    LeastFrequentlyUsed,
    FirstInFirstOut,
    Random,
    Custom
}