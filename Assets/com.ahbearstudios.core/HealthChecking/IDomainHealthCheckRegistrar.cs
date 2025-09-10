using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;

namespace AhBearStudios.Core.HealthChecking;

/// <summary>
/// Interface for domain health check registration following the IPooledObject pattern.
/// Each domain (Database, Network, Messaging, etc.) can implement this to self-register
/// their health checks with the HealthCheckService automatically.
/// </summary>
/// <remarks>
/// This pattern allows domains to manage their own health check registration and configuration
/// while maintaining loose coupling with the core HealthCheckService. Similar to how domains
/// register their pooled objects with IPoolingService.
/// </remarks>
public interface IDomainHealthCheckRegistrar
{
    /// <summary>
    /// Gets the domain name for this registrar (e.g., "Database", "Network", "Messaging")
    /// </summary>
    string DomainName { get; }

    /// <summary>
    /// Gets the priority for this domain's health checks.
    /// Higher values are registered first.
    /// </summary>
    int RegistrationPriority { get; }

    /// <summary>
    /// Registers all health checks for this domain with the provided health check service.
    /// </summary>
    /// <param name="healthCheckService">The health check service to register with</param>
    /// <param name="serviceConfig">Optional service configuration for domain-specific settings</param>
    /// <returns>Dictionary of registered health checks with their configurations</returns>
    Dictionary<IHealthCheck, HealthCheckConfiguration> RegisterHealthChecks(
        IHealthCheckService healthCheckService, 
        HealthCheckServiceConfig serviceConfig = null);

    /// <summary>
    /// Unregisters all health checks for this domain from the provided health check service.
    /// </summary>
    /// <param name="healthCheckService">The health check service to unregister from</param>
    void UnregisterHealthChecks(IHealthCheckService healthCheckService);

    /// <summary>
    /// Gets health check configurations for this domain without registering them.
    /// Useful for testing or inspection purposes.
    /// </summary>
    /// <param name="serviceConfig">Optional service configuration for domain-specific settings</param>
    /// <returns>Dictionary of health checks with their configurations</returns>
    Dictionary<IHealthCheck, HealthCheckConfiguration> GetHealthCheckConfigurations(
        HealthCheckServiceConfig serviceConfig = null);
}