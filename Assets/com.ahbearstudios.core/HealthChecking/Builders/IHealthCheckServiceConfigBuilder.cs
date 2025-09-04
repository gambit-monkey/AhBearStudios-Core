using System;
using System.Collections.Generic;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Builders
{
    /// <summary>
    /// Interface for building HealthCheckServiceConfig instances with validation, 
    /// preset configurations, and environment-specific settings.
    /// Provides a fluent interface for building health check service configurations
    /// with comprehensive validation and support for different deployment environments.
    /// </summary>
    public interface IHealthCheckServiceConfigBuilder
    {
        /// <summary>
        /// Sets the automatic health check interval
        /// </summary>
        /// <param name="interval">Interval between automatic health checks</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithAutomaticCheckInterval(TimeSpan interval);

        /// <summary>
        /// Sets the maximum number of concurrent health checks
        /// </summary>
        /// <param name="maxConcurrent">Maximum concurrent health checks</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithMaxConcurrentHealthChecks(int maxConcurrent);

        /// <summary>
        /// Sets the default timeout for health checks
        /// </summary>
        /// <param name="timeout">Default health check timeout</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithDefaultTimeout(TimeSpan timeout);

        /// <summary>
        /// Configures automatic health checks
        /// </summary>
        /// <param name="enabled">Whether to enable automatic checks</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithAutomaticChecks(bool enabled = true);

        /// <summary>
        /// Sets the maximum history size for health check results
        /// </summary>
        /// <param name="maxHistory">Maximum number of results to keep in history</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithMaxHistorySize(int maxHistory);

        /// <summary>
        /// Configures retry settings for failed health checks
        /// </summary>
        /// <param name="maxRetries">Maximum number of retries</param>
        /// <param name="retryDelay">Delay between retries</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithRetrySettings(int maxRetries, TimeSpan retryDelay);

        /// <summary>
        /// Configures circuit breaker functionality
        /// </summary>
        /// <param name="enabled">Whether to enable circuit breakers</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithCircuitBreaker(bool enabled = true);

        /// <summary>
        /// Sets the default circuit breaker configuration
        /// </summary>
        /// <param name="config">Circuit breaker configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithDefaultCircuitBreakerConfig(CircuitBreakerConfig config);

        /// <summary>
        /// Configures circuit breaker alerts
        /// </summary>
        /// <param name="enabled">Whether to enable circuit breaker alerts</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithCircuitBreakerAlerts(bool enabled = true);

        /// <summary>
        /// Sets the default failure threshold for circuit breakers
        /// </summary>
        /// <param name="threshold">Failure threshold</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithDefaultFailureThreshold(int threshold);

        /// <summary>
        /// Sets the default circuit breaker timeout
        /// </summary>
        /// <param name="timeout">Circuit breaker timeout</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithDefaultCircuitBreakerTimeout(TimeSpan timeout);

        /// <summary>
        /// Configures graceful degradation functionality
        /// </summary>
        /// <param name="enabled">Whether to enable graceful degradation</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithGracefulDegradation(bool enabled = true);

        /// <summary>
        /// Sets degradation thresholds
        /// </summary>
        /// <param name="thresholds">Degradation thresholds configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithDegradationThresholds(DegradationThresholds thresholds);

        /// <summary>
        /// Configures degradation alerts
        /// </summary>
        /// <param name="enabled">Whether to enable degradation alerts</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithDegradationAlerts(bool enabled = true);

        /// <summary>
        /// Configures automatic degradation
        /// </summary>
        /// <param name="enabled">Whether to enable automatic degradation</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithAutomaticDegradation(bool enabled = true);

        /// <summary>
        /// Configures health alerts
        /// </summary>
        /// <param name="enabled">Whether to enable health alerts</param>
        /// <param name="severities">Custom alert severities for different health statuses</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithHealthAlerts(bool enabled = true, Dictionary<HealthStatus, AlertSeverity> severities = null);

        /// <summary>
        /// Sets alert tags for health check alerts
        /// </summary>
        /// <param name="tags">Alert tags to set</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithAlertTags(params FixedString64Bytes[] tags);

        /// <summary>
        /// Adds alert tags to existing tags
        /// </summary>
        /// <param name="tags">Alert tags to add</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder AddAlertTags(params FixedString64Bytes[] tags);

        /// <summary>
        /// Sets the alert failure threshold
        /// </summary>
        /// <param name="threshold">Number of consecutive failures before triggering alert</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithAlertFailureThreshold(int threshold);

        /// <summary>
        /// Configures health check logging
        /// </summary>
        /// <param name="enabled">Whether to enable health check logging</param>
        /// <param name="logLevel">Log level for health check operations</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithHealthCheckLogging(bool enabled = true, LogLevel logLevel = LogLevel.Info);

        /// <summary>
        /// Configures performance profiling
        /// </summary>
        /// <param name="enabled">Whether to enable profiling</param>
        /// <param name="slowThreshold">Threshold for slow health check logging (milliseconds)</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithProfiling(bool enabled = true, int slowThreshold = 1000);

        /// <summary>
        /// Configures detailed logging
        /// </summary>
        /// <param name="enabled">Whether to enable detailed logging</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithDetailedLogging(bool enabled = true);

        /// <summary>
        /// Configures memory and cleanup settings
        /// </summary>
        /// <param name="maxMemoryUsageMB">Maximum memory usage in MB</param>
        /// <param name="historyCleanupInterval">How often to clean up history</param>
        /// <param name="maxHistoryAge">Maximum age of history to keep</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithMemorySettings(int maxMemoryUsageMB, TimeSpan historyCleanupInterval, TimeSpan maxHistoryAge);

        /// <summary>
        /// Sets the thread priority for health check execution
        /// </summary>
        /// <param name="priority">Thread priority</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithThreadPriority(System.Threading.ThreadPriority priority);

        /// <summary>
        /// Sets health thresholds
        /// </summary>
        /// <param name="thresholds">Health thresholds configuration</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithHealthThresholds(HealthThresholds thresholds);

        /// <summary>
        /// Sets the unhealthy threshold
        /// </summary>
        /// <param name="threshold">Percentage of unhealthy checks that triggers overall unhealthy status</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithUnhealthyThreshold(double threshold);

        /// <summary>
        /// Sets the warning threshold
        /// </summary>
        /// <param name="threshold">Percentage of warning checks that triggers overall warning status</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithWarningThreshold(double threshold);

        /// <summary>
        /// Configures dependency validation
        /// </summary>
        /// <param name="enabled">Whether to enable dependency validation</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithDependencyValidation(bool enabled = true);

        /// <summary>
        /// Configures result caching
        /// </summary>
        /// <param name="enabled">Whether to enable result caching</param>
        /// <param name="cacheDuration">How long to cache results</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithResultCaching(bool enabled, TimeSpan cacheDuration);

        /// <summary>
        /// Configures execution timeouts
        /// </summary>
        /// <param name="enabled">Whether to enable execution timeouts</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithExecutionTimeouts(bool enabled = true);

        /// <summary>
        /// Configures correlation IDs
        /// </summary>
        /// <param name="enabled">Whether to enable correlation IDs</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithCorrelationIds(bool enabled = true);

        /// <summary>
        /// Sets default metadata for all health check results
        /// </summary>
        /// <param name="metadata">Default metadata dictionary</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder WithDefaultMetadata(Dictionary<string, object> metadata);

        /// <summary>
        /// Adds a single metadata entry
        /// </summary>
        /// <param name="key">Metadata key</param>
        /// <param name="value">Metadata value</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder AddMetadata(string key, object value);

        /// <summary>
        /// Configures the builder for a specific environment
        /// </summary>
        /// <param name="environment">Target environment</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder ForEnvironment(HealthCheckEnvironment environment);

        /// <summary>
        /// Applies development environment preset
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder ApplyDevelopmentPreset();

        /// <summary>
        /// Applies testing environment preset
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder ApplyTestingPreset();

        /// <summary>
        /// Applies staging environment preset
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder ApplyStagingPreset();

        /// <summary>
        /// Applies production environment preset
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder ApplyProductionPreset();

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        /// <returns>List of validation errors</returns>
        List<string> Validate();

        /// <summary>
        /// Builds the HealthCheckServiceConfig instance
        /// </summary>
        /// <returns>Configured HealthCheckServiceConfig instance</returns>
        HealthCheckServiceConfig Build();

        /// <summary>
        /// Resets the builder to its initial state
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckServiceConfigBuilder Reset();
    }
}