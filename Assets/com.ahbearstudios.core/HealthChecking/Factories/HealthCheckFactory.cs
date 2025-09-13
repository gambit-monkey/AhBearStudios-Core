using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Profiling;
using ZLinq;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.HealthChecks;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Serialization;
using Reflex.Core;

namespace AhBearStudios.Core.HealthChecking.Factories;

/// <summary>
/// Production factory for creating health check instances with dependency injection support
/// </summary>
/// <remarks>
/// Handles creation of health check instances with proper dependency resolution and configuration.
/// Follows Builder → Config → Factory → Service pattern from CLAUDE.md.
/// </remarks>
public sealed class HealthCheckFactory : IHealthCheckFactory
{
    private readonly ILoggingService _logger;
    private readonly IAlertService _alertService;
    private readonly IMessageBusService _messageBus;
    private readonly IPoolingService _poolingService;
    private readonly IProfilerService _profilerService;
    private readonly ISerializationService _serializationService;
    private readonly Container _container;
    private readonly Dictionary<Type, Func<HealthCheckConfiguration, UniTask<IHealthCheck>>> _healthCheckCreators;
    private readonly HashSet<Type> _availableTypes;
    private readonly ProfilerMarker _createHealthCheckMarker = new ProfilerMarker("HealthCheckFactory.CreateHealthCheck");
    private readonly Guid _factoryId;

    /// <summary>
    /// Initializes the factory with all core system dependencies
    /// </summary>
    /// <param name="logger">Logging service for factory operations</param>
    /// <param name="alertService">Alert service for critical notifications</param>
    /// <param name="messageBus">Message bus for factory events</param>
    /// <param name="poolingService">Pooling service for memory management</param>
    /// <param name="profilerService">Profiler service for performance monitoring</param>
    /// <param name="serializationService">Serialization service for configurations</param>
    /// <param name="container">Reflex container for dependency resolution</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
    public HealthCheckFactory(
        ILoggingService logger,
        IAlertService alertService,
        IMessageBusService messageBus,
        IPoolingService poolingService,
        IProfilerService profilerService,
        ISerializationService serializationService,
        Container container)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _poolingService = poolingService ?? throw new ArgumentNullException(nameof(poolingService));
        _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
        _serializationService = serializationService ?? throw new ArgumentNullException(nameof(serializationService));
        _container = container ?? throw new ArgumentNullException(nameof(container));
        
        _factoryId = DeterministicIdGenerator.GenerateHealthCheckId("HealthCheckFactory", Environment.MachineName);
        _healthCheckCreators = new Dictionary<Type, Func<HealthCheckConfiguration, UniTask<IHealthCheck>>>();
        _availableTypes = new HashSet<Type>();
        
        InitializeHealthCheckCreators();
        
        var correlationId = DeterministicIdGenerator.GenerateCorrelationId("FactoryInit", _factoryId.ToString());
        _logger.LogInfo($"HealthCheckFactory initialized with {_availableTypes.Count} available health check types", correlationId, sourceContext: nameof(HealthCheckFactory));
    }

    /// <inheritdoc />
    public async UniTask<T> CreateHealthCheckAsync<T>(HealthCheckConfiguration config = null) where T : class, IHealthCheck
    {
        using (_createHealthCheckMarker.Auto())
        {
            var healthCheckType = typeof(T);
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CreateHealthCheck", healthCheckType.Name);
            
            if (!CanCreateHealthCheck(healthCheckType))
            {
                var errorMessage = $"Cannot create health check of type {healthCheckType.Name}";
                _logger.LogError(errorMessage, correlationId, sourceContext: nameof(HealthCheckFactory));
                
                await _messageBus.PublishMessageAsync(new HealthCheckFactoryErrorMessage
                {
                    Id = DeterministicIdGenerator.GenerateMessageId(messageType: "HealthCheckFactoryError", source: "HealthCheckFactory", correlationId: null),
                    TimestampTicks = DateTime.UtcNow.Ticks,
                    TypeCode = MessageTypeCodes.HealthCheckFactoryErrorMessage,
                    Source = "HealthCheckFactory",
                    Priority = MessagePriority.High,
                    CorrelationId = correlationId,
                    ErrorMessage = errorMessage,
                    HealthCheckType = healthCheckType.Name
                });
                
                throw new InvalidOperationException(errorMessage);
            }

            try
            {
                var healthCheck = await CreateHealthCheckInternalAsync(healthCheckType, config);
                
                if (healthCheck is T typedHealthCheck)
                {
                    _logger.LogInfo($"Created health check: {healthCheckType.Name}", correlationId, sourceContext: nameof(HealthCheckFactory));
                    
                    await _messageBus.PublishMessageAsync(new HealthCheckCreatedMessage
                    {
                        Id = DeterministicIdGenerator.GenerateMessageId(messageType: "HealthCheckCreated", source: healthCheckType.Name, correlationId: null),
                        TimestampTicks = DateTime.UtcNow.Ticks,
                        TypeCode = MessageTypeCodes.HealthCheckCreatedMessage,
                        Source = "HealthCheckFactory",
                        Priority = MessagePriority.Normal,
                        CorrelationId = correlationId,
                        HealthCheckName = healthCheckType.Name,
                        HealthCheckType = healthCheckType.FullName
                    });
                    
                    return typedHealthCheck;
                }
                
                var conversionError = $"Created health check is not of expected type {typeof(T).Name}";
                _logger.LogError(conversionError, correlationId, sourceContext: nameof(HealthCheckFactory));
                throw new InvalidOperationException(conversionError);
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to create health check of type {healthCheckType.Name}", ex, correlationId.ToString());
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async UniTask<IHealthCheck> CreateHealthCheckAsync(string typeName, HealthCheckConfiguration config = null)
    {
        using (_createHealthCheckMarker.Auto())
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CreateHealthCheckByName", typeName);
            var healthCheckType = _availableTypes.AsValueEnumerable().FirstOrDefault(t => 
                t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
                t.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase));

            if (healthCheckType == null)
            {
                var errorMessage = $"Health check type '{typeName}' not found or not available";
                _logger.LogError(errorMessage, correlationId, sourceContext: nameof(HealthCheckFactory));
                
                await _messageBus.PublishMessageAsync(new HealthCheckFactoryErrorMessage
                {
                    Id = DeterministicIdGenerator.GenerateMessageId(messageType: "HealthCheckFactoryError", source: "HealthCheckFactory", correlationId: null),
                    TimestampTicks = DateTime.UtcNow.Ticks,
                    TypeCode = MessageTypeCodes.HealthCheckFactoryErrorMessage,
                    Source = "HealthCheckFactory",
                    Priority = MessagePriority.High,
                    CorrelationId = correlationId,
                    ErrorMessage = errorMessage,
                    HealthCheckType = typeName
                });
                
                throw new ArgumentException(errorMessage, nameof(typeName));
            }

            return await CreateHealthCheckInternalAsync(healthCheckType, config);
        }
    }

    /// <inheritdoc />
    public async UniTask<Dictionary<string, IHealthCheck>> CreateHealthChecksAsync(
        IEnumerable<Type> healthCheckTypes, 
        HealthCheckConfiguration defaultConfig = null)
    {
        using (_createHealthCheckMarker.Auto())
        {
            if (healthCheckTypes == null)
                throw new ArgumentNullException(nameof(healthCheckTypes));

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CreateMultipleHealthChecks", "Factory");
            var healthChecks = new Dictionary<string, IHealthCheck>();
            var failedCreations = new List<string>();

            foreach (var type in healthCheckTypes)
            {
                try
                {
                    if (CanCreateHealthCheck(type))
                    {
                        var healthCheck = await CreateHealthCheckInternalAsync(type, defaultConfig);
                        healthChecks[type.Name] = healthCheck;
                    }
                    else
                    {
                        failedCreations.Add(type.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogException($"Failed to create health check: {type.Name}", ex, correlationId.ToString(), sourceContext: nameof(HealthCheckFactory));
                    failedCreations.Add(type.Name);
                }
            }

            if (failedCreations.Count > 0)
            {
                _logger.LogWarning($"Failed to create {failedCreations.Count} health checks: {string.Join(", ", failedCreations)}", correlationId, sourceContext: nameof(HealthCheckFactory));
            }

            _logger.LogInfo($"Successfully created {healthChecks.Count} health checks", correlationId, sourceContext: nameof(HealthCheckFactory));
            return healthChecks;
        }
    }

    /// <inheritdoc />
    public bool CanCreateHealthCheck(Type healthCheckType)
    {
        if (healthCheckType == null)
            return false;

        return _availableTypes.Contains(healthCheckType) && 
               _healthCheckCreators.ContainsKey(healthCheckType);
    }

    /// <inheritdoc />
    public IEnumerable<Type> GetAvailableHealthCheckTypes()
    {
        return _availableTypes.AsValueEnumerable().ToList().AsReadOnly();
    }

    private void InitializeHealthCheckCreators()
    {
        RegisterHealthCheckCreator<SystemResourceHealthCheck>(async (config) =>
        {
            return new SystemResourceHealthCheck(_logger, _poolingService);
        });

        RegisterHealthCheckCreator<DatabaseHealthCheck>(async (config) =>
        {
            var healthCheckService = _container.Resolve<IHealthCheckService>();
            var databaseService = _container.Resolve<IDatabaseService>();
            return new DatabaseHealthCheck(databaseService, healthCheckService, _logger);
        });

        RegisterHealthCheckCreator<MessageBusHealthCheck>(async (config) =>
        {
            var messageBusConfig = _container.Resolve<MessageBusConfig>();
            return new MessageBusHealthCheck(_messageBus, messageBusConfig, _logger);
        });

        // RegisterHealthCheckCreator<NetworkHealthCheck>(async (config) =>
        // {
        //     var healthCheckService = _container.Resolve<IHealthCheckService>();
        //     return new NetworkHealthCheck(healthCheckService, _logger, null);
        // });

        var correlationId = DeterministicIdGenerator.GenerateCorrelationId("InitializeCreators", _factoryId.ToString());
        _logger.LogDebug($"Registered {_healthCheckCreators.Count} health check creators", correlationId, sourceContext: nameof(HealthCheckFactory));
    }

    private void RegisterHealthCheckCreator<T>(Func<HealthCheckConfiguration, UniTask<IHealthCheck>> creator) 
        where T : class, IHealthCheck
    {
        var type = typeof(T);
        _healthCheckCreators[type] = creator;
        _availableTypes.Add(type);
    }

    private async UniTask<IHealthCheck> CreateHealthCheckInternalAsync(Type healthCheckType, HealthCheckConfiguration config)
    {
        if (_healthCheckCreators.TryGetValue(healthCheckType, out var creator))
        {
            var healthCheck = await creator(config);
            
            if (config != null)
            {
                // Configure the health check if configuration is provided
                // All IHealthCheck implementations have a Configure method
                healthCheck.Configure(config);
            }
            
            return healthCheck;
        }

        throw new InvalidOperationException($"No creator registered for health check type: {healthCheckType.Name}");
    }
}