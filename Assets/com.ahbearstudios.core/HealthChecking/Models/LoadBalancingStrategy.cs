namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Load balancing strategies
/// </summary>
public enum LoadBalancingStrategy
{
    RoundRobin,
    Random,
    ConsistentHash,
    WeightedRoundRobin
}