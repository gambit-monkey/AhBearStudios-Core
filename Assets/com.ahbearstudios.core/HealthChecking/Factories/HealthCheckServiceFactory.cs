using System.Collections.Generic;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.HealthChecking.Builders;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using Reflex.Core;

namespace AhBearStudios.Core.HealthChecking.Factories;

/// <summary>
/// Production factory for creating HealthCheckService instances with full dependency injection
/// </summary>
/// <remarks>
/// Manages the complete lifecycle of health check service creation including dependency
/// validation, configuration verification, and proper service initialization
/// </remarks>
public sealed class HealthCheckServiceFactory : IHealthCheckServiceFactory
{
    private readonly ILoggingService _logger;
    private readonly Container _container;
    private readonly IHealthCheckFactory _healthCheckFactory;
    private readonly CircuitBreakerFactory _circuitBreakerFactory;
    private readonly HealthCheckServiceConfig _defaultConfig;

    /// <summary>
    /// Initializes the factory with all required dependencies
    /// </summary>
    /// <param name="logger">Logging service for factory operations</param>
    /// <param name="container">Reflex container for dependency resolution</param>
    /// <param name="healthCheckFactory">Factory for creating individual health checks</param>
    /// <param name="circuitBreakerFactory">Factory for creating circuit breakers</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null</exception>
    public HealthCheckServiceFactory(
        ILoggingService logger,
        Container container,
        IHealthCheckFactory healthCheckFactory,
        CircuitBreakerFactory circuitBreakerFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _healthCheckFactory = healthCheckFactory ?? throw new ArgumentNullException(nameof(healthCheckFactory));
        _circuitBreakerFactory = circuitBreakerFactory ?? throw new ArgumentNullException(nameof(circuitBreakerFactory));
        
        _defaultConfig = CreateDefaultConfiguration();
        
        _logger.LogInfo("HealthCheckServiceFactory initialized with all dependencies");
    }

    /// <inheritdoc />
    public IHealthCheckService CreateService(HealthCheckServiceConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        ValidateConfigurationOrThrow(config);
        ValidateDependenciesOrThrow();

        try
        {
            var alertService = _container.Resolve<IAlertService>();
            var messageBusService = _container.Resolve<IMessageBusService>();
            var poolingService = _container.HasBinding<IPoolingService>() 
                ? _container.Resolve<IPoolingService>() 
                : null;
            var profilerService = _container.HasBinding<IProfilerService>() 
                ? _container.Resolve<IProfilerService>() 
                : null;

            var service = new HealthCheckService(
                config,
                _logger,
                alertService,
                messageBusService,
                _healthCheckFactory,
                _circuitBreakerFactory,
                poolingService,
                profilerService);

            _logger.LogInfo($"HealthCheckService created successfully with {config.GetType().Name}");
            return service;
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "Failed to create HealthCheckService");
            throw new InvalidOperationException("Failed to create health check service", ex);
        }
    }

    /// <inheritdoc />
    public IHealthCheckService CreateServiceWithDefaults()
    {
        _logger.LogInfo("Creating HealthCheckService with default configuration");
        return CreateService(_defaultConfig);
    }

    /// <inheritdoc />
    public bool ValidateDependencies()
    {
        var requiredDependencies = new[]
        {
            typeof(ILoggingService),
            typeof(IAlertService),
            typeof(IMessageBusService),
            typeof(IHealthCheckFactory),
            typeof(CircuitBreakerFactory)
        };

        var missingDependencies = new List<string>();
        
        foreach (var dependency in requiredDependencies)
        {
            if (!_container.HasBinding(dependency))
            {
                missingDependencies.Add(dependency.Name);
            }
        }

        if (missingDependencies.Count > 0)
        {
            _logger.LogError($"Missing required dependencies: {string.Join(", ", missingDependencies)}");
            return false;
        }

        _logger.LogDebug("All required dependencies validated successfully");
        return true;
    }

    /// <inheritdoc />
    public HealthCheckServiceConfig GetDefaultConfiguration()
    {
        return _defaultConfig;
    }

    private HealthCheckServiceConfig CreateDefaultConfiguration()
    {
        return new HealthCheckServiceConfigBuilder()
            .WithDefaultHealthCheckInterval(TimeSpan.FromMinutes(1))
            .WithDefaultTimeout(TimeSpan.FromSeconds(30))
            .WithAutomaticChecks(enabled: true)
            .WithHistoryRetention(TimeSpan.FromHours(24))
            .WithCircuitBreakers(enabled: true)
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
            _logger.LogError(errorMessage);
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