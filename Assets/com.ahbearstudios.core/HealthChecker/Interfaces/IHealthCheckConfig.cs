using System;
using System.Collections.Generic;
using AhBearStudios.Core.HealthCheck.Messages;
using Unity.Collections;
using AhBearStudios.Core.HealthCheck.Models;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.HealthCheck.Interfaces;

/// <summary>
/// Defines the contract for health check configuration that controls behavior, timing,
/// and response characteristics of individual health checks within the system.
/// Provides comprehensive configuration options for enterprise-grade health monitoring
/// with support for alerting, remediation, retry policies, and performance tuning.
/// Integrates with the message bus system for event-driven configuration management.
/// </summary>
public interface IHealthCheckConfig
{
    #region Basic Configuration

    /// <summary>
    /// Gets or sets the unique identifier for this health check configuration.
    /// Used for tracking, logging, and configuration management purposes.
    /// </summary>
    string ConfigId { get; set; }

    /// <summary>
    /// Gets the message bus service used for publishing configuration change events.
    /// </summary>
    IMessageBusService MessageBusService { get; }

    /// <summary>
    /// Gets or sets the display name for this health check.
    /// Used in user interfaces, reports, and notifications.
    /// </summary>
    string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description of what this health check monitors.
    /// Provides context for operators and documentation systems.
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Gets or sets whether this health check is currently enabled.
    /// Disabled health checks are not executed during automatic runs.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the category that this health check belongs to.
    /// Used for grouping, filtering, and targeted execution.
    /// </summary>
    FixedString64Bytes Category { get; set; }

    /// <summary>
    /// Gets or sets optional tags for additional categorization and filtering.
    /// Supports comma-separated values for multiple tags.
    /// </summary>
    FixedString64Bytes Tags { get; set; }

    #endregion

    #region Execution Configuration

    /// <summary>
    /// Gets or sets the interval between automatic executions of this health check.
    /// Set to TimeSpan.Zero to disable automatic execution.
    /// </summary>
    TimeSpan ExecutionInterval { get; set; }

    /// <summary>
    /// Gets or sets the maximum time allowed for this health check to execute.
    /// Executions exceeding this timeout will be considered failed.
    /// </summary>
    TimeSpan ExecutionTimeout { get; set; }

    /// <summary>
    /// Gets or sets the initial delay before the first execution of this health check.
    /// Useful for staggering health check execution during system startup.
    /// </summary>
    TimeSpan InitialDelay { get; set; }

    /// <summary>
    /// Gets or sets the priority level for this health check execution.
    /// Higher priority health checks are executed before lower priority ones.
    /// </summary>
    int Priority { get; set; }

    /// <summary>
    /// Gets or sets whether this health check should be executed in parallel with others.
    /// Sequential execution may be required for health checks that modify shared state.
    /// </summary>
    bool AllowParallelExecution { get; set; }

    /// <summary>
    /// Gets or sets the maximum degree of parallelism for this health check.
    /// Only applicable when AllowParallelExecution is true.
    /// </summary>
    int MaxDegreeOfParallelism { get; set; }

    #endregion

    #region Retry and Resilience Configuration

    /// <summary>
    /// Gets or sets whether retry logic is enabled for this health check.
    /// When enabled, failed health checks will be retried according to the retry policy.
    /// </summary>
    bool EnableRetry { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed health checks.
    /// Does not include the initial execution attempt.
    /// </summary>
    int MaxRetryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the base delay between retry attempts.
    /// The actual delay may be modified by the retry strategy.
    /// </summary>
    TimeSpan RetryDelay { get; set; }

    /// <summary>
    /// Gets or sets the retry strategy to use for failed health checks.
    /// Determines how retry delays are calculated and applied.
    /// </summary>
    HealthCheckRetryStrategy RetryStrategy { get; set; }

    /// <summary>
    /// Gets or sets the maximum delay between retry attempts.
    /// Prevents exponential backoff from creating excessively long delays.
    /// </summary>
    TimeSpan MaxRetryDelay { get; set; }

    /// <summary>
    /// Gets or sets whether to add random jitter to retry delays.
    /// Helps prevent thundering herd effects when multiple health checks fail simultaneously.
    /// </summary>
    bool UseRetryJitter { get; set; }

    #endregion

    #region Alerting Configuration

    /// <summary>
    /// Gets or sets whether alerting is enabled for this health check.
    /// When disabled, no alerts will be generated regardless of health check results.
    /// </summary>
    bool EnableAlerting { get; set; }

    /// <summary>
    /// Gets or sets the minimum severity level that will trigger an alert.
    /// Health check results below this severity will not generate alerts.
    /// </summary>
    HealthSeverity AlertSeverityThreshold { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of consecutive failures required to trigger an alert.
    /// Helps prevent alert storms from transient failures.
    /// </summary>
    int AlertFailureThreshold { get; set; }

    /// <summary>
    /// Gets or sets the time window for evaluating alert conditions.
    /// Failures must occur within this window to count toward the threshold.
    /// </summary>
    TimeSpan AlertEvaluationWindow { get; set; }

    /// <summary>
    /// Gets or sets whether to suppress duplicate alerts for the same condition.
    /// When enabled, additional alerts will not be sent until the condition clears.
    /// </summary>
    bool SuppressDuplicateAlerts { get; set; }

    /// <summary>
    /// Gets or sets the minimum time between alert notifications for this health check.
    /// Prevents alert spam by enforcing a cooldown period.
    /// </summary>
    TimeSpan AlertCooldownPeriod { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically escalate alerts after a specified time.
    /// Escalated alerts may trigger additional notification channels or procedures.
    /// </summary>
    bool EnableAlertEscalation { get; set; }

    /// <summary>
    /// Gets or sets the time after which unresolved alerts will be escalated.
    /// Only applicable when EnableAlertEscalation is true.
    /// </summary>
    TimeSpan AlertEscalationDelay { get; set; }

    #endregion

    #region Remediation Configuration

    /// <summary>
    /// Gets or sets whether automatic remediation is enabled for this health check.
    /// When enabled, registered remediation handlers may attempt to fix detected issues.
    /// </summary>
    bool EnableRemediation { get; set; }

    /// <summary>
    /// Gets or sets the minimum severity level that will trigger automatic remediation.
    /// Health check results below this severity will not trigger remediation attempts.
    /// </summary>
    HealthSeverity RemediationSeverityThreshold { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of remediation attempts for a single failure.
    /// Prevents infinite remediation loops for persistent issues.
    /// </summary>
    int MaxRemediationAttempts { get; set; }

    /// <summary>
    /// Gets or sets the delay between remediation attempts.
    /// Allows time for remediation actions to take effect before retrying.
    /// </summary>
    TimeSpan RemediationDelay { get; set; }

    /// <summary>
    /// Gets or sets whether to re-execute the health check after successful remediation.
    /// Verifies that remediation actions have resolved the detected issue.
    /// </summary>
    bool VerifyAfterRemediation { get; set; }

    /// <summary>
    /// Gets or sets the delay before re-executing the health check after remediation.
    /// Allows time for remediation actions to fully take effect.
    /// </summary>
    TimeSpan RemediationVerificationDelay { get; set; }

    #endregion

    #region Performance and Resource Configuration

    /// <summary>
    /// Gets or sets the expected normal execution time for this health check.
    /// Used for performance monitoring and alerting on slow executions.
    /// </summary>
    TimeSpan ExpectedExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the execution time threshold for slow execution warnings.
    /// Health checks exceeding this time will generate slow execution alerts.
    /// </summary>
    TimeSpan SlowExecutionThreshold { get; set; }

    /// <summary>
    /// Gets or sets the maximum memory usage allowed for this health check.
    /// Set to zero to disable memory monitoring.
    /// </summary>
    long MaxMemoryUsageBytes { get; set; }

    /// <summary>
    /// Gets or sets the maximum CPU usage percentage allowed for this health check.
    /// Set to zero to disable CPU monitoring.
    /// </summary>
    double MaxCpuUsagePercent { get; set; }

    /// <summary>
    /// Gets or sets whether to collect detailed performance metrics for this health check.
    /// Enabling this may impact performance but provides valuable diagnostic information.
    /// </summary>
    bool CollectPerformanceMetrics { get; set; }

    /// <summary>
    /// Gets or sets the number of execution history entries to retain.
    /// Used for trend analysis and performance monitoring.
    /// </summary>
    int HistoryRetentionCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum age of history entries to retain.
    /// Older entries will be automatically purged to manage memory usage.
    /// </summary>
    TimeSpan HistoryRetentionPeriod { get; set; }

    #endregion

    #region Dependency and Environment Configuration

    /// <summary>
    /// Gets or sets the names of other health checks that must be healthy before this one executes.
    /// Supports dependency-based execution ordering and conditional health checking.
    /// </summary>
    IReadOnlyList<FixedString64Bytes> Dependencies { get; set; }

    /// <summary>
    /// Gets or sets whether to skip execution if dependencies are unhealthy.
    /// When false, the health check will execute regardless of dependency status.
    /// </summary>
    bool SkipOnUnhealthyDependencies { get; set; }

    /// <summary>
    /// Gets or sets the environments where this health check should be active.
    /// Empty list means active in all environments.
    /// </summary>
    IReadOnlyList<string> ActiveEnvironments { get; set; }

    /// <summary>
    /// Gets or sets the minimum system resources required to execute this health check.
    /// Health check may be skipped if system resources are below these thresholds.
    /// </summary>
    SystemResourceRequirements MinimumSystemResources { get; set; }

    /// <summary>
    /// Gets or sets custom properties specific to the health check implementation.
    /// Allows health check implementations to define their own configuration parameters.
    /// </summary>
    IReadOnlyDictionary<string, object> CustomProperties { get; set; }

    #endregion

    #region Validation and Lifecycle

    /// <summary>
    /// Validates that all configuration values are valid and consistent.
    /// Publishes a HealthCheckValidationFailedMessage if validation fails.
    /// </summary>
    /// <returns>A validation result indicating any configuration errors.</returns>
    ConfigurationValidationResult Validate();

    /// <summary>
    /// Creates a deep copy of this configuration with all current values.
    /// </summary>
    /// <returns>A new configuration instance with the same settings.</returns>
    IHealthCheckConfig Clone();

    /// <summary>
    /// Merges settings from another configuration into this one.
    /// Publishes a HealthCheckConfigurationChangedMessage when changes are applied.
    /// Null or default values in the source configuration are ignored.
    /// </summary>
    /// <param name="source">The configuration to merge settings from.</param>
    /// <param name="overwriteExisting">Whether to overwrite existing non-default values.</param>
    /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
    void MergeFrom(IHealthCheckConfig source, bool overwriteExisting = false);

    /// <summary>
    /// Resets all configuration values to their defaults.
    /// Publishes a HealthCheckConfigurationChangedMessage when reset is complete.
    /// </summary>
    void ResetToDefaults();

    /// <summary>
    /// Gets a dictionary representation of all configuration values.
    /// Useful for serialization, logging, and external configuration systems.
    /// </summary>
    /// <returns>A dictionary containing all configuration key-value pairs.</returns>
    IReadOnlyDictionary<string, object> ToDictionary();

    /// <summary>
    /// Loads configuration values from a dictionary representation.
    /// Publishes a HealthCheckConfigurationChangedMessage when changes are applied.
    /// Typically used with configuration systems and deserialization.
    /// </summary>
    /// <param name="values">The dictionary containing configuration values.</param>
    /// <param name="ignoreUnknownKeys">Whether to ignore keys that don't map to known properties.</param>
    /// <exception cref="ArgumentNullException">Thrown when values is null.</exception>
    /// <exception cref="ArgumentException">Thrown when required values are missing or invalid.</exception>
    void FromDictionary(IReadOnlyDictionary<string, object> values, bool ignoreUnknownKeys = true);

    #endregion

}