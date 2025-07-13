namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Preset scenarios for circuit breaker configuration
/// </summary>
public enum CircuitBreakerScenario
{
    /// <summary>
    /// Critical service with strict fault tolerance
    /// </summary>
    CriticalService,

    /// <summary>
    /// Database connection with recovery strategies
    /// </summary>
    Database,

    /// <summary>
    /// External network service with retry logic
    /// </summary>
    NetworkService,

    /// <summary>
    /// High throughput system with load management
    /// </summary>
    HighThroughput,

    /// <summary>
    /// Development environment with relaxed settings
    /// </summary>
    Development
}