using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.com.ahbearstudios.core.HealthChecker.Interfaces;
using Unity.Collections;
using AhBearStudios.Core.HealthCheck.Models;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Logging.Interfaces;

namespace AhBearStudios.Core.HealthCheck.Interfaces;

/// <summary>
/// Central service interface for coordinating health check operations across the system.
/// Provides comprehensive health monitoring, alerting, and remediation capabilities
/// with support for async operations, bulk processing, and real-time status reporting.
/// Integrates with the message bus for event-driven health monitoring and logging systems.
/// </summary>
public interface IHealthCheckService : IDisposable
{
    #region Service State and Configuration

    /// <summary>
    /// Gets whether the health check service is currently running and accepting operations.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets whether the health check service has been properly initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets the current configuration of the health check service.
    /// </summary>
    IHealthCheckSystemConfig Configuration { get; }

    /// <summary>
    /// Gets the message bus service used for publishing health check events.
    /// </summary>
    IMessageBusService MessageBusService { get; }

    /// <summary>
    /// Gets the logging service used for health check operation logging.
    /// </summary>
    ILoggingService Logger { get; }

    /// <summary>
    /// Gets statistics about health check operations since service start.
    /// </summary>
    HealthCheckServiceStatistics Statistics { get; }

    #endregion

    #region Service Lifecycle

    /// <summary>
    /// Initializes the health check service with the specified configuration.
    /// Must be called before any other operations.
    /// </summary>
    /// <param name="configuration">The configuration to use for the service.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when initialization is finished.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when service is already initialized.</exception>
    Task InitializeAsync(IHealthCheckSystemConfig configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the health check service and begins automatic health check execution.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the service has started.</returns>
    /// <exception cref="InvalidOperationException">Thrown when service is not initialized or already running.</exception>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the health check service and cancels any ongoing operations.
    /// </summary>
    /// <param name="gracefulShutdown">Whether to wait for current operations to complete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the service has stopped.</returns>
    Task StopAsync(bool gracefulShutdown = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restarts the health check service with optional new configuration.
    /// </summary>
    /// <param name="newConfiguration">Optional new configuration to apply during restart.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the service has restarted.</returns>
    Task RestartAsync(IHealthCheckSystemConfig newConfiguration = null, CancellationToken cancellationToken = default);

    #endregion

    #region Health Check Registration

    /// <summary>
    /// Registers a health check with the service for automatic execution.
    /// </summary>
    /// <param name="healthCheck">The health check to register.</param>
    /// <param name="config">Optional configuration for this specific health check.</param>
    /// <returns>A registration token that can be used to unregister the health check.</returns>
    /// <exception cref="ArgumentNullException">Thrown when healthCheck is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a health check with the same name is already registered.</exception>
    IHealthCheckRegistration RegisterHealthCheck(IHealthCheck healthCheck, IHealthCheckConfig config = null);

    /// <summary>
    /// Registers multiple health checks in a single operation.
    /// </summary>
    /// <param name="healthChecks">The health checks to register.</param>
    /// <param name="defaultConfig">Default configuration to apply to all health checks.</param>
    /// <returns>A collection of registration tokens for the registered health checks.</returns>
    /// <exception cref="ArgumentNullException">Thrown when healthChecks is null.</exception>
    IReadOnlyList<IHealthCheckRegistration> RegisterHealthChecks(
        IEnumerable<IHealthCheck> healthChecks, 
        IHealthCheckConfig defaultConfig = null);

    /// <summary>
    /// Unregisters a health check using its registration token.
    /// </summary>
    /// <param name="registration">The registration token returned during registration.</param>
    /// <returns>True if the health check was successfully unregistered; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when registration is null.</exception>
    bool UnregisterHealthCheck(IHealthCheckRegistration registration);

    /// <summary>
    /// Unregisters a health check by its name.
    /// </summary>
    /// <param name="name">The name of the health check to unregister.</param>
    /// <returns>True if the health check was successfully unregistered; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name is null or empty.</exception>
    bool UnregisterHealthCheck(FixedString64Bytes name);

    /// <summary>
    /// Gets all currently registered health checks.
    /// </summary>
    /// <returns>A read-only collection of health check registrations.</returns>
    IReadOnlyCollection<IHealthCheckRegistration> GetRegisteredHealthChecks();

    /// <summary>
    /// Checks if a health check with the specified name is registered.
    /// </summary>
    /// <param name="name">The name of the health check to check for.</param>
    /// <returns>True if a health check with the name is registered; otherwise, false.</returns>
    bool IsHealthCheckRegistered(FixedString64Bytes name);

    #endregion

    #region Health Check Execution

    /// <summary>
    /// Executes a single health check by name and returns the result.
    /// </summary>
    /// <param name="name">The name of the health check to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The result of the health check execution.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no health check with the name is registered.</exception>
    Task<HealthCheckResult> ExecuteHealthCheckAsync(FixedString64Bytes name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes multiple health checks by name and returns their results.
    /// </summary>
    /// <param name="names">The names of the health checks to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of health check results.</returns>
    /// <exception cref="ArgumentNullException">Thrown when names is null.</exception>
    Task<IReadOnlyList<HealthCheckResult>> ExecuteHealthChecksAsync(
        IEnumerable<FixedString64Bytes> names, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all registered health checks and returns their results.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of all health check results.</returns>
    Task<IReadOnlyList<HealthCheckResult>> ExecuteAllHealthChecksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes health checks in a specific category and returns their results.
    /// </summary>
    /// <param name="category">The category of health checks to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of health check results for the specified category.</returns>
    /// <exception cref="ArgumentNullException">Thrown when category is null or empty.</exception>
    Task<IReadOnlyList<HealthCheckResult>> ExecuteHealthChecksByCategoryAsync(
        FixedString64Bytes category, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes health checks with a minimum severity level and returns their results.
    /// </summary>
    /// <param name="minimumSeverity">The minimum severity level to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of health check results meeting the severity criteria.</returns>
    Task<IReadOnlyList<HealthCheckResult>> ExecuteHealthChecksBySeverityAsync(
        HealthSeverity minimumSeverity, 
        CancellationToken cancellationToken = default);

    #endregion

    #region Health Status and Reporting

    /// <summary>
    /// Gets the overall health status of the system based on all registered health checks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The overall system health status.</returns>
    Task<OverallHealthStatus> GetOverallHealthStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the health status for a specific category of health checks.
    /// </summary>
    /// <param name="category">The category to get the health status for.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The health status for the specified category.</returns>
    /// <exception cref="ArgumentNullException">Thrown when category is null or empty.</exception>
    Task<CategoryHealthStatus> GetCategoryHealthStatusAsync(
        FixedString64Bytes category, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest result for a specific health check.
    /// </summary>
    /// <param name="name">The name of the health check to get the result for.</param>
    /// <returns>The latest health check result, or null if no result is available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name is null or empty.</exception>
    HealthCheckResult? GetLatestResult(FixedString64Bytes name);

    /// <summary>
    /// Gets the latest results for all registered health checks.
    /// </summary>
    /// <returns>A dictionary mapping health check names to their latest results.</returns>
    IReadOnlyDictionary<FixedString64Bytes, HealthCheckResult> GetAllLatestResults();

    /// <summary>
    /// Gets historical results for a specific health check.
    /// </summary>
    /// <param name="name">The name of the health check to get history for.</param>
    /// <param name="maxResults">The maximum number of historical results to return.</param>
    /// <returns>A collection of historical health check results in descending chronological order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxResults is less than 1.</exception>
    IReadOnlyList<HealthCheckResult> GetHealthCheckHistory(FixedString64Bytes name, int maxResults = 100);

    #endregion

    #region Alerting and Notifications

    /// <summary>
    /// Registers an alert handler that will be notified when health check results meet specified criteria.
    /// </summary>
    /// <param name="handler">The alert handler to register.</param>
    /// <param name="criteria">The criteria that must be met to trigger the alert.</param>
    /// <returns>A registration token that can be used to unregister the alert handler.</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler or criteria is null.</exception>
    IAlertHandlerRegistration RegisterAlertHandler(IHealthCheckAlertHandler handler, AlertCriteria criteria);

    /// <summary>
    /// Unregisters an alert handler using its registration token.
    /// </summary>
    /// <param name="registration">The registration token returned during registration.</param>
    /// <returns>True if the alert handler was successfully unregistered; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when registration is null.</exception>
    bool UnregisterAlertHandler(IAlertHandlerRegistration registration);

    /// <summary>
    /// Manually triggers an alert for testing or administrative purposes.
    /// </summary>
    /// <param name="result">The health check result to trigger an alert for.</param>
    /// <param name="reason">The reason for manually triggering the alert.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the alert has been processed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when reason is null or empty.</exception>
    Task TriggerManualAlertAsync(HealthCheckResult result, string reason, CancellationToken cancellationToken = default);

    #endregion

    #region Remediation and Self-Healing

    /// <summary>
    /// Registers a remediation handler that will attempt to fix issues when health checks fail.
    /// </summary>
    /// <param name="handler">The remediation handler to register.</param>
    /// <param name="criteria">The criteria that must be met to trigger remediation.</param>
    /// <returns>A registration token that can be used to unregister the remediation handler.</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler or criteria is null.</exception>
    IRemediationHandlerRegistration RegisterRemediationHandler(
        IHealthCheckRemediationHandler handler, 
        RemediationCriteria criteria);

    /// <summary>
    /// Unregisters a remediation handler using its registration token.
    /// </summary>
    /// <param name="registration">The registration token returned during registration.</param>
    /// <returns>True if the remediation handler was successfully unregistered; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when registration is null.</exception>
    bool UnregisterRemediationHandler(IRemediationHandlerRegistration registration);

    /// <summary>
    /// Manually triggers remediation for a specific health check result.
    /// </summary>
    /// <param name="result">The health check result to trigger remediation for.</param>
    /// <param name="reason">The reason for manually triggering remediation.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when remediation has been attempted.</returns>
    /// <exception cref="ArgumentNullException">Thrown when reason is null or empty.</exception>
    Task<RemediationResult> TriggerManualRemediationAsync(
        HealthCheckResult result, 
        string reason, 
        CancellationToken cancellationToken = default);

    #endregion

    #region Configuration and Management

    /// <summary>
    /// Updates the configuration of a specific health check.
    /// </summary>
    /// <param name="name">The name of the health check to update.</param>
    /// <param name="config">The new configuration to apply.</param>
    /// <returns>True if the configuration was successfully updated; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name is null or config is null.</exception>
    bool UpdateHealthCheckConfiguration(FixedString64Bytes name, IHealthCheckConfig config);

    /// <summary>
    /// Updates the global service configuration.
    /// </summary>
    /// <param name="config">The new configuration to apply.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the configuration has been updated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    Task UpdateServiceConfigurationAsync(IHealthCheckSystemConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables a specific health check.
    /// </summary>
    /// <param name="name">The name of the health check to enable or disable.</param>
    /// <param name="enabled">True to enable the health check; false to disable it.</param>
    /// <returns>True if the health check state was successfully changed; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name is null or empty.</exception>
    bool SetHealthCheckEnabled(FixedString64Bytes name, bool enabled);

    /// <summary>
    /// Gets the configuration for a specific health check.
    /// </summary>
    /// <param name="name">The name of the health check to get the configuration for.</param>
    /// <returns>The configuration for the specified health check, or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name is null or empty.</exception>
    IHealthCheckConfig GetHealthCheckConfiguration(FixedString64Bytes name);

    #endregion

    #region Events and Observability

    /// <summary>
    /// Event raised when a health check is executed.
    /// </summary>
    event EventHandler<HealthCheckExecutedEventArgs> HealthCheckExecuted;

    /// <summary>
    /// Event raised when a health check fails.
    /// </summary>
    event EventHandler<HealthCheckFailedEventArgs> HealthCheckFailed;

    /// <summary>
    /// Event raised when the overall system health status changes.
    /// </summary>
    event EventHandler<OverallHealthStatusChangedEventArgs> OverallHealthStatusChanged;

    /// <summary>
    /// Event raised when a health check is registered.
    /// </summary>
    event EventHandler<HealthCheckRegisteredEventArgs> HealthCheckRegistered;

    /// <summary>
    /// Event raised when a health check is unregistered.
    /// </summary>
    event EventHandler<HealthCheckUnregisteredEventArgs> HealthCheckUnregistered;

    /// <summary>
    /// Event raised when the service configuration is updated.
    /// </summary>
    event EventHandler<ServiceConfigurationUpdatedEventArgs> ServiceConfigurationUpdated;

    #endregion

    #region Diagnostics and Maintenance

    /// <summary>
    /// Performs a diagnostic check of the health check service itself.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A diagnostic result indicating the health of the service.</returns>
    Task<ServiceDiagnosticResult> PerformSelfDiagnosticAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the history for a specific health check.
    /// </summary>
    /// <param name="name">The name of the health check to clear history for.</param>
    /// <returns>True if the history was successfully cleared; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name is null or empty.</exception>
    bool ClearHealthCheckHistory(FixedString64Bytes name);

    /// <summary>
    /// Clears the history for all health checks.
    /// </summary>
    void ClearAllHealthCheckHistory();

    /// <summary>
    /// Exports health check data for analysis or backup purposes.
    /// </summary>
    /// <param name="format">The format to export the data in.</param>
    /// <param name="includeHistory">Whether to include historical data in the export.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The exported health check data in the specified format.</returns>
    Task<HealthCheckExportData> ExportHealthCheckDataAsync(
        HealthCheckExportFormat format = HealthCheckExportFormat.Json,
        bool includeHistory = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the integrity of all registered health checks and their configurations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A validation result indicating any issues found.</returns>
    Task<HealthCheckValidationResult> ValidateHealthChecksAsync(CancellationToken cancellationToken = default);

    #endregion
}