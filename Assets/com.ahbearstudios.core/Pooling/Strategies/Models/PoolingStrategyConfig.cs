using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Pooling.Strategies.Models
{
    /// <summary>
    /// Configuration for pooling strategies.
    /// Provides strategy-specific settings and behavior customization.
    /// </summary>
    public sealed class PoolingStrategyConfig
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
        /// Creates a default configuration suitable for most scenarios.
        /// </summary>
        /// <param name="name">Name of the configuration</param>
        /// <returns>Default strategy configuration</returns>
        public static PoolingStrategyConfig Default(string name = "Default")
        {
            return new PoolingStrategyConfig
            {
                Name = name,
                PerformanceBudget = PerformanceBudget.For60FPS(),
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 5,
                CircuitBreakerRecoveryTime = TimeSpan.FromSeconds(30),
                EnableHealthMonitoring = true,
                HealthCheckInterval = TimeSpan.FromSeconds(30),
                EnableDetailedMetrics = true,
                MaxMetricsSamples = 100,
                EnableNetworkOptimizations = true,
                EnableUnityOptimizations = true,
                EnableDebugLogging = false,
                Tags = new HashSet<string> { "default", "production" }
            };
        }

        /// <summary>
        /// Creates a high-performance configuration optimized for intensive operations.
        /// </summary>
        /// <param name="name">Name of the configuration</param>
        /// <returns>High-performance strategy configuration</returns>
        public static PoolingStrategyConfig HighPerformance(string name = "HighPerformance")
        {
            return new PoolingStrategyConfig
            {
                Name = name,
                PerformanceBudget = PerformanceBudget.For60FPS(),
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 3,
                CircuitBreakerRecoveryTime = TimeSpan.FromSeconds(15),
                EnableHealthMonitoring = true,
                HealthCheckInterval = TimeSpan.FromSeconds(15),
                EnableDetailedMetrics = false, // Reduced overhead
                MaxMetricsSamples = 50,
                EnableNetworkOptimizations = true,
                EnableUnityOptimizations = true,
                EnableDebugLogging = false,
                Tags = new HashSet<string> { "high-performance", "production", "60fps" }
            };
        }

        /// <summary>
        /// Creates a memory-optimized configuration for resource-constrained environments.
        /// </summary>
        /// <param name="name">Name of the configuration</param>
        /// <returns>Memory-optimized strategy configuration</returns>
        public static PoolingStrategyConfig MemoryOptimized(string name = "MemoryOptimized")
        {
            return new PoolingStrategyConfig
            {
                Name = name,
                PerformanceBudget = PerformanceBudget.For30FPS(),
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 3,
                CircuitBreakerRecoveryTime = TimeSpan.FromSeconds(45),
                EnableHealthMonitoring = true,
                HealthCheckInterval = TimeSpan.FromMinutes(1),
                EnableDetailedMetrics = false, // Minimal memory usage
                MaxMetricsSamples = 20,
                EnableNetworkOptimizations = true,
                EnableUnityOptimizations = true,
                EnableDebugLogging = false,
                Tags = new HashSet<string> { "memory-optimized", "mobile", "low-memory" }
            };
        }

        /// <summary>
        /// Creates a development configuration with extensive debugging and monitoring.
        /// </summary>
        /// <param name="name">Name of the configuration</param>
        /// <returns>Development strategy configuration</returns>
        public static PoolingStrategyConfig Development(string name = "Development")
        {
            return new PoolingStrategyConfig
            {
                Name = name,
                PerformanceBudget = PerformanceBudget.ForDevelopment(),
                EnableCircuitBreaker = false, // Disabled for easier debugging
                CircuitBreakerFailureThreshold = 10,
                CircuitBreakerRecoveryTime = TimeSpan.FromMinutes(1),
                EnableHealthMonitoring = true,
                HealthCheckInterval = TimeSpan.FromSeconds(10),
                EnableDetailedMetrics = true,
                MaxMetricsSamples = 500, // More samples for analysis
                EnableNetworkOptimizations = false, // Easier debugging
                EnableUnityOptimizations = false, // Easier debugging
                EnableDebugLogging = true,
                Tags = new HashSet<string> { "development", "debug", "testing" }
            };
        }

        /// <summary>
        /// Creates a network-optimized configuration for intensive network operations.
        /// </summary>
        /// <param name="name">Name of the configuration</param>
        /// <returns>Network-optimized strategy configuration</returns>
        public static PoolingStrategyConfig NetworkOptimized(string name = "NetworkOptimized")
        {
            return new PoolingStrategyConfig
            {
                Name = name,
                PerformanceBudget = PerformanceBudget.For60FPS(),
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 2, // More sensitive for network issues
                CircuitBreakerRecoveryTime = TimeSpan.FromSeconds(10),
                EnableHealthMonitoring = true,
                HealthCheckInterval = TimeSpan.FromSeconds(5), // More frequent monitoring
                EnableDetailedMetrics = true,
                MaxMetricsSamples = 200,
                EnableNetworkOptimizations = true,
                EnableUnityOptimizations = true,
                EnableDebugLogging = false,
                Tags = new HashSet<string> { "network-optimized", "multiplayer", "fishnet" },
                CustomParameters = new Dictionary<string, object>
                {
                    ["NetworkSpikeThreshold"] = 0.8,
                    ["PreemptiveAllocationRatio"] = 0.2,
                    ["LatencyThresholdMs"] = 50.0,
                    ["ThroughputThresholdMbps"] = 10.0
                }
            };
        }

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