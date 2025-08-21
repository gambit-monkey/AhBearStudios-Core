using System;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.Messaging.Configs;

namespace AhBearStudios.Core.Messaging.Builders;

/// <summary>
/// Builder interface for creating message circuit breaker configurations.
/// Follows CLAUDE.md Builder → Config → Factory → Service pattern.
/// </summary>
public interface IMessageCircuitBreakerBuilder
{
    /// <summary>
    /// Sets the default circuit breaker configuration for all message types.
    /// </summary>
    /// <param name="config">Default circuit breaker configuration</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageCircuitBreakerBuilder WithDefaultConfig(CircuitBreakerConfig config);

    /// <summary>
    /// Configures the default circuit breaker with specific parameters.
    /// </summary>
    /// <param name="name">Circuit breaker name</param>
    /// <param name="failureThreshold">Number of failures before opening circuit</param>
    /// <param name="timeoutMinutes">Timeout in minutes before attempting to close circuit</param>
    /// <param name="samplingDurationMinutes">Sampling duration in minutes</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageCircuitBreakerBuilder WithDefaultConfig(string name, int failureThreshold, double timeoutMinutes, double samplingDurationMinutes);

    /// <summary>
    /// Adds a message type specific circuit breaker configuration.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="config">Circuit breaker configuration for this message type</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageCircuitBreakerBuilder AddMessageTypeConfig<TMessage>(CircuitBreakerConfig config);

    /// <summary>
    /// Adds a message type specific circuit breaker configuration with parameters.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="failureThreshold">Number of failures before opening circuit</param>
    /// <param name="timeoutMinutes">Timeout in minutes before attempting to close circuit</param>
    /// <param name="samplingDurationMinutes">Sampling duration in minutes</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageCircuitBreakerBuilder AddMessageTypeConfig<TMessage>(int failureThreshold, double timeoutMinutes, double samplingDurationMinutes);

    /// <summary>
    /// Enables or disables publishing of circuit breaker state changes to the message bus.
    /// </summary>
    /// <param name="enabled">True to enable state change publishing</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageCircuitBreakerBuilder WithStateChangePublishing(bool enabled = true);

    /// <summary>
    /// Enables or disables performance monitoring with profiler markers.
    /// </summary>
    /// <param name="enabled">True to enable performance monitoring</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageCircuitBreakerBuilder WithPerformanceMonitoring(bool enabled = true);

    /// <summary>
    /// Uses a predefined configuration template.
    /// </summary>
    /// <param name="template">Configuration template</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessageCircuitBreakerBuilder UseTemplate(MessageCircuitBreakerConfig template);

    /// <summary>
    /// Configures the circuit breaker for development environment.
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    IMessageCircuitBreakerBuilder ForDevelopment();

    /// <summary>
    /// Configures the circuit breaker for production environment.
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    IMessageCircuitBreakerBuilder ForProduction();

    /// <summary>
    /// Configures the circuit breaker for high-performance scenarios.
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    IMessageCircuitBreakerBuilder ForHighPerformance();

    /// <summary>
    /// Builds the final configuration.
    /// </summary>
    /// <returns>Validated message circuit breaker configuration</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    MessageCircuitBreakerConfig Build();
}