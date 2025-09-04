using System;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Threshold configuration for system resource health checking.
    /// Defines warning and critical limits for CPU, memory, disk, and other system resources.
    /// </summary>
    public sealed class SystemResourceThresholds
    {
        /// <summary>
        /// CPU usage percentage that triggers warning status (0-100)
        /// </summary>
        public double CpuUsageWarningThreshold { get; set; } = 80.0;

        /// <summary>
        /// CPU usage percentage that triggers critical status (0-100)
        /// </summary>
        public double CpuUsageCriticalThreshold { get; set; } = 95.0;

        /// <summary>
        /// Memory usage percentage that triggers warning status (0-100)
        /// </summary>
        public double MemoryUsageWarningThreshold { get; set; } = 80.0;

        /// <summary>
        /// Memory usage percentage that triggers critical status (0-100)
        /// </summary>
        public double MemoryUsageCriticalThreshold { get; set; } = 95.0;

        /// <summary>
        /// Disk usage percentage that triggers warning status (0-100)
        /// </summary>
        public double DiskUsageWarningThreshold { get; set; } = 85.0;

        /// <summary>
        /// Disk usage percentage that triggers critical status (0-100)
        /// </summary>
        public double DiskUsageCriticalThreshold { get; set; } = 95.0;

        /// <summary>
        /// Available memory threshold in bytes that triggers warning status
        /// </summary>
        public long AvailableMemoryWarningThreshold { get; set; } = 1024 * 1024 * 512; // 512 MB

        /// <summary>
        /// Available memory threshold in bytes that triggers critical status
        /// </summary>
        public long AvailableMemoryCriticalThreshold { get; set; } = 1024 * 1024 * 256; // 256 MB

        /// <summary>
        /// GC pressure threshold that triggers warning status
        /// </summary>
        public double GcPressureWarningThreshold { get; set; } = 0.8;

        /// <summary>
        /// GC pressure threshold that triggers critical status
        /// </summary>
        public double GcPressureCriticalThreshold { get; set; } = 0.95;

        /// <summary>
        /// Thread count threshold that triggers warning status
        /// </summary>
        public int ThreadCountWarningThreshold { get; set; } = 100;

        /// <summary>
        /// Thread count threshold that triggers critical status
        /// </summary>
        public int ThreadCountCriticalThreshold { get; set; } = 200;

        /// <summary>
        /// Handle count threshold that triggers warning status (Windows only)
        /// </summary>
        public int HandleCountWarningThreshold { get; set; } = 1000;

        /// <summary>
        /// Handle count threshold that triggers critical status (Windows only)
        /// </summary>
        public int HandleCountCriticalThreshold { get; set; } = 2000;

        /// <summary>
        /// Creates default system resource thresholds
        /// </summary>
        /// <returns>SystemResourceThresholds with default settings</returns>
        public static SystemResourceThresholds CreateDefault()
        {
            return new SystemResourceThresholds();
        }

        /// <summary>
        /// Creates conservative system resource thresholds for production environments
        /// </summary>
        /// <returns>SystemResourceThresholds with conservative settings</returns>
        public static SystemResourceThresholds CreateConservative()
        {
            return new SystemResourceThresholds
            {
                CpuUsageWarningThreshold = 70.0,
                CpuUsageCriticalThreshold = 90.0,
                MemoryUsageWarningThreshold = 70.0,
                MemoryUsageCriticalThreshold = 90.0,
                DiskUsageWarningThreshold = 80.0,
                DiskUsageCriticalThreshold = 90.0,
                AvailableMemoryWarningThreshold = 1024 * 1024 * 1024, // 1 GB
                AvailableMemoryCriticalThreshold = 1024 * 1024 * 512,  // 512 MB
                ThreadCountWarningThreshold = 50,
                ThreadCountCriticalThreshold = 100
            };
        }

        /// <summary>
        /// Creates aggressive system resource thresholds for high-performance scenarios
        /// </summary>
        /// <returns>SystemResourceThresholds with aggressive settings</returns>
        public static SystemResourceThresholds CreateAggressive()
        {
            return new SystemResourceThresholds
            {
                CpuUsageWarningThreshold = 90.0,
                CpuUsageCriticalThreshold = 98.0,
                MemoryUsageWarningThreshold = 90.0,
                MemoryUsageCriticalThreshold = 98.0,
                DiskUsageWarningThreshold = 90.0,
                DiskUsageCriticalThreshold = 98.0,
                AvailableMemoryWarningThreshold = 1024 * 1024 * 256, // 256 MB
                AvailableMemoryCriticalThreshold = 1024 * 1024 * 128, // 128 MB
                ThreadCountWarningThreshold = 150,
                ThreadCountCriticalThreshold = 300
            };
        }
    }
}