using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Pools.Async;

namespace AhBearStudios.Pooling.Extensions
{
    /// <summary>
    /// Extensions methods for pools providing common operations and utility functions
    /// </summary>
    public static class PoolExtensions
    {
        /// <summary>
        /// Acquires multiple items from a pool at once
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">The pool to acquire items from</param>
        /// <param name="count">Number of items to acquire</param>
        /// <returns>List of acquired items</returns>
        /// <exception cref="ArgumentNullException">Thrown if the pool is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is not positive</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed</exception>
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
            catch (Exception)
            {
                // If acquiring fails, return any successfully acquired items back to the pool
                foreach (var item in items)
                {
                    try
                    {
                        pool.Release(item);
                    }
                    catch
                    {
                        // Ignore exceptions during cleanup
                    }
                }
                
                throw;
            }
        }
        
        /// <summary>
        /// Releases multiple items back to a pool at once
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">The pool to release items to</param>
        /// <param name="items">Items to release</param>
        /// <exception cref="ArgumentNullException">Thrown if the pool or items collection is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed</exception>
        public static void ReleaseMultiple<T>(this IPool<T> pool, IEnumerable<T> items)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (items == null)
                throw new ArgumentNullException(nameof(items));
                
            foreach (var item in items)
            {
                if (item != null) // Skip null items to make the method more robust
                {
                    pool.Release(item);
                }
            }
        }
        
        /// <summary>
        /// Acquires an item and initializes it with a setup action
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">The pool to acquire the item from</param>
        /// <param name="setupAction">Action to initialize the item</param>
        /// <returns>The acquired and initialized item</returns>
        /// <exception cref="ArgumentNullException">Thrown if the pool is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed</exception>
        public static T AcquireAndSetup<T>(this IPool<T> pool, Action<T> setupAction)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            var item = pool.Acquire();
            
            try
            {
                setupAction?.Invoke(item);
                return item;
            }
            catch (Exception)
            {
                // If setup fails, return the item to the pool
                try
                {
                    pool.Release(item);
                }
                catch
                {
                    // Ignore exceptions during cleanup
                }
                
                throw;
            }
        }
        
        /// <summary>
        /// Acquires an item with a lease that automatically returns it to the pool when disposed
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">The pool to acquire the item from</param>
        /// <returns>A disposable lease for the item</returns>
        /// <exception cref="ArgumentNullException">Thrown if the pool is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed</exception>
        public static PoolLease<T> AcquireWithLease<T>(this IPool<T> pool)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            return new PoolLease<T>(pool, pool.Acquire());
        }
        
        /// <summary>
        /// Gets detailed metrics from a pool including utilization ratio
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">The pool to get metrics from</param>
        /// <returns>Dictionary of metrics</returns>
        /// <exception cref="ArgumentNullException">Thrown if the pool is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed</exception>
        public static Dictionary<string, object> GetDetailedMetrics<T>(this IPool<T> pool)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            var metrics = pool.GetMetrics() ?? new Dictionary<string, object>();
            
            // Add total count if not already present
            if (!metrics.ContainsKey("TotalCount"))
            {
                metrics["TotalCount"] = pool.ActiveCount + pool.InactiveCount;
            }
            
            // Calculate utilization ratio if not already present
            if (!metrics.ContainsKey("UtilizationRatio"))
            {
                int totalCount = pool.ActiveCount + pool.InactiveCount;
                
                if (totalCount > 0)
                {
                    metrics["UtilizationRatio"] = (float)pool.ActiveCount / totalCount;
                }
                else
                {
                    metrics["UtilizationRatio"] = 0f;
                }
            }
            
            // Add timestamp for when the metrics were collected
            metrics["CollectedAt"] = DateTime.UtcNow;
            
            return metrics;
        }
        
        /// <summary>
        /// Gets an async adapter for this pool, enabling async/await usage patterns
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">The pool to adapt</param>
        /// <param name="ownsPool">Whether the adapter should dispose the base pool when disposed</param>
        /// <returns>An async pool adapter</returns>
        /// <exception cref="ArgumentNullException">Thrown if the pool is null</exception>
        public static IAsyncPool<T> AsAsync<T>(this IPool<T> pool, bool ownsPool = false) where T : class
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
        
            // Check if the pool already implements IAsyncPool
            if (pool is IAsyncPool<T> asyncPool)
            {
                return asyncPool;
            }
    
            // Create a new adapter, passing the ownership parameter
            return new AsyncPoolAdapter<T>(pool, ownsPool);
        }
        
        /// <summary>
        /// Ensures a pool doesn't exceed a maximum size by releasing excess items
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="pool">The pool to trim</param>
        /// <param name="maxSize">Maximum desired size of the pool</param>
        /// <returns>Number of items released</returns>
        /// <exception cref="ArgumentNullException">Thrown if the pool is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if maxSize is negative</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed</exception>
        public static int TrimExcess<T>(this IPool<T> pool, int maxSize)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (maxSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Maximum size cannot be negative");
                
            // If the pool already has special trimming capabilities, use those
            if (pool is IShrinkablePool shrinkablePool)
            {
                if (shrinkablePool.ShrinkTo(maxSize))
                {
                    // Return an approximation of items released
                    return Math.Max(0, pool.TotalCount - maxSize);
                }
                return 0;
            }
            
            // Standard pools cannot easily trim excess items as we don't have access to internal storage
            return 0;
        }
    }
}