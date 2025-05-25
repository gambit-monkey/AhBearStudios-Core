using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;
using AhBearStudios.Pooling.Services;
using UnityEngine;

namespace AhBearStudios.Pooling.Pools.Unity
{
    /// <summary>
    /// A robust pool implementation for managing Unity GameObjects with improved memory 
    /// efficiency and safety features. Implements IUnityPool interface for enhanced 
    /// functionality specific to Unity object pooling.
    /// </summary>
    public class GameObjectPool : IGameObjectPool<GameObject>
    {
        #region Private Fields

        private readonly List<GameObject> _inactiveObjects;
        private readonly HashSet<GameObject> _activeObjects;
        private GameObject _prefab;
        private GameObject _currentPrefab;
        private Transform _parent;
        private readonly PoolLogger _logger;
        private readonly string _poolName;
        private GameObjectPoolConfig _config;
        private bool _isDisposed;
        private int _totalCreated;
        private int _peakActiveCount;
        private float _lastShrinkTime;
        private bool _isCreated;
        private int _totalAcquiredCount;
        private int _totalReleasedCount;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether this pool has been created and initialized
        /// </summary>
        public bool IsCreated => _isCreated;

        /// <summary>
        /// Gets the total number of items in the pool (active + inactive)
        /// </summary>
        public int TotalCount => ActiveCount + InactiveCount;

        /// <summary>
        /// Gets the number of active items
        /// </summary>
        public int ActiveCount => _activeObjects.Count;

        /// <summary>
        /// Gets the number of inactive items
        /// </summary>
        public int InactiveCount => _inactiveObjects.Count;

        /// <summary>
        /// Gets the peak number of simultaneously active items
        /// </summary>
        public int PeakUsage => _peakActiveCount;

        /// <summary>
        /// Gets the total number of items ever created by this pool
        /// </summary>
        public int TotalCreated => _totalCreated;

        /// <summary>
        /// Gets the type of items in the pool
        /// </summary>
        public Type ItemType => typeof(GameObject);

        /// <summary>
        /// Gets whether this pool has been disposed
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Gets the name of this pool
        /// </summary>
        public string PoolName => _poolName;

        /// <summary>
        /// Gets the parent transform for pooled objects
        /// </summary>
        public Transform ParentTransform => _parent;

        /// <summary>
        /// Gets the capacity of the pool
        /// </summary>
        public int Capacity => TotalCount;

        /// <summary>
        /// Gets whether this pool is using a parent transform
        /// </summary>
        public bool UsesParentTransform => _parent != null;

        /// <summary>
        /// Gets whether this pool supports automatic shrinking
        /// </summary>
        public bool SupportsAutoShrink => _config?.EnableAutoShrink ?? false;

        /// <summary>
        /// Gets or sets the minimum capacity that the pool will maintain even when shrinking
        /// </summary>
        public int MinimumCapacity { get; set; }

        /// <summary>
        /// Gets or sets the maximum capacity that the pool can grow to
        /// </summary>
        public int MaximumCapacity { get; set; }

        /// <summary>
        /// Gets or sets the shrink interval in seconds
        /// </summary>
        public float ShrinkInterval { get; set; }

        /// <summary>
        /// Gets or sets the growth factor when the pool needs to expand
        /// </summary>
        public float GrowthFactor { get; set; }

        /// <summary>
        /// Gets or sets the shrink threshold
        /// </summary>
        public float ShrinkThreshold { get; set; }

        /// <summary>
        /// Gets whether objects from this pool should be reset when released
        /// </summary>
        public bool ResetOnRelease => _config?.ResetOnRelease ?? true;

        /// <summary>
        /// Gets whether objects from this pool should be disabled when released
        /// </summary>
        public bool DisableOnRelease => _config?.DisableOnRelease ?? true;

        /// <summary>
        /// Gets the threading mode for this pool
        /// </summary>
        public PoolThreadingMode ThreadingMode => PoolThreadingMode.ThreadLocal;

        #region IPoolMetrics Implementation

        /// <summary>
        /// Gets the peak number of simultaneously active items.
        /// </summary>
        public int PeakActiveCount => _peakActiveCount;

        /// <summary>
        /// Gets the total number of items ever created by this pool.
        /// </summary>
        public int TotalCreatedCount => _totalCreated;

        /// <summary>
        /// Gets the total number of items acquired from this pool.
        /// </summary>
        public int TotalAcquiredCount => _totalAcquiredCount;

        /// <summary>
        /// Gets the total number of items released back to this pool.
        /// </summary>
        public int TotalReleasedCount => _totalReleasedCount;

        /// <summary>
        /// Gets the current number of active items.
        /// </summary>
        public int CurrentActiveCount => _activeObjects.Count;

        /// <summary>
        /// Gets the current capacity of the pool.
        /// </summary>
        public int CurrentCapacity => TotalCount;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new GameObject pool
        /// </summary>
        /// <param name="prefab">Prefab GameObject to clone</param>
        /// <param name="parent">Optional parent transform for spawned objects</param>
        /// <param name="config">Optional pool configuration</param>
        /// <param name="poolName">Optional name for the pool</param>
        public GameObjectPool(GameObject prefab, Transform parent = null, GameObjectPoolConfig config = null, string poolName = null)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            _prefab = prefab;
            _currentPrefab = null;
            _parent = parent;
            _config = config ?? new GameObjectPoolConfig();
            _poolName = poolName ?? $"GameObjectPool_{prefab.name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            _logger = PoolingServices.TryGetService<PoolLogger>(out var logger) ? logger : null;
    
            // Initialize collections with capacity to avoid resizing
            int initialCapacity = _config.InitialCapacity;
            _inactiveObjects = new List<GameObject>(initialCapacity);
            _activeObjects = new HashSet<GameObject>(initialCapacity);
    
            // Set up shrinking parameters
            MinimumCapacity = Math.Max(1, _config.InitialCapacity / 2);
            MaximumCapacity = _config.MaximumCapacity > 0 ? _config.MaximumCapacity : int.MaxValue;
            ShrinkInterval = _config.ShrinkInterval;
            GrowthFactor = _config.GrowthFactor;
            ShrinkThreshold = _config.ShrinkThreshold;
    
            // Initialize metrics
            _totalCreated = 0;
            _peakActiveCount = 0;
            _totalAcquiredCount = 0;
            _totalReleasedCount = 0;
    
            // Register with registry if available
            PoolingServices.TryGetService<PoolRegistry>(out var registry);
            registry?.RegisterPool(this, _poolName);

            // Prewarm the pool if configured to do so
            if (_config.PrewarmOnInit && initialCapacity > 0)
            {
                PrewarmPool(initialCapacity);
            }

            _isCreated = true;
            _logger?.LogInfoInstance($"Created GameObjectPool for '{prefab.name}' with name '{_poolName}'");
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Tries to automatically shrink the pool based on elapsed time and thresholds
        /// </summary>
        public void TryAutoShrink()
        {
            if (!SupportsAutoShrink)
                return;

            float currentTime = Time.time;
            if (currentTime - _lastShrinkTime < ShrinkInterval)
                return;

            _lastShrinkTime = currentTime;
            TryShrink(ShrinkThreshold);
        }
        
        /// <summary>
        /// Destroys a GameObject instance, removing it from the pool permanently
        /// </summary>
        /// <param name="item">GameObject to destroy</param>
        public void DestroyItem(GameObject item)
        {
            if (item == null)
                return;

            // Remove from tracking collections if present
            _activeObjects.Remove(item);
            _inactiveObjects.Remove(item);

            // Destroy the GameObject
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(item);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(item);
            }
        }
        
        /// <summary>
        /// Prewarms the pool by creating GameObjects
        /// </summary>
        /// <param name="count">Number of GameObjects to create</param>
        public void PrewarmPool(int count)
        {
            ThrowIfDisposed();

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");

            for (int i = 0; i < count; i++)
            {
                var item = CreateNewInstance();
                _inactiveObjects.Add(item);
                _totalCreated++;
            }

            _logger?.LogInfoInstance($"Prewarmed {count} objects in pool '{PoolName}'");
        }

        /// <summary>
        /// Acquires a GameObject from the pool
        /// </summary>
        /// <returns>An available GameObject</returns>
        public GameObject Acquire()
        {
            ThrowIfDisposed();

            GameObject go = null;

            // Check if we have an inactive object
            if (_inactiveObjects.Count > 0)
            {
                int lastIndex = _inactiveObjects.Count - 1;
                go = _inactiveObjects[lastIndex];
                _inactiveObjects.RemoveAt(lastIndex);
            }
            else
            {
                // Create a new GameObject if needed
                if (_config.MaximumCapacity > 0 && TotalCount >= _config.MaximumCapacity)
                {
                    if (_config.ThrowIfExceedingMaxCount)
                    {
                        throw new InvalidOperationException($"Pool '{PoolName}' has reached its maximum size of {_config.MaximumCapacity}");
                    }
                    else
                    {
                        _logger?.LogWarningInstance($"Pool '{PoolName}' has reached its maximum size of {_config.MaximumCapacity}, but creating new instance anyway");
                    }
                }

                go = CreateNewInstance();
                _totalCreated++;
            }

            // Activate the GameObject
            go.SetActive(true);
            _activeObjects.Add(go);
            _totalAcquiredCount++;

            // Update peak count
            if (_activeObjects.Count > _peakActiveCount)
            {
                _peakActiveCount = _activeObjects.Count;
            }

            // Notify all IPoolable components
            var poolables = go.GetComponents<IPoolable>();
            foreach (var poolable in poolables)
            {
                poolable.OnAcquire();
            }

            return go;
        }

        /// <summary>
        /// Acquires a GameObject and positions it at the specified location
        /// </summary>
        /// <param name="position">World position</param>
        /// <param name="rotation">World rotation</param>
        /// <returns>The acquired and positioned object</returns>
        public GameObject Acquire(Vector3 position, Quaternion rotation)
        {
            return AcquireAndSetup(go =>
            {
                Transform t = go.transform;
                t.position = position;
                t.rotation = rotation;
            });
        }

        /// <summary>
        /// Acquires an object and positions it at the specified transform
        /// </summary>
        /// <param name="transform">Transform to match position and rotation</param>
        /// <returns>The acquired and positioned object</returns>
        public GameObject AcquireAtTransform(Transform transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            return Acquire(transform.position, transform.rotation);
        }

        /// <summary>
        /// Acquires an item and initializes it with a setup action
        /// </summary>
        /// <param name="setupAction">Action to initialize the item</param>
        /// <returns>The acquired and initialized item</returns>
        public GameObject AcquireAndSetup(Action<GameObject> setupAction)
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            GameObject go = Acquire();
            setupAction(go);
            return go;
        }

        /// <summary>
        /// Acquires multiple items from the pool at once
        /// </summary>
        /// <param name="count">Number of items to acquire</param>
        /// <returns>List of acquired items</returns>
        public List<GameObject> AcquireMultiple(int count)
        {
            ThrowIfDisposed();

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");

            List<GameObject> result = new List<GameObject>(count);

            for (int i = 0; i < count; i++)
            {
                result.Add(Acquire());
            }

            return result;
        }

        /// <summary>
        /// Releases a GameObject back to the pool
        /// </summary>
        /// <param name="gameObject">GameObject to release</param>
        public void Release(GameObject gameObject)
        {
            ThrowIfDisposed();

            if (gameObject == null)
            {
                _logger?.LogWarningInstance("Attempted to release null GameObject");
                return;
            }

            // Ensure the object belongs to this pool
            if (!_activeObjects.Remove(gameObject))
            {
                _logger?.LogWarningInstance($"Attempted to release GameObject that is not active in pool '{PoolName}'");
                return;
            }

            _totalReleasedCount++;

            // Notify all IPoolable components
            var poolables = gameObject.GetComponents<IPoolable>();
            foreach (var poolable in poolables)
            {
                poolable.OnRelease();
            }

            // Reset the GameObject
            if (ResetOnRelease)
            {
                foreach (var poolable in poolables)
                {
                    poolable.Reset();
                }
            }

            // Disable the GameObject if configured to do so
            if (DisableOnRelease)
            {
                gameObject.SetActive(false);
            }

            // Reattach to parent if needed
            if (UsesParentTransform && gameObject.transform.parent != _parent)
            {
                gameObject.transform.SetParent(_parent, false);
            }

            // Add to inactive objects
            _inactiveObjects.Add(gameObject);

            // Try auto-shrinking
            TryAutoShrink();
        }

        /// <summary>
        /// Releases multiple objects back to the pool
        /// </summary>
        /// <param name="items">Collection of objects to release</param>
        public void ReleaseMultiple(IEnumerable<GameObject> items)
        {
            ReleaseMultiple(items, null);
        }

        /// <summary>
        /// Releases multiple objects back to the pool
        /// </summary>
        /// <param name="items">Collection of objects to release</param>
        /// <param name="onRelease">Optional action to perform on each object when releasing</param>
        public void ReleaseMultiple(IEnumerable<GameObject> items, Action<GameObject> onRelease = null)
        {
            ThrowIfDisposed();

            if (items == null)
                throw new ArgumentNullException(nameof(items));

            foreach (var go in items)
            {
                if (go != null)
                {
                    if (onRelease != null)
                    {
                        onRelease(go);
                    }
                    Release(go);
                }
                else
                {
                    _logger?.LogWarningInstance("Attempted to release null GameObject in ReleaseMultiple");
                }
            }
        }

        /// <summary>
        /// Clears the pool by releasing all active GameObjects
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();

            // Create a temporary list to avoid collection modification during iteration
            var activeItems = new List<GameObject>(_activeObjects);
            
            foreach (var item in activeItems)
            {
                Release(item);
            }

            _logger?.LogInfoInstance($"Cleared all active objects in pool '{PoolName}'");
        }

        /// <summary>
        /// Ensures the pool has at least the specified capacity
        /// </summary>
        /// <param name="capacity">Required capacity</param>
        public void EnsureCapacity(int capacity)
        {
            ThrowIfDisposed();

            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive");

            int toCreate = capacity - TotalCount;
            if (toCreate > 0)
            {
                PrewarmPool(toCreate);
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

            // Update existing inactive objects to use the new parent
            foreach (var go in _inactiveObjects)
            {
                if (go != null)
                {
                    go.transform.SetParent(parent, false);
                }
            }
        }

        /// <summary>
        /// Attempts to shrink the pool's capacity to reduce memory usage
        /// </summary>
        /// <param name="threshold">Threshold factor determining when shrinking occurs</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool TryShrink(float threshold)
        {
            ThrowIfDisposed();

            if (!SupportsAutoShrink || _inactiveObjects.Count <= MinimumCapacity)
                return false;

            float usageRatio = (float)ActiveCount / TotalCount;
            if (usageRatio >= threshold)
                return false;

            int targetCapacity = Math.Max(MinimumCapacity, ActiveCount + (int)(ActiveCount * 0.5f));
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

            targetCapacity = Math.Max(ActiveCount, Math.Max(MinimumCapacity, targetCapacity));
            
            if (TotalCount <= targetCapacity)
                return false;

            int excessCount = TotalCount - targetCapacity;
            int destroyCount = Math.Min(excessCount, _inactiveObjects.Count);
            
            if (destroyCount <= 0)
                return false;

            for (int i = 0; i < destroyCount; i++)
            {
                int lastIndex = _inactiveObjects.Count - 1;
                GameObject go = _inactiveObjects[lastIndex];
                _inactiveObjects.RemoveAt(lastIndex);
                
                if (go != null)
                {
                    UnityEngine.Object.Destroy(go);
                }
            }

            _logger?.LogInfoInstance($"Shrunk pool '{PoolName}' by destroying {destroyCount} inactive objects");
            return true;
        }

        /// <summary>
        /// Sets the prefab used to create new instances when the pool needs to grow
        /// </summary>
        /// <param name="prefab">The prefab to use</param>
        public void SetPrefab(GameObject prefab)
        {
            ThrowIfDisposed();

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            // Only allow changing prefab if pool is empty or we're in editor mode
            if (TotalCount > 0 && Application.isPlaying)
            {
                _logger?.LogWarningInstance($"Changing prefab for non-empty pool {PoolName}. This may cause inconsistencies.");
            }

            _currentPrefab = prefab;
        }

        /// <summary>
        /// Gets the original prefab this pool is using to create instances
        /// </summary>
        /// <returns>The original prefab</returns>
        public GameObject GetPrefab()
        {
            ThrowIfDisposed();
            return _currentPrefab != null ? _currentPrefab : _prefab;
        }

        /// <summary>
        /// Creates a new instance of the pooled object type
        /// </summary>
        /// <returns>A new instance</returns>
        public GameObject CreateNewInstance()
        {
            ThrowIfDisposed();

            GameObject prefabToUse = _currentPrefab != null ? _currentPrefab : _prefab;
            GameObject instance;

            if (_parent != null)
            {
                instance = UnityEngine.Object.Instantiate(prefabToUse, _parent);
            }
            else
            {
                instance = UnityEngine.Object.Instantiate(prefabToUse);
            }

            // Disable initially
            instance.SetActive(false);

            return instance;
        }

        /// <summary>
        /// Explicitly enables or disables automatic shrinking
        /// </summary>
        /// <param name="enabled">Whether automatic shrinking should be enabled</param>
        public void SetAutoShrink(bool enabled)
        {
            ThrowIfDisposed();
            
            if (_config != null)
            {
                _config.EnableAutoShrink = enabled;
            }
        }

        /// <summary>
        /// Attempts to reduce fragmentation in the pool
        /// </summary>
        /// <returns>True if defragmentation was performed, false otherwise</returns>
        public bool TryDefragment()
        {
            ThrowIfDisposed();
            
            // Nothing to defragment in this implementation
            return false;
        }

        /// <summary>
        /// Checks if a specific object instance belongs to this pool
        /// </summary>
        /// <param name="objectInstance">The object to check</param>
        /// <returns>True if the object belongs to this pool, false otherwise</returns>
        public bool ContainsInstance(UnityEngine.Object objectInstance)
        {
            ThrowIfDisposed();
            
            if (objectInstance == null)
                return false;

            GameObject go = objectInstance as GameObject;
            if (go != null)
            {
                return _activeObjects.Contains(go) || _inactiveObjects.Contains(go);
            }

            Component comp = objectInstance as Component;
            if (comp != null && comp.gameObject != null)
            {
                return _activeObjects.Contains(comp.gameObject) || _inactiveObjects.Contains(comp.gameObject);
            }

            return false;
        }

        /// <summary>
        /// Gets metrics for this pool
        /// </summary>
        /// <returns>Dictionary of pool metrics</returns>
        public Dictionary<string, object> GetMetrics()
        {
            ThrowIfDisposed();
            
            return new Dictionary<string, object>
            {
                { "PoolName", PoolName },
                { "ItemType", ItemType.Name },
                { "TotalCount", TotalCount },
                { "ActiveCount", ActiveCount },
                { "InactiveCount", InactiveCount },
                { "PeakUsage", PeakUsage },
                { "TotalCreated", TotalCreated },
                { "TotalAcquired", TotalAcquiredCount },
                { "TotalReleased", TotalReleasedCount },
                { "UsageRatio", TotalCount > 0 ? (float)ActiveCount / TotalCount : 0f },
                { "SupportsAutoShrink", SupportsAutoShrink },
                { "MinimumCapacity", MinimumCapacity },
                { "MaximumCapacity", MaximumCapacity },
                { "ResetOnRelease", ResetOnRelease },
                { "DisableOnRelease", DisableOnRelease },
                { "UsesParentTransform", UsesParentTransform }
            };
        }

        /// <summary>
        /// Gets Unity-specific metrics for this pool
        /// </summary>
        /// <returns>Dictionary of Unity-specific metrics</returns>
        public Dictionary<string, object> GetUnityMetrics()
        {
            ThrowIfDisposed();
            
            return new Dictionary<string, object>
            {
                { "PrefabName", GetPrefab()?.name ?? "None" },
                { "ParentName", ParentTransform?.name ?? "None" },
                { "DisableOnRelease", DisableOnRelease },
                { "ResetOnRelease", ResetOnRelease }
            };
        }

        /// <summary>
        /// Resets the performance metrics of the pool
        /// </summary>
        public void ResetMetrics()
        {
            ThrowIfDisposed();
            
            _peakActiveCount = _activeObjects.Count;
            _totalAcquiredCount = 0;
            _totalReleasedCount = 0;
            
            _logger?.LogInfoInstance($"Reset metrics for pool '{PoolName}'");
        }

        
        /// <summary>
        /// Disposes the pool and destroys all pooled objects
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            // Unregister from registry if available
            PoolingServices.TryGetService<PoolRegistry>(out var registry);
            registry?.UnregisterPool(this);

            // Destroy all active and inactive objects
            foreach (var go in _activeObjects)
            {
                if (go != null)
                {
                    UnityEngine.Object.Destroy(go);
                }
            }

            foreach (var go in _inactiveObjects)
            {
                if (go != null)
                {
                    UnityEngine.Object.Destroy(go);
                }
            }

            _activeObjects.Clear();
            _inactiveObjects.Clear();
            _isDisposed = true;

            _logger?.LogInfoInstance($"Disposed GameObjectPool '{PoolName}'");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Throws an exception if the pool has been disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(PoolName, "Cannot access a disposed pool");
            }
        }

        #endregion
    }
}