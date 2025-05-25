using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Pooling.Core;

namespace AhBearStudios.Pooling.Extensions
{
    /// <summary>
    /// Extension methods for asynchronous pool operations
    /// </summary>
    public static class AsyncPoolExtensions
    {
        /// <summary>
        /// Acquires an item from a pool asynchronously
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task returning the acquired item</returns>
        public static Task<T> AcquireAsync<T>(this IPool<T> pool, CancellationToken cancellationToken = default)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            // This is not truly async, but we return a task for API consistency
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(pool.Acquire());
        }
        
        /// <summary>
        /// Acquires multiple items from a pool
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="count">Number of items to acquire</param>
        /// <returns>List of acquired items</returns>
        public static List<T> AcquireMultiple<T>(this IPool<T> pool, int count)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
                
            var items = new List<T>(count);
            
            try
            {
                for (int i = 0; i < count; i++)
                {
                    items.Add(pool.Acquire());
                }
                
                return items;
            }
            catch
            {
                // Release any acquired items
                foreach (var item in items)
                {
                    pool.Release(item);
                }
                
                throw;
            }
        }
        
        /// <summary>
        /// Acquires multiple items from a pool asynchronously
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="count">Number of items to acquire</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task returning a list of acquired items</returns>
        public static async Task<List<T>> AcquireMultipleAsync<T>(this IPool<T> pool, int count, CancellationToken cancellationToken = default)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
                
            var items = new List<T>(count);
            
            try
            {
                for (int i = 0; i < count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    items.Add(await AcquireAsync(pool, cancellationToken));
                }
                
                return items;
            }
            catch
            {
                // Release any acquired items
                foreach (var item in items)
                {
                    pool.Release(item);
                }
                
                throw;
            }
        }
        
        /// <summary>
        /// Releases multiple items back to the pool
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="items">Items to release</param>
        public static void ReleaseMultiple<T>(this IPool<T> pool, IEnumerable<T> items)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (items == null)
                throw new ArgumentNullException(nameof(items));
                
            foreach (var item in items)
            {
                if (item != null)
                {
                    pool.Release(item);
                }
            }
        }
        
        /// <summary>
        /// Releases multiple items back to the pool asynchronously
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="items">Items to release</param>
        /// <returns>Completion task</returns>
        public static Task ReleaseMultipleAsync<T>(this IPool<T> pool, IEnumerable<T> items)
        {
            ReleaseMultiple(pool, items);
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Executes an action for each acquired item and then releases it
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="count">Number of items to process</param>
        /// <param name="action">Action to perform on each item</param>
        public static void ProcessItems<T>(this IPool<T> pool, int count, Action<T> action)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
                
            for (int i = 0; i < count; i++)
            {
                var item = pool.Acquire();
                try
                {
                    action(item);
                }
                finally
                {
                    pool.Release(item);
                }
            }
        }
        
        /// <summary>
        /// Executes an async action for each acquired item and then releases it
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="count">Number of items to process</param>
        /// <param name="action">Async action to perform on each item</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Completion task</returns>
        public static async Task ProcessItemsAsync<T>(this IPool<T> pool, int count, Func<T, Task> action, CancellationToken cancellationToken = default)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
                
            for (int i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var item = pool.Acquire();
                try
                {
                    await action(item);
                }
                finally
                {
                    pool.Release(item);
                }
            }
        }
    }
}