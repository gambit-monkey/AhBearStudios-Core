namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Represents performance trend information over a specific time period.
/// </summary>
public readonly record struct PerformanceTrend(
    int PeriodMinutes,
    int TotalEvents,
    int SuccessfulEvents,
    int FailedEvents,
    double? AverageCreationTimeMs,
    TrendDirection TrendDirection)
{
    /// <summary>
    /// Gets the success rate for this period as a percentage.
    /// </summary>
    public double SuccessRate => TotalEvents == 0 ? 0.0 : (double)SuccessfulEvents / TotalEvents * 100.0;

    /// <summary>
    /// Gets the failure rate for this period as a percentage.
    /// </summary>
    public double FailureRate => TotalEvents == 0 ? 0.0 : (double)FailedEvents / TotalEvents * 100.0;

    /// <summary>
    /// Gets the event rate (events per minute) for this period.
    /// </summary>
    public double EventRate => PeriodMinutes == 0 ? 0.0 : (double)TotalEvents / PeriodMinutes;
}