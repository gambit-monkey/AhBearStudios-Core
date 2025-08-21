using System;
using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.Messaging.Configs;

namespace AhBearStudios.Core.Messaging.Builders;

/// <summary>
/// Builder for creating message circuit breaker configurations
/// </summary>
public sealed class MessageCircuitBreakerConfigBuilder : IMessageCircuitBreakerConfigBuilder
{
    private CircuitBreakerConfig _defaultConfig;
    private readonly Dictionary<Type, CircuitBreakerConfig> _messageTypeConfigs = new();
    private bool _publishStateChanges = true;
    private bool _enablePerformanceMonitoring = true;

    /// <summary>
    /// Sets the default circuit breaker configuration
    /// </summary>
    /// <param name="config">Default circuit breaker configuration</param>
    /// <returns>Builder instance for fluent API</returns>
    public IMessageCircuitBreakerConfigBuilder WithDefaultConfig(CircuitBreakerConfig config)
    {
        _defaultConfig = config ?? throw new ArgumentNullException(nameof(config));
        return this;
    }

    /// <summary>
    /// Sets the default circuit breaker configuration with basic parameters
    /// </summary>
    /// <param name="failureThreshold">Number of failures before opening circuit</param>
    /// <param name="timeout">Timeout before moving to half-open state</param>
    /// <param name="monitoringPeriod">Period for monitoring circuit health</param>
    /// <returns>Builder instance for fluent API</returns>
    public IMessageCircuitBreakerConfigBuilder WithDefaultConfig(
        int failureThreshold = 5, 
        TimeSpan? timeout = null, 
        TimeSpan? monitoringPeriod = null)
    {
        _defaultConfig = new CircuitBreakerConfig
        {
            Name = "Default_MessageBus_CircuitBreaker",
            FailureThreshold = failureThreshold,
            Timeout = timeout ?? TimeSpan.FromMinutes(1),
            SamplingDuration = monitoringPeriod ?? TimeSpan.FromMinutes(5)
        };
        return this;
    }

    /// <summary>
    /// Adds a message type specific circuit breaker configuration
    /// </summary>
    /// <typeparam name="TMessage">Message type</typeparam>
    /// <param name="config">Circuit breaker configuration for the message type</param>
    /// <returns>Builder instance for fluent API</returns>
    public IMessageCircuitBreakerConfigBuilder WithMessageTypeConfig<TMessage>(CircuitBreakerConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        _messageTypeConfigs[typeof(TMessage)] = config;
        return this;
    }

    /// <summary>
    /// Adds a message type specific circuit breaker configuration with basic parameters
    /// </summary>
    /// <typeparam name="TMessage">Message type</typeparam>
    /// <param name="failureThreshold">Number of failures before opening circuit</param>
    /// <param name="timeout">Timeout before moving to half-open state</param>
    /// <param name="monitoringPeriod">Period for monitoring circuit health</param>
    /// <returns>Builder instance for fluent API</returns>
    public IMessageCircuitBreakerConfigBuilder WithMessageTypeConfig<TMessage>(
        int failureThreshold = 5, 
        TimeSpan? timeout = null, 
        TimeSpan? monitoringPeriod = null)
    {
        var messageTypeName = typeof(TMessage).Name;
        var config = new CircuitBreakerConfig
        {
            Name = $"MessageBus_{messageTypeName}",
            FailureThreshold = failureThreshold,
            Timeout = timeout ?? TimeSpan.FromMinutes(1),
            SamplingDuration = monitoringPeriod ?? TimeSpan.FromMinutes(5)
        };

        _messageTypeConfigs[typeof(TMessage)] = config;
        return this;
    }

    /// <summary>
    /// Enables or disables publishing of circuit breaker state change messages
    /// </summary>
    /// <param name="enabled">True to enable state change messages</param>
    /// <returns>Builder instance for fluent API</returns>
    public IMessageCircuitBreakerConfigBuilder WithStateChangeMessages(bool enabled = true)
    {
        _publishStateChanges = enabled;
        return this;
    }

    /// <summary>
    /// Enables or disables performance monitoring with profiler markers
    /// </summary>
    /// <param name="enabled">True to enable performance monitoring</param>
    /// <returns>Builder instance for fluent API</returns>
    public IMessageCircuitBreakerConfigBuilder WithPerformanceMonitoring(bool enabled = true)
    {
        _enablePerformanceMonitoring = enabled;
        return this;
    }

    /// <summary>
    /// Builds the final configuration
    /// </summary>
    /// <returns>Validated message circuit breaker configuration</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    public MessageCircuitBreakerConfig Build()
    {
        // Use default configuration if none specified
        _defaultConfig ??= MessageCircuitBreakerConfig.Default.DefaultCircuitBreakerConfig;

        var config = new MessageCircuitBreakerConfig
        {
            DefaultCircuitBreakerConfig = _defaultConfig,
            MessageTypeConfigs = new Dictionary<Type, CircuitBreakerConfig>(_messageTypeConfigs),
            PublishStateChanges = _publishStateChanges,
            EnablePerformanceMonitoring = _enablePerformanceMonitoring
        };

        var validationErrors = config.Validate();
        if (validationErrors.Count > 0)
        {
            var errorMessage = $"MessageCircuitBreakerConfig validation failed: {string.Join(", ", validationErrors)}";
            throw new InvalidOperationException(errorMessage);
        }

        return config;
    }
}