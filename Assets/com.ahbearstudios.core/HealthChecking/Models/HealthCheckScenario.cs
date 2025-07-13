namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Preset scenarios for health check configuration
/// </summary>
public enum HealthCheckScenario
{
    /// <summary>
    /// Critical system component with strict monitoring
    /// </summary>
    CriticalSystem,

    /// <summary>
    /// Database connectivity and performance check
    /// </summary>
    Database,

    /// <summary>
    /// External network service check
    /// </summary>
    NetworkService,

    /// <summary>
    /// Performance metrics monitoring
    /// </summary>
    PerformanceMonitoring,

    /// <summary>
    /// Development environment with relaxed settings
    /// </summary>
    Development
}