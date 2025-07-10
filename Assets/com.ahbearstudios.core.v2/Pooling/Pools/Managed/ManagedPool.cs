using System;
using System.Collections.Generic;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Diagnostics;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Pools.Managed
{
    /// <summary>
    /// A generic object pool that manages reusable instances of type T.
    /// Implements thread-local pooling with comprehensive diagnostics and metrics support.
    /// </summary>
    /// <typeparam name="T">Type of objects to pool</typeparam>
    public class ManagedPool<T> : IManagedPool<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDestroy;
        private readonly List<T> _inactive = new List<T>();
        private readonly HashSet<T> _active = new HashSet<T>();
        private readonly object _lock = new object();
        private readonly string _poolName;
        private readonly PoolLogger _logger;
        private readonly PoolProfiler _profiler;
        private readonly IPoolDiagnostics _diagnostics;
        private readonly PoolThreadingMode _threadingMode;
        
        private int _peakActiveCount;
        private int _totalCreatedCount;
        private int _totalAcquiredCount;
        private int _totalReleasedCount;
        private float _lastShrinkTime;
        private bool _isDisposed;

        
        public PoolThreadingMode ThreadingMode { get; }
        
        /// <summary>
        /// Gets the total number of items in the pool (active + inactive)
        /// </summary>
        public int TotalCount => ActiveCount + InactiveCount;

        /// <summary>
        /// Gets the number of active items
        /// </summary>
        public int ActiveCount => _active.Count;

        /// <summary>
        /// Gets the number of inactive items
        /// </summary>
        public int InactiveCount => _inactive.Count;

        /// <summary>
        /// Gets the peak number of simultaneously active items
        /// </summary>
        public int PeakUsage => _peakActiveCount;

        /// <summary>
        /// Gets the total number of items ever created by this pool
        /// </summary>
        public int TotalCreated => _totalCreatedCount;

        /// <summary>
        /// Gets whether this pool has been properly created and initialized
        /// </summary>
        public bool IsCreated => _factory != null && !_isDisposed;

        /// <summary>
        /// Gets whether this pool has been disposed
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Gets the type of items in the pool
        /// </summary>
        public Type ItemType => typeof(T);

        /// <summary>
        /// Gets the name of this pool
        /// </summary>
        public string PoolName => _poolName;

        /// <summary>
        /// Gets the peak number of active items since the pool was created or last reset
        /// </summary>
        public int PeakActiveCount => _peakActiveCount;

        /// <summary>
        /// Gets the total number of items created by this pool
        /// </summary>
        public int TotalCreatedCount => _totalCreatedCount;

        /// <summary>
        /// Gets the total number of acquire operations
        /// </summary>
        public int TotalAcquiredCount => _totalAcquiredCount;

        /// <summary>
        /// Gets the total number of release operations
        /// </summary>
        public int TotalReleasedCount => _totalReleasedCount;

        /// <summary>
        /// Gets the current number of active items
        /// </summary>
        public int CurrentActiveCount => _active.Count;

        /// <summary>
        /// Gets the current capacity of the pool
        /// </summary>
        public int CurrentCapacity => TotalCount;

        /// <summary>
        /// Gets or sets the minimum capacity that the pool will maintain even when shrinking
        /// </summary>
        public int MinimumCapacity { get; set; } = 0;

        /// <summary>
        /// Gets or sets the maximum capacity that the pool can grow to
        /// </summary>
        public int MaximumCapacity { get; set; } = 0;

        /// <summary>
        /// Gets or sets the interval in seconds between auto-shrink operations
        /// </summary>
        public float ShrinkInterval { get; set; } = 60f;

        /// <summary>
        /// Gets or sets the growth factor when the pool needs to expand
        /// </summary>
        public float GrowthFactor { get; set; } = 2.0f;

        /// <summary>
        /// Gets or sets the usage-to-capacity ratio below which shrinking will occur
        /// </summary>
        public float ShrinkThreshold { get; set; } = 0.25f;

        /// <summary>
        /// Gets whether this pool supports automatic shrinking
        /// </summary>
        public bool SupportsAutoShrink { get; private set; } = false;

        /// <summary>
        /// Creates a new pool with the specified factory function and optional configuration
        /// </summary>
        /// <param name="factory">Function that creates new instances</param>
        /// <param name="config">Optional pool configuration</param>
        /// <param name="onGet">Optional action to execute when an item is acquired</param>
        /// <param name="onRelease">Optional action to execute when an item is released</param>
        /// <param name="onDestroy">Optional action to execute when an item is destroyed</param>
        /// <param name="poolName">Optional name for the pool</param>
        public ManagedPool(
            Func<T> factory,
            ManagedObjectPoolConfig config = null,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            string poolName = null)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory), "Factory function cannot be null");

            _factory = factory;
            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            _poolName = poolName ?? $"Pool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            _threadingMode = config?.ThreadingMode ?? PoolThreadingMode.SingleThreaded;

            // Get services
            _logger = PoolingServices.GetService<PoolLogger>();
            _profiler = PoolingServices.GetService<PoolProfiler>();
            _diagnostics = PoolingServices.GetService<IPoolDiagnostics>();

            // Register with diagnostics
            _diagnostics?.RegisterPool(this, _poolName);

            // Apply configuration if provided
            if (config != null)
            {
                ApplyConfiguration(config);
            }

            _logger?.LogInfoInstance($"Created pool: {_poolName}");
        }

        /// <summary>
        /// Applies configuration settings to this pool
        /// </summary>
        /// <param name="config">Configuration to apply</param>
        private void ApplyConfiguration(IPoolConfig config)
        {
            // Apply configuration
            MinimumCapacity = config.InitialCapacity;
            MaximumCapacity = config.MaximumCapacity;
            SupportsAutoShrink = config.EnableAutoShrink;
            ShrinkThreshold = config.ShrinkThreshold;
            ShrinkInterval = config.ShrinkInterval;

            // Prewarm the pool if enabled
            if (config.PrewarmOnInit)
            {
                PrewarmPool(config.InitialCapacity);
            }
            
            _logger?.LogDebugInstance($"Applied configuration to pool {_poolName}: InitialCapacity={config.InitialCapacity}, MaximumCapacity={config.MaximumCapacity}, EnableAutoShrink={config.EnableAutoShrink}");
        }

        /// <summary>
        /// Prewarms the pool by creating a specified number of instances
        /// </summary>
        /// <param name="count">Number of instances to create</param>
        public void PrewarmPool(int count)
        {
            if (count <= 0 || _isDisposed)
                return;

            _profiler?.BeginSample("Prewarm", _poolName);
            
            try
            {
                lock (_lock)
                {
                    for (int i = 0; i < count; i++)
                    {
                        T item = CreateNew();
                        _inactive.Add(item);
                    }
                }
                
                _logger?.LogDebugInstance($"Prewarmed pool {_poolName} with {count} items");
            }
            finally
            {
                _profiler?.EndSample("Prewarm", _poolName, ActiveCount, InactiveCount);
            }
        }

        /// <summary>
        /// Creates a new instance using the factory method
        /// </summary>
        /// <returns>A new instance of T</returns>
        public T CreateNew()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            _profiler?.BeginSample("Create", _poolName);
            
            try
            {
                T item = _factory();
                
                if (item == null)
                    throw new InvalidOperationException("Factory function returned null");
                
                _totalCreatedCount++;
                
                // Record the creation
                _diagnostics?.RecordCreate(this);
                
                return item;
            }
            finally
            {
                _profiler?.EndSample("Create", _poolName, ActiveCount, InactiveCount);
            }
        }

        /// <summary>
        /// Acquires an item from the pool
        /// </summary>
        /// <returns>The acquired item</returns>
        public T Acquire()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            _profiler?.BeginSample("Acquire", _poolName);
            _diagnostics?.RecordAcquireStart(this);
    
            T item = default; // Initialize with default value
    
            try
            {
                lock (_lock)
                {
                    if (_inactive.Count > 0)
                    {
                        int lastIndex = _inactive.Count - 1;
                        item = _inactive[lastIndex];
                        _inactive.RemoveAt(lastIndex);
                    }
                    else
                    {
                        // If we've reached the maximum capacity and it's greater than 0, throw or return null
                        if (MaximumCapacity > 0 && TotalCount >= MaximumCapacity)
                        {
                            string errorMessage = $"Pool {_poolName} has reached its maximum capacity of {MaximumCapacity}";
                            _logger?.LogWarningInstance(errorMessage);
                            throw new InvalidOperationException(errorMessage);
                        }
                
                        item = CreateNew();
                    }
            
                    _active.Add(item);
                    _totalAcquiredCount++;
            
                    if (_active.Count > _peakActiveCount)
                    {
                        _peakActiveCount = _active.Count;
                    }
                }
        
                // Invoke the OnGet action outside the lock
                OnItemAcquired(item);
        
                return item;
            }
            finally
            {
                _profiler?.EndSample("Acquire", _poolName, ActiveCount, InactiveCount);
                _diagnostics?.RecordAcquireComplete(this, ActiveCount, item);
            }
        }

        /// <summary>
        /// Invoked when an item is acquired from the pool
        /// </summary>
        /// <param name="item">The acquired item</param>
        protected void OnItemAcquired(T item)
        {
            // Call the OnGet action if provided
            _onGet?.Invoke(item);
            
            // If the item implements IPoolable, call its OnAcquire method
            if (item is IPoolable poolable)
            {
                poolable.OnAcquire();
            }
        }

        /// <summary>
        /// Releases an item back to the pool
        /// </summary>
        /// <param name="item">Item to release</param>
        public void Release(T item)
        {
            if (item == null)
                return;
                
            if (_isDisposed)
            {
                // If the pool is disposed, just destroy the item
                DestroyItem(item);
                return;
            }

            _profiler?.BeginSample("Release", _poolName);
            
            try
            {
                bool wasActive;
                
                lock (_lock)
                {
                    wasActive = _active.Remove(item);
                    
                    if (wasActive)
                    {
                        _inactive.Add(item);
                        _totalReleasedCount++;
                    }
                    else
                    {
                        _logger?.LogWarningInstance($"Attempted to release an item to pool {_poolName} that was not acquired from it");
                    }
                }
                
                if (wasActive)
                {
                    // Invoke the OnRelease action outside the lock
                    OnItemReleased(item);
                    
                    // Check if we should auto-shrink
                    if (SupportsAutoShrink)
                    {
                        TryAutoShrink();
                    }
                }
                
                _diagnostics?.RecordRelease(this, ActiveCount, item);
            }
            finally
            {
                _profiler?.EndSample("Release", _poolName, ActiveCount, InactiveCount);
            }
        }

        /// <summary>
        /// Invoked when an item is released back to the pool
        /// </summary>
        /// <param name="item">The released item</param>
        protected void OnItemReleased(T item)
        {
            // Call the OnRelease action if provided
            _onRelease?.Invoke(item);
            
            // If the item implements IPoolable, call its OnRelease method
            if (item is IPoolable poolable)
            {
                poolable.OnRelease();
                poolable.Reset();
            }
        }

        /// <summary>
        /// Destroys an item, removing it from the pool permanently
        /// </summary>
        /// <param name="item">Item to destroy</param>
        public void DestroyItem(T item)
        {
            if (item == null)
                return;

            _profiler?.BeginSample("Destroy", _poolName);
            
            try
            {
                lock (_lock)
                {
                    _active.Remove(item);
                    _inactive.Remove(item);
                }
                
                // Call the OnDestroy action if provided
                _onDestroy?.Invoke(item);
                
                // If the item implements IPoolable, call its OnDestroy method
                if (item is IPoolable poolable)
                {
                    poolable.OnDestroy();
                }
                
                // If the item is a UnityEngine.Object, destroy it properly
                if (item is UnityEngine.Object unityObject)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(unityObject);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(unityObject);
                    }
                }
            }
            finally
            {
                _profiler?.EndSample("Destroy", _poolName, ActiveCount, InactiveCount);
            }
        }

        /// <summary>
        /// Clears the pool, returning all active items to the inactive state
        /// </summary>
        public void Clear()
        {
            if (_isDisposed)
                return;

            _profiler?.BeginSample("Clear", _poolName);
            
            try
            {
                lock (_lock)
                {
                    // Create a copy of the active items to avoid modification during enumeration
                    var itemsToRelease = new List<T>(_active);
                    
                    foreach (var item in itemsToRelease)
                    {
                        Release(item);
                    }
                    
                    _logger?.LogDebugInstance($"Cleared pool {_poolName}");
                }
            }
            finally
            {
                _profiler?.EndSample("Clear", _poolName, ActiveCount, InactiveCount);
            }
        }

        /// <summary>
        /// Ensures the pool has at least the specified capacity
        /// </summary>
        /// <param name="capacity">Required capacity</param>
        public void EnsureCapacity(int capacity)
        {
            if (_isDisposed || capacity <= TotalCount)
                return;

            _profiler?.BeginSample("EnsureCapacity", _poolName);
            
            try
            {
                lock (_lock)
                {
                    int itemsToCreate = capacity - TotalCount;
                    
                    for (int i = 0; i < itemsToCreate; i++)
                    {
                        T item = CreateNew();
                        _inactive.Add(item);
                    }
                    
                    _logger?.LogDebugInstance($"Expanded pool {_poolName} capacity to {capacity}");
                }
            }
            finally
            {
                _profiler?.EndSample("EnsureCapacity", _poolName, ActiveCount, InactiveCount);
            }
        }

        /// <summary>
        /// Gets a dictionary of metrics about the pool
        /// </summary>
        /// <returns>Dictionary mapping metric names to values</returns>
        public Dictionary<string, object> GetMetrics()
        {
            var metrics = new Dictionary<string, object>
            {
                { "PoolName", _poolName },
                { "ItemType", typeof(T).Name },
                { "TotalCount", TotalCount },
                { "ActiveCount", ActiveCount },
                { "InactiveCount", InactiveCount },
                { "PeakActiveCount", _peakActiveCount },
                { "TotalCreatedCount", _totalCreatedCount },
                { "TotalAcquiredCount", _totalAcquiredCount },
                { "TotalReleasedCount", _totalReleasedCount },
                { "ActiveCount", _active.Count },
                { "CurrentCapacity", TotalCount },
                { "MinimumCapacity", MinimumCapacity },
                { "MaximumCapacity", MaximumCapacity },
                { "SupportsAutoShrink", SupportsAutoShrink },
                { "ShrinkThreshold", ShrinkThreshold },
                { "GrowthFactor", GrowthFactor },
                { "IsDisposed", _isDisposed }
            };
            
            return metrics;
        }

        /// <summary>
        /// Resets the performance metrics of the pool
        /// </summary>
        public void ResetMetrics()
        {
            lock (_lock)
            {
                _peakActiveCount = _active.Count;
                _totalAcquiredCount = 0;
                _totalReleasedCount = 0;
            }
        }

        /// <summary>
        /// Attempts to shrink the pool's capacity to reduce memory usage
        /// </summary>
        /// <param name="threshold">Threshold factor (0-1) determining when shrinking occurs</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool TryShrink(float threshold)
        {
            if (_isDisposed || threshold <= 0f || threshold >= 1f)
                return false;

            _profiler?.BeginSample("TryShrink", _poolName);
            
            try
            {
                lock (_lock)
                {
                    int activeCount = _active.Count;
                    int totalCount = TotalCount;
                    
                    // If usage ratio is below threshold and we have more than the minimum capacity,
                    // shrink the pool
                    if (totalCount > MinimumCapacity && activeCount < totalCount * threshold)
                    {
                        // Calculate the new target capacity, but not lower than our minimum
                        int targetCapacity = Math.Max(MinimumCapacity, 
                                             Math.Max(activeCount, (int)(totalCount * threshold)));
                        
                        return ShrinkTo(targetCapacity);
                    }
                }
                
                return false;
            }
            finally
            {
                _profiler?.EndSample("TryShrink", _poolName, ActiveCount, InactiveCount);
            }
        }

        /// <summary>
        /// Explicitly shrinks the pool to the specified capacity
        /// </summary>
        /// <param name="targetCapacity">The target capacity to shrink to</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool ShrinkTo(int targetCapacity)
        {
            if (_isDisposed)
                return false;
                
            // Ensure target capacity isn't below minimum
            targetCapacity = Math.Max(targetCapacity, MinimumCapacity);
            
            // If current capacity is already at or below target, nothing to do
            if (TotalCount <= targetCapacity)
                return false;

            _profiler?.BeginSample("ShrinkTo", _poolName);
            
            try
            {
                lock (_lock)
                {
                    // Calculate how many items to remove
                    int excessItems = TotalCount - targetCapacity;
                    
                    // Limited by the number of inactive items (we can't remove active ones)
                    int itemsToRemove = Math.Min(excessItems, _inactive.Count);
                    
                    if (itemsToRemove <= 0)
                        return false;
                    
                    // Remove and destroy the oldest inactive items (at the beginning of the list)
                    for (int i = 0; i < itemsToRemove; i++)
                    {
                        T item = _inactive[0];
                        _inactive.RemoveAt(0);
                        DestroyItem(item);
                    }
                    
                    _logger?.LogDebugInstance($"Shrunk pool {_poolName} by removing {itemsToRemove} items. New capacity: {TotalCount}");
                    
                    return true;
                }
            }
            finally
            {
                _profiler?.EndSample("ShrinkTo", _poolName, ActiveCount, InactiveCount);
            }
        }

        /// <summary>
        /// Explicitly enables or disables automatic shrinking
        /// </summary>
        /// <param name="enabled">Whether automatic shrinking should be enabled</param>
        public void SetAutoShrink(bool enabled)
        {
            SupportsAutoShrink = enabled;
        }

        /// <summary>
        /// Tries to automatically shrink the pool based on elapsed time and thresholds
        /// </summary>
        public void TryAutoShrink()
        {
            if (!SupportsAutoShrink || _isDisposed)
                return;
                
            float currentTime = Time.realtimeSinceStartup;
            
            // Only shrink if enough time has passed since the last shrink
            if (currentTime - _lastShrinkTime >= ShrinkInterval)
            {
                if (TryShrink(ShrinkThreshold))
                {
                    _lastShrinkTime = currentTime;
                }
            }
        }

        /// <summary>
        /// Disposes the pool, releasing all resources
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _profiler?.BeginSample("Dispose", _poolName);
            
            try
            {
                lock (_lock)
                {
                    // Destroy all active items
                    foreach (var item in _active)
                    {
                        DestroyItem(item);
                    }
                    
                    // Destroy all inactive items
                    foreach (var item in _inactive)
                    {
                        DestroyItem(item);
                    }
                    
                    _active.Clear();
                    _inactive.Clear();
                    _isDisposed = true;
                    
                    // Unregister from diagnostics
                    _diagnostics?.UnregisterPool(this);
                    
                    _logger?.LogInfoInstance($"Disposed pool: {_poolName}");
                }
            }
            finally
            {
                _profiler?.EndSample("Dispose", _poolName, 0, 0);
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
                
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            var result = new List<T>(count);
            
            _profiler?.BeginSample("AcquireMultiple", _poolName);
            
            try
            {
                for (int i = 0; i < count; i++)
                {
                    result.Add(Acquire());
                }
                
                return result;
            }
            finally
            {
                _profiler?.EndSample("AcquireMultiple", _poolName, ActiveCount, InactiveCount);
            }
        }

        /// <summary>
        /// Releases multiple items back to the pool at once
        /// </summary>
        /// <param name="items">Items to release</param>
        public void ReleaseMultiple(IEnumerable<T> items)
        {
            if (items == null)
                return;
                
            if (_isDisposed)
                return;

            _profiler?.BeginSample("ReleaseMultiple", _poolName);
            
            try
            {
                foreach (var item in items)
                {
                    Release(item);
                }
            }
            finally
            {
                _profiler?.EndSample("ReleaseMultiple", _poolName, ActiveCount, InactiveCount);
            }
        }

        /// <summary>
        /// Acquires an item and initializes it with a setup action
        /// </summary>
        /// <param name="setupAction">Action to initialize the item</param>
        /// <returns>The acquired and initialized item</returns>
        public T AcquireAndSetup(Action<T> setupAction)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            _profiler?.BeginSample("AcquireAndSetup", _poolName);
            
            try
            {
                T item = Acquire();
                
                setupAction?.Invoke(item);
                
                return item;
            }
            finally
            {
                _profiler?.EndSample("AcquireAndSetup", _poolName, ActiveCount, InactiveCount);
            }
        }
    }
}