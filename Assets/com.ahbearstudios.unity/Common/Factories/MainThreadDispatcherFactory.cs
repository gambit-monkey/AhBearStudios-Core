using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Unity.Common.Components;
using AhBearStudios.Unity.Common.Configs;
using UnityEngine;

namespace AhBearStudios.Unity.Common.Factories;

/// <summary>
/// Factory interface for creating MainThreadDispatcher instances.
/// Follows CLAUDE.md Factory pattern - simple creation only, no lifecycle management.
/// </summary>
public interface IMainThreadDispatcherFactory
{
    /// <summary>
    /// Creates a MainThreadDispatcher instance using the provided configuration.
    /// Simple creation only - does not track or manage the created instance.
    /// </summary>
    /// <param name="config">Validated configuration for the dispatcher</param>
    /// <returns>Configured MainThreadDispatcher instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when Unity context is invalid</exception>
    IMainThreadDispatcher CreateMainThreadDispatcher(MainThreadDispatcherConfig config);
}

/// <summary>
/// Factory implementation for creating MainThreadDispatcher instances.
/// Takes validated configs and creates instances - no lifecycle management.
/// </summary>
public sealed class MainThreadDispatcherFactory : IMainThreadDispatcherFactory
{
    private readonly ILoggingService _logger;
    private readonly IMessageBusService _messageBus;
    private readonly IHealthCheckService _healthCheckService;

    /// <summary>
    /// Initializes a new MainThreadDispatcherFactory.
    /// </summary>
    /// <param name="logger">Logging service for factory operations</param>
    /// <param name="messageBus">Optional message bus for dispatcher events</param>
    /// <param name="healthCheckService">Optional health check service for monitoring</param>
    public MainThreadDispatcherFactory(
        ILoggingService logger,
        IMessageBusService messageBus = null,
        IHealthCheckService healthCheckService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageBus = messageBus;
        _healthCheckService = healthCheckService;
    }

    public IMainThreadDispatcher CreateMainThreadDispatcher(MainThreadDispatcherConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (!config.IsValid())
        {
            var message = "MainThreadDispatcherConfig is invalid. Use builder to create valid configuration.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        try
        {
            _logger.LogInfo($"Creating MainThreadDispatcher with config: FrameBudget={config.FrameBudgetMs}ms, MaxQueue={config.MaxQueueCapacity}");

            // Create GameObject for the dispatcher
            var gameObject = new GameObject(config.GameObjectName);
            
            if (config.DontDestroyOnLoad)
            {
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
            }

            // Add and configure the dispatcher component
            var dispatcher = gameObject.AddComponent<UnityMainThreadDispatcher>();
            
            // Initialize with dependencies - dispatcher handles its own setup
            dispatcher.Initialize(config, _logger, _messageBus, _healthCheckService);

            _logger.LogInfo($"MainThreadDispatcher created successfully: GameObject='{config.GameObjectName}'");
            
            return dispatcher;
        }
        catch (Exception ex)
        {
            var message = $"Failed to create MainThreadDispatcher: {ex.Message}";
            _logger.LogException(message, ex);
            throw new InvalidOperationException(message, ex);
        }
    }
}