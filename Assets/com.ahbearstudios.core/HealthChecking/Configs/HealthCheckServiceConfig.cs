using System.Collections.Generic;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Simplified configuration for the HealthCheck Service focused on game development.
/// Designed for Unity with performance-first approach and 60+ FPS targets.
/// Follows CLAUDE.md patterns with static factory methods and no field initializers.
/// </summary>
public sealed record HealthCheckServiceConfig : IHealthCheckServiceConfig
{
    #region Core Health Check Settings

    /// <summary>
    /// Default interval between automatic health checks
    /// </summary>
    public TimeSpan AutomaticCheckInterval { get; init; }

    /// <summary>
    /// Maximum number of concurrent health checks that can run simultaneously
    /// </summary>
    public int MaxConcurrentHealthChecks { get; init; }

    /// <summary>
    /// Default timeout for health checks if not specified
    /// </summary>
    public TimeSpan DefaultTimeout { get; init; }

    /// <summary>
    /// Whether to enable automatic health checks on startup
    /// </summary>
    public bool EnableAutomaticChecks { get; init; }

    /// <summary>
    /// Maximum number of health check results to keep in history per check
    /// </summary>
    public int MaxHistorySize { get; init; }

    /// <summary>
    /// Maximum number of retries for failed health checks
    /// </summary>
    public int MaxRetries { get; init; }

    /// <summary>
    /// Delay between health check retries
    /// </summary>
    public TimeSpan RetryDelay { get; init; }

    #endregion

    #region Circuit Breaker Settings

    /// <summary>
    /// Whether to enable circuit breaker functionality for health checks
    /// </summary>
    public bool EnableCircuitBreaker { get; init; }

    /// <summary>
    /// Default circuit breaker configuration for health checks
    /// </summary>
    public CircuitBreakerConfig DefaultCircuitBreakerConfig { get; init; }

    /// <summary>
    /// Gets the default circuit breaker configuration as interface for compatibility
    /// </summary>
    ICircuitBreakerConfig IHealthCheckServiceConfig.DefaultCircuitBreakerConfig => DefaultCircuitBreakerConfig;

    /// <summary>
    /// Whether to enable alerts when circuit breakers trigger
    /// </summary>
    public bool EnableCircuitBreakerAlerts { get; init; }

    /// <summary>
    /// Default failure threshold for circuit breakers
    /// </summary>
    public int DefaultFailureThreshold { get; init; }

    /// <summary>
    /// Default circuit breaker timeout duration
    /// </summary>
    public TimeSpan DefaultCircuitBreakerTimeout { get; init; }

    #endregion

    #region Degradation Settings

    /// <summary>
    /// Whether to enable graceful degradation based on health check results
    /// </summary>
    public bool EnableGracefulDegradation { get; init; }

    /// <summary>
    /// Configuration for degradation behavior
    /// </summary>
    public DegradationConfig DegradationConfig { get; init; }

    /// <summary>
    /// Whether to raise alerts for degradation level changes
    /// </summary>
    public bool EnableDegradationAlerts { get; init; }

    /// <summary>
    /// Degradation thresholds for automatic system degradation
    /// </summary>
    public DegradationThresholds DegradationThresholds { get; init; }

    /// <summary>
    /// Whether to automatically adjust degradation levels based on health status
    /// </summary>
    public bool EnableAutomaticDegradation { get; init; }

    #endregion

    #region Performance Settings

    /// <summary>
    /// Performance configuration for health checks
    /// </summary>
    public PerformanceConfig PerformanceConfig { get; init; }

    /// <summary>
    /// Whether to enable performance monitoring
    /// </summary>
    public bool EnablePerformanceMonitoring { get; init; }

    #endregion

    #region Alert Settings

    /// <summary>
    /// Whether to raise alerts for health status changes
    /// </summary>
    public bool EnableHealthAlerts { get; init; }

    /// <summary>
    /// Custom alert severities for different health status levels
    /// </summary>
    public Dictionary<HealthStatus, AlertSeverity> AlertSeverities { get; init; }

    /// <summary>
    /// Tags to apply to health check alerts for filtering and routing
    /// </summary>
    public HashSet<FixedString64Bytes> AlertTags { get; init; }

    /// <summary>
    /// Number of consecutive failures before triggering an alert
    /// </summary>
    public int AlertFailureThreshold { get; init; }

    #endregion

    #region Logging Settings

    /// <summary>
    /// Whether to enable logging for health check operations
    /// </summary>
    public bool EnableHealthCheckLogging { get; init; }

    /// <summary>
    /// Log level for health check operations
    /// </summary>
    public LogLevel HealthCheckLogLevel { get; init; }

    /// <summary>
    /// Whether to enable detailed logging (performance impact)
    /// </summary>
    public bool EnableDetailedLogging { get; init; }

    #endregion

    #region Profiling Settings

    /// <summary>
    /// Whether to enable profiling for health check operations
    /// </summary>
    public bool EnableProfiling { get; init; }

    /// <summary>
    /// Threshold in milliseconds for considering a health check slow
    /// </summary>
    public int SlowHealthCheckThreshold { get; init; }

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a health check service configuration with proper validation and defaults
    /// </summary>
    /// <returns>New HealthCheckServiceConfig instance</returns>
    public static HealthCheckServiceConfig Create()
    {
        return new HealthCheckServiceConfig
        {
            AutomaticCheckInterval = TimeSpan.FromSeconds(30),
            MaxConcurrentHealthChecks = 10,
            DefaultTimeout = TimeSpan.FromSeconds(30),
            EnableAutomaticChecks = true,
            MaxHistorySize = 100,
            MaxRetries = 3,
            RetryDelay = TimeSpan.FromSeconds(1),
            EnableCircuitBreaker = true,
            DefaultCircuitBreakerConfig = CircuitBreakerConfig.Create("DefaultHealthCheckCircuitBreaker"),
            EnableCircuitBreakerAlerts = true,
            DefaultFailureThreshold = 5,
            DefaultCircuitBreakerTimeout = TimeSpan.FromSeconds(60),
            EnableGracefulDegradation = true,
            DegradationConfig = new DegradationConfig(),
            EnableDegradationAlerts = true,
            DegradationThresholds = new DegradationThresholds(),
            EnableAutomaticDegradation = true,
            PerformanceConfig = PerformanceConfig.ForProduction(),
            EnablePerformanceMonitoring = true,
            EnableHealthAlerts = true,
            AlertSeverities = GetDefaultAlertSeverities(),
            AlertTags = GetDefaultAlertTags(),
            AlertFailureThreshold = 3,
            EnableHealthCheckLogging = true,
            HealthCheckLogLevel = LogLevel.Info,
            EnableDetailedLogging = false,
            EnableProfiling = true,
            SlowHealthCheckThreshold = 1000
        };
    }

    /// <summary>
    /// Creates a configuration optimized for high-performance games (60+ FPS)
    /// </summary>
    /// <returns>High-performance game configuration</returns>
    public static HealthCheckServiceConfig ForHighPerformanceGames()
    {
        return new HealthCheckServiceConfig
        {
            AutomaticCheckInterval = TimeSpan.FromSeconds(10),
            MaxConcurrentHealthChecks = 3,
            DefaultTimeout = TimeSpan.FromSeconds(5),
            EnableAutomaticChecks = true,
            MaxHistorySize = 50,
            MaxRetries = 1,
            RetryDelay = TimeSpan.FromMilliseconds(100),
            EnableCircuitBreaker = true,
            DefaultCircuitBreakerConfig = CircuitBreakerConfig.ForCriticalService(),
            EnableCircuitBreakerAlerts = true,
            DefaultFailureThreshold = 3,
            DefaultCircuitBreakerTimeout = TimeSpan.FromSeconds(30),
            EnableGracefulDegradation = true,
            DegradationConfig = DegradationConfig.ForCriticalSystem(),
            EnableDegradationAlerts = true,
            DegradationThresholds = DegradationThresholds.ForHighAvailability(),
            EnableAutomaticDegradation = true,
            PerformanceConfig = PerformanceConfig.ForHighPerformanceGames(),
            EnablePerformanceMonitoring = true,
            EnableHealthAlerts = true,
            AlertSeverities = GetCriticalAlertSeverities(),
            AlertTags = GetDefaultAlertTags(),
            AlertFailureThreshold = 1,
            EnableHealthCheckLogging = true,
            HealthCheckLogLevel = LogLevel.Warning,
            EnableDetailedLogging = false,
            EnableProfiling = true,
            SlowHealthCheckThreshold = 10
        };
    }

    /// <summary>
    /// Creates a configuration optimized for production environments
    /// </summary>
    /// <returns>Production configuration</returns>
    public static HealthCheckServiceConfig ForProduction()
    {
        return Create(); // Same as default for now
    }

    /// <summary>
    /// Creates a minimal configuration for development environments
    /// </summary>
    /// <returns>Development configuration</returns>
    public static HealthCheckServiceConfig ForDevelopment()
    {
        return new HealthCheckServiceConfig
        {
            AutomaticCheckInterval = TimeSpan.FromSeconds(60),
            MaxConcurrentHealthChecks = 20,
            DefaultTimeout = TimeSpan.FromMinutes(1),
            EnableAutomaticChecks = true,
            MaxHistorySize = 200,
            MaxRetries = 1,
            RetryDelay = TimeSpan.FromSeconds(2),
            EnableCircuitBreaker = false,
            DefaultCircuitBreakerConfig = CircuitBreakerConfig.ForDevelopment(),
            EnableCircuitBreakerAlerts = false,
            DefaultFailureThreshold = 2,
            DefaultCircuitBreakerTimeout = TimeSpan.FromSeconds(10),
            EnableGracefulDegradation = false,
            DegradationConfig = DegradationConfig.ForDevelopment(),
            EnableDegradationAlerts = false,
            DegradationThresholds = DegradationThresholds.ForDevelopment(),
            EnableAutomaticDegradation = false,
            PerformanceConfig = PerformanceConfig.ForDevelopment(),
            EnablePerformanceMonitoring = false,
            EnableHealthAlerts = false,
            AlertSeverities = GetDefaultAlertSeverities(),
            AlertTags = GetDefaultAlertTags(),
            AlertFailureThreshold = 10,
            EnableHealthCheckLogging = true,
            HealthCheckLogLevel = LogLevel.Debug,
            EnableDetailedLogging = true,
            EnableProfiling = false,
            SlowHealthCheckThreshold = 5000
        };
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validates this configuration and returns any validation errors
    /// </summary>
    /// <returns>List of validation error messages, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // Core validation
        if (AutomaticCheckInterval <= TimeSpan.Zero)
            errors.Add("AutomaticCheckInterval must be greater than zero");

        if (MaxConcurrentHealthChecks < 1)
            errors.Add("MaxConcurrentHealthChecks must be at least 1");

        if (DefaultTimeout <= TimeSpan.Zero)
            errors.Add("DefaultTimeout must be greater than zero");

        if (MaxHistorySize < 10)
            errors.Add("MaxHistorySize must be at least 10");

        if (MaxRetries < 0)
            errors.Add("MaxRetries must be non-negative");

        if (RetryDelay < TimeSpan.Zero)
            errors.Add("RetryDelay must be non-negative");

        if (AlertFailureThreshold < 1)
            errors.Add("AlertFailureThreshold must be at least 1");

        if (SlowHealthCheckThreshold < 1)
            errors.Add("SlowHealthCheckThreshold must be at least 1ms");

        if (DefaultFailureThreshold < 1)
            errors.Add("DefaultFailureThreshold must be at least 1");

        if (DefaultCircuitBreakerTimeout <= TimeSpan.Zero)
            errors.Add("DefaultCircuitBreakerTimeout must be greater than zero");

        // Unity game development specific validations
        if (DefaultTimeout > TimeSpan.FromSeconds(30))
            errors.Add("DefaultTimeout should not exceed 30 seconds for game performance");

        if (AutomaticCheckInterval < TimeSpan.FromSeconds(1))
            errors.Add("AutomaticCheckInterval should be at least 1 second for performance");

        // Validate nested configurations
        if (DefaultCircuitBreakerConfig != null)
            errors.AddRange(DefaultCircuitBreakerConfig.Validate());

        if (DegradationConfig != null)
            errors.AddRange(DegradationConfig.Validate());

        if (PerformanceConfig != null)
            errors.AddRange(PerformanceConfig.Validate());

        if (DegradationThresholds != null)
            errors.AddRange(DegradationThresholds.Validate());

        return errors;
    }

    /// <summary>
    /// Validates configuration and throws exception if invalid
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown when configuration is invalid</exception>
    public void ValidateAndThrow()
    {
        var errors = Validate();
        if (errors.Count > 0)
        {
            throw new System.InvalidOperationException($"Invalid HealthCheckServiceConfig: {string.Join(", ", errors)}");
        }
    }

    #endregion

    #region Helper Methods

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

    private static HashSet<FixedString64Bytes> GetDefaultAlertTags()
    {
        return new HashSet<FixedString64Bytes> { "HealthCheck", "SystemMonitoring" };
    }

    #endregion
}