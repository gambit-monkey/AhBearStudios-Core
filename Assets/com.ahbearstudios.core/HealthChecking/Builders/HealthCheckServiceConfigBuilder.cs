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
/// Simplified builder for HealthCheckServiceConfig aligned with CLAUDE.md patterns.
/// Focuses on essential health check service configuration for Unity game development.
/// Uses static factory methods and avoids field initializers.
/// </summary>
public sealed class HealthCheckServiceConfigBuilder : IHealthCheckServiceConfigBuilder
{
    private readonly ILoggingService _logger;
    private bool _isBuilt;

    #region Core Health Check Settings
    
    private TimeSpan _automaticCheckInterval;
    private int _maxConcurrentHealthChecks;
    private TimeSpan _defaultTimeout;
    private bool _enableAutomaticChecks;
    private int _maxHistorySize;
    private int _maxRetries;
    private TimeSpan _retryDelay;

    #endregion

    #region Circuit Breaker Settings
    
    private bool _enableCircuitBreaker;
    private CircuitBreakerConfig _defaultCircuitBreakerConfig;
    private bool _enableCircuitBreakerAlerts;

    #endregion

    #region Degradation Settings
    
    private bool _enableGracefulDegradation;
    private DegradationConfig _degradationConfig;
    private bool _enableDegradationAlerts;

    #endregion

    #region Performance Settings
    
    private PerformanceConfig _performanceConfig;
    private bool _enablePerformanceMonitoring;

    #endregion

    #region Alert Settings
    
    private bool _enableHealthAlerts;
    private Dictionary<HealthStatus, AlertSeverity> _alertSeverities;
    private HashSet<FixedString64Bytes> _alertTags;
    private int _alertFailureThreshold;

    #endregion

    #region Logging Settings
    
    private bool _enableHealthCheckLogging;
    private LogLevel _healthCheckLogLevel;
    private bool _enableDetailedLogging;

    #endregion

    #region Profiling Settings
    
    private bool _enableProfiling;
    private int _slowHealthCheckThreshold;

    #endregion

    /// <summary>
    /// Initializes a new instance of the HealthCheckServiceConfigBuilder class
    /// </summary>
    /// <param name="logger">Logging service for build operations</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
    public HealthCheckServiceConfigBuilder(ILoggingService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Initialize with sensible defaults
        InitializeDefaults();
        
        _logger.LogDebug("HealthCheckServiceConfigBuilder initialized");
    }

    /// <summary>
    /// Creates a builder from an existing configuration
    /// </summary>
    /// <param name="existingConfig">Existing configuration to copy</param>
    /// <param name="logger">Logging service for build operations</param>
    /// <returns>New builder with copied settings</returns>
    /// <exception cref="ArgumentNullException">Thrown when existingConfig or logger is null</exception>
    public static HealthCheckServiceConfigBuilder FromExisting(HealthCheckServiceConfig existingConfig, ILoggingService logger)
    {
        if (existingConfig == null)
            throw new ArgumentNullException(nameof(existingConfig));
        if (logger == null)
            throw new ArgumentNullException(nameof(logger));

        var builder = new HealthCheckServiceConfigBuilder(logger);
        
        // Copy all settings from existing config
        builder._automaticCheckInterval = existingConfig.AutomaticCheckInterval;
        builder._maxConcurrentHealthChecks = existingConfig.MaxConcurrentHealthChecks;
        builder._defaultTimeout = existingConfig.DefaultTimeout;
        builder._enableAutomaticChecks = existingConfig.EnableAutomaticChecks;
        builder._maxHistorySize = existingConfig.MaxHistorySize;
        builder._maxRetries = existingConfig.MaxRetries;
        builder._retryDelay = existingConfig.RetryDelay;
        builder._enableCircuitBreaker = existingConfig.EnableCircuitBreaker;
        builder._defaultCircuitBreakerConfig = existingConfig.DefaultCircuitBreakerConfig;
        builder._enableCircuitBreakerAlerts = existingConfig.EnableCircuitBreakerAlerts;
        builder._enableGracefulDegradation = existingConfig.EnableGracefulDegradation;
        builder._degradationConfig = existingConfig.DegradationConfig;
        builder._enableDegradationAlerts = existingConfig.EnableDegradationAlerts;
        builder._performanceConfig = existingConfig.PerformanceConfig;
        builder._enablePerformanceMonitoring = existingConfig.EnablePerformanceMonitoring;
        builder._enableHealthAlerts = existingConfig.EnableHealthAlerts;
        builder._alertSeverities = new Dictionary<HealthStatus, AlertSeverity>(existingConfig.AlertSeverities);
        builder._alertTags = new HashSet<FixedString64Bytes>(existingConfig.AlertTags);
        builder._alertFailureThreshold = existingConfig.AlertFailureThreshold;
        builder._enableHealthCheckLogging = existingConfig.EnableHealthCheckLogging;
        builder._healthCheckLogLevel = existingConfig.HealthCheckLogLevel;
        builder._enableDetailedLogging = existingConfig.EnableDetailedLogging;
        builder._enableProfiling = existingConfig.EnableProfiling;
        builder._slowHealthCheckThreshold = existingConfig.SlowHealthCheckThreshold;

        logger.LogDebug("HealthCheckServiceConfigBuilder created from existing configuration");
        return builder;
    }

    #region Core Configuration Methods

    /// <summary>
    /// Sets the automatic check interval
    /// </summary>
    /// <param name="interval">Interval between automatic health checks</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithAutomaticCheckInterval(TimeSpan interval)
    {
        ThrowIfBuilt();
        
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than zero");
        
        if (interval > TimeSpan.FromMinutes(30))
            _logger.LogWarning("Long automatic check interval: {Interval}. Consider if this is appropriate for your use case.", interval);

        _automaticCheckInterval = interval;
        return this;
    }

    /// <summary>
    /// Sets the maximum concurrent health checks
    /// </summary>
    /// <param name="maxConcurrent">Maximum number of concurrent health checks</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxConcurrent is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithMaxConcurrentHealthChecks(int maxConcurrent)
    {
        ThrowIfBuilt();
        
        if (maxConcurrent < 1)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrent), "Max concurrent health checks must be at least 1");
        
        if (maxConcurrent > 100)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrent), "Max concurrent health checks should not exceed 100");

        _maxConcurrentHealthChecks = maxConcurrent;
        return this;
    }

    /// <summary>
    /// Sets the default timeout for health checks
    /// </summary>
    /// <param name="timeout">Default timeout</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithDefaultTimeout(TimeSpan timeout)
    {
        ThrowIfBuilt();
        
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than zero");
        
        if (timeout > TimeSpan.FromSeconds(60))
            _logger.LogWarning("Long default timeout: {Timeout}. This may affect game performance.", timeout);

        _defaultTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets whether automatic checks are enabled
    /// </summary>
    /// <param name="enabled">Whether to enable automatic checks</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithAutomaticChecks(bool enabled)
    {
        ThrowIfBuilt();
        _enableAutomaticChecks = enabled;
        return this;
    }

    /// <summary>
    /// Sets the maximum history size
    /// </summary>
    /// <param name="maxHistorySize">Maximum number of history entries per health check</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxHistorySize is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithMaxHistorySize(int maxHistorySize)
    {
        ThrowIfBuilt();
        
        if (maxHistorySize < 10)
            throw new ArgumentOutOfRangeException(nameof(maxHistorySize), "Max history size must be at least 10");
        
        if (maxHistorySize > 10000)
            throw new ArgumentOutOfRangeException(nameof(maxHistorySize), "Max history size should not exceed 10000 for memory efficiency");

        _maxHistorySize = maxHistorySize;
        return this;
    }

    /// <summary>
    /// Sets the maximum retries and retry delay
    /// </summary>
    /// <param name="maxRetries">Maximum number of retries</param>
    /// <param name="retryDelay">Delay between retries</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithRetrySettings(int maxRetries, TimeSpan retryDelay)
    {
        ThrowIfBuilt();
        
        if (maxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetries), "Max retries must be non-negative");
        
        if (maxRetries > 10)
            throw new ArgumentOutOfRangeException(nameof(maxRetries), "Max retries should not exceed 10");
        
        if (retryDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(retryDelay), "Retry delay must be non-negative");

        _maxRetries = maxRetries;
        _retryDelay = retryDelay;
        return this;
    }

    #endregion

    #region Circuit Breaker Configuration Methods

    /// <summary>
    /// Sets circuit breaker configuration
    /// </summary>
    /// <param name="enabled">Whether circuit breaker is enabled</param>
    /// <param name="config">Default circuit breaker configuration</param>
    /// <param name="enableAlerts">Whether to enable circuit breaker alerts</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithCircuitBreaker(bool enabled = true, CircuitBreakerConfig config = null, bool enableAlerts = true)
    {
        ThrowIfBuilt();
        
        _enableCircuitBreaker = enabled;
        _defaultCircuitBreakerConfig = config ?? CircuitBreakerConfig.Create("DefaultHealthCheckCircuitBreaker");
        _enableCircuitBreakerAlerts = enableAlerts;
        
        return this;
    }

    #endregion

    #region Degradation Configuration Methods

    /// <summary>
    /// Sets degradation configuration
    /// </summary>
    /// <param name="enabled">Whether degradation is enabled</param>
    /// <param name="config">Degradation configuration</param>
    /// <param name="enableAlerts">Whether to enable degradation alerts</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithDegradation(bool enabled = true, DegradationConfig config = null, bool enableAlerts = true)
    {
        ThrowIfBuilt();
        
        _enableGracefulDegradation = enabled;
        _degradationConfig = config ?? new DegradationConfig();
        _enableDegradationAlerts = enableAlerts;
        
        return this;
    }

    #endregion

    #region Performance Configuration Methods

    /// <summary>
    /// Sets performance configuration
    /// </summary>
    /// <param name="config">Performance configuration</param>
    /// <param name="enableMonitoring">Whether to enable performance monitoring</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithPerformance(PerformanceConfig config = null, bool enableMonitoring = true)
    {
        ThrowIfBuilt();
        
        _performanceConfig = config ?? PerformanceConfig.ForProduction();
        _enablePerformanceMonitoring = enableMonitoring;
        
        return this;
    }

    #endregion

    #region Alert Configuration Methods

    /// <summary>
    /// Sets alert configuration
    /// </summary>
    /// <param name="enabled">Whether alerts are enabled</param>
    /// <param name="severities">Alert severities for different health statuses</param>
    /// <param name="tags">Alert tags</param>
    /// <param name="failureThreshold">Alert failure threshold</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithAlerts(
        bool enabled = true,
        Dictionary<HealthStatus, AlertSeverity> severities = null,
        HashSet<FixedString64Bytes> tags = null,
        int failureThreshold = 3)
    {
        ThrowIfBuilt();
        
        _enableHealthAlerts = enabled;
        _alertSeverities = severities ?? GetDefaultAlertSeverities();
        _alertTags = tags ?? GetDefaultAlertTags();
        _alertFailureThreshold = Math.Max(1, failureThreshold);
        
        return this;
    }

    #endregion

    #region Logging Configuration Methods

    /// <summary>
    /// Sets logging configuration
    /// </summary>
    /// <param name="enabled">Whether logging is enabled</param>
    /// <param name="logLevel">Log level for health checks</param>
    /// <param name="enableDetailedLogging">Whether to enable detailed logging</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithLogging(bool enabled = true, LogLevel logLevel = LogLevel.Info, bool enableDetailedLogging = false)
    {
        ThrowIfBuilt();
        
        _enableHealthCheckLogging = enabled;
        _healthCheckLogLevel = logLevel;
        _enableDetailedLogging = enableDetailedLogging;
        
        return this;
    }

    #endregion

    #region Profiling Configuration Methods

    /// <summary>
    /// Sets profiling configuration
    /// </summary>
    /// <param name="enabled">Whether profiling is enabled</param>
    /// <param name="slowThreshold">Threshold in milliseconds for considering a health check slow</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when slowThreshold is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithProfiling(bool enabled = true, int slowThreshold = 1000)
    {
        ThrowIfBuilt();
        
        if (slowThreshold < 1)
            throw new ArgumentOutOfRangeException(nameof(slowThreshold), "Slow threshold must be at least 1ms");

        _enableProfiling = enabled;
        _slowHealthCheckThreshold = slowThreshold;
        
        return this;
    }

    #endregion

    #region Preset Methods

    /// <summary>
    /// Configures for high-performance games (60+ FPS)
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder ForHighPerformanceGames()
    {
        ThrowIfBuilt();
        
        _automaticCheckInterval = TimeSpan.FromSeconds(10);
        _maxConcurrentHealthChecks = 3;
        _defaultTimeout = TimeSpan.FromSeconds(5);
        _enableAutomaticChecks = true;
        _maxHistorySize = 50;
        _maxRetries = 1;
        _retryDelay = TimeSpan.FromMilliseconds(100);
        
        _enableCircuitBreaker = true;
        _defaultCircuitBreakerConfig = CircuitBreakerConfig.ForCriticalService();
        _enableCircuitBreakerAlerts = true;
        
        _enableGracefulDegradation = true;
        _degradationConfig = DegradationConfig.ForCriticalSystem();
        _enableDegradationAlerts = true;
        
        _performanceConfig = PerformanceConfig.ForHighPerformanceGames();
        _enablePerformanceMonitoring = true;
        
        _enableHealthAlerts = true;
        _alertSeverities = GetDefaultAlertSeverities();
        _alertTags = GetDefaultAlertTags();
        _alertFailureThreshold = 1;
        
        _enableHealthCheckLogging = true;
        _healthCheckLogLevel = LogLevel.Warning;
        _enableDetailedLogging = false;
        
        _enableProfiling = true;
        _slowHealthCheckThreshold = 10;
        
        _logger.LogDebug("Applied high-performance games preset");
        return this;
    }

    /// <summary>
    /// Configures for production environments
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder ForProduction()
    {
        ThrowIfBuilt();
        
        _automaticCheckInterval = TimeSpan.FromSeconds(30);
        _maxConcurrentHealthChecks = 10;
        _defaultTimeout = TimeSpan.FromSeconds(30);
        _enableAutomaticChecks = true;
        _maxHistorySize = 100;
        _maxRetries = 3;
        _retryDelay = TimeSpan.FromSeconds(1);
        
        _enableCircuitBreaker = true;
        _defaultCircuitBreakerConfig = CircuitBreakerConfig.Create("ProductionHealthCheckCircuitBreaker");
        _enableCircuitBreakerAlerts = true;
        
        _enableGracefulDegradation = true;
        _degradationConfig = new DegradationConfig();
        _enableDegradationAlerts = true;
        
        _performanceConfig = PerformanceConfig.ForProduction();
        _enablePerformanceMonitoring = true;
        
        _enableHealthAlerts = true;
        _alertSeverities = GetDefaultAlertSeverities();
        _alertTags = GetDefaultAlertTags();
        _alertFailureThreshold = 3;
        
        _enableHealthCheckLogging = true;
        _healthCheckLogLevel = LogLevel.Info;
        _enableDetailedLogging = false;
        
        _enableProfiling = true;
        _slowHealthCheckThreshold = 1000;
        
        _logger.LogDebug("Applied production preset");
        return this;
    }

    /// <summary>
    /// Configures for development environments
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder ForDevelopment()
    {
        ThrowIfBuilt();
        
        _automaticCheckInterval = TimeSpan.FromSeconds(60);
        _maxConcurrentHealthChecks = 20;
        _defaultTimeout = TimeSpan.FromMinutes(1);
        _enableAutomaticChecks = true;
        _maxHistorySize = 200;
        _maxRetries = 1;
        _retryDelay = TimeSpan.FromSeconds(2);
        
        _enableCircuitBreaker = false;
        _defaultCircuitBreakerConfig = CircuitBreakerConfig.ForDevelopment();
        _enableCircuitBreakerAlerts = false;
        
        _enableGracefulDegradation = false;
        _degradationConfig = DegradationConfig.ForDevelopment();
        _enableDegradationAlerts = false;
        
        _performanceConfig = PerformanceConfig.ForDevelopment();
        _enablePerformanceMonitoring = false;
        
        _enableHealthAlerts = false;
        _alertSeverities = GetDefaultAlertSeverities();
        _alertTags = GetDefaultAlertTags();
        _alertFailureThreshold = 10;
        
        _enableHealthCheckLogging = true;
        _healthCheckLogLevel = LogLevel.Debug;
        _enableDetailedLogging = true;
        
        _enableProfiling = false;
        _slowHealthCheckThreshold = 5000;
        
        _logger.LogDebug("Applied development preset");
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
        if (_automaticCheckInterval <= TimeSpan.Zero)
            errors.Add("Automatic check interval must be greater than zero");

        if (_maxConcurrentHealthChecks < 1)
            errors.Add("Max concurrent health checks must be at least 1");

        if (_defaultTimeout <= TimeSpan.Zero)
            errors.Add("Default timeout must be greater than zero");

        if (_maxHistorySize < 10)
            errors.Add("Max history size must be at least 10");

        if (_maxRetries < 0)
            errors.Add("Max retries must be non-negative");

        if (_retryDelay < TimeSpan.Zero)
            errors.Add("Retry delay must be non-negative");

        if (_alertFailureThreshold < 1)
            errors.Add("Alert failure threshold must be at least 1");

        if (_slowHealthCheckThreshold < 1)
            errors.Add("Slow health check threshold must be at least 1ms");

        // Unity game development specific validations
        if (_defaultTimeout > TimeSpan.FromSeconds(30))
            errors.Add("Default timeout should not exceed 30 seconds for game performance");

        // Validate nested configurations
        if (_defaultCircuitBreakerConfig != null)
            errors.AddRange(_defaultCircuitBreakerConfig.Validate());

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
            throw new InvalidOperationException($"Invalid health check service configuration: {string.Join(", ", errors)}");
        }
    }

    /// <summary>
    /// Builds the final HealthCheckServiceConfig instance
    /// </summary>
    /// <returns>New HealthCheckServiceConfig instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid or builder has already been built</exception>
    public HealthCheckServiceConfig Build()
    {
        ThrowIfBuilt();
        
        ValidateAndThrow();

        var config = new HealthCheckServiceConfig
        {
            AutomaticCheckInterval = _automaticCheckInterval,
            MaxConcurrentHealthChecks = _maxConcurrentHealthChecks,
            DefaultTimeout = _defaultTimeout,
            EnableAutomaticChecks = _enableAutomaticChecks,
            MaxHistorySize = _maxHistorySize,
            MaxRetries = _maxRetries,
            RetryDelay = _retryDelay,
            EnableCircuitBreaker = _enableCircuitBreaker,
            DefaultCircuitBreakerConfig = _defaultCircuitBreakerConfig,
            EnableCircuitBreakerAlerts = _enableCircuitBreakerAlerts,
            EnableGracefulDegradation = _enableGracefulDegradation,
            DegradationConfig = _degradationConfig,
            EnableDegradationAlerts = _enableDegradationAlerts,
            PerformanceConfig = _performanceConfig,
            EnablePerformanceMonitoring = _enablePerformanceMonitoring,
            EnableHealthAlerts = _enableHealthAlerts,
            AlertSeverities = _alertSeverities,
            AlertTags = _alertTags,
            AlertFailureThreshold = _alertFailureThreshold,
            EnableHealthCheckLogging = _enableHealthCheckLogging,
            HealthCheckLogLevel = _healthCheckLogLevel,
            EnableDetailedLogging = _enableDetailedLogging,
            EnableProfiling = _enableProfiling,
            SlowHealthCheckThreshold = _slowHealthCheckThreshold
        };

        _isBuilt = true;
        _logger.LogDebug("HealthCheckServiceConfig built successfully");

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

    private void InitializeDefaults()
    {
        _automaticCheckInterval = TimeSpan.FromSeconds(30);
        _maxConcurrentHealthChecks = 10;
        _defaultTimeout = TimeSpan.FromSeconds(30);
        _enableAutomaticChecks = true;
        _maxHistorySize = 100;
        _maxRetries = 3;
        _retryDelay = TimeSpan.FromSeconds(1);
        
        _enableCircuitBreaker = true;
        _defaultCircuitBreakerConfig = CircuitBreakerConfig.Create("DefaultHealthCheckCircuitBreaker");
        _enableCircuitBreakerAlerts = true;
        
        _enableGracefulDegradation = true;
        _degradationConfig = new DegradationConfig();
        _enableDegradationAlerts = true;
        
        _performanceConfig = PerformanceConfig.ForProduction();
        _enablePerformanceMonitoring = true;
        
        _enableHealthAlerts = true;
        _alertSeverities = GetDefaultAlertSeverities();
        _alertTags = GetDefaultAlertTags();
        _alertFailureThreshold = 3;
        
        _enableHealthCheckLogging = true;
        _healthCheckLogLevel = LogLevel.Info;
        _enableDetailedLogging = false;
        
        _enableProfiling = true;
        _slowHealthCheckThreshold = 1000;
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

    private static HashSet<FixedString64Bytes> GetDefaultAlertTags()
    {
        return new HashSet<FixedString64Bytes> { "HealthCheck", "SystemMonitoring" };
    }

    #endregion
}