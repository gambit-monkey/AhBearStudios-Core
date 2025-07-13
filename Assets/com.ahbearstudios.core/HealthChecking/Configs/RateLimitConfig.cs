using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Configuration for rate limiting
/// </summary>
public sealed record RateLimitConfig
{
    /// <summary>
    /// Whether rate limiting is enabled
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Maximum requests per second allowed
    /// </summary>
    public double RequestsPerSecond { get; init; } = 100.0;

    /// <summary>
    /// Burst size for handling traffic spikes
    /// </summary>
    public int BurstSize { get; init; } = 150;

    /// <summary>
    /// Time window for rate calculation
    /// </summary>
    public TimeSpan RateWindow { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Whether to queue requests that exceed rate limit
    /// </summary>
    public bool QueueExcessRequests { get; init; } = false;

    /// <summary>
    /// Maximum queue size for excess requests
    /// </summary>
    public int MaxQueueSize { get; init; } = 50;

    /// <summary>
    /// Validates rate limit configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (RequestsPerSecond <= 0)
            errors.Add("RequestsPerSecond must be greater than zero");

        if (BurstSize < RequestsPerSecond)
            errors.Add("BurstSize should be greater than or equal to RequestsPerSecond");

        if (RateWindow <= TimeSpan.Zero)
            errors.Add("RateWindow must be greater than zero");

        if (MaxQueueSize < 0)
            errors.Add("MaxQueueSize must be non-negative");

        return errors;
    }
}