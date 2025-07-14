namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Cache storage strategies
/// </summary>
public enum CacheStorageStrategy
{
    Memory,
    Persistent,
    Distributed,
    Hybrid
}