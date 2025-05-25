using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;
using AhBearStudios.Pooling.Services;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace AhBearStudios.Pooling.Pools.Advanced
{
    /// <summary>
    /// A high-performance object pool implementation that supports advanced features like object prewarming,
    /// automatic shrinking, metrics collection, and lifecycle callbacks.
    /// Uses modern Unity Collections v2 for internal storage and supports Burst compilation.
    /// </summary>
    /// <typeparam name="T">Type of objects to pool</typeparam>
    public sealed class ComplexObjectPool<T> : IComplexObjectPool<T>
    {
        #region Private Fields

        private readonly Func<T> _objectFactory;
        private readonly Action<T> _initializeFunc;
        private readonly Action<T> _resetFunc;
        private readonly Func<T, bool> _validateFunc;
        private readonly string _poolName;
        private readonly Guid _id;
        private readonly IPoolingServiceLocator _serviceLocator;
        private readonly IPoolLogger _logger;
        private readonly IPoolProfiler _profiler;
        private readonly IPoolDiagnostics _diagnostics;
        private readonly IPoolMetrics _metrics;
        private readonly IPoolRegistry _registry;

        // Use Unity Collections v2 for better performance and Burst compatibility
        private NativeList<IntPtr> _inactiveItems;
        private NativeParallelHashMap<IntPtr, bool> _activeItems;
        private NativeParallelHashMap<IntPtr, NativeList<IntPtr>> _itemDependencies;

        // Keep these as managed collections since they don't need to be Burst-compatible
        private readonly Dictionary<IntPtr, Dictionary<string, object>> _itemProperties;

        private bool _isDisposed;
        private float _lastShrinkTime;
        private bool _autoShrinkEnabled;
        private int _minimumCapacity = 5;
        private int _maximumCapacity;
        private float _shrinkInterval = 60f;
        private float _growthFactor = 2.0f;
        private float _shrinkThreshold = 0.25f;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new complex object pool with dependency injection
        /// </summary>
        /// <param name="objectFactory">Factory function to create new objects</param>
        /// <param name="config">Optional pool configuration</param>
        /// <param name="serviceLocator">Service locator for pool services</param>
        /// <param name="initializeFunc">Optional function to initialize objects when first created</param>
        /// <param name="resetFunc">Optional function to reset objects when they are returned to the pool</param>
        /// <param name="validateFunc">Optional function to validate objects before they are returned from the pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <exception cref="ArgumentNullException">Thrown if objectFactory is null</exception>
        public ComplexObjectPool(
            Func<T> objectFactory,
            ComplexObjectPoolConfig config = null,
            IPoolingServiceLocator serviceLocator = null,
            Action<T> initializeFunc = null,
            Action<T> resetFunc = null,
            Func<T, bool> validateFunc = null,
            string poolName = null)
        {
            if (objectFactory == null)
                throw new ArgumentNullException(nameof(objectFactory));

            _objectFactory = objectFactory;
            _initializeFunc = initializeFunc;
            _resetFunc = resetFunc;
            _validateFunc = validateFunc;
            _poolName = poolName ?? typeof(T).Name + "Pool";
            _id = Guid.NewGuid();
            
            // Use dependency injection instead of static service locator
            _serviceLocator = serviceLocator ?? DefaultPoolingServices.Instance;
            _logger = _serviceLocator.GetService<IPoolLogger>();
            _profiler = _serviceLocator.GetService<IPoolProfiler>();
            _diagnostics = _serviceLocator.GetService<IPoolDiagnostics>();
            _metrics = _serviceLocator.GetService<IPoolMetrics>();
            _registry = _serviceLocator.GetService<IPoolRegistry>();

            // Initialize collections with Unity Collections v2
            var allocator = config?.NativeAllocator ?? Allocator.Persistent;

            _inactiveItems = new NativeList<IntPtr>(config?.InitialCapacity ?? 10, allocator);
            _activeItems = new NativeParallelHashMap<IntPtr, bool>(config?.InitialCapacity ?? 10, allocator);
            _itemDependencies =
                new NativeParallelHashMap<IntPtr, NativeList<IntPtr>>(config?.InitialCapacity ?? 10, allocator);
            _itemProperties = new Dictionary<IntPtr, Dictionary<string, object>>();

            // Register with diagnostics if available
            _diagnostics?.RegisterPool(this, _poolName);

            // Register with pool registry if available
            _registry?.RegisterPool(this);

            // Apply configuration
            if (config != null)
            {
                _minimumCapacity = config.MinimumCapacity;
                _maximumCapacity = config.MaximumCapacity;
                _autoShrinkEnabled = config.EnableAutoShrink;
                _shrinkThreshold = config.ShrinkThreshold;
                _shrinkInterval = config.ShrinkInterval;
                _growthFactor = config.GrowthFactor;

                // Prewarm if requested
                if (config.PrewarmOnInit)
                {
                    PrewarmPool(config.InitialCapacity);
                }
            }

            // Initialize shrink timer
            _lastShrinkTime = Time.realtimeSinceStartup;

            _logger?.LogInfoInstance($"Created pool '{_poolName}' with minimum capacity {_minimumCapacity}");
        }

        #endregion

        #region IPool Properties

        /// <inheritdoc />
        public bool IsCreated => !_isDisposed && _inactiveItems.IsCreated && _activeItems.IsCreated;

        public Guid Id => _id;

        /// <inheritdoc />
        public int TotalCount => _activeItems.Count() + _inactiveItems.Length;

        /// <inheritdoc />
        public int ActiveCount => _activeItems.Count();

        /// <inheritdoc />
        public int InactiveCount => _inactiveItems.Length;

        /// <inheritdoc />
        public int PeakUsage => _metrics.PeakActiveCount;

        /// <inheritdoc />
        public int TotalCreated => _metrics.TotalCreatedCount;

        /// <inheritdoc />
        public Type ItemType => typeof(T);

        /// <inheritdoc />
        public bool IsDisposed => _isDisposed;

        /// <inheritdoc />
        public string PoolName => _poolName;

        #endregion

        #region IShrinkablePool Properties

        /// <inheritdoc />
        public bool SupportsAutoShrink => true;

        /// <inheritdoc />
        public int MinimumCapacity
        {
            get => _minimumCapacity;
            set => _minimumCapacity = Math.Max(0, value);
        }

        /// <inheritdoc />
        public int MaximumCapacity
        {
            get => _maximumCapacity;
            set => _maximumCapacity = value > 0 ? value : 0;
        }

        /// <inheritdoc />
        public float ShrinkInterval
        {
            get => _shrinkInterval;
            set => _shrinkInterval = Math.Max(0, value);
        }

        /// <inheritdoc />
        public float GrowthFactor
        {
            get => _growthFactor;
            set => _growthFactor = Math.Max(1.1f, value);
        }

        /// <inheritdoc />
        public float ShrinkThreshold
        {
            get => _shrinkThreshold;
            set => _shrinkThreshold = Mathf.Clamp01(value);
        }

        #endregion

        #region IPoolMetrics Properties

        /// <inheritdoc />
        public int PeakActiveCount => _metrics.PeakActiveCount;

        /// <inheritdoc />
        public int TotalCreatedCount => _metrics.TotalCreatedCount;

        /// <inheritdoc />
        public int TotalAcquiredCount => _metrics.TotalAcquiredCount;

        /// <inheritdoc />
        public int TotalReleasedCount => _metrics.TotalReleasedCount;

        /// <inheritdoc />
        public int CurrentActiveCount => _activeItems.Count();

        /// <inheritdoc />
        public int CurrentCapacity => TotalCount;

        #endregion

        #region Pool Operations

        /// <inheritdoc />
        public void PrewarmPool(int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            if (count <= 0)
                return;

            using (_profiler?.Sample($"Prewarm pool", _id, _poolName,0,0))
            {
                for (int i = 0; i < count; i++)
                {
                    var item = CreateNewItem();
                    _inactiveItems.Add(GCHandle.ToIntPtr(GCHandle.Alloc(item)));
                }
            }

            _logger?.LogInfoInstance($"Prewarmed pool '{_poolName}' with {count} items");
        }

        /// <summary>
        /// Creates a new item for the pool
        /// </summary>
        private T CreateNewItem()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            T item = default;

            using (_profiler?.Sample("Create New Item", _id, _poolName,0,0))
            {
                item = _objectFactory();
                _metrics.RecordCreate(_id,1,3L);

                // Initialize the new object if an initialize function was provided
                _initializeFunc?.Invoke(item);

                // Call OnCreate for IPoolable objects
                if (item is IPoolable poolable)
                {
                    poolable.OnAcquire();
                }
            }

            _diagnostics?.RecordCreate(this);

            return item;
        }

        /// <inheritdoc />
        public T Acquire()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            T item = default;
            IntPtr itemPtr = IntPtr.Zero;

            _diagnostics?.RecordAcquireStart(this);

            using (_profiler?.Sample("Acquire", _id, _poolName,0,0))
            {
                // If we have inactive items, use one of them
                if (_inactiveItems.Length > 0)
                {
                    int lastIndex = _inactiveItems.Length - 1;
                    itemPtr = _inactiveItems[lastIndex];
                    _inactiveItems.RemoveAt(lastIndex);

                    item = (T)GCHandle.FromIntPtr(itemPtr).Target;
                }
                // Otherwise create a new item
                else
                {
                    // Check if we're at the maximum capacity and shouldn't create more
                    if (_maximumCapacity > 0 && TotalCount >= _maximumCapacity)
                    {
                        _logger?.LogWarningInstance(
                            $"Pool '{_poolName}' has reached maximum capacity of {_maximumCapacity}. Cannot create more objects.");
                        throw new InvalidOperationException(
                            $"Pool '{_poolName}' has reached maximum capacity of {_maximumCapacity}.");
                    }

                    item = CreateNewItem();
                    itemPtr = GCHandle.ToIntPtr(GCHandle.Alloc(item));
                }

                // Add to active items
                _activeItems.Add(itemPtr, true);
                _metrics.RecordCreate(_id,1,3L); // TODO();

                // Validate if needed
                if (_validateFunc != null && !_validateFunc(item))
                {
                    _logger?.LogWarningInstance(
                        $"Item from pool '{_poolName}' failed validation. Creating a new item instead.");

                    // Remove from active items
                    _activeItems.Remove(itemPtr);

                    // Dispose the invalid item and its handle
                    DestroyItemInternal(itemPtr, item);

                    // Create a new item
                    item = CreateNewItem();
                    itemPtr = GCHandle.ToIntPtr(GCHandle.Alloc(item));
                    _activeItems.Add(itemPtr, true);
                }

                // Call OnAcquire for IPoolable objects
                if (item is IPoolable poolable)
                {
                    poolable.OnAcquire();
                }

                OnItemAcquiredInternal(item);
            }

            _diagnostics?.RecordAcquireComplete(this, _activeItems.Count(), item);

            return item;
        }

        /// <inheritdoc />
        public List<T> AcquireMultiple(int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero");

            var result = new List<T>(count);

            using (_profiler?.Sample("AcquireMultiple", _id, _poolName,0,0))
            {
                for (int i = 0; i < count; i++)
                {
                    result.Add(Acquire());
                }
            }

            return result;
        }

        /// <summary>
        /// Called when an item is acquired from the pool
        /// </summary>
        /// <param name="item">The acquired item</param>
        private void OnItemAcquiredInternal(T item)
        {
            // Implement any required acquisition logic here
            // For example, notify any item tracking systems, update metrics, etc.
    
            // Example implementation:
            // if (item is IComplexPoolable complexPoolable)
            // {
            //     complexPoolable.OnPoolAcquire();
            // }
            //
            // // Fire any acquisition events
            // _diagnostics?.RecordItemUsage(this, item);
        }


        /// <inheritdoc />
        public T AcquireAndSetup(Action<T> setupAction)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            T item = Acquire();

            using (_profiler?.Sample("Setup", _id, _poolName,0,0))
            {
                setupAction(item);
            }

            return item;
        }

        /// <inheritdoc />
        public void Release(T item)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            using (_profiler?.Sample("Release", _id, _poolName,0,0))
            {
                // Find the handle for this item
                IntPtr itemPtr = IntPtr.Zero;
                bool found = false;

                using (var keyValueArrays = _activeItems.GetKeyValueArrays(Allocator.Temp))
                {
                    for (int i = 0; i < keyValueArrays.Length; i++)
                    {
                        var handle = GCHandle.FromIntPtr(keyValueArrays.Keys[i]);
                        if (handle.Target.Equals(item))
                        {
                            itemPtr = keyValueArrays.Keys[i];
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    _logger?.LogWarningInstance(
                        $"Attempted to release an item to pool '{_poolName}' that was not acquired from it.");
                    return;
                }

                // Remove from active items
                _activeItems.Remove(itemPtr);

                // Reset the item if a reset function was provided
                _resetFunc?.Invoke(item);

                // Call OnRelease for IPoolable objects
                if (item is IPoolable poolable)
                {
                    poolable.OnRelease();
                }

                // Add to inactive items
                _inactiveItems.Add(itemPtr);
                _metrics.RecordRelease(_id,-1,3L); // TODO();

                OnItemReleasedInternal(item);
            }

            _diagnostics?.RecordRelease(this, _activeItems.Count(), item);

            // Check if we need to auto-shrink
            if (_autoShrinkEnabled)
            {
                TryAutoShrink();
            }
        }

        /// <inheritdoc />
        public void ReleaseMultiple(IEnumerable<T> items)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            if (items == null)
                throw new ArgumentNullException(nameof(items));

            using (_profiler?.Sample("ReleaseMultiple", _id, _poolName,0,0))
            {
                foreach (var item in items)
                {
                    if (item != null)
                    {
                        Release(item);
                    }
                }
            }
        }

        /// <summary>
        /// Called when an item is released back to the pool
        /// </summary>
        /// <param name="item">The released item</param>
        private void OnItemReleasedInternal(T item)
        {
            // Base implementation does nothing - override in derived classes
        }

        /// <summary>
        /// Destroys an item and its dependencies
        /// </summary>
        /// <param name="itemPtr">Pointer to the item to destroy</param>
        /// <param name="item">The item to destroy</param>
        private void DestroyItemInternal(IntPtr itemPtr, T item)
        {
            if (itemPtr == IntPtr.Zero || item == null)
                return;

            // Call OnDestroy for IPoolable objects
            if (item is IPoolable poolable)
            {
                poolable.OnDestroy();
            }

            // Dispose any registered dependencies
            if (_itemDependencies.TryGetValue(itemPtr, out var dependencies))
            {
                for (int i = 0; i < dependencies.Length; i++)
                {
                    try
                    {
                        var dependencyHandle = GCHandle.FromIntPtr(dependencies[i]);
                        var dependency = (IDisposable)dependencyHandle.Target;
                        dependency.Dispose();
                        dependencyHandle.Free();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogErrorInstance($"Error disposing dependency for item in pool '{_poolName}': {ex.Message}");
                    }
                }

                dependencies.Dispose();
                _itemDependencies.Remove(itemPtr);
            }

            // Remove properties
            _itemProperties.Remove(itemPtr);

            // Dispose the item itself if it's disposable
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

            // Free the GCHandle
            GCHandle.FromIntPtr(itemPtr).Free();
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            using (_profiler?.Sample("Clear", _id, _poolName,0,0))
            {
                // Process active items
                using (var activeKeys = _activeItems.GetKeyArray(Allocator.Temp))
                {
                    foreach (var itemPtr in activeKeys)
                    {
                        var handle = GCHandle.FromIntPtr(itemPtr);
                        DestroyItemInternal(itemPtr, (T)handle.Target);
                    }
                }

                _activeItems.Clear();

                // Process inactive items
                for (int i = 0; i < _inactiveItems.Length; i++)
                {
                    var itemPtr = _inactiveItems[i];
                    var handle = GCHandle.FromIntPtr(itemPtr);
                    DestroyItemInternal(itemPtr, (T)handle.Target);
                }

                _inactiveItems.Clear();

                // Clear other collections
                foreach (var dependencies in _itemDependencies)
                {
                    dependencies.Value.Dispose();
                }

                _itemDependencies.Clear();
                _itemProperties.Clear();
            }

            _logger?.LogInfoInstance($"Cleared pool '{_poolName}'");
        }

        /// <inheritdoc />
        public void EnsureCapacity(int capacity)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero");

            // Check if we need to create more items
            int itemsToCreate = capacity - TotalCount;

            if (itemsToCreate > 0)
            {
                // Check if this would exceed the maximum capacity
                if (_maximumCapacity > 0 && capacity > _maximumCapacity)
                {
                    _logger?.LogWarningInstance(
                        $"Requested capacity {capacity} for pool '{_poolName}' exceeds maximum capacity of {_maximumCapacity}. Creating up to the maximum instead.");
                    itemsToCreate = _maximumCapacity - TotalCount;
                }

                if (itemsToCreate > 0)
                {
                    using (_profiler?.Sample("EnsureCapacity", _id, _poolName,0,0))
                    {
                        for (int i = 0; i < itemsToCreate; i++)
                        {
                            var item = CreateNewItem();
                            _inactiveItems.Add(GCHandle.ToIntPtr(GCHandle.Alloc(item)));
                        }
                    }

                    _logger?.LogInfoInstance(
                        $"Expanded pool '{_poolName}' by {itemsToCreate} items to ensure capacity of {capacity}");
                }
            }

            // Ensure native collections have sufficient capacity
            if (_inactiveItems.Capacity < capacity)
            {
                _inactiveItems.Capacity = capacity;
            }

            if (_activeItems.Capacity < capacity)
            {
                _activeItems.Capacity = capacity;
            }
        }

        public void SetPoolName(string newName)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IPoolMetrics Implementation

        /// <inheritdoc />
        public Dictionary<string, object> GetMetrics()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            var metrics = new Dictionary<string, object>
            {
                { "PoolName", _poolName },
                { "ItemType", typeof(T).Name },
                { "TotalCount", TotalCount },
                { "ActiveCount", _activeItems.Count() },
                { "InactiveCount", _inactiveItems.Length },
                { "PeakActiveCount", _metrics.PeakActiveCount },
                { "TotalCreated", _metrics.TotalCreatedCount },
                { "TotalAcquired", _metrics.TotalAcquiredCount },
                { "TotalReleased", _metrics.TotalReleasedCount },
                { "MinimumCapacity", _minimumCapacity },
                { "MaximumCapacity", _maximumCapacity },
                { "AutoShrinkEnabled", _autoShrinkEnabled },
                { "ShrinkThreshold", _shrinkThreshold },
                { "ShrinkInterval", _shrinkInterval },
                { "TimeSinceLastShrink", Time.realtimeSinceStartup - _lastShrinkTime },
                { "IsDisposed", _isDisposed },
                { "GrowthFactor", _growthFactor },
                { "IsCreated", IsCreated },
                { "MemoryUsage", GetApproximateMemoryUsage() }
            };

            return metrics;
        }

        /// <summary>
        /// Calculates approximate memory usage of the pool in bytes
        /// </summary>
        /// <returns>Approximate memory usage in bytes</returns>
        private long GetApproximateMemoryUsage()
        {
            if (_isDisposed)
                return 0;

            // Calculate memory used by native collections
            long memoryUsage = 0;

            memoryUsage += _inactiveItems.Capacity * UnsafeUtility.SizeOf<IntPtr>();
            memoryUsage += _activeItems.Capacity * (UnsafeUtility.SizeOf<IntPtr>() + sizeof(bool));

            // Calculate memory for dependencies
            memoryUsage += _itemDependencies.Count() * UnsafeUtility.SizeOf<IntPtr>() * 2;

            return memoryUsage;
        }

        /// <inheritdoc />
        public void ResetMetrics()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            _metrics.ResetMetrics(_id);

            _logger?.LogInfoInstance($"Reset metrics for pool '{_poolName}'");
        }

        #endregion

        #region Item Dependencies and Properties

        /// <inheritdoc />
        public void RegisterDependency(T item, IDisposable dependency)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (dependency == null)
                throw new ArgumentNullException(nameof(dependency));

            // Find the handle for this item
            IntPtr itemPtr = IntPtr.Zero;
            bool found = false;

            using (var keyValueArrays = _activeItems.GetKeyValueArrays(Allocator.Temp))
            {
                for (int i = 0; i < keyValueArrays.Length; i++)
                {
                    var handle = GCHandle.FromIntPtr(keyValueArrays.Keys[i]);
                    if (handle.Target.Equals(item))
                    {
                        itemPtr = keyValueArrays.Keys[i];
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                using (var inactiveArray = _inactiveItems.AsArray())
                {
                    for (int i = 0; i < inactiveArray.Length; i++)
                    {
                        var handle = GCHandle.FromIntPtr(inactiveArray[i]);
                        if (handle.Target.Equals(item))
                        {
                            itemPtr = inactiveArray[i];
                            found = true;
                            break;
                        }
                    }
                }
            }

            if (!found)
            {
                _logger?.LogWarningInstance(
                    $"Attempted to register a dependency for an item not belonging to pool '{_poolName}'.");
                return;
            }

            // Get or create the dependencies list
            if (!_itemDependencies.TryGetValue(itemPtr, out var dependencies))
            {
                dependencies =
                    new NativeList<IntPtr>(_inactiveItems.IsCreated ? Allocator.Temp : Allocator.Persistent);
                _itemDependencies.Add(itemPtr, dependencies);
            }

            // Add the dependency (wrapped in a GCHandle)
            dependencies.Add(GCHandle.ToIntPtr(GCHandle.Alloc(dependency)));
        }

        /// <inheritdoc />
        public void SetProperty(T item, string propertyName, object value)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

            // Find the handle for this item
            IntPtr itemPtr = IntPtr.Zero;
            bool found = false;

            using (var keyValueArrays = _activeItems.GetKeyValueArrays(Allocator.Temp))
            {
                for (int i = 0; i < keyValueArrays.Length; i++)
                {
                    var handle = GCHandle.FromIntPtr(keyValueArrays.Keys[i]);
                    if (handle.Target.Equals(item))
                    {
                        itemPtr = keyValueArrays.Keys[i];
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                using (var inactiveArray = _inactiveItems.AsArray())
                {
                    for (int i = 0; i < inactiveArray.Length; i++)
                    {
                        var handle = GCHandle.FromIntPtr(inactiveArray[i]);
                        if (handle.Target.Equals(item))
                        {
                            itemPtr = inactiveArray[i];
                            found = true;
                            break;
                        }
                    }
                }
            }

            if (!found)
            {
                _logger?.LogWarningInstance($"Attempted to set a property for an item not belonging to pool '{_poolName}'.");
                return;
            }

            // Get or create the properties dictionary
            if (!_itemProperties.TryGetValue(itemPtr, out var properties))
            {
                properties = new Dictionary<string, object>();
                _itemProperties[itemPtr] = properties;
            }

            // Set the property
            properties[propertyName] = value;
        }
        
        /// <inheritdoc />
        public object GetProperty(T item, string propertyName, object defaultValue = null)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

            // Find the handle for this item
            IntPtr itemPtr = IntPtr.Zero;
            bool found = false;

            using (var keyValueArrays = _activeItems.GetKeyValueArrays(Allocator.Temp))
            {
                for (int i = 0; i < keyValueArrays.Length; i++)
                {
                    var handle = GCHandle.FromIntPtr(keyValueArrays.Keys[i]);
                    if (handle.Target.Equals(item))
                    {
                        itemPtr = keyValueArrays.Keys[i];
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                using (var inactiveArray = _inactiveItems.AsArray())
                {
                    for (int i = 0; i < inactiveArray.Length; i++)
                    {
                        var handle = GCHandle.FromIntPtr(inactiveArray[i]);
                        if (handle.Target.Equals(item))
                        {
                            itemPtr = inactiveArray[i];
                            found = true;
                            break;
                        }
                    }
                }
            }

            if (!found)
            {
                _logger?.LogWarningInstance(
                    $"Attempted to get a property for an item not belonging to pool '{_poolName}'.");
                return defaultValue;
            }

            // Get the properties dictionary
            if (!_itemProperties.TryGetValue(itemPtr, out var properties))
                return defaultValue;

            // Get the property
            if (!properties.TryGetValue(propertyName, out var value))
                return defaultValue;

            return value;
        }

        #endregion

        #region IShrinkablePool Implementation
        
        /// <inheritdoc />
        public bool TryShrink(float threshold)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            // Validate threshold
            float validThreshold = Mathf.Clamp01(threshold);

            // Check if shrinking is needed
            if (TotalCount <= _minimumCapacity)
                return false;

            float usageRatio = (float)_activeItems.Count() / TotalCount;

            // If usage is above the threshold, don't shrink
            if (usageRatio >= validThreshold)
                return false;

            // Calculate the new target capacity
            int targetCapacity = Math.Max(_minimumCapacity, (int)(_activeItems.Count() * _growthFactor));

            return ShrinkTo(targetCapacity);
        }

        /// <inheritdoc />
        public bool ShrinkTo(int targetCapacity)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);

            // Ensure we don't go below the minimum capacity
            int validTargetCapacity = Math.Max(_minimumCapacity, targetCapacity);

            // Check if shrinking is needed
            if (TotalCount <= validTargetCapacity)
                return false;

            // Calculate how many items to remove
            int itemsToRemove = TotalCount - validTargetCapacity;

            // Ensure we only remove from inactive items
            itemsToRemove = Math.Min(itemsToRemove, _inactiveItems.Length);

            if (itemsToRemove <= 0)
                return false;

            using (_profiler?.Sample("Shrink", _id, _poolName,0,0))
            {
                // Remove and destroy the oldest items (from the beginning of the list)
                for (int i = 0; i < itemsToRemove && i < _inactiveItems.Length; i++)
                {
                    var itemPtr = _inactiveItems[i];
                    var item = (T)GCHandle.FromIntPtr(itemPtr).Target;
                    DestroyItemInternal(itemPtr, item);
                }

                // Remove the items from the inactive list
                if (itemsToRemove >= _inactiveItems.Length)
                {
                    _inactiveItems.Clear();
                }
                else
                {
                    // Create a new list without the removed items
                    var newInactiveItems = new NativeList<IntPtr>(_inactiveItems.Length - itemsToRemove,
                        Allocator.Persistent);
                    for (int i = itemsToRemove; i < _inactiveItems.Length; i++)
                    {
                        newInactiveItems.Add(_inactiveItems[i]);
                    }

                    // Replace the old list with the new one
                    _inactiveItems.Dispose();
                    _inactiveItems = newInactiveItems;
                }
            }

            _lastShrinkTime = Time.realtimeSinceStartup;

            _logger?.LogInfoInstance(
                $"Shrunk pool '{_poolName}' by {itemsToRemove} items to target capacity {validTargetCapacity}");

            return true;
        }

        /// <inheritdoc />
        public void SetAutoShrink(bool enabled)
        {
            _autoShrinkEnabled = enabled;

            if (enabled)
            {
                _lastShrinkTime = Time.realtimeSinceStartup;
            }

            _logger?.LogInfoInstance($"Auto-shrink for pool '{_poolName}' {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Attempts to shrink the pool if auto-shrink is enabled and the interval has elapsed
        /// </summary>
        public void TryAutoShrink()
        {
            if (_isDisposed || !_autoShrinkEnabled)
                return;

            float timeSinceLastShrink = Time.realtimeSinceStartup - _lastShrinkTime;

            if (timeSinceLastShrink >= _shrinkInterval)
            {
                TryShrink(_shrinkThreshold);
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
                return;

            using (_profiler?.Sample("Dispose", _id, _poolName,0,0))
            {
                // Destroy all items
                using (var activeKeys = _activeItems.GetKeyArray(Allocator.Temp))
                {
                    foreach (var itemPtr in activeKeys)
                    {
                        var handle = GCHandle.FromIntPtr(itemPtr);
                        DestroyItemInternal(itemPtr, (T)handle.Target);
                    }
                }

                for (int i = 0; i < _inactiveItems.Length; i++)
                {
                    var itemPtr = _inactiveItems[i];
                    var handle = GCHandle.FromIntPtr(itemPtr);
                    DestroyItemInternal(itemPtr, (T)handle.Target);
                }

                // Dispose native collections
                if (_inactiveItems.IsCreated)
                {
                    _inactiveItems.Dispose();
                }

                if (_activeItems.IsCreated)
                {
                    _activeItems.Dispose();
                }

                // Dispose all dependency lists
                using (var keyValueArrays = _itemDependencies.GetKeyValueArrays(Allocator.Temp))
                {
                    for (int i = 0; i < keyValueArrays.Length; i++)
                    {
                        keyValueArrays.Values[i].Dispose();
                    }
                }

                if (_itemDependencies.IsCreated)
                {
                    _itemDependencies.Dispose();
                }

                // Clear managed collections
                _itemProperties.Clear();

                // Unregister from diagnostics
                if (_diagnostics != null)
                {
                    _diagnostics.UnregisterPool(this);
                }

                // Unregister from registry
                if (_registry != null)
                {
                    _registry.UnregisterPool(this);
                }

                _isDisposed = true;
            }

            _logger?.LogInfoInstance($"Disposed pool '{_poolName}'");
        }

        #endregion

    }
}