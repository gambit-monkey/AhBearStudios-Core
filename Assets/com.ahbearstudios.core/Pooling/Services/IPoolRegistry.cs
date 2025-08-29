using System;
using System.Collections.Generic;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Pools;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Service interface for managing pool registration and lookup operations.
    /// Provides centralized storage and retrieval of pool instances by type.
    /// Designed for Unity game development with 60+ FPS performance requirements.
    /// </summary>
    public interface IPoolRegistry : IDisposable
    {
        #region Pool Registration

        /// <summary>
        /// Registers a pool for the specified type.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="pool">Pool instance to register</param>
        /// <returns>True if registration was successful</returns>
        bool RegisterPool<T>(IObjectPool<T> pool) where T : class, IPooledObject, new();

        /// <summary>
        /// Unregisters and disposes a pool for the specified type.
        /// </summary>
        /// <typeparam name="T">Type to unregister pool for</typeparam>
        /// <returns>True if unregistration was successful</returns>
        bool UnregisterPool<T>() where T : class, IPooledObject;

        /// <summary>
        /// Unregisters all pools and disposes their resources.
        /// </summary>
        void UnregisterAllPools();

        #endregion

        #region Pool Lookup

        /// <summary>
        /// Gets the pool for the specified type.
        /// </summary>
        /// <typeparam name="T">Type to get pool for</typeparam>
        /// <returns>Pool instance or null if not registered</returns>
        IObjectPool<T> GetPool<T>() where T : class, IPooledObject, new();

        /// <summary>
        /// Gets the pool for the specified type as a generic IObjectPool.
        /// </summary>
        /// <param name="poolType">Type to get pool for</param>
        /// <returns>Pool instance or null if not registered</returns>
        IObjectPool GetPool(Type poolType);

        /// <summary>
        /// Checks if a pool is registered for the specified type.
        /// </summary>
        /// <typeparam name="T">Type to check</typeparam>
        /// <returns>True if pool is registered</returns>
        bool IsPoolRegistered<T>() where T : class, IPooledObject;

        /// <summary>
        /// Checks if a pool is registered for the specified type.
        /// </summary>
        /// <param name="poolType">Type to check</param>
        /// <returns>True if pool is registered</returns>
        bool IsPoolRegistered(Type poolType);

        #endregion

        #region Pool Information

        /// <summary>
        /// Gets the number of registered pools.
        /// </summary>
        int RegisteredPoolCount { get; }

        /// <summary>
        /// Gets the types of all registered pools.
        /// </summary>
        /// <returns>Collection of registered pool types</returns>
        IEnumerable<Type> GetRegisteredPoolTypes();

        /// <summary>
        /// Gets all registered pools as generic IObjectPool instances.
        /// </summary>
        /// <returns>Dictionary of pool types to pool instances</returns>
        Dictionary<Type, IObjectPool> GetAllPools();

        #endregion

        #region Statistics

        /// <summary>
        /// Gets statistics for all registered pools.
        /// </summary>
        /// <returns>Dictionary of pool statistics by type name</returns>
        Dictionary<string, PoolStatistics> GetAllPoolStatistics();

        /// <summary>
        /// Gets statistics for a specific pool type.
        /// </summary>
        /// <typeparam name="T">Pool type to get statistics for</typeparam>
        /// <returns>Pool statistics or null if not registered</returns>
        PoolStatistics GetPoolStatistics<T>() where T : class, IPooledObject;

        /// <summary>
        /// Gets statistics for a specific pool type.
        /// </summary>
        /// <param name="poolType">Pool type to get statistics for</param>
        /// <returns>Pool statistics or null if not registered</returns>
        PoolStatistics GetPoolStatistics(Type poolType);

        #endregion

        #region Validation

        /// <summary>
        /// Validates all registered pools.
        /// </summary>
        /// <returns>True if all pools are valid</returns>
        bool ValidateAllPools();

        /// <summary>
        /// Validates a specific pool.
        /// </summary>
        /// <typeparam name="T">Pool type to validate</typeparam>
        /// <returns>True if pool is valid</returns>
        bool ValidatePool<T>() where T : class, IPooledObject;

        /// <summary>
        /// Validates a specific pool.
        /// </summary>
        /// <param name="poolType">Pool type to validate</param>
        /// <returns>True if pool is valid</returns>
        bool ValidatePool(Type poolType);

        #endregion

        #region Maintenance

        /// <summary>
        /// Clears all objects from all registered pools.
        /// </summary>
        void ClearAllPools();

        /// <summary>
        /// Clears all objects from a specific pool.
        /// </summary>
        /// <typeparam name="T">Pool type to clear</typeparam>
        void ClearPool<T>() where T : class, IPooledObject;

        /// <summary>
        /// Clears all objects from a specific pool.
        /// </summary>
        /// <param name="poolType">Pool type to clear</param>
        void ClearPool(Type poolType);

        /// <summary>
        /// Trims excess objects from all registered pools.
        /// </summary>
        void TrimAllPools();

        /// <summary>
        /// Trims excess objects from a specific pool.
        /// </summary>
        /// <typeparam name="T">Pool type to trim</typeparam>
        void TrimPool<T>() where T : class, IPooledObject;

        /// <summary>
        /// Trims excess objects from a specific pool.
        /// </summary>
        /// <param name="poolType">Pool type to trim</param>
        void TrimPool(Type poolType);

        #endregion
    }
}