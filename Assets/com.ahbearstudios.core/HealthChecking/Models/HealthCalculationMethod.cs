namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Methods for calculating overall health status
/// </summary>
public enum HealthCalculationMethod
{
    /// <summary>
    /// Simple percentage-based calculation
    /// </summary>
    Simple,

    /// <summary>
    /// Weighted average based on category weights
    /// </summary>
    WeightedAverage,

    /// <summary>
    /// Majority voting with category consideration
    /// </summary>
    MajorityVoting,

    /// <summary>
    /// Worst-case scenario (most pessimistic)
    /// </summary>
    WorstCase,

    /// <summary>
    /// Best-case scenario (most optimistic)
    /// </summary>
    BestCase,

    /// <summary>
    /// Trend-based calculation considering historical data
    /// </summary>
    TrendBased,

    /// <summary>
    /// Custom calculation using advanced rules
    /// </summary>
    Custom
}