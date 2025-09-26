using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.Tests.Shared.TestDoubles.Fakes
{
    /// <summary>
    /// Fake implementation of IPoolingService for TDD testing.
    /// Creates new instances without actual pooling logic.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// </summary>
    public sealed class FakePoolingService : IPoolingService
    {
        private readonly Dictionary<Type, PoolConfiguration> _registeredPools = new();
        private readonly Dictionary<Type, int> _getCallCounts = new();
        private readonly Dictionary<Type, int> _returnCallCounts = new();
        private readonly Dictionary<Type, int> _createdObjectCounts = new();
        private readonly object _lockObject = new();
        private bool _isDisposed;
        private int _totalInstancesCreated;

        #region Test Verification Properties

        /// <summary>
        /// Gets the total number of instances created.
        /// </summary>
        public int TotalInstancesCreated => _totalInstancesCreated;

        /// <summary>
        /// Gets the number of Get calls for a specific type.
        /// </summary>
        public int GetCallCount<T>() where T : class, IPooledObject, new()
        {
            lock (_lockObject)
            {
                return _getCallCounts.TryGetValue(typeof(T), out var count) ? count : 0;
            }
        }

        /// <summary>
        /// Gets the number of Return calls for a specific type.
        /// </summary>
        public int ReturnCallCount<T>() where T : class, IPooledObject, new()
        {
            lock (_lockObject)
            {
                return _returnCallCounts.TryGetValue(typeof(T), out var count) ? count : 0;
            }
        }

        /// <summary>
        /// Gets the number of created objects for a specific type.
        /// </summary>
        public int CreatedObjectCount<T>() where T : class, IPooledObject, new()
        {
            lock (_lockObject)
            {
                return _createdObjectCounts.TryGetValue(typeof(T), out var count) ? count : 0;
            }
        }

        /// <summary>
        /// Checks if a pool is registered for the specified type.
        /// </summary>
        public bool IsPoolRegistered<T>() where T : class, IPooledObject, new()
        {
            lock (_lockObject)
            {
                return _registeredPools.ContainsKey(typeof(T));
            }
        }

        /// <summary>
        /// Gets all registered pool types.
        /// </summary>
        public IReadOnlyList<Type> RegisteredPoolTypes
        {
            get
            {
                lock (_lockObject)
                {
                    return _registeredPools.Keys.ToList();
                }
            }
        }

        /// <summary>
        /// Clears all recorded interactions and registered pools.
        /// </summary>
        public void ClearRecordedInteractions()
        {
            lock (_lockObject)
            {
                _getCallCounts.Clear();
                _returnCallCounts.Clear();
                _createdObjectCounts.Clear();
                _totalInstancesCreated = 0;
            }
        }

        #endregion

        #region IPoolingService Implementation - Fake Behavior

        // Properties
        public bool IsEnabled { get; set; } = true;
        public IMessageBusService MessageBus { get; private set; }

        public FakePoolingService(IMessageBusService messageBus = null)
        {
            MessageBus = messageBus;
        }

        // Pool management - creates new instances without pooling
        public T Get<T>() where T : class, IPooledObject, new()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FakePoolingService));

            if (!IsPoolRegistered<T>())
                throw new InvalidOperationException($"Pool for type {typeof(T).Name} is not registered");

            lock (_lockObject)
            {
                var typeKey = typeof(T);
                _getCallCounts[typeKey] = _getCallCounts.TryGetValue(typeKey, out var count) ? count + 1 : 1;
                _createdObjectCounts[typeKey] = _createdObjectCounts.TryGetValue(typeKey, out var created) ? created + 1 : 1;
                _totalInstancesCreated++;

                // Create new instance (no actual pooling)
                var instance = new T();

                // Set basic pooled object properties
                var config = _registeredPools[typeKey];
                instance.PoolName = config.Name;
                instance.PoolId = Guid.NewGuid();
                instance.LastUsed = DateTime.UtcNow;
                instance.UseCount = 1;

                // Call lifecycle method
                instance.OnGet();

                return instance;
            }
        }

        public async UniTask<T> GetAsync<T>() where T : class, IPooledObject, new()
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;
            return Get<T>();
        }

        public void Return<T>(T item) where T : class, IPooledObject, new()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FakePoolingService));

            if (item == null)
                return;

            if (!IsPoolRegistered<T>())
                throw new InvalidOperationException($"Pool for type {typeof(T).Name} is not registered");

            lock (_lockObject)
            {
                var typeKey = typeof(T);
                _returnCallCounts[typeKey] = _returnCallCounts.TryGetValue(typeKey, out var count) ? count + 1 : 1;

                // Call lifecycle methods
                item.OnReturn();
                item.Reset();
                item.LastUsed = DateTime.UtcNow;

                // Note: Fake doesn't actually pool objects, just records the call
            }
        }

        public async UniTask ReturnAsync<T>(T item) where T : class, IPooledObject, new()
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;
            Return(item);
        }

        public T[] GetMultiple<T>(int count) where T : class, IPooledObject, new()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FakePoolingService));

            if (count <= 0)
                return Array.Empty<T>();

            var items = new T[count];
            for (int i = 0; i < count; i++)
            {
                items[i] = Get<T>();
            }
            return items;
        }

        public async UniTask<T[]> GetMultipleAsync<T>(int count) where T : class, IPooledObject, new()
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;
            return GetMultiple<T>(count);
        }

        public void ReturnMultiple<T>(T[] items) where T : class, IPooledObject, new()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FakePoolingService));

            if (items == null)
                return;

            foreach (var item in items)
            {
                Return(item);
            }
        }

        public async UniTask ReturnMultipleAsync<T>(T[] items) where T : class, IPooledObject, new()
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;
            ReturnMultiple(items);
        }

        // Pool registration - minimal implementation
        public void RegisterPool<T>(PoolConfiguration config) where T : class, IPooledObject, new()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FakePoolingService));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            lock (_lockObject)
            {
                var typeKey = typeof(T);
                if (_registeredPools.ContainsKey(typeKey))
                    throw new InvalidOperationException($"Pool for type {typeof(T).Name} is already registered");

                _registeredPools[typeKey] = config;
            }
        }

        public void RegisterPool<T>(string poolName) where T : class, IPooledObject, new()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FakePoolingService));

            if (string.IsNullOrEmpty(poolName))
                throw new ArgumentException("Pool name cannot be null or empty", nameof(poolName));

            // Create a basic configuration for the pool using static factory method
            var config = PoolConfiguration.CreateDefault(poolName);

            RegisterPool<T>(config);
        }

        public void UnregisterPool<T>() where T : class, IPooledObject, new()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FakePoolingService));

            lock (_lockObject)
            {
                var typeKey = typeof(T);
                var removed = _registeredPools.Remove(typeKey);
                if (removed)
                {
                    _getCallCounts.Remove(typeKey);
                    _returnCallCounts.Remove(typeKey);
                    _createdObjectCounts.Remove(typeKey);
                }
            }
        }

        // Pool information - returns fake data
        public PoolStatistics GetPoolStatistics<T>() where T : class, IPooledObject, new()
        {
            if (!IsPoolRegistered<T>())
                return null;

            lock (_lockObject)
            {
                var typeKey = typeof(T);
                var getCount = _getCallCounts.TryGetValue(typeKey, out var gets) ? gets : 0;
                var returnCount = _returnCallCounts.TryGetValue(typeKey, out var returns) ? returns : 0;
                var createdCount = _createdObjectCounts.TryGetValue(typeKey, out var created) ? created : 0;

                return new PoolStatistics
                {
                    TotalCount = 0, // Fake doesn't actually pool
                    AvailableCount = 0,
                    ActiveCount = Math.Max(getCount - returnCount, 0),
                    PeakActiveCount = Math.Max(getCount - returnCount, 0),
                    PeakSize = 0,
                    TotalCreated = createdCount,
                    TotalGets = getCount,
                    TotalReturns = returnCount,
                    TotalDestroyed = 0,
                    TotalRequestCount = getCount,
                    FailedGets = 0,
                    CacheHits = 0, // Fake doesn't actually pool
                    CacheMisses = getCount, // All gets are misses since no pooling
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    LastMaintenance = DateTime.UtcNow,
                    InitialCapacity = _registeredPools[typeKey].InitialCapacity,
                    MaxCapacity = _registeredPools[typeKey].MaxCapacity
                };
            }
        }

        public Dictionary<string, PoolStatistics> GetAllPoolStatistics()
        {
            lock (_lockObject)
            {
                var stats = new Dictionary<string, PoolStatistics>();
                foreach (var kvp in _registeredPools)
                {
                    var poolType = kvp.Key;
                    var config = kvp.Value;

                    // Use reflection to call GetPoolStatistics<T>() for each type
                    var method = GetType().GetMethod(nameof(GetPoolStatistics))?.MakeGenericMethod(poolType);
                    if (method?.Invoke(this, null) is PoolStatistics poolStats)
                    {
                        stats[poolType.Name] = poolStats;
                    }
                }
                return stats;
            }
        }

        // Pool operations - no-op implementations for fake
        public void ClearPool<T>() where T : class, IPooledObject, new()
        {
            // No-op: fake doesn't actually maintain pools
        }

        public void ClearAllPools()
        {
            // No-op: fake doesn't actually maintain pools
        }

        public void WarmUpPool<T>(int targetSize) where T : class, IPooledObject, new()
        {
            // No-op: fake doesn't pre-create objects
        }

        public async UniTask WarmUpPoolAsync<T>(int targetSize) where T : class, IPooledObject, new()
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;
            // No-op: fake doesn't pre-create objects
        }

        public void TrimPool<T>(int targetSize) where T : class, IPooledObject, new()
        {
            // No-op: fake doesn't maintain pool sizes
        }

        public void TrimPool<T>() where T : class, IPooledObject, new()
        {
            // No-op: fake doesn't maintain pool sizes
        }

        public void TrimAllPools()
        {
            // No-op: fake doesn't maintain pool sizes
        }

        // Pool validation - simple implementations for fake
        public bool ValidatePool<T>() where T : class, IPooledObject, new()
        {
            return IsPoolRegistered<T>();
        }

        public bool ValidateAllPools()
        {
            lock (_lockObject)
            {
                return _registeredPools.Count > 0;
            }
        }

        // Pool state snapshot methods - fake implementations
        public async UniTask<PoolStateSnapshot> GetPoolStateSnapshotAsync<T>() where T : class, IPooledObject, new()
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;

            if (!IsPoolRegistered<T>())
                return null;

            lock (_lockObject)
            {
                var typeKey = typeof(T);
                var config = _registeredPools[typeKey];
                var getCount = _getCallCounts.TryGetValue(typeKey, out var gets) ? gets : 0;
                var returnCount = _returnCallCounts.TryGetValue(typeKey, out var returns) ? returns : 0;
                var createdCount = _createdObjectCounts.TryGetValue(typeKey, out var created) ? created : 0;

                return PoolStateSnapshot.Create(
                    poolId: Guid.NewGuid(),
                    poolName: config.Name,
                    poolType: typeof(T).Name,
                    strategyName: config.StrategyType.ToString());
            }
        }

        public async UniTask<PoolStateSnapshot> LoadPoolStateSnapshotAsync<T>() where T : class, IPooledObject, new()
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;

            // Fake implementation - just return current state
            return await GetPoolStateSnapshotAsync<T>();
        }

        public async UniTask<bool> SavePoolStateSnapshotAsync<T>() where T : class, IPooledObject, new()
        {
            // Unity Test Runner compatible async - no actual async work
            await UniTask.CompletedTask;

            // Fake implementation - always succeeds
            return IsPoolRegistered<T>();
        }

        // Health and monitoring - simple implementations
        public HealthStatus GetHealthStatus()
        {
            return IsEnabled ? HealthStatus.Healthy : HealthStatus.Unhealthy;
        }

        public ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default)
        {
            return ValidationResult.Success("FakePoolingService");
        }

        public bool PerformHealthCheck()
        {
            return IsEnabled;
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (!_isDisposed)
            {
                lock (_lockObject)
                {
                    _registeredPools.Clear();
                    ClearRecordedInteractions();
                }
                _isDisposed = true;
            }
        }

        #endregion
    }
}