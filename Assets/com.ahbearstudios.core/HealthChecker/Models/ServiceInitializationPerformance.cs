namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Represents the performance category of service initialization.
/// </summary>
public enum ServiceInitializationPerformance : byte
{
    /// <summary>
    /// Initialization completed in under 100ms - excellent performance.
    /// </summary>
    Excellent = 0,

    /// <summary>
    /// Initialization completed in 100-500ms - good performance.
    /// </summary>
    Good = 1,

    /// <summary>
    /// Initialization completed in 500-1000ms - acceptable performance.
    /// </summary>
    Acceptable = 2,

    /// <summary>
    /// Initialization completed in 1-5 seconds - slow performance.
    /// </summary>
    Slow = 3,

    /// <summary>
    /// Initialization took over 5 seconds - poor performance.
    /// </summary>
    Poor = 4
}