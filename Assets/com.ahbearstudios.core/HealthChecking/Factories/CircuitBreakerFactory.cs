using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Profiling;
using ZLinq;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Common.Extensions;
using AhBearStudios.Core.HealthChecking.Builders;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Serialization;

namespace AhBearStudios.Core.HealthChecking.Factories;

/// <summary>
/// Factory for creating circuit breaker instances with proper configuration and monitoring.
/// Follows Builder → Config → Factory → Service pattern - factories only create, never manage lifecycle.
/// </summary>
/// <remarks>
/// Provides creation of circuit breakers with health check integration and dependency management.
/// Circuit breaker lifecycle is managed by IPoolingService or the consuming services.
/// </remarks>
public sealed class CircuitBreakerFactory : ICircuitBreakerFactory
{
    private readonly ILoggingService _logger;
    private readonly IAlertService _alertService;
    private readonly IMessageBusService _messageBus;
    private readonly IPoolingService _poolingService;
    private readonly IProfilerService _profilerService;
    private readonly ISerializationService _serializationService;
    private readonly IHealthCheckService _healthCheckService;
    private readonly CircuitBreakerConfig _defaultConfig;
    private readonly ProfilerMarker _createCircuitBreakerMarker = new ProfilerMarker("CircuitBreakerFactory.CreateCircuitBreaker");
    private readonly Guid _factoryId;
    private readonly HashSet<Type> _availableTypes;

    /// <summary>
    /// Initializes the factory with all core system dependencies
    /// </summary>
    /// <param name="logger">Logging service for circuit breaker operations</param>
    /// <param name="alertService">Alert service for circuit breaker notifications</param>
    /// <param name="messageBus">Message bus for factory events</param>
    /// <param name="poolingService">Pooling service for memory management</param>
    /// <param name="profilerService">Profiler service for performance monitoring</param>
    /// <param name="serializationService">Serialization service for configurations</param>
    /// <param name="healthCheckService">Health check service for integration</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null</exception>
    public CircuitBreakerFactory(
        ILoggingService logger,
        IAlertService alertService,
        IMessageBusService messageBus,
        IPoolingService poolingService,
        IProfilerService profilerService,
        ISerializationService serializationService,
        IHealthCheckService healthCheckService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _poolingService = poolingService ?? throw new ArgumentNullException(nameof(poolingService));
        _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
        _serializationService = serializationService ?? throw new ArgumentNullException(nameof(serializationService));
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
        
        _factoryId = DeterministicIdGenerator.GenerateHealthCheckId("CircuitBreakerFactory", Environment.MachineName);
        _availableTypes = new HashSet<Type> { typeof(CircuitBreaker) };
        _defaultConfig = CreateDefaultConfiguration();
        
        var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerFactoryInit", _factoryId.ToString());
        _logger.LogInfo("CircuitBreakerFactory initialized with all dependencies", correlationId, sourceContext: null);
    }

    /// <inheritdoc />
    public async UniTask<ICircuitBreaker> CreateCircuitBreakerAsync(string operationName, CircuitBreakerConfig config = null)
    {
        using (_createCircuitBreakerMarker.Auto())
        {
            if (string.IsNullOrWhiteSpace(operationName))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("CreateCircuitBreaker", operationName);
            var circuitBreakerConfig = config ?? _defaultConfig;
            
            ValidateConfigurationOrThrow(circuitBreakerConfig);

            var circuitBreaker = new CircuitBreaker(operationName, circuitBreakerConfig, _logger, _messageBus);
            
            await ConfigureCircuitBreakerIntegrationAsync(circuitBreaker, correlationId);
            
            _logger.LogInfo($"Created circuit breaker for operation: {operationName}", correlationId, sourceContext: null);

            await _messageBus.PublishMessageAsync(new CircuitBreakerCreatedMessage
            {
                Id = DeterministicIdGenerator.GenerateMessageId(messageType: "CircuitBreakerCreated", source: operationName),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.CircuitBreakerCreatedMessage,
                Source = "CircuitBreakerFactory",
                Priority = MessagePriority.Normal,
                CorrelationId = correlationId,
                OperationName = operationName
            });

            return circuitBreaker;
        }
    }

    /// <inheritdoc />
    public async UniTask<ICircuitBreaker> CreateCircuitBreakerWithHealthCheckAsync(
        string operationName, 
        string healthCheckName, 
        CircuitBreakerConfig config = null)
    {
        var circuitBreaker = await CreateCircuitBreakerAsync(operationName, config);
        
        // Register health check integration
        if (!string.IsNullOrWhiteSpace(healthCheckName))
        {
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ConfigureHealthCheckIntegration", operationName);
            await ConfigureHealthCheckIntegrationAsync(circuitBreaker, healthCheckName, correlationId);
        }
        
        return circuitBreaker;
    }

    /// <inheritdoc />
    public IEnumerable<Type> GetAvailableCircuitBreakerTypes()
    {
        return _availableTypes.AsValueEnumerable().ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public bool ValidateConfiguration(CircuitBreakerConfig config)
    {
        if (config == null)
            return false;
            
        var validationErrors = config.Validate();
        return validationErrors.Count == 0;
    }



    /// <inheritdoc />
    public void ValidateDependencies()
    {
        var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ValidateDependencies", _factoryId.ToString());
        var missingDependencies = new List<string>();

        if (_logger == null) missingDependencies.Add(nameof(ILoggingService));
        if (_alertService == null) missingDependencies.Add(nameof(IAlertService));
        if (_messageBus == null) missingDependencies.Add(nameof(IMessageBusService));
        if (_poolingService == null) missingDependencies.Add(nameof(IPoolingService));
        if (_profilerService == null) missingDependencies.Add(nameof(IProfilerService));
        if (_serializationService == null) missingDependencies.Add(nameof(ISerializationService));
        if (_healthCheckService == null) missingDependencies.Add(nameof(IHealthCheckService));

        if (missingDependencies.Count > 0)
        {
            var errorMessage = $"Required dependencies are missing: {string.Join(", ", missingDependencies)}";
            _logger?.LogError(errorMessage, correlationId, sourceContext: null);
            throw new InvalidOperationException(errorMessage);
        }

        var validationErrors = _defaultConfig.Validate();
        if (validationErrors.Count > 0)
        {
            var errorMessage = $"Default configuration validation failed: {string.Join(", ", validationErrors)}";
            _logger.LogError(errorMessage, correlationId, sourceContext: null);
            throw new InvalidOperationException(errorMessage);
        }

        _logger.LogDebug("CircuitBreakerFactory dependencies validated successfully", correlationId, sourceContext: null);
    }

    /// <summary>
    /// Validates factory configuration and dependencies
    /// </summary>
    /// <returns>True if factory is properly configured</returns>
    public bool ValidateFactory()
    {
        try
        {
            ValidateDependencies();
            return true;
        }
        catch (Exception ex)
        {
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ValidateFactory", _factoryId.ToString());
            _logger?.LogException("Circuit breaker factory validation failed", ex, correlationId.ToCorrelationFixedString());
            return false;
        }
    }

    private CircuitBreakerConfig CreateDefaultConfiguration()
    {
        return new CircuitBreakerConfigBuilder(_logger)
            .WithFailureThreshold(5)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .WithSuccessThreshold(80.0)
            .WithSlidingWindow(true, SlidingWindowType.CountBased, 10)
            .WithHalfOpenMaxCalls(2)
            .Build();
    }

    private void ValidateConfigurationOrThrow(CircuitBreakerConfig config)
    {
        var validationErrors = config.Validate();
        if (validationErrors.Count > 0)
        {
            var errorMessage = $"Invalid circuit breaker configuration: {string.Join(", ", validationErrors)}";
            _logger.LogError(errorMessage, Guid.Empty, sourceContext: null);
            throw new InvalidOperationException(errorMessage);
        }
    }

    private async UniTask ConfigureCircuitBreakerIntegrationAsync(ICircuitBreaker circuitBreaker, Guid correlationId)
    {
        // Subscribe to circuit breaker state change messages via message bus instead of events
        _messageBus.SubscribeToMessageAsync<HealthCheckCircuitBreakerStateChangedMessage>(OnCircuitBreakerStateChangedMessage);
        
        await _messageBus.PublishMessageAsync(new HealthCheckCircuitBreakerIntegrationConfiguredMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId(messageType: "CircuitBreakerIntegrationConfigured", source: circuitBreaker.Name.ToString()),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.HealthCheckCircuitBreakerIntegrationConfiguredMessage,
            Source = "CircuitBreakerFactory",
            Priority = MessagePriority.Low,
            CorrelationId = correlationId,
            CircuitBreakerName = circuitBreaker.Name
        });
    }

    private async UniTask ConfigureHealthCheckIntegrationAsync(ICircuitBreaker circuitBreaker, string healthCheckName, Guid correlationId)
    {
        // Integration with health check service
        _logger.LogDebug($"Configured health check integration for circuit breaker: {circuitBreaker.Name} -> {healthCheckName}", correlationId, sourceContext: null);
        
        await _messageBus.PublishMessageAsync(new HealthCheckCircuitBreakerIntegrationConfiguredMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId(messageType: "HealthCheckIntegrationConfigured", source: circuitBreaker.Name.ToString()),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.HealthCheckCircuitBreakerIntegrationConfiguredMessage,
            Source = "CircuitBreakerFactory",
            Priority = MessagePriority.Low,
            CorrelationId = correlationId,
            CircuitBreakerName = circuitBreaker.Name,
            HealthCheckName = healthCheckName
        });
    }

    private async UniTask OnCircuitBreakerStateChangedMessage(HealthCheckCircuitBreakerStateChangedMessage message)
    {
        var correlationId = DeterministicIdGenerator.GenerateCorrelationId(operation: "ProcessCircuitBreakerStateChanged", context: message.CircuitBreakerName.ToString());
        
        // Parse state enum values from message
        if (!Enum.TryParse<CircuitBreakerState>(message.NewState.ToString(), out var newState))
            newState = CircuitBreakerState.Closed;
        
        if (!Enum.TryParse<CircuitBreakerState>(message.OldState.ToString(), out var oldState))
            oldState = CircuitBreakerState.Closed;

        var severity = newState switch
        {
            CircuitBreakerState.Open => AlertSeverity.High,
            CircuitBreakerState.HalfOpen => AlertSeverity.Medium,
            CircuitBreakerState.Closed => AlertSeverity.Low,
            _ => AlertSeverity.Medium
        };

        await _alertService.RaiseAlertAsync(
            message: $"Circuit breaker state changed: {message.OldState} -> {message.NewState}",
            severity: severity,
            source: "CircuitBreakerFactory",
            correlationId: correlationId);

        _logger.LogInfo($"Circuit breaker '{message.CircuitBreakerName}' state changed: {message.OldState} -> {message.NewState}", correlationId, sourceContext: null);
    }
}