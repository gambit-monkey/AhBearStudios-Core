using System;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Metrics;
using AhBearStudios.Core.MessageBus.Interfaces;
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
        /// <param name="messageBus">Optional message bus for alerts</param>
        /// <param name="initialCapacity">Initial capacity for tracking pools</param>
        /// <returns>A new pool metrics instance</returns>
        public static IPoolMetrics CreateStandard(IMessageBus messageBus = null, int initialCapacity = 64)
        {
            return new PoolMetrics(messageBus, initialCapacity);
        }
        
        /// <summary>
        /// Creates a standard pool metrics instance with a specific pool already configured
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="poolType">Type of pool</param>
        /// <param name="itemType">Type of items in the pool</param>
        /// <param name="messageBus">Optional message bus for alerts</param>
        /// <param name="estimatedItemSizeBytes">Estimated size of each item in bytes (0 for automatic estimation)</param>
        /// <returns>A new pool metrics instance</returns>
        public static IPoolMetrics CreateStandard(
            Guid poolId,
            string poolName,
            Type poolType,
            Type itemType,
            IMessageBus messageBus = null,
            int estimatedItemSizeBytes = 0)
        {
            return new PoolMetrics(
                poolId,
                poolName,
                poolType,
                itemType,
                messageBus,
                estimatedItemSizeBytes);
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