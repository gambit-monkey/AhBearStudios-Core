namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Represents the performance category of the clear operation.
/// </summary>
public enum ClearOperationPerformance : byte
{
    /// <summary>
    /// Clear operation completed in under 100ms - excellent performance.
    /// </summary>
    Excellent = 0,

    /// <summary>
    /// Clear operation completed in 100-500ms - good performance.
    /// </summary>
    Good = 1,

    /// <summary>
    /// Clear operation completed in 500-1000ms - acceptable performance.
    /// </summary>
    Acceptable = 2,

    /// <summary>
    /// Clear operation completed in 1-5 seconds - slow performance.
    /// </summary>
    Slow = 3,

    /// <summary>
    /// Clear operation took over 5 seconds - poor performance.
    /// </summary>
    Poor = 4,

    /// <summary>
    /// Clear operation failed to complete successfully.
    /// </summary>
    Failed = 5
}