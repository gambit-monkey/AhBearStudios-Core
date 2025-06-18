using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Models
{
    /// <summary>
    /// A result from executing a health check.
    /// </summary>
    public struct HealthCheckResult
    {
        public FixedString64Bytes Name;
        public HealthStatus Status;
        public FixedString128Bytes Message;
        public double TimestampUtc;
        public FixedString64Bytes SourceSystem;
        public FixedString64Bytes CorrelationId;
        public FixedString64Bytes Category;
    }
}