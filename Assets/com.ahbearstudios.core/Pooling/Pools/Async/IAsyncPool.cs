using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AhBearStudios.Core.Pooling.Pools.Async
{
    /// <summary>
    /// Interface for pools that support asynchronous operations
    /// </summary>
    /// <typeparam name="T">Type of items in the pool</typeparam>
    public interface IAsyncPool<T> : IPool<T>, IShrinkablePool
    {
        /// <summary>
        /// Asynchronously acquires an item from the pool
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the asynchronous operation with the acquired item</returns>
        Task<T> AcquireAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Asynchronously releases an item back to the pool
        /// </summary>
        /// <param name="item">Item to release</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task ReleaseAsync(T item);
        
        /// <summary>
        /// Asynchronously acquires multiple items from the pool at once
        /// </summary>
        /// <param name="count">Number of items to acquire</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the asynchronous operation with a list of acquired items</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is not positive</exception>
        Task<List<T>> AcquireMultipleAsync(int count, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Asynchronously releases multiple items back to the pool at once
        /// </summary>
        /// <param name="items">Items to release</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task ReleaseMultipleAsync(IEnumerable<T> items);
        
        /// <summary>
        /// Asynchronously acquires an item and initializes it with a setup action
        /// </summary>
        /// <param name="setupAction">Action to initialize the item</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the asynchronous operation with the acquired and initialized item</returns>
        Task<T> AcquireAndSetupAsync(Action<T> setupAction, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Asynchronously ensures the pool has at least the specified capacity
        /// </summary>
        /// <param name="capacity">Required capacity</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task EnsureCapacityAsync(int capacity, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Asynchronously clears the pool, returning all active items to the inactive state
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task ClearAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Attempts to acquire an item from the pool without waiting if none are available
        /// </summary>
        /// <param name="item">The acquired item if successful</param>
        /// <returns>True if an item was acquired, false otherwise</returns>
        bool TryAcquire(out T item);
        
        /// <summary>
        /// Asynchronously acquires an item, uses it, and releases it back to the pool
        /// </summary>
        /// <typeparam name="TResult">Type of the result</typeparam>
        /// <param name="func">Function that uses the item and returns a result</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the operation with the result</returns>
        Task<TResult> UseItemAsync<TResult>(Func<T, Task<TResult>> func, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Asynchronously acquires an item, uses it, and releases it back to the pool
        /// </summary>
        /// <param name="action">Action that uses the item</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the operation</returns>
        Task UseItemAsync(Func<T, Task> action, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Asynchronously attempts to shrink the pool's capacity to reduce memory usage
        /// </summary>
        /// <param name="threshold">Threshold factor (0-1) determining when shrinking occurs</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the operation with a boolean indicating if the pool was shrunk</returns>
        Task<bool> TryShrinkAsync(float threshold, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Asynchronously shrinks the pool to the specified capacity
        /// </summary>
        /// <param name="targetCapacity">The target capacity to shrink to</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the operation with a boolean indicating if the pool was shrunk</returns>
        Task<bool> ShrinkToAsync(int targetCapacity, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the estimated memory usage of the pool in bytes
        /// </summary>
        long EstimatedMemoryUsageBytes { get; }
        
        /// <summary>
        /// Gets the timestamp of the last shrink operation
        /// </summary>
        float LastShrinkTime { get; }
        
        /// <summary>
        /// Gets whether automatic shrinking is currently enabled
        /// </summary>
        bool AutoShrinkEnabled { get; }
        
        /// <summary>
        /// Asynchronously sets whether automatic shrinking is enabled
        /// </summary>
        /// <param name="enabled">Whether automatic shrinking should be enabled</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the operation</returns>
        Task SetAutoShrinkAsync(bool enabled, CancellationToken cancellationToken = default);
    }
}