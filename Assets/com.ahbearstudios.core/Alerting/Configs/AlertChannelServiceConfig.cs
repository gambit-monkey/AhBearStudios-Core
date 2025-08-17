using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Configs
{
    /// <summary>
    /// Configuration for the alert channel service.
    /// Defines global settings for channel management, health monitoring, and alert delivery orchestration.
    /// Optimized for Unity game development with performance-first design.
    /// </summary>
    public sealed record AlertChannelServiceConfig
    {
        /// <summary>
        /// Gets whether the channel service is enabled.
        /// </summary>
        public bool IsEnabled { get; init; } = true;

        /// <summary>
        /// Gets the interval for performing health checks on channels.
        /// Regular health checks ensure channels remain operational.
        /// </summary>
        public TimeSpan HealthCheckInterval { get; init; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Gets the interval for collecting and updating metrics.
        /// Metrics help monitor channel performance and reliability.
        /// </summary>
        public TimeSpan MetricsCollectionInterval { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets whether automatic health monitoring is enabled.
        /// When enabled, channels are periodically checked for health.
        /// </summary>
        public bool EnableAutoHealthChecks { get; init; } = true;

        /// <summary>
        /// Gets whether automatic metrics collection is enabled.
        /// When enabled, channel metrics are collected periodically.
        /// </summary>
        public bool EnableMetricsCollection { get; init; } = true;

        /// <summary>
        /// Gets the maximum number of consecutive failures before a channel is marked unhealthy.
        /// </summary>
        public int MaxConsecutiveFailures { get; init; } = 3;

        /// <summary>
        /// Gets the default timeout for channel operations.
        /// Operations exceeding this timeout are considered failed.
        /// </summary>
        public TimeSpan DefaultOperationTimeout { get; init; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets whether parallel delivery to multiple channels is enabled.
        /// Parallel delivery improves performance but may increase resource usage.
        /// </summary>
        public bool EnableParallelDelivery { get; init; } = true;

        /// <summary>
        /// Gets the maximum degree of parallelism for channel operations.
        /// Limits concurrent channel operations to prevent resource exhaustion.
        /// </summary>
        public int MaxParallelism { get; init; } = 4;

        /// <summary>
        /// Gets whether to continue delivery to other channels if one fails.
        /// When false, delivery stops at the first failure.
        /// </summary>
        public bool ContinueOnChannelFailure { get; init; } = true;

        /// <summary>
        /// Gets the initial channel configurations to register on startup.
        /// Channels are automatically registered when the service starts.
        /// </summary>
        public IReadOnlyList<ChannelConfig> InitialChannels { get; init; } = Array.Empty<ChannelConfig>();

        /// <summary>
        /// Gets whether to automatically retry failed channel operations.
        /// </summary>
        public bool EnableAutoRetry { get; init; } = true;

        /// <summary>
        /// Gets the default retry policy for failed operations.
        /// </summary>
        public RetryPolicyConfig DefaultRetryPolicy { get; init; } = RetryPolicyConfig.Default;

        /// <summary>
        /// Gets whether to enable circuit breaker pattern for failing channels.
        /// Circuit breaker temporarily disables channels that are failing consistently.
        /// </summary>
        public bool EnableCircuitBreaker { get; init; } = true;

        /// <summary>
        /// Gets the circuit breaker configuration.
        /// </summary>
        public CircuitBreakerConfig CircuitBreaker { get; init; } = CircuitBreakerConfig.Default;

        /// <summary>
        /// Gets whether to track detailed performance metrics.
        /// Detailed metrics provide more insight but may impact performance.
        /// </summary>
        public bool EnableDetailedMetrics { get; init; } = false;

        /// <summary>
        /// Gets the maximum number of alerts to keep in metrics history.
        /// </summary>
        public int MaxMetricsHistorySize { get; init; } = 1000;

        /// <summary>
        /// Gets the priority for emergency channel fallback.
        /// Emergency channels are used when primary channels fail.
        /// </summary>
        public int EmergencyChannelPriority { get; init; } = 1;

        /// <summary>
        /// Validates the configuration for correctness.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (HealthCheckInterval <= TimeSpan.Zero)
                throw new InvalidOperationException("Health check interval must be greater than zero.");

            if (MetricsCollectionInterval <= TimeSpan.Zero)
                throw new InvalidOperationException("Metrics collection interval must be greater than zero.");

            if (MaxConsecutiveFailures <= 0)
                throw new InvalidOperationException("Max consecutive failures must be greater than zero.");

            if (DefaultOperationTimeout <= TimeSpan.Zero)
                throw new InvalidOperationException("Default operation timeout must be greater than zero.");

            if (MaxParallelism <= 0)
                throw new InvalidOperationException("Max parallelism must be greater than zero.");

            if (MaxMetricsHistorySize < 0)
                throw new InvalidOperationException("Max metrics history size cannot be negative.");

            if (EmergencyChannelPriority < 1 || EmergencyChannelPriority > 1000)
                throw new InvalidOperationException("Emergency channel priority must be between 1 and 1000.");

            DefaultRetryPolicy?.Validate();
            CircuitBreaker?.Validate();

            foreach (var channel in InitialChannels)
            {
                channel?.Validate();
            }
        }

        /// <summary>
        /// Creates a default configuration suitable for most game scenarios.
        /// </summary>
        public static AlertChannelServiceConfig Default => new()
        {
            IsEnabled = true,
            HealthCheckInterval = TimeSpan.FromMinutes(2),
            MetricsCollectionInterval = TimeSpan.FromSeconds(30),
            EnableAutoHealthChecks = true,
            EnableMetricsCollection = true,
            MaxConsecutiveFailures = 3,
            DefaultOperationTimeout = TimeSpan.FromSeconds(10),
            EnableParallelDelivery = true,
            MaxParallelism = 4,
            ContinueOnChannelFailure = true,
            EnableAutoRetry = true,
            DefaultRetryPolicy = RetryPolicyConfig.Default,
            EnableCircuitBreaker = true,
            CircuitBreaker = CircuitBreakerConfig.Default,
            EnableDetailedMetrics = false,
            MaxMetricsHistorySize = 1000,
            EmergencyChannelPriority = 1
        };

        /// <summary>
        /// Creates a performance-optimized configuration for high-throughput scenarios.
        /// </summary>
        public static AlertChannelServiceConfig HighPerformance => new()
        {
            IsEnabled = true,
            HealthCheckInterval = TimeSpan.FromMinutes(5),
            MetricsCollectionInterval = TimeSpan.FromMinutes(1),
            EnableAutoHealthChecks = false,
            EnableMetricsCollection = false,
            MaxConsecutiveFailures = 5,
            DefaultOperationTimeout = TimeSpan.FromSeconds(5),
            EnableParallelDelivery = true,
            MaxParallelism = 8,
            ContinueOnChannelFailure = true,
            EnableAutoRetry = false,
            DefaultRetryPolicy = RetryPolicyConfig.NoRetry,
            EnableCircuitBreaker = true,
            CircuitBreaker = CircuitBreakerConfig.Aggressive,
            EnableDetailedMetrics = false,
            MaxMetricsHistorySize = 100,
            EmergencyChannelPriority = 1
        };

        /// <summary>
        /// Creates a debug configuration with verbose monitoring and no optimizations.
        /// </summary>
        public static AlertChannelServiceConfig Debug => new()
        {
            IsEnabled = true,
            HealthCheckInterval = TimeSpan.FromSeconds(30),
            MetricsCollectionInterval = TimeSpan.FromSeconds(10),
            EnableAutoHealthChecks = true,
            EnableMetricsCollection = true,
            MaxConsecutiveFailures = 1,
            DefaultOperationTimeout = TimeSpan.FromSeconds(30),
            EnableParallelDelivery = false,
            MaxParallelism = 1,
            ContinueOnChannelFailure = false,
            EnableAutoRetry = true,
            DefaultRetryPolicy = new RetryPolicyConfig
            {
                MaxAttempts = 5,
                BaseDelay = TimeSpan.FromSeconds(2),
                MaxDelay = TimeSpan.FromMinutes(1),
                BackoffMultiplier = 1.5,
                JitterEnabled = false
            },
            EnableCircuitBreaker = false,
            CircuitBreaker = CircuitBreakerConfig.Disabled,
            EnableDetailedMetrics = true,
            MaxMetricsHistorySize = 10000,
            EmergencyChannelPriority = 1
        };
    }

    /// <summary>
    /// Configuration for circuit breaker pattern implementation.
    /// Prevents cascading failures by temporarily disabling failing channels.
    /// </summary>
    public sealed record CircuitBreakerConfig
    {
        /// <summary>
        /// Gets the number of failures before opening the circuit.
        /// </summary>
        public int FailureThreshold { get; init; } = 5;

        /// <summary>
        /// Gets the time window for counting failures.
        /// </summary>
        public TimeSpan FailureWindow { get; init; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets the duration to keep the circuit open before attempting recovery.
        /// </summary>
        public TimeSpan OpenDuration { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets the number of successful operations required to close the circuit.
        /// </summary>
        public int SuccessThreshold { get; init; } = 3;

        /// <summary>
        /// Gets whether the circuit breaker is enabled.
        /// </summary>
        public bool IsEnabled { get; init; } = true;

        /// <summary>
        /// Validates the circuit breaker configuration.
        /// </summary>
        public void Validate()
        {
            if (FailureThreshold <= 0)
                throw new InvalidOperationException("Failure threshold must be greater than zero.");

            if (FailureWindow <= TimeSpan.Zero)
                throw new InvalidOperationException("Failure window must be greater than zero.");

            if (OpenDuration <= TimeSpan.Zero)
                throw new InvalidOperationException("Open duration must be greater than zero.");

            if (SuccessThreshold <= 0)
                throw new InvalidOperationException("Success threshold must be greater than zero.");
        }

        /// <summary>
        /// Gets the default circuit breaker configuration.
        /// </summary>
        public static CircuitBreakerConfig Default => new()
        {
            FailureThreshold = 5,
            FailureWindow = TimeSpan.FromMinutes(1),
            OpenDuration = TimeSpan.FromMinutes(5),
            SuccessThreshold = 3,
            IsEnabled = true
        };

        /// <summary>
        /// Gets an aggressive circuit breaker configuration for critical systems.
        /// </summary>
        public static CircuitBreakerConfig Aggressive => new()
        {
            FailureThreshold = 3,
            FailureWindow = TimeSpan.FromSeconds(30),
            OpenDuration = TimeSpan.FromMinutes(10),
            SuccessThreshold = 5,
            IsEnabled = true
        };

        /// <summary>
        /// Gets a disabled circuit breaker configuration.
        /// </summary>
        public static CircuitBreakerConfig Disabled => new()
        {
            IsEnabled = false
        };
    }
}