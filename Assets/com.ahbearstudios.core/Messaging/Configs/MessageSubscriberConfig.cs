using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Builders;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Configs;

/// <summary>
/// Configuration for MessageSubscriber instances.
/// Contains validated settings for subscription behavior, performance, and monitoring.
/// Follows CLAUDE.md patterns with immutable configuration and performance optimization.
/// </summary>
public sealed class MessageSubscriberConfig
{
    /// <summary>
    /// Gets the maximum number of concurrent message handlers.
    /// </summary>
    public int MaxConcurrentHandlers { get; init; } = 16;

    /// <summary>
    /// Gets the default timeout for asynchronous message processing.
    /// </summary>
    public TimeSpan ProcessingTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets whether to enable detailed performance profiling.
    /// </summary>
    public bool EnableProfiling { get; init; } = true;

    /// <summary>
    /// Gets whether to publish IMessage events for subscription lifecycle.
    /// </summary>
    public bool EnableMessageBusIntegration { get; init; } = true;

    /// <summary>
    /// Gets the statistics collection interval.
    /// </summary>
    public TimeSpan StatisticsInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the default minimum priority level for subscriptions.
    /// </summary>
    public MessagePriority DefaultMinPriority { get; init; } = MessagePriority.Debug;

    /// <summary>
    /// Gets whether to enable circuit breaker pattern for failed subscriptions.
    /// </summary>
    public bool EnableCircuitBreaker { get; init; } = true;

    /// <summary>
    /// Gets the failure threshold for circuit breaker activation.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; init; } = 5;

    /// <summary>
    /// Gets the circuit breaker recovery timeout.
    /// </summary>
    public TimeSpan CircuitBreakerRecoveryTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets whether to enable automatic error retry for failed subscriptions.
    /// </summary>
    public bool EnableErrorRetry { get; init; } = true;

    /// <summary>
    /// Gets the maximum number of retry attempts for failed subscriptions.
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Gets the base delay between retry attempts.
    /// </summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets whether to use exponential backoff for retry delays.
    /// </summary>
    public bool UseExponentialBackoff { get; init; } = true;

    /// <summary>
    /// Gets the correlation ID for configuration tracking.
    /// </summary>
    public FixedString128Bytes CorrelationId { get; init; }

    /// <summary>
    /// Gets additional configuration metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Initializes a new instance of MessageSubscriberConfig.
    /// </summary>
    /// <param name="maxConcurrentHandlers">Maximum concurrent handlers</param>
    /// <param name="processingTimeout">Processing timeout</param>
    /// <param name="enableProfiling">Enable profiling</param>
    /// <param name="enableMessageBusIntegration">Enable message bus integration</param>
    /// <param name="statisticsInterval">Statistics collection interval</param>
    /// <param name="defaultMinPriority">Default minimum priority</param>
    /// <param name="enableCircuitBreaker">Enable circuit breaker</param>
    /// <param name="circuitBreakerFailureThreshold">Circuit breaker failure threshold</param>
    /// <param name="circuitBreakerRecoveryTimeout">Circuit breaker recovery timeout</param>
    /// <param name="enableErrorRetry">Enable error retry</param>
    /// <param name="maxRetryAttempts">Maximum retry attempts</param>
    /// <param name="retryDelay">Retry delay</param>
    /// <param name="useExponentialBackoff">Use exponential backoff</param>
    /// <param name="correlationId">Configuration correlation ID</param>
    /// <param name="metadata">Additional metadata</param>
    public MessageSubscriberConfig(
        int maxConcurrentHandlers = 16,
        TimeSpan processingTimeout = default,
        bool enableProfiling = true,
        bool enableMessageBusIntegration = true,
        TimeSpan statisticsInterval = default,
        MessagePriority defaultMinPriority = MessagePriority.Debug,
        bool enableCircuitBreaker = true,
        int circuitBreakerFailureThreshold = 5,
        TimeSpan circuitBreakerRecoveryTimeout = default,
        bool enableErrorRetry = true,
        int maxRetryAttempts = 3,
        TimeSpan retryDelay = default,
        bool useExponentialBackoff = true,
        FixedString128Bytes correlationId = default,
        Dictionary<string, object> metadata = null)
    {
        if (maxConcurrentHandlers <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrentHandlers), "Must be greater than zero");
        if (circuitBreakerFailureThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(circuitBreakerFailureThreshold), "Must be greater than zero");
        if (maxRetryAttempts < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetryAttempts), "Must be non-negative");

        MaxConcurrentHandlers = maxConcurrentHandlers;
        ProcessingTimeout = processingTimeout == default ? TimeSpan.FromSeconds(30) : processingTimeout;
        EnableProfiling = enableProfiling;
        EnableMessageBusIntegration = enableMessageBusIntegration;
        StatisticsInterval = statisticsInterval == default ? TimeSpan.FromSeconds(5) : statisticsInterval;
        DefaultMinPriority = defaultMinPriority;
        EnableCircuitBreaker = enableCircuitBreaker;
        CircuitBreakerFailureThreshold = circuitBreakerFailureThreshold;
        CircuitBreakerRecoveryTimeout = circuitBreakerRecoveryTimeout == default ? TimeSpan.FromSeconds(30) : circuitBreakerRecoveryTimeout;
        EnableErrorRetry = enableErrorRetry;
        MaxRetryAttempts = maxRetryAttempts;
        RetryDelay = retryDelay == default ? TimeSpan.FromMilliseconds(100) : retryDelay;
        UseExponentialBackoff = useExponentialBackoff;
        CorrelationId = correlationId.IsEmpty ? $"Config_{Guid.NewGuid():N}"[..32] : correlationId;
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Creates a default configuration optimized for performance.
    /// </summary>
    /// <returns>Performance-optimized configuration</returns>
    public static MessageSubscriberConfig Default() => new();

    /// <summary>
    /// Creates a configuration optimized for high-throughput scenarios.
    /// </summary>
    /// <returns>High-throughput optimized configuration</returns>
    public static MessageSubscriberConfig HighThroughput() => new(
        maxConcurrentHandlers: 32,
        processingTimeout: TimeSpan.FromSeconds(10),
        statisticsInterval: TimeSpan.FromSeconds(1),
        enableCircuitBreaker: true,
        circuitBreakerFailureThreshold: 10);

    /// <summary>
    /// Creates a configuration optimized for low-latency scenarios.
    /// </summary>
    /// <returns>Low-latency optimized configuration</returns>
    public static MessageSubscriberConfig LowLatency() => new(
        maxConcurrentHandlers: 8,
        processingTimeout: TimeSpan.FromSeconds(5),
        statisticsInterval: TimeSpan.FromSeconds(10),
        enableErrorRetry: false,
        maxRetryAttempts: 0);

    /// <summary>
    /// Creates a configuration for development/debugging scenarios.
    /// </summary>
    /// <returns>Development-optimized configuration</returns>
    public static MessageSubscriberConfig Development() => new(
        maxConcurrentHandlers: 4,
        processingTimeout: TimeSpan.FromMinutes(5),
        enableProfiling: true,
        statisticsInterval: TimeSpan.FromSeconds(1),
        enableCircuitBreaker: false,
        enableErrorRetry: true,
        maxRetryAttempts: 5);

    /// <summary>
    /// Validates the configuration values.
    /// </summary>
    /// <returns>True if configuration is valid</returns>
    public bool IsValid()
    {
        return MaxConcurrentHandlers > 0 &&
               ProcessingTimeout > TimeSpan.Zero &&
               StatisticsInterval > TimeSpan.Zero &&
               CircuitBreakerFailureThreshold > 0 &&
               CircuitBreakerRecoveryTimeout > TimeSpan.Zero &&
               MaxRetryAttempts >= 0 &&
               RetryDelay >= TimeSpan.Zero;
    }

    /// <summary>
    /// Creates a copy of this configuration with modified values.
    /// </summary>
    /// <param name="modifications">Action to modify configuration values</param>
    /// <returns>New configuration with modifications applied</returns>
    public MessageSubscriberConfig WithModifications(Action<MessageSubscriberConfigBuilder> modifications)
    {
        var builder = MessageSubscriberConfigBuilder.FromExisting(this);
        modifications(builder);
        return builder.Build();
    }

    /// <summary>
    /// Returns a string representation of the configuration.
    /// </summary>
    /// <returns>Configuration summary</returns>
    public override string ToString() =>
        $"MessageSubscriberConfig[{CorrelationId}]: " +
        $"MaxHandlers={MaxConcurrentHandlers}, " +
        $"Timeout={ProcessingTimeout.TotalSeconds:F1}s, " +
        $"Profiling={EnableProfiling}, " +
        $"CircuitBreaker={EnableCircuitBreaker}";
}