using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Interfaces
{
    /// <summary>
    /// Registry for registering and running health checks.
    /// </summary>
    public interface IHealthCheckRegistry
    {
        void Register(IHealthCheck check);
        void Unregister(FixedString64Bytes name);
        bool Contains(FixedString64Bytes name);
        NativeList<HealthCheckResult> RunAllChecks(Allocator allocator, double timestampUtc);
    }
}