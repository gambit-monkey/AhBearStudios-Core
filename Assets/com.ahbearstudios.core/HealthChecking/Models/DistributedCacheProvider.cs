namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Distributed cache providers
/// </summary>
public enum DistributedCacheProvider
{
    Memory,
    Redis,
    SqlServer,
    NCache,
    Custom
}