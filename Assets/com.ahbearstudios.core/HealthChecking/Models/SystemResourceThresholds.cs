namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
    /// Configuration for system resource monitoring thresholds
    /// </summary>
    public sealed class SystemResourceThresholds
    {
        /// <summary>
        /// CPU usage percentage that triggers warning status
        /// </summary>
        public float CpuWarningThreshold { get; set; } = 70.0f;

        /// <summary>
        /// CPU usage percentage that triggers critical status
        /// </summary>
        public float CpuCriticalThreshold { get; set; } = 90.0f;

        /// <summary>
        /// Managed memory size in bytes that triggers warning status
        /// </summary>
        public long MemoryWarningThreshold { get; set; } = 500 * 1024 * 1024; // 500 MB

        /// <summary>
        /// Managed memory size in bytes that triggers critical status
        /// </summary>
        public long MemoryCriticalThreshold { get; set; } = 1024 * 1024 * 1024; // 1 GB

        /// <summary>
        /// Working set size in bytes that triggers warning status
        /// </summary>
        public long WorkingSetWarningThreshold { get; set; } = 1024 * 1024 * 1024; // 1 GB

        /// <summary>
        /// Working set size in bytes that triggers critical status
        /// </summary>
        public long WorkingSetCriticalThreshold { get; set; } = 2L * 1024 * 1024 * 1024; // 2 GB

        /// <summary>
        /// Thread count that triggers warning status
        /// </summary>
        public int ThreadCountWarningThreshold { get; set; } = 100;

        /// <summary>
        /// Thread count that triggers critical status
        /// </summary>
        public int ThreadCountCriticalThreshold { get; set; } = 200;

        /// <summary>
        /// Handle count that triggers warning status
        /// </summary>
        public int HandleCountWarningThreshold { get; set; } = 5000;

        /// <summary>
        /// Handle count that triggers critical status
        /// </summary>
        public int HandleCountCriticalThreshold { get; set; } = 10000;

        /// <summary>
        /// Creates default system resource thresholds appropriate for most applications
        /// </summary>
        /// <returns>Default threshold configuration</returns>
        public static SystemResourceThresholds CreateDefault()
        {
            return new SystemResourceThresholds();
        }

        /// <summary>
        /// Creates conservative thresholds for resource-constrained environments
        /// </summary>
        /// <returns>Conservative threshold configuration</returns>
        public static SystemResourceThresholds CreateConservative()
        {
            return new SystemResourceThresholds
            {
                CpuWarningThreshold = 50.0f,
                CpuCriticalThreshold = 80.0f,
                MemoryWarningThreshold = 256 * 1024 * 1024, // 256 MB
                MemoryCriticalThreshold = 512 * 1024 * 1024, // 512 MB
                WorkingSetWarningThreshold = 512 * 1024 * 1024, // 512 MB
                WorkingSetCriticalThreshold = 1024 * 1024 * 1024, // 1 GB
                ThreadCountWarningThreshold = 50,
                ThreadCountCriticalThreshold = 100,
                HandleCountWarningThreshold = 2500,
                HandleCountCriticalThreshold = 5000
            };
        }

        /// <summary>
        /// Creates aggressive thresholds for high-performance applications
        /// </summary>
        /// <returns>Aggressive threshold configuration</returns>
        public static SystemResourceThresholds CreateAggressive()
        {
            return new SystemResourceThresholds
            {
                CpuWarningThreshold = 85.0f,
                CpuCriticalThreshold = 95.0f,
                MemoryWarningThreshold = 2L * 1024 * 1024 * 1024, // 2 GB
                MemoryCriticalThreshold = 4L * 1024 * 1024 * 1024, // 4 GB
                WorkingSetWarningThreshold = 4L * 1024 * 1024 * 1024, // 4 GB
                WorkingSetCriticalThreshold = 8L * 1024 * 1024 * 1024, // 8 GB
                ThreadCountWarningThreshold = 200,
                ThreadCountCriticalThreshold = 500,
                HandleCountWarningThreshold = 10000,
                HandleCountCriticalThreshold = 20000
            };
        }
    }