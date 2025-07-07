using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.HealthCheck.Models;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.HealthCheck.Interfaces;

/// <summary>
/// Defines the contract for health check configuration builders that implement the fluent pattern.
/// Provides a comprehensive, type-safe way to construct health check configurations with
/// validation, defaults, and enterprise-grade features including alerting, remediation,
/// and performance tuning. Integrates with the message bus system for configuration events.
/// </summary>
/// <typeparam name="TConfig">The health check configuration type being built</typeparam>
/// <typeparam name="TBuilder">The builder type itself (for method chaining)</typeparam>
public interface IHealthCheckConfigBuilder<TConfig, TBuilder> 
    where TConfig : IHealthCheckConfig
    where TBuilder : IHealthCheckConfigBuilder<TConfig, TBuilder>
{
    #region Core Configuration

    /// <summary>
    /// Sets the configuration identifier for the health check.
    /// </summary>
    /// <param name="configId">The unique configuration identifier.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configId is null or empty.</exception>
    TBuilder WithConfigId(string configId);

    /// <summary>
    /// Sets the display name for the health check.
    /// </summary>
    /// <param name="displayName">The human-readable display name.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when displayName is null or empty.</exception>
    TBuilder WithDisplayName(string displayName);

    /// <summary>
    /// Sets the description of what the health check monitors.
    /// </summary>
    /// <param name="description">The descriptive text explaining the health check purpose.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when description is null or empty.</exception>
    TBuilder WithDescription(string description);

    /// <summary>
    /// Sets whether the health check is enabled.
    /// </summary>
    /// <param name="enabled">True to enable the health check; false to disable it.</param>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithEnabled(bool enabled = true);

    /// <summary>
    /// Sets the category for the health check.
    /// </summary>
    /// <param name="category">The category identifier for grouping health checks.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when category is null or empty.</exception>
    TBuilder WithCategory(FixedString64Bytes category);

    /// <summary>
    /// Sets the category using a predefined category constant.
    /// </summary>
    /// <param name="category">The predefined category from HealthCheckCategory.</param>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithCategory(string category);

    /// <summary>
    /// Sets the tags for additional categorization and filtering.
    /// </summary>
    /// <param name="tags">Comma-separated tags for the health check.</param>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithTags(FixedString64Bytes tags);

    /// <summary>
    /// Sets the tags using an array of individual tag values.
    /// </summary>
    /// <param name="tags">Individual tag values to combine.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when tags array is null.</exception>
    TBuilder WithTags(params string[] tags);

    /// <summary>
    /// Adds a single tag to the existing tags.
    /// </summary>
    /// <param name="tag">The tag to add.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when tag is null or empty.</exception>
    TBuilder AddTag(string tag);

    #endregion

    #region Execution Configuration

    /// <summary>
    /// Sets the execution interval for automatic health check runs.
    /// </summary>
    /// <param name="interval">The time between automatic executions.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is negative.</exception>
    TBuilder WithExecutionInterval(TimeSpan interval);

    /// <summary>
    /// Sets the execution interval using seconds.
    /// </summary>
    /// <param name="intervalSeconds">The interval in seconds between executions.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when intervalSeconds is negative.</exception>
    TBuilder WithExecutionInterval(double intervalSeconds);

    /// <summary>
    /// Sets the maximum execution timeout for the health check.
    /// </summary>
    /// <param name="timeout">The maximum time allowed for execution.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is negative or zero.</exception>
    TBuilder WithExecutionTimeout(TimeSpan timeout);

    /// <summary>
    /// Sets the execution timeout using seconds.
    /// </summary>
    /// <param name="timeoutSeconds">The timeout in seconds.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when timeoutSeconds is negative or zero.</exception>
    TBuilder WithExecutionTimeout(double timeoutSeconds);

    /// <summary>
    /// Sets the initial delay before the first execution.
    /// </summary>
    /// <param name="delay">The initial delay before first execution.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when delay is negative.</exception>
    TBuilder WithInitialDelay(TimeSpan delay);

    /// <summary>
    /// Sets the initial delay using seconds.
    /// </summary>
    /// <param name="delaySeconds">The initial delay in seconds.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when delaySeconds is negative.</exception>
    TBuilder WithInitialDelay(double delaySeconds);

    /// <summary>
    /// Sets the execution priority for the health check.
    /// </summary>
    /// <param name="priority">The priority level (higher values execute first).</param>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithPriority(int priority);

    /// <summary>
    /// Enables or disables parallel execution for the health check.
    /// </summary>
    /// <param name="allowParallel">True to allow parallel execution; false for sequential.</param>
    /// <param name="maxDegreeOfParallelism">Optional maximum degree of parallelism. A value of -1 means to use processor count i.e., Environment.ProcessorCount </param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxDegreeOfParallelism is less than 1.</exception>
    TBuilder WithParallelExecution(bool allowParallel = true, int maxDegreeOfParallelism = -1);

    #endregion

    #region Retry and Resilience Configuration

    /// <summary>
    /// Enables retry logic with default retry settings.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithRetryEnabled();

    /// <summary>
    /// Configures retry logic with specific parameters.
    /// </summary>
    /// <param name="maxAttempts">Maximum number of retry attempts.</param>
    /// <param name="retryDelay">Base delay between retry attempts.</param>
    /// <param name="strategy">The retry strategy to use.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxAttempts is negative or retryDelay is negative.</exception>
    TBuilder WithRetry(int maxAttempts, TimeSpan retryDelay, HealthCheckRetryStrategy strategy = HealthCheckRetryStrategy.ExponentialBackoff);

    /// <summary>
    /// Configures retry logic using seconds for delay.
    /// </summary>
    /// <param name="maxAttempts">Maximum number of retry attempts.</param>
    /// <param name="retryDelaySeconds">Base delay in seconds between retry attempts.</param>
    /// <param name="strategy">The retry strategy to use.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxAttempts is negative or retryDelaySeconds is negative.</exception>
    TBuilder WithRetry(int maxAttempts, double retryDelaySeconds, HealthCheckRetryStrategy strategy = HealthCheckRetryStrategy.ExponentialBackoff);

    /// <summary>
    /// Sets the retry strategy for failed health checks.
    /// </summary>
    /// <param name="strategy">The retry strategy to use.</param>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithRetryStrategy(HealthCheckRetryStrategy strategy);

    /// <summary>
    /// Sets the maximum delay allowed between retry attempts.
    /// </summary>
    /// <param name="maxDelay">The maximum delay between retries.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxDelay is negative or zero.</exception>
    TBuilder WithMaxRetryDelay(TimeSpan maxDelay);

    /// <summary>
    /// Enables or disables jitter in retry delays.
    /// </summary>
    /// <param name="useJitter">True to add random jitter to retry delays.</param>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithRetryJitter(bool useJitter = true);

    /// <summary>
    /// Disables retry logic completely.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithoutRetry();

    #endregion

    #region Alerting Configuration

    /// <summary>
    /// Enables alerting with default alert settings.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithAlertingEnabled();

    /// <summary>
    /// Configures alerting with specific parameters.
    /// </summary>
    /// <param name="severityThreshold">Minimum severity level to trigger alerts.</param>
    /// <param name="failureThreshold">Number of consecutive failures to trigger alert.</param>
    /// <param name="evaluationWindow">Time window for evaluating alert conditions.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when failureThreshold is less than 1 or evaluationWindow is negative.</exception>
    TBuilder WithAlerting(HealthSeverity severityThreshold, int failureThreshold = 1, TimeSpan evaluationWindow = default);

    /// <summary>
    /// Sets the alert severity threshold.
    /// </summary>
    /// <param name="threshold">Minimum severity level to trigger alerts.</param>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithAlertSeverityThreshold(HealthSeverity threshold);

    /// <summary>
    /// Sets the alert failure threshold.
    /// </summary>
    /// <param name="threshold">Number of consecutive failures required to trigger alert.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is less than 1.</exception>
    TBuilder WithAlertFailureThreshold(int threshold);

    /// <summary>
    /// Configures alert suppression and cooldown settings.
    /// </summary>
    /// <param name="suppressDuplicates">Whether to suppress duplicate alerts.</param>
    /// <param name="cooldownPeriod">Minimum time between alert notifications.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when cooldownPeriod is negative.</exception>
    TBuilder WithAlertSuppression(bool suppressDuplicates = true, TimeSpan cooldownPeriod = default);

    /// <summary>
    /// Configures alert escalation settings.
    /// </summary>
    /// <param name="enableEscalation">Whether to enable alert escalation.</param>
    /// <param name="escalationDelay">Time after which unresolved alerts are escalated.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when escalationDelay is negative or zero and enableEscalation is true.</exception>
    TBuilder WithAlertEscalation(bool enableEscalation = true, TimeSpan escalationDelay = default);

    /// <summary>
    /// Disables alerting completely.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithoutAlerting();

    #endregion

    #region Remediation Configuration

    /// <summary>
    /// Enables remediation with default remediation settings.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithRemediationEnabled();

    /// <summary>
    /// Configures remediation with specific parameters.
    /// </summary>
    /// <param name="severityThreshold">Minimum severity level to trigger remediation.</param>
    /// <param name="maxAttempts">Maximum number of remediation attempts.</param>
    /// <param name="remediationDelay">Delay between remediation attempts.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxAttempts is less than 1 or remediationDelay is negative.</exception>
    TBuilder WithRemediation(HealthSeverity severityThreshold, int maxAttempts = 3, TimeSpan remediationDelay = default);

    /// <summary>
    /// Sets the remediation severity threshold.
    /// </summary>
    /// <param name="threshold">Minimum severity level to trigger remediation.</param>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithRemediationSeverityThreshold(HealthSeverity threshold);

    /// <summary>
    /// Configures remediation verification settings.
    /// </summary>
    /// <param name="verifyAfterRemediation">Whether to re-execute health check after remediation.</param>
    /// <param name="verificationDelay">Delay before re-executing after remediation.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when verificationDelay is negative.</exception>
    TBuilder WithRemediationVerification(bool verifyAfterRemediation = true, TimeSpan verificationDelay = default);

    /// <summary>
    /// Disables remediation completely.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithoutRemediation();

    #endregion

    #region Performance and Resource Configuration

    /// <summary>
    /// Sets the expected normal execution time for performance monitoring.
    /// </summary>
    /// <param name="expectedTime">The expected execution time.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when expectedTime is negative or zero.</exception>
    TBuilder WithExpectedExecutionTime(TimeSpan expectedTime);

    /// <summary>
    /// Sets the slow execution threshold for performance alerting.
    /// </summary>
    /// <param name="threshold">The threshold above which execution is considered slow.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is negative or zero.</exception>
    TBuilder WithSlowExecutionThreshold(TimeSpan threshold);

    /// <summary>
    /// Configures resource usage limits for the health check.
    /// </summary>
    /// <param name="maxMemoryBytes">Maximum memory usage in bytes (0 = no limit).</param>
    /// <param name="maxCpuPercent">Maximum CPU usage percentage (0 = no limit).</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when values are negative or CPU percentage exceeds 100.</exception>
    TBuilder WithResourceLimits(long maxMemoryBytes = 0, double maxCpuPercent = 0);

    /// <summary>
    /// Enables or disables performance metrics collection.
    /// </summary>
    /// <param name="collectMetrics">Whether to collect detailed performance metrics.</param>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithPerformanceMetrics(bool collectMetrics = true);

    /// <summary>
    /// Configures history retention settings.
    /// </summary>
    /// <param name="retentionCount">Number of history entries to retain.</param>
    /// <param name="retentionPeriod">Maximum age of history entries.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when retentionCount is negative or retentionPeriod is negative.</exception>
    TBuilder WithHistoryRetention(int retentionCount = 100, TimeSpan retentionPeriod = default);

    #endregion

    #region Dependency and Environment Configuration

    /// <summary>
    /// Sets the health check dependencies.
    /// </summary>
    /// <param name="dependencies">Names of health checks that must be healthy before this one executes.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dependencies is null.</exception>
    TBuilder WithDependencies(params FixedString64Bytes[] dependencies);

    /// <summary>
    /// Sets the health check dependencies using string names.
    /// </summary>
    /// <param name="dependencies">Names of health checks that must be healthy before this one executes.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dependencies is null.</exception>
    TBuilder WithDependencies(params string[] dependencies);

    /// <summary>
    /// Adds a single dependency to the existing dependencies.
    /// </summary>
    /// <param name="dependency">Name of a health check that must be healthy before this one executes.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dependency is null or empty.</exception>
    TBuilder AddDependency(FixedString64Bytes dependency);

    /// <summary>
    /// Adds a single dependency using a string name.
    /// </summary>
    /// <param name="dependency">Name of a health check that must be healthy before this one executes.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dependency is null or empty.</exception>
    TBuilder AddDependency(string dependency);

    /// <summary>
    /// Configures dependency behavior.
    /// </summary>
    /// <param name="skipOnUnhealthy">Whether to skip execution if dependencies are unhealthy.</param>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithDependencyBehavior(bool skipOnUnhealthy = true);

    /// <summary>
    /// Sets the active environments for this health check.
    /// </summary>
    /// <param name="environments">Environment names where this health check should be active.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when environments is null.</exception>
    TBuilder WithActiveEnvironments(params string[] environments);

    /// <summary>
    /// Sets the minimum system resource requirements.
    /// </summary>
    /// <param name="requirements">The minimum resource requirements for execution.</param>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithSystemResourceRequirements(SystemResourceRequirements requirements);

    /// <summary>
    /// Adds custom properties specific to the health check implementation.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null or empty.</exception>
    TBuilder WithCustomProperty(string key, object value);

    /// <summary>
    /// Adds multiple custom properties.
    /// </summary>
    /// <param name="properties">Dictionary of custom properties to add.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when properties is null.</exception>
    TBuilder WithCustomProperties(IReadOnlyDictionary<string, object> properties);

    #endregion

    #region Preset Configurations

    /// <summary>
    /// Applies a minimal configuration suitable for basic health checks.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithMinimalConfiguration();

    /// <summary>
    /// Applies a standard configuration suitable for most health checks.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithStandardConfiguration();

    /// <summary>
    /// Applies a comprehensive configuration suitable for critical health checks.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithComprehensiveConfiguration();

    /// <summary>
    /// Applies a high-frequency configuration suitable for real-time monitoring.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithHighFrequencyConfiguration();

    /// <summary>
    /// Applies a low-frequency configuration suitable for resource-intensive checks.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithLowFrequencyConfiguration();

    /// <summary>
    /// Applies configuration optimized for critical infrastructure components.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithCriticalInfrastructureConfiguration();

    /// <summary>
    /// Applies configuration optimized for external service dependencies.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    TBuilder WithExternalServiceConfiguration();

    #endregion

    #region Validation and Building

    /// <summary>
    /// Validates the current configuration without building.
    /// </summary>
    /// <returns>The validation result indicating any issues.</returns>
    ConfigurationValidationResult Validate();

    /// <summary>
    /// Gets the current state of the configuration being built.
    /// </summary>
    /// <returns>A dictionary representation of the current configuration state.</returns>
    IReadOnlyDictionary<string, object> GetCurrentState();

    /// <summary>
    /// Resets the builder to its initial state.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    TBuilder Reset();

    /// <summary>
    /// Creates a copy of this builder with the same configuration.
    /// </summary>
    /// <returns>A new builder instance with the same configuration.</returns>
    TBuilder Clone();

    /// <summary>
    /// Applies configuration from an existing configuration object.
    /// </summary>
    /// <param name="config">The configuration to copy settings from.</param>
    /// <param name="overwriteExisting">Whether to overwrite existing non-default values.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    TBuilder FromExisting(IHealthCheckConfig config, bool overwriteExisting = true);

    /// <summary>
    /// Builds the final health check configuration.
    /// </summary>
    /// <param name="messageBusService">The message bus service for configuration events.</param>
    /// <returns>The configured health check configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when messageBusService is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the configuration is invalid.</exception>
    TConfig Build(IMessageBusService messageBusService);

    /// <summary>
    /// Builds the final health check configuration with validation disabled.
    /// </summary>
    /// <param name="messageBusService">The message bus service for configuration events.</param>
    /// <returns>The configured health check configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when messageBusService is null.</exception>
    TConfig BuildWithoutValidation(IMessageBusService messageBusService);

    #endregion
}