using System;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Pools;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Service for selecting the appropriate pool implementation type based on object type and configuration.
    /// Follows CLAUDE.md Builder → Config → Factory → Service pattern by only selecting types, not creating instances.
    /// Pool creation is delegated to appropriate factories (INetworkBufferPoolFactory, etc.).
    /// </summary>
    public interface IPoolTypeSelector
    {
        /// <summary>
        /// Selects the appropriate pool type for the given object type and configuration.
        /// </summary>
        /// <typeparam name="T">The type of objects that will be pooled</typeparam>
        /// <param name="configuration">Pool configuration containing hints for pool selection</param>
        /// <returns>The recommended pool type for optimal performance</returns>
        PoolType SelectPoolType<T>(PoolConfiguration configuration) where T : class, IPooledObject, new();

        /// <summary>
        /// Determines if a pool type can be used for the specified object type.
        /// </summary>
        /// <typeparam name="T">The type of objects that will be pooled</typeparam>
        /// <param name="poolType">The pool type to check</param>
        /// <returns>True if the pool type is compatible with the object type</returns>
        bool CanUsePoolType<T>(PoolType poolType) where T : class, IPooledObject, new();

        /// <summary>
        /// Gets the estimated memory usage per object for the given pool type.
        /// </summary>
        /// <typeparam name="T">The type of objects that will be pooled</typeparam>
        /// <param name="poolType">The pool type to analyze</param>
        /// <returns>Estimated memory usage in bytes per object</returns>
        long GetEstimatedMemoryUsage<T>(PoolType poolType) where T : class, IPooledObject, new();
    }
}