using System;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;

namespace AhBearStudios.Pooling.Pools.Advanced
{
    /// <summary>
    /// Advanced object pool interface with extended functionality. Provides configurable auto-shrinking,
    /// lifecycle management, validation, and detailed metrics. Designed for high-performance
    /// single-threaded scenarios.
    /// </summary>
    /// <typeparam name="T">Type of objects to pool</typeparam>
    public interface IAdvancedObjectPool<T> : IPool<T>, IShrinkablePool where T : class
    {
        /// <summary>
        /// Creates a new item and ensures it's valid
        /// </summary>
        /// <returns>A new valid item</returns>
        T CreateValidItem();

        /// <summary>
        /// Destroys an item
        /// </summary>
        /// <param name="item">Item to destroy</param>
        void DestroyItem(T item);

        /// <summary>
        /// Prewarms the pool by creating the specified number of instances
        /// </summary>
        /// <param name="count">Number of instances to create</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative</exception>
        void PrewarmPool(int count);

        /// <summary>
        /// Attempts to auto-shrink the pool if conditions are met
        /// </summary>
        void TryAutoShrink();
    }
}