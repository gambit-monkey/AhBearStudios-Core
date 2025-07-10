namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Represents the retry strategy options for health check failures.
/// </summary>
public enum HealthCheckRetryStrategy : byte
{
    /// <summary>
    /// No retry strategy - fixed delay between attempts.
    /// </summary>
    None = 0,

    /// <summary>
    /// Fixed delay between retry attempts.
    /// </summary>
    FixedDelay = 1,

    /// <summary>
    /// Exponential backoff with increasing delays between attempts.
    /// </summary>
    ExponentialBackoff = 2,

    /// <summary>
    /// Linear backoff with linearly increasing delays.
    /// </summary>
    LinearBackoff = 3,

    /// <summary>
    /// Custom retry strategy defined by the health check implementation.
    /// </summary>
    Custom = 4
}