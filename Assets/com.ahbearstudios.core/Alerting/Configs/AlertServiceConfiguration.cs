using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Configs
{
    /// <summary>
    /// Service-level configuration for AlertService instances.
    /// Provides comprehensive settings for service initialization, dependency injection, and operational behavior.
    /// Designed for Unity game development with support for different deployment configurations.
    /// </summary>
    public record AlertServiceConfiguration
    {
        /// <summary>
        /// Gets the unique service instance name for identification and logging.
        /// </summary>
        public FixedString64Bytes ServiceName { get; init; } = "DefaultAlertService";

        /// <summary>
        /// Gets the core alert system configuration.
        /// Contains all alert processing, filtering, and channel settings.
        /// </summary>
        public AlertConfig AlertConfig { get; init; } = new AlertConfig();

        /// <summary>
        /// Gets the service startup timeout for initialization operations.
        /// Service creation will fail if initialization exceeds this timeout.
        /// </summary>
        public TimeSpan StartupTimeout { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets the service shutdown timeout for graceful cleanup operations.
        /// Remaining alerts will be processed up to this timeout during shutdown.
        /// </summary>
        public TimeSpan ShutdownTimeout { get; init; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets whether the service should automatically start processing alerts upon creation.
        /// When false, the service must be manually started after creation.
        /// </summary>
        public bool AutoStart { get; init; } = true;

        /// <summary>
        /// Gets whether the service should validate its configuration on startup.
        /// When enabled, invalid configurations will prevent service startup.
        /// </summary>
        public bool ValidateOnStartup { get; init; } = true;

        /// <summary>
        /// Gets the environment type that affects service behavior and defaults.
        /// Different environments may have different channel configurations and settings.
        /// </summary>
        public AlertEnvironmentType Environment { get; init; } = AlertEnvironmentType.Development;

        /// <summary>
        /// Gets whether Unity-specific service integrations are enabled.
        /// When enabled, integrates with Unity lifecycle, MonoBehaviour updates, and application events.
        /// </summary>
        public bool EnableUnityIntegration { get; init; } = true;

        /// <summary>
        /// Gets whether service health monitoring and reporting is enabled.
        /// When enabled, service health is reported to the centralized health check system.
        /// </summary>
        public bool EnableHealthReporting { get; init; } = true;

        /// <summary>
        /// Gets the interval for service health check operations.
        /// Health checks verify service responsiveness and channel availability.
        /// </summary>
        public TimeSpan HealthCheckInterval { get; init; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets whether performance metrics collection is enabled for this service instance.
        /// When enabled, detailed metrics are collected and made available for monitoring.
        /// </summary>
        public bool EnableMetrics { get; init; } = true;

        /// <summary>
        /// Gets the metrics collection interval for performance data gathering.
        /// More frequent collection provides better observability but uses more resources.
        /// </summary>
        public TimeSpan MetricsInterval { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets whether service telemetry and tracing is enabled.
        /// When enabled, service operations are traced for distributed debugging and analysis.
        /// </summary>
        public bool EnableTelemetry { get; init; } = true;

        /// <summary>
        /// Gets the collection of service-specific tags for identification and filtering.
        /// Tags are used for service discovery, monitoring, and operational classification.
        /// </summary>
        public IReadOnlyDictionary<string, string> ServiceTags { get; init; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets whether automatic dependency resolution is enabled.
        /// When enabled, dependencies are automatically resolved from registered services.
        /// </summary>
        public bool AutoResolveDependencies { get; init; } = true;

        /// <summary>
        /// Gets whether missing dependencies should cause service creation to fail.
        /// When false, null dependencies are allowed and must be handled by the service.
        /// </summary>
        public bool RequireAllDependencies { get; init; } = false;

        /// <summary>
        /// Gets the timeout for dependency resolution operations.
        /// </summary>
        public TimeSpan DependencyResolutionTimeout { get; init; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets the maximum memory usage limit for the service in MB.
        /// When exceeded, the service will attempt to free resources or enter a degraded state.
        /// </summary>
        public int MaxMemoryUsageMB { get; init; } = 100;

        /// <summary>
        /// Gets the maximum number of concurrent operations the service can perform.
        /// </summary>
        public int MaxConcurrentOperations { get; init; } = 50;

        /// <summary>
        /// Gets the maximum number of alerts the service can queue for processing.
        /// </summary>
        public int MaxQueuedAlerts { get; init; } = 1000;

        /// <summary>
        /// Gets whether the service should continue operating after non-critical errors.
        /// When false, any error causes the service to enter a failed state.
        /// </summary>
        public bool ContinueOnError { get; init; } = true;

        /// <summary>
        /// Gets the maximum number of consecutive errors before the service enters degraded mode.
        /// </summary>
        public int MaxConsecutiveErrors { get; init; } = 10;

        /// <summary>
        /// Gets the time window for error rate calculations.
        /// </summary>
        public TimeSpan ErrorWindow { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets whether errors should be logged to the configured logging service.
        /// </summary>
        public bool LogErrors { get; init; } = true;

        /// <summary>
        /// Gets whether critical errors should trigger emergency escalation.
        /// </summary>
        public bool EscalateCriticalErrors { get; init; } = true;

        /// <summary>
        /// Gets whether the service should automatically dispose resources on application shutdown.
        /// When enabled, ensures proper cleanup of channels, filters, and other resources.
        /// </summary>
        public bool AutoDisposeOnShutdown { get; init; } = true;

        /// <summary>
        /// Gets the correlation ID for tracking this service instance across distributed operations.
        /// Used for debugging and tracing service interactions.
        /// </summary>
        public Guid CorrelationId { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Validates the service configuration for correctness and consistency.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration validation fails.</exception>
        public void Validate()
        {
            if (ServiceName.IsEmpty)
                throw new InvalidOperationException("Service name cannot be empty.");

            if (StartupTimeout <= TimeSpan.Zero)
                throw new InvalidOperationException("Startup timeout must be greater than zero.");

            if (ShutdownTimeout <= TimeSpan.Zero)
                throw new InvalidOperationException("Shutdown timeout must be greater than zero.");

            if (HealthCheckInterval <= TimeSpan.Zero)
                throw new InvalidOperationException("Health check interval must be greater than zero.");

            if (MetricsInterval <= TimeSpan.Zero)
                throw new InvalidOperationException("Metrics interval must be greater than zero.");

            if (!Enum.IsDefined(typeof(AlertEnvironmentType), Environment))
                throw new InvalidOperationException("Environment type is not valid.");

            if (DependencyResolutionTimeout <= TimeSpan.Zero)
                throw new InvalidOperationException("Dependency resolution timeout must be greater than zero.");

            if (MaxMemoryUsageMB <= 0)
                throw new InvalidOperationException("Max memory usage must be greater than zero.");

            if (MaxConcurrentOperations <= 0)
                throw new InvalidOperationException("Max concurrent operations must be greater than zero.");

            if (MaxQueuedAlerts <= 0)
                throw new InvalidOperationException("Max queued alerts must be greater than zero.");

            if (MaxConsecutiveErrors <= 0)
                throw new InvalidOperationException("Max consecutive errors must be greater than zero.");

            if (ErrorWindow <= TimeSpan.Zero)
                throw new InvalidOperationException("Error window must be greater than zero.");

            // Validate the nested AlertConfig
            AlertConfig.Validate();
        }
    }
}