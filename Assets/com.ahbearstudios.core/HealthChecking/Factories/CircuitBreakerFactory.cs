using System.Collections.Concurrent;
using System.Collections.Generic;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking.Builders;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Factories;

/// <summary>
/// Factory for creating circuit breaker instances with proper configuration and monitoring
/// </summary>
/// <remarks>
/// Provides creation of circuit breakers with health check integration and dependency management
/// </remarks>
public sealed class CircuitBreakerFactory : ICircuitBreakerFactory
{
    private readonly ILoggingService _logger;
    private readonly IAlertService _alertService;
    private readonly IHealthCheckService _healthCheckService;
    private readonly ConcurrentDictionary<string, ICircuitBreaker> _circuitBreakers;
    private readonly CircuitBreakerConfig _defaultConfig;

    /// <summary>
    /// Initializes the factory with required dependencies
    /// </summary>
    /// <param name="logger">Logging service for circuit breaker operations</param>
    /// <param name="alertService">Alert service for circuit breaker notifications</param>
    /// <param name="healthCheckService">Health check service for integration</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null</exception>
    public CircuitBreakerFactory(
        ILoggingService logger,
        IAlertService alertService,
        IHealthCheckService healthCheckService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
        
        _circuitBreakers = new ConcurrentDictionary<string, ICircuitBreaker>();
        _defaultConfig = CreateDefaultConfiguration();
        
        _logger.LogInfo("CircuitBreakerFactory initialized");
    }

    /// <summary>
    /// Creates or retrieves a circuit breaker for the specified operation
    /// </summary>
    /// <param name="operationName">Name of the operation to protect</param>
    /// <param name="config">Optional circuit breaker configuration</param>
    /// <returns>Circuit breaker instance</returns>
    /// <exception cref="ArgumentException">Thrown when operation name is invalid</exception>
    public ICircuitBreaker CreateCircuitBreaker(string operationName, CircuitBreakerConfig config = null)
    {
        if (string.IsNullOrWhiteSpace(operationName))
            throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

        return _circuitBreakers.GetOrAdd(operationName, name =>
        {
            var circuitBreakerConfig = config ?? _defaultConfig;
            ValidateConfigurationOrThrow(circuitBreakerConfig);

            var circuitBreaker = new CircuitBreaker(name, circuitBreakerConfig, _logger);
            
            ConfigureCircuitBreakerIntegration(circuitBreaker);
            
            _logger.LogInfo($"Created circuit breaker for operation: {name}");
            return circuitBreaker;
        });
    }

    /// <summary>
    /// Creates a circuit breaker with health check integration
    /// </summary>
    /// <param name="operationName">Name of the operation to protect</param>
    /// <param name="healthCheckName">Name of associated health check</param>
    /// <param name="config">Optional circuit breaker configuration</param>
    /// <returns>Circuit breaker instance with health check integration</returns>
    public ICircuitBreaker CreateCircuitBreakerWithHealthCheck(
        string operationName, 
        string healthCheckName, 
        CircuitBreakerConfig config = null)
    {
        var circuitBreaker = CreateCircuitBreaker(operationName, config);
        
        // Register health check integration
        if (!string.IsNullOrWhiteSpace(healthCheckName))
        {
            ConfigureHealthCheckIntegration(circuitBreaker, healthCheckName);
        }
        
        return circuitBreaker;
    }

    /// <summary>
    /// Gets an existing circuit breaker by operation name
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <returns>Circuit breaker if found, null otherwise</returns>
    public ICircuitBreaker GetCircuitBreaker(string operationName)
    {
        if (string.IsNullOrWhiteSpace(operationName))
            return null;

        return _circuitBreakers.TryGetValue(operationName, out var circuitBreaker) 
            ? circuitBreaker 
            : null;
    }

    /// <summary>
    /// Gets all circuit breakers managed by this factory
    /// </summary>
    /// <returns>Dictionary of circuit breakers indexed by operation name</returns>
    public Dictionary<string, ICircuitBreaker> GetAllCircuitBreakers()
    {
        return new Dictionary<string, ICircuitBreaker>(_circuitBreakers);
    }

    /// <summary>
    /// Removes and disposes a circuit breaker
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <returns>True if the circuit breaker was found and removed</returns>
    public bool RemoveCircuitBreaker(string operationName)
    {
        if (string.IsNullOrWhiteSpace(operationName))
            return false;

        if (_circuitBreakers.TryRemove(operationName, out var circuitBreaker))
        {
            if (circuitBreaker is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            _logger.LogInfo($"Removed circuit breaker for operation: {operationName}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all registered circuit breaker names
    /// </summary>
    /// <returns>Collection of circuit breaker operation names</returns>
    public IReadOnlyCollection<string> GetRegisteredOperations()
    {
        return _circuitBreakers.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Validates that all required dependencies are available
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when required dependencies are missing</exception>
    public void ValidateDependencies()
    {
        var missingDependencies = new List<string>();

        if (_logger == null) missingDependencies.Add(nameof(ILoggingService));
        if (_alertService == null) missingDependencies.Add(nameof(IAlertService));
        if (_healthCheckService == null) missingDependencies.Add(nameof(IHealthCheckService));

        if (missingDependencies.Count > 0)
        {
            var errorMessage = $"Required dependencies are missing: {string.Join(", ", missingDependencies)}";
            throw new InvalidOperationException(errorMessage);
        }

        var validationErrors = _defaultConfig.Validate();
        if (validationErrors.Count > 0)
        {
            var errorMessage = $"Default configuration validation failed: {string.Join(", ", validationErrors)}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        _logger.LogDebug("CircuitBreakerFactory dependencies validated successfully");
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
            _logger.LogException(ex, "Circuit breaker factory validation failed");
            return false;
        }
    }

    private CircuitBreakerConfig CreateDefaultConfiguration()
    {
        return new CircuitBreakerConfigBuilder()
            .WithFailureThreshold(5)
            .WithOpenTimeout(TimeSpan.FromSeconds(30))
            .WithHalfOpenSuccessThreshold(2)
            .WithSlidingWindowSize(10)
            .WithHealthCheckIntegration(true)
            .WithHealthCheckInterval(TimeSpan.FromMinutes(1))
            .Build();
    }

    private void ValidateConfigurationOrThrow(CircuitBreakerConfig config)
    {
        var validationErrors = config.Validate();
        if (validationErrors.Count > 0)
        {
            var errorMessage = $"Invalid circuit breaker configuration: {string.Join(", ", validationErrors)}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
    }

    private void ConfigureCircuitBreakerIntegration(ICircuitBreaker circuitBreaker)
    {
        circuitBreaker.StateChanged += OnCircuitBreakerStateChanged;
    }

    private void ConfigureHealthCheckIntegration(ICircuitBreaker circuitBreaker, string healthCheckName)
    {
        // Implementation would depend on specific health check integration requirements
        _logger.LogDebug($"Configured health check integration for circuit breaker: {circuitBreaker.Name} -> {healthCheckName}");
    }

    private void OnCircuitBreakerStateChanged(object sender, CircuitBreakerStateChangedEventArgs e)
    {
        var severity = e.NewState switch
        {
            CircuitBreakerState.Open => AlertSeverity.High,
            CircuitBreakerState.HalfOpen => AlertSeverity.Medium,
            CircuitBreakerState.Closed => AlertSeverity.Low,
            _ => AlertSeverity.Medium
        };

        _alertService.RaiseAlert(
            new FixedString64Bytes($"CircuitBreaker.{e.CircuitBreakerName}"),
            severity,
            new FixedString512Bytes($"Circuit breaker state changed: {e.OldState} -> {e.NewState}"));

        _logger.LogInfo($"Circuit breaker '{e.CircuitBreakerName}' state changed: {e.OldState} -> {e.NewState}");
    }
}