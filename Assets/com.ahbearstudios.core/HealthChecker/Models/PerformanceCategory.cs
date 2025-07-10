namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Defines performance categories for health check creation operations.
/// </summary>
public enum PerformanceCategory : byte
{
    /// <summary>
    /// Excellent performance (< 10ms).
    /// </summary>
    Excellent = 0,

    /// <summary>
    /// Good performance (10-50ms).
    /// </summary>
    Good = 1,

    /// <summary>
    /// Acceptable performance (50-200ms).
    /// </summary>
    Acceptable = 2,

    /// <summary>
    /// Slow performance (200ms-1s).
    /// </summary>
    Slow = 3,

    /// <summary>
    /// Very slow performance (> 1s).
    /// </summary>
    VerySlow = 4
}