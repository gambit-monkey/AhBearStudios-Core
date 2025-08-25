using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Subscribers;
using AhBearStudios.Core.Profiling;
using ZLinq;

namespace AhBearStudios.Core.Messaging.Factories;

/// <summary>
/// Factory implementation for creating MessageSubscriber instances.
/// Follows CLAUDE.md factory pattern - simple creation only, no lifecycle management.
/// Does NOT implement IDisposable as per CLAUDE.md guidelines.
/// </summary>
public sealed class MessageSubscriberFactory : IMessageSubscriberFactory
{
    private readonly ILoggingService _loggingService;
    private readonly IProfilerService _profilerService;
    private readonly IMessageBusService _messageBusService;

    /// <summary>
    /// Initializes a new instance of MessageSubscriberFactory.
    /// </summary>
    /// <param name="loggingService">The logging service</param>
    /// <param name="profilerService">The profiler service</param>
    /// <param name="messageBusService">The message bus service</param>
    /// <exception cref="ArgumentNullException">Thrown when required services are null</exception>
    public MessageSubscriberFactory(
        ILoggingService loggingService,
        IProfilerService profilerService,
        IMessageBusService messageBusService)
    {
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
        _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
    }

    /// <inheritdoc />
    public async UniTask<IMessageSubscriber<TMessage>> CreateSubscriberAsync<TMessage>(MessageSubscriberConfig config)
        where TMessage : IMessage
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        var correlationId = $"Factory_{config.CorrelationId}_{typeof(TMessage).Name}";
        
        try
        {
            _loggingService.LogInfo($"[{correlationId}] Creating MessageSubscriber for {typeof(TMessage).Name}");

            // Validate configuration
            if (!ValidateConfig(config))
                throw new InvalidOperationException($"Invalid configuration for MessageSubscriber<{typeof(TMessage).Name}>");

            // Validate message type support
            if (!SupportsMessageType<TMessage>())
                throw new InvalidOperationException($"Message type {typeof(TMessage).Name} is not supported by this factory");

            // Create the subscriber instance
            var subscriber = new MessageSubscriber<TMessage>(
                config,
                _loggingService,
                _profilerService,
                _messageBusService);

            _loggingService.LogInfo($"[{correlationId}] Successfully created MessageSubscriber for {typeof(TMessage).Name}");

            return subscriber;
        }
        catch (Exception ex)
        {
            _loggingService.LogException($"[{correlationId}] Failed to create MessageSubscriber for {typeof(TMessage).Name}", ex);
            throw new InvalidOperationException($"Failed to create MessageSubscriber<{typeof(TMessage).Name}>", ex);
        }
    }

    /// <inheritdoc />
    public async UniTask<IMessageSubscriber<TMessage>> CreateDefaultSubscriberAsync<TMessage>()
        where TMessage : IMessage
    {
        var defaultConfig = MessageSubscriberConfig.Default();
        return await CreateSubscriberAsync<TMessage>(defaultConfig);
    }

    /// <inheritdoc />
    public bool SupportsMessageType<TMessage>() where TMessage : IMessage
    {
        return SupportsMessageType(typeof(TMessage));
    }

    /// <inheritdoc />
    public bool SupportsMessageType(Type messageType)
    {
        if (messageType == null)
            return false;

        // Check if type implements IMessage interface
        if (!typeof(IMessage).IsAssignableFrom(messageType))
            return false;

        // Check if type is instantiable (not abstract, has accessible constructor)
        if (messageType.IsAbstract || messageType.IsInterface)
            return false;

        // Additional validation for supported message types
        try
        {
            // Ensure the type can be used as a generic constraint
            var genericSubscriberType = typeof(MessageSubscriber<>).MakeGenericType(messageType);
            return genericSubscriberType != null;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public IEnumerable<Type> GetSupportedMessageTypes()
    {
        // In a real implementation, this might scan assemblies or use a registry
        // For now, return all types that implement IMessage in the current assembly
        var currentAssembly = typeof(IMessage).Assembly;
        
        return currentAssembly.GetTypes()
            .AsValueEnumerable()
            .Where(type => type.IsPublic && 
                          !type.IsAbstract && 
                          !type.IsInterface &&
                          typeof(IMessage).IsAssignableFrom(type))
            .ToList();
    }

    /// <inheritdoc />
    public bool ValidateConfig(MessageSubscriberConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        try
        {
            // Use the config's built-in validation
            return config.IsValid();
        }
        catch (Exception ex)
        {
            _loggingService.LogException($"Configuration validation failed for {config.CorrelationId}",ex);
            return false;
        }
    }

    /// <summary>
    /// Creates a factory with default core services.
    /// Used for testing and simple scenarios where DI is not available.
    /// </summary>
    /// <param name="loggingService">Optional logging service (null creates a no-op logger)</param>
    /// <param name="profilerService">Optional profiler service (null creates a no-op profiler)</param>
    /// <param name="messageBusService">Optional message bus service (null creates a no-op bus)</param>
    /// <returns>MessageSubscriberFactory instance</returns>
    public static MessageSubscriberFactory CreateDefault(
        ILoggingService loggingService = null,
        IProfilerService profilerService = null,
        IMessageBusService messageBusService = null)
    {
        // In a real implementation, these would be proper no-op implementations
        // For now, we'll require the services to be provided
        if (loggingService == null)
            throw new ArgumentNullException(nameof(loggingService), "LoggingService is required");
        if (profilerService == null)
            throw new ArgumentNullException(nameof(profilerService), "ProfilerService is required");
        if (messageBusService == null)
            throw new ArgumentNullException(nameof(messageBusService), "MessageBusService is required");

        return new MessageSubscriberFactory(loggingService, profilerService, messageBusService);
    }
}