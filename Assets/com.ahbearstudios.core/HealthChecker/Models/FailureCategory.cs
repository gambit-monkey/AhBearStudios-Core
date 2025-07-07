namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Defines categories of health check creation failures for classification and routing.
/// </summary>
public enum FailureCategory : byte
{
    /// <summary>
    /// Unknown or unclassified failure.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Configuration-related failure (invalid settings, missing config).
    /// </summary>
    Configuration = 1,

    /// <summary>
    /// Dependency-related failure (missing services, unavailable resources).
    /// </summary>
    CriticalDependency = 2,

    /// <summary>
    /// Invalid input or parameter failure.
    /// </summary>
    InvalidInput = 3,

    /// <summary>
    /// Internal error or bug in the health check implementation.
    /// </summary>
    InternalError = 4,

    /// <summary>
    /// Timeout during creation process.
    /// </summary>
    Timeout = 5,

    /// <summary>
    /// Resource exhaustion (memory, CPU, disk space).
    /// </summary>
    ResourceExhaustion = 6,

    /// <summary>
    /// Network connectivity issues.
    /// </summary>
    NetworkConnectivity = 7,

    /// <summary>
    /// Security-related failure (permissions, authentication).
    /// </summary>
    Security = 8,

    /// <summary>
    /// Version compatibility issues.
    /// </summary>
    Compatibility = 9,

    /// <summary>
    /// External service or system failure.
    /// </summary>
    ExternalService = 10
}