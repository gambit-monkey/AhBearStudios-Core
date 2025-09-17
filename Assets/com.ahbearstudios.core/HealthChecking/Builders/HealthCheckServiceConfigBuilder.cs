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

    #region Additional Settings (for interface compliance)

    private Dictionary<string, object> _defaultMetadata;
    private System.Threading.ThreadPriority _threadPriority;
    private HealthThresholds _healthThresholds;
    private double _unhealthyThreshold;
    private double _warningThreshold;
    private bool _enableDependencyValidation;
    private bool _enableResultCaching;
    private TimeSpan _cacheDuration;
    private bool _enableExecutionTimeouts;
    private bool _enableCorrelationIds;
    private int _maxMemoryUsageMB;
    private TimeSpan _historyCleanupInterval;
    private TimeSpan _maxHistoryAge;
    private int _defaultFailureThreshold;
    private TimeSpan _defaultCircuitBreakerTimeout;
    private bool _enableAutomaticDegradation;

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

        // Copy additional settings (initialize with defaults if not available in existing config)
        builder._defaultMetadata = new Dictionary<string, object>();
        builder._threadPriority = System.Threading.ThreadPriority.Normal;
        builder._healthThresholds = HealthThresholds.CreateDefault();
        builder._unhealthyThreshold = 0.8;
        builder._warningThreshold = 0.6;
        builder._enableDependencyValidation = true;
        builder._enableResultCaching = true;
        builder._cacheDuration = TimeSpan.FromMinutes(5);
        builder._enableExecutionTimeouts = true;
        builder._enableCorrelationIds = true;
        builder._maxMemoryUsageMB = 100;
        builder._historyCleanupInterval = TimeSpan.FromHours(1);
        builder._maxHistoryAge = TimeSpan.FromDays(7);
        builder._defaultFailureThreshold = 5;
        builder._defaultCircuitBreakerTimeout = TimeSpan.FromSeconds(30);
        builder._enableAutomaticDegradation = true;

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
    /// Configures circuit breaker functionality (simple overload)
    /// </summary>
    /// <param name="enabled">Whether to enable circuit breakers</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithCircuitBreaker(bool enabled)
    {
        return WithCircuitBreaker(enabled, null, true);
    }

    /// <summary>
    /// Sets circuit breaker configuration
    /// </summary>
    /// <param name="enabled">Whether circuit breaker is enabled</param>
    /// <param name="config">Default circuit breaker configuration</param>
    /// <param name="enableAlerts">Whether to enable circuit breaker alerts</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithCircuitBreaker(bool enabled, CircuitBreakerConfig config, bool enableAlerts)
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
    /// Configures health check logging
    /// </summary>
    /// <param name="enabled">Whether to enable health check logging</param>
    /// <param name="logLevel">Log level for health check operations</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithHealthCheckLogging(bool enabled = true, LogLevel logLevel = LogLevel.Info)
    {
        ThrowIfBuilt();

        _enableHealthCheckLogging = enabled;
        _healthCheckLogLevel = logLevel;

        return this;
    }

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

    #region Missing Interface Methods

    /// <summary>
    /// Adds alert tags to existing tags
    /// </summary>
    /// <param name="tags">Alert tags to add</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder AddAlertTags(params FixedString64Bytes[] tags)
    {
        ThrowIfBuilt();

        if (tags != null)
        {
            foreach (var tag in tags)
            {
                _alertTags.Add(tag);
            }
        }

        return this;
    }

    /// <summary>
    /// Adds a single metadata entry
    /// </summary>
    /// <param name="key">Metadata key</param>
    /// <param name="value">Metadata value</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentException">Thrown when key is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder AddMetadata(string key, object value)
    {
        ThrowIfBuilt();

        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Metadata key cannot be null or empty", nameof(key));

        _defaultMetadata[key] = value;
        return this;
    }

    /// <summary>
    /// Applies development environment preset
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder ApplyDevelopmentPreset()
    {
        // Delegate to existing ForDevelopment method
        return ForDevelopment();
    }

    /// <summary>
    /// Applies production environment preset
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder ApplyProductionPreset()
    {
        // Delegate to existing ForProduction method
        return ForProduction();
    }

    /// <summary>
    /// Applies staging environment preset
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder ApplyStagingPreset()
    {
        ThrowIfBuilt();

        // Staging preset: balanced between development and production
        _automaticCheckInterval = TimeSpan.FromSeconds(45);
        _maxConcurrentHealthChecks = 15;
        _defaultTimeout = TimeSpan.FromSeconds(45);
        _enableAutomaticChecks = true;
        _maxHistorySize = 150;
        _maxRetries = 2;
        _retryDelay = TimeSpan.FromSeconds(1.5);

        _enableCircuitBreaker = true;
        _defaultCircuitBreakerConfig = CircuitBreakerConfig.Create("StagingHealthCheckCircuitBreaker");
        _enableCircuitBreakerAlerts = true;

        _enableGracefulDegradation = true;
        _degradationConfig = new DegradationConfig();
        _enableDegradationAlerts = true;

        _performanceConfig = PerformanceConfig.ForProduction();
        _enablePerformanceMonitoring = true;

        _enableHealthAlerts = true;
        _alertSeverities = GetDefaultAlertSeverities();
        _alertTags = GetDefaultAlertTags();
        _alertFailureThreshold = 2;

        _enableHealthCheckLogging = true;
        _healthCheckLogLevel = LogLevel.Info;
        _enableDetailedLogging = true;

        _enableProfiling = true;
        _slowHealthCheckThreshold = 500;

        _logger.LogDebug("Applied staging preset");
        return this;
    }

    /// <summary>
    /// Applies testing environment preset
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder ApplyTestingPreset()
    {
        ThrowIfBuilt();

        // Testing preset: strict validation, detailed logging
        _automaticCheckInterval = TimeSpan.FromSeconds(30);
        _maxConcurrentHealthChecks = 5;
        _defaultTimeout = TimeSpan.FromMinutes(2);
        _enableAutomaticChecks = true;
        _maxHistorySize = 1000;
        _maxRetries = 0; // No retries in testing
        _retryDelay = TimeSpan.Zero;

        _enableCircuitBreaker = false; // Disabled for testing
        _defaultCircuitBreakerConfig = CircuitBreakerConfig.ForDevelopment();
        _enableCircuitBreakerAlerts = false;

        _enableGracefulDegradation = false; // Disabled for testing
        _degradationConfig = DegradationConfig.ForDevelopment();
        _enableDegradationAlerts = false;

        _performanceConfig = PerformanceConfig.ForDevelopment();
        _enablePerformanceMonitoring = true;

        _enableHealthAlerts = false; // Disabled for testing
        _alertSeverities = GetDefaultAlertSeverities();
        _alertTags = GetDefaultAlertTags();
        _alertFailureThreshold = 1;

        _enableHealthCheckLogging = true;
        _healthCheckLogLevel = LogLevel.Debug;
        _enableDetailedLogging = true;

        _enableProfiling = true;
        _slowHealthCheckThreshold = 100;

        _healthThresholds = HealthThresholds.CreateRelaxed();

        _logger.LogDebug("Applied testing preset");
        return this;
    }

    /// <summary>
    /// Configures the builder for a specific environment
    /// </summary>
    /// <param name="environment">Target environment</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder ForEnvironment(HealthCheckEnvironment environment)
    {
        ThrowIfBuilt();

        return environment switch
        {
            HealthCheckEnvironment.Development => ApplyDevelopmentPreset(),
            HealthCheckEnvironment.Testing => ApplyTestingPreset(),
            HealthCheckEnvironment.Staging => ApplyStagingPreset(),
            HealthCheckEnvironment.Production => ApplyProductionPreset(),
            _ => throw new ArgumentException($"Unknown environment: {environment}", nameof(environment))
        };
    }

    /// <summary>
    /// Resets the builder to its initial state
    /// </summary>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder Reset()
    {
        ThrowIfBuilt();

        InitializeDefaults();
        _logger.LogDebug("Builder reset to initial state");

        return this;
    }

    /// <summary>
    /// Sets the alert failure threshold
    /// </summary>
    /// <param name="threshold">Number of consecutive failures before triggering alert</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithAlertFailureThreshold(int threshold)
    {
        ThrowIfBuilt();

        if (threshold < 1)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Alert failure threshold must be at least 1");

        _alertFailureThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Sets alert tags for health check alerts
    /// </summary>
    /// <param name="tags">Alert tags to set</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithAlertTags(params FixedString64Bytes[] tags)
    {
        ThrowIfBuilt();

        _alertTags.Clear();
        if (tags != null)
        {
            foreach (var tag in tags)
            {
                _alertTags.Add(tag);
            }
        }

        return this;
    }

    /// <summary>
    /// Configures automatic degradation
    /// </summary>
    /// <param name="enabled">Whether to enable automatic degradation</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithAutomaticDegradation(bool enabled = true)
    {
        ThrowIfBuilt();

        _enableAutomaticDegradation = enabled;
        return this;
    }



    /// <summary>
    /// Configures correlation IDs
    /// </summary>
    /// <param name="enabled">Whether to enable correlation IDs</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithCorrelationIds(bool enabled = true)
    {
        ThrowIfBuilt();

        _enableCorrelationIds = enabled;
        return this;
    }

    /// <summary>
    /// Configures circuit breaker alerts
    /// </summary>
    /// <param name="enabled">Whether to enable circuit breaker alerts</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithCircuitBreakerAlerts(bool enabled = true)
    {
        ThrowIfBuilt();

        _enableCircuitBreakerAlerts = enabled;
        return this;
    }

    /// <summary>
    /// Sets the default circuit breaker configuration
    /// </summary>
    /// <param name="config">Circuit breaker configuration</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithDefaultCircuitBreakerConfig(CircuitBreakerConfig config)
    {
        ThrowIfBuilt();

        _defaultCircuitBreakerConfig = config ?? throw new ArgumentNullException(nameof(config));
        return this;
    }

    /// <summary>
    /// Sets the default circuit breaker timeout
    /// </summary>
    /// <param name="timeout">Circuit breaker timeout</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithDefaultCircuitBreakerTimeout(TimeSpan timeout)
    {
        ThrowIfBuilt();

        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Circuit breaker timeout must be greater than zero");

        _defaultCircuitBreakerTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets the default failure threshold for circuit breakers
    /// </summary>
    /// <param name="threshold">Failure threshold</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithDefaultFailureThreshold(int threshold)
    {
        ThrowIfBuilt();

        if (threshold < 1)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Default failure threshold must be at least 1");

        _defaultFailureThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Sets default metadata for all health check results
    /// </summary>
    /// <param name="metadata">Default metadata dictionary</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithDefaultMetadata(Dictionary<string, object> metadata)
    {
        ThrowIfBuilt();

        _defaultMetadata = metadata ?? new Dictionary<string, object>();
        return this;
    }

    /// <summary>
    /// Configures degradation alerts
    /// </summary>
    /// <param name="enabled">Whether to enable degradation alerts</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithDegradationAlerts(bool enabled = true)
    {
        ThrowIfBuilt();

        _enableDegradationAlerts = enabled;
        return this;
    }

    /// <summary>
    /// Configures dependency validation
    /// </summary>
    /// <param name="enabled">Whether to enable dependency validation</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithDependencyValidation(bool enabled = true)
    {
        ThrowIfBuilt();

        _enableDependencyValidation = enabled;
        return this;
    }

    /// <summary>
    /// Configures detailed logging
    /// </summary>
    /// <param name="enabled">Whether to enable detailed logging</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithDetailedLogging(bool enabled = true)
    {
        ThrowIfBuilt();

        _enableDetailedLogging = enabled;
        return this;
    }

    /// <summary>
    /// Configures execution timeouts
    /// </summary>
    /// <param name="enabled">Whether to enable execution timeouts</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithExecutionTimeouts(bool enabled = true)
    {
        ThrowIfBuilt();

        _enableExecutionTimeouts = enabled;
        return this;
    }

    /// <summary>
    /// Configures graceful degradation functionality
    /// </summary>
    /// <param name="enabled">Whether to enable graceful degradation</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithGracefulDegradation(bool enabled = true)
    {
        ThrowIfBuilt();

        _enableGracefulDegradation = enabled;
        return this;
    }

    /// <summary>
    /// Configures health alerts (alternative signature)
    /// </summary>
    /// <param name="enabled">Whether to enable health alerts</param>
    /// <param name="severities">Custom alert severities for different health statuses</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithHealthAlerts(bool enabled = true, Dictionary<HealthStatus, AlertSeverity> severities = null)
    {
        ThrowIfBuilt();

        _enableHealthAlerts = enabled;
        if (severities != null)
        {
            _alertSeverities = severities;
        }

        return this;
    }


    /// <summary>
    /// Sets health thresholds
    /// </summary>
    /// <param name="thresholds">Health thresholds configuration</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentNullException">Thrown when thresholds is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithHealthThresholds(HealthThresholds thresholds)
    {
        ThrowIfBuilt();

        _healthThresholds = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
        return this;
    }

    /// <summary>
    /// Configures memory and cleanup settings
    /// </summary>
    /// <param name="maxMemoryUsageMB">Maximum memory usage in MB</param>
    /// <param name="historyCleanupInterval">How often to clean up history</param>
    /// <param name="maxHistoryAge">Maximum age of history to keep</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithMemorySettings(int maxMemoryUsageMB, TimeSpan historyCleanupInterval, TimeSpan maxHistoryAge)
    {
        ThrowIfBuilt();

        if (maxMemoryUsageMB < 1)
            throw new ArgumentOutOfRangeException(nameof(maxMemoryUsageMB), "Max memory usage must be at least 1 MB");

        if (historyCleanupInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(historyCleanupInterval), "History cleanup interval must be greater than zero");

        if (maxHistoryAge <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maxHistoryAge), "Max history age must be greater than zero");

        _maxMemoryUsageMB = maxMemoryUsageMB;
        _historyCleanupInterval = historyCleanupInterval;
        _maxHistoryAge = maxHistoryAge;

        return this;
    }

    /// <summary>
    /// Configures result caching
    /// </summary>
    /// <param name="enabled">Whether to enable result caching</param>
    /// <param name="cacheDuration">How long to cache results</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when cacheDuration is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithResultCaching(bool enabled, TimeSpan cacheDuration)
    {
        ThrowIfBuilt();

        if (enabled && cacheDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(cacheDuration), "Cache duration must be greater than zero when caching is enabled");

        _enableResultCaching = enabled;
        _cacheDuration = cacheDuration;

        return this;
    }

    /// <summary>
    /// Sets the thread priority for health check execution
    /// </summary>
    /// <param name="priority">Thread priority</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithThreadPriority(System.Threading.ThreadPriority priority)
    {
        ThrowIfBuilt();

        _threadPriority = priority;
        return this;
    }

    /// <summary>
    /// Sets the unhealthy threshold
    /// </summary>
    /// <param name="threshold">Percentage of unhealthy checks that triggers overall unhealthy status</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithUnhealthyThreshold(double threshold)
    {
        ThrowIfBuilt();

        if (threshold < 0.0 || threshold > 1.0)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Unhealthy threshold must be between 0.0 and 1.0");

        _unhealthyThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Sets the warning threshold
    /// </summary>
    /// <param name="threshold">Percentage of warning checks that triggers overall warning status</param>
    /// <returns>Builder instance for fluent API</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public IHealthCheckServiceConfigBuilder WithWarningThreshold(double threshold)
    {
        ThrowIfBuilt();

        if (threshold < 0.0 || threshold > 1.0)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Warning threshold must be between 0.0 and 1.0");

        _warningThreshold = threshold;
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

        // Additional settings defaults
        _defaultMetadata = new Dictionary<string, object>();
        _threadPriority = System.Threading.ThreadPriority.Normal;
        _healthThresholds = HealthThresholds.CreateDefault();
        _unhealthyThreshold = 0.8;
        _warningThreshold = 0.6;
        _enableDependencyValidation = true;
        _enableResultCaching = true;
        _cacheDuration = TimeSpan.FromMinutes(5);
        _enableExecutionTimeouts = true;
        _enableCorrelationIds = true;
        _maxMemoryUsageMB = 100;
        _historyCleanupInterval = TimeSpan.FromHours(1);
        _maxHistoryAge = TimeSpan.FromDays(7);
        _defaultFailureThreshold = 5;
        _defaultCircuitBreakerTimeout = TimeSpan.FromSeconds(30);
        _enableAutomaticDegradation = true;
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