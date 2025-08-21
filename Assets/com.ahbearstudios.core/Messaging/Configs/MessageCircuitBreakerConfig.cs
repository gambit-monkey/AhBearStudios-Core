using System;
using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Configs;

namespace AhBearStudios.Core.Messaging.Configs;

/// <summary>
/// Configuration for message circuit breaker service
/// </summary>
public sealed class MessageCircuitBreakerConfig
{
    /// <summary>
    /// Default configuration for message circuit breakers
    /// </summary>
    public static readonly MessageCircuitBreakerConfig Default = new()
    {
        DefaultCircuitBreakerConfig = new CircuitBreakerConfig
        {
            Name = "Default_MessageBus_CircuitBreaker",
            FailureThreshold = 5,
            Timeout = TimeSpan.FromMinutes(1),
            SamplingDuration = TimeSpan.FromMinutes(5)
        },
        PublishStateChanges = true,
        EnablePerformanceMonitoring = true,
        MessageTypeConfigs = new Dictionary<Type, CircuitBreakerConfig>()
    };

    /// <summary>
    /// Default circuit breaker configuration to use for all message types
    /// </summary>
    public CircuitBreakerConfig DefaultCircuitBreakerConfig { get; init; }

    /// <summary>
    /// Message type specific circuit breaker configurations
    /// </summary>
    public Dictionary<Type, CircuitBreakerConfig> MessageTypeConfigs { get; init; } = new();

    /// <summary>
    /// Whether to publish circuit breaker state change messages to the message bus
    /// </summary>
    public bool PublishStateChanges { get; init; } = true;

    /// <summary>
    /// Whether to enable performance monitoring with profiler markers
    /// </summary>
    public bool EnablePerformanceMonitoring { get; init; } = true;

    /// <summary>
    /// Validates the configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (DefaultCircuitBreakerConfig == null)
        {
            errors.Add("DefaultCircuitBreakerConfig cannot be null");
        }
        else
        {
            errors.AddRange(DefaultCircuitBreakerConfig.Validate());
        }

        if (MessageTypeConfigs == null)
        {
            errors.Add("MessageTypeConfigs cannot be null");
        }
        else
        {
            foreach (var kvp in MessageTypeConfigs)
            {
                if (kvp.Key == null)
                {
                    errors.Add("Message type in MessageTypeConfigs cannot be null");
                }

                if (kvp.Value == null)
                {
                    errors.Add($"Circuit breaker config for message type {kvp.Key?.Name} cannot be null");
                }
                else
                {
                    var configErrors = kvp.Value.Validate();
                    foreach (var error in configErrors)
                    {
                        errors.Add($"Message type {kvp.Key?.Name}: {error}");
                    }
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Gets the circuit breaker configuration for a specific message type
    /// </summary>
    /// <param name="messageType">The message type</param>
    /// <returns>Circuit breaker configuration for the message type</returns>
    public CircuitBreakerConfig GetConfigForMessageType(Type messageType)
    {
        return MessageTypeConfigs.TryGetValue(messageType, out var config) 
            ? config 
            : DefaultCircuitBreakerConfig;
    }

    /// <summary>
    /// Checks if the configuration is valid
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        return Validate().Count == 0;
    }
}