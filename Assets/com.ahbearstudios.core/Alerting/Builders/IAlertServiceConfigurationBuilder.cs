using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Builders
{
    /// <summary>
    /// Builder interface for creating AlertServiceConfiguration instances with fluent syntax.
    /// Provides comprehensive configuration options for alert service setup across different environments.
    /// Follows the Builder pattern for complex configuration construction.
    /// </summary>
    public interface IAlertServiceConfigurationBuilder
    {
        /// <summary>
        /// Sets the service name for identification and logging.
        /// </summary>
        /// <param name="serviceName">The unique service name.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithServiceName(string serviceName);

        /// <summary>
        /// Sets the core alert configuration.
        /// </summary>
        /// <param name="alertConfig">The alert configuration to use.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithAlertConfig(AlertConfig alertConfig);

        /// <summary>
        /// Configures the core alert system using a builder.
        /// </summary>
        /// <param name="configAction">Action to configure the alert system.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithAlertConfig(Action<IAlertConfigBuilder> configAction);

        /// <summary>
        /// Sets the service startup timeout.
        /// </summary>
        /// <param name="timeout">The startup timeout duration.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithStartupTimeout(TimeSpan timeout);

        /// <summary>
        /// Sets the service shutdown timeout.
        /// </summary>
        /// <param name="timeout">The shutdown timeout duration.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithShutdownTimeout(TimeSpan timeout);

        /// <summary>
        /// Sets whether the service should automatically start upon creation.
        /// </summary>
        /// <param name="autoStart">True to enable auto-start, false otherwise.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithAutoStart(bool autoStart);

        /// <summary>
        /// Sets whether configuration validation should occur on startup.
        /// </summary>
        /// <param name="validateOnStartup">True to enable startup validation, false otherwise.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithValidateOnStartup(bool validateOnStartup);

        /// <summary>
        /// Sets the environment type for the service.
        /// </summary>
        /// <param name="environment">The environment type.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithEnvironment(AlertEnvironmentType environment);

        /// <summary>
        /// Sets whether Unity integration is enabled.
        /// </summary>
        /// <param name="enableUnityIntegration">True to enable Unity integration, false otherwise.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithUnityIntegration(bool enableUnityIntegration);

        /// <summary>
        /// Sets whether health reporting is enabled.
        /// </summary>
        /// <param name="enableHealthReporting">True to enable health reporting, false otherwise.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithHealthReporting(bool enableHealthReporting);

        /// <summary>
        /// Sets the health check interval.
        /// </summary>
        /// <param name="interval">The health check interval.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithHealthCheckInterval(TimeSpan interval);

        /// <summary>
        /// Sets whether metrics collection is enabled.
        /// </summary>
        /// <param name="enableMetrics">True to enable metrics, false otherwise.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithMetrics(bool enableMetrics);

        /// <summary>
        /// Sets the metrics collection interval.
        /// </summary>
        /// <param name="interval">The metrics collection interval.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithMetricsInterval(TimeSpan interval);

        /// <summary>
        /// Sets whether telemetry is enabled.
        /// </summary>
        /// <param name="enableTelemetry">True to enable telemetry, false otherwise.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithTelemetry(bool enableTelemetry);

        /// <summary>
        /// Adds service tags for identification and classification.
        /// </summary>
        /// <param name="tags">Dictionary of service tags.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithServiceTags(IDictionary<string, string> tags);

        /// <summary>
        /// Adds a single service tag.
        /// </summary>
        /// <param name="key">The tag key.</param>
        /// <param name="value">The tag value.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithServiceTag(string key, string value);

        /// <summary>
        /// Sets dependency resolution options.
        /// </summary>
        /// <param name="autoResolve">Whether to auto-resolve dependencies.</param>
        /// <param name="requireAll">Whether all dependencies are required.</param>
        /// <param name="timeout">Dependency resolution timeout.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithDependencyResolution(bool autoResolve, bool requireAll = false, TimeSpan? timeout = null);

        /// <summary>
        /// Sets resource limits for the service.
        /// </summary>
        /// <param name="maxMemoryMB">Maximum memory usage in MB.</param>
        /// <param name="maxConcurrentOperations">Maximum concurrent operations.</param>
        /// <param name="maxQueuedAlerts">Maximum queued alerts.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithResourceLimits(int maxMemoryMB, int maxConcurrentOperations, int maxQueuedAlerts);

        /// <summary>
        /// Sets error handling behavior.
        /// </summary>
        /// <param name="continueOnError">Whether to continue on non-critical errors.</param>
        /// <param name="maxConsecutiveErrors">Maximum consecutive errors before degraded mode.</param>
        /// <param name="errorWindow">Time window for error rate calculations.</param>
        /// <param name="logErrors">Whether to log errors.</param>
        /// <param name="escalateCriticalErrors">Whether to escalate critical errors.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithErrorHandling(bool continueOnError = true, int maxConsecutiveErrors = 10, TimeSpan? errorWindow = null, bool logErrors = true, bool escalateCriticalErrors = true);

        /// <summary>
        /// Sets whether automatic resource disposal is enabled on shutdown.
        /// </summary>
        /// <param name="autoDispose">True to enable auto-disposal, false otherwise.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithAutoDisposeOnShutdown(bool autoDispose);

        /// <summary>
        /// Sets the correlation ID for service tracking.
        /// </summary>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder WithCorrelationId(Guid correlationId);

        /// <summary>
        /// Applies a development environment preset configuration.
        /// </summary>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder ForDevelopment();

        /// <summary>
        /// Applies a production environment preset configuration.
        /// </summary>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder ForProduction();

        /// <summary>
        /// Applies a testing environment preset configuration.
        /// </summary>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder ForTesting();

        /// <summary>
        /// Builds and returns the configured AlertServiceConfiguration instance.
        /// </summary>
        /// <returns>The configured AlertServiceConfiguration.</returns>
        AlertServiceConfiguration Build();

        /// <summary>
        /// Validates the current configuration without building it.
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise.</returns>
        bool IsValid();

        /// <summary>
        /// Resets the builder to its initial state.
        /// </summary>
        /// <returns>The builder instance for method chaining.</returns>
        IAlertServiceConfigurationBuilder Reset();
    }
}