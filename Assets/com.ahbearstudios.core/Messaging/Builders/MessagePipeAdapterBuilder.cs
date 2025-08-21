using System;
using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Configs;

namespace AhBearStudios.Core.Messaging.Builders;

/// <summary>
/// Builder for creating MessagePipe adapter configurations.
/// Handles complexity and validation following CLAUDE.md guidelines.
/// </summary>
public sealed class MessagePipeAdapterBuilder : IMessagePipeAdapterBuilder
{
    private bool _enableHealthChecks = true;
    private bool _enablePerformanceMonitoring = true;
    private bool _enableDetailedLogging = false;
    private int _healthCheckIntervalSeconds = 60;
    private int _maxHealthHistoryEntries = 100;
    private int _publishTimeoutMs = 5000;
    private int _subscriptionTimeoutMs = 30000;
    private bool _enableCircuitBreaker = true;
    private int _circuitBreakerFailureThreshold = 5;
    private int _circuitBreakerResetTimeoutSeconds = 60;
    private readonly Dictionary<string, object> _customMetadata = new();

    /// <inheritdoc />
    public IMessagePipeAdapterBuilder WithHealthChecks(bool enabled = true)
    {
        _enableHealthChecks = enabled;
        return this;
    }

    /// <inheritdoc />
    public IMessagePipeAdapterBuilder WithPerformanceMonitoring(bool enabled = true)
    {
        _enablePerformanceMonitoring = enabled;
        return this;
    }

    /// <inheritdoc />
    public IMessagePipeAdapterBuilder WithDetailedLogging(bool enabled = true)
    {
        _enableDetailedLogging = enabled;
        return this;
    }

    /// <inheritdoc />
    public IMessagePipeAdapterBuilder WithHealthCheckInterval(int seconds)
    {
        if (seconds <= 0)
            throw new ArgumentException("Health check interval must be greater than zero", nameof(seconds));
        
        _healthCheckIntervalSeconds = seconds;
        return this;
    }

    /// <inheritdoc />
    public IMessagePipeAdapterBuilder WithMaxHealthHistory(int maxEntries)
    {
        if (maxEntries <= 0)
            throw new ArgumentException("Max health history entries must be greater than zero", nameof(maxEntries));
        
        if (maxEntries > 1000)
            throw new ArgumentException("Max health history entries should not exceed 1000 for memory efficiency", nameof(maxEntries));
        
        _maxHealthHistoryEntries = maxEntries;
        return this;
    }

    /// <inheritdoc />
    public IMessagePipeAdapterBuilder WithPublishTimeout(int milliseconds)
    {
        if (milliseconds <= 0)
            throw new ArgumentException("Publish timeout must be greater than zero", nameof(milliseconds));
        
        if (milliseconds > 30000)
            throw new ArgumentException("Publish timeout should not exceed 30 seconds for game responsiveness", nameof(milliseconds));
        
        _publishTimeoutMs = milliseconds;
        return this;
    }

    /// <inheritdoc />
    public IMessagePipeAdapterBuilder WithSubscriptionTimeout(int milliseconds)
    {
        if (milliseconds <= 0)
            throw new ArgumentException("Subscription timeout must be greater than zero", nameof(milliseconds));
        
        _subscriptionTimeoutMs = milliseconds;
        return this;
    }

    /// <inheritdoc />
    public IMessagePipeAdapterBuilder WithCircuitBreaker(bool enabled = true, int failureThreshold = 5, int resetTimeoutSeconds = 60)
    {
        if (failureThreshold <= 0)
            throw new ArgumentException("Circuit breaker failure threshold must be greater than zero", nameof(failureThreshold));
        
        if (resetTimeoutSeconds <= 0)
            throw new ArgumentException("Circuit breaker reset timeout must be greater than zero", nameof(resetTimeoutSeconds));
        
        _enableCircuitBreaker = enabled;
        _circuitBreakerFailureThreshold = failureThreshold;
        _circuitBreakerResetTimeoutSeconds = resetTimeoutSeconds;
        return this;
    }

    /// <inheritdoc />
    public IMessagePipeAdapterBuilder AddCustomMetadata(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be null or empty", nameof(key));
        
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        
        _customMetadata[key] = value;
        return this;
    }

    /// <inheritdoc />
    public IMessagePipeAdapterBuilder UseTemplate(MessagePipeAdapterConfig template)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));
        
        _enableHealthChecks = template.EnableHealthChecks;
        _enablePerformanceMonitoring = template.EnablePerformanceMonitoring;
        _enableDetailedLogging = template.EnableDetailedLogging;
        _healthCheckIntervalSeconds = template.HealthCheckIntervalSeconds;
        _maxHealthHistoryEntries = template.MaxHealthHistoryEntries;
        _publishTimeoutMs = template.PublishTimeoutMs;
        _subscriptionTimeoutMs = template.SubscriptionTimeoutMs;
        _enableCircuitBreaker = template.EnableCircuitBreaker;
        _circuitBreakerFailureThreshold = template.CircuitBreakerFailureThreshold;
        _circuitBreakerResetTimeoutSeconds = template.CircuitBreakerResetTimeoutSeconds;
        
        if (template.CustomMetadata != null)
        {
            foreach (var kvp in template.CustomMetadata)
            {
                _customMetadata[kvp.Key] = kvp.Value;
            }
        }
        
        return this;
    }

    /// <inheritdoc />
    public IMessagePipeAdapterBuilder ForDevelopment()
    {
        return UseTemplate(MessagePipeAdapterConfig.ForDevelopment());
    }

    /// <inheritdoc />
    public IMessagePipeAdapterBuilder ForProduction()
    {
        return UseTemplate(MessagePipeAdapterConfig.ForProduction());
    }

    /// <inheritdoc />
    public IMessagePipeAdapterBuilder ForHighPerformance()
    {
        return UseTemplate(MessagePipeAdapterConfig.ForHighPerformance());
    }

    /// <inheritdoc />
    public MessagePipeAdapterConfig Build()
    {
        var config = new MessagePipeAdapterConfig
        {
            EnableHealthChecks = _enableHealthChecks,
            EnablePerformanceMonitoring = _enablePerformanceMonitoring,
            EnableDetailedLogging = _enableDetailedLogging,
            HealthCheckIntervalSeconds = _healthCheckIntervalSeconds,
            MaxHealthHistoryEntries = _maxHealthHistoryEntries,
            PublishTimeoutMs = _publishTimeoutMs,
            SubscriptionTimeoutMs = _subscriptionTimeoutMs,
            EnableCircuitBreaker = _enableCircuitBreaker,
            CircuitBreakerFailureThreshold = _circuitBreakerFailureThreshold,
            CircuitBreakerResetTimeoutSeconds = _circuitBreakerResetTimeoutSeconds,
            CustomMetadata = new Dictionary<string, object>(_customMetadata)
        };

        var validationErrors = config.Validate();
        if (validationErrors.Count > 0)
        {
            var errorMessage = $"MessagePipeAdapterConfig validation failed: {string.Join(", ", validationErrors)}";
            throw new InvalidOperationException(errorMessage);
        }

        return config;
    }
}