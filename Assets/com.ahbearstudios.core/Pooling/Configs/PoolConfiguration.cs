using System;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Strategies;

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
        /// Gets the pooling strategy to use.
        /// </summary>
        public IPoolingStrategy Strategy { get; init; }

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
                Strategy = new DynamicSizeStrategy(),
                EnableValidation = true,
                EnableStatistics = true,
                DisposalPolicy = PoolDisposalPolicy.ReturnToPool
            };
        }
    }
}