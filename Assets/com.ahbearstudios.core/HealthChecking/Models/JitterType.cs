namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Types of jitter distribution for load balancing
/// </summary>
public enum JitterType
{
    /// <summary>
    /// Uniform random distribution
    /// </summary>
    Uniform,

    /// <summary>
    /// Gaussian (normal) distribution
    /// </summary>
    Gaussian,

    /// <summary>
    /// Exponential distribution
    /// </summary>
    Exponential
}