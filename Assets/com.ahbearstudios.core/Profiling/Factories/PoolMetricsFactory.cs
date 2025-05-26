using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Metrics;
using Unity.Collections;

namespace AhBearStudios.Core.Profiling.Factories
{
    /// <summary>
    /// Factory for creating pool metrics instances
    /// </summary>
    public static class PoolMetricsFactory
    {
        /// <summary>
        /// Creates a standard pool metrics instance
        /// </summary>
        /// <param name="initialCapacity">Initial capacity for tracking pools</param>
        /// <returns>A new pool metrics instance</returns>
        public static IPoolMetrics CreateStandard(int initialCapacity = 64)
        {
            return new PoolMetrics(initialCapacity);
        }
        
        /// <summary>
        /// Creates a native pool metrics instance for use with Burst and Jobs
        /// </summary>
        /// <param name="initialCapacity">Initial capacity for tracking pools</param>
        /// <param name="allocator">Memory allocator to use</param>
        /// <returns>A new native pool metrics instance</returns>
        public static INativePoolMetrics CreateNative(int initialCapacity = 64, Allocator allocator = Allocator.Persistent)
        {
            return new NativePoolMetrics(initialCapacity, allocator);
        }
    }
}