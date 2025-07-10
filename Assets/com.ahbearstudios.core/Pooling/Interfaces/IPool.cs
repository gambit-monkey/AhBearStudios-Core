using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Pooling.Interfaces
{
    /// <summary>
    /// Base interface for all pool implementations with shared functionality
    /// </summary>
    public interface IPool : IDisposable
    {
        /// <summary>
        /// Gets the unique identifier for this pool
        /// </summary>
        Guid Id { get; }
        
        /// <summary>
        /// Gets the total number of items in the pool (active + inactive)
        /// </summary>
        int TotalCount { get; }

        /// <summary>
        /// Gets the number of active items
        /// </summary>
        int ActiveCount { get; }

        /// <summary>
        /// Gets the number of inactive items
        /// </summary>
        int InactiveCount { get; }

        /// <summary>
        /// Gets the peak number of simultaneously active items
        /// </summary>
        int PeakUsage { get; }

        /// <summary>
        /// Gets the total number of items ever created by this pool
        /// </summary>
        int TotalCreated { get; }

        /// <summary>
        /// Gets whether this pool has been properly created and initialized
        /// </summary>
        bool IsCreated { get; }

        /// <summary>
        /// Gets whether this pool has been disposed
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Gets the type of items in the pool
        /// </summary>
        Type ItemType { get; }

        /// <summary>
        /// Gets metrics for this pool
        /// </summary>
        /// <returns>Dictionary of pool metrics</returns>
        Dictionary<string, object> GetMetrics();
        
        /// <summary>
        /// Gets the name of this pool
        /// </summary>
        string PoolName { get; }

        /// <summary>
        /// Clears the pool, returning all active items to the inactive state
        /// </summary>
        void Clear();

        /// <summary>
        /// Ensures the pool has at least the specified capacity
        /// </summary>
        /// <param name="capacity">Required capacity</param>
        void EnsureCapacity(int capacity);
        
        /// <summary>
        /// Sets the pool name. Used primarily for resolving naming conflicts during registration.
        /// </summary>
        /// <param name="newName">The new name for the pool</param>
        void SetPoolName(string newName);
        
        /// <summary>
        /// Gets the threading mode for this pool. Always returns ThreadLocal for this implementation.
        /// </summary>
        PoolThreadingMode ThreadingMode => PoolThreadingMode.ThreadLocal;
    }

    /// <summary>
    /// Generic interface for pools of specific item types
    /// </summary>
    /// <typeparam name="T">Type of items in the pool</typeparam>
    public interface IPool<T> : IPool
    {
        /// <summary>
        /// Acquires an item from the pool
        /// </summary>
        /// <returns>The acquired item</returns>
        T Acquire();
        
        /// <summary>
        /// Releases an item back to the pool
        /// </summary>
        /// <param name="item">Item to release</param>
        void Release(T item);
        
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