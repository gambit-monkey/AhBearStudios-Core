using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;
using AhBearStudios.Pooling.Services;
using Unity.Mathematics;
using UnityEngine;

namespace AhBearStudios.Pooling.Pools.Advanced
{
    /// <summary>
    /// Advanced object pool implementation that provides configurable auto-shrinking,
    /// lifecycle management, validation, and detailed metrics. Designed for high-performance
    /// single-threaded scenarios.
    /// </summary>
    /// <typeparam name="T">Type of objects to pool</typeparam>
    public sealed class AdvancedObjectPool<T> : IAdvancedObjectPool<T>, IDisposable where T : class
    {
        // Core pool components
        private readonly Stack<T> _inactiveItems;
        private readonly HashSet<T> _activeItems;
        private readonly Func<T> _factory;
        private readonly Action<T> _resetAction;
        private readonly Func<T, bool> _validator;
        
        // Pooling services and diagnostics
        private readonly IPoolProfiler _profiler;
        private readonly IPoolLogger _logger;
        private readonly IPoolDiagnostics _diagnostics;
        private readonly IPoolHealthChecker _healthChecker;
        private readonly IPoolingServiceLocator _serviceLocator;
        
        // Pool configuration
        private AdvancedPoolConfig _config;
        
        // Metrics data
        private PoolMetrics _metrics;
        
        // Pool properties
        private string _poolName;
        private readonly Guid _poolId;
        private float _lastShrinkTime;
        private bool _isDisposed;
        private bool _isInitialized;
        private bool _autoShrinkEnabled;

        #region IPool Implementation

        /// <summary>
        /// Gets whether this pool has been properly created and initialized
        /// </summary>
        public bool IsCreated => _isInitialized && !_isDisposed;

        /// <summary>
        /// Gets the total number of items in the pool (active + inactive)
        /// </summary>
        public int TotalCount => ActiveCount + InactiveCount;

        /// <summary>
        /// Gets the number of active items
        /// </summary>
        public int ActiveCount => _activeItems.Count;

        /// <summary>
        /// Gets the number of inactive items
        /// </summary>
        public int InactiveCount => _inactiveItems.Count;

        /// <summary>
        /// Gets the peak number of simultaneously active items
        /// </summary>
        public int PeakUsage => _metrics.PeakActiveCount;

        /// <summary>
        /// Gets the total number of items ever created by this pool
        /// </summary>
        public int TotalCreated => _metrics.TotalCreatedCount;

        /// <summary>
        /// Gets the type of items in the pool
        /// </summary>
        public Type ItemType => typeof(T);

        /// <summary>
        /// Gets whether this pool has been disposed
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Gets the name of this pool
        /// </summary>
        public string PoolName => _poolName;

        /// <summary>
        /// Gets the unique identifier for this pool
        /// </summary>
        public Guid Id => _poolId;

        /// <summary>
        /// Gets the threading mode for this pool
        /// </summary>
        public PoolThreadingMode ThreadingMode => PoolThreadingMode.SingleThreaded;

        #endregion

        #region IShrinkablePool Implementation

        /// <summary>
        /// Gets whether this pool supports automatic shrinking
        /// </summary>
        public bool SupportsAutoShrink => true;

        /// <summary>
        /// Gets or sets the minimum capacity that the pool will maintain even when shrinking
        /// </summary>
        public int MinimumCapacity
        {
            get => _config.MinimumCapacity;
            set => _config.MinimumCapacity = value;
        }

        /// <summary>
        /// Gets or sets the maximum capacity that the pool can grow to
        /// </summary>
        public int MaximumCapacity
        {
            get => _config.MaximumCapacity;
            set => _config.MaximumCapacity = value;
        }

        /// <summary>
        /// Gets or sets the shrink interval in seconds
        /// </summary>
        public float ShrinkInterval
        {
            get => _config.ShrinkInterval;
            set => _config.ShrinkInterval = value;
        }

        /// <summary>
        /// Gets or sets the growth factor when the pool needs to expand
        /// </summary>
        public float GrowthFactor
        {
            get => _config.GrowthFactor;
            set => _config.GrowthFactor = value;
        }

        /// <summary>
        /// Gets or sets the shrink threshold as a percentage (0-1)
        /// </summary>
        public float ShrinkThreshold
        {
            get => _config.ShrinkThreshold;
            set => _config.ShrinkThreshold = value;
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the AdvancedObjectPool class with dependency injection
        /// </summary>
        /// <param name="factory">Factory function to create new items</param>
        /// <param name="config">Configuration for the pool</param>
        /// <param name="profiler">Optional pool profiler for performance tracking</param>
        /// <param name="logger">Optional pool logger for logging events</param>
        /// <param name="diagnostics">Optional pool diagnostics for tracking metrics</param>
        /// <param name="healthChecker">Optional health checker for validating pool health</param>
        /// <param name="serviceLocator">Optional service locator for resolving dependencies</param>
        /// <param name="resetAction">Optional action to reset items when returned to pool</param>
        /// <param name="validator">Optional validator function to ensure items are valid</param>
        /// <param name="name">Optional name for the pool</param>
        public AdvancedObjectPool(
            Func<T> factory, 
            AdvancedPoolConfig config,
            IPoolProfiler profiler = null,
            IPoolLogger logger = null,
            IPoolDiagnostics diagnostics = null,
            IPoolHealthChecker healthChecker = null,
            IPoolingServiceLocator serviceLocator = null,
            Action<T> resetAction = null,
            Func<T, bool> validator = null,
            string name = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _resetAction = resetAction;
            _validator = validator;
            _poolName = string.IsNullOrEmpty(name) ? $"AdvPool<{typeof(T).Name}>" : name;
            _poolId = Guid.NewGuid();
            
            // Resolve services
            _serviceLocator = serviceLocator ?? DefaultPoolingServices.Instance;
            _profiler = profiler ?? _serviceLocator.GetService<IPoolProfiler>();
            _logger = logger ?? _serviceLocator.GetService<IPoolLogger>();
            _diagnostics = diagnostics ?? _serviceLocator.GetService<IPoolDiagnostics>();
            _healthChecker = healthChecker ?? _serviceLocator.GetService<IPoolHealthChecker>();
            
            // Initialize collections
            _inactiveItems = new Stack<T>(_config.InitialCapacity);
            _activeItems = new HashSet<T>();
            
            // Initialize metrics
            _metrics = new PoolMetrics(_poolId, _poolName, GetType(), typeof(T), _serviceLocator);
            
            // Register with diagnostics
            _diagnostics?.RegisterPool(this, _poolName);
            
            // Complete initialization
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the AdvancedObjectPool class with default services
        /// </summary>
        /// <param name="factory">Factory function to create new items</param>
        /// <param name="config">Configuration for the pool</param>
        /// <param name="resetAction">Optional action to reset items when returned to pool</param>
        /// <param name="validator">Optional validator function to ensure items are valid</param>
        /// <param name="name">Optional name for the pool</param>
        public AdvancedObjectPool(
            Func<T> factory,
            AdvancedPoolConfig config,
            Action<T> resetAction = null,
            Func<T, bool> validator = null,
            string name = null)
            : this(factory, config, null, null, null, null, DefaultPoolingServices.Instance, resetAction, validator, name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AdvancedObjectPool class with simplified parameters
        /// </summary>
        /// <param name="factory">Factory function to create new items</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="resetAction">Optional action to reset items when returned to pool</param>
        /// <param name="validator">Optional validator function to ensure items are valid</param>
        /// <param name="name">Optional name for the pool</param>
        public AdvancedObjectPool(
            Func<T> factory,
            int initialCapacity,
            Action<T> resetAction = null,
            Func<T, bool> validator = null,
            string name = null)
            : this(factory, new AdvancedPoolConfig
            {
                InitialCapacity = initialCapacity,
                MinimumCapacity = 0,
                MaximumCapacity = math.max(1000, initialCapacity * 2),
                GrowthFactor = 2.0f,
                ShrinkInterval = 60f,
                ShrinkThreshold = 0.25f
            }, null, null, null, null, DefaultPoolingServices.Instance, resetAction, validator, name)
        {
        }

        /// <summary>
        /// Acquires an item from the pool, creating a new one if necessary
        /// </summary>
        /// <returns>The acquired item</returns>
        public T Acquire()
        {
            using (_profiler?.Sample("AdvancedObjectPool.Acquire"))
            {
                if (IsDisposed)
                {
                    _logger?.LogErrorInstance($"[{PoolName}] Cannot acquire item from disposed pool");
                    throw new ObjectDisposedException(PoolName);
                }

                float startTime = Time.realtimeSinceStartup;
                T item = AcquireInternal();
                float acquireTime = (Time.realtimeSinceStartup - startTime) * 1000f; // Convert to ms
                
                _metrics.RecordAcquire(_poolId, ActiveCount, acquireTime);
                _diagnostics?.RecordAcquireComplete(this, ActiveCount, item);
                
                return item;
            }
        }

        /// <summary>
        /// Releases an item back to the pool
        /// </summary>
        /// <param name="item">The item to release</param>
        public void Release(T item)
        {
            using (_profiler?.Sample("AdvancedObjectPool.Release"))
            {
                if (IsDisposed)
                {
                    _logger?.LogWarningInstance($"[{PoolName}] Cannot release item to a disposed pool. Item will be abandoned.");
                    return;
                }

                if (item == null)
                {
                    _logger?.LogWarningInstance($"[{PoolName}] Attempted to release a null item");
                    return;
                }

                float startTime = Time.realtimeSinceStartup;
                ReleaseInternal(item);
                float releaseTime = (Time.realtimeSinceStartup - startTime) * 1000f; // Convert to ms
                
                _metrics.RecordRelease(_poolId, ActiveCount, releaseTime);
                _diagnostics?.RecordRelease(this, ActiveCount, item);
                
                // Check if auto-shrink is needed
                if (_autoShrinkEnabled)
                {
                    TryAutoShrink();
                }
            }
        }

        /// <summary>
        /// Acquires multiple items from the pool at once
        /// </summary>
        /// <param name="count">Number of items to acquire</param>
        /// <returns>List of acquired items</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is not positive</exception>
        public List<T> AcquireMultiple(int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
            
            using (_profiler?.Sample("AdvancedObjectPool.AcquireMultiple"))
            {
                var result = new List<T>(count);
                for (int i = 0; i < count; i++)
                {
                    result.Add(Acquire());
                }
                return result;
            }
        }

        /// <summary>
        /// Releases multiple items back to the pool at once
        /// </summary>
        /// <param name="items">Items to release</param>
        public void ReleaseMultiple(IEnumerable<T> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            
            using (_profiler?.Sample("AdvancedObjectPool.ReleaseMultiple"))
            {
                foreach (var item in items)
                {
                    Release(item);
                }
            }
        }

        /// <summary>
        /// Acquires an item and initializes it with a setup action
        /// </summary>
        /// <param name="setupAction">Action to initialize the item</param>
        /// <returns>The acquired and initialized item</returns>
        public T AcquireAndSetup(Action<T> setupAction)
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));
            
            T item = Acquire();
            try
            {
                setupAction(item);
                return item;
            }
            catch (Exception ex)
            {
                _logger?.LogErrorInstance($"[{PoolName}] Setup action failed: {ex.Message}");
                Release(item);
                throw;
            }
        }

        /// <summary>
        /// Sets the name of the pool
        /// </summary>
        /// <param name="newName">The new name for the pool</param>
        public void SetPoolName(string newName)
        {
            if (_diagnostics != null)
            {
                _diagnostics.UnregisterPool(this);
            }

            if (string.IsNullOrEmpty(newName))
                return;
        
            _poolName = newName;
    
            // Re-register with diagnostics to update the name
            if (_diagnostics != null)
            {
                _diagnostics.RegisterPool(this, newName);
            }
        }

        /// <summary>
        /// Ensures the pool has at least the specified capacity
        /// </summary>
        /// <param name="capacity">Required capacity</param>
        public void EnsureCapacity(int capacity)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(PoolName);
            
            using (_profiler?.Sample("AdvancedObjectPool.EnsureCapacity"))
            {
                // Calculate how many new items we need to create
                int targetCapacity = math.min(capacity, MaximumCapacity);
                int toCreate = targetCapacity - TotalCount;
                
                if (toCreate <= 0)
                    return;
                
                for (int i = 0; i < toCreate; i++)
                {
                    _inactiveItems.Push(CreateValidItem());
                }
                
                _metrics.RecordCreate(_poolId, toCreate);
                
                _logger?.LogDebugInstance($"[{PoolName}] Expanded pool capacity to {TotalCount}");
            }
        }

        /// <summary>
        /// Clears the pool, returning all active items to the inactive state
        /// </summary>
        public void Clear()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(PoolName);
            
            using (_profiler?.Sample("AdvancedObjectPool.Clear"))
            {
                ClearInternal();
                _metrics.ResetMetrics(_poolId);
                _logger?.LogDebugInstance($"[{PoolName}] Pool cleared");
            }
        }

        /// <summary>
        /// Gets a dictionary of metrics about the pool
        /// </summary>
        /// <returns>Dictionary mapping metric names to values</returns>
        public Dictionary<string, object> GetMetrics()
        {
            using (_profiler?.Sample("AdvancedObjectPool.GetMetrics"))
            {
                return _metrics.GetMetrics(_poolId);
            }
        }

        /// <summary>
        /// Attempts to shrink the pool's capacity to reduce memory usage
        /// </summary>
        /// <param name="threshold">Threshold factor (0-1) determining when shrinking occurs</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool TryShrink(float threshold)
        {
            if (IsDisposed || !SupportsAutoShrink)
                return false;
            
            using (_profiler?.Sample("AdvancedObjectPool.TryShrink"))
            {
                // Calculate usage ratio
                float usageRatio = TotalCount > 0 ? (float)ActiveCount / TotalCount : 0;
                
                // Only shrink if usage ratio is below threshold
                if (usageRatio > threshold)
                    return false;
                
                // Calculate how many excess items we have
                int minimumItems = math.max(MinimumCapacity, ActiveCount);
                int targetCapacity = math.max(minimumItems, (int)(ActiveCount / threshold));
                int excessItems = TotalCount - targetCapacity;
                
                if (excessItems <= 0)
                    return false;
                
                bool didShrink = ShrinkInternalByCount(excessItems);
                
                if (didShrink)
                {
                    _lastShrinkTime = Time.realtimeSinceStartup;
                    _logger?.LogDebugInstance($"[{PoolName}] Shrunk pool by {excessItems} items to {TotalCount} total items");
                }
                
                return didShrink;
            }
        }

        /// <summary>
        /// Explicitly enables or disables automatic shrinking
        /// </summary>
        /// <param name="enabled">Whether automatic shrinking should be enabled</param>
        public void SetAutoShrink(bool enabled)
        {
            _autoShrinkEnabled = enabled;
        }

        /// <summary>
        /// Explicitly shrinks the pool to the specified capacity
        /// </summary>
        /// <param name="targetCapacity">The target capacity to shrink to</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool ShrinkTo(int targetCapacity)
        {
            if (IsDisposed || !SupportsAutoShrink)
                return false;
            
            using (_profiler?.Sample("AdvancedObjectPool.ShrinkTo"))
            {
                // Ensure we don't go below minimum capacity or active count
                int effectiveMinimum = math.max(MinimumCapacity, ActiveCount);
                int safeTargetCapacity = math.max(effectiveMinimum, targetCapacity);
                
                // Check if we have excess items
                int excessItems = TotalCount - safeTargetCapacity;
                
                if (excessItems <= 0)
                    return false;
                
                bool didShrink = ShrinkInternalByCount(excessItems);
                
                if (didShrink)
                {
                    _lastShrinkTime = Time.realtimeSinceStartup;
                    _logger?.LogDebugInstance($"[{PoolName}] Explicitly shrunk pool to {TotalCount} total items");
                }
                
                return didShrink;
            }
        }

        /// <summary>
        /// Resets the pool's metrics
        /// </summary>
        public void ResetMetrics()
        {
            _metrics.ResetMetrics(_poolId);
        }

        /// <summary>
        /// Creates a new item and ensures it's valid
        /// </summary>
        /// <returns>A new valid item</returns>
        public T CreateValidItem()
        {
            using (_profiler?.Sample("AdvancedObjectPool.CreateValidItem"))
            {
                T item = _factory();
                
                if (item == null)
                {
                    _logger?.LogErrorInstance($"[{PoolName}] Factory returned null item");
                    throw new InvalidOperationException($"Factory for {PoolName} returned null");
                }
                
                if (_validator != null && !_validator(item))
                {
                    _logger?.LogWarningInstance($"[{PoolName}] Factory created invalid item, retrying");
                    DestroyItem(item);
                    return CreateValidItem(); // Recursively try again
                }
                
                return item;
            }
        }

        /// <summary>
        /// Destroys an item
        /// </summary>
        /// <param name="item">Item to destroy</param>
        public void DestroyItem(T item)
        {
            if (item == null)
                return;
            
            using (_profiler?.Sample("AdvancedObjectPool.DestroyItem"))
            {
                // If the item implements IDisposable, dispose it
                if (item is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogErrorInstance($"[{PoolName}] Error disposing item: {ex.Message}");
                    }
                }
                
                // If the item implements IPoolable, reset it
                if (item is IPoolable poolable)
                {
                    try
                    {
                        poolable.OnDestroy();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogErrorInstance($"[{PoolName}] Error calling OnDestroy on poolable item: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Prewarms the pool by creating the specified number of instances
        /// </summary>
        /// <param name="count">Number of instances to create</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative</exception>
        public void PrewarmPool(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative");
            
            if (IsDisposed)
                throw new ObjectDisposedException(PoolName);
            
            using (_profiler?.Sample("AdvancedObjectPool.PrewarmPool"))
            {
                int initialCount = TotalCount;
                int targetCapacity = math.min(initialCount + count, MaximumCapacity);
                int actualCount = targetCapacity - initialCount;
                
                if (actualCount <= 0)
                    return;
                
                _logger?.LogDebugInstance($"[{PoolName}] Prewarming pool with {actualCount} items");
                
                for (int i = 0; i < actualCount; i++)
                {
                    T item = CreateValidItem();
                    _inactiveItems.Push(item);
                }
                
                _metrics.RecordCreate(_poolId, actualCount);
            }
        }

        /// <summary>
        /// Attempts to auto-shrink the pool if conditions are met
        /// </summary>
        public void TryAutoShrink()
        {
            if (!_autoShrinkEnabled || IsDisposed)
                return;
            
            float timeSinceLastShrink = Time.realtimeSinceStartup - _lastShrinkTime;
            
            if (timeSinceLastShrink >= ShrinkInterval)
            {
                using (_profiler?.Sample("AdvancedObjectPool.TryAutoShrink"))
                {
                    TryShrink(ShrinkThreshold);
                }
            }
        }

        /// <summary>
        /// Disposes the pool and releases all resources
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
            
            using (_profiler?.Sample("AdvancedObjectPool.Dispose"))
            {
                DisposeInternal();
            }
        }

        #region Internal Implementation

        /// <summary>
        /// Initializes the pool
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized)
                return;
            
            using (_profiler?.Sample("AdvancedObjectPool.Initialize"))
            {
                _lastShrinkTime = Time.realtimeSinceStartup;
                _autoShrinkEnabled = true;
                
                if (_config.InitialCapacity > 0)
                {
                    int initialCapacity = math.min(_config.InitialCapacity, _config.MaximumCapacity);
                    PrewarmPool(initialCapacity);
                }
                
                _isInitialized = true;
                _logger?.LogDebugInstance($"[{PoolName}] Pool initialized with {TotalCount} items");
            }
        }

        /// <summary>
        /// Internal implementation of acquiring an item
        /// </summary>
        /// <returns>The acquired item</returns>
        private T AcquireInternal()
        {
            _diagnostics?.RecordAcquireStart(this);
            
            T item;
            
            // If we have an inactive item, use it
            if (_inactiveItems.Count > 0)
            {
                item = _inactiveItems.Pop();
            }
            else
            {
                // Otherwise, grow the pool
                GrowPool();
                
                // Try again (should succeed unless we hit our maximum capacity)
                if (_inactiveItems.Count > 0)
                {
                    item = _inactiveItems.Pop();
                }
                else
                {
                    _logger?.LogWarningInstance($"[{PoolName}] Failed to create new item - maximum capacity reached");
                    throw new InvalidOperationException($"Pool {PoolName} reached maximum capacity of {MaximumCapacity} items");
                }
            }
            
            _activeItems.Add(item);
            
            // If the item is IPoolable, notify it that it's been acquired
            if (item is IPoolable poolable)
            {
                try
                {
                    poolable.OnAcquire();
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"[{PoolName}] Error calling OnAcquire on poolable item: {ex.Message}");
                }
            }
            
            return item;
        }

        /// <summary>
        /// Internal implementation of releasing an item
        /// </summary>
        /// <param name="item">The item to release</param>
        private void ReleaseInternal(T item)
        {
            // If the item is not in the active set, ignore it
            if (!_activeItems.Remove(item))
            {
                _logger?.LogWarningInstance($"[{PoolName}] Attempted to release an item that is not active");
                return;
            }
            
            // If the item is IPoolable, notify it that it's been released
            if (item is IPoolable poolable)
            {
                try
                {
                    poolable.OnRelease();
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"[{PoolName}] Error calling OnRelease on poolable item: {ex.Message}");
                }
            }
            
            // Apply reset action if provided
            if (_resetAction != null)
            {
                try
                {
                    _resetAction(item);
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"[{PoolName}] Error applying reset action: {ex.Message}");
                }
            }
            
            // Add to inactive set
            _inactiveItems.Push(item);
        }

        /// <summary>
        /// Internal implementation of clearing the pool
        /// </summary>
        private void ClearInternal()
        {
            // Copy active items to a temporary list to avoid modifying during iteration
            var activeItems = new List<T>(_activeItems);
            
            // Release all active items
            foreach (var item in activeItems)
            {
                ReleaseInternal(item);
            }
            
            // Verify cleanup
            if (_activeItems.Count > 0)
            {
                _logger?.LogWarningInstance($"[{PoolName}] Failed to clear all active items");
            }
        }

        /// <summary>
        /// Grows the pool by creating new items
        /// </summary>
        private void GrowPool()
        {
            int currentCapacity = TotalCount;
            if (currentCapacity >= MaximumCapacity)
            {
                _logger?.LogWarningInstance($"[{PoolName}] Cannot grow pool beyond maximum capacity of {MaximumCapacity}");
                return;
            }
            
            int desiredCapacity = math.max(
                1,
                currentCapacity > 0 
                    ? math.min((int)(currentCapacity * GrowthFactor), MaximumCapacity)
                    : 1
            );
            
            int growAmount = desiredCapacity - currentCapacity;
            if (growAmount <= 0)
                return;
            
            _logger?.LogDebugInstance($"[{PoolName}] Growing pool by {growAmount} items");
            
            for (int i = 0; i < growAmount; i++)
            {
                T item = CreateValidItem();
                _inactiveItems.Push(item);
            }
            
            _metrics.RecordCreate(_poolId, growAmount);
        }

        /// <summary>
        /// Shrinks the pool by removing the specified number of inactive items
        /// </summary>
        /// <param name="excessItems">Number of items to remove</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        private bool ShrinkInternalByCount(int excessItems)
        {
            if (excessItems <= 0 || InactiveCount <= 0)
                return false;
            
            int shrinkCount = math.min(excessItems, InactiveCount);
            
            if (shrinkCount <= 0)
                return false;
            
            for (int i = 0; i < shrinkCount; i++)
            {
                if (_inactiveItems.Count > 0)
                {
                    T item = _inactiveItems.Pop();
                    DestroyItem(item);
                }
            }
            
            return true;
        }

        /// <summary>
        /// Internal implementation of disposing the pool
        /// </summary>
        private void DisposeInternal()
        {
            if (_isDisposed)
                return;
            
            _logger?.LogDebugInstance($"[{PoolName}] Disposing pool with {TotalCount} items");
            
            // Dispose all items
            while (_inactiveItems.Count > 0)
            {
                DestroyItem(_inactiveItems.Pop());
            }
            
            foreach (var item in _activeItems)
            {
                DestroyItem(item);
            }
            
            _activeItems.Clear();
            
            // Dispose metrics if IDisposable
            if (_metrics is IDisposable disposableMetrics)
            {
                disposableMetrics.Dispose();
            }
            
            // Unregister from diagnostics
            _diagnostics?.UnregisterPool(this);
            
            _isDisposed = true;
        }

        #endregion
    }
}