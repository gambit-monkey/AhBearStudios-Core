using System;

namespace AhBearStudios.Core.Pooling.Models
{
    /// <summary>
    /// Health monitoring thresholds for network buffer pools.
    /// Defines performance and health monitoring configuration for pooled network buffers.
    /// </summary>
    public class NetworkBufferHealthThresholds
    {
        /// <summary>
        /// Maximum consecutive failures before circuit breaker triggers.
        /// </summary>
        public int MaxConsecutiveFailures { get; set; } = 5;

        /// <summary>
        /// Maximum validation errors before marking as corrupted.
        /// </summary>
        public int MaxValidationErrors { get; set; } = 10;

        /// <summary>
        /// Corruption threshold as percentage of failed validations.
        /// </summary>
        public double CorruptionThresholdPercentage { get; set; } = 0.25;

        /// <summary>
        /// Warning threshold for total pool memory usage.
        /// </summary>
        public long WarningMemoryUsageBytes { get; set; } = 64 * 1024 * 1024; // 64MB

        /// <summary>
        /// Critical threshold for total pool memory usage.
        /// </summary>
        public long CriticalMemoryUsageBytes { get; set; } = 128 * 1024 * 1024; // 128MB

        /// <summary>
        /// Maximum idle time before cleanup consideration.
        /// </summary>
        public TimeSpan MaxIdleTimeBeforeCleanup { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Frequency of health validation checks.
        /// </summary>
        public TimeSpan ValidationFrequency { get; set; } = TimeSpan.FromMinutes(2);
    }
}