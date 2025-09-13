using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Services
{
    /// <summary>
    /// Interface for centralized message bus health management.
    /// Coordinates health checks across all messaging services and provides unified health status.
    /// Focused on health coordination responsibilities only, following single responsibility principle.
    /// </summary>
    public interface IMessageBusHealthService : IDisposable
    {
        #region Health Status Management

        /// <summary>
        /// Gets the overall health status of the message bus system.
        /// </summary>
        /// <returns>Current overall health status</returns>
        HealthStatus GetOverallHealthStatus();

        /// <summary>
        /// Gets the health status of individual messaging services.
        /// </summary>
        /// <returns>Dictionary of service names to health statuses</returns>
        Dictionary<string, HealthStatus> GetServiceHealthStatuses();

        /// <summary>
        /// Forces a comprehensive health check of all services.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Overall health check result</returns>
        UniTask<HealthStatus> CheckOverallHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Forces a health check of a specific service.
        /// </summary>
        /// <param name="serviceName">Name of the service to check</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Service health check result</returns>
        UniTask<HealthStatus> CheckServiceHealthAsync(string serviceName, CancellationToken cancellationToken = default);

        #endregion

        #region Service Registration

        /// <summary>
        /// Registers a service for health monitoring.
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="healthCheckFunc">Function to check the service's health</param>
        void RegisterService(string serviceName, Func<CancellationToken, UniTask<HealthStatus>> healthCheckFunc);

        /// <summary>
        /// Unregisters a service from health monitoring.
        /// </summary>
        /// <param name="serviceName">Name of the service to unregister</param>
        void UnregisterService(string serviceName);

        /// <summary>
        /// Gets the names of all registered services.
        /// </summary>
        /// <returns>Collection of registered service names</returns>
        IEnumerable<string> GetRegisteredServiceNames();

        #endregion

        #region Health Check Configuration

        /// <summary>
        /// Sets the health check interval for the overall system.
        /// </summary>
        /// <param name="interval">Health check interval</param>
        void SetHealthCheckInterval(TimeSpan interval);

        /// <summary>
        /// Sets the health check interval for a specific service.
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="interval">Health check interval</param>
        void SetServiceHealthCheckInterval(string serviceName, TimeSpan interval);

        /// <summary>
        /// Gets the current health check configuration.
        /// </summary>
        /// <returns>Current health check configuration</returns>
        MessageBusHealthConfig GetHealthConfiguration();

        /// <summary>
        /// Updates the health check configuration.
        /// </summary>
        /// <param name="config">New health configuration</param>
        void UpdateHealthConfiguration(MessageBusHealthConfig config);

        #endregion

        #region Health History

        /// <summary>
        /// Gets the health status history for the overall system.
        /// </summary>
        /// <param name="duration">Duration of history to retrieve</param>
        /// <returns>Collection of health status changes</returns>
        IEnumerable<HealthStatusChange> GetHealthHistory(TimeSpan duration);

        /// <summary>
        /// Gets the health status history for a specific service.
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="duration">Duration of history to retrieve</param>
        /// <returns>Collection of health status changes</returns>
        IEnumerable<HealthStatusChange> GetServiceHealthHistory(string serviceName, TimeSpan duration);

        /// <summary>
        /// Clears the health status history.
        /// </summary>
        void ClearHealthHistory();

        #endregion

        #region Degraded Service Management

        /// <summary>
        /// Gets the list of services currently in degraded state.
        /// </summary>
        /// <returns>Collection of degraded service names</returns>
        IEnumerable<string> GetDegradedServices();

        /// <summary>
        /// Gets the list of services currently in unhealthy state.
        /// </summary>
        /// <returns>Collection of unhealthy service names</returns>
        IEnumerable<string> GetUnhealthyServices();

        /// <summary>
        /// Manually marks a service as healthy (for administrative override).
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="reason">Reason for the manual override</param>
        void MarkServiceHealthy(string serviceName, string reason);

        /// <summary>
        /// Manually marks a service as unhealthy (for administrative override).
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="reason">Reason for the manual override</param>
        void MarkServiceUnhealthy(string serviceName, string reason);

        #endregion

        #region Health Dependencies

        /// <summary>
        /// Sets up health dependencies between services.
        /// </summary>
        /// <param name="serviceName">Name of the dependent service</param>
        /// <param name="dependencies">Names of services this service depends on</param>
        void SetServiceDependencies(string serviceName, IEnumerable<string> dependencies);

        /// <summary>
        /// Gets the health dependencies for a service.
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <returns>Collection of dependency service names</returns>
        IEnumerable<string> GetServiceDependencies(string serviceName);

        /// <summary>
        /// Gets services that depend on the specified service.
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <returns>Collection of dependent service names</returns>
        IEnumerable<string> GetDependentServices(string serviceName);

        #endregion

        #region Events

        /// <summary>
        /// Event fired when the overall health status changes.
        /// </summary>
        event Action<HealthStatus, HealthStatus, string> OverallHealthStatusChanged;

        /// <summary>
        /// Event fired when a service health status changes.
        /// </summary>
        event Action<string, HealthStatus, HealthStatus, string> ServiceHealthStatusChanged;

        /// <summary>
        /// Event fired when a health check fails.
        /// </summary>
        event Action<string, Exception> HealthCheckFailed;

        /// <summary>
        /// Event fired when a service becomes unhealthy.
        /// </summary>
        event Action<string, string> ServiceBecameUnhealthy;

        /// <summary>
        /// Event fired when a service recovers to healthy state.
        /// </summary>
        event Action<string, string> ServiceRecovered;

        #endregion
    }

    /// <summary>
    /// Represents a health status change event.
    /// </summary>
    public sealed class HealthStatusChange
    {
        /// <summary>
        /// Gets or sets the service name (null for overall system).
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the previous health status.
        /// </summary>
        public HealthStatus PreviousStatus { get; set; }

        /// <summary>
        /// Gets or sets the current health status.
        /// </summary>
        public HealthStatus CurrentStatus { get; set; }

        /// <summary>
        /// Gets or sets when the change occurred.
        /// </summary>
        public DateTime ChangedAt { get; set; }

        /// <summary>
        /// Gets or sets the reason for the change.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets additional context about the change.
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        /// Returns a string representation of the health status change.
        /// </summary>
        /// <returns>Health status change summary string</returns>
        public override string ToString()
        {
            var service = string.IsNullOrEmpty(ServiceName) ? "System" : ServiceName;
            return $"HealthStatusChange[{service}]: {PreviousStatus} -> {CurrentStatus} at {ChangedAt:HH:mm:ss} ({Reason})";
        }
    }
}