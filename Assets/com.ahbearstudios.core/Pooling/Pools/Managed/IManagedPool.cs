using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;

namespace AhBearStudios.Pooling.Pools.Managed
{
    /// <summary>
    /// Interface for managed object pools, providing comprehensive object lifecycle management
    /// with configurable auto-shrinking, resource tracking, and detailed metrics.
    /// </summary>
    /// <typeparam name="T">Type of objects to pool</typeparam>
    public interface IManagedPool<T> : IPool<T>, IShrinkablePool where T : class
    {
        /// <summary>
        /// Creates a new instance using the factory method
        /// </summary>
        /// <returns>A new instance of T</returns>
        T CreateNew();

        /// <summary>
        /// Prewarms the pool by creating a specified number of instances
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
        /// Gets or sets the growth factor when the pool needs to expand
        /// </summary>
        float GrowthFactor { get; set; }
    }
}