using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Models
{
    /// <summary>
    /// Predefined categories for grouping health checks.
    /// </summary>
    public static class HealthCheckCategory
    {
        public static readonly FixedString64Bytes Memory    = "Memory";
        public static readonly FixedString64Bytes Network   = "Network";
        public static readonly FixedString64Bytes Database  = "Database";
        public static readonly FixedString64Bytes Core      = "Core";
        public static readonly FixedString64Bytes Messaging = "Messaging";
        public static readonly FixedString64Bytes LiveOps   = "LiveOps";
        public static readonly FixedString64Bytes Custom    = "Custom";
    }
}