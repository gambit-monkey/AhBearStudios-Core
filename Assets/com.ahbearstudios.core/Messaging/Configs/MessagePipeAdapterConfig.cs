using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging.Configs;

/// <summary>
/// Configuration for the MessagePipe adapter service.
/// Follows CLAUDE.md guidelines for simple, game-focused configuration.
/// </summary>
public sealed class MessagePipeAdapterConfig
{
    /// <summary>
    /// Default configuration for MessagePipe adapter.
    /// </summary>
    public static readonly MessagePipeAdapterConfig Default = new()
    {
        EnableHealthChecks = true,
        EnablePerformanceMonitoring = true,
        EnableDetailedLogging = false,
        HealthCheckIntervalSeconds = 60,
        MaxHealthHistoryEntries = 100,
        PublishTimeoutMs = 5000,
        SubscriptionTimeoutMs = 30000,
        EnableCircuitBreaker = true,
        CircuitBreakerFailureThreshold = 5,
        CircuitBreakerResetTimeoutSeconds = 60
    };

    /// <summary>
    /// Gets or sets whether to enable health checks.
    /// </summary>
    public bool EnableHealthChecks { get; init; } = true;

    /// <summary>
    /// Gets or sets whether to enable performance monitoring with profiler markers.
    /// </summary>
    public bool EnablePerformanceMonitoring { get; init; } = true;

    /// <summary>
    /// Gets or sets whether to enable detailed logging for debugging.
    /// </summary>
    public bool EnableDetailedLogging { get; init; } = false;

    /// <summary>
    /// Gets or sets the health check interval in seconds.
    /// </summary>
    public int HealthCheckIntervalSeconds { get; init; } = 60;

    /// <summary>
    /// Gets or sets the maximum number of health history entries to keep.
    /// </summary>
    public int MaxHealthHistoryEntries { get; init; } = 100;

    /// <summary>
    /// Gets or sets the publish operation timeout in milliseconds.
    /// </summary>
    public int PublishTimeoutMs { get; init; } = 5000;

    /// <summary>
    /// Gets or sets the subscription operation timeout in milliseconds.
    /// </summary>
    public int SubscriptionTimeoutMs { get; init; } = 30000;

    /// <summary>
    /// Gets or sets whether to enable circuit breaker for fault tolerance.
    /// </summary>
    public bool EnableCircuitBreaker { get; init; } = true;

    /// <summary>
    /// Gets or sets the number of failures before opening the circuit.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; init; } = 5;

    /// <summary>
    /// Gets or sets the circuit breaker reset timeout in seconds.
    /// </summary>
    public int CircuitBreakerResetTimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// Gets or sets custom metadata for the adapter.
    /// </summary>
    public Dictionary<string, object> CustomMetadata { get; init; } = new();

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (HealthCheckIntervalSeconds <= 0)
            errors.Add("HealthCheckIntervalSeconds must be greater than zero");

        if (MaxHealthHistoryEntries <= 0)
            errors.Add("MaxHealthHistoryEntries must be greater than zero");

        if (MaxHealthHistoryEntries > 1000)
            errors.Add("MaxHealthHistoryEntries should not exceed 1000 for memory efficiency");

        if (PublishTimeoutMs <= 0)
            errors.Add("PublishTimeoutMs must be greater than zero");

        if (PublishTimeoutMs > 30000)
            errors.Add("PublishTimeoutMs should not exceed 30 seconds for game responsiveness");

        if (SubscriptionTimeoutMs <= 0)
            errors.Add("SubscriptionTimeoutMs must be greater than zero");

        if (CircuitBreakerFailureThreshold <= 0)
            errors.Add("CircuitBreakerFailureThreshold must be greater than zero");

        if (CircuitBreakerResetTimeoutSeconds <= 0)
            errors.Add("CircuitBreakerResetTimeoutSeconds must be greater than zero");

        return errors;
    }

    /// <summary>
    /// Creates a configuration optimized for development.
    /// </summary>
    public static MessagePipeAdapterConfig ForDevelopment()
    {
        return new MessagePipeAdapterConfig
        {
            EnableHealthChecks = true,
            EnablePerformanceMonitoring = true,
            EnableDetailedLogging = true, // Verbose logging for debugging
            HealthCheckIntervalSeconds = 30,
            MaxHealthHistoryEntries = 50,
            PublishTimeoutMs = 10000, // Longer timeout for debugging
            SubscriptionTimeoutMs = 60000,
            EnableCircuitBreaker = false, // Disable for easier debugging
            CircuitBreakerFailureThreshold = 10,
            CircuitBreakerResetTimeoutSeconds = 30
        };
    }

    /// <summary>
    /// Creates a configuration optimized for production.
    /// </summary>
    public static MessagePipeAdapterConfig ForProduction()
    {
        return new MessagePipeAdapterConfig
        {
            EnableHealthChecks = true,
            EnablePerformanceMonitoring = false, // Disable for performance
            EnableDetailedLogging = false,
            HealthCheckIntervalSeconds = 120,
            MaxHealthHistoryEntries = 100,
            PublishTimeoutMs = 3000, // Strict timeout for responsiveness
            SubscriptionTimeoutMs = 20000,
            EnableCircuitBreaker = true,
            CircuitBreakerFailureThreshold = 3,
            CircuitBreakerResetTimeoutSeconds = 120
        };
    }

    /// <summary>
    /// Creates a configuration optimized for high-performance scenarios.
    /// </summary>
    public static MessagePipeAdapterConfig ForHighPerformance()
    {
        return new MessagePipeAdapterConfig
        {
            EnableHealthChecks = false, // Disable for maximum performance
            EnablePerformanceMonitoring = false,
            EnableDetailedLogging = false,
            HealthCheckIntervalSeconds = 300,
            MaxHealthHistoryEntries = 10,
            PublishTimeoutMs = 1000, // Very strict timeout
            SubscriptionTimeoutMs = 10000,
            EnableCircuitBreaker = true,
            CircuitBreakerFailureThreshold = 2,
            CircuitBreakerResetTimeoutSeconds = 60
        };
    }
}