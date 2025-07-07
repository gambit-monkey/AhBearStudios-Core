namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Represents the health status of the factory.
/// </summary>
public enum FactoryHealthStatus : byte
{
    /// <summary>
    /// Factory health is unknown or cannot be determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Factory is in a healthy operational state.
    /// </summary>
    Healthy = 1,

    /// <summary>
    /// Factory is experiencing degraded performance.
    /// </summary>
    Degraded = 2,

    /// <summary>
    /// Factory is in an unhealthy state.
    /// </summary>
    Unhealthy = 3,

    /// <summary>
    /// Factory is in a critical failure state.
    /// </summary>
    Critical = 4,

    /// <summary>
    /// Factory has been cleared and is empty.
    /// </summary>
    Cleared = 5
}