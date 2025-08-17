using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Builders
{
    /// <summary>
    /// Builder for creating alert channel service configurations.
    /// Provides a fluent API for configuring channel management, health monitoring, and delivery settings.
    /// Handles complexity and validation to ensure valid configurations for the factory.
    /// </summary>
    public sealed class AlertChannelServiceBuilder
    {
        private bool _isEnabled = true;
        private TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(2);
        private TimeSpan _metricsCollectionInterval = TimeSpan.FromSeconds(30);
        private bool _enableAutoHealthChecks = true;
        private bool _enableMetricsCollection = true;
        private int _maxConsecutiveFailures = 3;
        private TimeSpan _defaultOperationTimeout = TimeSpan.FromSeconds(10);
        private bool _enableParallelDelivery = true;
        private int _maxParallelism = 4;
        private bool _continueOnChannelFailure = true;
        private readonly List<ChannelConfig> _initialChannels = new();
        private bool _enableAutoRetry = true;
        private RetryPolicyConfig _defaultRetryPolicy = RetryPolicyConfig.Default;
        private bool _enableCircuitBreaker = true;
        private CircuitBreakerConfig _circuitBreaker = CircuitBreakerConfig.Default;
        private bool _enableDetailedMetrics = false;
        private int _maxMetricsHistorySize = 1000;
        private int _emergencyChannelPriority = 1;

        /// <summary>
        /// Sets whether the channel service is enabled.
        /// </summary>
        /// <param name="enabled">Enable state</param>
        /// <returns>Builder instance for chaining</returns>
        public AlertChannelServiceBuilder WithEnabled(bool enabled)
        {
            _isEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets the health check interval.
        /// </summary>
        /// <param name="interval">Health check interval</param>
        /// <returns>Builder instance for chaining</returns>
        /// <exception cref="ArgumentException">Thrown when interval is invalid</exception>
        public AlertChannelServiceBuilder WithHealthCheckInterval(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
                throw new ArgumentException("Health check interval must be greater than zero.", nameof(interval));
            
            _healthCheckInterval = interval;
            return this;
        }

        /// <summary>
        /// Sets the metrics collection interval.
        /// </summary>
        /// <param name="interval">Metrics collection interval</param>
        /// <returns>Builder instance for chaining</returns>
        /// <exception cref="ArgumentException">Thrown when interval is invalid</exception>
        public AlertChannelServiceBuilder WithMetricsCollectionInterval(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
                throw new ArgumentException("Metrics collection interval must be greater than zero.", nameof(interval));
            
            _metricsCollectionInterval = interval;
            return this;
        }

        /// <summary>
        /// Enables or disables automatic health checks.
        /// </summary>
        /// <param name="enable">Enable auto health checks</param>
        /// <returns>Builder instance for chaining</returns>
        public AlertChannelServiceBuilder WithAutoHealthChecks(bool enable)
        {
            _enableAutoHealthChecks = enable;
            return this;
        }

        /// <summary>
        /// Enables or disables metrics collection.
        /// </summary>
        /// <param name="enable">Enable metrics collection</param>
        /// <returns>Builder instance for chaining</returns>
        public AlertChannelServiceBuilder WithMetricsCollection(bool enable)
        {
            _enableMetricsCollection = enable;
            return this;
        }

        /// <summary>
        /// Sets the maximum consecutive failures before marking a channel unhealthy.
        /// </summary>
        /// <param name="maxFailures">Maximum consecutive failures</param>
        /// <returns>Builder instance for chaining</returns>
        /// <exception cref="ArgumentException">Thrown when value is invalid</exception>
        public AlertChannelServiceBuilder WithMaxConsecutiveFailures(int maxFailures)
        {
            if (maxFailures <= 0)
                throw new ArgumentException("Max consecutive failures must be greater than zero.", nameof(maxFailures));
            
            _maxConsecutiveFailures = maxFailures;
            return this;
        }

        /// <summary>
        /// Sets the default operation timeout.
        /// </summary>
        /// <param name="timeout">Operation timeout</param>
        /// <returns>Builder instance for chaining</returns>
        /// <exception cref="ArgumentException">Thrown when timeout is invalid</exception>
        public AlertChannelServiceBuilder WithDefaultOperationTimeout(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentException("Default operation timeout must be greater than zero.", nameof(timeout));
            
            _defaultOperationTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Configures parallel delivery settings.
        /// </summary>
        /// <param name="enable">Enable parallel delivery</param>
        /// <param name="maxParallelism">Maximum degree of parallelism</param>
        /// <returns>Builder instance for chaining</returns>
        /// <exception cref="ArgumentException">Thrown when parallelism is invalid</exception>
        public AlertChannelServiceBuilder WithParallelDelivery(bool enable, int maxParallelism = 4)
        {
            if (maxParallelism <= 0)
                throw new ArgumentException("Max parallelism must be greater than zero.", nameof(maxParallelism));
            
            _enableParallelDelivery = enable;
            _maxParallelism = maxParallelism;
            return this;
        }

        /// <summary>
        /// Sets whether to continue delivery to other channels if one fails.
        /// </summary>
        /// <param name="continueOnFailure">Continue on failure flag</param>
        /// <returns>Builder instance for chaining</returns>
        public AlertChannelServiceBuilder WithContinueOnChannelFailure(bool continueOnFailure)
        {
            _continueOnChannelFailure = continueOnFailure;
            return this;
        }

        /// <summary>
        /// Adds an initial channel configuration.
        /// </summary>
        /// <param name="channelConfig">Channel configuration to add</param>
        /// <returns>Builder instance for chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        public AlertChannelServiceBuilder AddInitialChannel(ChannelConfig channelConfig)
        {
            if (channelConfig == null)
                throw new ArgumentNullException(nameof(channelConfig));
            
            channelConfig.Validate();
            _initialChannels.Add(channelConfig);
            return this;
        }

        /// <summary>
        /// Adds multiple initial channel configurations.
        /// </summary>
        /// <param name="channelConfigs">Channel configurations to add</param>
        /// <returns>Builder instance for chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when configs is null</exception>
        public AlertChannelServiceBuilder AddInitialChannels(IEnumerable<ChannelConfig> channelConfigs)
        {
            if (channelConfigs == null)
                throw new ArgumentNullException(nameof(channelConfigs));
            
            foreach (var config in channelConfigs)
            {
                AddInitialChannel(config);
            }
            return this;
        }

        /// <summary>
        /// Configures retry policy settings.
        /// </summary>
        /// <param name="enable">Enable auto retry</param>
        /// <param name="retryPolicy">Retry policy configuration</param>
        /// <returns>Builder instance for chaining</returns>
        public AlertChannelServiceBuilder WithRetryPolicy(bool enable, RetryPolicyConfig retryPolicy = null)
        {
            _enableAutoRetry = enable;
            _defaultRetryPolicy = retryPolicy ?? RetryPolicyConfig.Default;
            _defaultRetryPolicy.Validate();
            return this;
        }

        /// <summary>
        /// Configures circuit breaker settings.
        /// </summary>
        /// <param name="enable">Enable circuit breaker</param>
        /// <param name="config">Circuit breaker configuration</param>
        /// <returns>Builder instance for chaining</returns>
        public AlertChannelServiceBuilder WithCircuitBreaker(bool enable, CircuitBreakerConfig config = null)
        {
            _enableCircuitBreaker = enable;
            _circuitBreaker = config ?? CircuitBreakerConfig.Default;
            _circuitBreaker.Validate();
            return this;
        }

        /// <summary>
        /// Configures detailed metrics settings.
        /// </summary>
        /// <param name="enable">Enable detailed metrics</param>
        /// <param name="maxHistorySize">Maximum metrics history size</param>
        /// <returns>Builder instance for chaining</returns>
        /// <exception cref="ArgumentException">Thrown when history size is invalid</exception>
        public AlertChannelServiceBuilder WithDetailedMetrics(bool enable, int maxHistorySize = 1000)
        {
            if (maxHistorySize < 0)
                throw new ArgumentException("Max metrics history size cannot be negative.", nameof(maxHistorySize));
            
            _enableDetailedMetrics = enable;
            _maxMetricsHistorySize = maxHistorySize;
            return this;
        }

        /// <summary>
        /// Sets the emergency channel priority.
        /// </summary>
        /// <param name="priority">Priority level (1-1000)</param>
        /// <returns>Builder instance for chaining</returns>
        /// <exception cref="ArgumentException">Thrown when priority is invalid</exception>
        public AlertChannelServiceBuilder WithEmergencyChannelPriority(int priority)
        {
            if (priority < 1 || priority > 1000)
                throw new ArgumentException("Emergency channel priority must be between 1 and 1000.", nameof(priority));
            
            _emergencyChannelPriority = priority;
            return this;
        }

        /// <summary>
        /// Applies a preset configuration template.
        /// </summary>
        /// <param name="preset">Preset type to apply</param>
        /// <returns>Builder instance for chaining</returns>
        public AlertChannelServiceBuilder UsePreset(ChannelServicePreset preset)
        {
            switch (preset)
            {
                case ChannelServicePreset.Default:
                    ApplyDefaultPreset();
                    break;
                case ChannelServicePreset.HighPerformance:
                    ApplyHighPerformancePreset();
                    break;
                case ChannelServicePreset.Debug:
                    ApplyDebugPreset();
                    break;
                case ChannelServicePreset.Production:
                    ApplyProductionPreset();
                    break;
                default:
                    throw new ArgumentException($"Unknown preset: {preset}", nameof(preset));
            }
            return this;
        }

        /// <summary>
        /// Builds the alert channel service configuration.
        /// </summary>
        /// <returns>Validated configuration instance</returns>
        public AlertChannelServiceConfig Build()
        {
            var config = new AlertChannelServiceConfig
            {
                IsEnabled = _isEnabled,
                HealthCheckInterval = _healthCheckInterval,
                MetricsCollectionInterval = _metricsCollectionInterval,
                EnableAutoHealthChecks = _enableAutoHealthChecks,
                EnableMetricsCollection = _enableMetricsCollection,
                MaxConsecutiveFailures = _maxConsecutiveFailures,
                DefaultOperationTimeout = _defaultOperationTimeout,
                EnableParallelDelivery = _enableParallelDelivery,
                MaxParallelism = _maxParallelism,
                ContinueOnChannelFailure = _continueOnChannelFailure,
                InitialChannels = _initialChannels.ToArray(),
                EnableAutoRetry = _enableAutoRetry,
                DefaultRetryPolicy = _defaultRetryPolicy,
                EnableCircuitBreaker = _enableCircuitBreaker,
                CircuitBreaker = _circuitBreaker,
                EnableDetailedMetrics = _enableDetailedMetrics,
                MaxMetricsHistorySize = _maxMetricsHistorySize,
                EmergencyChannelPriority = _emergencyChannelPriority
            };

            config.Validate();
            return config;
        }

        private void ApplyDefaultPreset()
        {
            _isEnabled = true;
            _healthCheckInterval = TimeSpan.FromMinutes(2);
            _metricsCollectionInterval = TimeSpan.FromSeconds(30);
            _enableAutoHealthChecks = true;
            _enableMetricsCollection = true;
            _maxConsecutiveFailures = 3;
            _defaultOperationTimeout = TimeSpan.FromSeconds(10);
            _enableParallelDelivery = true;
            _maxParallelism = 4;
            _continueOnChannelFailure = true;
            _enableAutoRetry = true;
            _defaultRetryPolicy = RetryPolicyConfig.Default;
            _enableCircuitBreaker = true;
            _circuitBreaker = CircuitBreakerConfig.Default;
            _enableDetailedMetrics = false;
            _maxMetricsHistorySize = 1000;
            _emergencyChannelPriority = 1;
        }

        private void ApplyHighPerformancePreset()
        {
            _isEnabled = true;
            _healthCheckInterval = TimeSpan.FromMinutes(5);
            _metricsCollectionInterval = TimeSpan.FromMinutes(1);
            _enableAutoHealthChecks = false;
            _enableMetricsCollection = false;
            _maxConsecutiveFailures = 5;
            _defaultOperationTimeout = TimeSpan.FromSeconds(5);
            _enableParallelDelivery = true;
            _maxParallelism = 8;
            _continueOnChannelFailure = true;
            _enableAutoRetry = false;
            _defaultRetryPolicy = RetryPolicyConfig.NoRetry;
            _enableCircuitBreaker = true;
            _circuitBreaker = CircuitBreakerConfig.Aggressive;
            _enableDetailedMetrics = false;
            _maxMetricsHistorySize = 100;
            _emergencyChannelPriority = 1;
        }

        private void ApplyDebugPreset()
        {
            _isEnabled = true;
            _healthCheckInterval = TimeSpan.FromSeconds(30);
            _metricsCollectionInterval = TimeSpan.FromSeconds(10);
            _enableAutoHealthChecks = true;
            _enableMetricsCollection = true;
            _maxConsecutiveFailures = 1;
            _defaultOperationTimeout = TimeSpan.FromSeconds(30);
            _enableParallelDelivery = false;
            _maxParallelism = 1;
            _continueOnChannelFailure = false;
            _enableAutoRetry = true;
            _defaultRetryPolicy = new RetryPolicyConfig
            {
                MaxAttempts = 5,
                BaseDelay = TimeSpan.FromSeconds(2),
                MaxDelay = TimeSpan.FromMinutes(1),
                BackoffMultiplier = 1.5,
                JitterEnabled = false
            };
            _enableCircuitBreaker = false;
            _circuitBreaker = CircuitBreakerConfig.Disabled;
            _enableDetailedMetrics = true;
            _maxMetricsHistorySize = 10000;
            _emergencyChannelPriority = 1;
        }

        private void ApplyProductionPreset()
        {
            _isEnabled = true;
            _healthCheckInterval = TimeSpan.FromMinutes(3);
            _metricsCollectionInterval = TimeSpan.FromMinutes(1);
            _enableAutoHealthChecks = true;
            _enableMetricsCollection = true;
            _maxConsecutiveFailures = 3;
            _defaultOperationTimeout = TimeSpan.FromSeconds(15);
            _enableParallelDelivery = true;
            _maxParallelism = 6;
            _continueOnChannelFailure = true;
            _enableAutoRetry = true;
            _defaultRetryPolicy = new RetryPolicyConfig
            {
                MaxAttempts = 3,
                BaseDelay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromMinutes(2),
                BackoffMultiplier = 2.0,
                JitterEnabled = true,
                JitterMaxPercentage = 0.1
            };
            _enableCircuitBreaker = true;
            _circuitBreaker = new CircuitBreakerConfig
            {
                FailureThreshold = 5,
                FailureWindow = TimeSpan.FromMinutes(2),
                OpenDuration = TimeSpan.FromMinutes(5),
                SuccessThreshold = 3,
                IsEnabled = true
            };
            _enableDetailedMetrics = false;
            _maxMetricsHistorySize = 500;
            _emergencyChannelPriority = 1;
        }
    }

    /// <summary>
    /// Preset configurations for the channel service.
    /// </summary>
    public enum ChannelServicePreset
    {
        /// <summary>
        /// Default configuration suitable for most scenarios.
        /// </summary>
        Default,

        /// <summary>
        /// Optimized for high-performance, low-latency scenarios.
        /// </summary>
        HighPerformance,

        /// <summary>
        /// Debug configuration with verbose monitoring.
        /// </summary>
        Debug,

        /// <summary>
        /// Production-ready configuration with balanced settings.
        /// </summary>
        Production
    }
}