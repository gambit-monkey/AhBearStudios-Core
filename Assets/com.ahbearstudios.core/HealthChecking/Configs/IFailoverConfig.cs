using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Interface for failover behavior configuration when circuit is open
    /// </summary>
    public interface IFailoverConfig
    {
        /// <summary>
        /// Whether failover is enabled
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Type of failover strategy to use
        /// </summary>
        FailoverStrategy Strategy { get; }

        /// <summary>
        /// Default value to return when circuit is open (for ReturnDefault strategy)
        /// </summary>
        object DefaultValue { get; }

        /// <summary>
        /// Alternative endpoints to try (for Retry strategy)
        /// </summary>
        List<string> AlternativeEndpoints { get; }

        /// <summary>
        /// Maximum time to spend on failover attempts
        /// </summary>
        TimeSpan MaxFailoverDuration { get; }

        /// <summary>
        /// Whether to enable fallback caching
        /// </summary>
        bool EnableFallbackCache { get; }

        /// <summary>
        /// Cache duration for fallback values
        /// </summary>
        TimeSpan FallbackCacheDuration { get; }

        /// <summary>
        /// Validates failover configuration
        /// </summary>
        /// <returns>List of validation errors</returns>
        List<string> Validate();
    }
}