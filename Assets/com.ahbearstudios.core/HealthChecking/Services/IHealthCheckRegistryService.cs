using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Specialized service responsible for managing health check registration and storage.
    /// Handles registration, unregistration, and retrieval of health checks and their configurations.
    /// Follows CLAUDE.md patterns for specialized service delegation.
    /// </summary>
    public interface IHealthCheckRegistryService
    {
        /// <summary>
        /// Registers a health check with its configuration.
        /// </summary>
        /// <param name="healthCheck">The health check to register</param>
        /// <param name="configuration">Optional configuration for the health check</param>
        /// <exception cref="System.ArgumentNullException">Thrown when healthCheck is null</exception>
        /// <exception cref="System.InvalidOperationException">Thrown when a health check with the same name already exists</exception>
        void RegisterHealthCheck(IHealthCheck healthCheck, HealthCheckConfiguration configuration = null);

        /// <summary>
        /// Registers multiple health checks in a single operation.
        /// </summary>
        /// <param name="healthChecks">Dictionary of health checks with their configurations</param>
        /// <exception cref="System.ArgumentNullException">Thrown when healthChecks is null</exception>
        void RegisterHealthChecks(Dictionary<IHealthCheck, HealthCheckConfiguration> healthChecks);

        /// <summary>
        /// Unregisters a health check by name.
        /// </summary>
        /// <param name="name">Name of the health check to unregister</param>
        /// <returns>True if the health check was found and removed, false otherwise</returns>
        bool UnregisterHealthCheck(FixedString64Bytes name);

        /// <summary>
        /// Unregisters all health checks.
        /// </summary>
        /// <returns>Number of health checks that were removed</returns>
        int UnregisterAllHealthChecks();

        /// <summary>
        /// Gets a health check by name.
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <returns>The health check if found, null otherwise</returns>
        IHealthCheck GetHealthCheck(FixedString64Bytes name);

        /// <summary>
        /// Gets the configuration for a specific health check.
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <returns>The configuration if found, null otherwise</returns>
        HealthCheckConfiguration GetHealthCheckConfiguration(FixedString64Bytes name);

        /// <summary>
        /// Gets all registered health checks with their configurations.
        /// </summary>
        /// <returns>Dictionary of health checks with their configurations</returns>
        Dictionary<IHealthCheck, HealthCheckConfiguration> GetAllHealthChecks();

        /// <summary>
        /// Gets names of all registered health checks.
        /// </summary>
        /// <returns>List of health check names</returns>
        List<FixedString64Bytes> GetRegisteredHealthCheckNames();

        /// <summary>
        /// Gets health checks by category.
        /// </summary>
        /// <param name="category">Category to filter by</param>
        /// <returns>List of health checks in the specified category</returns>
        List<IHealthCheck> GetHealthChecksByCategory(HealthCheckCategory category);

        /// <summary>
        /// Checks if a health check is registered.
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <returns>True if registered, false otherwise</returns>
        bool IsHealthCheckRegistered(FixedString64Bytes name);

        /// <summary>
        /// Checks if a health check is enabled.
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <returns>True if enabled, false if disabled or not found</returns>
        bool IsHealthCheckEnabled(FixedString64Bytes name);

        /// <summary>
        /// Sets whether a health check is enabled.
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="enabled">Whether to enable or disable the check</param>
        /// <returns>True if the state was changed, false if health check not found</returns>
        bool SetHealthCheckEnabled(FixedString64Bytes name, bool enabled);

        /// <summary>
        /// Updates the configuration for a health check.
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="configuration">New configuration</param>
        /// <returns>True if updated, false if health check not found</returns>
        bool UpdateHealthCheckConfiguration(FixedString64Bytes name, HealthCheckConfiguration configuration);

        /// <summary>
        /// Gets the total count of registered health checks.
        /// </summary>
        /// <returns>Number of registered health checks</returns>
        int GetHealthCheckCount();

        /// <summary>
        /// Gets the count of enabled health checks.
        /// </summary>
        /// <returns>Number of enabled health checks</returns>
        int GetEnabledHealthCheckCount();

        /// <summary>
        /// Gets metadata for a specific health check.
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <returns>Metadata dictionary</returns>
        Dictionary<string, object> GetHealthCheckMetadata(FixedString64Bytes name);
    }
}