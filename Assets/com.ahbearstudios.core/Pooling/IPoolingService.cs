using System;
using System.Collections.Generic;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Messaging;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Pooling
{
    /// <summary>
    /// Core pooling service interface for managing object pools with production-ready features.
    /// Provides high-performance object reuse with health monitoring and validation.
    /// Designed for Unity game development with 60+ FPS performance requirements.
    /// </summary>
    public interface IPoolingService : IDisposable
    {
        #region Pool Management
        
        /// <summary>
        /// Gets an object from the specified pool type.
        /// Creates new objects if pool is empty and capacity allows.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <returns>Object from the pool</returns>
        T Get<T>() where T : class, IPooledObject, new();
        
        /// <summary>
        /// Gets an object from the specified pool type asynchronously.
        /// Useful for pools that might need initialization or waiting.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <returns>Object from the pool</returns>
        UniTask<T> GetAsync<T>() where T : class, IPooledObject, new();
        
        /// <summary>
        /// Returns an object to its appropriate pool for reuse.
        /// Objects are validated and reset before being returned to the pool.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="item">Object to return to the pool</param>
        void Return<T>(T item) where T : class, IPooledObject, new();
        
        /// <summary>
        /// Returns an object to its pool asynchronously.
        /// Useful for pools that need cleanup or validation.
        /// </summary>
        /// <typeparam name="T">Type that implements IPooledObject</typeparam>
        /// <param name="item">Object to return to the pool</param>
        UniTask ReturnAsync<T>(T item) where T : class, IPooledObject, new();
        
        #endregion
        
        #region Pool Registration
        
        /// <summary>
        /// Registers a pool for the specified type with the given configuration.
        /// </summary>
        /// <typeparam name="T">Type to register pool for</typeparam>
        /// <param name="configuration">Pool configuration</param>
        void RegisterPool<T>(PoolConfiguration configuration) where T : class, IPooledObject, new();
        
        /// <summary>
        /// Registers a pool for the specified type with default configuration.
        /// </summary>
        /// <typeparam name="T">Type to register pool for</typeparam>
        /// <param name="poolName">Name of the pool</param>
        void RegisterPool<T>(string poolName = null) where T : class, IPooledObject, new();
        
        /// <summary>
        /// Unregisters and disposes a pool for the specified type.
        /// </summary>
        /// <typeparam name="T">Type to unregister pool for</typeparam>
        void UnregisterPool<T>() where T : class, IPooledObject, new();
        
        /// <summary>
        /// Checks if a pool is registered for the specified type.
        /// </summary>
        /// <typeparam name="T">Type to check</typeparam>
        /// <returns>True if pool is registered</returns>
        bool IsPoolRegistered<T>() where T : class, IPooledObject, new();
        
        #endregion
        
        #region Statistics and Monitoring
        
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
        PoolStatistics GetPoolStatistics<T>() where T : class, IPooledObject, new();
        
        /// <summary>
        /// Gets a complete pool state snapshot for analysis and persistence.
        /// Includes statistics, configuration, health status, and performance metrics.
        /// </summary>
        /// <typeparam name="T">Pool type to get snapshot for</typeparam>
        /// <returns>Complete pool state snapshot or null if pool not found</returns>
        UniTask<PoolStateSnapshot> GetPoolStateSnapshotAsync<T>() where T : class, IPooledObject, new();
        
        /// <summary>
        /// Saves a pool state snapshot to persistent storage for recovery and analysis.
        /// </summary>
        /// <typeparam name="T">Pool type to save snapshot for</typeparam>
        /// <returns>True if snapshot was saved successfully</returns>
        UniTask<bool> SavePoolStateSnapshotAsync<T>() where T : class, IPooledObject, new();
        
        /// <summary>
        /// Loads a previously saved pool state snapshot from persistent storage.
        /// </summary>
        /// <typeparam name="T">Pool type to load snapshot for</typeparam>
        /// <returns>Loaded pool state snapshot or null if not found</returns>
        UniTask<PoolStateSnapshot> LoadPoolStateSnapshotAsync<T>() where T : class, IPooledObject, new();
        
        /// <summary>
        /// Validates all pools and returns overall health status.
        /// </summary>
        /// <returns>True if all pools are healthy</returns>
        bool ValidateAllPools();
        
        /// <summary>
        /// Validates a specific pool.
        /// </summary>
        /// <typeparam name="T">Pool type to validate</typeparam>
        /// <returns>True if pool is healthy</returns>
        bool ValidatePool<T>() where T : class, IPooledObject, new();
        
        #endregion
        
        #region Maintenance
        
        /// <summary>
        /// Clears all objects from all pools and releases resources.
        /// </summary>
        void ClearAllPools();
        
        /// <summary>
        /// Clears all objects from a specific pool.
        /// </summary>
        /// <typeparam name="T">Pool type to clear</typeparam>
        void ClearPool<T>() where T : class, IPooledObject, new();
        
        /// <summary>
        /// Removes excess objects from all pools to reduce memory usage.
        /// </summary>
        void TrimAllPools();
        
        /// <summary>
        /// Removes excess objects from a specific pool.
        /// </summary>
        /// <typeparam name="T">Pool type to trim</typeparam>
        void TrimPool<T>() where T : class, IPooledObject, new();
        
        #endregion
        
        #region Message Bus Integration
        
        /// <summary>
        /// Gets the message bus service used for publishing pool events.
        /// Pool events are published as IMessage records following CLAUDE.md guidelines.
        /// Events include: PoolObjectRetrievedMessage, PoolObjectReturnedMessage, 
        /// PoolCapacityReachedMessage, and PoolValidationIssuesMessage.
        /// </summary>
        IMessageBusService MessageBus { get; }
        
        #endregion
    }
}