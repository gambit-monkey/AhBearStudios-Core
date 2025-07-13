using System.Collections.Generic;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthCheck.Models;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.HealthCheck.Configs
{
    /// <summary>
    /// Configuration for the HealthCheck Service with comprehensive monitoring settings
    /// </summary>
    public sealed record HealthCheckServiceConfig
    {
        /// <summary>
        /// Default interval between automatic health checks
        /// </summary>
        public TimeSpan DefaultCheckInterval { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Maximum number of concurrent health checks that can run simultaneously
        /// </summary>
        public int MaxConcurrentChecks { get; init; } = 10;

        /// <summary>
        /// Default timeout for health checks if not specified
        /// </summary>
        public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Whether to enable automatic health checks on startup
        /// </summary>
        public bool EnableAutomaticChecks { get; init; } = true;

        /// <summary>
        /// Whether to enable circuit breaker functionality
        /// </summary>
        public bool EnableCircuitBreakers { get; init; } = true;

        /// <summary>
        /// Whether to enable graceful degradation features
        /// </summary>
        public bool EnableGracefulDegradation { get; init; } = true;

        /// <summary>
        /// Maximum number of health check results to keep in history per check
        /// </summary>
        public int MaxHistoryPerCheck { get; init; } = 100;

        /// <summary>
        /// Whether to raise alerts for health status changes
        /// </summary>
        public bool EnableHealthAlerts { get; init; } = true;

        /// <summary>
        /// Whether to raise alerts for circuit breaker state changes
        /// </summary>
        public bool EnableCircuitBreakerAlerts { get; init; } = true;

        /// <summary>
        /// Whether to raise alerts for degradation level changes
        /// </summary>
        public bool EnableDegradationAlerts { get; init; } = true;

        /// <summary>
        /// Thresholds for determining overall system health status
        /// </summary>
        public HealthThresholds HealthThresholds { get; init; } = new();

        /// <summary>
        /// Degradation thresholds for automatic system degradation
        /// </summary>
        public DegradationThresholds DegradationThresholds { get; init; } = new();

        /// <summary>
        /// Default circuit breaker configuration for health checks
        /// </summary>
        public CircuitBreakerConfig DefaultCircuitBreakerConfig { get; init; } = new();

        /// <summary>
        /// Custom alert severities for different health status levels
        /// </summary>
        public Dictionary<HealthStatus, AlertSeverity> AlertSeverities { get; init; } = new()
        {
            { HealthStatus.Healthy, AlertSeverity.Low },
            { HealthStatus.Warning, AlertSeverity.Warning },
            { HealthStatus.Degraded, AlertSeverity.Warning },
            { HealthStatus.Unhealthy, AlertSeverity.Critical },
            { HealthStatus.Critical, AlertSeverity.Critical },
            { HealthStatus.Offline, AlertSeverity.Critical }, 
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
        public int SlowHealthCheckThreshold { get; init; } = 1000;

        /// <summary>
        /// Validates the configuration and returns any validation errors
        /// </summary>
        /// <returns>List of validation error messages, empty if valid</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (DefaultCheckInterval <= TimeSpan.Zero)
                errors.Add("DefaultCheckInterval must be greater than zero");

            if (MaxConcurrentChecks <= 0)
                errors.Add("MaxConcurrentChecks must be greater than zero");

            if (DefaultTimeout <= TimeSpan.Zero)
                errors.Add("DefaultTimeout must be greater than zero");

            if (MaxHistoryPerCheck < 0)
                errors.Add("MaxHistoryPerCheck must be non-negative");

            if (SlowHealthCheckThreshold < 0)
                errors.Add("SlowHealthCheckThreshold must be non-negative");

            // Validate nested configurations
            errors.AddRange(HealthThresholds.Validate());
            errors.AddRange(DegradationThresholds.Validate());
            errors.AddRange(DefaultCircuitBreakerConfig.Validate());

            return errors;
        }

        /// <summary>
        /// Creates a configuration optimized for development environments
        /// </summary>
        /// <returns>Development-optimized configuration</returns>
        public static HealthCheckServiceConfig ForDevelopment()
        {
            return new HealthCheckServiceConfig
            {
                DefaultCheckInterval = TimeSpan.FromSeconds(10),
                MaxConcurrentChecks = 5,
                DefaultTimeout = TimeSpan.FromSeconds(10),
                EnableAutomaticChecks = true,
                EnableCircuitBreakers = true,
                EnableGracefulDegradation = false,
                MaxHistoryPerCheck = 50,
                EnableHealthCheckLogging = true,
                HealthCheckLogLevel = LogLevel.Debug,
                EnableProfiling = true,
                SlowHealthCheckThreshold = 500
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
                DefaultCheckInterval = TimeSpan.FromMinutes(1),
                MaxConcurrentChecks = 20,
                DefaultTimeout = TimeSpan.FromSeconds(30),
                EnableAutomaticChecks = true,
                EnableCircuitBreakers = true,
                EnableGracefulDegradation = true,
                MaxHistoryPerCheck = 200,
                EnableHealthCheckLogging = true,
                HealthCheckLogLevel = LogLevel.Info,
                EnableProfiling = true,
                SlowHealthCheckThreshold = 2000
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
                DefaultCheckInterval = TimeSpan.FromSeconds(1),
                MaxConcurrentChecks = 1,
                DefaultTimeout = TimeSpan.FromSeconds(5),
                EnableAutomaticChecks = false,
                EnableCircuitBreakers = false,
                EnableGracefulDegradation = false,
                MaxHistoryPerCheck = 10,
                EnableHealthAlerts = false,
                EnableCircuitBreakerAlerts = false,
                EnableDegradationAlerts = false,
                EnableHealthCheckLogging = false,
                EnableProfiling = false
            };
        }
    }
}