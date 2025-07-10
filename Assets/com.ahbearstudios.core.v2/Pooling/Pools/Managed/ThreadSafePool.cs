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
    /// Thread-safe implementation of an object pool that can be safely accessed from multiple threads.
    /// Uses locking mechanisms to ensure operations are atomic, and provides robust diagnostics and metrics.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool</typeparam>
    public class ThreadSafePool<T> : IThreadSafePool<T> where T : class
    {
        // Core pool data structures
        private readonly ConcurrentBag<T> _inactive;
        private readonly HashSet<T> _active;
        private readonly object _lock = new object();
        
        // Factory function to create new instances
        private readonly Func<T> _factory;
        
        // Optional action to reset objects when returned to the pool
        private readonly Action<T> _resetAction;
        
        // Configuration
        private readonly ThreadSafePoolConfig _config;
        
        // Metrics tracking
        private int _totalCreated;
        private int _peakUsage;
        private int _totalAcquired;
        private int _totalReleased;
        private float _lastShrinkTime;
        
        // Object lifetime tracking (for metrics)
        private readonly Dictionary<T, float> _acquisitionTimes;
        
        // Dependencies
        private readonly PoolLogger _logger;
        private readonly PoolProfiler _profiler;
        private readonly IPoolDiagnostics _diagnostics;
        
        // Shrinking configuration
        private int _minimumCapacity;
        private int _maximumCapacity;
        private float _shrinkInterval = 60f;
        private float _growthFactor = 2.0f;
        private float _shrinkThreshold = 0.25f;
        private bool _autoShrinkEnabled;
        
        #region Properties

        /// <inheritdoc />
        public bool IsCreated { get; private set; }

        /// <inheritdoc />
        public int TotalCount => ActiveCount + InactiveCount;

        /// <inheritdoc />
        public int ActiveCount
        {
            get
            {
                lock (_lock)
                {
                    return _active.Count;
                }
            }
        }

        /// <inheritdoc />
        public int InactiveCount => _inactive.Count;

        /// <inheritdoc />
        public int PeakUsage => _peakUsage;

        /// <inheritdoc />
        public int TotalCreated => _totalCreated;

        /// <inheritdoc />
        public Type ItemType => typeof(T);

        /// <inheritdoc />
        public bool IsDisposed { get; private set; }

        /// <inheritdoc />
        public string PoolName { get; }

        /// <inheritdoc />
        public int PeakActiveCount => _peakUsage;

        /// <inheritdoc />
        public int TotalCreatedCount => _totalCreated;

        /// <inheritdoc />
        public int TotalAcquiredCount => _totalAcquired;

        /// <inheritdoc />
        public int TotalReleasedCount => _totalReleased;

        /// <inheritdoc />
        public int CurrentActiveCount => ActiveCount;

        /// <inheritdoc />
        public int CurrentCapacity => TotalCount;

        /// <inheritdoc />
        public PoolThreadingMode ThreadingMode => PoolThreadingMode.ThreadSafe;

        /// <inheritdoc />
        public bool SupportsAutoShrink => true;

        /// <inheritdoc />
        public int MinimumCapacity
        {
            get => _minimumCapacity;
            set => _minimumCapacity = Mathf.Max(0, value);
        }

        /// <inheritdoc />
        public int MaximumCapacity
        {
            get => _maximumCapacity;
            set => _maximumCapacity = value <= 0 ? int.MaxValue : value;
        }

        /// <inheritdoc />
        public float ShrinkInterval
        {
            get => _shrinkInterval;
            set => _shrinkInterval = Mathf.Max(1f, value);
        }

        /// <inheritdoc />
        public float GrowthFactor
        {
            get => _growthFactor;
            set => _growthFactor = Mathf.Max(1.1f, value);
        }

        /// <inheritdoc />
        public float ShrinkThreshold
        {
            get => _shrinkThreshold;
            set => _shrinkThreshold = Mathf.Clamp(value, 0.1f, 0.9f);
        }

        #endregion

        #region Constructors
        
        /// <summary>
        /// Creates a new thread-safe pool with the specified factory function
        /// </summary>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="config">Optional configuration for the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        public ThreadSafePool(Func<T> factory, ThreadSafePoolConfig config = null, string poolName = null)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _factory = factory;
            _resetAction = null;
            _config = config ?? new ThreadSafePoolConfig();
            PoolName = poolName ?? $"ThreadSafePool<{typeof(T).Name}>";
            
            _inactive = new ConcurrentBag<T>();
            _active = new HashSet<T>();
            
            // Initialize metrics tracking
            _totalCreated = 0;
            _peakUsage = 0;
            _totalAcquired = 0;
            _totalReleased = 0;
            _lastShrinkTime = Time.realtimeSinceStartup;
            
            if (_config.CollectMetrics)
            {
                _acquisitionTimes = new Dictionary<T, float>();
            }
            
            // Configure shrinking parameters
            _minimumCapacity = _config.InitialCapacity;
            _maximumCapacity = _config.MaximumCapacity <= 0 ? int.MaxValue : _config.MaximumCapacity;
            _autoShrinkEnabled = _config.EnableAutoShrink;
            _shrinkInterval = _config.ShrinkInterval;
            _shrinkThreshold = _config.ShrinkThreshold;
            
            // Get dependencies
            _logger = PoolingServices.GetService<PoolLogger>();
            _profiler = PoolingServices.GetService<PoolProfiler>();
            _diagnostics = PoolingServices.GetService<IPoolDiagnostics>();
            
            // Register with diagnostics
            _diagnostics?.RegisterPool(this, PoolName);
            
            // Prewarm the pool if configured
            if (_config.PrewarmOnInit)
            {
                PrewarmPool(_config.InitialCapacity);
            }
            
            IsCreated = true;
            
            _logger?.LogInfoInstance($"Created {PoolName} with initial capacity {_config.InitialCapacity}");
        }
        
        /// <summary>
        /// Creates a new thread-safe pool with the specified factory function and reset action
        /// </summary>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="resetAction">Action to reset objects when returned to the pool</param>
        /// <param name="config">Optional configuration for the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        public ThreadSafePool(Func<T> factory, Action<T> resetAction = null, ThreadSafePoolConfig config = null, string poolName = null)
            : this(factory, config, poolName)
        {
            _resetAction = resetAction;
        }
        
        #endregion

        #region Pool Operations
        
        /// <summary>
        /// Prewarms the pool by creating a specified number of objects
        /// </summary>
        /// <param name="count">Number of objects to create</param>
        public void PrewarmPool(int count)
        {
            if (count <= 0) return;
            
            if (_profiler != null)
            {
                _profiler.BeginSample("Prewarm", PoolName);
            }
            
            try
            {
                for (int i = 0; i < count; i++)
                {
                    _inactive.Add(CreateNewItem());
                }
                
                _logger?.LogInfoInstance($"Prewarmed {PoolName} with {count} items");
            }
            finally
            {
                if (_profiler != null)
                {
                    _profiler.EndSample("Prewarm", PoolName, ActiveCount, InactiveCount);
                }
            }
        }

        /// <inheritdoc />
        public T Acquire()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(PoolName);
                
            if (_profiler != null)
            {
                _profiler.BeginSample("Acquire", PoolName);
            }
            
            _diagnostics?.RecordAcquireStart(this);
                
            T item;
            bool isNew = false;
            
            // Try to get an inactive item
            if (!_inactive.TryTake(out item))
            {
                // No inactive items, create a new one
                item = CreateNewItem();
                isNew = true;
            }
            
            // Mark as active
            lock (_lock)
            {
                _active.Add(item);
                _totalAcquired++;
                
                if (_active.Count > _peakUsage)
                {
                    _peakUsage = _active.Count;
                }
                
                // Track acquisition time for metrics
                if (_acquisitionTimes != null)
                {
                    _acquisitionTimes[item] = Time.realtimeSinceStartup;
                }
            }
            
            // Call OnAcquire for IPoolable items
            OnItemAcquired(item);
            
            _diagnostics?.RecordAcquireComplete(this, ActiveCount, item);
            
            if (_profiler != null)
            {
                _profiler.EndSample("Acquire", PoolName, ActiveCount, InactiveCount);
            }
            
            return item;
        }

        /// <inheritdoc />
        public List<T> AcquireMultiple(int count)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(PoolName);
                
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
                
            var result = new List<T>(count);
            
            for (int i = 0; i < count; i++)
            {
                result.Add(Acquire());
            }
            
            return result;
        }

        /// <inheritdoc />
        public T AcquireAndSetup(Action<T> setupAction)
        {
            var item = Acquire();
            
            try
            {
                setupAction?.Invoke(item);
            }
            catch (Exception ex)
            {
                // If setup fails, return the item to the pool and rethrow
                Release(item);
                _logger?.LogErrorInstance($"Setup action failed in {PoolName}: {ex.Message}");
                throw;
            }
            
            return item;
        }

        /// <inheritdoc />
        public void Release(T item)
        {
            if (IsDisposed)
            {
                DestroyItem(item);
                return;
            }
                
            if (item == null)
            {
                _logger?.LogWarningInstance($"Attempted to release null item to {PoolName}");
                return;
            }
            
            if (_profiler != null)
            {
                _profiler.BeginSample("Release", PoolName);
            }
            
            bool wasActive;
            float acquisitionTime = 0f;
            
            // Remove from active collection
            lock (_lock)
            {
                wasActive = _active.Remove(item);
                _totalReleased++;
                
                // Track object lifetime for metrics
                if (_acquisitionTimes != null && _acquisitionTimes.TryGetValue(item, out float acquiredAt))
                {
                    acquisitionTime = Time.realtimeSinceStartup - acquiredAt;
                    _acquisitionTimes.Remove(item);
                }
            }
            
            if (!wasActive)
            {
                _logger?.LogWarningInstance($"Attempted to release an item that wasn't active in {PoolName}");
                return;
            }
            
            // Call OnRelease for IPoolable items
            OnItemReleased(item);
            
            // Reset the item if needed
            if (_config.ResetOnRelease && _resetAction != null)
            {
                try
                {
                    _resetAction(item);
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"Reset action failed in {PoolName}: {ex.Message}");
                }
            }
            
            // Add back to inactive collection
            _inactive.Add(item);
            
            _diagnostics?.RecordRelease(this, ActiveCount, item);
            
            if (_profiler != null)
            {
                _profiler.EndSample("Release", PoolName, ActiveCount, InactiveCount);
            }
            
            // Check if we should auto-shrink
            CheckAutoShrink();
        }

        /// <inheritdoc />
        public void ReleaseMultiple(IEnumerable<T> items)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(PoolName);
                
            if (items == null)
                throw new ArgumentNullException(nameof(items));
                
            foreach (var item in items)
            {
                Release(item);
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(PoolName);
                
            if (_profiler != null)
            {
                _profiler.BeginSample("Clear", PoolName);
            }
            
            List<T> activeItems;
            
            // Get all active items
            lock (_lock)
            {
                activeItems = new List<T>(_active);
            }
            
            // Release all active items
            foreach (var item in activeItems)
            {
                Release(item);
            }
            
            _logger?.LogInfoInstance($"Cleared {PoolName}");
            
            if (_profiler != null)
            {
                _profiler.EndSample("Clear", PoolName, ActiveCount, InactiveCount);
            }
        }

        /// <inheritdoc />
        public void EnsureCapacity(int capacity)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(PoolName);
                
            if (capacity <= 0) return;
            
            int currentCapacity = TotalCount;
            int toAdd = capacity - currentCapacity;
            
            if (toAdd > 0)
            {
                PrewarmPool(toAdd);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (IsDisposed) return;
            
            Clear();
            
            // Clear inactive items
            lock (_lock)
            {
                while (_inactive.TryTake(out T item))
                {
                    DestroyItem(item);
                }
                
                _active.Clear();
                
                if (_acquisitionTimes != null)
                {
                    _acquisitionTimes.Clear();
                }
            }
            
            // Unregister from diagnostics
            _diagnostics?.UnregisterPool(this);
            
            _logger?.LogInfoInstance($"Disposed {PoolName}");
            
            IsDisposed = true;
            IsCreated = false;
        }
        
        #endregion

        #region Shrinking Support
        
        /// <inheritdoc />
        public T CreateNew()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(PoolName);
        
            return CreateNewItem();
        }

        /// <inheritdoc />
        public void TryAutoShrink()
        {
            if (IsDisposed || !_autoShrinkEnabled)
                return;
        
            float currentTime = Time.realtimeSinceStartup;
    
            // Only try to shrink if enough time has passed since last shrink
            if (currentTime - _lastShrinkTime < _shrinkInterval)
                return;
        
            if (TryShrink(_shrinkThreshold))
            {
                _lastShrinkTime = currentTime;
            }
        }
        
        /// <inheritdoc />
        public bool TryShrink(float threshold)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(PoolName);
                
            if (threshold <= 0f || threshold >= 1f)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be between 0 and 1");
                
            // Only shrink if we're below the threshold
            int currentActive = ActiveCount;
            int totalItems = TotalCount;
            
            if (totalItems <= _minimumCapacity || totalItems == 0)
                return false;
                
            float usageRatio = (float)currentActive / totalItems;
            
            if (usageRatio > threshold)
                return false;
                
            // Target capacity is the maximum of:
            // 1. Current active count * growth factor (to avoid immediate regrowth)
            // 2. Minimum capacity
            int targetCapacity = Mathf.Max(
                Mathf.CeilToInt(currentActive * _growthFactor),
                _minimumCapacity
            );
            
            return ShrinkTo(targetCapacity);
        }

        /// <inheritdoc />
        public bool ShrinkTo(int targetCapacity)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(PoolName);
                
            if (targetCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(targetCapacity), "Target capacity cannot be negative");
                
            if (_profiler != null)
            {
                _profiler.BeginSample("Shrink", PoolName);
            }
            
            try
            {
                int currentActive = ActiveCount;
                
                // Cannot shrink below active count
                if (targetCapacity < currentActive)
                {
                    targetCapacity = currentActive;
                }
                
                // Cannot shrink below minimum capacity
                if (targetCapacity < _minimumCapacity)
                {
                    targetCapacity = _minimumCapacity;
                }
                
                int totalItems = TotalCount;
                
                // Already at or below target capacity
                if (totalItems <= targetCapacity)
                {
                    return false;
                }
                
                int toRemove = totalItems - targetCapacity;
                int removed = 0;
                
                // Remove items from the inactive pool until we reach the target
                while (removed < toRemove && _inactive.TryTake(out T item))
                {
                    DestroyItem(item);
                    removed++;
                }
                
                _logger?.LogInfoInstance($"Shrunk {PoolName} by removing {removed} items, new capacity: {TotalCount}");
                
                return removed > 0;
            }
            finally
            {
                if (_profiler != null)
                {
                    _profiler.EndSample("Shrink", PoolName, ActiveCount, InactiveCount);
                }
            }
        }

        /// <inheritdoc />
        public void SetAutoShrink(bool enabled)
        {
            _autoShrinkEnabled = enabled;
            _lastShrinkTime = Time.realtimeSinceStartup;
        }
        
        /// <summary>
        /// Checks if auto-shrinking should be performed and does it if needed
        /// </summary>
        private void CheckAutoShrink()
        {
            if (!_autoShrinkEnabled)
                return;
                
            float currentTime = Time.realtimeSinceStartup;
            
            if (currentTime - _lastShrinkTime < _shrinkInterval)
                return;
                
            if (TryShrink(_shrinkThreshold))
            {
                _lastShrinkTime = currentTime;
            }
        }
        
        #endregion

        #region Helper Methods
        
        /// <summary>
        /// Creates a new item using the factory
        /// </summary>
        /// <returns>Newly created item</returns>
        private T CreateNewItem()
        {
            if (_profiler != null)
            {
                _profiler.BeginSample("Create", PoolName);
            }
            
            try
            {
                var item = _factory();
                
                if (item == null)
                {
                    throw new InvalidOperationException($"Factory function in {PoolName} returned null");
                }
                
                Interlocked.Increment(ref _totalCreated);
                _diagnostics?.RecordCreate(this);
                
                return item;
            }
            finally
            {
                if (_profiler != null)
                {
                    _profiler.EndSample("Create", PoolName, ActiveCount, InactiveCount);
                }
            }
        }

        /// <summary>
        /// Called when an item is acquired from the pool
        /// </summary>
        /// <param name="item">The acquired item</param>
        protected virtual void OnItemAcquired(T item)
        {
            // Call OnAcquire for IPoolable items
            if (item is IPoolable poolable)
            {
                try
                {
                    poolable.OnAcquire();
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"OnAcquire failed in {PoolName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Called when an item is released back to the pool
        /// </summary>
        /// <param name="item">The released item</param>
        protected virtual void OnItemReleased(T item)
        {
            // Call OnRelease for IPoolable items
            if (item is IPoolable poolable)
            {
                try
                {
                    poolable.OnRelease();
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"OnRelease failed in {PoolName}: {ex.Message}");
                }
                
                // Also call Reset for IPoolable items if configured
                if (_config.ResetOnRelease)
                {
                    try
                    {
                        poolable.Reset();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogErrorInstance($"Reset failed in {PoolName}: {ex.Message}");
                    }
                }
            }
        }

        /// <inheritdoc />
        public virtual void DestroyItem(T item)
        {
            if (item == null) return;
            
            // Call OnDestroy for IPoolable items
            if (item is IPoolable poolable)
            {
                try
                {
                    poolable.OnDestroy();
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"OnDestroy failed in {PoolName}: {ex.Message}");
                }
            }
            
            // Handle specific types
            if (item is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"Dispose failed in {PoolName}: {ex.Message}");
                }
            }
            else if (item is UnityEngine.Object unityObj)
            {
                try
                {
                    UnityEngine.Object.Destroy(unityObj);
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"Destroy failed in {PoolName}: {ex.Message}");
                }
            }
        }

        #endregion

        #region Metrics Support
        
        /// <inheritdoc />
        public Dictionary<string, object> GetMetrics()
        {
            var metrics = new Dictionary<string, object>
            {
                { "PoolName", PoolName },
                { "ItemType", typeof(T).Name },
                { "TotalCount", TotalCount },
                { "ActiveCount", ActiveCount },
                { "InactiveCount", InactiveCount },
                { "PeakActiveCount", _peakUsage },
                { "TotalCreated", _totalCreated },
                { "TotalAcquired", _totalAcquired },
                { "TotalReleased", _totalReleased },
                { "ThreadingMode", ThreadingMode.ToString() },
                { "MinimumCapacity", _minimumCapacity },
                { "MaximumCapacity", _maximumCapacity },
                { "AutoShrinkEnabled", _autoShrinkEnabled },
                { "ShrinkThreshold", _shrinkThreshold },
                { "ShrinkInterval", _shrinkInterval },
                { "GrowthFactor", _growthFactor },
                { "IsDisposed", IsDisposed },
                { "IsCreated", IsCreated },
                { "SampleTime", Time.realtimeSinceStartup }
            };
            
            return metrics;
        }

        /// <inheritdoc />
        public void ResetMetrics()
        {
            lock (_lock)
            {
                _peakUsage = ActiveCount;
                _totalAcquired = ActiveCount;
                _totalReleased = 0;
                
                if (_acquisitionTimes != null)
                {
                    _acquisitionTimes.Clear();
                    
                    // Rebuild acquisition times for currently active items
                    float now = Time.realtimeSinceStartup;
                    foreach (var item in _active)
                    {
                        _acquisitionTimes[item] = now;
                    }
                }
            }
            
            _logger?.LogInfoInstance($"Reset metrics for {PoolName}");
        }

        #endregion
    }
}