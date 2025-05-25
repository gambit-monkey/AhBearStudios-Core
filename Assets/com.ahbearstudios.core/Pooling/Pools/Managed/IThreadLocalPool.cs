using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;

namespace AhBearStudios.Pooling.Pools.Managed
{
    /// <summary>
    /// Interface for thread-local object pools that maintain separate pools for each thread.
    /// Provides high-performance pooling by eliminating contention in multi-threaded scenarios.
    /// </summary>
    /// <typeparam name="T">Type of objects to pool</typeparam>
    public interface IThreadLocalPool<T> : IPool<T>, IShrinkablePool where T : class
    {
        /// <summary>
        /// Creates a new instance using the factory method
        /// </summary>
        /// <returns>A new instance of T</returns>
        T CreateNew();

        /// <summary>
        /// Prewarms the pool by creating the specified number of instances
        /// </summary>
        /// <param name="count">Number of instances to create</param>
        void PrewarmPool(int count);

        /// <summary>
        /// Destroys an item, removing it from the pool permanently
        /// </summary>
        /// <param name="item">Item to destroy</param>
        void DestroyItem(T item);

        /// <summary>
        /// Tries to automatically shrink the pool based on elapsed time and thresholds
        /// </summary>
        void TryAutoShrink();
        
        /// <summary>
        /// Shrinks the current thread's pool to the specified target capacity
        /// </summary>
        /// <param name="targetCapacity">Target capacity</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        bool ShrinkCurrentThreadPoolTo(int targetCapacity);
        
        /// <summary>
        /// Applies configuration settings to this pool
        /// </summary>
        /// <param name="config">Configuration to apply</param>
        void ApplyConfiguration(IPoolConfig config);
    }
}