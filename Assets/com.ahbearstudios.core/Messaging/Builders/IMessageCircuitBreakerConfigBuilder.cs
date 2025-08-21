using System;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.Messaging.Configs;

namespace AhBearStudios.Core.Messaging.Builders;

/// <summary>
/// Builder interface for creating message circuit breaker configurations
/// </summary>
public interface IMessageCircuitBreakerConfigBuilder
{
    /// <summary>
    /// Sets the default circuit breaker configuration
    /// </summary>
    /// <param name="config">Default circuit breaker configuration</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageCircuitBreakerConfigBuilder WithDefaultConfig(CircuitBreakerConfig config);

    /// <summary>
    /// Sets the default circuit breaker configuration with basic parameters
    /// </summary>
    /// <param name="failureThreshold">Number of failures before opening circuit</param>
    /// <param name="timeout">Timeout before moving to half-open state</param>
    /// <param name="monitoringPeriod">Period for monitoring circuit health</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageCircuitBreakerConfigBuilder WithDefaultConfig(
        int failureThreshold = 5, 
        TimeSpan? timeout = null, 
        TimeSpan? monitoringPeriod = null);

    /// <summary>
    /// Adds a message type specific circuit breaker configuration
    /// </summary>
    /// <typeparam name="TMessage">Message type</typeparam>
    /// <param name="config">Circuit breaker configuration for the message type</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageCircuitBreakerConfigBuilder WithMessageTypeConfig<TMessage>(CircuitBreakerConfig config);

    /// <summary>
    /// Adds a message type specific circuit breaker configuration with basic parameters
    /// </summary>
    /// <typeparam name="TMessage">Message type</typeparam>
    /// <param name="failureThreshold">Number of failures before opening circuit</param>
    /// <param name="timeout">Timeout before moving to half-open state</param>
    /// <param name="monitoringPeriod">Period for monitoring circuit health</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageCircuitBreakerConfigBuilder WithMessageTypeConfig<TMessage>(
        int failureThreshold = 5, 
        TimeSpan? timeout = null, 
        TimeSpan? monitoringPeriod = null);

    /// <summary>
    /// Enables or disables publishing of circuit breaker state change messages
    /// </summary>
    /// <param name="enabled">True to enable state change messages</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageCircuitBreakerConfigBuilder WithStateChangeMessages(bool enabled = true);

    /// <summary>
    /// Enables or disables performance monitoring with profiler markers
    /// </summary>
    /// <param name="enabled">True to enable performance monitoring</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageCircuitBreakerConfigBuilder WithPerformanceMonitoring(bool enabled = true);

    /// <summary>
    /// Builds the final configuration
    /// </summary>
    /// <returns>Validated message circuit breaker configuration</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    MessageCircuitBreakerConfig Build();
}