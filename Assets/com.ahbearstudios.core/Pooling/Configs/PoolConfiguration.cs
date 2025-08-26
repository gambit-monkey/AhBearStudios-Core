using System;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;

namespace AhBearStudios.Core.Pooling.Configs
{
    /// <summary>
    /// Immutable configuration record for object pools.
    /// Defines capacity, lifecycle management, and validation settings.
    /// Use with-expressions to create modified copies.
    /// </summary>
    public record PoolConfiguration
    {
        /// <summary>
        /// Gets the name of the pool.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets the initial capacity of the pool.
        /// </summary>
        public int InitialCapacity { get; init; } = 10;

        /// <summary>
        /// Gets the maximum capacity of the pool.
        /// </summary>
        public int MaxCapacity { get; init; } = 100;

        /// <summary>
        /// Gets the factory function for creating new objects.
        /// </summary>
        public Func<object> Factory { get; init; }

        /// <summary>
        /// Gets the action to reset objects when returned to the pool.
        /// </summary>
        public Action<object> ResetAction { get; init; }

        /// <summary>
        /// Gets the function to validate objects in the pool.
        /// </summary>
        public Func<object, bool> ValidationFunc { get; init; }

        /// <summary>
        /// Gets the interval between validation checks.
        /// </summary>
        public TimeSpan ValidationInterval { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets the maximum idle time before objects are considered for removal.
        /// </summary>
        public TimeSpan MaxIdleTime { get; init; } = TimeSpan.FromMinutes(30);


        /// <summary>
        /// Gets whether validation is enabled.
        /// </summary>
        public bool EnableValidation { get; init; } = true;

        /// <summary>
        /// Gets whether statistics collection is enabled.
        /// </summary>
        public bool EnableStatistics { get; init; } = true;

        /// <summary>
        /// Gets the disposal policy for objects.
        /// </summary>
        public PoolDisposalPolicy DisposalPolicy { get; init; } = PoolDisposalPolicy.ReturnToPool;

        /// <summary>
        /// Gets whether the pool should block when exhausted or throw an exception.
        /// </summary>
        public bool BlockWhenExhausted { get; init; } = false;

        /// <summary>
        /// Gets the maximum time to wait when pool is exhausted (in milliseconds).
        /// </summary>
        public int MaxWaitTime { get; init; } = 5000;

        /// <summary>
        /// Gets the pooling strategy type to use for this pool.
        /// </summary>
        public PoolingStrategyType StrategyType { get; init; } = PoolingStrategyType.Default;

        /// <summary>
        /// Gets the performance budget constraints for this pool.
        /// </summary>
        public PerformanceBudget PerformanceBudget { get; init; }

        /// <summary>
        /// Gets additional configuration specific to the chosen strategy.
        /// </summary>
        public PoolingStrategyConfig StrategyConfig { get; init; }
        
        /// <summary>
        /// Creates a default pool configuration with basic settings.
        /// </summary>
        /// <param name="name">Name of the pool</param>
        /// <returns>Default pool configuration</returns>
        public static PoolConfiguration CreateDefault(string name)
        {
            return new PoolConfiguration
            {
                Name = name,
                InitialCapacity = 10,
                MaxCapacity = 100,
                ValidationInterval = TimeSpan.FromMinutes(5),
                MaxIdleTime = TimeSpan.FromMinutes(30),
                EnableValidation = true,
                EnableStatistics = true,
                DisposalPolicy = PoolDisposalPolicy.ReturnToPool,
                BlockWhenExhausted = false,
                MaxWaitTime = 5000,
                StrategyType = PoolingStrategyType.Default,
                PerformanceBudget = PerformanceBudget.For60FPS()
            };
        }

        /// <summary>
        /// Creates a high-performance pool configuration optimized for 60+ FPS.
        /// </summary>
        /// <param name="name">Name of the pool</param>
        /// <returns>High-performance pool configuration</returns>
        public static PoolConfiguration CreateHighPerformance(string name)
        {
            return new PoolConfiguration
            {
                Name = name,
                InitialCapacity = 50,
                MaxCapacity = 500,
                ValidationInterval = TimeSpan.FromMinutes(10),
                MaxIdleTime = TimeSpan.FromMinutes(15),
                EnableValidation = true,
                EnableStatistics = true,
                DisposalPolicy = PoolDisposalPolicy.ReturnToPool,
                BlockWhenExhausted = false,
                MaxWaitTime = 1000,
                StrategyType = PoolingStrategyType.HighPerformance,
                PerformanceBudget = PerformanceBudget.For60FPS()
            };
        }

        /// <summary>
        /// Creates a network-optimized pool configuration for adaptive networking.
        /// </summary>
        /// <param name="name">Name of the pool</param>
        /// <returns>Network-optimized pool configuration</returns>
        public static PoolConfiguration CreateNetworkOptimized(string name)
        {
            return new PoolConfiguration
            {
                Name = name,
                InitialCapacity = 25,
                MaxCapacity = 1000,
                ValidationInterval = TimeSpan.FromMinutes(2),
                MaxIdleTime = TimeSpan.FromMinutes(5),
                EnableValidation = true,
                EnableStatistics = true,
                DisposalPolicy = PoolDisposalPolicy.ReturnToPool,
                BlockWhenExhausted = false,
                MaxWaitTime = 500,
                StrategyType = PoolingStrategyType.AdaptiveNetwork,
                PerformanceBudget = PerformanceBudget.For60FPS()
            };
        }
    }
}