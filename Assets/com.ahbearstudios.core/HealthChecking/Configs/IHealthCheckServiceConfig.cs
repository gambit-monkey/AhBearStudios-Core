using System.Collections.Generic;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Interface for HealthCheck Service configuration with circuit breaker integration,
    /// graceful degradation, and comprehensive monitoring capabilities
    /// </summary>
    public interface IHealthCheckServiceConfig
    {
        #region Core Health Check Settings
        
        /// <summary>
        /// Default interval between automatic health checks
        /// </summary>
        TimeSpan AutomaticCheckInterval { get; }

        /// <summary>
        /// Maximum number of concurrent health checks that can run simultaneously
        /// </summary>
        int MaxConcurrentHealthChecks { get; }

        /// <summary>
        /// Default timeout for health checks if not specified
        /// </summary>
        TimeSpan DefaultTimeout { get; }

        /// <summary>
        /// Whether to enable automatic health checks on startup
        /// </summary>
        bool EnableAutomaticChecks { get; }

        /// <summary>
        /// Maximum number of health check results to keep in history per check
        /// </summary>
        int MaxHistorySize { get; }

        /// <summary>
        /// Maximum number of retries for failed health checks
        /// </summary>
        int MaxRetries { get; }

        /// <summary>
        /// Delay between retry attempts for failed health checks
        /// </summary>
        TimeSpan RetryDelay { get; }

        #endregion

        #region Circuit Breaker Settings

        /// <summary>
        /// Whether to enable circuit breaker functionality
        /// </summary>
        bool EnableCircuitBreaker { get; }

        /// <summary>
        /// Default circuit breaker configuration for health checks
        /// </summary>
        ICircuitBreakerConfig DefaultCircuitBreakerConfig { get; }

        /// <summary>
        /// Whether to raise alerts for circuit breaker state changes
        /// </summary>
        bool EnableCircuitBreakerAlerts { get; }

        /// <summary>
        /// Default failure threshold for circuit breakers
        /// </summary>
        int DefaultFailureThreshold { get; }

        /// <summary>
        /// Default circuit breaker timeout duration
        /// </summary>
        TimeSpan DefaultCircuitBreakerTimeout { get; }

        #endregion

        #region Graceful Degradation Settings

        /// <summary>
        /// Whether to enable graceful degradation features
        /// </summary>
        bool EnableGracefulDegradation { get; }

        /// <summary>
        /// Degradation thresholds for automatic system degradation
        /// </summary>
        DegradationThresholds DegradationThresholds { get; }

        /// <summary>
        /// Whether to raise alerts for degradation level changes
        /// </summary>
        bool EnableDegradationAlerts { get; }

        /// <summary>
        /// Whether to automatically adjust degradation levels based on health status
        /// </summary>
        bool EnableAutomaticDegradation { get; }

        #endregion

        #region Alert and Notification Settings

        /// <summary>
        /// Whether to raise alerts for health status changes
        /// </summary>
        bool EnableHealthAlerts { get; }

        /// <summary>
        /// Custom alert severities for different health status levels
        /// </summary>
        Dictionary<HealthStatus, AlertSeverity> AlertSeverities { get; }

        /// <summary>
        /// Tags to apply to health check alerts for filtering and routing
        /// </summary>
        HashSet<FixedString64Bytes> AlertTags { get; }

        /// <summary>
        /// Number of consecutive failures before triggering an alert
        /// </summary>
        int AlertFailureThreshold { get; }

        #endregion

        #region Logging and Profiling Settings

        /// <summary>
        /// Whether to log health check executions for debugging
        /// </summary>
        bool EnableHealthCheckLogging { get; }

        /// <summary>
        /// Log level for health check operations
        /// </summary>
        LogLevel HealthCheckLogLevel { get; }

        /// <summary>
        /// Whether to enable performance profiling for health checks
        /// </summary>
        bool EnableProfiling { get; }

        /// <summary>
        /// Performance threshold for logging slow health checks (in milliseconds)
        /// </summary>
        int SlowHealthCheckThreshold { get; }

        /// <summary>
        /// Whether to log detailed health check statistics
        /// </summary>
        bool EnableDetailedLogging { get; }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates the configuration and returns any validation errors
        /// </summary>
        /// <returns>List of validation error messages, empty if valid</returns>
        List<string> Validate();

        /// <summary>
        /// Validates configuration and throws exception if invalid
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        void ValidateAndThrow();

        #endregion
    }
}