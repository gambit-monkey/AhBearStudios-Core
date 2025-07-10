namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Summary information about a health check creation for metrics and monitoring.
/// </summary>
public readonly record struct HealthCheckCreationSummary(
    string HealthCheckType,
    double CreationDurationMs,
    PerformanceCategory PerformanceCategory,
    string Environment,
    bool HasWarnings,
    int TagCount,
    DateTime Timestamp)
{
    /// <summary>
    /// Gets whether this creation was slow based on performance category.
    /// </summary>
    public bool IsSlowCreation => PerformanceCategory >= PerformanceCategory.Slow;

    /// <summary>
    /// Gets whether this creation had excellent performance.
    /// </summary>
    public bool IsExcellentPerformance => PerformanceCategory == PerformanceCategory.Excellent;

    /// <summary>
    /// Gets the age of this creation relative to the current time.
    /// </summary>
    public TimeSpan Age => DateTime.UtcNow - Timestamp;
}