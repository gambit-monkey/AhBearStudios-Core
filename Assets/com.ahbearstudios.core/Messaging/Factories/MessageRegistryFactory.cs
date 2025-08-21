using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Services;
using AhBearStudios.Core.Pooling;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Factories;

/// <summary>
/// Factory for creating message registry services.
/// Simple creation following CLAUDE.md guidelines - no lifecycle management.
/// </summary>
public sealed class MessageRegistryFactory : IMessageRegistryFactory
{
    /// <inheritdoc />
    public async UniTask<IMessageRegistry> CreateServiceAsync(
        MessageRegistryConfig config,
        ILoggingService logger,
        IPoolingService poolingService)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        
        if (logger == null)
            throw new ArgumentNullException(nameof(logger));
        
        if (poolingService == null)
            throw new ArgumentNullException(nameof(poolingService));

        // Validate configuration before creating service
        var validationErrors = config.Validate();
        if (validationErrors.Count > 0)
        {
            var errorMessage = $"Invalid MessageRegistryConfig: {string.Join(", ", validationErrors)}";
            throw new ArgumentException(errorMessage, nameof(config));
        }

        await UniTask.SwitchToMainThread();

        // Simple creation - factory doesn't track the created instance
        return new MessageRegistry(config, logger, poolingService);
    }

    /// <inheritdoc />
    public async UniTask<IMessageRegistry> CreateDefaultServiceAsync(
        ILoggingService logger,
        IPoolingService poolingService)
    {
        return await CreateServiceAsync(
            MessageRegistryConfig.Default,
            logger,
            poolingService);
    }
}