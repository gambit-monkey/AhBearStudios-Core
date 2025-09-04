using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Interface for retry configuration for failed health checks
    /// </summary>
    public interface IRetryConfig
    {
        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        int MaxRetries { get; }

        /// <summary>
        /// Delay between retry attempts
        /// </summary>
        TimeSpan RetryDelay { get; }

        /// <summary>
        /// Multiplier for exponential backoff (1.0 = no backoff)
        /// </summary>
        double BackoffMultiplier { get; }

        /// <summary>
        /// Maximum delay between retries (prevents excessive backoff)
        /// </summary>
        TimeSpan MaxRetryDelay { get; }

        /// <summary>
        /// Types of exceptions that should trigger retries
        /// </summary>
        HashSet<Type> RetriableExceptions { get; }

        /// <summary>
        /// Validates retry configuration
        /// </summary>
        /// <returns>List of validation errors</returns>
        List<string> Validate();
    }
}