using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Configs;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Builders;

/// <summary>
/// Builder for MessageSubscriberConfig instances.
/// Handles configuration complexity through fluent API following CLAUDE.md Builder pattern.
/// Provides validation, default values, and complex setup logic.
/// </summary>
public sealed class MessageSubscriberConfigBuilder
{
    private int _maxConcurrentHandlers = 16;
    private TimeSpan _processingTimeout = TimeSpan.FromSeconds(30);
    private bool _enableProfiling = true;
    private bool _enableMessageBusIntegration = true;
    private TimeSpan _statisticsInterval = TimeSpan.FromSeconds(5);
    private MessagePriority _defaultMinPriority = MessagePriority.Debug;
    private bool _enableCircuitBreaker = true;
    private int _circuitBreakerFailureThreshold = 5;
    private TimeSpan _circuitBreakerRecoveryTimeout = TimeSpan.FromSeconds(30);
    private bool _enableErrorRetry = true;
    private int _maxRetryAttempts = 3;
    private TimeSpan _retryDelay = TimeSpan.FromMilliseconds(100);
    private bool _useExponentialBackoff = true;
    private FixedString128Bytes _correlationId = default;
    private readonly Dictionary<string, object> _metadata = new();

    /// <summary>
    /// Initializes a new instance of MessageSubscriberConfigBuilder.
    /// </summary>
    public MessageSubscriberConfigBuilder()
    {
        _correlationId = $"Builder_{Guid.NewGuid():N}"[..32];
    }

    /// <summary>
    /// Sets the maximum number of concurrent message handlers.
    /// </summary>
    /// <param name="maxHandlers">Maximum concurrent handlers (must be > 0)</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxHandlers is <= 0</exception>
    public MessageSubscriberConfigBuilder WithMaxConcurrentHandlers(int maxHandlers)
    {
        if (maxHandlers <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxHandlers), "Must be greater than zero");
        
        _maxConcurrentHandlers = maxHandlers;
        return this;
    }

    /// <summary>
    /// Sets the processing timeout for asynchronous operations.
    /// </summary>
    /// <param name="timeout">Processing timeout (must be > TimeSpan.Zero)</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is <= TimeSpan.Zero</exception>
    public MessageSubscriberConfigBuilder WithProcessingTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Must be greater than zero");
        
        _processingTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Enables or disables performance profiling.
    /// </summary>
    /// <param name="enable">Whether to enable profiling</param>
    /// <returns>Builder instance for fluent API</returns>
    public MessageSubscriberConfigBuilder WithProfiling(bool enable = true)
    {
        _enableProfiling = enable;
        return this;
    }

    /// <summary>
    /// Enables or disables message bus integration for lifecycle events.
    /// </summary>
    /// <param name="enable">Whether to enable message bus integration</param>
    /// <returns>Builder instance for fluent API</returns>
    public MessageSubscriberConfigBuilder WithMessageBusIntegration(bool enable = true)
    {
        _enableMessageBusIntegration = enable;
        return this;
    }

    /// <summary>
    /// Sets the statistics collection interval.
    /// </summary>
    /// <param name="interval">Statistics collection interval (must be > TimeSpan.Zero)</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is <= TimeSpan.Zero</exception>
    public MessageSubscriberConfigBuilder WithStatisticsInterval(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), "Must be greater than zero");
        
        _statisticsInterval = interval;
        return this;
    }

    /// <summary>
    /// Sets the default minimum priority level for subscriptions.
    /// </summary>
    /// <param name="priority">Default minimum priority level</param>
    /// <returns>Builder instance for fluent API</returns>
    public MessageSubscriberConfigBuilder WithDefaultMinPriority(MessagePriority priority)
    {
        _defaultMinPriority = priority;
        return this;
    }

    /// <summary>
    /// Configures circuit breaker behavior.
    /// </summary>
    /// <param name="enable">Whether to enable circuit breaker</param>
    /// <param name="failureThreshold">Failure threshold for activation (must be > 0)</param>
    /// <param name="recoveryTimeout">Recovery timeout (must be > TimeSpan.Zero)</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are invalid</exception>
    public MessageSubscriberConfigBuilder WithCircuitBreaker(
        bool enable = true, 
        int failureThreshold = 5, 
        TimeSpan recoveryTimeout = default)
    {
        if (failureThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(failureThreshold), "Must be greater than zero");
        
        var timeout = recoveryTimeout == default ? TimeSpan.FromSeconds(30) : recoveryTimeout;
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(recoveryTimeout), "Must be greater than zero");
        
        _enableCircuitBreaker = enable;
        _circuitBreakerFailureThreshold = failureThreshold;
        _circuitBreakerRecoveryTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Configures error retry behavior.
    /// </summary>
    /// <param name="enable">Whether to enable error retry</param>
    /// <param name="maxAttempts">Maximum retry attempts (must be >= 0)</param>
    /// <param name="baseDelay">Base delay between retries (must be >= TimeSpan.Zero)</param>
    /// <param name="useExponentialBackoff">Whether to use exponential backoff</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are invalid</exception>
    public MessageSubscriberConfigBuilder WithErrorRetry(
        bool enable = true,
        int maxAttempts = 3,
        TimeSpan baseDelay = default,
        bool useExponentialBackoff = true)
    {
        if (maxAttempts < 0)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Must be non-negative");
        
        var delay = baseDelay == default ? TimeSpan.FromMilliseconds(100) : baseDelay;
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(baseDelay), "Must be non-negative");
        
        _enableErrorRetry = enable;
        _maxRetryAttempts = maxAttempts;
        _retryDelay = delay;
        _useExponentialBackoff = useExponentialBackoff;
        return this;
    }

    /// <summary>
    /// Sets a custom correlation ID for configuration tracking.
    /// </summary>
    /// <param name="correlationId">Custom correlation ID</param>
    /// <returns>Builder instance for fluent API</returns>
    public MessageSubscriberConfigBuilder WithCorrelationId(string correlationId)
    {
        if (string.IsNullOrEmpty(correlationId))
            throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));
        
        _correlationId = correlationId.Length > 128 ? correlationId[..128] : correlationId;
        return this;
    }

    /// <summary>
    /// Adds metadata to the configuration.
    /// </summary>
    /// <param name="key">Metadata key</param>
    /// <param name="value">Metadata value</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentException">Thrown when key is null or empty</exception>
    public MessageSubscriberConfigBuilder WithMetadata(string key, object value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Metadata key cannot be null or empty", nameof(key));
        
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple metadata entries to the configuration.
    /// </summary>
    /// <param name="metadata">Metadata dictionary to add</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentNullException">Thrown when metadata is null</exception>
    public MessageSubscriberConfigBuilder WithMetadata(Dictionary<string, object> metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));
        
        foreach (var kvp in metadata)
        {
            if (!string.IsNullOrEmpty(kvp.Key))
                _metadata[kvp.Key] = kvp.Value;
        }
        
        return this;
    }

    /// <summary>
    /// Applies a performance optimization preset.
    /// </summary>
    /// <param name="preset">Performance preset to apply</param>
    /// <returns>Builder instance for fluent API</returns>
    public MessageSubscriberConfigBuilder WithPerformancePreset(PerformancePreset preset)
    {
        return preset switch
        {
            PerformancePreset.HighThroughput => ApplyHighThroughputPreset(),
            PerformancePreset.LowLatency => ApplyLowLatencyPreset(),
            PerformancePreset.Development => ApplyDevelopmentPreset(),
            PerformancePreset.Default => this, // Keep current settings
            _ => throw new ArgumentException($"Unknown performance preset: {preset}", nameof(preset))
        };
    }

    /// <summary>
    /// Creates a builder from an existing configuration.
    /// </summary>
    /// <param name="existingConfig">Existing configuration to copy</param>
    /// <returns>New builder with copied settings</returns>
    /// <exception cref="ArgumentNullException">Thrown when existingConfig is null</exception>
    public static MessageSubscriberConfigBuilder FromExisting(MessageSubscriberConfig existingConfig)
    {
        if (existingConfig == null)
            throw new ArgumentNullException(nameof(existingConfig));

        var builder = new MessageSubscriberConfigBuilder();
        builder._maxConcurrentHandlers = existingConfig.MaxConcurrentHandlers;
        builder._processingTimeout = existingConfig.ProcessingTimeout;
        builder._enableProfiling = existingConfig.EnableProfiling;
        builder._enableMessageBusIntegration = existingConfig.EnableMessageBusIntegration;
        builder._statisticsInterval = existingConfig.StatisticsInterval;
        builder._defaultMinPriority = existingConfig.DefaultMinPriority;
        builder._enableCircuitBreaker = existingConfig.EnableCircuitBreaker;
        builder._circuitBreakerFailureThreshold = existingConfig.CircuitBreakerFailureThreshold;
        builder._circuitBreakerRecoveryTimeout = existingConfig.CircuitBreakerRecoveryTimeout;
        builder._enableErrorRetry = existingConfig.EnableErrorRetry;
        builder._maxRetryAttempts = existingConfig.MaxRetryAttempts;
        builder._retryDelay = existingConfig.RetryDelay;
        builder._useExponentialBackoff = existingConfig.UseExponentialBackoff;
        builder._correlationId = existingConfig.CorrelationId;
        
        foreach (var kvp in existingConfig.Metadata)
        {
            builder._metadata[kvp.Key] = kvp.Value;
        }

        return builder;
    }

    /// <summary>
    /// Builds and validates the final configuration.
    /// </summary>
    /// <returns>Validated MessageSubscriberConfig instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    public MessageSubscriberConfig Build()
    {
        var config = new MessageSubscriberConfig(
            _maxConcurrentHandlers,
            _processingTimeout,
            _enableProfiling,
            _enableMessageBusIntegration,
            _statisticsInterval,
            _defaultMinPriority,
            _enableCircuitBreaker,
            _circuitBreakerFailureThreshold,
            _circuitBreakerRecoveryTimeout,
            _enableErrorRetry,
            _maxRetryAttempts,
            _retryDelay,
            _useExponentialBackoff,
            _correlationId,
            new Dictionary<string, object>(_metadata));

        if (!config.IsValid())
            throw new InvalidOperationException("Built configuration is invalid");

        return config;
    }

    /// <summary>
    /// Applies high-throughput optimization settings.
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    private MessageSubscriberConfigBuilder ApplyHighThroughputPreset()
    {
        _maxConcurrentHandlers = 32;
        _processingTimeout = TimeSpan.FromSeconds(10);
        _statisticsInterval = TimeSpan.FromSeconds(1);
        _circuitBreakerFailureThreshold = 10;
        _enableCircuitBreaker = true;
        return this;
    }

    /// <summary>
    /// Applies low-latency optimization settings.
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    private MessageSubscriberConfigBuilder ApplyLowLatencyPreset()
    {
        _maxConcurrentHandlers = 8;
        _processingTimeout = TimeSpan.FromSeconds(5);
        _statisticsInterval = TimeSpan.FromSeconds(10);
        _enableErrorRetry = false;
        _maxRetryAttempts = 0;
        return this;
    }

    /// <summary>
    /// Applies development/debugging settings.
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    private MessageSubscriberConfigBuilder ApplyDevelopmentPreset()
    {
        _maxConcurrentHandlers = 4;
        _processingTimeout = TimeSpan.FromMinutes(5);
        _enableProfiling = true;
        _statisticsInterval = TimeSpan.FromSeconds(1);
        _enableCircuitBreaker = false;
        _enableErrorRetry = true;
        _maxRetryAttempts = 5;
        return this;
    }
}

/// <summary>
/// Performance preset options for MessageSubscriber configuration.
/// </summary>
public enum PerformancePreset
{
    /// <summary>
    /// Default balanced configuration.
    /// </summary>
    Default,

    /// <summary>
    /// Optimized for high-throughput scenarios.
    /// </summary>
    HighThroughput,

    /// <summary>
    /// Optimized for low-latency scenarios.
    /// </summary>
    LowLatency,

    /// <summary>
    /// Optimized for development and debugging.
    /// </summary>
    Development
}