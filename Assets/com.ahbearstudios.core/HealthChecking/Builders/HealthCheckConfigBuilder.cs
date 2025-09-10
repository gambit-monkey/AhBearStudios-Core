using System.Collections.Generic;
using ZLinq;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Builders;

/// <summary>
/// Simplified builder for HealthCheckConfiguration aligned with CLAUDE.md patterns.
/// Focuses on game development essentials with 60+ FPS performance targets.
/// Uses static factory methods and avoids field initializers.
/// </summary>
public sealed class HealthCheckConfigBuilder : IHealthCheckConfigBuilder
{
    private readonly ILoggingService _logger;
    private readonly List<string> _validationErrors = new();
    
    // Core configuration properties
    private string _name = string.Empty;
    private string _displayName = string.Empty;
    private HealthCheckCategory _category = HealthCheckCategory.Custom;
    private bool _enabled = true;
    private TimeSpan _interval = TimeSpan.FromSeconds(30);
    private TimeSpan _timeout = TimeSpan.FromSeconds(30);
    private int _priority = 100;
    
    // Resilience configuration
    private CircuitBreakerConfig _circuitBreakerConfig;
    private RetryConfig _retryConfig = new();
    private DegradationConfig _degradationConfig = new();
    private PerformanceConfig _performanceConfig;
    
    // Monitoring settings
    private bool _enableAlerting = true;
    private bool _enableProfiling = true;
    private bool _enableDetailedLogging = false;
    private LogLevel _logLevel = LogLevel.Info;
    private Dictionary<HealthStatus, AlertSeverity> _alertSeverities;
    private bool _isCritical = false;
    
    // Build state tracking
    private bool _isBuilt = false;

    /// <summary>
    /// Initializes a new instance of the HealthCheckConfigBuilder class
    /// </summary>
    /// <param name="logger">Logging service for build operations</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
    public HealthCheckConfigBuilder(ILoggingService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Initialize with sensible defaults
        _circuitBreakerConfig = CircuitBreakerConfig.Create("DefaultCircuitBreaker");
        _performanceConfig = PerformanceConfig.ForProduction();
        _alertSeverities = GetDefaultAlertSeverities();
        
        _logger.LogDebug("HealthCheckConfigBuilder initialized");
    }

    /// <summary>
    /// Creates a builder from an existing configuration
    /// </summary>
    /// <param name="existingConfig">Existing configuration to copy</param>
    /// <param name="logger">Logging service for build operations</param>
    /// <returns>New builder with copied settings</returns>
    /// <exception cref="ArgumentNullException">Thrown when existingConfig or logger is null</exception>
    public static HealthCheckConfigBuilder FromExisting(HealthCheckConfiguration existingConfig, ILoggingService logger)
    {
        if (existingConfig == null)
            throw new ArgumentNullException(nameof(existingConfig));
        if (logger == null)
            throw new ArgumentNullException(nameof(logger));

        var builder = new HealthCheckConfigBuilder(logger)
        {
            _name = existingConfig.Name.ToString(),
            _displayName = existingConfig.DisplayName,
            _category = existingConfig.Category,
            _enabled = existingConfig.Enabled,
            _interval = existingConfig.Interval,
            _timeout = existingConfig.Timeout,
            _priority = existingConfig.Priority,
            _circuitBreakerConfig = existingConfig.CircuitBreaker,
            _retryConfig = existingConfig.Retry,
            _degradationConfig = existingConfig.Degradation,
            _performanceConfig = existingConfig.Performance,
            _enableAlerting = existingConfig.EnableAlerting,
            _enableProfiling = existingConfig.EnableProfiling,
            _enableDetailedLogging = existingConfig.EnableDetailedLogging,
            _logLevel = existingConfig.LogLevel,
            _alertSeverities = new Dictionary<HealthStatus, AlertSeverity>(existingConfig.AlertSeverities),
            _isCritical = existingConfig.IsCritical
        };

        logger.LogDebug("HealthCheckConfigBuilder created from existing configuration: {Name}", existingConfig.Name);
        return builder;
    }

    #region Core Configuration Methods

    /// <summary>
    /// Sets the name for the health check
    /// </summary>
    /// <param name="name">Unique name for the health check</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentException">Thrown when name is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithName(string name)
    {
        ThrowIfBuilt();
        
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        _name = name;
        _logger.LogDebug("Health check name set to: {Name}", name);
        return this;
    }

    /// <summary>
    /// Sets the display name for the health check
    /// </summary>
    /// <param name="displayName">Human-readable display name</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithDisplayName(string displayName)
    {
        ThrowIfBuilt();
        _displayName = displayName ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets the category for the health check
    /// </summary>
    /// <param name="category">Health check category</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithCategory(HealthCheckCategory category)
    {
        ThrowIfBuilt();
        _category = category;
        return this;
    }

    /// <summary>
    /// Sets whether the health check is enabled
    /// </summary>
    /// <param name="enabled">Whether the health check is enabled</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithEnabled(bool enabled)
    {
        ThrowIfBuilt();
        _enabled = enabled;
        return this;
    }

    /// <summary>
    /// Sets the execution interval for the health check
    /// </summary>
    /// <param name="interval">Execution interval</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is zero or negative</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithInterval(TimeSpan interval)
    {
        ThrowIfBuilt();
        
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than zero");

        _interval = interval;
        return this;
    }

    /// <summary>
    /// Sets the timeout for the health check
    /// </summary>
    /// <param name="timeout">Execution timeout</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is zero or negative</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithTimeout(TimeSpan timeout)
    {
        ThrowIfBuilt();
        
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than zero");

        _timeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets the priority for the health check
    /// </summary>
    /// <param name="priority">Execution priority (higher numbers execute first)</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithPriority(int priority)
    {
        ThrowIfBuilt();
        _priority = priority;
        return this;
    }

    #endregion

    #region Resilience Configuration Methods

    /// <summary>
    /// Sets the circuit breaker configuration
    /// </summary>
    /// <param name="circuitBreakerConfig">Circuit breaker configuration</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentNullException">Thrown when circuitBreakerConfig is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithCircuitBreaker(CircuitBreakerConfig circuitBreakerConfig)
    {
        ThrowIfBuilt();
        _circuitBreakerConfig = circuitBreakerConfig ?? throw new ArgumentNullException(nameof(circuitBreakerConfig));
        return this;
    }

    /// <summary>
    /// Sets the retry configuration
    /// </summary>
    /// <param name="retryConfig">Retry configuration</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentNullException">Thrown when retryConfig is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithRetry(RetryConfig retryConfig)
    {
        ThrowIfBuilt();
        _retryConfig = retryConfig ?? throw new ArgumentNullException(nameof(retryConfig));
        return this;
    }

    /// <summary>
    /// Sets the degradation configuration
    /// </summary>
    /// <param name="degradationConfig">Degradation configuration</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentNullException">Thrown when degradationConfig is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithDegradation(DegradationConfig degradationConfig)
    {
        ThrowIfBuilt();
        _degradationConfig = degradationConfig ?? throw new ArgumentNullException(nameof(degradationConfig));
        return this;
    }

    /// <summary>
    /// Sets the performance configuration
    /// </summary>
    /// <param name="performanceConfig">Performance configuration</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentNullException">Thrown when performanceConfig is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithPerformance(PerformanceConfig performanceConfig)
    {
        ThrowIfBuilt();
        _performanceConfig = performanceConfig ?? throw new ArgumentNullException(nameof(performanceConfig));
        return this;
    }

    #endregion

    #region Monitoring Configuration Methods

    /// <summary>
    /// Sets whether alerting is enabled
    /// </summary>
    /// <param name="enabled">Whether alerting is enabled</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithAlerting(bool enabled)
    {
        ThrowIfBuilt();
        _enableAlerting = enabled;
        return this;
    }

    /// <summary>
    /// Sets whether profiling is enabled
    /// </summary>
    /// <param name="enabled">Whether profiling is enabled</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithProfiling(bool enabled)
    {
        ThrowIfBuilt();
        _enableProfiling = enabled;
        return this;
    }

    /// <summary>
    /// Sets whether detailed logging is enabled
    /// </summary>
    /// <param name="enabled">Whether detailed logging is enabled</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithDetailedLogging(bool enabled)
    {
        ThrowIfBuilt();
        _enableDetailedLogging = enabled;
        return this;
    }

    /// <summary>
    /// Sets the log level
    /// </summary>
    /// <param name="logLevel">Log level for health check operations</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithLogLevel(LogLevel logLevel)
    {
        ThrowIfBuilt();
        _logLevel = logLevel;
        return this;
    }

    /// <summary>
    /// Sets alert severities for different health statuses
    /// </summary>
    /// <param name="alertSeverities">Dictionary mapping health status to alert severity</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentNullException">Thrown when alertSeverities is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithAlertSeverities(Dictionary<HealthStatus, AlertSeverity> alertSeverities)
    {
        ThrowIfBuilt();
        _alertSeverities = alertSeverities ?? throw new ArgumentNullException(nameof(alertSeverities));
        return this;
    }

    /// <summary>
    /// Sets whether this health check is critical
    /// </summary>
    /// <param name="isCritical">Whether this health check is critical</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder WithCritical(bool isCritical)
    {
        ThrowIfBuilt();
        _isCritical = isCritical;
        return this;
    }

    #endregion

    #region Preset Methods

    /// <summary>
    /// Configures the builder for a critical system health check
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder ForCriticalSystem()
    {
        ThrowIfBuilt();
        
        _category = HealthCheckCategory.System;
        _interval = TimeSpan.FromSeconds(15);
        _timeout = TimeSpan.FromSeconds(10);
        _priority = 1000;
        _circuitBreakerConfig = CircuitBreakerConfig.ForCriticalService();
        _retryConfig = new RetryConfig { MaxRetries = 1, RetryDelay = TimeSpan.FromSeconds(1) };
        _degradationConfig = DegradationConfig.ForCriticalSystem();
        _performanceConfig = PerformanceConfig.ForHighPerformanceGames();
        _enableAlerting = true;
        _enableProfiling = true;
        _enableDetailedLogging = true;
        _logLevel = LogLevel.Warning;
        _alertSeverities = GetCriticalAlertSeverities();
        _isCritical = true;
        
        return this;
    }

    /// <summary>
    /// Configures the builder for a database health check
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder ForDatabase()
    {
        ThrowIfBuilt();
        
        _category = HealthCheckCategory.Database;
        _interval = TimeSpan.FromSeconds(30);
        _timeout = TimeSpan.FromSeconds(15);
        _priority = 800;
        _circuitBreakerConfig = CircuitBreakerConfig.ForDatabase();
        _retryConfig = new RetryConfig { MaxRetries = 2, RetryDelay = TimeSpan.FromSeconds(1), BackoffMultiplier = 2.0 };
        _performanceConfig = PerformanceConfig.ForProduction();
        
        return this;
    }

    /// <summary>
    /// Configures the builder for a network health check
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder ForNetwork()
    {
        ThrowIfBuilt();
        
        _category = HealthCheckCategory.Network;
        _interval = TimeSpan.FromSeconds(45);
        _timeout = TimeSpan.FromSeconds(20);
        _priority = 600;
        _circuitBreakerConfig = CircuitBreakerConfig.ForNetworkService();
        _retryConfig = new RetryConfig { MaxRetries = 3, RetryDelay = TimeSpan.FromSeconds(2), BackoffMultiplier = 1.5 };
        _performanceConfig = PerformanceConfig.ForProduction();
        
        return this;
    }

    /// <summary>
    /// Configures the builder for development environment
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckConfigBuilder ForDevelopment()
    {
        ThrowIfBuilt();
        
        _interval = TimeSpan.FromSeconds(10);
        _timeout = TimeSpan.FromSeconds(5);
        _priority = 100;
        _circuitBreakerConfig = CircuitBreakerConfig.ForDevelopment();
        _degradationConfig = DegradationConfig.ForDevelopment();
        _performanceConfig = PerformanceConfig.ForDevelopment();
        _enableAlerting = false;
        _enableProfiling = false;
        _enableDetailedLogging = true;
        _logLevel = LogLevel.Debug;
        _isCritical = false;
        
        return this;
    }

    #endregion

    #region Build Methods

    /// <summary>
    /// Validates the current configuration and returns any validation errors
    /// </summary>
    /// <returns>List of validation error messages, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // Core validation
        if (string.IsNullOrEmpty(_name))
            errors.Add("Name is required");

        if (_interval <= TimeSpan.Zero)
            errors.Add("Interval must be greater than zero");

        if (_timeout <= TimeSpan.Zero)
            errors.Add("Timeout must be greater than zero");

        if (_timeout >= _interval)
            errors.Add("Timeout should be less than execution interval");

        // Unity game development specific validations
        if (_timeout > TimeSpan.FromSeconds(30))
            errors.Add("Timeout should not exceed 30 seconds for game performance");

        if (_priority < 0)
            errors.Add("Priority must be non-negative");

        // Validate nested configurations
        if (_circuitBreakerConfig != null)
            errors.AddRange(_circuitBreakerConfig.Validate());

        if (_retryConfig != null)
            errors.AddRange(_retryConfig.Validate());

        if (_degradationConfig != null)
            errors.AddRange(_degradationConfig.Validate());

        if (_performanceConfig != null)
            errors.AddRange(_performanceConfig.Validate());

        return errors;
    }

    /// <summary>
    /// Validates the configuration and throws exception if invalid
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    public void ValidateAndThrow()
    {
        var errors = Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Invalid health check configuration: {string.Join(", ", errors)}");
        }
    }

    /// <summary>
    /// Builds the final HealthCheckConfiguration instance
    /// </summary>
    /// <returns>New HealthCheckConfiguration instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid or builder has already been built</exception>
    public HealthCheckConfiguration Build()
    {
        ThrowIfBuilt();
        
        ValidateAndThrow();

        var config = new HealthCheckConfiguration
        {
            Id = new FixedString64Bytes(DeterministicIdGenerator.GenerateHealthCheckId("HealthCheck", _name).ToString("N")[..16]),
            Name = _name,
            DisplayName = string.IsNullOrEmpty(_displayName) ? _name : _displayName,
            Category = _category,
            Enabled = _enabled,
            Interval = _interval,
            Timeout = _timeout,
            Priority = _priority,
            CircuitBreaker = _circuitBreakerConfig,
            Retry = _retryConfig,
            Degradation = _degradationConfig,
            Performance = _performanceConfig,
            EnableAlerting = _enableAlerting,
            EnableProfiling = _enableProfiling,
            EnableDetailedLogging = _enableDetailedLogging,
            LogLevel = _logLevel,
            AlertSeverities = _alertSeverities,
            IsCritical = _isCritical
        };

        _isBuilt = true;
        _logger.LogDebug("HealthCheckConfiguration built successfully for: {Name}", _name);

        return config;
    }

    #endregion

    #region Helper Methods

    private void ThrowIfBuilt()
    {
        if (_isBuilt)
        {
            throw new InvalidOperationException("Builder has already been built and cannot be modified");
        }
    }

    private static Dictionary<HealthStatus, AlertSeverity> GetDefaultAlertSeverities()
    {
        return new Dictionary<HealthStatus, AlertSeverity>
        {
            { HealthStatus.Healthy, AlertSeverity.Info },
            { HealthStatus.Warning, AlertSeverity.Warning },
            { HealthStatus.Degraded, AlertSeverity.Warning },
            { HealthStatus.Unhealthy, AlertSeverity.Critical },
            { HealthStatus.Critical, AlertSeverity.Critical },
            { HealthStatus.Offline, AlertSeverity.Emergency },
            { HealthStatus.Unknown, AlertSeverity.Warning }
        };
    }

    private static Dictionary<HealthStatus, AlertSeverity> GetCriticalAlertSeverities()
    {
        return new Dictionary<HealthStatus, AlertSeverity>
        {
            { HealthStatus.Healthy, AlertSeverity.Info },
            { HealthStatus.Warning, AlertSeverity.Critical },
            { HealthStatus.Degraded, AlertSeverity.Critical },
            { HealthStatus.Unhealthy, AlertSeverity.Emergency },
            { HealthStatus.Critical, AlertSeverity.Emergency },
            { HealthStatus.Offline, AlertSeverity.Emergency },
            { HealthStatus.Unknown, AlertSeverity.Critical }
        };
    }

    #endregion
}