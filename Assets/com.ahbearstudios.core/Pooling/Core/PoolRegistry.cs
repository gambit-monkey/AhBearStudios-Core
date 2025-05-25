
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Profiling;
using AhBearStudios.Pooling.Diagnostics;

namespace AhBearStudios.Pooling.Core
{
    /// <summary>
    /// Registry for managing object pools with thread-safe access.
    /// Provides centralized pool management with name and type-based lookups.
    /// </summary>
    public class PoolRegistry : IPoolRegistry
    {
        private readonly Dictionary<string, IPool> _pools;
        private readonly Dictionary<Type, string> _typeToPoolMap;
        private readonly ReadOnlyCollection<string> _poolNamesView;
        private readonly object _lockObject = new object();
        private readonly ProfilerMarker _registerMarker = new ProfilerMarker("PoolRegistry.Register");
        private readonly ProfilerMarker _lookupMarker = new ProfilerMarker("PoolRegistry.Lookup");
        private PoolNameConflictResolution _defaultConflictResolution;
        private bool _isDisposed;

        /// <inheritdoc />
        public int Count => _pools.Count;

        /// <inheritdoc />
        public bool IsDisposed => _isDisposed;

        /// <inheritdoc />
        public string RegistryName { get; }

        /// <summary>
        /// Initializes a new pool registry with optional name.
        /// </summary>
        /// <param name="registryName">Optional name for the registry</param>
        public PoolRegistry(string registryName = null)
        {
            RegistryName = registryName ?? "Default";
            _pools = new Dictionary<string, IPool>();
            _typeToPoolMap = new Dictionary<Type, string>();
            _poolNamesView = new ReadOnlyCollection<string>(_pools.Keys.ToList());
            _defaultConflictResolution = PoolNameConflictResolution.ThrowException;
        }

        /// <inheritdoc />
        public void SetConflictResolutionStrategy(PoolNameConflictResolution strategy)
        {
            _defaultConflictResolution = strategy;
        }

        /// <inheritdoc />
        public string RegisterPool(IPool pool, string name = null, PoolNameConflictResolution? conflictResolution = null)
        {
            ThrowIfDisposed();
            if (pool == null) throw new ArgumentNullException(nameof(pool));

            using (_registerMarker.Auto())
            {
                string poolName = name ?? $"Pool_{Guid.NewGuid()}";
                var resolution = conflictResolution ?? _defaultConflictResolution;

                lock (_lockObject)
                {
                    if (_pools.TryGetValue(poolName, out var existingPool))
                    {
                        switch (resolution)
                        {
                            case PoolNameConflictResolution.ThrowException:
                                throw new ArgumentException(
                                    $"Pool with name '{poolName}' exists but contains items of type {existingPool.ItemType.Name}, not {pool.ItemType.Name}");

                            case PoolNameConflictResolution.AutoRenameNew:
                                int suffix = 1;
                                string newName;
                                do
                                {
                                    newName = $"{poolName}_{suffix++}";
                                } while (_pools.ContainsKey(newName));
                                poolName = newName;
                                break;

                            case PoolNameConflictResolution.Replace:
                                _pools.Remove(poolName);
                                if (_typeToPoolMap.ContainsValue(poolName))
                                {
                                    var typeToRemove = _typeToPoolMap.First(x => x.Value == poolName).Key;
                                    _typeToPoolMap.Remove(typeToRemove);
                                }
                                break;
                        }
                    }

                    _pools[poolName] = pool;
                    
                    // Only register the first pool of a given type
                    if (!_typeToPoolMap.ContainsKey(pool.ItemType))
                    {
                        _typeToPoolMap[pool.ItemType] = poolName;
                    }
                }

                return poolName;
            }
        }

        /// <inheritdoc />
        public IPoolRegistry RegisterPool<T>(IPool<T> pool, string name = null)
        {
            RegisterPool((IPool)pool, name);
            return this;
        }

        /// <inheritdoc />
        public bool UnregisterPool(string poolName)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(poolName)) throw new ArgumentNullException(nameof(poolName));

            lock (_lockObject)
            {
                if (_pools.TryGetValue(poolName, out var pool))
                {
                    _pools.Remove(poolName);
                    if (_typeToPoolMap.ContainsKey(pool.ItemType) && _typeToPoolMap[pool.ItemType] == poolName)
                    {
                        _typeToPoolMap.Remove(pool.ItemType);
                    }
                    return true;
                }
                return false;
            }
        }

        /// <inheritdoc />
        public bool UnregisterPool(IPool pool)
        {
            ThrowIfDisposed();
            if (pool == null) throw new ArgumentNullException(nameof(pool));

            lock (_lockObject)
            {
                var entry = _pools.FirstOrDefault(x => x.Value == pool);
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    return UnregisterPool(entry.Key);
                }
                return false;
            }
        }

        /// <inheritdoc />
        public bool UnregisterPoolByType<T>()
        {
            ThrowIfDisposed();

            lock (_lockObject)
            {
                if (_typeToPoolMap.TryGetValue(typeof(T), out var poolName))
                {
                    return UnregisterPool(poolName);
                }
                return false;
            }
        }

        /// <inheritdoc />
        public IPool GetPool(string poolName)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(poolName)) throw new ArgumentNullException(nameof(poolName));

            using (_lookupMarker.Auto())
            {
                lock (_lockObject)
                {
                    return _pools.TryGetValue(poolName, out var pool) ? pool : null;
                }
            }
        }

        /// <inheritdoc />
        public IPool<T> GetPool<T>(string poolName)
        {
            var pool = GetPool(poolName);
            return pool as IPool<T>;
        }

        /// <inheritdoc />
        public IPool<T> GetPoolByType<T>()
        {
            ThrowIfDisposed();

            using (_lookupMarker.Auto())
            {
                lock (_lockObject)
                {
                    if (_typeToPoolMap.TryGetValue(typeof(T), out var poolName))
                    {
                        return GetPool<T>(poolName);
                    }
                    return null;
                }
            }
        }

        /// <inheritdoc />
        public bool HasPool(string poolName)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(poolName)) throw new ArgumentNullException(nameof(poolName));

            lock (_lockObject)
            {
                return _pools.ContainsKey(poolName);
            }
        }

        /// <inheritdoc />
        public bool HasPoolForType<T>()
        {
            ThrowIfDisposed();

            lock (_lockObject)
            {
                return _typeToPoolMap.ContainsKey(typeof(T));
            }
        }

        /// <inheritdoc />
        public IReadOnlyCollection<string> GetAllPoolNames()
        {
            ThrowIfDisposed();
            return _poolNamesView;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IPool> GetAllPools()
        {
            ThrowIfDisposed();
            lock (_lockObject)
            {
                return new ReadOnlyCollection<IPool>(_pools.Values.ToList());
            }
        }

        /// <inheritdoc />
        public void ClearAllPools(bool dispose = false)
        {
            ThrowIfDisposed();

            lock (_lockObject)
            {
                if (dispose)
                {
                    foreach (var pool in _pools.Values)
                    {
                        if (pool is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                }
                _pools.Clear();
                _typeToPoolMap.Clear();
            }
        }

        /// <inheritdoc />
        public Dictionary<string, Dictionary<string, object>> GetAllPoolMetrics()
        {
            ThrowIfDisposed();

            lock (_lockObject)
            {
                var metrics = new Dictionary<string, Dictionary<string, object>>();
                foreach (var kvp in _pools)
                {
                    if (kvp.Value is IPoolMetrics poolMetrics)
                    {
                        metrics[kvp.Key] = poolMetrics.GetMetrics();
                    }
                }
                return metrics;
            }
        }

        /// <inheritdoc />
        public void ResetAllPoolMetrics()
        {
            ThrowIfDisposed();

            lock (_lockObject)
            {
                foreach (var pool in _pools.Values)
                {
                    if (pool is IPoolMetrics poolMetrics)
                    {
                        poolMetrics.ResetMetrics();
                    }
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    ClearAllPools(true);
                }
                _isDisposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}