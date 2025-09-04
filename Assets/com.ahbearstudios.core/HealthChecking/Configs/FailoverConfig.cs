using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
    /// Configuration for failover behavior when circuit is open
    /// </summary>
    public sealed record FailoverConfig : IFailoverConfig
    {
        /// <summary>
        /// Whether failover is enabled
        /// </summary>
        public bool Enabled { get; init; } = false;

        /// <summary>
        /// Type of failover strategy to use
        /// </summary>
        public FailoverStrategy Strategy { get; init; } = FailoverStrategy.ReturnDefault;

        /// <summary>
        /// Default value to return when circuit is open (for ReturnDefault strategy)
        /// </summary>
        public object DefaultValue { get; init; }

        /// <summary>
        /// Alternative endpoints to try (for Retry strategy)
        /// </summary>
        public List<string> AlternativeEndpoints { get; init; } = new();

        /// <summary>
        /// Maximum time to spend on failover attempts
        /// </summary>
        public TimeSpan MaxFailoverDuration { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Whether to enable fallback caching
        /// </summary>
        public bool EnableFallbackCache { get; init; } = false;

        /// <summary>
        /// Cache duration for fallback values
        /// </summary>
        public TimeSpan FallbackCacheDuration { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Validates failover configuration
        /// </summary>
        /// <returns>List of validation errors</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (!Enum.IsDefined(typeof(FailoverStrategy), Strategy))
                errors.Add($"Invalid failover strategy: {Strategy}");

            if (MaxFailoverDuration < TimeSpan.Zero)
                errors.Add("MaxFailoverDuration must be non-negative");

            if (FallbackCacheDuration < TimeSpan.Zero)
                errors.Add("FallbackCacheDuration must be non-negative");

            if (Strategy == FailoverStrategy.Retry && AlternativeEndpoints.Count == 0)
                errors.Add("AlternativeEndpoints must be provided when using Retry strategy");

            return errors;
        }
    }