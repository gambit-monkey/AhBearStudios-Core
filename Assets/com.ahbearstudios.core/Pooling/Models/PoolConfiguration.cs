using System;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Models
{
    /// <summary>
    /// Configuration for object pools.
    /// Defines capacity, lifecycle management, and validation settings.
    /// </summary>
    public class PoolConfiguration
    {
        /// <summary>
        /// Gets or sets the name of the pool.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the initial capacity of the pool.
        /// </summary>
        public int InitialCapacity { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum capacity of the pool.
        /// </summary>
        public int MaxCapacity { get; set; } = 100;

        /// <summary>
        /// Gets or sets the factory function for creating new objects.
        /// </summary>
        public Func<object> Factory { get; set; }

        /// <summary>
        /// Gets or sets the action to reset objects when returned to the pool.
        /// </summary>
        public Action<object> ResetAction { get; set; }

        /// <summary>
        /// Gets or sets the function to validate objects in the pool.
        /// </summary>
        public Func<object, bool> ValidationFunc { get; set; }

        /// <summary>
        /// Gets or sets the interval between validation checks.
        /// </summary>
        public TimeSpan ValidationInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the maximum idle time before objects are considered for removal.
        /// </summary>
        public TimeSpan MaxIdleTime { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets the pooling strategy to use.
        /// </summary>
        public IPoolStrategy Strategy { get; set; }

        /// <summary>
        /// Gets or sets whether validation is enabled.
        /// </summary>
        public bool EnableValidation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether statistics collection is enabled.
        /// </summary>
        public bool EnableStatistics { get; set; } = true;

        /// <summary>
        /// Gets or sets the disposal policy for objects.
        /// </summary>
        public PoolDisposalPolicy DisposalPolicy { get; set; } = PoolDisposalPolicy.ReturnToPool;
    }

    /// <summary>
    /// Disposal policy for pooled objects.
    /// </summary>
    public enum PoolDisposalPolicy
    {
        /// <summary>
        /// Return the object to the pool for reuse.
        /// </summary>
        ReturnToPool,

        /// <summary>
        /// Dispose the object immediately.
        /// </summary>
        DisposeOnReturn,

        /// <summary>
        /// Let the pool decide based on capacity and usage.
        /// </summary>
        PoolDecision
    }
}