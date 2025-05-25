using System;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Pooling.Services;

namespace AhBearStudios.Core.Pooling.Pools.Async
{
    /// <summary>
    /// Provides asynchronous pooling operations
    /// </summary>
    public static class AsyncPooling
    {
        private static readonly PoolLogger _logger = PoolingServices.GetService<PoolLogger>();
        
        /// <summary>
        /// Acquires an item from a pool asynchronously
        /// </summary>
        /// <typeparam name="T">Type of the item</typeparam>
        /// <param name="pool">The pool to acquire from</param>
        /// <param name="cancellationToken">Optional token to cancel the operation</param>
        /// <returns>Task representing the acquire operation with the acquired item</returns>
        public static async Task<T> AcquireAsync<T>(this IPool<T> pool, CancellationToken cancellationToken = default)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            // If already cancelled, throw
            cancellationToken.ThrowIfCancellationRequested();
            
            // Try to acquire immediately
            if (pool.InactiveCount > 0 || pool.TotalCount < ((PoolConfig)pool.GetMetrics()["Config"]).MaximumCapacity)
            {
                return pool.Acquire();
            }
            
            // Otherwise, wait for an item to become available
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            
            // Setup cancellation
            CancellationTokenRegistration registration = cancellationToken.Register(() => 
            {
                tcs.TrySetCanceled();
            });
            
            // Start a polling task
            _ = Task.Run(async () => 
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if (pool.InactiveCount > 0 || pool.TotalCount < ((PoolConfig)pool.GetMetrics()["Config"]).MaximumCapacity)
                        {
                            try
                            {
                                T item = pool.Acquire();
                                tcs.TrySetResult(item);
                                break;
                            }
                            catch (Exception ex)
                            {
                                tcs.TrySetException(ex);
                                break;
                            }
                        }
                        
                        await Task.Delay(10, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                finally
                {
                    registration.Dispose();
                }
            }, cancellationToken);
            
            return await tcs.Task;
        }
        
        /// <summary>
        /// Releases an item back to a pool asynchronously
        /// </summary>
        /// <typeparam name="T">Type of the item</typeparam>
        /// <param name="pool">The pool to release to</param>
        /// <param name="item">The item to release</param>
        /// <returns>Task representing the release operation</returns>
        public static Task ReleaseAsync<T>(this IPool<T> pool, T item)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            return Task.Run(() => 
            {
                try
                {
                    pool.Release(item);
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"Error releasing item asynchronously: {ex.Message}");
                    throw;
                }
            });
        }
        
        /// <summary>
        /// Acquires an item, uses it, and releases it back to the pool
        /// </summary>
        /// <typeparam name="T">Type of the item</typeparam>
        /// <typeparam name="TResult">Type of the result</typeparam>
        /// <param name="pool">The pool to work with</param>
        /// <param name="func">Function that uses the item and returns a result</param>
        /// <param name="cancellationToken">Optional token to cancel the operation</param>
        /// <returns>Task representing the operation with the result</returns>
        public static async Task<TResult> UseItemAsync<T, TResult>(this IPool<T> pool, Func<T, Task<TResult>> func, CancellationToken cancellationToken = default)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (func == null)
                throw new ArgumentNullException(nameof(func));
                
            T item = await pool.AcquireAsync(cancellationToken).ConfigureAwait(false);
            
            try
            {
                return await func(item).ConfigureAwait(false);
            }
            finally
            {
                await pool.ReleaseAsync(item).ConfigureAwait(false);
            }
        }
        
        /// <summary>
        /// Acquires an item, uses it, and releases it back to the pool
        /// </summary>
        /// <typeparam name="T">Type of the item</typeparam>
        /// <param name="pool">The pool to work with</param>
        /// <param name="action">Action that uses the item</param>
        /// <param name="cancellationToken">Optional token to cancel the operation</param>
        /// <returns>Task representing the operation</returns>
        public static async Task UseItemAsync<T>(this IPool<T> pool, Func<T, Task> action, CancellationToken cancellationToken = default)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            T item = await pool.AcquireAsync(cancellationToken).ConfigureAwait(false);
            
            try
            {
                await action(item).ConfigureAwait(false);
            }
            finally
            {
                await pool.ReleaseAsync(item).ConfigureAwait(false);
            }
        }
    }
}