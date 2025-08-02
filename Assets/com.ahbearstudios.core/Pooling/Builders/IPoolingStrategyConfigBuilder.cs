using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Models;
using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Interface for building pooling strategy configurations.
    /// Provides fluent interface for constructing PoolingStrategyConfig instances.
    /// </summary>
    public interface IPoolingStrategyConfigBuilder
    {
        /// <summary>
        /// Sets the name of the strategy configuration.
        /// </summary>
        /// <param name="name">Name of the configuration</param>
        /// <returns>Builder for method chaining</returns>
        IPoolingStrategyConfigBuilder WithName(string name);

        /// <summary>
        /// Sets the performance budget for the strategy.
        /// </summary>
        /// <param name="performanceBudget">Performance budget</param>
        /// <returns>Builder for method chaining</returns>
        IPoolingStrategyConfigBuilder WithPerformanceBudget(PerformanceBudget performanceBudget);

        /// <summary>
        /// Configures circuit breaker settings.
        /// </summary>
        /// <param name="enabled">Whether circuit breaker is enabled</param>
        /// <param name="failureThreshold">Number of failures before opening</param>
        /// <param name="recoveryTime">Time before attempting recovery</param>
        /// <returns>Builder for method chaining</returns>
        IPoolingStrategyConfigBuilder WithCircuitBreaker(bool enabled, int failureThreshold, TimeSpan recoveryTime);

        /// <summary>
        /// Configures health monitoring settings.
        /// </summary>
        /// <param name="enabled">Whether health monitoring is enabled</param>
        /// <param name="checkInterval">Interval between health checks</param>
        /// <returns>Builder for method chaining</returns>
        IPoolingStrategyConfigBuilder WithHealthMonitoring(bool enabled, TimeSpan checkInterval);

        /// <summary>
        /// Configures metrics collection settings.
        /// </summary>
        /// <param name="enabled">Whether detailed metrics are enabled</param>
        /// <param name="maxSamples">Maximum number of metric samples</param>
        /// <returns>Builder for method chaining</returns>
        IPoolingStrategyConfigBuilder WithMetrics(bool enabled, int maxSamples);

        /// <summary>
        /// Enables or disables network optimizations.
        /// </summary>
        /// <param name="enabled">Whether network optimizations are enabled</param>
        /// <returns>Builder for method chaining</returns>
        IPoolingStrategyConfigBuilder WithNetworkOptimizations(bool enabled);

        /// <summary>
        /// Enables or disables Unity-specific optimizations.
        /// </summary>
        /// <param name="enabled">Whether Unity optimizations are enabled</param>
        /// <returns>Builder for method chaining</returns>
        IPoolingStrategyConfigBuilder WithUnityOptimizations(bool enabled);

        /// <summary>
        /// Enables or disables debug logging.
        /// </summary>
        /// <param name="enabled">Whether debug logging is enabled</param>
        /// <returns>Builder for method chaining</returns>
        IPoolingStrategyConfigBuilder WithDebugLogging(bool enabled);

        /// <summary>
        /// Adds custom parameters to the configuration.
        /// </summary>
        /// <param name="key">Parameter key</param>
        /// <param name="value">Parameter value</param>
        /// <returns>Builder for method chaining</returns>
        IPoolingStrategyConfigBuilder WithCustomParameter(string key, object value);

        /// <summary>
        /// Adds tags to the configuration.
        /// </summary>
        /// <param name="tags">Tags to add</param>
        /// <returns>Builder for method chaining</returns>
        IPoolingStrategyConfigBuilder WithTags(params string[] tags);

        /// <summary>
        /// Creates a default configuration.
        /// </summary>
        /// <returns>Builder with default settings</returns>
        IPoolingStrategyConfigBuilder Default();

        /// <summary>
        /// Creates a high-performance configuration.
        /// </summary>
        /// <returns>Builder with high-performance settings</returns>
        IPoolingStrategyConfigBuilder HighPerformance();

        /// <summary>
        /// Creates a memory-optimized configuration.
        /// </summary>
        /// <returns>Builder with memory-optimized settings</returns>
        IPoolingStrategyConfigBuilder MemoryOptimized();

        /// <summary>
        /// Creates a development configuration.
        /// </summary>
        /// <returns>Builder with development settings</returns>
        IPoolingStrategyConfigBuilder Development();

        /// <summary>
        /// Creates a network-optimized configuration.
        /// </summary>
        /// <returns>Builder with network-optimized settings</returns>
        IPoolingStrategyConfigBuilder NetworkOptimized();

        /// <summary>
        /// Builds the final PoolingStrategyConfig instance.
        /// </summary>
        /// <returns>Configured PoolingStrategyConfig</returns>
        PoolingStrategyConfig Build();
    }
}