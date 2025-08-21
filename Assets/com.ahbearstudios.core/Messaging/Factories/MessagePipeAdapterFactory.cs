using System;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Services;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Serialization;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Factories;

/// <summary>
/// Factory for creating MessagePipe adapter services.
/// Simple creation following CLAUDE.md guidelines - no lifecycle management.
/// </summary>
public sealed class MessagePipeAdapterFactory : IMessagePipeAdapterFactory
{
    /// <inheritdoc />
    public async UniTask<IMessageBusAdapter> CreateServiceAsync(
        MessagePipeAdapterConfig config,
        ILoggingService logger,
        IAlertService alertService = null,
        IHealthCheckService healthCheckService = null,
        IPoolingService poolingService = null,
        IProfilerService profilerService = null,
        ISerializationService serializationService = null)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        
        if (logger == null)
            throw new ArgumentNullException(nameof(logger));

        // Validate configuration before creating service
        var validationErrors = config.Validate();
        if (validationErrors.Count > 0)
        {
            var errorMessage = $"Invalid MessagePipeAdapterConfig: {string.Join(", ", validationErrors)}";
            throw new ArgumentException(errorMessage, nameof(config));
        }

        await UniTask.SwitchToMainThread();

        // Simple creation - factory doesn't track the created instance
        // Note: MessagePipeAdapter doesn't currently take config in constructor
        // This could be enhanced to pass config to adapter when needed
        return new MessagePipeAdapter(
            logger,
            alertService,
            healthCheckService,
            poolingService,
            profilerService,
            serializationService);
    }

    /// <inheritdoc />
    public async UniTask<IMessageBusAdapter> CreateDefaultServiceAsync(
        ILoggingService logger,
        IAlertService alertService = null,
        IHealthCheckService healthCheckService = null,
        IPoolingService poolingService = null,
        IProfilerService profilerService = null,
        ISerializationService serializationService = null)
    {
        return await CreateServiceAsync(
            MessagePipeAdapterConfig.Default,
            logger,
            alertService,
            healthCheckService,
            poolingService,
            profilerService,
            serializationService);
    }
}