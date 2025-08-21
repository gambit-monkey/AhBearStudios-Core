using System;
using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Configs;

namespace AhBearStudios.Core.Messaging.Builders;

/// <summary>
/// Builder interface for creating MessagePipe adapter configurations.
/// Follows CLAUDE.md Builder → Config → Factory → Service pattern.
/// </summary>
public interface IMessagePipeAdapterBuilder
{
    /// <summary>
    /// Enables or disables health checks for the adapter.
    /// </summary>
    /// <param name="enabled">True to enable health checks</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessagePipeAdapterBuilder WithHealthChecks(bool enabled = true);

    /// <summary>
    /// Enables or disables performance monitoring with profiler markers.
    /// </summary>
    /// <param name="enabled">True to enable performance monitoring</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessagePipeAdapterBuilder WithPerformanceMonitoring(bool enabled = true);

    /// <summary>
    /// Enables or disables detailed logging for debugging.
    /// </summary>
    /// <param name="enabled">True to enable detailed logging</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessagePipeAdapterBuilder WithDetailedLogging(bool enabled = true);

    /// <summary>
    /// Sets the health check interval.
    /// </summary>
    /// <param name="seconds">Health check interval in seconds</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessagePipeAdapterBuilder WithHealthCheckInterval(int seconds);

    /// <summary>
    /// Sets the maximum number of health history entries to keep.
    /// </summary>
    /// <param name="maxEntries">Maximum health history entries</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessagePipeAdapterBuilder WithMaxHealthHistory(int maxEntries);

    /// <summary>
    /// Sets the publish operation timeout.
    /// </summary>
    /// <param name="milliseconds">Timeout in milliseconds</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessagePipeAdapterBuilder WithPublishTimeout(int milliseconds);

    /// <summary>
    /// Sets the subscription operation timeout.
    /// </summary>
    /// <param name="milliseconds">Timeout in milliseconds</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessagePipeAdapterBuilder WithSubscriptionTimeout(int milliseconds);

    /// <summary>
    /// Configures circuit breaker settings.
    /// </summary>
    /// <param name="enabled">True to enable circuit breaker</param>
    /// <param name="failureThreshold">Number of failures before opening circuit</param>
    /// <param name="resetTimeoutSeconds">Reset timeout in seconds</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessagePipeAdapterBuilder WithCircuitBreaker(bool enabled = true, int failureThreshold = 5, int resetTimeoutSeconds = 60);

    /// <summary>
    /// Adds custom metadata to the adapter configuration.
    /// </summary>
    /// <param name="key">Metadata key</param>
    /// <param name="value">Metadata value</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessagePipeAdapterBuilder AddCustomMetadata(string key, object value);

    /// <summary>
    /// Uses a predefined configuration template.
    /// </summary>
    /// <param name="template">Configuration template</param>
    /// <returns>Builder instance for fluent API</returns>
    IMessagePipeAdapterBuilder UseTemplate(MessagePipeAdapterConfig template);

    /// <summary>
    /// Configures the adapter for development environment.
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    IMessagePipeAdapterBuilder ForDevelopment();

    /// <summary>
    /// Configures the adapter for production environment.
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    IMessagePipeAdapterBuilder ForProduction();

    /// <summary>
    /// Configures the adapter for high-performance scenarios.
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    IMessagePipeAdapterBuilder ForHighPerformance();

    /// <summary>
    /// Builds the final configuration.
    /// </summary>
    /// <returns>Validated MessagePipe adapter configuration</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    MessagePipeAdapterConfig Build();
}