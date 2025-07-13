using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Retry configuration for failed health checks
/// </summary>
public sealed record RetryConfig
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetries { get; init; } = 0;

    /// <summary>
    /// Delay between retry attempts
    /// </summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Multiplier for exponential backoff (1.0 = no backoff)
    /// </summary>
    public double BackoffMultiplier { get; init; } = 1.0;

    /// <summary>
    /// Maximum delay between retries (prevents excessive backoff)
    /// </summary>
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Types of exceptions that should trigger retries
    /// </summary>
    public HashSet<Type> RetriableExceptions { get; init; } = new()
    {
        typeof(TimeoutException),
        //TODO Add Network Support typeof(System.Net.NetworkException),
        typeof(System.IO.IOException)
    };

    /// <summary>
    /// Validates retry configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (MaxRetries < 0)
            errors.Add("MaxRetries must be non-negative");

        if (MaxRetries > 10)
            errors.Add("MaxRetries should not exceed 10 for stability");

        if (RetryDelay < TimeSpan.Zero)
            errors.Add("RetryDelay must be non-negative");

        if (BackoffMultiplier < 1.0)
            errors.Add("BackoffMultiplier must be at least 1.0");

        if (MaxRetryDelay < RetryDelay)
            errors.Add("MaxRetryDelay must be greater than or equal to RetryDelay");

        return errors;
    }
}