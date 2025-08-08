using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// System information for alerts.
    /// </summary>
    public sealed partial record AlertSystemInfo
    {
        /// <summary>
        /// Machine or instance name.
        /// </summary>
        public FixedString64Bytes MachineName { get; init; }

        /// <summary>
        /// Process ID.
        /// </summary>
        public int ProcessId { get; init; }

        /// <summary>
        /// Thread ID.
        /// </summary>
        public int ThreadId { get; init; }

        /// <summary>
        /// Available memory at time of alert.
        /// </summary>
        public long AvailableMemoryBytes { get; init; }

        /// <summary>
        /// Total system memory.
        /// </summary>
        public long TotalMemoryBytes { get; init; }
    }
}