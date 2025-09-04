using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Interface for rate limiting configuration
    /// </summary>
    public interface IRateLimitConfig
    {
        /// <summary>
        /// Whether rate limiting is enabled
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Maximum requests per second allowed
        /// </summary>
        double RequestsPerSecond { get; }

        /// <summary>
        /// Burst size for handling traffic spikes
        /// </summary>
        int BurstSize { get; }

        /// <summary>
        /// Time window for rate calculation
        /// </summary>
        TimeSpan RateWindow { get; }

        /// <summary>
        /// Whether to queue requests that exceed rate limit
        /// </summary>
        bool QueueExcessRequests { get; }

        /// <summary>
        /// Maximum queue size for excess requests
        /// </summary>
        int MaxQueueSize { get; }

        /// <summary>
        /// Validates rate limit configuration
        /// </summary>
        /// <returns>List of validation errors</returns>
        List<string> Validate();
    }
}