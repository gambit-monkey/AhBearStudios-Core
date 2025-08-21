using System;
using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.Messaging.Configs;

namespace AhBearStudios.Core.Messaging.Builders;

/// <summary>
/// Builder for creating message circuit breaker configurations.
/// Handles complexity and validation following CLAUDE.md guidelines.
/// </summary>
public sealed class MessageCircuitBreakerBuilder : IMessageCircuitBreakerBuilder
{
    private CircuitBreakerConfig _defaultConfig;
    private bool _publishStateChanges = true;
    private bool _enablePerformanceMonitoring = true;
    private readonly Dictionary<Type, CircuitBreakerConfig> _messageTypeConfigs = new();

    public MessageCircuitBreakerBuilder()
    {
        // Initialize with default configuration
        _defaultConfig = new CircuitBreakerConfig
        {
            Name = "Default_MessageBus_CircuitBreaker",
            FailureThreshold = 5,
            Timeout = TimeSpan.FromMinutes(1),
            SamplingDuration = TimeSpan.FromMinutes(5)
        };
    }

    /// <inheritdoc />
    public IMessageCircuitBreakerBuilder WithDefaultConfig(CircuitBreakerConfig config)
    {
        _defaultConfig = config ?? throw new ArgumentNullException(nameof(config));
        return this;
    }

    /// <inheritdoc />
    public IMessageCircuitBreakerBuilder WithDefaultConfig(string name, int failureThreshold, double timeoutMinutes, double samplingDurationMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Circuit breaker name cannot be null or empty", nameof(name));
        
        if (failureThreshold <= 0)
            throw new ArgumentException("Failure threshold must be greater than zero", nameof(failureThreshold));
        
        if (timeoutMinutes <= 0)
            throw new ArgumentException("Timeout must be greater than zero", nameof(timeoutMinutes));
        
        if (samplingDurationMinutes <= 0)
            throw new ArgumentException("Sampling duration must be greater than zero", nameof(samplingDurationMinutes));

        _defaultConfig = new CircuitBreakerConfig
        {
            Name = name,
            FailureThreshold = failureThreshold,
            Timeout = TimeSpan.FromMinutes(timeoutMinutes),
            SamplingDuration = TimeSpan.FromMinutes(samplingDurationMinutes)
        };

        return this;
    }

    /// <inheritdoc />
    public IMessageCircuitBreakerBuilder AddMessageTypeConfig<TMessage>(CircuitBreakerConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        var messageType = typeof(TMessage);
        _messageTypeConfigs[messageType] = config;
        return this;
    }

    /// <inheritdoc />
    public IMessageCircuitBreakerBuilder AddMessageTypeConfig<TMessage>(int failureThreshold, double timeoutMinutes, double samplingDurationMinutes)
    {
        if (failureThreshold <= 0)
            throw new ArgumentException("Failure threshold must be greater than zero", nameof(failureThreshold));
        
        if (timeoutMinutes <= 0)
            throw new ArgumentException("Timeout must be greater than zero", nameof(timeoutMinutes));
        
        if (samplingDurationMinutes <= 0)
            throw new ArgumentException("Sampling duration must be greater than zero", nameof(samplingDurationMinutes));

        var messageType = typeof(TMessage);
        var config = new CircuitBreakerConfig
        {
            Name = $"MessageBus_{messageType.Name}_CircuitBreaker",
            FailureThreshold = failureThreshold,
            Timeout = TimeSpan.FromMinutes(timeoutMinutes),
            SamplingDuration = TimeSpan.FromMinutes(samplingDurationMinutes)
        };

        _messageTypeConfigs[messageType] = config;
        return this;
    }

    /// <inheritdoc />
    public IMessageCircuitBreakerBuilder WithStateChangePublishing(bool enabled = true)
    {
        _publishStateChanges = enabled;
        return this;
    }

    /// <inheritdoc />
    public IMessageCircuitBreakerBuilder WithPerformanceMonitoring(bool enabled = true)
    {
        _enablePerformanceMonitoring = enabled;
        return this;
    }

    /// <inheritdoc />
    public IMessageCircuitBreakerBuilder UseTemplate(MessageCircuitBreakerConfig template)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));

        _defaultConfig = template.DefaultCircuitBreakerConfig;
        _publishStateChanges = template.PublishStateChanges;
        _enablePerformanceMonitoring = template.EnablePerformanceMonitoring;

        if (template.MessageTypeConfigs != null)
        {
            foreach (var kvp in template.MessageTypeConfigs)
            {
                _messageTypeConfigs[kvp.Key] = kvp.Value;
            }
        }

        return this;
    }

    /// <inheritdoc />
    public IMessageCircuitBreakerBuilder ForDevelopment()
    {
        _defaultConfig = new CircuitBreakerConfig
        {
            Name = "Development_MessageBus_CircuitBreaker",
            FailureThreshold = 10, // Higher threshold for debugging
            Timeout = TimeSpan.FromMinutes(2), // Longer timeout for debugging
            SamplingDuration = TimeSpan.FromMinutes(10)
        };

        _publishStateChanges = true;
        _enablePerformanceMonitoring = true;
        return this;
    }

    /// <inheritdoc />
    public IMessageCircuitBreakerBuilder ForProduction()
    {
        _defaultConfig = new CircuitBreakerConfig
        {
            Name = "Production_MessageBus_CircuitBreaker",
            FailureThreshold = 3, // Stricter threshold
            Timeout = TimeSpan.FromSeconds(30), // Shorter timeout for responsiveness
            SamplingDuration = TimeSpan.FromMinutes(3)
        };

        _publishStateChanges = true;
        _enablePerformanceMonitoring = false; // Disable for performance
        return this;
    }

    /// <inheritdoc />
    public IMessageCircuitBreakerBuilder ForHighPerformance()
    {
        _defaultConfig = new CircuitBreakerConfig
        {
            Name = "HighPerformance_MessageBus_CircuitBreaker",
            FailureThreshold = 2, // Very strict
            Timeout = TimeSpan.FromSeconds(15),
            SamplingDuration = TimeSpan.FromMinutes(1)
        };

        _publishStateChanges = false; // Disable for maximum performance
        _enablePerformanceMonitoring = false;
        return this;
    }

    /// <inheritdoc />
    public MessageCircuitBreakerConfig Build()
    {
        var config = new MessageCircuitBreakerConfig
        {
            DefaultCircuitBreakerConfig = _defaultConfig,
            PublishStateChanges = _publishStateChanges,
            EnablePerformanceMonitoring = _enablePerformanceMonitoring,
            MessageTypeConfigs = new Dictionary<Type, CircuitBreakerConfig>(_messageTypeConfigs)
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