using System;
using System.Collections.Generic;
using Unity.Collections;
using ZLinq;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Tests.Shared.Mocks
{
    public sealed class MockPoolingService : IPoolingService
    {
        private readonly Dictionary<Type, object> _poolConfigs = new Dictionary<Type, object>();
        private readonly Dictionary<Type, int> _getCallCounts = new Dictionary<Type, int>();
        private readonly Dictionary<Type, int> _returnCallCounts = new Dictionary<Type, int>();
        private readonly Dictionary<Type, Queue<object>> _objectQueues = new Dictionary<Type, Queue<object>>();

        public bool IsEnabled { get; set; } = true;
        public int TotalGetCalls { get; private set; }
        public int TotalReturnCalls { get; private set; }
        public bool ShouldThrowOnGet { get; set; }
        public bool ShouldThrowOnReturn { get; set; }
        public bool SimulateActualPooling { get; set; } = true;

        public void RegisterPool<T>(PoolConfiguration<T> configuration) where T : class, new()
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _poolConfigs[typeof(T)] = configuration;

            if (SimulateActualPooling && !_objectQueues.ContainsKey(typeof(T)))
            {
                _objectQueues[typeof(T)] = new Queue<object>();

                // Pre-populate with initial capacity
                for (int i = 0; i < configuration.InitialCapacity; i++)
                {
                    _objectQueues[typeof(T)].Enqueue(new T());
                }
            }
        }

        public T Get<T>() where T : class, new()
        {
            TotalGetCalls++;

            if (!_getCallCounts.ContainsKey(typeof(T)))
                _getCallCounts[typeof(T)] = 0;
            _getCallCounts[typeof(T)]++;

            if (ShouldThrowOnGet)
                throw new InvalidOperationException("Mock pooling get error");

            if (!IsPoolRegistered<T>())
                throw new InvalidOperationException($"Pool for type {typeof(T).Name} is not registered");

            if (SimulateActualPooling && _objectQueues.TryGetValue(typeof(T), out var queue) && queue.Count > 0)
            {
                return (T)queue.Dequeue();
            }

            // Create new instance if pool is empty or not simulating
            return new T();
        }

        public void Return<T>(T item) where T : class
        {
            if (item == null)
                return;

            TotalReturnCalls++;

            if (!_returnCallCounts.ContainsKey(typeof(T)))
                _returnCallCounts[typeof(T)] = 0;
            _returnCallCounts[typeof(T)]++;

            if (ShouldThrowOnReturn)
                throw new InvalidOperationException("Mock pooling return error");

            if (!IsPoolRegistered<T>())
                throw new InvalidOperationException($"Pool for type {typeof(T).Name} is not registered");

            if (SimulateActualPooling && _objectQueues.TryGetValue(typeof(T), out var queue))
            {
                queue.Enqueue(item);
            }
        }

        public bool IsPoolRegistered<T>() where T : class
        {
            return _poolConfigs.ContainsKey(typeof(T));
        }

        public PoolStatistics GetPoolStatistics<T>() where T : class
        {
            if (!IsPoolRegistered<T>())
                return null;

            var getCount = _getCallCounts.TryGetValue(typeof(T), out var gets) ? gets : 0;
            var returnCount = _returnCallCounts.TryGetValue(typeof(T), out var returns) ? returns : 0;
            var activeCount = getCount - returnCount;
            var availableCount = SimulateActualPooling && _objectQueues.TryGetValue(typeof(T), out var queue) ? queue.Count : 0;

            return PoolStatistics.Create(
                poolId: Guid.NewGuid().ToString(),
                typeName: typeof(T).Name,
                totalCreated: getCount,
                totalDestroyed: 0,
                activeCount: activeCount,
                availableCount: availableCount,
                peakUsage: Math.Max(activeCount, availableCount),
                getRequests: getCount,
                returnRequests: returnCount,
                expansions: 0,
                contractions: 0);
        }

        public IEnumerable<PoolStatistics> GetAllPoolStatistics()
        {
            foreach (var poolType in _poolConfigs.Keys)
            {
                var method = typeof(MockPoolingService).GetMethod(nameof(GetPoolStatistics));
                var genericMethod = method.MakeGenericMethod(poolType);
                var stats = (PoolStatistics)genericMethod.Invoke(this, null);
                if (stats != null)
                    yield return stats;
            }
        }

        public int GetCallCount<T>() where T : class
        {
            return _getCallCounts.TryGetValue(typeof(T), out var count) ? count : 0;
        }

        public int GetReturnCount<T>() where T : class
        {
            return _returnCallCounts.TryGetValue(typeof(T), out var count) ? count : 0;
        }

        public void Clear()
        {
            _poolConfigs.Clear();
            _getCallCounts.Clear();
            _returnCallCounts.Clear();
            _objectQueues.Clear();
            TotalGetCalls = 0;
            TotalReturnCalls = 0;
        }

        public ValidationResult ValidateConfiguration()
        {
            return ValidationResult.Success("MockPoolingService");
        }

        public void Dispose()
        {
            Clear();
        }

        public async UniTask WarmupPoolAsync<T>(int count = -1) where T : class, new()
        {
            await UniTask.CompletedTask;
            // Mock implementation - no actual warmup needed
        }

        public async UniTask DrainPoolAsync<T>() where T : class
        {
            if (SimulateActualPooling && _objectQueues.TryGetValue(typeof(T), out var queue))
            {
                queue.Clear();
            }
            await UniTask.CompletedTask;
        }
    }
}