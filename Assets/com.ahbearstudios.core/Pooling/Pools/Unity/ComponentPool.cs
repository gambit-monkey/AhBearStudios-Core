using System;
using System.Collections.Generic;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Pooling.Services;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Pools.Unity
{
    /// <summary>
    /// A pool for Unity Component instances that provides efficient object reuse.
    /// This implementation follows the IUnityPool interface for consistent Unity-specific pooling behavior.
    /// </summary>
    /// <typeparam name="T">Type of component to pool</typeparam>
    public class ComponentPool<T> : IComponentPool<T> where T : Component
    {
        private readonly T _prefab;
        private T _currentPrefab;
        private Transform _parent;
        private readonly ComponentPoolConfig _config;
        private readonly List<T> _activeItems = new List<T>();
        private readonly List<T> _inactiveItems = new List<T>();
        private readonly PoolLogger _logger;
        private readonly IPoolDiagnostics _diagnostics;
        private bool _setActiveOnAcquire = true;
        private bool _setInactiveOnRelease = true;
        
        private float _lastShrinkTime = 0f;
        private bool _isDisposed = false;
        private int _totalCreated = 0;
        private int _totalAcquired = 0;
        private int _totalReleased = 0;
        private int _peakActiveCount = 0;
        
        /// <summary>
        /// Gets whether this pool has been created and initialized
        /// </summary>
        public bool IsCreated => !_isDisposed;
        
        /// <summary>
        /// Gets whether this pool has been disposed
        /// </summary>
        public bool IsDisposed => _isDisposed;
        
        /// <summary>
        /// Gets the total number of items in the pool (active + inactive)
        /// </summary>
        public int TotalCount => _activeItems.Count + _inactiveItems.Count;
        
        /// <summary>
        /// Gets the number of active items
        /// </summary>
        public int ActiveCount => _activeItems.Count;
        
        /// <summary>
        /// Gets the number of inactive items
        /// </summary>
        public int InactiveCount => _inactiveItems.Count;
        
        /// <summary>
        /// Gets the type of items in the pool
        /// </summary>
        public Type ItemType => typeof(T);
        
        /// <summary>
        /// Gets the name of this pool
        /// </summary>
        public string PoolName { get; }
        
        /// <summary>
        /// Gets the capacity of the pool
        /// </summary>
        public int Capacity => TotalCount;
        
        /// <summary>
        /// Gets the parent transform for pooled objects
        /// </summary>
        public Transform ParentTransform => _parent;
        
        /// <summary>
        /// Gets whether this pool is using a parent transform
        /// </summary>
        public bool UsesParentTransform => _parent != null;
        
        /// <summary>
        /// Gets whether objects from this pool should be reset when released
        /// </summary>
        public bool ResetOnRelease => _config.ResetOnRelease;
        
        /// <summary>
        /// Gets whether objects from this pool should be disabled when released
        /// </summary>
        public bool DisableOnRelease => _config.DisableOnRelease || true; // Always disable for components
        
        /// <summary>
        /// Gets the peak number of active items at any one time
        /// </summary>
        public int PeakActiveCount => _peakActiveCount;
        
        /// <summary>
        /// Gets the total number of items created by this pool
        /// </summary>
        public int TotalCreatedCount => _totalCreated;
        
        /// <summary>
        /// Gets the total number of items acquired from this pool
        /// </summary>
        public int TotalAcquiredCount => _totalAcquired;
        
        /// <summary>
        /// Gets the total number of items released back to this pool
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
        public bool SupportsAutoShrink => _config.EnableAutoShrink;
        
        /// <summary>
        /// Gets or sets the minimum capacity that the pool will maintain even when shrinking
        /// </summary>
        public int MinimumCapacity { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum capacity that the pool can grow to
        /// </summary>
        public int MaximumCapacity 
        { 
            get => _config.MaximumCapacity;
            set => _config.MaximumCapacity = value;
        }
        
        /// <summary>
        /// Gets or sets the shrink interval in seconds.
        /// </summary>
        public float ShrinkInterval 
        { 
            get => _config.ShrinkInterval;
            set => _config.ShrinkInterval = value;
        }
        
        /// <summary>
        /// Gets or sets the growth factor when the pool needs to expand.
        /// </summary>
        public float GrowthFactor 
        { 
            get => _config.GrowthFactor;
            set => _config.GrowthFactor = value;
        }
        
        /// <summary>
        /// Gets or sets the shrink threshold.
        /// </summary>
        public float ShrinkThreshold 
        { 
            get => _config.ShrinkThreshold;
            set => _config.ShrinkThreshold = value;
        }

        /// <summary>
        /// Gets the peak usage of this pool (required by IPool)
        /// </summary>
        public int PeakUsage => _peakActiveCount;

        /// <summary>
        /// Gets the total items created by this pool (required by IPool)
        /// </summary>
        public int TotalCreated => _totalCreated;

        /// <summary>
        /// Creates a new component pool
        /// </summary>
        /// <param name="prefab">Prefab to instantiate for new pool items</param>
        /// <param name="parent">Optional parent transform for pooled objects</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <exception cref="ArgumentNullException">Thrown if prefab is null</exception>
        public ComponentPool(T prefab, Transform parent = null, ComponentPoolConfig config = null, string poolName = null)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab), "Prefab cannot be null");

            _prefab = prefab;
            _parent = parent;
            _config = config ?? new ComponentPoolConfig();
            PoolName = poolName ?? $"{typeof(T).Name}Pool_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Set minimum capacity
            MinimumCapacity = _config.InitialCapacity / 2;
            if (MinimumCapacity < 1) MinimumCapacity = 1;
            
            // Get services
            _logger = PoolingServices.GetService<PoolLogger>();
            _diagnostics = PoolingServices.GetService<IPoolDiagnostics>();
            
            // Register with diagnostics if available
            _diagnostics?.RegisterPool(this, PoolName);
            
            // Override parent from config if specified
            if (_config.UseParentTransform && _config.ParentTransform != null)
            {
                _parent = _config.ParentTransform;
            }

            // Initialize the pool with prewarm if configured
            if (_config.PrewarmOnInit && _config.InitialCapacity > 0)
            {
                PrewarmPool(_config.InitialCapacity);
            }
            
            _logger?.LogInfoInstance($"Created component pool: {PoolName} for {typeof(T).Name}");
        }

        /// <summary>
        /// Prewarms the pool by creating a specified number of inactive instances
        /// </summary>
        /// <param name="count">Number of instances to create</param>
        public void PrewarmPool(int count)
        {
            ThrowIfDisposed();
            
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
                
            for (int i = 0; i < count; i++)
            {
                T instance = CreateNewInstance();
                _inactiveItems.Add(instance);
                _totalCreated++;
            }
            
            _logger?.LogDebugInstance($"Prewarmed pool {PoolName} with {count} items");
        }
        
        // <summary>
        /// Gets or sets whether components should be activated when acquired
        /// </summary>
        public bool SetActiveOnAcquire
        {
            get => _setActiveOnAcquire;
            set => _setActiveOnAcquire = value;
        }

        /// <summary>
        /// Gets or sets whether components should be deactivated when released
        /// </summary>
        public bool SetInactiveOnRelease
        {
            get => _setInactiveOnRelease;
            set => _setInactiveOnRelease = value;
        }

        // Constructor and other existing methods...

        /// <summary>
        /// Creates a new instance of the pooled object type
        /// </summary>
        /// <returns>A new instance</returns>
        public T CreateNew()
        {
            return CreateNewInstance();
        }

        /// <summary>
        /// Acquires an item from the pool
        /// </summary>
        /// <returns>The acquired component</returns>
        public T Acquire()
        {
            ThrowIfDisposed();

            _diagnostics?.RecordAcquireStart(this);

            // Try to get an inactive item, or create a new one if needed
            T item;
            if (_inactiveItems.Count > 0)
            {
                int lastIndex = _inactiveItems.Count - 1;
                item = _inactiveItems[lastIndex];
                _inactiveItems.RemoveAt(lastIndex);
            }
            else
            {
                // Check if we've reached the maximum pool size
                if (_config.MaximumCapacity > 0 && TotalCount >= _config.MaximumCapacity)
                {
                    if (_config.ThrowIfExceedingMaxCount)
                    {
                        throw new InvalidOperationException($"Pool '{PoolName}' has reached its maximum size of {_config.MaximumCapacity}");
                    }

                    // If we're not throwing, return the oldest active item
                    if (_activeItems.Count > 0)
                    {
                        _logger?.LogWarningInstance($"Pool {PoolName} exceeded max size ({_config.MaximumCapacity}). Recycling oldest active item.");
                        item = _activeItems[0];
                        _activeItems.RemoveAt(0);
                    }
                    else
                    {
                        // Should never happen but just in case
                        item = CreateNew();
                        _totalCreated++;
                    }
                }
                else
                {
                    // Create a new instance
                    item = CreateNew();
                    _totalCreated++;
                }
            }

            // Enable the instance and mark it as active
            if (SetActiveOnAcquire && item.gameObject != null)
            {
                item.gameObject.SetActive(true);
            }

            // Add to active list
            _activeItems.Add(item);
            _totalAcquired++;

            // Update peak usage
            if (_activeItems.Count > _peakActiveCount)
            {
                _peakActiveCount = _activeItems.Count;
            }

            // Call OnAcquire if this is a poolable component
            if (_config.InvokeLifecycleMethods && item is IPoolable poolable)
            {
                poolable.OnAcquire();
            }

            _diagnostics?.RecordAcquireComplete(this, ActiveCount, item);

            return item;
        }

        /// <summary>
        /// Acquires an item and applies a setup action before returning it
        /// </summary>
        /// <param name="setupAction">Action to perform on the item before returning it</param>
        /// <returns>The acquired and setup component</returns>
        public T AcquireAndSetup(Action<T> setupAction)
        {
            T item = Acquire();
            
            if (item != null && setupAction != null)
            {
                setupAction(item);
            }
            
            return item;
        }

        /// <summary>
        /// Acquires an item and positions it in the scene
        /// </summary>
        /// <param name="position">Position to set</param>
        /// <param name="rotation">Rotation to set</param>
        /// <returns>The acquired component</returns>
        public T Acquire(Vector3 position, Quaternion rotation)
        {
            T item = Acquire();
            
            if (item != null && item.transform != null)
            {
                item.transform.position = position;
                item.transform.rotation = rotation;
            }
            
            return item;
        }

        /// <summary>
        /// Acquires an object and positions it at the specified transform
        /// </summary>
        /// <param name="transform">Transform to match position and rotation</param>
        /// <returns>The acquired and positioned object</returns>
        public T AcquireAtTransform(Transform transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));
                
            return Acquire(transform.position, transform.rotation);
        }

        /// <summary>
        /// Acquires multiple items from the pool at once
        /// </summary>
        /// <param name="count">Number of items to acquire</param>
        /// <returns>List of acquired components</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative</exception>
        public List<T> AcquireMultiple(int count)
        {
            ThrowIfDisposed();
            
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
        /// Releases an item back to the pool
        /// </summary>
        /// <param name="component">Component to release</param>
        public void Release(T component)
        {
            if (component == null)
                return;

            if (_isDisposed)
            {
                DestroyItem(component);
                return;
            }

            // Check if the item is actually in our active list
            if (!_activeItems.Remove(component))
            {
                // The item wasn't in our active list, maybe it's already released
                if (_inactiveItems.Contains(component))
                {
                    _logger?.LogWarningInstance($"Attempted to release component that is already in the inactive pool: {component.name}");
                    return;
                }

                // It's not from this pool, either destroy it or add it anyway
                if (_config.MaximumCapacity > 0 && TotalCount >= _config.MaximumCapacity)
                {
                    _logger?.LogWarningInstance($"Attempted to release external component when pool is at capacity: {component.name}");
                    DestroyItem(component);
                    return;
                }

                _logger?.LogWarningInstance($"Adding external component to pool: {component.name}");
            }

            // Call OnRelease if this is a poolable component
            if (_config.InvokeLifecycleMethods && component is IPoolable poolable)
            {
                poolable.OnRelease();
            }

            // Reset component state if configured
            if (_config.ResetComponentOnRelease && component is IPoolable resetable)
            {
                resetable.Reset();
            }

            // Disable the gameObject if configured or forced
            if (SetInactiveOnRelease && component.gameObject != null)
            {
                component.gameObject.SetActive(false);
            }

            // Detach from hierarchy if configured
            if (_config.DetachFromHierarchy && component.transform != null)
            {
                if (_parent != null)
                {
                    component.transform.SetParent(_parent, false);
                }
                else
                {
                    component.transform.SetParent(null);
                }
            }

            // Add to inactive list
            _inactiveItems.Add(component);
            _totalReleased++;

            _diagnostics?.RecordRelease(this, ActiveCount, component);

            // Check if we should auto-shrink
            if (SupportsAutoShrink)
            {
                TryAutoShrink();
            }
        }

        /// <summary>
        /// Releases multiple objects back to the pool
        /// </summary>
        /// <param name="items">Collection of objects to release</param>
        public void ReleaseMultiple(IEnumerable<T> items)
        {
            if (items == null)
                return;
                
            foreach (T item in items)
            {
                if (item == null) continue;
                
                Release(item);
            }
        }

        /// <summary>
        /// Releases multiple objects back to the pool
        /// </summary>
        /// <param name="items">Collection of objects to release</param>
        /// <param name="onRelease">Optional action to perform on each object when releasing</param>
        public void ReleaseMultiple(IEnumerable<T> items, Action<T> onRelease = null)
        {
            if (items == null)
                return;
                
            foreach (T item in items)
            {
                if (item == null) continue;
                
                if (onRelease != null)
                {
                    onRelease(item);
                }
                
                Release(item);
            }
        }

        /// <summary>
        /// Clears the pool, returning all active items to the inactive state
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();
            
            // Copy to avoid modification during iteration
            var activeItemsCopy = new List<T>(_activeItems);
            
            foreach (T item in activeItemsCopy)
            {
                Release(item);
            }
            
            // Ensure active list is clean
            _activeItems.Clear();
            
            _logger?.LogDebugInstance($"Cleared pool {PoolName}");
        }

        /// <summary>
        /// Ensures the pool has at least the specified capacity
        /// </summary>
        /// <param name="capacity">Required capacity</param>
        public void EnsureCapacity(int capacity)
        {
            ThrowIfDisposed();
            
            int currentCapacity = TotalCount;
            int additionalItemsNeeded = capacity - currentCapacity;
            
            if (additionalItemsNeeded <= 0)
                return;
                
            // Create additional items
            for (int i = 0; i < additionalItemsNeeded; i++)
            {
                T instance = CreateNewInstance();
                _inactiveItems.Add(instance);
                _totalCreated++;
            }
            
            _logger?.LogDebugInstance($"Expanded pool {PoolName} capacity to {capacity}");
        }

        /// <summary>
        /// Destroys a component instance, removing it from the pool permanently
        /// </summary>
        /// <param name="item">Component to destroy</param>
        public void DestroyItem(T item)
        {
            if (item == null)
                return;

            // Call OnDestroy if this is a poolable component
            if (_config.InvokeLifecycleMethods && item is IPoolable poolable)
            {
                poolable.OnDestroy();
            }

            // Destroy the gameObject
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(item.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(item.gameObject);
            }
        }

        /// <summary>
        /// Sets the parent transform for pooled objects
        /// </summary>
        /// <param name="parent">The parent transform to use</param>
        public void SetParentTransform(Transform parent)
        {
            ThrowIfDisposed();
            
            _parent = parent;
            
            // Update existing inactive items to use the new parent
            foreach (var item in _inactiveItems)
            {
                if (item != null && item.transform != null)
                {
                    item.transform.SetParent(_parent, false);
                }
            }
        }

        /// <summary>
        /// Gets metrics for this pool
        /// </summary>
        /// <returns>Dictionary of pool metrics</returns>
        public Dictionary<string, object> GetMetrics()
        {
            var metrics = new Dictionary<string, object>
            {
                ["PoolName"] = PoolName,
                ["ItemType"] = typeof(T).Name,
                ["ActiveCount"] = ActiveCount,
                ["InactiveCount"] = InactiveCount,
                ["TotalCount"] = TotalCount,
                ["PeakActiveCount"] = _peakActiveCount,
                ["TotalCreated"] = _totalCreated,
                ["TotalAcquired"] = _totalAcquired,
                ["TotalReleased"] = _totalReleased,
                ["IsDisposed"] = _isDisposed,
                ["MaximumCapacity"] = _config.MaximumCapacity,
                ["AutoShrink"] = _config.EnableAutoShrink,
                ["ShrinkThreshold"] = _config.ShrinkThreshold,
                ["MinimumCapacity"] = MinimumCapacity
            };
            
            return metrics;
        }

        /// <summary>
        /// Gets Unity-specific metrics for this pool
        /// </summary>
        /// <returns>Dictionary of Unity-specific metrics</returns>
        public Dictionary<string, object> GetUnityMetrics()
        {
            var metrics = new Dictionary<string, object>
            {
                ["PrefabType"] = _prefab.GetType().Name,
                ["HasParent"] = _parent != null,
                ["ParentName"] = _parent != null ? _parent.name : "None",
                ["DisableOnRelease"] = _config.DisableOnRelease,
                ["ResetOnRelease"] = _config.ResetComponentOnRelease,
                ["DetachFromHierarchy"] = _config.DetachFromHierarchy,
                ["InvokeLifecycleMethods"] = _config.InvokeLifecycleMethods,
                ["ThrowIfExceedingMaxCount"] = _config.ThrowIfExceedingMaxCount,
                ["UseExponentialGrowth"] = _config.UseExponentialGrowth,
                ["GrowthFactor"] = _config.GrowthFactor,
                ["GrowthIncrement"] = _config.GrowthIncrement
            };
            
            return metrics;
        }
        
        /// <summary>
        /// Sets the prefab used to create new instances when the pool needs to grow
        /// </summary>
        /// <param name="prefab">The prefab to use</param>
        public void SetPrefab(T prefab)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
        
            // Only allow changing prefab if pool is empty or we're in editor mode
            if (TotalCount > 0 && Application.isPlaying)
            {
                _logger?.LogWarningInstance($"Changing prefab for non-empty pool {PoolName}. This may cause inconsistencies.");
            }
    
            // Store the prefab in a cache since we can't modify the readonly field
            _currentPrefab = prefab;
        }

        /// <summary>
        /// Gets the original prefab this pool is using to create instances
        /// </summary>
        /// <returns>The original prefab</returns>
        public T GetPrefab()
        {
            // Return the current prefab if it exists, otherwise return the original
            return _currentPrefab != null ? _currentPrefab : _prefab;
        }

        /// <summary>
        /// Creates a new instance of the pooled object type
        /// </summary>
        /// <returns>A new instance</returns>
        public T CreateNewInstance()
        {
            T instance;
            // Use the current prefab if available, otherwise use the original
            T prefabToUse = _currentPrefab != null ? _currentPrefab : _prefab;
    
            if (_parent != null)
            {
                instance = UnityEngine.Object.Instantiate(prefabToUse, _parent);
            }
            else
            {
                instance = UnityEngine.Object.Instantiate(prefabToUse);
            }
    
            // Disable initially
            if (instance.gameObject != null)
            {
                instance.gameObject.SetActive(false);
            }
    
            return instance;
        }

        /// <summary>
        /// Attempts to reduce fragmentation in the pool
        /// </summary>
        /// <returns>True if defragmentation was performed, false otherwise</returns>
        public bool TryDefragment()
        {
            // Unity objects don't need traditional defragmentation as they're managed by Unity
            // This is more for consistency with the interface
            return false;
        }

        /// <summary>
        /// Checks if a specific game object or component belongs to this pool
        /// </summary>
        /// <param name="objectInstance">The object to check</param>
        /// <returns>True if the object belongs to this pool, false otherwise</returns>
        public bool ContainsInstance(UnityEngine.Object objectInstance)
        {
            if (objectInstance == null)
                return false;
                
            if (objectInstance is T component)
            {
                return _activeItems.Contains(component) || _inactiveItems.Contains(component);
            }
            else if (objectInstance is GameObject gameObject)
            {
                // Check if any of our components belong to this GameObject
                foreach (var item in _activeItems)
                {
                    if (item.gameObject == gameObject)
                        return true;
                }
                
                foreach (var item in _inactiveItems)
                {
                    if (item.gameObject == gameObject)
                        return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Attempts to shrink the pool's capacity to reduce memory usage
        /// </summary>
        /// <param name="threshold">Threshold factor (0-1) determining when shrinking occurs</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool TryShrink(float threshold)
        {
            ThrowIfDisposed();
            
            // Can't shrink below active count or minimum capacity
            int minCapacity = Math.Max(_activeItems.Count, MinimumCapacity);
            
            // Calculate the target capacity based on usage and threshold
            float usageRatio = (float)_activeItems.Count / TotalCount;
            if (usageRatio >= threshold)
            {
                // Usage is above threshold, no need to shrink
                return false;
            }
            
            // Calculate target capacity: enough to hold active items plus some buffer
            int targetCapacity = (int)(_activeItems.Count / threshold);
            targetCapacity = Math.Max(targetCapacity, minCapacity);
            
            return ShrinkTo(targetCapacity);
        }

        /// <summary>
        /// Explicitly shrinks the pool to the specified capacity
        /// </summary>
        /// <param name="targetCapacity">The target capacity to shrink to</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool ShrinkTo(int targetCapacity)
        {
            ThrowIfDisposed();
            
            // Can't shrink below active count or minimum capacity
            int minCapacity = Math.Max(_activeItems.Count, MinimumCapacity);
            
            if (targetCapacity < minCapacity)
            {
                targetCapacity = minCapacity;
            }
            
            // Check if we need to shrink
            if (TotalCount <= targetCapacity)
            {
                return false;
            }
            
            // Calculate how many items to remove
            int excessItems = TotalCount - targetCapacity;
            int itemsToRemove = Math.Min(excessItems, _inactiveItems.Count);
            
            if (itemsToRemove <= 0)
            {
                return false;
            }
            
            // Remove items from the end of the inactive list
            for (int i = 0; i < itemsToRemove; i++)
            {
                int lastIndex = _inactiveItems.Count - 1;
                T item = _inactiveItems[lastIndex];
                _inactiveItems.RemoveAt(lastIndex);
                
                DestroyItem(item);
            }
            
            _logger?.LogDebugInstance($"Shrunk pool {PoolName} by removing {itemsToRemove} items. New capacity: {TotalCount}");
            _diagnostics?.RecordShrink(this, itemsToRemove);
            
            return true;
        }

        /// <summary>
        /// Explicitly enables or disables automatic shrinking
        /// </summary>
        /// <param name="enabled">Whether automatic shrinking should be enabled</param>
        public void SetAutoShrink(bool enabled)
        {
            _config.EnableAutoShrink = enabled;
        }

        /// <summary>
        /// Tries to automatically shrink the pool based on elapsed time and thresholds
        /// </summary>
        public void TryAutoShrink()
        {
            if (!_config.EnableAutoShrink)
                return;

            float currentTime = Time.time;
            if (currentTime - _lastShrinkTime < _config.ShrinkInterval)
                return;

            _lastShrinkTime = currentTime;

            TryShrink(_config.ShrinkThreshold);
        }

        /// <summary>
        /// Resets the performance metrics of the pool
        /// </summary>
        public void ResetMetrics()
        {
            _peakActiveCount = _activeItems.Count;
            _totalCreated = TotalCount;
            _totalAcquired = _activeItems.Count;
            _totalReleased = _inactiveItems.Count;
            
            _logger?.LogDebugInstance($"Reset metrics for pool {PoolName}");
        }

        /// <summary>
        /// Throws an exception if the pool has been disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ComponentPool<T>), $"Pool '{PoolName}' has been disposed");
            }
        }

        /// <summary>
        /// Disposes the pool, destroying all items
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            // Destroy all items
            foreach (T item in _activeItems)
            {
                DestroyItem(item);
            }
            
            foreach (T item in _inactiveItems)
            {
                DestroyItem(item);
            }
            
            _activeItems.Clear();
            _inactiveItems.Clear();
            
            // Unregister from diagnostics
            _diagnostics?.UnregisterPool(this);
            
            _logger?.LogInfoInstance($"Disposed component pool: {PoolName}");
            
            _isDisposed = true;
        }
    }
}