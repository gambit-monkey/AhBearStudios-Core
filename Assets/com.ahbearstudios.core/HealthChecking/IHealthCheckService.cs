using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking;

/// <summary>
/// Interface for health check service (placeholder for core system integration).
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Registers a health check with the service.
    /// </summary>
    /// <param name="healthCheck">The health check to register</param>
    void RegisterHealthCheck(IHealthCheck healthCheck);

    /// <summary>
    /// Gets the overall health status of all registered checks.
    /// </summary>
    /// <returns>The overall health status</returns>
    Task<HealthCheckResult> GetOverallHealthStatusAsync();
}