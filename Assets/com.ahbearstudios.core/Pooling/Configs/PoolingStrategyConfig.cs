using System;
using System.Collections.Generic;
using AhBearStudios.Core.Pooling.Models;

namespace AhBearStudios.Core.Pooling.Configs
{
    /// <summary>
    /// Configuration for pooling strategies.
    /// Provides strategy-specific settings and behavior customization.
    /// </summary>
    public sealed record PoolingStrategyConfig
    {
        /// <summary>
        /// Name of the strategy configuration.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Performance budget for the strategy.
        /// </summary>
        public PerformanceBudget PerformanceBudget { get; init; }

        /// <summary>
        /// Whether circuit breaker functionality is enabled.
        /// </summary>
        public bool EnableCircuitBreaker { get; init; }

        /// <summary>
        /// Number of consecutive failures before circuit breaker opens.
        /// </summary>
        public int CircuitBreakerFailureThreshold { get; init; }

        /// <summary>
        /// Time to wait before attempting to close circuit breaker.
        /// </summary>
        public TimeSpan CircuitBreakerRecoveryTime { get; init; }

        /// <summary>
        /// Whether health monitoring is enabled.
        /// </summary>
        public bool EnableHealthMonitoring { get; init; }

        /// <summary>
        /// Interval between health status checks.
        /// </summary>
        public TimeSpan HealthCheckInterval { get; init; }

        /// <summary>
        /// Whether to collect detailed metrics.
        /// </summary>
        public bool EnableDetailedMetrics { get; init; }

        /// <summary>
        /// Maximum number of metrics samples to keep in memory.
        /// </summary>
        public int MaxMetricsSamples { get; init; }

        /// <summary>
        /// Whether to enable network-specific optimizations.
        /// </summary>
        public bool EnableNetworkOptimizations { get; init; }

        /// <summary>
        /// Whether to enable Unity-specific performance optimizations.
        /// </summary>
        public bool EnableUnityOptimizations { get; init; }

        /// <summary>
        /// Whether to log strategy operations for debugging.
        /// </summary>
        public bool EnableDebugLogging { get; init; }

        /// <summary>
        /// Custom strategy-specific parameters.
        /// </summary>
        public Dictionary<string, object> CustomParameters { get; init; } = new();

        /// <summary>
        /// Tags for categorizing and filtering strategies.
        /// </summary>
        public HashSet<string> Tags { get; init; } = new();

        /// <summary>
        /// Validates that the configuration is valid and internally consistent.
        /// </summary>
        /// <returns>List of validation errors, empty if valid</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Name cannot be null or empty");

            if (PerformanceBudget == null)
                errors.Add("PerformanceBudget cannot be null");
            else if (!PerformanceBudget.IsValid())
                errors.Add("PerformanceBudget is not valid");

            if (CircuitBreakerFailureThreshold < 1)
                errors.Add("CircuitBreakerFailureThreshold must be at least 1");

            if (CircuitBreakerRecoveryTime <= TimeSpan.Zero)
                errors.Add("CircuitBreakerRecoveryTime must be positive");

            if (HealthCheckInterval <= TimeSpan.Zero)
                errors.Add("HealthCheckInterval must be positive");

            if (MaxMetricsSamples < 1)
                errors.Add("MaxMetricsSamples must be at least 1");

            return errors;
        }

        /// <summary>
        /// Gets whether the configuration is valid.
        /// </summary>
        public bool IsValid => Validate().Count == 0;

        /// <summary>
        /// Creates a copy of this configuration with modifications.
        /// </summary>
        /// <param name="name">New name (optional)</param>
        /// <returns>Modified configuration copy</returns>
        public PoolingStrategyConfig WithName(string name)
        {
            return this with { Name = name };
        }

        /// <summary>
        /// Creates a copy of this configuration with a different performance budget.
        /// </summary>
        /// <param name="performanceBudget">New performance budget</param>
        /// <returns>Modified configuration copy</returns>
        public PoolingStrategyConfig WithPerformanceBudget(PerformanceBudget performanceBudget)
        {
            return this with { PerformanceBudget = performanceBudget };
        }

        /// <summary>
        /// Creates a copy of this configuration with additional tags.
        /// </summary>
        /// <param name="additionalTags">Tags to add</param>
        /// <returns>Modified configuration copy</returns>
        public PoolingStrategyConfig WithAdditionalTags(params string[] additionalTags)
        {
            var newTags = new HashSet<string>(Tags);
            foreach (var tag in additionalTags)
                newTags.Add(tag);
            return this with { Tags = newTags };
        }
    }
}