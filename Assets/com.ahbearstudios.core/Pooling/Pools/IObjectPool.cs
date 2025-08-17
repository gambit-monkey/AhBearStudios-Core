using System;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Pools
{
    /// <summary>
    /// Non-generic base interface for object pools.
    /// Provides common operations that don't require type information.
    /// </summary>
    public interface IObjectPool : IDisposable
    {
        /// <summary>
        /// Gets the name of this pool.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets the total number of objects in the pool (available + active).
        /// </summary>
        int Count { get; }
        
        /// <summary>
        /// Gets the number of objects available for use.
        /// </summary>
        int AvailableCount { get; }
        
        /// <summary>
        /// Gets the number of objects currently in use.
        /// </summary>
        int ActiveCount { get; }
        
        /// <summary>
        /// Gets the configuration for this pool.
        /// </summary>
        PoolConfiguration Configuration { get; }
        
        /// <summary>
        /// Clears all objects from the pool.
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Removes excess objects from the pool to reduce memory usage.
        /// </summary>
        void TrimExcess();
        
        /// <summary>
        /// Validates all objects in the pool.
        /// </summary>
        /// <returns>True if all objects are valid</returns>
        bool Validate();
        
        /// <summary>
        /// Gets statistics about this pool's usage.
        /// </summary>
        /// <returns>Pool statistics</returns>
        PoolStatistics GetStatistics();
    }

    /// <summary>
    /// Core interface for type-specific object pools.
    /// Provides lifecycle management, statistics, and maintenance operations for pooled objects.
    /// </summary>
    /// <typeparam name="T">The type of objects managed by this pool</typeparam>
    public interface IObjectPool<T> : IObjectPool where T : class
    {
        // Basic operations
        /// <summary>
        /// Gets an object from the pool, creating a new one if necessary.
        /// </summary>
        /// <returns>An object from the pool</returns>
        T Get();
        
        /// <summary>
        /// Returns an object to the pool for reuse.
        /// </summary>
        /// <param name="item">The object to return to the pool</param>
        void Return(T item);
        
        /// <summary>
        /// Gets the pooling strategy used by this pool.
        /// </summary>
        IPoolingStrategy Strategy { get; }
        
        // Events
        /// <summary>
        /// Raised when a new object is created for the pool.
        /// </summary>
        event Action<T> ObjectCreated;
        
        /// <summary>
        /// Raised when an object is returned to the pool.
        /// </summary>
        event Action<T> ObjectReturned;
        
        /// <summary>
        /// Raised when an object is destroyed (removed from pool).
        /// </summary>
        event Action<T> ObjectDestroyed;
    }
}