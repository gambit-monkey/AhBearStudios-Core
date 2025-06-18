using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Interfaces
{
    /// <summary>
    /// Contract for a single health check operation.
    /// </summary>
    public interface IHealthCheck
    {
        FixedString64Bytes Name { get; }
        FixedString64Bytes Category { get; }
        HealthCheckResult Execute(in double timestampUtc);
    }
}