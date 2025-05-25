using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;

namespace AhBearStudios.Pooling.Pools.Advanced
{
    /// <summary>
    /// Thread-safe pool interface that uses a semaphore to limit concurrent access.
    /// Provides asynchronous and non-blocking acquisition methods.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool</typeparam>
    public interface ISemaphorePool<T> : IPool<T>, IShrinkablePool
    {
        /// <summary>
        /// Acquires an item from the pool asynchronously
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task containing the acquired item</returns>
        Task<T> AcquireAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Tries to acquire an item without blocking
        /// </summary>
        /// <param name="item">The acquired item if successful</param>
        /// <returns>True if an item was acquired, false otherwise</returns>
        bool TryAcquire(out T item);
    }
}