using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Profiling;
using ZLinq;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Builders;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.HealthChecking.Services;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Serialization;
using Reflex.Core;

namespace AhBearStudios.Core.HealthChecking.Factories;

/// <summary>
/// Production factory for creating HealthCheckService instances with consolidated service dependencies.
/// </summary>
/// <remarks>
/// Creates 4 consolidated services (operation, event, resilience, registry) that integrate directly
/// with core systems rather than wrapping them. Follows Builder → Config → Factory → Service pattern.
/// </remarks>
public sealed class HealthCheckServiceFactory : IHealthCheckServiceFactory
{
    private readonly ILoggingService _logger;
    private readonly IAlertService _alertService;
    private readonly IMessageBusService _messageBus;
    private readonly IPoolingService _poolingService;
    private readonly IProfilerService _profilerService;
    private readonly ISerializationService _serializationService;
    private readonly Container _container;
    private readonly HealthCheckServiceConfig _defaultConfig;
    private readonly ProfilerMarker _createServiceMarker = new ProfilerMarker("HealthCheckServiceFactory.CreateService");
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
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null</exception>
    public HealthCheckServiceFactory(
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
        
        _factoryId = DeterministicIdGenerator.GenerateHealthCheckId("HealthCheckServiceFactory", Environment.MachineName);
        _defaultConfig = CreateDefaultConfiguration();
        
        var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ServiceFactoryInit", _factoryId.ToString());
        _logger.LogInfo("HealthCheckServiceFactory initialized with consolidated service creation capabilities", (Guid)correlationId, sourceContext: nameof(HealthCheckServiceFactory));
    }

    /// <inheritdoc />
    public async UniTask<IHealthCheckService> CreateServiceAsync(HealthCheckServiceConfig config)
    {
        using (_createServiceMarker.Auto())
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CreateHealthCheckService", _factoryId.ToString());
            
            ValidateConfigurationOrThrow(config);
            ValidateDependenciesOrThrow();

            try
            {
                // Create consolidated services following simplified architecture
                var registryService = new HealthCheckRegistryService(
                    _logger,
                    config);

                var operationService = new HealthCheckOperationService(
                    _logger,
                    _profilerService,
                    config);

                var eventService = new HealthCheckEventService(
                    _messageBus,
                    _profilerService,
                    _alertService,
                    _logger);

                var resilienceService = new HealthCheckResilienceService(
                    _logger,
                    _messageBus,
                    _alertService);

                // Create main health check service with consolidated services
                var service = new HealthCheckService(
                    config,
                    operationService,
                    registryService,
                    eventService,
                    resilienceService,
                    _logger,
                    _alertService,
                    _profilerService,
                    _messageBus);

                _logger.LogInfo($"HealthCheckService created successfully with consolidated services", (Guid)correlationId, sourceContext: nameof(HealthCheckServiceFactory));

                // Publish service creation message
                await _messageBus.PublishMessageAsync(new HealthCheckServiceCreatedMessage
                {
                    Id = DeterministicIdGenerator.GenerateMessageId(messageType: "HealthCheckServiceCreated", source: "HealthCheckServiceFactory", correlationId: null),
                    TimestampTicks = DateTime.UtcNow.Ticks,
                    TypeCode = MessageTypeCodes.HealthCheckServiceCreatedMessage,
                    Source = "HealthCheckServiceFactory",
                    Priority = MessagePriority.Normal,
                    CorrelationId = correlationId,
                    ServiceId = DeterministicIdGenerator.GenerateHealthCheckId("HealthCheckService", Environment.MachineName).ToString(),
                    ConfigurationHash = config.GetHashCode().ToString()
                });

                return service;
            }
            catch (Exception ex)
            {
                _logger.LogException("Failed to create HealthCheckService", ex, correlationId, sourceContext: nameof(HealthCheckServiceFactory));
                
                await _messageBus.PublishMessageAsync(new HealthCheckServiceCreationFailedMessage
                {
                    Id = DeterministicIdGenerator.GenerateMessageId(messageType: "HealthCheckServiceCreationFailed", source: "HealthCheckServiceFactory", correlationId: null),
                    TimestampTicks = DateTime.UtcNow.Ticks,
                    TypeCode = MessageTypeCodes.HealthCheckServiceCreationFailedMessage,
                    Source = "HealthCheckServiceFactory",
                    Priority = MessagePriority.High,
                    CorrelationId = correlationId,
                    ErrorMessage = ex.Message,
                    ConfigType = config?.GetType().Name ?? "null"
                });
                
                throw new InvalidOperationException("Failed to create health check service", ex);
            }
        }
    }

    /// <inheritdoc />
    public async UniTask<IHealthCheckService> CreateServiceWithDefaultsAsync()
    {
        var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CreateServiceWithDefaults", _factoryId.ToString());
        _logger.LogInfo("Creating HealthCheckService with default configuration", (Guid)correlationId, sourceContext: nameof(HealthCheckServiceFactory));
        return await CreateServiceAsync(_defaultConfig);
    }

    /// <inheritdoc />
    public bool ValidateDependencies()
    {
        var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ValidateDependencies", _factoryId.ToString());
        var requiredDependencies = new[]
        {
            typeof(ILoggingService),
            typeof(IAlertService),
            typeof(IMessageBusService),
            typeof(IPoolingService),
            typeof(IProfilerService),
            typeof(ISerializationService)
        };

        var missingDependencies = requiredDependencies
            .AsValueEnumerable()
            .Where(dependency => !_container.HasBinding(dependency))
            .Select(dependency => dependency.Name)
            .ToList();

        if (missingDependencies.Count > 0)
        {
            _logger.LogError($"Missing required dependencies: {string.Join(", ", missingDependencies)}", (Guid)correlationId, sourceContext: nameof(HealthCheckServiceFactory));
            return false;
        }

        _logger.LogDebug("All required dependencies validated successfully", (Guid)correlationId, sourceContext: nameof(HealthCheckServiceFactory));
        return true;
    }

    /// <inheritdoc />
    public HealthCheckServiceConfig GetDefaultConfiguration()
    {
        return _defaultConfig;
    }

    private HealthCheckServiceConfig CreateDefaultConfiguration()
    {
        return new HealthCheckServiceConfigBuilder(_logger)
            .WithAutomaticCheckInterval(TimeSpan.FromMinutes(1))
            .WithDefaultTimeout(TimeSpan.FromSeconds(30))
            .WithAutomaticChecks(enabled: true)
            .WithMaxHistorySize(100)
            .WithCircuitBreaker(enabled: true)
            .WithGracefulDegradation(enabled: true)
            .WithProfiling(enabled: true, slowThreshold: 1000)
            .WithHealthCheckLogging(enabled: true)
            .Build();
    }

    private void ValidateConfigurationOrThrow(HealthCheckServiceConfig config)
    {
        var validationErrors = config.Validate();
        if (validationErrors.Count > 0)
        {
            var errorMessage = $"Invalid configuration: {string.Join(", ", validationErrors)}";
            _logger.LogError(errorMessage, (Guid)default, sourceContext: nameof(HealthCheckServiceFactory));
            throw new InvalidOperationException(errorMessage);
        }
    }

    private void ValidateDependenciesOrThrow()
    {
        if (!ValidateDependencies())
        {
            throw new InvalidOperationException("Required dependencies are not available for service creation");
        }
    }

}