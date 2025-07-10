using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Interfaces
{
    /// <summary>
    /// Broadcasts results to configured targets.
    /// </summary>
    public interface IHealthCheckReporter
    {
        void AddTarget(IHealthCheckTarget target);
        void RemoveTarget(IHealthCheckTarget target);
        bool HasTarget(FixedString64Bytes id);
        void Report(in HealthCheckResult result);
    }
}