namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Enumeration of predefined circuit breaker scenarios with optimized configurations.
/// Each scenario provides preset configurations tailored for specific use cases.
/// </summary>
public enum CircuitBreakerScenario : byte
{
    /// <summary>
    /// Configuration optimized for critical service operations.
    /// Features aggressive failure detection and quick recovery.
    /// </summary>
    CriticalService = 0,

    /// <summary>
    /// Configuration optimized for database connections.
    /// Features longer timeouts and time-based sliding windows.
    /// </summary>
    Database = 1,

    /// <summary>
    /// Configuration optimized for network service calls.
    /// Features balanced thresholds and count-based windows.
    /// </summary>
    NetworkService = 2,

    /// <summary>
    /// Configuration optimized for high-throughput scenarios.
    /// Features higher thresholds and larger sliding windows.
    /// </summary>
    HighThroughput = 3,

    /// <summary>
    /// Configuration optimized for development and testing.
    /// Features lower thresholds and faster recovery for quick iteration.
    /// </summary>
    Development = 4
}