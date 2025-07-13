using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.HealthCheck.Models;
using AhBearStudios.Core.HealthCheck.Checks;

namespace AhBearStudios.Core.HealthCheck
{
    /// <summary>
    /// Enhanced health check service providing comprehensive system health monitoring,
    /// circuit breaker protection, and graceful degradation capabilities.
    /// </summary>
    public interface IHealthCheckService
    {
        /// <summary>
        /// Event triggered when overall health status changes
        /// </summary>
        event EventHandler<HealthStatusChangedEventArgs> HealthStatusChanged;
        
        /// <summary>
        /// Event triggered when circuit breaker state changes
        /// </summary>
        event EventHandler<CircuitBreakerStateChangedEventArgs> CircuitBreakerStateChanged;
        
        /// <summary>
        /// Event triggered when system degradation level changes
        /// </summary>
        event EventHandler<DegradationStatusChangedEventArgs> DegradationStatusChanged;

        /// <summary>
        /// Registers a health check with the service
        /// </summary>
        /// <param name="healthCheck">The health check to register</param>
        /// <param name="config">Optional configuration for the health check</param>
        /// <exception cref="ArgumentNullException">Thrown when healthCheck is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when a health check with the same name already exists</exception>
        void RegisterHealthCheck(IHealthCheck healthCheck, HealthCheckConfiguration config = null);

        /// <summary>
        /// Registers multiple health checks in a single operation
        /// </summary>
        /// <param name="healthChecks">Dictionary of health checks with their configurations</param>
        /// <exception cref="ArgumentNullException">Thrown when healthChecks is null</exception>
        void RegisterHealthChecks(Dictionary<IHealthCheck, HealthCheckConfiguration> healthChecks);

        /// <summary>
        /// Unregisters a health check from the service
        /// </summary>
        /// <param name="name">Name of the health check to unregister</param>
        /// <returns>True if the health check was found and removed, false otherwise</returns>
        bool UnregisterHealthCheck(FixedString64Bytes name);

        /// <summary>
        /// Executes a specific health check by name
        /// </summary>
        /// <param name="name">Name of the health check to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check result</returns>
        /// <exception cref="ArgumentException">Thrown when health check name is not found</exception>
        Task<HealthCheckResult> ExecuteHealthCheckAsync(FixedString64Bytes name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes all registered health checks
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Comprehensive health report</returns>
        Task<HealthReport> ExecuteAllHealthChecksAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the overall health status of the system
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Overall system health status</returns>
        Task<HealthStatus> GetOverallHealthStatusAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current degradation level of the system
        /// </summary>
        /// <returns>Current degradation level</returns>
        DegradationLevel GetCurrentDegradationLevel();

        /// <summary>
        /// Gets circuit breaker state for a specific operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>Circuit breaker state</returns>
        CircuitBreakerState GetCircuitBreakerState(FixedString64Bytes operationName);

        /// <summary>
        /// Gets all circuit breaker states
        /// </summary>
        /// <returns>Dictionary of operation names to circuit breaker states</returns>
        Dictionary<FixedString64Bytes, CircuitBreakerState> GetAllCircuitBreakerStates();

        /// <summary>
        /// Gets health check history for a specific check
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>List of historical health check results</returns>
        List<HealthCheckResult> GetHealthCheckHistory(FixedString64Bytes name, int maxResults = 100);

        /// <summary>
        /// Gets names of all registered health checks
        /// </summary>
        /// <returns>List of health check names</returns>
        List<FixedString64Bytes> GetRegisteredHealthCheckNames();

        /// <summary>
        /// Gets metadata for a specific health check
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <returns>Health check metadata</returns>
        Dictionary<string, object> GetHealthCheckMetadata(FixedString64Bytes name);

        /// <summary>
        /// Starts automatic health check execution with configured intervals
        /// </summary>
        void StartAutomaticChecks();

        /// <summary>
        /// Stops automatic health check execution
        /// </summary>
        void StopAutomaticChecks();

        /// <summary>
        /// Checks if automatic health checks are currently running
        /// </summary>
        /// <returns>True if automatic checks are running, false otherwise</returns>
        bool IsAutomaticChecksRunning();

        /// <summary>
        /// Forces circuit breaker to open state for a specific operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="reason">Reason for forcing the circuit breaker open</param>
        void ForceCircuitBreakerOpen(FixedString64Bytes operationName, string reason);

        /// <summary>
        /// Forces circuit breaker to closed state for a specific operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="reason">Reason for forcing the circuit breaker closed</param>
        void ForceCircuitBreakerClosed(FixedString64Bytes operationName, string reason);

        /// <summary>
        /// Sets the system degradation level manually
        /// </summary>
        /// <param name="level">Degradation level to set</param>
        /// <param name="reason">Reason for the degradation level change</param>
        void SetDegradationLevel(DegradationLevel level, string reason);

        /// <summary>
        /// Gets comprehensive system health statistics
        /// </summary>
        /// <returns>System health statistics</returns>
        HealthStatistics GetHealthStatistics();

        /// <summary>
        /// Checks if a specific health check is currently enabled
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <returns>True if enabled, false otherwise</returns>
        bool IsHealthCheckEnabled(FixedString64Bytes name);

        /// <summary>
        /// Enables or disables a specific health check
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="enabled">Whether to enable or disable the check</param>
        void SetHealthCheckEnabled(FixedString64Bytes name, bool enabled);
    }
}