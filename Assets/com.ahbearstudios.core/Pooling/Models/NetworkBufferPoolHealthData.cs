using System;

namespace AhBearStudios.Core.Pooling.Models
{
    /// <summary>
    /// Health data for network buffer pools.
    /// Contains diagnostic information about buffer pool status and performance.
    /// </summary>
    public class NetworkBufferPoolHealthData
    {
        /// <summary>
        /// Total number of buffers created across all pools.
        /// </summary>
        public int TotalBuffersCreated { get; set; }

        /// <summary>
        /// Number of currently active (in-use) buffers.
        /// </summary>
        public int ActiveBuffers { get; set; }

        /// <summary>
        /// Total memory usage of all buffer pools in bytes.
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// Ratio of active buffers to total buffers created.
        /// </summary>
        public double ActiveBufferRatio => TotalBuffersCreated > 0 ? (double)ActiveBuffers / TotalBuffersCreated : 0.0;

        /// <summary>
        /// Memory usage in megabytes for easier reading.
        /// </summary>
        public double MemoryUsageMB => MemoryUsageBytes / (1024.0 * 1024.0);

        /// <summary>
        /// Timestamp of the health check.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}