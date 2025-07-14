namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Cache warming strategies
/// </summary>
public enum CacheWarmingStrategy
{
    Scheduled,
    Predictive,
    OnDemand,
    Hybrid
}