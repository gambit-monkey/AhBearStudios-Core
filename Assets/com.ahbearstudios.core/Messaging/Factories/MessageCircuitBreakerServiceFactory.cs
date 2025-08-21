using System;
using AhBearStudios.Core.HealthChecking.Factories;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Services;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Factories;

/// <summary>
/// Factory for creating message circuit breaker services.
/// Simple creation following CLAUDE.md guidelines - no lifecycle management.
/// </summary>
public sealed class MessageCircuitBreakerServiceFactory : IMessageCircuitBreakerServiceFactory
{
    /// <inheritdoc />
    public async UniTask<IMessageCircuitBreakerService> CreateServiceAsync(
        MessageCircuitBreakerConfig config,
        ILoggingService logger,
        ICircuitBreakerFactory circuitBreakerFactory,
        IMessageBusService messageBus = null)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        
        if (logger == null)
            throw new ArgumentNullException(nameof(logger));
        
        if (circuitBreakerFactory == null)
            throw new ArgumentNullException(nameof(circuitBreakerFactory));

        // Validate configuration before creating service
        var validationErrors = config.Validate();
        if (validationErrors.Count > 0)
        {
            var errorMessage = $"Invalid MessageCircuitBreakerConfig: {string.Join(", ", validationErrors)}";
            throw new ArgumentException(errorMessage, nameof(config));
        }

        await UniTask.SwitchToMainThread();

        // Simple creation - factory doesn't track the created instance
        return new MessageCircuitBreakerService(config, logger, circuitBreakerFactory, messageBus);
    }

    /// <inheritdoc />
    public async UniTask<IMessageCircuitBreakerService> CreateDefaultServiceAsync(
        ILoggingService logger,
        ICircuitBreakerFactory circuitBreakerFactory,
        IMessageBusService messageBus = null)
    {
        return await CreateServiceAsync(
            MessageCircuitBreakerConfig.Default,
            logger,
            circuitBreakerFactory,
            messageBus);
    }
}