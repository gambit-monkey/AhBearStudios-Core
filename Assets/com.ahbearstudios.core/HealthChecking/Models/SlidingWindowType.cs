namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Enumeration of sliding window types for circuit breaker failure rate calculation.
/// </summary>
public enum SlidingWindowType : byte
{
    /// <summary>
    /// Window based on a fixed number of requests/calls.
    /// The sliding window maintains a count of the most recent N requests.
    /// </summary>
    CountBased = 0,

    /// <summary>
    /// Window based on a fixed time duration.
    /// The sliding window maintains requests within the last N time units.
    /// </summary>
    TimeBased = 1
}