using System;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Pools.Async;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory interface for creating asynchronous object pools with enhanced configuration options.
    /// </summary>
    public interface IAsyncPoolFactory : IPoolFactory
    {
        /// <summary>
        /// Creates a new asynchronous object pool with the specified configuration.
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="asyncFactory">Asynchronous factory function to create new items</param>
        /// <param name="config">Configuration for the pool</param>
        /// <param name="resetAction">Optional action to reset items when returned to the pool</param>
        /// <param name="poolName">Optional name for the pool (useful for diagnostics)</param>
        /// <returns>A new asynchronous object pool instance</returns>
        IAsyncPool<T> CreateAsyncPool<T>(
            Func<Task<T>> asyncFactory,
            AsyncObjectPoolConfig config,
            Action<T> resetAction = null,
            string poolName = null);
            
        /// <summary>
        /// Creates a new asynchronous object pool with the specified configuration and registers it with the pool registry.
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="asyncFactory">Asynchronous factory function to create new items</param>
        /// <param name="config">Configuration for the pool</param>
        /// <param name="resetAction">Optional action to reset items when returned to the pool</param>
        /// <param name="poolName">Optional name for the pool (useful for diagnostics)</param>
        /// <param name="registerPool">Whether to register the pool with the pool registry</param>
        /// <returns>A new asynchronous object pool instance</returns>
        IAsyncPool<T> CreateAsyncPool<T>(
            Func<Task<T>> asyncFactory,
            AsyncObjectPoolConfig config,
            Action<T> resetAction = null,
            string poolName = null,
            bool registerPool = true);
            
        /// <summary>
        /// Creates a new asynchronous object pool with basic configuration parameters.
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="asyncFactory">Asynchronous factory function to create new items</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum size the pool can grow to (0 for unlimited)</param>
        /// <param name="resetAction">Optional action to reset items when returned to the pool</param>
        /// <param name="poolName">Optional name for the pool (useful for diagnostics)</param>
        /// <returns>A new asynchronous object pool instance</returns>
        IAsyncPool<T> CreateAsyncPool<T>(
            Func<Task<T>> asyncFactory,
            int initialCapacity = 5,
            int maxSize = 0,
            Action<T> resetAction = null,
            string poolName = null);
            
        /// <summary>
        /// Creates a new asynchronous object pool preconfigured for a specific usage pattern.
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="asyncFactory">Asynchronous factory function to create new items</param>
        /// <param name="usagePattern">Predefined usage pattern for the pool</param>
        /// <param name="resetAction">Optional action to reset items when returned to the pool</param>
        /// <param name="poolName">Optional name for the pool (useful for diagnostics)</param>
        /// <returns>A new asynchronous object pool instance optimized for the specified pattern</returns>
        IAsyncPool<T> CreateAsyncPoolForPattern<T>(
            Func<Task<T>> asyncFactory,
            AsyncPoolUsagePattern usagePattern,
            Action<T> resetAction = null,
            string poolName = null);
            
        /// <summary>
        /// Creates a new asynchronous pool adapter that wraps a synchronous pool.
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="basePool">Underlying synchronous pool to wrap</param>
        /// <param name="asyncFactory">Optional custom factory function for asynchronous item creation</param>
        /// <param name="ownsPool">Whether this adapter owns and should dispose the base pool</param>
        /// <returns>A new asynchronous pool adapter</returns>
        IAsyncPool<T> CreateAsyncAdapter<T>(
            IPool<T> basePool,
            Func<CancellationToken, Task<T>> asyncFactory = null,
            bool ownsPool = false) where T : class;
            
        /// <summary>
        /// Creates a new asynchronous object pool using a configuration builder.
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="asyncFactory">Asynchronous factory function to create new items</param>
        /// <param name="builderSetup">Action to set up the configuration builder</param>
        /// <param name="resetAction">Optional action to reset items when returned to the pool</param>
        /// <param name="poolName">Optional name for the pool (useful for diagnostics)</param>
        /// <returns>A new asynchronous object pool instance</returns>
        IAsyncPool<T> CreateAsyncPool<T>(
            Func<Task<T>> asyncFactory,
            Action<AsyncObjectPoolConfigBuilder> builderSetup,
            Action<T> resetAction = null,
            string poolName = null);
    }
}