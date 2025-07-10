using AhBearStudios.Core.HealthCheck.Models;

namespace AhBearStudios.Core.HealthCheck.Healing
{
    /// <summary>
    /// Defines remediation for degraded or unhealthy checks.
    /// </summary>
    public interface IHealthRemediation
    {
        bool CanRemediate(HealthCheckResult result);
        void AttemptRemediation(HealthCheckResult result);
    }
}