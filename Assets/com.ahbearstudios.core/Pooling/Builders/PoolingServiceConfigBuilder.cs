using System;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Builder interface for creating PoolingService configurations.
    /// Provides fluent API for complex configuration setup following CLAUDE.md patterns.
    /// </summary>
    public interface IPoolingServiceConfigBuilder
    {
        /// <summary>
        /// Sets the service name for identification.
        /// </summary>
        /// <param name="serviceName">Name of the pooling service</param>
        /// <returns>Builder instance for fluent API</returns>
        IPoolingServiceConfigBuilder WithServiceName(string serviceName);

        /// <summary>
        /// Configures performance settings.
        /// </summary>
        /// <param name="defaultBudget">Default performance budget</param>
        /// <param name="maxPools">Maximum number of registered pools</param>
        /// <param name="asyncTimeout">Timeout for async operations</param>
        /// <returns>Builder instance for fluent API</returns>
        IPoolingServiceConfigBuilder WithPerformanceSettings(
            PerformanceBudget defaultBudget = null,
            int? maxPools = null,
            TimeSpan? asyncTimeout = null);

        /// <summary>
        /// Enables auto-scaling with specified settings.
        /// </summary>
        /// <param name="checkInterval">Interval between scaling checks</param>
        /// <param name="expansionThreshold">Utilization threshold for expansion</param>
        /// <param name="contractionThreshold">Utilization threshold for contraction</param>
        /// <returns>Builder instance for fluent API</returns>
        IPoolingServiceConfigBuilder WithAutoScaling(
            TimeSpan? checkInterval = null,
            double? expansionThreshold = null,
            double? contractionThreshold = null);

        /// <summary>
        /// Configures error recovery settings.
        /// </summary>
        /// <param name="maxRetries">Maximum retry attempts</param>
        /// <param name="retryDelay">Delay between retries</param>
        /// <param name="enableEmergencyRecovery">Enable emergency recovery mechanisms</param>
        /// <returns>Builder instance for fluent API</returns>
        IPoolingServiceConfigBuilder WithErrorRecovery(
            int? maxRetries = null,
            TimeSpan? retryDelay = null,
            bool? enableEmergencyRecovery = null);

        /// <summary>
        /// Configures validation settings.
        /// </summary>
        /// <param name="enableObjectValidation">Enable object validation during return</param>
        /// <param name="enablePoolValidation">Enable pool validation checks</param>
        /// <param name="validationInterval">Interval for automatic validation</param>
        /// <returns>Builder instance for fluent API</returns>
        IPoolingServiceConfigBuilder WithValidation(
            bool? enableObjectValidation = null,
            bool? enablePoolValidation = null,
            TimeSpan? validationInterval = null);

        /// <summary>
        /// Configures health monitoring settings.
        /// </summary>
        /// <param name="enableHealthMonitoring">Enable health monitoring</param>
        /// <param name="healthCheckInterval">Interval for health checks</param>
        /// <param name="alertThreshold">Threshold for health alerts</param>
        /// <param name="enableCircuitBreaker">Enable circuit breaker functionality</param>
        /// <returns>Builder instance for fluent API</returns>
        IPoolingServiceConfigBuilder WithHealthMonitoring(
            bool? enableHealthMonitoring = null,
            TimeSpan? healthCheckInterval = null,
            double? alertThreshold = null,
            bool? enableCircuitBreaker = null);

        /// <summary>
        /// Configures statistics collection settings.
        /// </summary>
        /// <param name="enableDetailedStats">Enable detailed statistics</param>
        /// <param name="updateInterval">Statistics update interval</param>
        /// <param name="retentionPeriod">How long to keep statistics</param>
        /// <returns>Builder instance for fluent API</returns>
        IPoolingServiceConfigBuilder WithStatistics(
            bool? enableDetailedStats = null,
            TimeSpan? updateInterval = null,
            TimeSpan? retentionPeriod = null);

        /// <summary>
        /// Disables all monitoring and validation for maximum performance.
        /// </summary>
        /// <returns>Builder instance for fluent API</returns>
        IPoolingServiceConfigBuilder WithHighPerformanceMode();

        /// <summary>
        /// Enables all monitoring and debugging features for development.
        /// </summary>
        /// <returns>Builder instance for fluent API</returns>
        IPoolingServiceConfigBuilder WithDevelopmentMode();

        /// <summary>
        /// Builds the final configuration.
        /// </summary>
        /// <returns>Configured PoolingServiceConfiguration</returns>
        PoolingServiceConfiguration Build();
    }

    /// <summary>
    /// Builder implementation for creating PoolingService configurations.
    /// Provides fluent API with validation and default value handling.
    /// Follows CLAUDE.md builder pattern guidelines.
    /// </summary>
    public sealed class PoolingServiceConfigBuilder : IPoolingServiceConfigBuilder
    {
        private readonly PoolingServiceConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the PoolingServiceConfigBuilder.
        /// </summary>
        public PoolingServiceConfigBuilder()
        {
            _config = PoolingServiceConfiguration.CreateDefault();
        }

        /// <summary>
        /// Initializes a new instance of the PoolingServiceConfigBuilder with a base configuration.
        /// </summary>
        /// <param name="baseConfig">Base configuration to start with</param>
        public PoolingServiceConfigBuilder(PoolingServiceConfiguration baseConfig)
        {
            _config = baseConfig ?? throw new ArgumentNullException(nameof(baseConfig));
        }

        /// <inheritdoc />
        public IPoolingServiceConfigBuilder WithServiceName(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));

            _config.ServiceName = new FixedString64Bytes(serviceName);
            return this;
        }

        /// <inheritdoc />
        public IPoolingServiceConfigBuilder WithPerformanceSettings(
            PerformanceBudget defaultBudget = null,
            int? maxPools = null,
            TimeSpan? asyncTimeout = null)
        {
            if (defaultBudget != null)
            {
                ValidatePerformanceBudget(defaultBudget);
                _config.DefaultPerformanceBudget = defaultBudget;
                _config.EnablePerformanceBudgets = true;
            }

            if (maxPools.HasValue)
            {
                if (maxPools.Value <= 0)
                    throw new ArgumentException("Maximum pools must be greater than zero", nameof(maxPools));
                _config.MaxRegisteredPools = maxPools.Value;
            }

            if (asyncTimeout.HasValue)
            {
                if (asyncTimeout.Value <= TimeSpan.Zero)
                    throw new ArgumentException("Async timeout must be greater than zero", nameof(asyncTimeout));
                _config.AsyncOperationTimeout = asyncTimeout.Value;
            }

            return this;
        }

        /// <inheritdoc />
        public IPoolingServiceConfigBuilder WithAutoScaling(
            TimeSpan? checkInterval = null,
            double? expansionThreshold = null,
            double? contractionThreshold = null)
        {
            _config.EnableAutoScaling = true;

            if (checkInterval.HasValue)
            {
                if (checkInterval.Value <= TimeSpan.Zero)
                    throw new ArgumentException("Check interval must be greater than zero", nameof(checkInterval));
                _config.AutoScalingCheckInterval = checkInterval.Value;
            }

            if (expansionThreshold.HasValue)
            {
                if (expansionThreshold.Value <= 0 || expansionThreshold.Value > 1)
                    throw new ArgumentException("Expansion threshold must be between 0 and 1", nameof(expansionThreshold));
                _config.ScalingExpansionThreshold = expansionThreshold.Value;
            }

            if (contractionThreshold.HasValue)
            {
                if (contractionThreshold.Value <= 0 || contractionThreshold.Value > 1)
                    throw new ArgumentException("Contraction threshold must be between 0 and 1", nameof(contractionThreshold));
                _config.ScalingContractionThreshold = contractionThreshold.Value;
            }

            // Validate that expansion threshold is higher than contraction threshold
            if (_config.ScalingExpansionThreshold <= _config.ScalingContractionThreshold)
            {
                throw new InvalidOperationException("Expansion threshold must be higher than contraction threshold");
            }

            return this;
        }

        /// <inheritdoc />
        public IPoolingServiceConfigBuilder WithErrorRecovery(
            int? maxRetries = null,
            TimeSpan? retryDelay = null,
            bool? enableEmergencyRecovery = null)
        {
            _config.EnableErrorRecovery = true;

            if (maxRetries.HasValue)
            {
                if (maxRetries.Value < 0)
                    throw new ArgumentException("Max retries cannot be negative", nameof(maxRetries));
                _config.MaxRetryAttempts = maxRetries.Value;
            }

            if (retryDelay.HasValue)
            {
                if (retryDelay.Value < TimeSpan.Zero)
                    throw new ArgumentException("Retry delay cannot be negative", nameof(retryDelay));
                _config.RetryDelay = retryDelay.Value;
            }

            if (enableEmergencyRecovery.HasValue)
            {
                _config.EnableEmergencyRecovery = enableEmergencyRecovery.Value;
            }

            return this;
        }

        /// <inheritdoc />
        public IPoolingServiceConfigBuilder WithValidation(
            bool? enableObjectValidation = null,
            bool? enablePoolValidation = null,
            TimeSpan? validationInterval = null)
        {
            if (enableObjectValidation.HasValue)
            {
                _config.EnableObjectValidation = enableObjectValidation.Value;
            }

            if (enablePoolValidation.HasValue)
            {
                _config.EnablePoolValidation = enablePoolValidation.Value;
            }

            if (validationInterval.HasValue)
            {
                if (validationInterval.Value <= TimeSpan.Zero)
                    throw new ArgumentException("Validation interval must be greater than zero", nameof(validationInterval));
                _config.ValidationInterval = validationInterval.Value;
            }

            return this;
        }

        /// <inheritdoc />
        public IPoolingServiceConfigBuilder WithHealthMonitoring(
            bool? enableHealthMonitoring = null,
            TimeSpan? healthCheckInterval = null,
            double? alertThreshold = null,
            bool? enableCircuitBreaker = null)
        {
            if (enableHealthMonitoring.HasValue)
            {
                _config.EnableHealthMonitoring = enableHealthMonitoring.Value;
            }

            if (healthCheckInterval.HasValue)
            {
                if (healthCheckInterval.Value <= TimeSpan.Zero)
                    throw new ArgumentException("Health check interval must be greater than zero", nameof(healthCheckInterval));
                _config.HealthCheckInterval = healthCheckInterval.Value;
            }

            if (alertThreshold.HasValue)
            {
                if (alertThreshold.Value < 0 || alertThreshold.Value > 1)
                    throw new ArgumentException("Alert threshold must be between 0 and 1", nameof(alertThreshold));
                _config.HealthAlertThreshold = alertThreshold.Value;
            }

            if (enableCircuitBreaker.HasValue)
            {
                _config.EnableCircuitBreaker = enableCircuitBreaker.Value;
            }

            return this;
        }

        /// <inheritdoc />
        public IPoolingServiceConfigBuilder WithStatistics(
            bool? enableDetailedStats = null,
            TimeSpan? updateInterval = null,
            TimeSpan? retentionPeriod = null)
        {
            if (enableDetailedStats.HasValue)
            {
                _config.EnableDetailedStatistics = enableDetailedStats.Value;
            }

            if (updateInterval.HasValue)
            {
                if (updateInterval.Value <= TimeSpan.Zero)
                    throw new ArgumentException("Update interval must be greater than zero", nameof(updateInterval));
                _config.StatisticsUpdateInterval = updateInterval.Value;
            }

            if (retentionPeriod.HasValue)
            {
                if (retentionPeriod.Value <= TimeSpan.Zero)
                    throw new ArgumentException("Retention period must be greater than zero", nameof(retentionPeriod));
                _config.StatisticsRetentionPeriod = retentionPeriod.Value;
            }

            return this;
        }

        /// <inheritdoc />
        public IPoolingServiceConfigBuilder WithHighPerformanceMode()
        {
            // Copy settings from high-performance factory method
            var highPerfConfig = PoolingServiceConfiguration.CreateHighPerformance();
            
            _config.EnableHealthMonitoring = highPerfConfig.EnableHealthMonitoring;
            _config.EnablePerformanceBudgets = highPerfConfig.EnablePerformanceBudgets;
            _config.EnableAutoScaling = highPerfConfig.EnableAutoScaling;
            _config.EnableErrorRecovery = highPerfConfig.EnableErrorRecovery;
            _config.DefaultPerformanceBudget = highPerfConfig.DefaultPerformanceBudget;
            _config.MaxRegisteredPools = highPerfConfig.MaxRegisteredPools;
            _config.AsyncOperationTimeout = highPerfConfig.AsyncOperationTimeout;
            _config.EnableObjectValidation = highPerfConfig.EnableObjectValidation;
            _config.EnablePoolValidation = highPerfConfig.EnablePoolValidation;
            _config.EnableCircuitBreaker = highPerfConfig.EnableCircuitBreaker;
            _config.EnableDetailedStatistics = highPerfConfig.EnableDetailedStatistics;
            _config.StatisticsUpdateInterval = highPerfConfig.StatisticsUpdateInterval;

            return this;
        }

        /// <inheritdoc />
        public IPoolingServiceConfigBuilder WithDevelopmentMode()
        {
            // Copy settings from development factory method
            var devConfig = PoolingServiceConfiguration.CreateDevelopment();
            
            _config.EnableHealthMonitoring = devConfig.EnableHealthMonitoring;
            _config.EnablePerformanceBudgets = devConfig.EnablePerformanceBudgets;
            _config.EnableAutoScaling = devConfig.EnableAutoScaling;
            _config.EnableErrorRecovery = devConfig.EnableErrorRecovery;
            _config.DefaultPerformanceBudget = devConfig.DefaultPerformanceBudget;
            _config.MaxRegisteredPools = devConfig.MaxRegisteredPools;
            _config.AsyncOperationTimeout = devConfig.AsyncOperationTimeout;
            _config.AutoScalingCheckInterval = devConfig.AutoScalingCheckInterval;
            _config.EnableObjectValidation = devConfig.EnableObjectValidation;
            _config.EnablePoolValidation = devConfig.EnablePoolValidation;
            _config.ValidationInterval = devConfig.ValidationInterval;
            _config.HealthCheckInterval = devConfig.HealthCheckInterval;
            _config.EnableCircuitBreaker = devConfig.EnableCircuitBreaker;
            _config.EnableDetailedStatistics = devConfig.EnableDetailedStatistics;
            _config.StatisticsUpdateInterval = devConfig.StatisticsUpdateInterval;
            _config.StatisticsRetentionPeriod = devConfig.StatisticsRetentionPeriod;

            return this;
        }

        /// <inheritdoc />
        public PoolingServiceConfiguration Build()
        {
            if (!_config.IsValid())
            {
                throw new InvalidOperationException("Configuration is invalid. Check all required settings.");
            }

            // Return a copy to prevent modification after build
            return new PoolingServiceConfiguration
            {
                ServiceName = _config.ServiceName,
                EnableHealthMonitoring = _config.EnableHealthMonitoring,
                EnablePerformanceBudgets = _config.EnablePerformanceBudgets,
                EnableAutoScaling = _config.EnableAutoScaling,
                EnableErrorRecovery = _config.EnableErrorRecovery,
                DefaultPerformanceBudget = _config.DefaultPerformanceBudget,
                MaxRegisteredPools = _config.MaxRegisteredPools,
                AsyncOperationTimeout = _config.AsyncOperationTimeout,
                AutoScalingCheckInterval = _config.AutoScalingCheckInterval,
                ScalingExpansionThreshold = _config.ScalingExpansionThreshold,
                ScalingContractionThreshold = _config.ScalingContractionThreshold,
                MaxRetryAttempts = _config.MaxRetryAttempts,
                RetryDelay = _config.RetryDelay,
                EnableEmergencyRecovery = _config.EnableEmergencyRecovery,
                EnableObjectValidation = _config.EnableObjectValidation,
                EnablePoolValidation = _config.EnablePoolValidation,
                ValidationInterval = _config.ValidationInterval,
                HealthCheckInterval = _config.HealthCheckInterval,
                HealthAlertThreshold = _config.HealthAlertThreshold,
                EnableCircuitBreaker = _config.EnableCircuitBreaker,
                EnableDetailedStatistics = _config.EnableDetailedStatistics,
                StatisticsUpdateInterval = _config.StatisticsUpdateInterval,
                StatisticsRetentionPeriod = _config.StatisticsRetentionPeriod
            };
        }

        #region Private Helper Methods

        private static void ValidatePerformanceBudget(PerformanceBudget budget)
        {
            if (!budget.IsValid())
                throw new ArgumentException("Performance budget configuration is invalid");
            
            if (budget.MaxOperationTime <= TimeSpan.Zero)
                throw new ArgumentException("Max operation time must be greater than zero");
            
            if (budget.MaxValidationTime <= TimeSpan.Zero)
                throw new ArgumentException("Max validation time must be greater than zero");
            
            if (budget.TargetFrameRate <= 0)
                throw new ArgumentException("Target frame rate must be greater than zero");
            
            if (budget.FrameTimePercentage <= 0 || budget.FrameTimePercentage > 1.0)
                throw new ArgumentException("Frame time percentage must be between 0 and 1");
        }

        #endregion
    }
}