using AhBearStudios.Core.HealthCheck.Interfaces;

namespace AhBearStudios.Core.HealthCheck.Discovery
{
    /// <summary>
    /// Marker for health checks that should be automatically registered.
    /// </summary>
    public interface IAutoRegisteringHealthCheck : IHealthCheck
    {
    }
}