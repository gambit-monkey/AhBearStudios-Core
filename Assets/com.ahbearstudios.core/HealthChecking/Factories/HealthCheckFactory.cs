using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Pooling;
using Reflex.Core;

namespace AhBearStudios.Core.HealthChecking.Factories;

/// <summary>
/// Production factory for creating health check instances with dependency injection support
/// </summary>
/// <remarks>
/// Handles creation of health check instances with proper dependency resolution and configuration
/// </remarks>
public sealed class HealthCheckFactory : IHealthCheckFactory
{
    private readonly ILoggingService _logger;
    private readonly Container _container;
    private readonly Dictionary<Type, Func<HealthCheckConfiguration, IHealthCheck>> _healthCheckCreators;
    private readonly HashSet<Type> _availableTypes;

    /// <summary>
    /// Initializes the factory with dependency injection container
    /// </summary>
    /// <param name="logger">Logging service for factory operations</param>
    /// <param name="container">Reflex container for dependency resolution</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null</exception>
    public HealthCheckFactory(ILoggingService logger, Container container)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _container = container ?? throw new ArgumentNullException(nameof(container));
        
        _healthCheckCreators = new Dictionary<Type, Func<HealthCheckConfiguration, IHealthCheck>>();
        _availableTypes = new HashSet<Type>();
        
        InitializeHealthCheckCreators();
        
        _logger.LogInfo($"HealthCheckFactory initialized with {_availableTypes.Count} available health check types");
    }

    /// <inheritdoc />
    public T CreateHealthCheck<T>(HealthCheckConfiguration config = null) where T : class, IHealthCheck
    {
        var healthCheckType = typeof(T);
        
        if (!CanCreateHealthCheck(healthCheckType))
        {
            var errorMessage = $"Cannot create health check of type {healthCheckType.Name}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        try
        {
            var healthCheck = CreateHealthCheckInternal(healthCheckType, config);
            
            if (healthCheck is T typedHealthCheck)
            {
                _logger.LogInfo($"Created health check: {healthCheckType.Name}");
                return typedHealthCheck;
            }
            
            var conversionError = $"Created health check is not of expected type {typeof(T).Name}";
            _logger.LogError(conversionError);
            throw new InvalidOperationException(conversionError);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, $"Failed to create health check of type {healthCheckType.Name}");
            throw;
        }
    }

    /// <inheritdoc />
    public IHealthCheck CreateHealthCheck(string typeName, HealthCheckConfiguration config = null)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));

        var healthCheckType = _availableTypes.FirstOrDefault(t => 
            t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
            t.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase));

        if (healthCheckType == null)
        {
            var errorMessage = $"Health check type '{typeName}' not found or not available";
            _logger.LogError(errorMessage);
            throw new ArgumentException(errorMessage, nameof(typeName));
        }

        return CreateHealthCheckInternal(healthCheckType, config);
    }

    /// <inheritdoc />
    public Dictionary<string, IHealthCheck> CreateHealthChecks(
        IEnumerable<Type> healthCheckTypes, 
        HealthCheckConfiguration defaultConfig = null)
    {
        if (healthCheckTypes == null)
            throw new ArgumentNullException(nameof(healthCheckTypes));

        var healthChecks = new Dictionary<string, IHealthCheck>();
        var failedCreations = new List<string>();

        foreach (var type in healthCheckTypes)
        {
            try
            {
                if (CanCreateHealthCheck(type))
                {
                    var healthCheck = CreateHealthCheckInternal(type, defaultConfig);
                    healthChecks[type.Name] = healthCheck;
                }
                else
                {
                    failedCreations.Add(type.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to create health check: {type.Name}");
                failedCreations.Add(type.Name);
            }
        }

        if (failedCreations.Count > 0)
        {
            _logger.LogWarning($"Failed to create {failedCreations.Count} health checks: {string.Join(", ", failedCreations)}");
        }

        _logger.LogInfo($"Successfully created {healthChecks.Count} health checks");
        return healthChecks;
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
        return _availableTypes.ToList().AsReadOnly();
    }

    private void InitializeHealthCheckCreators()
    {
        RegisterHealthCheckCreator<SystemResourceHealthCheck>((config) =>
        {
            var poolingService = _container.HasBinding<IPoolingService>() 
                ? _container.Resolve<IPoolingService>() 
                : null;
            return new SystemResourceHealthCheck(_logger, poolingService);
        });

        RegisterHealthCheckCreator<DatabaseHealthCheck>((config) =>
        {
            var healthCheckService = _container.Resolve<IHealthCheckService>();
            return new DatabaseHealthCheck(null, healthCheckService, _logger);
        });

        RegisterHealthCheckCreator<MessagingHealthCheck>((config) =>
        {
            var messageBusService = _container.Resolve<IMessageBusService>();
            var healthCheckService = _container.Resolve<IHealthCheckService>();
            return new MessagingHealthCheck(messageBusService, healthCheckService, _logger);
        });

        RegisterHealthCheckCreator<NetworkHealthCheck>((config) =>
        {
            var healthCheckService = _container.Resolve<IHealthCheckService>();
            return new NetworkHealthCheck(healthCheckService, _logger);
        });

        _logger.LogDebug($"Registered {_healthCheckCreators.Count} health check creators");
    }

    private void RegisterHealthCheckCreator<T>(Func<HealthCheckConfiguration, IHealthCheck> creator) 
        where T : class, IHealthCheck
    {
        var type = typeof(T);
        _healthCheckCreators[type] = creator;
        _availableTypes.Add(type);
    }

    private IHealthCheck CreateHealthCheckInternal(Type healthCheckType, HealthCheckConfiguration config)
    {
        if (_healthCheckCreators.TryGetValue(healthCheckType, out var creator))
        {
            var healthCheck = creator(config);
            
            if (config != null)
            {
                healthCheck.Configure(config.ToHealthCheckConfiguration());
            }
            
            return healthCheck;
        }

        throw new InvalidOperationException($"No creator registered for health check type: {healthCheckType.Name}");
    }
}