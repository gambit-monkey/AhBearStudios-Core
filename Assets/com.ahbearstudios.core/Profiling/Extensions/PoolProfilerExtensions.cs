using System;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Sessions;
using AhBearStudios.Core.Profiling.Tagging;

namespace AhBearStudios.Core.Profiling.Extensions
{
    /// <summary>
    /// Pool-specific extensions for IProfiler
    /// </summary>
    public static class PoolProfilerExtensions
    {
        /// <summary>
        /// Begins a profiling session for a pool operation
        /// </summary>
        /// <param name="profiler">The profiler instance</param>
        /// <param name="operationType">Operation type (acquire, release, etc.)</param>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        /// <param name="poolMetrics">Pool metrics tracker</param>
        /// <returns>A pool profiler session</returns>
        public static PoolProfilerSession BeginPoolScope(
            this IProfiler profiler,
            string operationType,
            Guid poolId,
            string poolName,
            int activeCount,
            int freeCount,
            IPoolMetrics poolMetrics)
        {
            if (profiler == null || !profiler.IsEnabled)
                return null;
                
            var tag = PoolProfilerTags.ForPool(operationType, poolId);
            return new PoolProfilerSession(tag, poolId, poolName, activeCount, freeCount, poolMetrics);
        }
        
        /// <summary>
        /// Begins a profiling session for a pool operation using just the pool name
        /// </summary>
        /// <param name="profiler">The profiler instance</param>
        /// <param name="operationType">Operation type (acquire, release, etc.)</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        /// <param name="poolMetrics">Pool metrics tracker</param>
        /// <returns>A pool profiler session</returns>
        public static PoolProfilerSession BeginPoolScope(
            this IProfiler profiler,
            string operationType,
            string poolName,
            int activeCount,
            int freeCount,
            IPoolMetrics poolMetrics)
        {
            if (profiler == null || !profiler.IsEnabled)
                return null;
                
            var tag = PoolProfilerTags.ForPoolName(operationType, poolName);
            
            // Create a deterministic GUID from the name
            Guid poolId;
            if (!string.IsNullOrEmpty(poolName))
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(poolName));
                    poolId = new Guid(hash);
                }
            }
            else
            {
                poolId = Guid.Empty;
            }
            
            return new PoolProfilerSession(tag, poolId, poolName, activeCount, freeCount, poolMetrics);
        }
        
        /// <summary>
        /// Begins a profiling session for a generic pool operation
        /// </summary>
        /// <param name="profiler">The profiler instance</param>
        /// <param name="operationType">Operation type (acquire, release, etc.)</param>
        /// <returns>A profiler session</returns>
        public static ProfilerSession BeginPoolScope(
            this IProfiler profiler,
            string operationType)
        {
            if (profiler == null || !profiler.IsEnabled)
                return null;
                
            var tag = PoolProfilerTags.ForOperation(operationType);
            return profiler.BeginScope(tag);
        }
        
        /// <summary>
        /// Profiles a pool action
        /// </summary>
        /// <param name="profiler">The profiler instance</param>
        /// <param name="operationType">Operation type</param>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        /// <param name="poolMetrics">Pool metrics tracker</param>
        /// <param name="action">Action to profile</param>
        public static void ProfilePoolAction(
            this IProfiler profiler,
            string operationType,
            Guid poolId,
            string poolName,
            int activeCount,
            int freeCount,
            IPoolMetrics poolMetrics,
            Action action)
        {
            if (profiler == null || !profiler.IsEnabled || action == null)
            {
                action?.Invoke();
                return;
            }
            
            using (profiler.BeginPoolScope(operationType, poolId, poolName, activeCount, freeCount, poolMetrics))
            {
                action.Invoke();
            }
        }
    }
}