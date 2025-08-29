using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ZLinq;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Pools;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Production-ready implementation of pool registration and lookup service.
    /// Provides thread-safe storage and retrieval of pool instances with comprehensive validation.
    /// Designed for Unity game development with 60+ FPS performance requirements.
    /// </summary>
    public sealed class PoolRegistry : IPoolRegistry
    {
        #region Private Fields

        private readonly ILoggingService _loggingService;
        private readonly ConcurrentDictionary<Type, object> _pools;
        private volatile bool _disposed;
        private readonly object _disposeLock = new object();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the PoolRegistry.
        /// </summary>
        /// <param name="loggingService">Logging service for system integration</param>
        public PoolRegistry(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _pools = new ConcurrentDictionary<Type, object>();
        }

        #endregion

        #region Pool Registration

        /// <inheritdoc />
        public bool RegisterPool<T>(IObjectPool<T> pool) where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            
            if (pool == null)
            {
                _loggingService.LogWarning($"Cannot register null pool for type {typeof(T).Name}");
                return false;
            }

            var poolType = typeof(T);
            
            if (_pools.TryAdd(poolType, pool))
            {
                _loggingService.LogInfo($"Successfully registered pool for type {poolType.Name}");
                return true;
            }

            _loggingService.LogWarning($"Pool for type {poolType.Name} is already registered");
            return false;
        }

        /// <inheritdoc />
        public bool UnregisterPool<T>() where T : class, IPooledObject
        {
            ThrowIfDisposed();
            
            var poolType = typeof(T);
            return UnregisterPoolInternal(poolType);
        }

        /// <inheritdoc />
        public void UnregisterAllPools()
        {
            ThrowIfDisposed();
            
            var poolTypes = _pools.Keys.AsValueEnumerable().ToList();
            
            foreach (var poolType in poolTypes)
            {
                UnregisterPoolInternal(poolType);
            }
        }

        #endregion

        #region Pool Lookup

        /// <inheritdoc />
        public IObjectPool<T> GetPool<T>() where T : class, IPooledObject, new()
        {
            ThrowIfDisposed();
            
            if (_pools.TryGetValue(typeof(T), out var poolObj))
            {
                return poolObj as IObjectPool<T>;
            }
            
            return null;
        }

        /// <inheritdoc />
        public IObjectPool GetPool(Type poolType)
        {
            ThrowIfDisposed();
            
            if (poolType == null)
                return null;

            if (_pools.TryGetValue(poolType, out var poolObj))
            {
                return poolObj as IObjectPool;
            }
            
            return null;
        }

        /// <inheritdoc />
        public bool IsPoolRegistered<T>() where T : class, IPooledObject
        {
            ThrowIfDisposed();
            return _pools.ContainsKey(typeof(T));
        }

        /// <inheritdoc />
        public bool IsPoolRegistered(Type poolType)
        {
            ThrowIfDisposed();
            
            if (poolType == null)
                return false;
                
            return _pools.ContainsKey(poolType);
        }

        #endregion

        #region Pool Information

        /// <inheritdoc />
        public int RegisteredPoolCount
        {
            get
            {
                ThrowIfDisposed();
                return _pools.Count;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Type> GetRegisteredPoolTypes()
        {
            ThrowIfDisposed();
            return _pools.Keys.AsValueEnumerable().ToList(); // Return a copy to prevent modification
        }

        /// <inheritdoc />
        public Dictionary<Type, IObjectPool> GetAllPools()
        {
            ThrowIfDisposed();
            
            var result = new Dictionary<Type, IObjectPool>();
            
            foreach (var kvp in _pools)
            {
                if (kvp.Value is IObjectPool pool)
                {
                    result[kvp.Key] = pool;
                }
            }
            
            return result;
        }

        #endregion

        #region Statistics

        /// <inheritdoc />
        public Dictionary<string, PoolStatistics> GetAllPoolStatistics()
        {
            ThrowIfDisposed();
            
            var statistics = new Dictionary<string, PoolStatistics>();
            
            foreach (var kvp in _pools)
            {
                if (kvp.Value is IObjectPool pool)
                {
                    try
                    {
                        statistics[kvp.Key.Name] = pool.GetStatistics();
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogException($"Failed to get statistics for pool {kvp.Key.Name}", ex);
                    }
                }
            }
            
            return statistics;
        }

        /// <inheritdoc />
        public PoolStatistics GetPoolStatistics<T>() where T : class, IPooledObject
        {
            ThrowIfDisposed();
            return GetPoolStatistics(typeof(T));
        }

        /// <inheritdoc />
        public PoolStatistics GetPoolStatistics(Type poolType)
        {
            ThrowIfDisposed();
            
            if (poolType == null)
                return null;

            if (_pools.TryGetValue(poolType, out var poolObj) && poolObj is IObjectPool pool)
            {
                try
                {
                    return pool.GetStatistics();
                }
                catch (Exception ex)
                {
                    _loggingService.LogException($"Failed to get statistics for pool {poolType.Name}", ex);
                }
            }
            
            return null;
        }

        #endregion

        #region Validation

        /// <inheritdoc />
        public bool ValidateAllPools()
        {
            ThrowIfDisposed();
            
            bool allValid = true;
            int totalIssues = 0;
            
            foreach (var kvp in _pools)
            {
                if (kvp.Value is IObjectPool pool)
                {
                    try
                    {
                        if (!pool.Validate())
                        {
                            allValid = false;
                            totalIssues++;
                            _loggingService.LogWarning($"Pool validation failed for type {kvp.Key.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogException($"Pool validation threw exception for type {kvp.Key.Name}", ex);
                        allValid = false;
                        totalIssues++;
                    }
                }
            }
            
            if (totalIssues > 0)
            {
                _loggingService.LogWarning($"Pool validation found {totalIssues} issues across {_pools.Count} pools");
            }
            
            return allValid;
        }

        /// <inheritdoc />
        public bool ValidatePool<T>() where T : class, IPooledObject
        {
            ThrowIfDisposed();
            return ValidatePool(typeof(T));
        }

        /// <inheritdoc />
        public bool ValidatePool(Type poolType)
        {
            ThrowIfDisposed();
            
            if (poolType == null)
                return false;

            if (_pools.TryGetValue(poolType, out var poolObj) && poolObj is IObjectPool pool)
            {
                try
                {
                    return pool.Validate();
                }
                catch (Exception ex)
                {
                    _loggingService.LogException($"Pool validation threw exception for type {poolType.Name}", ex);
                    return false;
                }
            }
            
            return false;
        }

        #endregion

        #region Maintenance

        /// <inheritdoc />
        public void ClearAllPools()
        {
            ThrowIfDisposed();
            
            foreach (var kvp in _pools)
            {
                if (kvp.Value is IObjectPool pool)
                {
                    try
                    {
                        pool.Clear();
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogException($"Failed to clear pool for type {kvp.Key.Name}", ex);
                    }
                }
            }
        }

        /// <inheritdoc />
        public void ClearPool<T>() where T : class, IPooledObject
        {
            ThrowIfDisposed();
            ClearPool(typeof(T));
        }

        /// <inheritdoc />
        public void ClearPool(Type poolType)
        {
            ThrowIfDisposed();
            
            if (poolType == null)
                return;

            if (_pools.TryGetValue(poolType, out var poolObj) && poolObj is IObjectPool pool)
            {
                try
                {
                    pool.Clear();
                }
                catch (Exception ex)
                {
                    _loggingService.LogException($"Failed to clear pool for type {poolType.Name}", ex);
                }
            }
        }

        /// <inheritdoc />
        public void TrimAllPools()
        {
            ThrowIfDisposed();
            
            foreach (var kvp in _pools)
            {
                if (kvp.Value is IObjectPool pool)
                {
                    try
                    {
                        pool.TrimExcess();
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogException($"Failed to trim pool for type {kvp.Key.Name}", ex);
                    }
                }
            }
        }

        /// <inheritdoc />
        public void TrimPool<T>() where T : class, IPooledObject
        {
            ThrowIfDisposed();
            TrimPool(typeof(T));
        }

        /// <inheritdoc />
        public void TrimPool(Type poolType)
        {
            ThrowIfDisposed();
            
            if (poolType == null)
                return;

            if (_pools.TryGetValue(poolType, out var poolObj) && poolObj is IObjectPool pool)
            {
                try
                {
                    pool.TrimExcess();
                }
                catch (Exception ex)
                {
                    _loggingService.LogException($"Failed to trim pool for type {poolType.Name}", ex);
                }
            }
        }

        #endregion

        #region Private Implementation

        private bool UnregisterPoolInternal(Type poolType)
        {
            if (_pools.TryRemove(poolType, out var poolObj))
            {
                if (poolObj is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogException($"Exception disposing pool for type {poolType.Name}", ex);
                    }
                }
                
                _loggingService.LogInfo($"Successfully unregistered pool for type {poolType.Name}");
                return true;
            }
            
            _loggingService.LogWarning($"No pool registered for type {poolType.Name}");
            return false;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PoolRegistry));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the pool registry and all registered pools.
        /// </summary>
        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (!_disposed)
                {
                    UnregisterAllPools();
                    _disposed = true;
                    _loggingService.LogInfo("PoolRegistry disposed successfully");
                }
            }
        }

        #endregion
    }
}