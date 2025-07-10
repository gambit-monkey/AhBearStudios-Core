using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Diagnostics;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Pools.Managed
{
    /// <summary>
    /// Thread-local pool implementation that maintains separate pools for each thread.
    /// Provides excellent performance by eliminating contention in highly parallel operations.
    /// </summary>
    /// <typeparam name="T">Type of items in the pool</typeparam>
    public class ThreadLocalPool<T> : IThreadLocalPool<T> where T : class
    {
        private readonly ConcurrentDictionary<int, Stack<T>> _threadLocalStacks = new ConcurrentDictionary<int, Stack<T>>();
        private readonly Func<T> _factory;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDestroy;
        private readonly string _poolName;
        private readonly ConcurrentDictionary<int, HashSet<T>> _activeItems = new ConcurrentDictionary<int, HashSet<T>>();
        
        private readonly object _metricsLock = new object();
        private readonly object _configLock = new object();
        private int _totalCreated;
        private int _peakUsage;
        private int _totalAcquired;
        private int _totalReleased;
        private int _peakActiveCount;
        private bool _isCreated;
        private bool _isDisposed;
        private float _lastShrinkTime;
        private readonly PoolProfiler _profiler;
        private readonly PoolLogger _logger;
        private readonly IPoolDiagnostics _diagnostics;

        // Pool configuration
        private int _minimumCapacity = 0;
        private int _maximumCapacity = 0;
        private float _shrinkInterval = 60.0f;
        private float _growthFactor = 2.0f;
        private float _shrinkThreshold = 0.25f;
        private bool _supportsAutoShrink = false;

        /// <summary>
        /// Gets the threading mode for this pool. Always returns ThreadLocal.
        /// </summary>
        public PoolThreadingMode ThreadingMode => PoolThreadingMode.ThreadLocal;

        /// <summary>
        /// Gets the total number of items in the pool (active + inactive)
        /// </summary>
        public int TotalCount
        {
            get
            {
                int active = ActiveCount;
                int inactive = InactiveCount;
                return active + inactive;
            }
        }

        /// <summary>
        /// Gets the number of active items
        /// </summary>
        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (var activeSet in _activeItems.Values)
                {
                    count += activeSet.Count;
                }
                return count;
            }
        }

        /// <summary>
        /// Gets the number of inactive items
        /// </summary>
        public int InactiveCount
        {
            get
            {
                int count = 0;
                foreach (var stack in _threadLocalStacks.Values)
                {
                    count += stack.Count;
                }
                return count;
            }
        }

        /// <summary>
        /// Gets the type of items in the pool
        /// </summary>
        public Type ItemType => typeof(T);

        /// <summary>
        /// Gets the name of this pool
        /// </summary>
        public string PoolName => _poolName;

        /// <summary>
        /// Gets whether this pool has been properly created and initialized
        /// </summary>
        public bool IsCreated => _isCreated;

        /// <summary>
        /// Gets whether this pool has been disposed
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Gets the peak number of simultaneously active items
        /// </summary>
        public int PeakUsage => _peakUsage;

        /// <summary>
        /// Gets the total number of items ever created by this pool
        /// </summary>
        public int TotalCreated => _totalCreated;

        /// <summary>
        /// Gets the peak number of active items since the pool was created or last reset
        /// </summary>
        public int PeakActiveCount => _peakActiveCount;

        /// <summary>
        /// Gets the total number of items created by this pool
        /// </summary>
        public int TotalCreatedCount => _totalCreated;

        /// <summary>
        /// Gets the total number of acquire operations
        /// </summary>
        public int TotalAcquiredCount => _totalAcquired;

        /// <summary>
        /// Gets the total number of release operations
        /// </summary>
        public int TotalReleasedCount => _totalReleased;

        /// <summary>
        /// Gets the current number of active items
        /// </summary>
        public int CurrentActiveCount => ActiveCount;

        /// <summary>
        /// Gets the current capacity of the pool
        /// </summary>
        public int CurrentCapacity => TotalCount;

        /// <summary>
        /// Gets whether this pool supports automatic shrinking
        /// </summary>
        public bool SupportsAutoShrink => _supportsAutoShrink;

        /// <summary>
        /// Gets or sets the minimum capacity that the pool will maintain even when shrinking
        /// </summary>
        public int MinimumCapacity
        {
            get { lock (_configLock) { return _minimumCapacity; } }
            set { lock (_configLock) { _minimumCapacity = Math.Max(0, value); } }
        }

        /// <summary>
        /// Gets or sets the maximum capacity that the pool can grow to
        /// </summary>
        public int MaximumCapacity
        {
            get { lock (_configLock) { return _maximumCapacity; } }
            set { lock (_configLock) { _maximumCapacity = Math.Max(0, value); } }
        }

        /// <summary>
        /// Gets or sets the shrink interval in seconds.
        /// </summary>
        public float ShrinkInterval
        {
            get { lock (_configLock) { return _shrinkInterval; } }
            set { lock (_configLock) { _shrinkInterval = Math.Max(0, value); } }
        }

        /// <summary>
        /// Gets or sets the growth factor when the pool needs to expand.
        /// </summary>
        public float GrowthFactor
        {
            get { lock (_configLock) { return _growthFactor; } }
            set { lock (_configLock) { _growthFactor = Math.Max(1.1f, value); } }
        }

        /// <summary>
        /// Gets or sets the shrink threshold.
        /// </summary>
        public float ShrinkThreshold
        {
            get { lock (_configLock) { return _shrinkThreshold; } }
            set { lock (_configLock) { _shrinkThreshold = Mathf.Clamp01(value); } }
        }

        /// <summary>
        /// Creates a new ThreadLocalPool.
        /// </summary>
        /// <param name="factory">Function that creates new items</param>
        /// <param name="config">Optional configuration</param>
        /// <param name="onGet">Optional action to perform when an item is acquired</param>
        /// <param name="onRelease">Optional action to perform when an item is released</param>
        /// <param name="onDestroy">Optional action to perform when an item is destroyed</param>
        /// <param name="poolName">Optional name for the pool</param>
        public ThreadLocalPool(
            Func<T> factory,
            ThreadLocalPoolConfig config = null,
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
            _poolName = poolName ?? $"ThreadLocalPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            _isCreated = true;

            // Try to get services from service locator
            if (PoolingServices.HasService<PoolProfiler>())
            {
                _profiler = PoolingServices.GetService<PoolProfiler>();
            }

            if (PoolingServices.HasService<PoolLogger>())
            {
                _logger = PoolingServices.GetService<PoolLogger>();
            }

            if (PoolingServices.HasService<IPoolDiagnostics>())
            {
                _diagnostics = PoolingServices.GetService<IPoolDiagnostics>();
                _diagnostics?.RegisterPool(this, _poolName);
            }

            // Apply configuration if provided
            if (config != null)
            {
                ApplyConfiguration(config);
            }

            // Initialize the pool for the current thread
            GetThreadLocalStack();
            GetActiveItemsSet();

            // Initialize timing
            _lastShrinkTime = Time.realtimeSinceStartup;

            _logger?.LogInfoInstance($"Created ThreadLocalPool<{typeof(T).Name}> '{_poolName}'");
        }

        /// <summary>
        /// Applies configuration settings to this pool
        /// </summary>
        /// <param name="config">Configuration to apply</param>
        public void ApplyConfiguration(IPoolConfig config)
        {
            if (config == null)
                return;

            lock (_configLock)
            {
                _minimumCapacity = config.InitialCapacity;
                _maximumCapacity = config.MaximumCapacity;
                _supportsAutoShrink = config.EnableAutoShrink;
                _shrinkThreshold = config.ShrinkThreshold;
                _shrinkInterval = config.ShrinkInterval;
                _growthFactor = config.UseExponentialGrowth ? config.GrowthFactor : 1.0f;
            }

            // Prewarm the pool if configured
            if (config.PrewarmOnInit)
            {
                PrewarmPool(config.InitialCapacity);
            }

            _logger?.LogDebugInstance($"Applied configuration to pool '{_poolName}'");
        }

        /// <summary>
        /// Prewarms the pool by creating the specified number of items
        /// </summary>
        /// <param name="count">Number of items to prewarm</param>
        public void PrewarmPool(int count)
        {
            if (count <= 0)
                return;

            _profiler?.BeginSample("Prewarm", _poolName);

            var stack = GetThreadLocalStack();
            for (int i = 0; i < count; i++)
            {
                T item = CreateNew();
                stack.Push(item);
            }

            _profiler?.EndSample("Prewarm", _poolName, ActiveCount, stack.Count);
            _logger?.LogDebugInstance($"Prewarmed pool '{_poolName}' with {count} items");
        }

        /// <summary>
        /// Creates a new item
        /// </summary>
        /// <returns>The created item</returns>
        public T CreateNew()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            if (!_isCreated)
                throw new InvalidOperationException("Pool has not been properly created");

            _profiler?.BeginSample("Create", _poolName);

            T item;
            try
            {
                item = _factory();
                Interlocked.Increment(ref _totalCreated);
                _diagnostics?.RecordCreate(this);
            }
            finally
            {
                _profiler?.EndSample("Create", _poolName, ActiveCount, InactiveCount);
            }

            return item;
        }

        /// <summary>
        /// Acquires an item from the pool
        /// </summary>
        /// <returns>The acquired item</returns>
        public T Acquire()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            if (!_isCreated)
                throw new InvalidOperationException("Pool has not been properly created");

            _profiler?.BeginSample("Acquire", _poolName);
            _diagnostics?.RecordAcquireStart(this);

            var stack = GetThreadLocalStack();
            T item;

            if (stack.Count > 0)
            {
                // Get item from pool
                item = stack.Pop();
            }
            else
            {
                // Create new item
                item = CreateNew();
            }

            // Add to active items
            var activeItems = GetActiveItemsSet();
            activeItems.Add(item);

            // Update metrics
            Interlocked.Increment(ref _totalAcquired);
            UpdatePeakActiveCount(ActiveCount);

            // Invoke on-get callback
            try
            {
                OnItemAcquired(item);
            }
            catch (Exception ex)
            {
                _logger?.LogErrorInstance($"Error in OnItemAcquired callback for pool '{_poolName}': {ex.Message}");
            }

            _diagnostics?.RecordAcquireComplete(this, ActiveCount, item);
            _profiler?.EndSample("Acquire", _poolName, ActiveCount, InactiveCount);

            return item;
        }

        /// <summary>
        /// Updates the peak active count metric
        /// </summary>
        /// <param name="currentCount">Current active count</param>
        public void UpdatePeakActiveCount(int currentCount)
        {
            lock (_metricsLock)
            {
                _peakUsage = Math.Max(_peakUsage, currentCount);
                _peakActiveCount = Math.Max(_peakActiveCount, currentCount);
            }
        }

        /// <summary>
        /// Called when an item is acquired from the pool
        /// </summary>
        /// <param name="item">The acquired item</param>
        protected void OnItemAcquired(T item)
        {
            // Invoke callback if provided
            _onGet?.Invoke(item);

            // If item implements IPoolable interface, call OnAcquire
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
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            if (item == null)
                throw new ArgumentNullException(nameof(item), "Cannot release null item to pool");

            _profiler?.BeginSample("Release", _poolName);

            // Find which thread owns this item
            bool released = false;
            foreach (var kvp in _activeItems)
            {
                var activeSet = kvp.Value;
                if (activeSet.Contains(item))
                {
                    // Remove from active items
                    activeSet.Remove(item);
                    released = true;

                    // Release back to the same thread's stack
                    var threadId = kvp.Key;
                    if (_threadLocalStacks.TryGetValue(threadId, out var stack))
                    {
                        // Process the item before returning to pool
                        try
                        {
                            OnItemReleased(item);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogErrorInstance($"Error in OnItemReleased callback for pool '{_poolName}': {ex.Message}");
                        }

                        // Return to pool (on the correct thread's stack)
                        stack.Push(item);
                    }
                    else
                    {
                        // If thread's stack no longer exists, add to current thread's stack
                        OnItemReleased(item);
                        GetThreadLocalStack().Push(item);
                    }
                    break;
                }
            }

            // If item wasn't found in any thread's active set, add it to current thread's stack
            if (!released)
            {
                _logger?.LogWarningInstance($"Released item was not tracked as active in pool '{_poolName}'");
                try
                {
                    OnItemReleased(item);
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"Error in OnItemReleased callback for pool '{_poolName}': {ex.Message}");
                }
                GetThreadLocalStack().Push(item);
            }

            // Update metrics
            Interlocked.Increment(ref _totalReleased);
            _diagnostics?.RecordRelease(this, ActiveCount, item);

            _profiler?.EndSample("Release", _poolName, ActiveCount, InactiveCount);

            // Try auto-shrink if enabled
            if (_supportsAutoShrink)
            {
                TryAutoShrink();
            }
        }

        /// <summary>
        /// Called when an item is released back to the pool
        /// </summary>
        /// <param name="item">The released item</param>
        protected void OnItemReleased(T item)
        {
            // Invoke callback if provided
            _onRelease?.Invoke(item);

            // If item implements IPoolable interface, call OnRelease
            if (item is IPoolable poolable)
            {
                poolable.OnRelease();
                poolable.Reset();
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

            // Remove from active items if present
            foreach (var activeSet in _activeItems.Values)
            {
                activeSet.Remove(item);
            }

            try
            {
                // Invoke destroy callback if provided
                _onDestroy?.Invoke(item);

                // If item implements IPoolable interface, call OnDestroy
                if (item is IPoolable poolable)
                {
                    poolable.OnDestroy();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogErrorInstance($"Error destroying item in pool '{_poolName}': {ex.Message}");
            }

            // If item implements IDisposable, dispose it
            if (item is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"Error disposing item in pool '{_poolName}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clears the pool, returning all active items to the inactive state
        /// </summary>
        public void Clear()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            _profiler?.BeginSample("Clear", _poolName);

            // Collect all active items for release
            List<T> itemsToRelease = new List<T>();
            foreach (var activeSet in _activeItems.Values)
            {
                itemsToRelease.AddRange(activeSet);
            }

            // Release all active items
            foreach (var item in itemsToRelease)
            {
                Release(item);
            }

            _profiler?.EndSample("Clear", _poolName, ActiveCount, InactiveCount);
            _logger?.LogDebugInstance($"Cleared pool '{_poolName}'");
        }

        /// <summary>
        /// Ensures the pool has at least the specified capacity
        /// </summary>
        /// <param name="capacity">Required capacity</param>
        public void EnsureCapacity(int capacity)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            if (capacity <= 0)
                return;

            int currentCapacity = TotalCount;
            if (currentCapacity >= capacity)
                return;

            // Only prewarm on current thread
            int toAdd = capacity - currentCapacity;
            PrewarmPool(toAdd);
        }

        /// <summary>
        /// Gets metrics for this pool
        /// </summary>
        /// <returns>Dictionary of pool metrics</returns>
        public Dictionary<string, object> GetMetrics()
        {
            var metrics = new Dictionary<string, object>
            {
                ["PoolName"] = _poolName,
                ["ItemType"] = typeof(T).Name,
                ["TotalCount"] = TotalCount,
                ["ActiveCount"] = ActiveCount,
                ["InactiveCount"] = InactiveCount,
                ["PeakActiveCount"] = _peakActiveCount,
                ["TotalCreated"] = _totalCreated,
                ["TotalAcquired"] = _totalAcquired,
                ["TotalReleased"] = _totalReleased,
                ["IsDisposed"] = _isDisposed,
                ["ThreadingMode"] = ThreadingMode.ToString(),
                ["ThreadCount"] = _threadLocalStacks.Count,
                ["CurrentCapacity"] = CurrentCapacity,
                ["SupportsAutoShrink"] = _supportsAutoShrink,
                ["MinimumCapacity"] = _minimumCapacity,
                ["MaximumCapacity"] = _maximumCapacity,
                ["ShrinkThreshold"] = _shrinkThreshold,
                ["ShrinkInterval"] = _shrinkInterval,
                ["GrowthFactor"] = _growthFactor,
                ["SampleDuration"] = Time.realtimeSinceStartup - _lastShrinkTime
            };

            return metrics;
        }

        /// <summary>
        /// Resets the performance metrics of the pool
        /// </summary>
        public void ResetMetrics()
        {
            lock (_metricsLock)
            {
                _peakUsage = ActiveCount;
                _peakActiveCount = ActiveCount;
                Interlocked.Exchange(ref _totalAcquired, 0);
                Interlocked.Exchange(ref _totalReleased, 0);
            }
            _logger?.LogDebugInstance($"Reset metrics for pool '{_poolName}'");
        }

        /// <summary>
        /// Attempts to shrink the pool's capacity to reduce memory usage
        /// </summary>
        /// <param name="threshold">Threshold factor determining when shrinking occurs</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool TryShrink(float threshold)
        {
            if (_isDisposed || !_isCreated)
                return false;

            int activeCount = ActiveCount;
            int totalCount = TotalCount;

            // Don't shrink if we don't have enough items
            if (totalCount <= _minimumCapacity)
                return false;

            // Only shrink if usage is below threshold
            float usage = totalCount > 0 ? (float)activeCount / totalCount : 0f;
            if (usage >= threshold)
                return false;

            // Calculate target capacity - keep at least minimum capacity
            int targetCapacity = Math.Max(
                _minimumCapacity,
                (int)Math.Ceiling(activeCount / threshold)
            );

            return ShrinkTo(targetCapacity);
        }

        /// <summary>
        /// Explicitly shrinks the pool to the specified capacity
        /// </summary>
        /// <param name="targetCapacity">The target capacity to shrink to</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool ShrinkTo(int targetCapacity)
        {
            if (_isDisposed || !_isCreated)
                return false;

            _profiler?.BeginSample("Shrink", _poolName);

            bool anyShrunk = false;

            // Shrink each thread's pool
            foreach (var threadId in _threadLocalStacks.Keys)
            {
                if (ShrinkCurrentThreadPoolTo(targetCapacity))
                {
                    anyShrunk = true;
                }
            }

            _profiler?.EndSample("Shrink", _poolName, ActiveCount, InactiveCount);

            if (anyShrunk)
            {
                _logger?.LogDebugInstance($"Shrunk pool '{_poolName}' to target capacity {targetCapacity}");
            }

            return anyShrunk;
        }

        /// <summary>
        /// Shrinks the current thread's pool to the specified target capacity
        /// </summary>
        /// <param name="targetCapacity">Target capacity</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool ShrinkCurrentThreadPoolTo(int targetCapacity)
        {
            if (_isDisposed || !_isCreated)
                return false;

            if (targetCapacity < 0)
                targetCapacity = 0;

            // Ensure we respect minimum capacity setting
            targetCapacity = Math.Max(targetCapacity, _minimumCapacity);

            // Get current thread's stack
            var stack = GetThreadLocalStack();
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;

            // Calculate how many items to destroy from this thread's stack
            int activeCount = _activeItems.TryGetValue(currentThreadId, out var activeItems)
                ? activeItems.Count
                : 0;

            int totalCount = activeCount + stack.Count;
            int toDestroy = Math.Max(0, totalCount - targetCapacity);

            if (toDestroy <= 0)
                return false;

            // Limit to the number of items in the stack (inactive items)
            toDestroy = Math.Min(toDestroy, stack.Count);

            // Remove and destroy items
            for (int i = 0; i < toDestroy; i++)
            {
                if (stack.Count == 0)
                    break;

                var item = stack.Pop();
                DestroyItem(item);
            }

            return toDestroy > 0;
        }

        /// <summary>
        /// Explicitly enables or disables automatic shrinking
        /// </summary>
        /// <param name="enabled">Whether automatic shrinking should be enabled</param>
        public void SetAutoShrink(bool enabled)
        {
            lock (_configLock)
            {
                _supportsAutoShrink = enabled;
                if (enabled)
                {
                    _lastShrinkTime = Time.realtimeSinceStartup;
                }
            }

            _logger?.LogDebugInstance($"{(enabled ? "Enabled" : "Disabled")} auto-shrink for pool '{_poolName}'");
        }

        /// <summary>
        /// Checks if auto-shrink should be performed and does it if needed
        /// </summary>
        public void TryAutoShrink()
        {
            if (!_supportsAutoShrink || _isDisposed || !_isCreated)
                return;

            float timeSinceLastShrink = Time.realtimeSinceStartup - _lastShrinkTime;
            if (timeSinceLastShrink < _shrinkInterval)
                return;

            if (TryShrink(_shrinkThreshold))
            {
                _lastShrinkTime = Time.realtimeSinceStartup;
            }
        }

        /// <summary>
        /// Disposes the pool and all contained resources
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            _profiler?.BeginSample("Dispose", _poolName);

            // Destroy all items in the pool
            foreach (var stack in _threadLocalStacks.Values)
            {
                while (stack.Count > 0)
                {
                    var item = stack.Pop();
                    DestroyItem(item);
                }
            }

            // Destroy all active items
            foreach (var activeSet in _activeItems.Values)
            {
                var activeItems = new List<T>(activeSet);
                foreach (var item in activeItems)
                {
                    DestroyItem(item);
                }
                activeSet.Clear();
            }

            // Clear collections
            _threadLocalStacks.Clear();
            _activeItems.Clear();

            // Unregister from diagnostics
            _diagnostics?.UnregisterPool(this);

            _profiler?.EndSample("Dispose", _poolName, 0, 0);
            _logger?.LogInfoInstance($"Disposed ThreadLocalPool '{_poolName}'");
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

            var result = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                result.Add(Acquire());
            }
            return result;
        }

        /// <summary>
        /// Releases multiple items back to the pool at once
        /// </summary>
        /// <param name="items">Items to release</param>
        public void ReleaseMultiple(IEnumerable<T> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
            {
                if (item != null)
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
            T item = Acquire();
            
            if (setupAction != null)
            {
                try
                {
                    setupAction(item);
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"Error in setup action for pool '{_poolName}': {ex.Message}");
                }
            }

            return item;
        }

        // Helper methods
        private Stack<T> GetThreadLocalStack()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            return _threadLocalStacks.GetOrAdd(threadId, _ => new Stack<T>());
        }

        private HashSet<T> GetActiveItemsSet()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            return _activeItems.GetOrAdd(threadId, _ => new HashSet<T>());
        }
    }
}