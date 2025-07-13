namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Types of sliding windows for failure rate calculation
/// </summary>
public enum SlidingWindowType
{
    /// <summary>
    /// Count-based sliding window (tracks last N requests)
    /// </summary>
    CountBased,

    /// <summary>
    /// Time-based sliding window (tracks requests in last N time units)
    /// </summary>
    TimeBased
}