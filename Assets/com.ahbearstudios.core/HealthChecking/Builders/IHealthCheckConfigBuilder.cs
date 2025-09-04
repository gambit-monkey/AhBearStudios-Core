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
    /// Interface for building individual HealthCheckConfiguration instances with comprehensive options.
    /// Provides a fluent API for configuring health check behavior, alerting, monitoring, and dependencies.
    /// </summary>
    public interface IHealthCheckConfigBuilder
    {
        /// <summary>
        /// Sets the display name for the health check
        /// </summary>
        /// <param name="name">Display name (required)</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithName(string name);

        /// <summary>
        /// Sets the description for the health check
        /// </summary>
        /// <param name="description">Description of what the health check validates</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithDescription(string description);

        /// <summary>
        /// Sets the category for the health check
        /// </summary>
        /// <param name="category">Health check category</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithCategory(HealthCheckCategory category);

        /// <summary>
        /// Sets whether the health check is enabled
        /// </summary>
        /// <param name="enabled">Whether the health check is enabled</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithEnabled(bool enabled = true);

        /// <summary>
        /// Sets the execution interval for the health check
        /// </summary>
        /// <param name="interval">Execution interval (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithInterval(TimeSpan interval);

        /// <summary>
        /// Sets the timeout for the health check
        /// </summary>
        /// <param name="timeout">Execution timeout (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithTimeout(TimeSpan timeout);

        /// <summary>
        /// Sets the priority for the health check
        /// </summary>
        /// <param name="priority">Execution priority (higher numbers execute first)</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithPriority(int priority);

        /// <summary>
        /// Configures circuit breaker for the health check
        /// </summary>
        /// <param name="enabled">Whether to enable circuit breaker</param>
        /// <param name="config">Circuit breaker configuration (optional)</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithCircuitBreaker(bool enabled = true, CircuitBreakerConfig config = null);

        /// <summary>
        /// Sets whether this health check should be included in overall status calculation
        /// </summary>
        /// <param name="include">Whether to include in overall status</param>
        /// <param name="weight">Weight for overall status calculation (0.0 to 1.0)</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithOverallStatusImpact(bool include = true, double weight = 1.0);

        /// <summary>
        /// Configures alerting for the health check
        /// </summary>
        /// <param name="enabled">Whether to enable alerting</param>
        /// <param name="onlyOnStatusChange">Whether to alert only on status changes</param>
        /// <param name="cooldown">Minimum time between alerts</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithAlerting(bool enabled = true, bool onlyOnStatusChange = true, TimeSpan? cooldown = null);

        /// <summary>
        /// Sets custom alert severities for different health statuses
        /// </summary>
        /// <param name="severities">Custom alert severities</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithAlertSeverities(Dictionary<HealthStatus, AlertSeverity> severities);

        /// <summary>
        /// Adds tags to the health check
        /// </summary>
        /// <param name="tags">Tags to add</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithTags(params FixedString64Bytes[] tags);

        /// <summary>
        /// Adds metadata to the health check
        /// </summary>
        /// <param name="key">Metadata key</param>
        /// <param name="value">Metadata value</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithMetadata(string key, object value);

        /// <summary>
        /// Adds multiple metadata entries to the health check
        /// </summary>
        /// <param name="metadata">Metadata dictionary</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithMetadata(Dictionary<string, object> metadata);

        /// <summary>
        /// Sets dependencies for the health check
        /// </summary>
        /// <param name="dependencies">Health check dependencies</param>
        /// <param name="skipOnUnhealthy">Whether to skip if dependencies are unhealthy</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithDependencies(IEnumerable<FixedString64Bytes> dependencies, bool skipOnUnhealthy = true);

        /// <summary>
        /// Configures history retention for the health check
        /// </summary>
        /// <param name="maxSize">Maximum number of history entries to keep</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithHistory(int maxSize);

        /// <summary>
        /// Configures logging for the health check
        /// </summary>
        /// <param name="enableDetailed">Whether to enable detailed logging</param>
        /// <param name="logLevel">Log level for operations</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithLogging(bool enableDetailed = false, LogLevel logLevel = LogLevel.Info);

        /// <summary>
        /// Configures profiling for the health check
        /// </summary>
        /// <param name="enabled">Whether to enable profiling</param>
        /// <param name="slowThreshold">Threshold for slow execution in milliseconds</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithProfiling(bool enabled = true, int slowThreshold = 1000);

        /// <summary>
        /// Adds custom parameters for the health check implementation
        /// </summary>
        /// <param name="key">Parameter key</param>
        /// <param name="value">Parameter value</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithCustomParameter(string key, object value);

        /// <summary>
        /// Adds multiple custom parameters for the health check implementation
        /// </summary>
        /// <param name="parameters">Parameters dictionary</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithCustomParameters(Dictionary<string, object> parameters);

        /// <summary>
        /// Configures retry behavior for the health check
        /// </summary>
        /// <param name="maxRetries">Maximum number of retries</param>
        /// <param name="retryDelay">Delay between retries</param>
        /// <param name="backoffMultiplier">Exponential backoff multiplier</param>
        /// <param name="maxRetryDelay">Maximum delay between retries</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithRetry(int maxRetries = 0, TimeSpan? retryDelay = null, double backoffMultiplier = 1.0, TimeSpan? maxRetryDelay = null);

        /// <summary>
        /// Configures degradation impact for the health check
        /// </summary>
        /// <param name="degradedImpact">Impact level when degraded</param>
        /// <param name="unhealthyImpact">Impact level when unhealthy</param>
        /// <param name="disabledFeatures">Features to disable when unhealthy</param>
        /// <param name="degradedServices">Services to degrade when unhealthy</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithDegradationImpact(DegradationLevel degradedImpact = DegradationLevel.Minor, DegradationLevel unhealthyImpact = DegradationLevel.Moderate, IEnumerable<FixedString64Bytes> disabledFeatures = null, IEnumerable<FixedString64Bytes> degradedServices = null);

        /// <summary>
        /// Configures validation for health check results
        /// </summary>
        /// <param name="enabled">Whether to enable validation</param>
        /// <param name="minExecutionTime">Minimum acceptable execution time</param>
        /// <param name="maxExecutionTime">Maximum acceptable execution time</param>
        /// <param name="requiredDataFields">Required data fields in results</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithValidation(bool enabled = true, TimeSpan? minExecutionTime = null, TimeSpan? maxExecutionTime = null, IEnumerable<string> requiredDataFields = null);

        /// <summary>
        /// Configures resource limits for the health check
        /// </summary>
        /// <param name="maxMemoryUsage">Maximum memory usage in bytes (0 = no limit)</param>
        /// <param name="maxCpuUsage">Maximum CPU usage percentage (0 = no limit)</param>
        /// <param name="maxConcurrentExecutions">Maximum concurrent executions</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder WithResourceLimits(long maxMemoryUsage = 0, double maxCpuUsage = 0, int maxConcurrentExecutions = 1);

        /// <summary>
        /// Applies a preset configuration for the specified scenario
        /// </summary>
        /// <param name="preset">Configuration preset to apply</param>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder ForScenario(HealthCheckScenario preset);

        /// <summary>
        /// Validates the current configuration without building
        /// </summary>
        /// <returns>List of validation errors, empty if valid</returns>
        List<string> Validate();

        /// <summary>
        /// Builds the HealthCheckConfiguration instance
        /// </summary>
        /// <returns>Configured HealthCheckConfiguration instance</returns>
        HealthCheckConfiguration Build();

        /// <summary>
        /// Resets the builder to allow building a new configuration
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        IHealthCheckConfigBuilder Reset();
    }
}