using System.Collections.Generic;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Comprehensive configuration for the HealthCheck Service with circuit breaker integration,
    /// graceful degradation, and comprehensive monitoring capabilities
    /// </summary>
    public sealed record HealthCheckServiceConfig_Backup : IHealthCheckServiceConfig
    {
        #region Core Health Check Settings

        /// <summary>
        /// Default interval between automatic health checks
        /// </summary>
        public TimeSpan AutomaticCheckInterval { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Maximum number of concurrent health checks that can run simultaneously
        /// </summary>
        [Range(1, 100)]
        public int MaxConcurrentHealthChecks { get; init; } = 10;

        /// <summary>
        /// Default timeout for health checks if not specified
        /// </summary>
        [Range(1, 300)]
        public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Whether to enable automatic health checks on startup
        /// </summary>
        public bool EnableAutomaticChecks { get; init; } = true;

        /// <summary>
        /// Maximum number of health check results to keep in history per check
        /// </summary>
        [Range(10, 10000)]
        public int MaxHistorySize { get; init; } = 100;

        /// <summary>
        /// Maximum number of retries for failed health checks
        /// </summary>
        [Range(0, 10)]
        public int MaxRetries { get; init; } = 3;

        /// <summary>
        /// Delay between retry attempts for failed health checks
        /// </summary>
        [Range(1, 60)]
        public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);

        #endregion

        #region Circuit Breaker Settings

        /// <summary>
        /// Whether to enable circuit breaker functionality
        /// </summary>
        public bool EnableCircuitBreaker { get; init; } = true;

        /// <summary>
        /// Default circuit breaker configuration for health checks
        /// </summary>
        public ICircuitBreakerConfig DefaultCircuitBreakerConfig { get; init; } = new CircuitBreakerConfig();

        /// <summary>
        /// Whether to raise alerts for circuit breaker state changes
        /// </summary>
        public bool EnableCircuitBreakerAlerts { get; init; } = true;

        /// <summary>
        /// Default failure threshold for circuit breakers
        /// </summary>
        [Range(1, 100)]
        public int DefaultFailureThreshold { get; init; } = 5;

        /// <summary>
        /// Default circuit breaker timeout duration
        /// </summary>
        [Range(1, 600)]
        public TimeSpan DefaultCircuitBreakerTimeout { get; init; } = TimeSpan.FromSeconds(30);

        #endregion

        #region Graceful Degradation Settings

        /// <summary>
        /// Whether to enable graceful degradation features
        /// </summary>
        public bool EnableGracefulDegradation { get; init; } = true;

        /// <summary>
        /// Degradation thresholds for automatic system degradation
        /// </summary>
        public DegradationThresholds DegradationThresholds { get; init; } = new();

        /// <summary>
        /// Whether to raise alerts for degradation level changes
        /// </summary>
        public bool EnableDegradationAlerts { get; init; } = true;

        /// <summary>
        /// Whether to automatically adjust degradation levels based on health status
        /// </summary>
        public bool EnableAutomaticDegradation { get; init; } = true;

        #endregion

        #region Alert and Notification Settings

        /// <summary>
        /// Whether to raise alerts for health status changes
        /// </summary>
        public bool EnableHealthAlerts { get; init; } = true;

        /// <summary>
        /// Custom alert severities for different health status levels
        /// </summary>
        public Dictionary<HealthStatus, AlertSeverity> AlertSeverities { get; init; } = new()
        {
            { HealthStatus.Healthy, AlertSeverity.Info },
            { HealthStatus.Warning, AlertSeverity.Warning },
            { HealthStatus.Degraded, AlertSeverity.Warning },
            { HealthStatus.Unhealthy, AlertSeverity.Critical },
            { HealthStatus.Critical, AlertSeverity.Critical },
            { HealthStatus.Offline, AlertSeverity.Emergency }, 
            { HealthStatus.Unknown, AlertSeverity.Warning }
        };

        /// <summary>
        /// Tags to apply to health check alerts for filtering and routing
        /// </summary>
        public HashSet<FixedString64Bytes> AlertTags { get; init; } = new()
        {
            "HealthCheck",
            "SystemMonitoring"
        };

        /// <summary>
        /// Number of consecutive failures before triggering an alert
        /// </summary>
        [Range(1, 10)]
        public int AlertFailureThreshold { get; init; } = 3;

        #endregion

        #region Logging and Profiling Settings

        /// <summary>
        /// Whether to log health check executions for debugging
        /// </summary>
        public bool EnableHealthCheckLogging { get; init; } = true;

        /// <summary>
        /// Log level for health check operations
        /// </summary>
        public LogLevel HealthCheckLogLevel { get; init; } = LogLevel.Info;

        /// <summary>
        /// Whether to enable performance profiling for health checks
        /// </summary>
        public bool EnableProfiling { get; init; } = true;

        /// <summary>
        /// Performance threshold for logging slow health checks (in milliseconds)
        /// </summary>
        [Range(100, 60000)]
        public int SlowHealthCheckThreshold { get; init; } = 1000;

        /// <summary>
        /// Whether to log detailed health check statistics
        /// </summary>
        public bool EnableDetailedLogging { get; init; } = false;

        #endregion

        #region Performance and Resource Settings

        /// <summary>
        /// Maximum memory usage for health check history (in MB)
        /// </summary>
        [Range(10, 1000)]
        public int MaxMemoryUsageMB { get; init; } = 50;

        /// <summary>
        /// How often to clean up expired health check history
        /// </summary>
        [Range(1, 1440)]
        public TimeSpan HistoryCleanupInterval { get; init; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Maximum age of health check results to keep in history
        /// </summary>
        [Range(1, 168)]
        public TimeSpan MaxHistoryAge { get; init; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Thread priority for health check execution
        /// </summary>
        public System.Threading.ThreadPriority HealthCheckThreadPriority { get; init; } = System.Threading.ThreadPriority.Normal;

        #endregion

        #region Health Status Thresholds

        /// <summary>
        /// Thresholds for determining overall system health status
        /// </summary>
        public HealthThresholds HealthThresholds { get; init; } = new();

        /// <summary>
        /// Percentage of unhealthy checks that triggers overall unhealthy status
        /// </summary>
        [Range(0.1f, 1.0f)]
        public double UnhealthyThreshold { get; init; } = 0.25;

        /// <summary>
        /// Percentage of warning checks that triggers overall warning status
        /// </summary>
        [Range(0.1f, 1.0f)]
        public double WarningThreshold { get; init; } = 0.5;

        #endregion

        #region Advanced Settings

        /// <summary>
        /// Whether to enable health check dependency validation
        /// </summary>
        public bool EnableDependencyValidation { get; init; } = true;

        /// <summary>
        /// Whether to enable health check result caching
        /// </summary>
        public bool EnableResultCaching { get; init; } = true;

        /// <summary>
        /// Duration to cache health check results
        /// </summary>
        [Range(1, 300)]
        public TimeSpan ResultCacheDuration { get; init; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Whether to enable health check execution timeouts
        /// </summary>
        public bool EnableExecutionTimeouts { get; init; } = true;

        /// <summary>
        /// Whether to enable health check correlation IDs for tracing
        /// </summary>
        public bool EnableCorrelationIds { get; init; } = true;

        /// <summary>
        /// Custom metadata to include with all health check results
        /// </summary>
        public Dictionary<string, object> DefaultMetadata { get; init; } = new();

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates the configuration and returns any validation errors
        /// </summary>
        /// <returns>List of validation error messages, empty if valid</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            // Core settings validation
            if (AutomaticCheckInterval <= TimeSpan.Zero)
                errors.Add("AutomaticCheckInterval must be greater than zero");

            if (MaxConcurrentHealthChecks <= 0)
                errors.Add("MaxConcurrentHealthChecks must be greater than zero");

            if (DefaultTimeout <= TimeSpan.Zero)
                errors.Add("DefaultTimeout must be greater than zero");

            if (MaxHistorySize < 0)
                errors.Add("MaxHistorySize must be non-negative");

            if (SlowHealthCheckThreshold < 0)
                errors.Add("SlowHealthCheckThreshold must be non-negative");

            if (MaxRetries < 0)
                errors.Add("MaxRetries must be non-negative");

            if (RetryDelay <= TimeSpan.Zero)
                errors.Add("RetryDelay must be greater than zero");

            // Circuit breaker validation
            if (EnableCircuitBreaker)
            {
                if (DefaultFailureThreshold <= 0)
                    errors.Add("DefaultFailureThreshold must be greater than zero");

                if (DefaultCircuitBreakerTimeout <= TimeSpan.Zero)
                    errors.Add("DefaultCircuitBreakerTimeout must be greater than zero");

                errors.AddRange(DefaultCircuitBreakerConfig.Validate());
            }

            // Degradation validation
            if (EnableGracefulDegradation)
            {
                errors.AddRange(DegradationThresholds.Validate());
            }

            // Performance validation
            if (MaxMemoryUsageMB <= 0)
                errors.Add("MaxMemoryUsageMB must be greater than zero");

            if (HistoryCleanupInterval <= TimeSpan.Zero)
                errors.Add("HistoryCleanupInterval must be greater than zero");

            if (MaxHistoryAge <= TimeSpan.Zero)
                errors.Add("MaxHistoryAge must be greater than zero");

            // Threshold validation
            if (UnhealthyThreshold < 0.0 || UnhealthyThreshold > 1.0)
                errors.Add("UnhealthyThreshold must be between 0.0 and 1.0");

            if (WarningThreshold < 0.0 || WarningThreshold > 1.0)
                errors.Add("WarningThreshold must be between 0.0 and 1.0");

            if (ResultCacheDuration <= TimeSpan.Zero)
                errors.Add("ResultCacheDuration must be greater than zero");

            errors.AddRange(HealthThresholds.Validate());

            return errors;
        }

        /// <summary>
        /// Validates configuration and throws exception if invalid
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        public void ValidateAndThrow()
        {
            var errors = Validate();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException($"Invalid HealthCheckServiceConfig: {string.Join(", ", errors)}");
            }
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a configuration optimized for development environments
        /// </summary>
        /// <returns>Development-optimized configuration</returns>
        public static HealthCheckServiceConfig ForDevelopment()
        {
            return new HealthCheckServiceConfig
            {
                AutomaticCheckInterval = TimeSpan.FromSeconds(10),
                MaxConcurrentHealthChecks = 5,
                DefaultTimeout = TimeSpan.FromSeconds(10),
                EnableAutomaticChecks = true,
                EnableCircuitBreaker = true,
                EnableGracefulDegradation = false,
                MaxHistorySize = 50,
                MaxRetries = 1,
                RetryDelay = TimeSpan.FromSeconds(1),
                EnableHealthCheckLogging = true,
                HealthCheckLogLevel = LogLevel.Debug,
                EnableProfiling = true,
                SlowHealthCheckThreshold = 500,
                EnableDetailedLogging = true,
                EnableResultCaching = false,
                ResultCacheDuration = TimeSpan.FromSeconds(5),
                MaxMemoryUsageMB = 20,
                HistoryCleanupInterval = TimeSpan.FromMinutes(10),
                MaxHistoryAge = TimeSpan.FromHours(2)
            };
        }

        /// <summary>
        /// Creates a configuration optimized for production environments
        /// </summary>
        /// <returns>Production-optimized configuration</returns>
        public static HealthCheckServiceConfig ForProduction()
        {
            return new HealthCheckServiceConfig
            {
                AutomaticCheckInterval = TimeSpan.FromMinutes(1),
                MaxConcurrentHealthChecks = 20,
                DefaultTimeout = TimeSpan.FromSeconds(30),
                EnableAutomaticChecks = true,
                EnableCircuitBreaker = true,
                EnableGracefulDegradation = true,
                MaxHistorySize = 500,
                MaxRetries = 3,
                RetryDelay = TimeSpan.FromSeconds(2),
                EnableHealthCheckLogging = true,
                HealthCheckLogLevel = LogLevel.Info,
                EnableProfiling = true,
                SlowHealthCheckThreshold = 2000,
                EnableDetailedLogging = false,
                EnableResultCaching = true,
                ResultCacheDuration = TimeSpan.FromSeconds(30),
                MaxMemoryUsageMB = 100,
                HistoryCleanupInterval = TimeSpan.FromMinutes(30),
                MaxHistoryAge = TimeSpan.FromHours(24),
                EnableDependencyValidation = true,
                EnableExecutionTimeouts = true,
                EnableCorrelationIds = true
            };
        }

        /// <summary>
        /// Creates a configuration optimized for testing environments
        /// </summary>
        /// <returns>Test-optimized configuration</returns>
        public static HealthCheckServiceConfig ForTesting()
        {
            return new HealthCheckServiceConfig
            {
                AutomaticCheckInterval = TimeSpan.FromSeconds(1),
                MaxConcurrentHealthChecks = 1,
                DefaultTimeout = TimeSpan.FromSeconds(5),
                EnableAutomaticChecks = false,
                EnableCircuitBreaker = false,
                EnableGracefulDegradation = false,
                MaxHistorySize = 10,
                MaxRetries = 0,
                RetryDelay = TimeSpan.FromMilliseconds(100),
                EnableHealthAlerts = false,
                EnableCircuitBreakerAlerts = false,
                EnableDegradationAlerts = false,
                EnableHealthCheckLogging = false,
                EnableProfiling = false,
                EnableDetailedLogging = false,
                EnableResultCaching = false,
                ResultCacheDuration = TimeSpan.FromSeconds(1),
                MaxMemoryUsageMB = 5,
                HistoryCleanupInterval = TimeSpan.FromMinutes(1),
                MaxHistoryAge = TimeSpan.FromMinutes(10),
                EnableDependencyValidation = false,
                EnableExecutionTimeouts = false,
                EnableCorrelationIds = false
            };
        }

        /// <summary>
        /// Creates a minimal configuration with only essential features enabled
        /// </summary>
        /// <returns>Minimal configuration</returns>
        public static HealthCheckServiceConfig Minimal()
        {
            return new HealthCheckServiceConfig
            {
                AutomaticCheckInterval = TimeSpan.FromMinutes(5),
                MaxConcurrentHealthChecks = 3,
                DefaultTimeout = TimeSpan.FromSeconds(15),
                EnableAutomaticChecks = true,
                EnableCircuitBreaker = false,
                EnableGracefulDegradation = false,
                MaxHistorySize = 20,
                MaxRetries = 1,
                RetryDelay = TimeSpan.FromSeconds(1),
                EnableHealthAlerts = false,
                EnableCircuitBreakerAlerts = false,
                EnableDegradationAlerts = false,
                EnableHealthCheckLogging = false,
                EnableProfiling = false,
                EnableDetailedLogging = false,
                EnableResultCaching = false,
                MaxMemoryUsageMB = 10,
                HistoryCleanupInterval = TimeSpan.FromHours(1),
                MaxHistoryAge = TimeSpan.FromHours(1),
                EnableDependencyValidation = false,
                EnableExecutionTimeouts = true,
                EnableCorrelationIds = false
            };
        }

        #endregion
    }
}