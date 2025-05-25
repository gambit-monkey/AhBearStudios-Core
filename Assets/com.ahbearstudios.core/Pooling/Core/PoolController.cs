using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Pooling.Factories;
using AhBearStudios.Core.Pooling.Pools.Native;

namespace AhBearStudios.Core.Pooling
{
    /// <summary>
    /// Domain-specific controller for managing a logically related group of pools.
    /// Uses the PoolRegistry as its backend storage, while providing a simpler interface
    /// for managing pools within a specific context.
    /// </summary>
    public sealed class PoolController : IDisposable
    {
        private readonly string _domain;
        private readonly HashSet<Guid> _managedPoolIds = new HashSet<Guid>();
        private readonly IPoolRegistry _registry;
        private readonly IPoolFactory _factory;
        private bool _isDisposed;

        /// <summary>
        /// Gets the domain name of this controller
        /// </summary>
        public string Domain => _domain;

        /// <summary>
        /// Gets the number of pools managed by this controller
        /// </summary>
        public int PoolCount => _managedPoolIds.Count;

        /// <summary>
        /// Gets whether this controller has been disposed
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PoolController"/> class.
        /// </summary>
        /// <param name="domain">Domain name for this controller</param>
        /// <param name="factory">Pool factory to create pools</param>
        /// <param name="registry">Optional custom registry to use</param>
        public PoolController(string domain, IPoolFactory factory, IPoolRegistry registry = null)
        {
            if (string.IsNullOrEmpty(domain))
            {
                throw new ArgumentException("Domain name cannot be null or empty", nameof(domain));
            }

            _domain = domain;
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _registry = registry ?? new PoolRegistry($"PoolRegistry-{domain}");
        }

        /// <summary>
        /// Creates a new pool with the specified configuration and registers it with this controller.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="config">Pool configuration</param>
        /// <param name="name">Name for the pool</param>
        /// <returns>The created pool</returns>
        public IPool<T> CreatePool<T>(IPoolConfig config, string name = null) where T : class
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(PoolController));
            }

            string poolName = string.IsNullOrEmpty(name) ? $"{_domain}_{typeof(T).Name}" : name;

            // Create pool using factory
            var pool = _factory.CreatePoolWithConfig(typeof(T), config, poolName) as IPool<T>;

            // Register with registry
            _registry.RegisterPool(pool, poolName);
            _managedPoolIds.Add(pool.Id);

            return pool;
        }

        /// <summary>
        /// Creates a new native pool with the specified configuration and registers it with this controller.
        /// </summary>
        /// <typeparam name="T">Type of items in the pool (must be unmanaged)</typeparam>
        /// <param name="config">Pool configuration</param>
        /// <param name="name">Name for the pool</param>
        /// <returns>The created native pool</returns>
        public INativePool<T> CreateNativePool<T>(NativePoolConfig config, string name = null) where T : unmanaged
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(PoolController));
            }

            string poolName = string.IsNullOrEmpty(name) ? $"{_domain}_{typeof(T).Name}" : name;

            // Native pools need special handling - create using appropriate method
            var nativePool = NativePoolRegistry.Instance.GetPool<T>(config.InitialCapacity) as INativePool<T>;

            // Track the native pool
            _managedPoolIds.Add(nativePool.Id);

            return nativePool;
        }

        /// <summary>
        /// Gets a pool by ID if it is managed by this controller.
        /// </summary>
        /// <param name="poolId">The ID of the pool to get</param>
        /// <returns>The pool with the specified ID, or null if not found or not managed by this controller</returns>
        public IPool GetPool(Guid poolId)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(PoolController));
            }

            if (!_managedPoolIds.Contains(poolId))
            {
                return null;
            }

            // Find pool by ID from all registry pools
            return _registry.GetAllPools().FirstOrDefault(p => p.Id == poolId);
        }

        /// <summary>
        /// Gets a pool by name if it is managed by this controller.
        /// </summary>
        /// <param name="poolName">The name of the pool to get</param>
        /// <returns>The pool with the specified name, or null if not found or not managed by this controller</returns>
        public IPool GetPoolByName(string poolName)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(PoolController));
            }

            if (string.IsNullOrEmpty(poolName))
            {
                throw new ArgumentException("Pool name cannot be null or empty", nameof(poolName));
            }

            var pool = _registry.GetPool(poolName);

            if (pool == null || !_managedPoolIds.Contains(pool.Id))
            {
                return null;
            }

            return pool;
        }

        /// <summary>
        /// Gets all pools managed by this controller.
        /// </summary>
        /// <returns>A list of all managed pools</returns>
        public List<IPool> GetAllPools()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(PoolController));
            }

            var result = new List<IPool>(_managedPoolIds.Count);

            foreach (var poolId in _managedPoolIds)
            {
                // Find pool by ID from all registry pools
                var pool = _registry.GetAllPools().FirstOrDefault(p => p.Id == poolId);
                if (pool != null)
                {
                    result.Add(pool);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets aggregated metrics for all pools managed by this controller.
        /// </summary>
        /// <returns>A dictionary containing aggregated metrics</returns>
        public Dictionary<string, object> GetAggregatedMetrics()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(PoolController));
            }

            var metrics = new Dictionary<string, object>
            {
                ["Domain"] = _domain,
                ["PoolCount"] = _managedPoolIds.Count
            };

            int totalActiveCount = 0;
            int totalCapacity = 0;
            int totalPeakUsage = 0;
            int totalCreatedCount = 0;
            int totalAcquiredCount = 0;
            int totalReleasedCount = 0;

            foreach (var poolId in _managedPoolIds)
            {
                // Find pool by ID from all registry pools
                var pool = _registry.GetAllPools().FirstOrDefault(p => p.Id == poolId) as IPoolMetrics;

                if (pool != null)
                {
                    totalActiveCount += pool.CurrentActiveCount;
                    totalCapacity += pool.CurrentCapacity;
                    totalPeakUsage += pool.PeakActiveCount;
                    totalCreatedCount += pool.TotalCreatedCount;
                    totalAcquiredCount += pool.TotalAcquiredCount;
                    totalReleasedCount += pool.TotalReleasedCount;
                }
            }

            metrics["TotalActiveCount"] = totalActiveCount;
            metrics["TotalCapacity"] = totalCapacity;
            metrics["TotalPeakUsage"] = totalPeakUsage;
            metrics["TotalCreatedCount"] = totalCreatedCount;
            metrics["TotalAcquiredCount"] = totalAcquiredCount;
            metrics["TotalReleasedCount"] = totalReleasedCount;
            metrics["OverallUsagePercentage"] = totalCapacity > 0 ? (float)totalActiveCount / totalCapacity : 0;

            return metrics;
        }

        /// <summary>
        /// Performs health checks on all managed pools.
        /// </summary>
        /// <param name="healthChecker">The health checker to use</param>
        /// <returns>List of pool health issues found</returns>
        public List<PoolHealthIssue> CheckPoolsHealth(IPoolHealthChecker healthChecker)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(PoolController));
            }

            if (healthChecker == null)
            {
                throw new ArgumentNullException(nameof(healthChecker));
            }

            var issues = new List<PoolHealthIssue>();

            foreach (var poolId in _managedPoolIds)
            {
                // Find pool by ID from all registry pools
                var pool = _registry.GetAllPools().FirstOrDefault(p => p.Id == poolId);

                if (pool != null)
                {
                    var poolIssues = healthChecker.CheckPoolHealth(pool);
                    issues.AddRange(poolIssues);
                }
            }

            return issues;
        }

        /// <summary>
        /// Performs auto-shrink checks on all managed pools that support shrinking.
        /// </summary>
        public void PerformAutoShrinkChecks()
        {
            if (_isDisposed)
            {
                return;
            }

            foreach (var poolId in _managedPoolIds)
            {
                // Find pool by ID from all registry pools
                var pool = _registry.GetAllPools().FirstOrDefault(p => p.Id == poolId) as IShrinkablePool;

                if (pool != null)
                {
                    // Pass the default shrink threshold value
                    pool.TryShrink(0.5f);
                }
            }
        }

        /// <summary>
        /// Resets metrics for all managed pools.
        /// </summary>
        public void ResetAllMetrics()
        {
            if (_isDisposed)
            {
                return;
            }

            foreach (var poolId in _managedPoolIds)
            {
                // Find pool by ID from all registry pools
                var pool = _registry.GetAllPools().FirstOrDefault(p => p.Id == poolId) as IPoolMetrics;

                if (pool != null)
                {
                    pool.ResetMetrics();
                }
            }
        }

        /// <summary>
        /// Disposes this controller and its managed pools.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            foreach (var poolId in _managedPoolIds)
            {
                // Find pool by ID from all registry pools
                var pool = _registry.GetAllPools().FirstOrDefault(p => p.Id == poolId);

                if (pool is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _managedPoolIds.Clear();
            _isDisposed = true;
        }
    }
}