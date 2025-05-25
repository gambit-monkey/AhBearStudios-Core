using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;

namespace AhBearStudios.Pooling.Pools.Managed
{
    /// <summary>
    /// Interface for thread-safe object pools that can be safely accessed from multiple threads.
    /// Provides atomic operations with locking mechanisms and robust diagnostics.
    /// </summary>
    /// <typeparam name="T">Type of objects to pool</typeparam>
    public interface IThreadSafePool<T> : IPool<T>, IShrinkablePool where T : class
    {
        /// <summary>
        /// Creates a new instance using the factory method
        /// </summary>
        /// <returns>A new instance of T</returns>
        T CreateNew();

        /// <summary>
        /// Prewarms the pool by creating a specified number of objects
        /// </summary>
        /// <param name="count">Number of objects to create</param>
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
        /// Acquires multiple items from the pool at once
        /// </summary>
        /// <param name="count">Number of items to acquire</param>
        /// <returns>List of acquired items</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is not positive</exception>
        List<T> AcquireMultiple(int count);

        /// <summary>
        /// Releases multiple items back to the pool at once
        /// </summary>
        /// <param name="items">Items to release</param>
        void ReleaseMultiple(IEnumerable<T> items);

        /// <summary>
        /// Acquires an item and initializes it with a setup action
        /// </summary>
        /// <param name="setupAction">Action to initialize the item</param>
        /// <returns>The acquired and initialized item</returns>
        T AcquireAndSetup(Action<T> setupAction);
    }
}