using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Configs;
using Cysharp.Threading.Tasks;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Interface for managing health check configurations at runtime
    /// Integrates with core systems for proper logging, alerting, and message bus integration
    /// </summary>
    public interface IHealthCheckConfigurationManager
    {
        /// <summary>
        /// Gets the current service configuration
        /// </summary>
        IHealthCheckServiceConfig ServiceConfig { get; }

        /// <summary>
        /// Updates the service configuration with proper validation and notification
        /// </summary>
        /// <param name="newConfig">New service configuration</param>
        /// <param name="changedBy">User or system making the change</param>
        /// <returns>Task representing the configuration update operation</returns>
        UniTask UpdateServiceConfigurationAsync(IHealthCheckServiceConfig newConfig, string changedBy = "System");

        /// <summary>
        /// Gets a specific health check configuration by name
        /// </summary>
        /// <param name="name">Name of the health check configuration</param>
        /// <returns>Health check configuration if found, null otherwise</returns>
        HealthCheckConfiguration GetHealthCheckConfiguration(FixedString64Bytes name);

        /// <summary>
        /// Updates a health check configuration with proper validation and notification
        /// </summary>
        /// <param name="configuration">Updated health check configuration</param>
        /// <param name="changedBy">User or system making the change</param>
        /// <returns>Task representing the configuration update operation</returns>
        UniTask UpdateHealthCheckConfigurationAsync(HealthCheckConfiguration configuration, string changedBy = "System");

        /// <summary>
        /// Enables or disables a health check with proper notification
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="enabled">Whether to enable or disable</param>
        /// <param name="changedBy">User or system making the change</param>
        /// <returns>Task representing the operation</returns>
        UniTask SetHealthCheckEnabledAsync(FixedString64Bytes name, bool enabled, string changedBy = "System");

        /// <summary>
        /// Updates the interval for a health check with proper notification
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="newInterval">New execution interval</param>
        /// <param name="changedBy">User or system making the change</param>
        /// <returns>Task representing the operation</returns>
        UniTask UpdateHealthCheckIntervalAsync(FixedString64Bytes name, TimeSpan newInterval, string changedBy = "System");

        /// <summary>
        /// Gets all registered health check configurations
        /// </summary>
        /// <returns>Collection of all health check configurations</returns>
        IReadOnlyCollection<HealthCheckConfiguration> GetAllConfigurations();

        /// <summary>
        /// Validates a configuration before applying it
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>List of validation errors, empty if valid</returns>
        List<string> ValidateConfiguration(IHealthCheckServiceConfig config);

        /// <summary>
        /// Validates a health check configuration before applying it
        /// </summary>
        /// <param name="config">Health check configuration to validate</param>
        /// <returns>List of validation errors, empty if valid</returns>
        List<string> ValidateHealthCheckConfiguration(HealthCheckConfiguration config);

        /// <summary>
        /// Reloads configurations from storage (if supported)
        /// </summary>
        /// <param name="source">Source of the reload request</param>
        /// <returns>Task representing the reload operation</returns>
        UniTask ReloadConfigurationsAsync(string source = "System");
    }
}