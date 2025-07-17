namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Categories for organizing health checks
/// </summary>
public enum HealthCheckCategory
{
    /// <summary>
    /// System-level health checks (CPU, memory, disk)
    /// </summary>
    System,

    /// <summary>
    /// Database connectivity and performance checks
    /// </summary>
    Database,
    
    /// <summary>
    /// Development Logging Service Health Check
    /// </summary>
    Development,
    
    /// <summary>
    /// Testing Logging Service Health Check
    /// </summary>
    Testing,

    /// <summary>
    /// Network connectivity and latency checks
    /// </summary>
    Network,

    /// <summary>
    /// Performance and throughput checks
    /// </summary>
    Performance,

    /// <summary>
    /// Security-related health checks
    /// </summary>
    Security,

    /// <summary>
    /// Circuit breaker health checks
    /// </summary>
    CircuitBreaker,

    /// <summary>
    /// Custom application-specific health checks
    /// </summary>
    Custom
}