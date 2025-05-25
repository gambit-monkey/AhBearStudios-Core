using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;
using AhBearStudios.Pooling.Pools.Unity;
using AhBearStudios.Pooling.Configurations; // Add this for ParticleSystemPoolConfig
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Pooling.Unity;
using UnityEngine;
using Unity.Collections; // Add this for Allocator enum

namespace AhBearStudios.Pooling.Pools.Unity
{
    /// <summary>
    /// A specialized pool for ParticleSystem components, providing efficient management of
    /// particle effects with built-in functionality for playing, duration tracking, and automatic release.
    /// </summary>
    public class ParticleSystemPool : IParticleSystemPool<ParticleSystem>, IDisposable
    {
        #region Private Fields

        private ParticleSystem _prefab;
        private readonly List<ParticleSystem> _inactiveItems = new List<ParticleSystem>();
        private readonly HashSet<ParticleSystem> _activeItems = new HashSet<ParticleSystem>();
        private readonly object _syncRoot = new object();
        private Transform _parent;
        private readonly IPoolDiagnostics _diagnostics;
        private readonly PoolLogger _logger;
        private readonly ParticleSystemPoolConfig _config;
        private bool _isDisposed;
        private bool _isCreated;
        private string _poolName;
        private float _lastShrinkTime;
        private bool _autoShrinkEnabled;
        private bool _isInitialized;
        private readonly Dictionary<ParticleSystem, int> _autoReleaseCoroutines = new Dictionary<ParticleSystem, int>();
        
        // Metrics
        private int _peakActiveCount;
        private int _totalCreated;
        private int _totalAcquired;
        private int _totalReleased;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ParticleSystemPool class.
        /// </summary>
        /// <param name="prefab">The particle system prefab to use for instantiation</param>
        /// <param name="parent">Optional parent transform for pooled objects</param>
        /// <param name="config">Configuration for the pool</param>
        /// <param name="poolName">Optional custom name for the pool</param>
        public ParticleSystemPool(
            ParticleSystem prefab,
            Transform parent = null,
            ParticleSystemPoolConfig config = null,
            string poolName = null) 
        {
            // Use the provided config or create a default one
            _config = config ?? new ParticleSystemPoolConfig();
            Initialize(prefab, parent, config, poolName);
        }

        /// <summary>
        /// Initializes a new instance of the ParticleSystemPool class with diagnostics.
        /// </summary>
        /// <param name="prefab">The particle system prefab to use for instantiation</param>
        /// <param name="parent">Optional parent transform for pooled objects</param>
        /// <param name="config">Configuration for the pool</param>
        /// <param name="diagnostics">Diagnostics service for tracking pool operations</param>
        /// <param name="logger">Logger for recording pool activities</param>
        /// <param name="poolName">Optional custom name for the pool</param>
        public ParticleSystemPool(
            ParticleSystem prefab,
            Transform parent,
            ParticleSystemPoolConfig config,
            IPoolDiagnostics diagnostics,
            PoolLogger logger,
            string poolName = null)
        {
            _diagnostics = diagnostics;
            _logger = logger;
            // Use the provided config or create a default one
            _config = config ?? new ParticleSystemPoolConfig();
            
            Initialize(prefab, parent, config, poolName);
        }

        private void Initialize(
            ParticleSystem prefab,
            Transform parent,
            ParticleSystemPoolConfig config,
            string poolName)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            _prefab = prefab;
            _parent = parent;
            _poolName = string.IsNullOrEmpty(poolName) ? $"ParticleSystemPool_{prefab.name}" : poolName;
            
            // Initialize based on config settings
            _autoShrinkEnabled = _config.EnableAutoShrink;
            
            // Set parent transform based on config
            if (_config.UseParentTransform && _parent == null)
            {
                _parent = _config.ParentTransform;
            }
            
            // Prewarm if needed
            if (_config.PrewarmOnInit && _config.InitialCapacity > 0)
            {
                PrewarmPool(_config.InitialCapacity);
            }
            
            _lastShrinkTime = Time.realtimeSinceStartup;
            _isCreated = true;
            _isInitialized = true;
            
            if (_config.DetailedLogging)
            {
                _logger?.LogInfoInstance($"ParticleSystemPool '{_poolName}' initialized with prefab '{_prefab.name}'");
            }
        }

        #endregion

        #region IPool Properties

        /// <summary>
        /// Gets whether the pool has been created and is ready for use.
        /// </summary>
        public bool IsCreated => _isCreated && !_isDisposed && _prefab != null;

        /// <summary>
        /// Gets whether the pool has been disposed.
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Gets the total number of items in the pool (active + inactive).
        /// </summary>
        public int TotalCount => _activeItems.Count + _inactiveItems.Count;

        /// <summary>
        /// Gets the current number of active (acquired) items.
        /// </summary>
        public int ActiveCount => _activeItems.Count;

        /// <summary>
        /// Gets the current number of inactive (available) items.
        /// </summary>
        public int InactiveCount => _inactiveItems.Count;

        /// <summary>
        /// Gets the type of items this pool manages.
        /// </summary>
        public Type ItemType => typeof(ParticleSystem);

        /// <summary>
        /// Gets the name of this pool.
        /// </summary>
        public string PoolName => _poolName;

        /// <summary>
        /// Gets the peak number of items in use at one time.
        /// </summary>
        public int PeakUsage => _peakActiveCount;

        /// <summary>
        /// Gets the total number of items created by this pool.
        /// </summary>
        public int TotalCreated => _totalCreated;

        #endregion

        #region IParticlePoolSystem Properties

        /// <summary>
        /// Gets the current capacity of the pool (total number of items).
        /// </summary>
        public int Capacity => TotalCount;

        /// <summary>
        /// Gets the parent transform for pooled objects.
        /// </summary>
        public Transform ParentTransform => _parent;

        /// <summary>
        /// Gets whether this pool is using a parent transform.
        /// </summary>
        public bool UsesParentTransform => _parent != null;

        /// <summary>
        /// Gets whether objects from this pool should be reset when released.
        /// </summary>
        public bool ResetOnRelease => _config.ResetOnRelease;

        /// <summary>
        /// Gets whether objects from this pool should be disabled when released.
        /// </summary>
        public bool DisableOnRelease => _config.UseParentTransform; // Maps to UseParentTransform in the updated config

        #endregion

        #region IPoolMetrics Properties

        /// <summary>
        /// Gets the peak number of active items the pool has ever had.
        /// </summary>
        public int PeakActiveCount => _peakActiveCount;

        /// <summary>
        /// Gets the total number of instances created by this pool.
        /// </summary>
        public int TotalCreatedCount => _totalCreated;

        /// <summary>
        /// Gets the total number of acquisitions performed by this pool.
        /// </summary>
        public int TotalAcquiredCount => _totalAcquired;

        /// <summary>
        /// Gets the total number of releases performed by this pool.
        /// </summary>
        public int TotalReleasedCount => _totalReleased;

        /// <summary>
        /// Gets the current number of active (acquired) items.
        /// </summary>
        public int CurrentActiveCount => _activeItems.Count;

        /// <summary>
        /// Gets the current capacity of the pool.
        /// </summary>
        public int CurrentCapacity => TotalCount;

        #endregion

        #region IShrinkablePool Properties

        /// <summary>
        /// Gets whether this pool supports automatic shrinking.
        /// </summary>
        public bool SupportsAutoShrink => true;

        /// <summary>
        /// Gets or sets the minimum capacity the pool can be shrunk to.
        /// </summary>
        public int MinimumCapacity 
        { 
            get => _config.InitialCapacity; // Use initial capacity as minimum in updated config
            set { /* Read-only in our implementation */ }
        }

        /// <summary>
        /// Gets or sets the maximum capacity the pool can grow to.
        /// </summary>
        public int MaximumCapacity
        {
            get => _config.MaximumCapacity;
            set { /* Read-only in our implementation */ }
        }

        /// <summary>
        /// Gets or sets the interval in seconds between automatic shrink operations.
        /// </summary>
        public float ShrinkInterval
        {
            get => _config.ShrinkInterval;
            set { /* Read-only in our implementation */ }
        }

        /// <summary>
        /// Gets or sets the growth factor for pool expansion.
        /// </summary>
        public float GrowthFactor
        {
            get => _config.GrowthFactor;
            set { /* Read-only in our implementation */ }
        }

        /// <summary>
        /// Gets or sets the threshold ratio of unused items before the pool will shrink.
        /// </summary>
        public float ShrinkThreshold
        {
            get => _config.ShrinkThreshold;
            set { /* Read-only in our implementation */ }
        }

        #endregion

        #region IPool Methods

        /// <summary>
        /// Acquires a particle system from the pool.
        /// </summary>
        /// <returns>A particle system instance</returns>
        public ParticleSystem Acquire()
        {
            ThrowIfDisposed();

            // Handle thread safety based on the configured mode
            bool needsLocking = _config.ThreadingMode == PoolThreadingMode.ThreadSafe;
            
            if (needsLocking)
            {
                lock (_syncRoot)
                {
                    return AcquireInternal();
                }
            }
            else
            {
                return AcquireInternal();
            }
        }
        
        private ParticleSystem AcquireInternal()
        {
            ParticleSystem item;

            // Get an inactive item or create a new one
            if (_inactiveItems.Count > 0)
            {
                int lastIndex = _inactiveItems.Count - 1;
                item = _inactiveItems[lastIndex];
                _inactiveItems.RemoveAt(lastIndex);
            }
            else
            {
                // Create a new item if we're below max capacity or have no max capacity
                if (_config.MaximumCapacity <= 0 || TotalCount < _config.MaximumCapacity)
                {
                    item = CreateNewInstance();
                }
                else if (_config.ThrowIfExceedingMaxCount)
                {
                    throw new InvalidOperationException(
                        $"Pool '{PoolName}' has reached its maximum capacity of {_config.MaximumCapacity} and cannot create more items.");
                }
                else
                {
                    // Reuse the oldest inactive item if available, otherwise return null
                    if (_inactiveItems.Count > 0)
                    {
                        item = _inactiveItems[0];
                        _inactiveItems.RemoveAt(0);
                    }
                    else
                    {
                        if (_config.LogWarnings)
                        {
                            _logger?.LogWarningInstance($"Pool '{PoolName}' has reached maximum capacity with no available items.");
                        }
                        return null;
                    }
                }
            }

            // Prepare the item for use
            if (item != null)
            {
                if (!item.gameObject.activeSelf)
                {
                    item.gameObject.SetActive(true);
                }

                _activeItems.Add(item);
                _totalAcquired++;
                _peakActiveCount = Math.Max(_peakActiveCount, _activeItems.Count);
                
                if (_config.CollectMetrics)
                {
                    _diagnostics?.RecordCreate(this);
                }
                
                if (_config.DetailedLogging)
                {
                    _logger?.LogDebugInstance($"Acquired particle system from pool '{PoolName}'");
                }
                
                return item;
            }
            
            throw new InvalidOperationException($"Failed to acquire a particle system from pool '{PoolName}'");
        }

        /// <summary>
        /// Acquires a particle system and applies a setup action to it.
        /// </summary>
        /// <param name="setupAction">Action to configure the particle system</param>
        /// <returns>The configured particle system</returns>
        public ParticleSystem AcquireAndSetup(Action<ParticleSystem> setupAction)
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));
                
            ParticleSystem item = Acquire();
            
            if (item != null)
            {
                setupAction(item);
            }
            
            return item;
        }

        /// <summary>
        /// Acquires multiple particle systems from the pool.
        /// </summary>
        /// <param name="count">Number of particle systems to acquire</param>
        /// <returns>List of acquired particle systems</returns>
        public List<ParticleSystem> AcquireMultiple(int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero");
                
            ThrowIfDisposed();
            
            List<ParticleSystem> result = new List<ParticleSystem>(count);
            for (int i = 0; i < count; i++)
            {
                var item = Acquire();
                if (item != null)
                {
                    result.Add(item);
                }
                else
                {
                    // If we couldn't get all requested items, log a warning
                    if (_config.LogWarnings)
                    {
                        _logger?.LogWarningInstance($"Could only acquire {i} of {count} requested items from pool '{PoolName}'");
                    }
                    break;
                }
            }
            
            return result;
        }

        /// <summary>
        /// Releases a particle system back to the pool.
        /// </summary>
        /// <param name="item">The particle system to release</param>
        public void Release(ParticleSystem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
                
            ThrowIfDisposed();
            
            // Handle thread safety based on the configured mode
            bool needsLocking = _config.ThreadingMode == PoolThreadingMode.ThreadSafe;
            
            if (needsLocking)
            {
                lock (_syncRoot)
                {
                    ReleaseInternal(item);
                }
            }
            else
            {
                ReleaseInternal(item);
            }
        }
        
        private void ReleaseInternal(ParticleSystem item)
        {
            // Verify the item is from this pool
            if (!_activeItems.Remove(item))
            {
                if (_config.LogWarnings)
                {
                    _logger?.LogWarningInstance($"Attempted to release a particle system that was not acquired from pool '{PoolName}'");
                }
                return;
            }
            
            // Cancel any auto-release coroutines
            if (_autoReleaseCoroutines.TryGetValue(item, out int coroutineId))
            {
                PoolCoroutineRunner.Instance.CancelCoroutine(coroutineId);
                _autoReleaseCoroutines.Remove(item);
            }
            
            // Reset the particle system
            if (_config.ResetOnRelease)
            {
                if (_config.StopOnRelease)
                {
                    item.Stop(true);
                }
                
                if (_config.ClearParticlesOnRelease)
                {
                    item.Clear(true);
                }
                
                if (_config.DisableEmissionOnRelease)
                {
                    var emission = item.emission;
                    emission.enabled = false;
                }
                
                // Reset transform if parented
                if (_parent != null)
                {
                    item.transform.SetParent(_parent, false);
                    item.transform.localPosition = Vector3.zero;
                    item.transform.localRotation = Quaternion.identity;
                    item.transform.localScale = Vector3.one;
                }
            }
            
            // Disable if configured to do so
            if (_config.UseParentTransform) // This replaces DisableOnRelease
            {
                item.gameObject.SetActive(false);
            }
            
            // Return to inactive pool
            _inactiveItems.Add(item);
            _totalReleased++;
            
            if (_config.CollectMetrics)
            {
                _diagnostics?.RecordRelease(this, item);
            }
            
            if (_config.DetailedLogging)
            {
                _logger?.LogDebugInstance($"Released particle system back to pool '{PoolName}'");
            }
            
            // Check for auto-shrink
            TryAutoShrink();
        }

        /// <summary>
        /// Releases multiple particle systems back to the pool.
        /// </summary>
        /// <param name="items">Collection of particle systems to release</param>
        public void ReleaseMultiple(IEnumerable<ParticleSystem> items)
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
        /// Ensures the pool has at least the specified capacity.
        /// </summary>
        /// <param name="capacity">The minimum capacity required</param>
        public void EnsureCapacity(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative");
                
            ThrowIfDisposed();
            
            // Handle thread safety based on the configured mode
            bool needsLocking = _config.ThreadingMode == PoolThreadingMode.ThreadSafe;
            
            if (needsLocking)
            {
                lock (_syncRoot)
                {
                    EnsureCapacityInternal(capacity);
                }
            }
            else
            {
                EnsureCapacityInternal(capacity);
            }
        }
        
        private void EnsureCapacityInternal(int capacity)
        {
            // Check if we need to grow
            int currentCapacity = TotalCount;
            int needed = capacity - currentCapacity;
            
            if (needed <= 0)
                return;
                
            // Respect maximum capacity if set
            if (_config.MaximumCapacity > 0)
            {
                needed = Math.Min(needed, _config.MaximumCapacity - currentCapacity);
                
                if (needed <= 0)
                {
                    if (_config.LogWarnings)
                    {
                        _logger?.LogWarningInstance($"Cannot grow pool '{PoolName}' to capacity {capacity} as it would exceed maximum capacity of {_config.MaximumCapacity}");
                    }
                    return;
                }
            }
            
            if (_config.DetailedLogging)
            {
                _logger?.LogInfoInstance($"Growing pool '{PoolName}' by {needed} items to ensure capacity of {capacity}");
            }
            
            for (int i = 0; i < needed; i++)
            {
                ParticleSystem newItem = CreateNewInstance();
                
                if (_config.UseParentTransform)
                {
                    newItem.gameObject.SetActive(false);
                }
                
                _inactiveItems.Add(newItem);
            }
        }

        /// <summary>
        /// Clears all pooled objects, releasing resources.
        /// </summary>
        public void Clear()
        {
            if (_isDisposed)
                return;
                
            // Handle thread safety based on the configured mode
            bool needsLocking = _config.ThreadingMode == PoolThreadingMode.ThreadSafe;
            
            if (needsLocking)
            {
                lock (_syncRoot)
                {
                    ClearInternal();
                }
            }
            else
            {
                ClearInternal();
            }
        }
        
        private void ClearInternal()
        {
            if (_config.DetailedLogging)
            {
                _logger?.LogInfoInstance($"Clearing pool '{PoolName}'");
            }
            
            // Destroy inactive items
            foreach (var item in _inactiveItems)
            {
                if (item != null)
                {
                    UnityEngine.Object.Destroy(item.gameObject);
                }
            }
            _inactiveItems.Clear();
            
            // Log warning about active items
            if (_activeItems.Count > 0 && _config.LogWarnings)
            {
                _logger?.LogWarningInstance($"Pool '{PoolName}' has {_activeItems.Count} active items that will not be destroyed by Clear()");
            }
            
            // Reset metrics, but keep active items tracked
            _totalCreated = _activeItems.Count;
            _totalAcquired = _activeItems.Count;
            _totalReleased = 0;
            _peakActiveCount = _activeItems.Count;
        }

        /// <summary>
        /// Disposes of the pool, releasing all resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            // Handle thread safety based on the configured mode
            bool needsLocking = _config.ThreadingMode == PoolThreadingMode.ThreadSafe;
            
            if (needsLocking)
            {
                lock (_syncRoot)
                {
                    DisposeInternal();
                }
            }
            else
            {
                DisposeInternal();
            }
        }
        
        private void DisposeInternal()
        {
            if (_config.DetailedLogging)
            {
                _logger?.LogInfoInstance($"Disposing pool '{PoolName}'");
            }
            
            // Destroy all inactive items
            foreach (var item in _inactiveItems)
            {
                if (item != null)
                {
                    UnityEngine.Object.Destroy(item.gameObject);
                }
            }
            _inactiveItems.Clear();
            
            // Try to destroy active items too
            foreach (var item in _activeItems)
            {
                if (item != null)
                {
                    if (_config.LogWarnings)
                    {
                        _logger?.LogWarningInstance($"Destroying active particle system during pool disposal: {item.name}");
                    }
                    UnityEngine.Object.Destroy(item.gameObject);
                }
            }
            _activeItems.Clear();
            
            // Clean up coroutines
            foreach (var coroutineId in _autoReleaseCoroutines.Values)
            {
                PoolCoroutineRunner.Instance.CancelCoroutine(coroutineId);
            }
            _autoReleaseCoroutines.Clear();
            
            _isDisposed = true;
            _isCreated = false;
            _prefab = null;
            _parent = null;
        }

        #endregion

        #region IParticlePoolSystem Methods

        /// <summary>
        /// Sets the parent transform for pooled objects.
        /// </summary>
        /// <param name="parentTransform">The parent transform to use</param>
        public void SetParentTransform(Transform parentTransform)
        {
            ThrowIfDisposed();
            
            _parent = parentTransform;
            
            // Reparent all inactive items
            foreach (var item in _inactiveItems)
            {
                if (item != null)
                {
                    item.transform.SetParent(_parent, false);
                    item.transform.localPosition = Vector3.zero;
                    item.transform.localRotation = Quaternion.identity;
                    item.transform.localScale = Vector3.one;
                }
            }
            
            if (_config.DetailedLogging)
            {
                _logger?.LogInfoInstance($"Set parent transform for pool '{PoolName}' to '{_parent?.name ?? "null"}'");
            }
        }

        /// <summary>
        /// Prewarms the pool by creating a specified number of instances.
        /// </summary>
        /// <param name="count">Number of instances to create</param>
        public void PrewarmPool(int count)
        {
            if (count <= 0)
                return;
                
            ThrowIfDisposed();
            
            // Check maximum capacity
            if (_config.MaximumCapacity > 0)
            {
                int available = _config.MaximumCapacity - TotalCount;
                if (available < count)
                {
                    if (_config.LogWarnings)
                    {
                        _logger?.LogWarningInstance($"Prewarming pool '{PoolName}' with {available} instead of requested {count} due to maximum capacity of {_config.MaximumCapacity}");
                    }
                    count = available;
                    
                    if (count <= 0)
                        return;
                }
            }
            
            if (_config.DetailedLogging)
            {
                _logger?.LogInfoInstance($"Prewarming pool '{PoolName}' with {count} particles");
            }
            
            for (int i = 0; i < count; i++)
            {
                ParticleSystem item = CreateNewInstance();
                
                if (_config.UseParentTransform)
                {
                    item.gameObject.SetActive(false);
                }
                
                // Prewarm the particles if configured to do so
                if (_config.PrewarmParticles)
                {
                    item.Simulate(0.1f, true, true);
                    item.Play(true);
                    item.Stop(true);
                }
                
                _inactiveItems.Add(item);
            }
        }

        /// <summary>
        /// Attempts to reduce fragmentation in the pool.
        /// </summary>
        /// <returns>True if defragmentation was performed, false otherwise</returns>
        public bool TryDefragment()
        {
            // For this implementation, defragmentation isn't applicable
            // as we're using a List and HashSet for storage
            return false;
        }

        /// <summary>
        /// Checks if a specific instance belongs to this pool.
        /// </summary>
        /// <param name="objectInstance">The object to check</param>
        /// <returns>True if the object belongs to this pool, false otherwise</returns>
        public bool ContainsInstance(UnityEngine.Object objectInstance)
        {
            if (objectInstance == null)
                return false;
                
            if (objectInstance is ParticleSystem ps)
            {
                return _activeItems.Contains(ps) || _inactiveItems.Contains(ps);
            }
            
            if (objectInstance is GameObject go)
            {
                ParticleSystem component = go.GetComponent<ParticleSystem>();
                if (component != null)
                {
                    return _activeItems.Contains(component) || _inactiveItems.Contains(component);
                }
            }
            
            return false;
        }

        /// <summary>
        /// Gets metrics about the pool's operation.
        /// </summary>
        /// <returns>Dictionary of metric names and values</returns>
        public Dictionary<string, object> GetMetrics()
        {
            var metrics = new Dictionary<string, object>
            {
                ["PoolName"] = PoolName,
                ["ItemType"] = ItemType.Name,
                ["IsDisposed"] = IsDisposed,
                ["ActiveCount"] = ActiveCount,
                ["InactiveCount"] = InactiveCount,
                ["TotalCount"] = TotalCount,
                ["PeakActiveCount"] = PeakActiveCount,
                ["TotalCreated"] = TotalCreated,
                ["TotalAcquired"] = TotalAcquiredCount,
                ["TotalReleased"] = TotalReleasedCount,
                ["MinimumCapacity"] = MinimumCapacity,
                ["MaximumCapacity"] = MaximumCapacity,
                ["AutoShrinkEnabled"] = _autoShrinkEnabled,
                ["ShrinkThreshold"] = ShrinkThreshold,
                ["ShrinkInterval"] = ShrinkInterval,
                ["GrowthFactor"] = GrowthFactor,
                ["ConfigId"] = _config.ConfigId
            };
            
            return metrics;
        }

        /// <summary>
        /// Gets Unity-specific metrics for this pool.
        /// </summary>
        /// <returns>Dictionary of metric names and values</returns>
        public Dictionary<string, object> GetUnityMetrics()
        {
            var metrics = new Dictionary<string, object>
            {
                ["PrefabName"] = _prefab?.name ?? "Unknown",
                ["UseParentTransform"] = _config.UseParentTransform,
                ["ResetOnRelease"] = _config.ResetOnRelease,
                ["HasParent"] = UsesParentTransform,
                ["ParentName"] = _parent?.name ?? "None",
                ["ThreadingMode"] = _config.ThreadingMode.ToString(),
                ["StopOnRelease"] = _config.StopOnRelease,
                ["ClearParticlesOnRelease"] = _config.ClearParticlesOnRelease,
                ["DisableEmissionOnRelease"] = _config.DisableEmissionOnRelease,
                ["CacheComponents"] = _config.CacheComponents,
                ["OptimizeForOneShot"] = _config.OptimizeForOneShot
            };
            
            return metrics;
        }
        
        /// <summary>
        /// Destroys a ParticleSystem instance, removing it from the pool permanently
        /// </summary>
        /// <param name="item">ParticleSystem to destroy</param>
        public void DestroyItem(ParticleSystem item)
        {
            if (item == null)
                return;

            // Remove from tracking collections
            _activeItems.Remove(item);
            _inactiveItems.Remove(item);

            // Cancel any auto-release coroutines
            if (_autoReleaseCoroutines.TryGetValue(item, out int coroutineId))
            {
                PoolCoroutineRunner.Instance.CancelCoroutine(coroutineId);
                _autoReleaseCoroutines.Remove(item);
            }

            // Destroy the GameObject
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(item.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(item.gameObject);
            }
        }


        #endregion

        #region IParticlePoolSystem<ParticleSystem> Methods

        /// <summary>
        /// Acquires a particle system and positions it at the specified location.
        /// </summary>
        /// <param name="position">World position</param>
        /// <param name="rotation">World rotation</param>
        /// <returns>The positioned particle system</returns>
        public ParticleSystem Acquire(Vector3 position, Quaternion rotation)
        {
            ParticleSystem item = Acquire();
            
            if (item == null)
                return null;
                
            // Set position and rotation
            item.transform.position = position;
            item.transform.rotation = rotation;
            
            return item;
        }

        /// <summary>
        /// Acquires a particle system and positions it at the specified transform.
        /// </summary>
        /// <param name="transform">Transform to match position and rotation</param>
        /// <returns>The positioned particle system</returns>
        public ParticleSystem AcquireAtTransform(Transform transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));
                
            return Acquire(transform.position, transform.rotation);
        }
        
                /// <summary>
        /// Releases multiple particle systems back to the pool with an optional action.
        /// </summary>
        /// <param name="items">Collection of particle systems to release</param>
        /// <param name="onRelease">Optional action to perform on each particle system before release</param>
        public void ReleaseMultiple(IEnumerable<ParticleSystem> items, Action<ParticleSystem> onRelease = null)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
                
            foreach (var item in items)
            {
                if (item != null)
                {
                    onRelease?.Invoke(item);
                    Release(item);
                }
            }
        }

        /// <summary>
        /// Creates a new instance of a particle system.
        /// </summary>
        /// <returns>A new particle system instance</returns>
        public ParticleSystem CreateNewInstance()
        {
            ThrowIfDisposed();
            
            if (_prefab == null)
                throw new InvalidOperationException($"Cannot create particle system instance in pool '{PoolName}' as prefab is null");
                
            ParticleSystem instance = UnityEngine.Object.Instantiate(_prefab, _parent);
            string instanceName = $"{_prefab.name}_Pooled_{_totalCreated}";
            instance.gameObject.name = instanceName;
            
            // Ensure it starts in a clean state
            instance.Stop(true);
            instance.Clear(true);
            
            // Apply optimizations if configured
            if (_config.OptimizeForOneShot)
            {
                var main = instance.main;
                main.stopAction = ParticleSystemStopAction.Callback;
            }
            
            // Cache components if configured
            if (_config.CacheComponents)
            {
                // GetComponent calls are cached internally by Unity
                var renderer = instance.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    // Just accessing it caches the component reference
                }
            }
            
            _totalCreated++;
            
            if (_config.CollectMetrics)
            {
                _diagnostics?.RecordCreate(this);
            }
            
            if (_config.DetailedLogging)
            {
                _logger?.LogDebugInstance($"Created new particle system instance '{instanceName}' in pool '{PoolName}'");
            }
            
            return instance;
        }

        /// <summary>
        /// Gets the prefab used by this pool.
        /// </summary>
        /// <returns>The particle system prefab</returns>
        public ParticleSystem GetPrefab()
        {
            ThrowIfDisposed();
            return _prefab;
        }

        /// <summary>
        /// Sets the prefab used by this pool.
        /// </summary>
        /// <param name="prefab">The new prefab to use</param>
        public void SetPrefab(ParticleSystem prefab)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
                
            ThrowIfDisposed();
            _prefab = prefab;
            
            if (_config.DetailedLogging)
            {
                _logger?.LogInfoInstance($"Set new prefab '{_prefab.name}' for pool '{PoolName}'");
            }
        }

        #endregion

        #region IShrinkablePool Methods

        /// <summary>
        /// Attempts to shrink the pool based on a usage threshold.
        /// </summary>
        /// <param name="threshold">The threshold (0-1) of unused items before shrinking</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool TryShrink(float threshold)
        {
            ThrowIfDisposed();
            
            if (threshold < 0 || threshold > 1)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be between 0 and 1");
                
            if (_inactiveItems.Count == 0 || TotalCount <= MinimumCapacity)
                return false;
                
            float unusedRatio = (float)_inactiveItems.Count / TotalCount;
            
            if (unusedRatio >= threshold)
            {
                // Calculate target capacity - keep active items plus some buffer
                int targetCapacity = Math.Max(MinimumCapacity, _activeItems.Count + 
                                                            (int)(_activeItems.Count * 0.2f)); // 20% buffer
                
                return ShrinkTo(targetCapacity);
            }
            
            return false;
        }

        /// <summary>
        /// Shrinks the pool to the specified target capacity.
        /// </summary>
        /// <param name="targetCapacity">The desired capacity</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool ShrinkTo(int targetCapacity)
        {
            ThrowIfDisposed();
            
            // Handle thread safety based on the configured mode
            bool needsLocking = _config.ThreadingMode == PoolThreadingMode.ThreadSafe;
            
            if (needsLocking)
            {
                lock (_syncRoot)
                {
                    return ShrinkToInternal(targetCapacity);
                }
            }
            else
            {
                return ShrinkToInternal(targetCapacity);
            }
        }
        
        private bool ShrinkToInternal(int targetCapacity)
        {
            targetCapacity = Math.Max(targetCapacity, MinimumCapacity);
            int currentCapacity = TotalCount;
            
            if (currentCapacity <= targetCapacity || _inactiveItems.Count == 0)
                return false;
                
            int itemsToRemove = Math.Min(_inactiveItems.Count, currentCapacity - targetCapacity);
            
            if (itemsToRemove <= 0)
                return false;
                
            if (_config.DetailedLogging)
            {
                _logger?.LogInfoInstance($"Shrinking pool '{PoolName}' by removing {itemsToRemove} items");
            }
            
            // Remove items from the end of the list (most recently added)
            for (int i = 0; i < itemsToRemove; i++)
            {
                int index = _inactiveItems.Count - 1;
                var item = _inactiveItems[index];
                _inactiveItems.RemoveAt(index);
                
                if (item != null)
                {
                    UnityEngine.Object.Destroy(item.gameObject);
                }
            }
            
            return true;
        }

        /// <summary>
        /// Enables or disables automatic shrinking of the pool.
        /// </summary>
        /// <param name="enabled">Whether auto-shrink should be enabled</param>
        public void SetAutoShrink(bool enabled)
        {
            _autoShrinkEnabled = enabled;
            _lastShrinkTime = Time.realtimeSinceStartup;
            
            if (_config.DetailedLogging)
            {
                _logger?.LogInfoInstance($"{(enabled ? "Enabled" : "Disabled")} auto-shrink for pool '{PoolName}'");
            }
        }

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

        #endregion

        #region IPoolMetrics Methods

        /// <summary>
        /// Resets all usage metrics for the pool.
        /// </summary>
        public void ResetMetrics()
        {
            _peakActiveCount = _activeItems.Count;
            _totalAcquired = _activeItems.Count;
            _totalCreated = TotalCount;
            _totalReleased = 0;
            
            if (_config.DetailedLogging)
            {
                _logger?.LogInfoInstance($"Reset metrics for pool '{PoolName}'");
            }
        }

        #endregion

        #region ParticleSystem Specific Methods

        /// <summary>
        /// Plays a particle system at the specified location.
        /// </summary>
        /// <param name="position">World position</param>
        /// <param name="rotation">World rotation</param>
        /// <returns>The playing particle system</returns>
        public ParticleSystem PlayAt(Vector3 position, Quaternion rotation)
        {
            ParticleSystem particleSystem = Acquire(position, rotation);
            
            if (particleSystem != null)
            {
                particleSystem.Play(true);
            }
            
            return particleSystem;
        }

        /// <summary>
        /// Plays a particle system at the specified transform.
        /// </summary>
        /// <param name="transform">Transform to match</param>
        /// <returns>The playing particle system</returns>
        public ParticleSystem PlayAtTransform(Transform transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));
                
            return PlayAt(transform.position, transform.rotation);
        }

        /// <summary>
        /// Plays a particle system and automatically releases it when complete.
        /// </summary>
        /// <param name="position">World position</param>
        /// <param name="rotation">World rotation</param>
        /// <param name="customLifetime">Optional custom lifetime in seconds (overrides calculated lifetime)</param>
        /// <returns>The playing particle system</returns>
        public ParticleSystem PlayAutoRelease(Vector3 position, Quaternion rotation, float? customLifetime = null)
        {
            ParticleSystem particleSystem = PlayAt(position, rotation);
            
            if (particleSystem != null)
            {
                StartAutoReleaseCoroutine(particleSystem, customLifetime);
            }
            
            return particleSystem;
        }

        /// <summary>
        /// Plays a particle system at a transform and automatically releases it when complete.
        /// </summary>
        /// <param name="transform">Transform to match</param>
        /// <param name="customLifetime">Optional custom lifetime in seconds (overrides calculated lifetime)</param>
        /// <returns>The playing particle system</returns>
        public ParticleSystem PlayAutoReleaseAtTransform(Transform transform, float? customLifetime = null)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));
                
            return PlayAutoRelease(transform.position, transform.rotation, customLifetime);
        }

        /// <summary>
        /// Plays a particle system with children and automatically releases it when all particles are gone.
        /// </summary>
        /// <param name="position">World position</param>
        /// <param name="rotation">World rotation</param>
        /// <returns>The playing particle system</returns>
        public ParticleSystem PlayWithChildrenAutoRelease(Vector3 position, Quaternion rotation)
        {
            ParticleSystem particleSystem = PlayAt(position, rotation);
            
            if (particleSystem != null)
            {
                // Start coroutine that will wait for all child particles to finish too
                StartWaitForChildrenCoroutine(particleSystem);
            }
            
            return particleSystem;
        }

        /// <summary>
        /// Starts an auto-release coroutine for a particle system.
        /// </summary>
        /// <param name="particleSystem">The particle system</param>
        /// <param name="customLifetime">Optional custom lifetime</param>
        private void StartAutoReleaseCoroutine(ParticleSystem particleSystem, float? customLifetime)
        {
            if (particleSystem == null)
                return;
                
            // Calculate duration if not provided
            float duration = customLifetime ?? CalculateParticleSystemDuration(particleSystem);
            
            // Add a small buffer to ensure all particles are gone
            duration += 0.5f;
            
            // Start the release coroutine
            int coroutineId = PoolCoroutineRunner.Instance.StartDelayedReleaseCoroutine(this, particleSystem, duration);
            
            // Store the coroutine ID for cancellation if needed
            if (_autoReleaseCoroutines.ContainsKey(particleSystem))
            {
                _autoReleaseCoroutines[particleSystem] = coroutineId;
            }
            else
            {
                _autoReleaseCoroutines.Add(particleSystem, coroutineId);
            }
        }

        /// <summary>
        /// Starts a coroutine that waits for all particles (including children) to complete.
        /// </summary>
        /// <param name="particleSystem">The root particle system</param>
        private void StartWaitForChildrenCoroutine(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
                return;
                
            int coroutineId = PoolCoroutineRunner.Instance.StartReleaseWhenCompleteCoroutine(this, particleSystem, true);
            
            // Store the coroutine ID for cancellation if needed
            if (_autoReleaseCoroutines.ContainsKey(particleSystem))
            {
                _autoReleaseCoroutines[particleSystem] = coroutineId;
            }
            else
            {
                _autoReleaseCoroutines.Add(particleSystem, coroutineId);
            }
        }
        
        
        // Add missing helper method for calculating particle system duration
        private float CalculateParticleSystemDuration(ParticleSystem ps)
        {
            if (ps == null)
                return 0f;
                
            var main = ps.main;
            float startLifetime = main.startLifetimeMultiplier;
            
            if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
                startLifetime = main.startLifetime.constant;
            else if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                startLifetime = main.startLifetime.constantMax;
                
            float duration = main.duration;
            bool isLooping = main.loop;
            
            // If the system is looping, use a fixed duration
            if (isLooping)
                return _config.DefaultLoopingEffectDuration;
                
            return duration + startLifetime + 0.5f; // Add small safety buffer
        }

        // Add missing helper method for auto-releasing
        private IEnumerator AutoReleaseAfterDelay(ParticleSystem ps, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (ps != null && _activeItems.Contains(ps))
            {
                Release(ps);
                _autoReleaseCoroutines.Remove(ps);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Throws an ObjectDisposedException if the pool has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(PoolName, "The particle system pool has been disposed");
        }

        #endregion
    }
}