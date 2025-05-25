using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using JetBrains.Annotations;
using UnityEngine;

namespace AhBearStudios.Pooling.Pools.Native
{
    /// <summary>
    /// A native memory pool implementation using Unity Collections v2 that provides
    /// efficient allocation and reuse of unmanaged objects.
    /// </summary>
    /// <typeparam name="T">The type of elements to store in the pool. Must be unmanaged.</typeparam>
    [MustDisposeResource]
    [GenerateTestsForBurstCompatibility]
    public sealed class NativePool<T> : INativePool<T>, IShrinkablePool, IPoolMetrics, IDisposable 
        where T : unmanaged
    {
        private UnsafeList<T> _items;
        private UnsafeList<int> _freeIndices;
        private UnsafeBitArray _activeFlags;
        private int _activeCount;
        private bool _isDisposed;
        private readonly PoolThreadingMode _threadingMode;
        private readonly NativePoolConfig _config;
        
        // Metrics tracking
        private int _peakActiveCount;
        private int _totalCreatedCount;
        private int _totalAcquiredCount;
        private int _totalReleasedCount;
        private float _lastShrinkTime;
        private bool _autoShrinkEnabled;
        private float _shrinkInterval = 60.0f; // Default: shrink check every minute
        private float _growthFactor = 2.0f;
        private float _shrinkThreshold = 0.3f;
        private int _minimumCapacity;
        
        // Registry integration
        private int _poolId;
        
        #region Properties

        /// <summary>
        /// Gets the allocator used by this pool
        /// </summary>
        public Allocator Allocator { get; }

        /// <summary>
        /// Gets the capacity of the pool
        /// </summary>
        public int Capacity => _items.Length;

        /// <summary>
        /// Gets the unique identifier for this pool
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the name of this pool
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Gets the name of this pool (IPool implementation)
        /// </summary>
        public string PoolName => Name;

        /// <summary>
        /// Gets the maximum capacity of this pool.
        /// Returns int.MaxValue if the pool has no fixed maximum.
        /// </summary>
        public int MaxCapacity => _config.MaximumCapacity;

        /// <summary>
        /// Gets a value indicating whether the pool has been disposed
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Gets the current usage percentage of the pool (0-1)
        /// </summary>
        public float UsagePercentage => Capacity > 0 ? (float)_activeCount / Capacity : 0;

        /// <summary>
        /// Gets metrics about this pool for diagnostic purposes
        /// </summary>
        public PoolMetricsData Metrics => new PoolMetricsData(new FixedString128Bytes(Name))
            .WithTypeInfo(
                new FixedString64Bytes(typeof(NativePool<T>).Name),
                new FixedString64Bytes(typeof(T).Name),
                UnsafeUtility.SizeOf<T>())
            .WithCapacity(
                Capacity,
                _activeCount,
                MaxCapacity)
            .RecordCreate(0); // Just to ensure existing counts are preserved
        
        /// <summary>
        /// Gets whether the pool supports automatic shrinking
        /// </summary>
        public bool SupportsAutoShrink => true;
        
        /// <summary>
        /// Gets or sets the minimum capacity that the pool will maintain even when shrinking
        /// </summary>
        public int MinimumCapacity 
        { 
            get => _minimumCapacity;
            set => _minimumCapacity = Math.Max(1, value);
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
            get => _shrinkInterval; 
            set => _shrinkInterval = Math.Max(0.1f, value);
        }
        
        /// <summary>
        /// Gets or sets the growth factor when the pool needs to expand
        /// </summary>
        public float GrowthFactor 
        { 
            get => _growthFactor; 
            set => _growthFactor = Math.Max(1.1f, value);
        }
        
        /// <summary>
        /// Gets or sets the shrink threshold
        /// </summary>
        public float ShrinkThreshold 
        { 
            get => _shrinkThreshold; 
            set => _shrinkThreshold = Math.Clamp(value, 0.1f, 0.9f);
        }
        
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
        public int CurrentActiveCount => _activeCount;
        
        /// <summary>
        /// Gets the current capacity of the pool
        /// </summary>
        public int CurrentCapacity => _items.Length;
        
        /// <summary>
        /// Gets whether automatic shrinking is enabled
        /// </summary>
        public bool AutoShrinkEnabled => _autoShrinkEnabled;
        
        /// <summary>
        /// Gets the timestamp of the last shrink operation
        /// </summary>
        public float LastShrinkTime => _lastShrinkTime;
        
        /// <summary>
        /// Gets the total number of items in the pool (active + inactive)
        /// </summary>
        public int TotalCount => _items.Length;
        
        /// <summary>
        /// Gets the number of active items in the pool
        /// </summary>
        public int ActiveCount => _activeCount;
        
        /// <summary>
        /// Gets the number of inactive items in the pool
        /// </summary>
        public int InactiveCount => _items.Length - _activeCount;
        
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
        public bool IsCreated => _items.IsCreated && !_isDisposed;
        
        /// <summary>
        /// Gets the type of items in the pool
        /// </summary>
        public Type ItemType => typeof(T);
        
        /// <summary>
        /// Gets the threading mode for this pool
        /// </summary>
        public PoolThreadingMode ThreadingMode => _threadingMode;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NativePool{T}"/> class.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Allocator to use for native collections</param>
        /// <param name="name">Name of the pool for diagnostics</param>
        /// <param name="threadingMode">Threading mode for this pool</param>
        [MustDisposeResource]
        public NativePool(int initialCapacity, Allocator allocator, string name = null, PoolThreadingMode threadingMode = PoolThreadingMode.SingleThreaded) 
            : this(new PoolConfig { InitialCapacity = initialCapacity }, allocator, name, threadingMode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NativePool{T}"/> class with a configuration.
        /// </summary>
        /// <param name="config">Configuration for the pool</param>
        /// <param name="allocator">Allocator to use for native collections</param>
        /// <param name="name">Name of the pool for diagnostics</param>
        /// <param name="threadingMode">Threading mode for this pool</param>
        [MustDisposeResource]
        public NativePool(IPoolConfig config, Allocator allocator, string name = null,
            PoolThreadingMode threadingMode = PoolThreadingMode.SingleThreaded)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            // Store the config using proper type checking and fallback
            _config = config as NativePoolConfig ?? new NativePoolConfig
            {
                InitialCapacity = config.InitialCapacity,
                MaximumCapacity = config.MaximumCapacity,
                PrewarmOnInit = config.PrewarmOnInit,
                EnableAutoShrink = config.EnableAutoShrink,
                ShrinkThreshold = config.ShrinkThreshold,
                ShrinkInterval = config.ShrinkInterval,
                ThreadingMode = config.ThreadingMode,
                NativeAllocator = config.NativeAllocator
            };

            // Set basic properties
            Allocator = allocator;
            Id = Guid.NewGuid();
            Name = string.IsNullOrEmpty(name) ? $"NativePool<{typeof(T).Name}>" : name;
            _threadingMode = threadingMode;

            // Initialize pool configuration values
            _minimumCapacity = Math.Max(1, _config.InitialCapacity);
            _autoShrinkEnabled = _config.EnableAutoShrink;
            _shrinkThreshold = _config.ShrinkThreshold;
            _shrinkInterval = _config.ShrinkInterval;
            _lastShrinkTime = Time.realtimeSinceStartup;

            // Create and initialize the underlying collections with proper sizes
            var capacity = Math.Max(1, _config.InitialCapacity);
            _items = new UnsafeList<T>(capacity, allocator);
            _items.Resize(capacity, NativeArrayOptions.ClearMemory);

            _freeIndices = new UnsafeList<int>(capacity, allocator);
            _activeFlags = new UnsafeBitArray(capacity, allocator, NativeArrayOptions.ClearMemory);

            // Initialize free list with all indices
            for (int i = 0; i < capacity; i++)
            {
                _freeIndices.Add(i);
            }

            // Register with the NativePoolRegistry
            _poolId = NativePoolRegistry.Instance.Register(this).PoolId;
            
            // Pre-warm if needed
            if (_config.PrewarmOnInit)
            {
                for (int i = 0; i < capacity; i++)
                {
                    int idx = AcquireIndex();
                    Release(idx);
                }
            }
        }

        #endregion

                #region IPool Implementation

        /// <summary>
        /// Gets metrics data about the pool as a dictionary of key-value pairs.
        /// </summary>
        /// <returns>A dictionary containing pool metrics.</returns>
        public Dictionary<string, object> GetMetrics()
        {
            var metrics = new Dictionary<string, object>
            {
                ["Name"] = Name,
                ["Type"] = typeof(T).Name,
                ["ItemSize"] = UnsafeUtility.SizeOf<T>(),
                ["Capacity"] = Capacity,
                ["ActiveCount"] = ActiveCount,
                ["InactiveCount"] = InactiveCount,
                ["MaxCapacity"] = MaxCapacity,
                ["PeakUsage"] = PeakActiveCount,
                ["TotalCreated"] = TotalCreatedCount,
                ["TotalAcquired"] = TotalAcquiredCount,
                ["TotalReleased"] = TotalReleasedCount,
                ["UsagePercentage"] = UsagePercentage,
                ["ThreadingMode"] = ThreadingMode.ToString(),
                ["Allocator"] = Allocator.ToString(),
                ["AutoShrinkEnabled"] = AutoShrinkEnabled,
                ["ShrinkThreshold"] = ShrinkThreshold,
                ["ShrinkInterval"] = ShrinkInterval,
                ["MinimumCapacity"] = MinimumCapacity,
                ["Id"] = Id.ToString(),
                ["IsDisposed"] = IsDisposed,
                ["LastShrinkTime"] = LastShrinkTime
            };

            return metrics;
        }

        #endregion

        #region IShrinkablePool Implementation

        /// <summary>
        /// Sets the auto-shrink enabled state of the pool.
        /// </summary>
        /// <param name="enabled">Whether auto-shrink should be enabled or disabled.</param>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        public void SetAutoShrink(bool enabled)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            _autoShrinkEnabled = enabled;
            
            // Reset the shrink timer if enabling
            if (enabled)
            {
                _lastShrinkTime = Time.realtimeSinceStartup;
            }
        }

        /// <summary>
        /// Attempts to shrink the pool to the specified capacity.
        /// </summary>
        /// <param name="targetCapacity">The target capacity to shrink to.</param>
        /// <returns>True if the pool was shrunk, false otherwise.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if targetCapacity is less than the number of active items.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        public bool ShrinkTo(int targetCapacity)
{
    if (_isDisposed)
    {
        throw new ObjectDisposedException(Name);
    }

    // Ensure we don't shrink below active count
    if (targetCapacity < _activeCount)
    {
        throw new ArgumentOutOfRangeException(nameof(targetCapacity), 
            $"Target capacity ({targetCapacity}) cannot be less than active count ({_activeCount})");
    }

    // Ensure we don't shrink below minimum capacity
    if (targetCapacity < _minimumCapacity)
    {
        targetCapacity = _minimumCapacity;
    }
    
    // If target is greater than or equal to current capacity, no shrinking needed
    if (targetCapacity >= _items.Length)
    {
        return false;
    }

    // Perform the same operations as in TryShrink but with a fixed target capacity
    // We need to rebuild the free indices list to account for the indices that will be removed
    // First, create a mapping from old indices to new indices
    var indexMapping = new UnsafeList<int>(_items.Length, Allocator.Temp);
    indexMapping.Resize(_items.Length, NativeArrayOptions.ClearMemory);

    // Initialize with sentinel values
    for (int i = 0; i < indexMapping.Length; i++)
    {
        indexMapping[i] = -1;
    }

    // We need to keep track of the active indices
    var activeIndices = new UnsafeList<int>(_activeCount, Allocator.Temp);
    for (int i = 0; i < _items.Length; i++)
    {
        if (_activeFlags.IsSet(i))
        {
            activeIndices.Add(i);
        }
    }

    // Ensure active indices are moved to the front of the new array
    for (int i = 0; i < activeIndices.Length; i++)
    {
        int oldIndex = activeIndices[i];
        if (i < targetCapacity)
        {
            indexMapping[oldIndex] = i;
        }
        else
        {
            // This shouldn't happen if we calculated correctly, but just in case
            Debug.LogWarning($"Pool '{Name}' couldn't fit all active items during shrink. Some items will be lost.");
        }
    }

    // Create new collections with the new capacity
    var newItems = new UnsafeList<T>(targetCapacity, Allocator);
    newItems.Resize(targetCapacity, NativeArrayOptions.ClearMemory);
    
    var newActiveFlags = new UnsafeBitArray(targetCapacity, Allocator, NativeArrayOptions.ClearMemory);
    var newFreeIndices = new UnsafeList<int>(targetCapacity - _activeCount, Allocator);

    // Set up index tracking
    int nextNewIndex = _activeCount; // Next available index after active items

    // Move items to their new positions
    for (int oldIndex = 0; oldIndex < _items.Length; oldIndex++)
    {
        int newIndex = indexMapping[oldIndex];
        
        if (newIndex >= 0) // This item has a mapping
        {
            // Copy the item
            newItems[newIndex] = _items[oldIndex];
            
            // Set active flag if it was active
            if (_activeFlags.IsSet(oldIndex))
            {
                newActiveFlags.Set(newIndex, true);
            }
            else
            {
                // This shouldn't happen with our allocation scheme, but handle it just in case
                newFreeIndices.Add(newIndex);
            }
        }
    }

    // Add remaining free indices
    for (int i = _activeCount; i < targetCapacity; i++)
    {
        if (!newFreeIndices.Contains(i))
        {
            newFreeIndices.Add(i);
        }
    }

    // Dispose old collections
    _items.Dispose();
    _activeFlags.Dispose();
    _freeIndices.Dispose();

    // Update with the new collections
    _items = newItems;
    _activeFlags = newActiveFlags;
    _freeIndices = newFreeIndices;

    // Clean up temporary collections
    indexMapping.Dispose();
    activeIndices.Dispose();

    // Update last shrink time
    _lastShrinkTime = Time.realtimeSinceStartup;
    
    // Update registry capacity information
    NativePoolRegistry.Instance.UpdateCapacity(_poolId, targetCapacity);

    return true;
}

        #endregion

        #region IPoolMetrics Implementation

        /// <summary>
        /// Gets metrics data about the pool as a dictionary of key-value pairs.
        /// This is an implementation of IPoolMetrics.GetMetrics().
        /// </summary>
        /// <returns>A dictionary containing pool metrics.</returns>
        Dictionary<string, object> IPoolMetrics.GetMetrics()
        {
            // Reuse the implementation from IPool.GetMetrics()
            return GetMetrics();
        }

        /// <summary>
        /// Acquires an item from the pool.
        /// </summary>
        /// <returns>The acquired item.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the pool has reached its maximum capacity and cannot grow further.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Acquire()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            int index = AcquireIndex();
            _totalAcquiredCount++;
            
            // Update peak count if necessary
            if (_activeCount > _peakActiveCount)
            {
                _peakActiveCount = _activeCount;
            }

            return _items[index];
        }

        /// <summary>
        /// Acquires an index from the pool.
        /// </summary>
        /// <returns>The acquired index.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the pool has reached its maximum capacity and cannot grow further.</exception>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AcquireIndex()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            // If no more free indices, grow the pool if possible
            if (_freeIndices.Length == 0)
            {
                // Check if we've reached the maximum capacity
                if (_config.MaximumCapacity > 0 && _items.Length >= _config.MaximumCapacity)
                {
                    if (_config.ThrowIfExceedingMaxCount)
                    {
                        throw new InvalidOperationException($"Pool '{Name}' has reached its maximum capacity of {_config.MaximumCapacity}");
                    }
                    
                    // Wait for an item to be released
                    return -1;
                }

                // Calculate new capacity using growth factor
                int newCapacity;
                if (_config.UseExponentialGrowth)
                {
                    newCapacity = Math.Max(_items.Length + 1, (int)(_items.Length * _growthFactor));
                }
                else
                {
                    newCapacity = _items.Length + _config.GrowthIncrement;
                }

                // Cap at maximum capacity if needed
                if (_config.MaximumCapacity > 0 && newCapacity > _config.MaximumCapacity)
                {
                    newCapacity = _config.MaximumCapacity;
                }

                // Grow the collections
                int oldCapacity = _items.Length;
                _items.Resize(newCapacity, NativeArrayOptions.ClearMemory);
                _activeFlags.Resize(newCapacity, NativeArrayOptions.ClearMemory);

                // Add new indices to free list
                for (int i = oldCapacity; i < newCapacity; i++)
                {
                    _freeIndices.Add(i);
                }
                
                // Update registry capacity information
                NativePoolRegistry.Instance.UpdateCapacity(_poolId, newCapacity);
            }

            // Get the next free index
            int index = _freeIndices[_freeIndices.Length - 1];
            _freeIndices.RemoveAt(_freeIndices.Length - 1);

            // Mark index as active
            _activeFlags.Set(index, true);
            _activeCount++;
            _totalCreatedCount++;
            
            // Update registry active count information
            NativePoolRegistry.Instance.UpdateActiveCount(_poolId, _activeCount);

            return index;
        }

        /// <summary>
        /// Acquires an item and initializes it with a setup action.
        /// </summary>
        /// <param name="setupAction">Action to initialize the item.</param>
        /// <returns>The acquired and initialized item.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the pool has reached its maximum capacity and cannot grow further.</exception>
        public T AcquireAndSetup(Action<T> setupAction)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            T item = Acquire();

            if (setupAction != null)
            {
                try
                {
                    setupAction(item);
                }
                catch (Exception)
                {
                    // If setup fails, release the item back to the pool and rethrow
                    try
                    {
                        Release(item);
                    }
                    catch
                    {
                        // Ignore exceptions during cleanup
                    }
                    throw;
                }
            }

            return item;
        }

        /// <summary>
        /// Acquires multiple items from the pool at once.
        /// </summary>
        /// <param name="count">Number of items to acquire.</param>
        /// <returns>List of acquired items.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is not positive.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the pool has reached its maximum capacity and cannot grow further.</exception>
        public List<T> AcquireMultiple(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
            }

            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            var result = new List<T>(count);
            
            try
            {
                for (int i = 0; i < count; i++)
                {
                    result.Add(Acquire());
                }
            }
            catch (Exception)
            {
                // If acquisition fails, return the items that were already acquired
                foreach (var item in result)
                {
                    try
                    {
                        Release(item);
                    }
                    catch
                    {
                        // Ignore exceptions during cleanup
                    }
                }
                throw;
            }

            return result;
        }

        /// <summary>
        /// Releases an item back to the pool.
        /// </summary>
        /// <param name="item">Item to release.</param>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        public void Release(T item)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            // Find the index of the item
            for (int i = 0; i < _items.Length; i++)
            {
                if (_activeFlags.IsSet(i) && EqualityComparer<T>.Default.Equals(_items[i], item))
                {
                    Release(i);
                    return;
                }
            }
            
            // Item wasn't found or was already inactive
        }

        /// <summary>
        /// Releases multiple items back to the pool at once.
        /// </summary>
        /// <param name="items">Items to release.</param>
        /// <exception cref="ArgumentNullException">Thrown if items is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        public void ReleaseMultiple(IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            foreach (var item in items)
            {
                Release(item);
            }
        }

        /// <summary>
        /// Releases an item at the specified index back to the pool.
        /// </summary>
        /// <param name="index">Index of the item to release.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release(int index)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            if (index < 0 || index >= _items.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range");
            }

            // Check if already inactive
            if (!_activeFlags.IsSet(index))
            {
                return;
            }

            // Mark as inactive
            _activeFlags.Set(index, false);
            _freeIndices.Add(index);
            _activeCount--;
            _totalReleasedCount++;
            
            // Update registry active count information
            NativePoolRegistry.Instance.UpdateActiveCount(_poolId, _activeCount);

            // Reset item if configured to do so
            if (_config.ResetOnRelease)
            {
                _items[index] = default;
            }

            // Check if auto-shrink is needed
            if (_autoShrinkEnabled && 
                Time.realtimeSinceStartup - _lastShrinkTime > _shrinkInterval &&
                _activeCount < _items.Length * _shrinkThreshold)
            {
                TryShrink(_shrinkThreshold);
            }
        }

        /// <summary>
        /// Gets the value at a specific index.
        /// </summary>
        /// <param name="index">Index to retrieve.</param>
        /// <returns>The value at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetValue(int index)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            if (index < 0 || index >= _items.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range");
            }

            return _items[index];
        }

        /// <summary>
        /// Sets the value at a specific index.
        /// </summary>
        /// <param name="index">Index to set.</param>
        /// <param name="value">Value to set.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the index is not active.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(int index, T value)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            if (index < 0 || index >= _items.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range");
            }

            if (!_activeFlags.IsSet(index))
            {
                throw new InvalidOperationException("Cannot set value for inactive index");
            }

            _items[index] = value;
        }

        /// <summary>
        /// Checks if an index is active in the pool.
        /// </summary>
        /// <param name="index">Index to check.</param>
        /// <returns>True if the item at the specified index is active, false otherwise.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsIndexActive(int index)
        {
            if (_isDisposed || index < 0 || index >= _items.Length)
            {
                return false;
            }

            return _activeFlags.IsSet(index);
        }

        /// <summary>
        /// Alias for GetValue that provides better compatibility with some interfaces.
        /// </summary>
        /// <param name="index">Index to retrieve.</param>
        /// <returns>The value at the specified index.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get(int index)
        {
            return GetValue(index);
        }

        /// <summary>
        /// Alias for SetValue that provides better compatibility with some interfaces.
        /// </summary>
        /// <param name="index">Index to set.</param>
        /// <param name="value">Value to set.</param>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, T value)
        {
            SetValue(index, value);
        }

        /// <summary>
        /// Alias for IsIndexActive that provides better compatibility with some interfaces.
        /// </summary>
        /// <param name="index">Index to check.</param>
        /// <returns>True if the item at the specified index is active, false otherwise.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsActive(int index)
        {
            return IsIndexActive(index);
        }

        /// <summary>
        /// Gets the number of active items in the pool.
        /// </summary>
        /// <returns>Number of active items.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetActiveCount()
        {
            if (_isDisposed)
            {
                return 0;
            }

            return _activeCount;
        }

        /// <summary>
        /// Gets an UnsafeList of active indices in the pool.
        /// </summary>
        /// <param name="allocator">Allocator to use for the returned list.</param>
        /// <returns>An UnsafeList containing all active indices.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        public UnsafeList<int> GetActiveIndices(Allocator allocator)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            var result = new UnsafeList<int>(_activeCount, allocator);
            
            for (int i = 0; i < _items.Length; i++)
            {
                if (_activeFlags.IsSet(i))
                {
                    result.Add(i);
                }
            }
            
            return result;
        }

        /// <summary>
        /// Gets active indices in the pool without allocating new memory.
        /// This writes to a pre-allocated UnsafeList and returns the count of active indices.
        /// </summary>
        /// <param name="indices">Pre-allocated UnsafeList to store the active indices.</param>
        /// <returns>Number of active indices written to the list.</returns>
        /// <exception cref="ArgumentException">Thrown if the indices list is not created.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        [BurstCompile]
        public int GetActiveIndicesUnsafe(ref UnsafeList<int> indices)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }
            
            if (!indices.IsCreated)
            {
                throw new ArgumentException("Indices list is not created", nameof(indices));
            }
            
            indices.Clear();
            
            for (int i = 0; i < _items.Length; i++)
            {
                if (_activeFlags.IsSet(i))
                {
                    indices.Add(i);
                }
            }
            
            return indices.Length;
        }

        /// <summary>
        /// Gets active indices in the pool writing directly to a provided pointer.
        /// </summary>
        /// <param name="indicesPtr">Pointer to an array where indices will be written.</param>
        /// <param name="maxLength">Maximum number of indices to write.</param>
        /// <returns>Number of active indices written to the array.</returns>
        /// <exception cref="ArgumentNullException">Thrown if indicesPtr is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if maxLength is not positive.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        [BurstCompile]
        public unsafe int GetActiveIndicesUnsafePtr(int* indicesPtr, int maxLength)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }
            
            if (indicesPtr == null)
            {
                throw new ArgumentNullException(nameof(indicesPtr));
            }
            
            if (maxLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLength), "Max length must be positive");
            }
            
            int count = 0;
            
            for (int i = 0; i < _items.Length && count < maxLength; i++)
            {
                if (_activeFlags.IsSet(i))
                {
                    indicesPtr[count++] = i;
                }
            }
            
            return count;
        }

        /// <summary>
        /// Clears the pool, returning all active items to the inactive state.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        public void Clear()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }
            
            // Reset all active flags
            _activeFlags.Clear();
            
            // Reset free indices list
            _freeIndices.Clear();
            for (int i = 0; i < _items.Length; i++)
            {
                _freeIndices.Add(i);
                
                // Reset item if configured to do so
                if (_config.ResetOnRelease)
                {
                    _items[i] = default;
                }
            }
            
            // Update active count
            _activeCount = 0;
            
            // Update registry active count information
            NativePoolRegistry.Instance.UpdateActiveCount(_poolId, 0);
        }

                /// <summary>
        /// Ensures the pool has at least the specified capacity.
        /// </summary>
        /// <param name="capacity">Required capacity.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is not positive.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        public void EnsureCapacity(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive");
            }
            
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }
            
            if (capacity <= _items.Length)
            {
                return; // Already have enough capacity
            }
            
            // Check maximum size
            if (_config.MaximumCapacity > 0 && capacity > _config.MaximumCapacity)
            {
                capacity = _config.MaximumCapacity;
            }
            
            int oldCapacity = _items.Length;
            
            // Resize collections
            _items.Resize(capacity, NativeArrayOptions.ClearMemory);
            _activeFlags.Resize(capacity, NativeArrayOptions.ClearMemory);
            
            // Add new indices to free list
            for (int i = oldCapacity; i < capacity; i++)
            {
                _freeIndices.Add(i);
            }
            
            // Update registry capacity information
            NativePoolRegistry.Instance.UpdateCapacity(_poolId, capacity);
        }

        /// <summary>
        /// Attempts to shrink the pool to reduce memory usage.
        /// </summary>
        /// <param name="targetUsage">Target usage percentage (0-1). If current usage is below this, the pool will shrink.</param>
        /// <returns>True if the pool was shrunk, false otherwise.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if targetUsage is not between 0 and 1.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        public bool TryShrink(float targetUsage = 0.5f)
        {
            if (targetUsage < 0f || targetUsage > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(targetUsage), "Target usage must be between 0 and 1");
            }

            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            // If we're at or below minimum capacity, don't shrink
            if (_items.Length <= _minimumCapacity)
            {
                return false;
            }

            // If our usage is above the target, don't shrink
            float currentUsage = (float)_activeCount / _items.Length;
            if (currentUsage >= targetUsage)
            {
                return false;
            }

            // Calculate the new capacity
            int newCapacity = Math.Max(_minimumCapacity, (int)Math.Ceiling(_activeCount / targetUsage));
            
            // Ensure we don't go below minimum capacity
            newCapacity = Math.Max(newCapacity, _minimumCapacity);
            
            // If the new capacity is the same or larger, don't shrink
            if (newCapacity >= _items.Length)
            {
                return false;
            }

            // We need to rebuild the free indices list to account for the indices that will be removed
            // First, create a mapping from old indices to new indices
            var indexMapping = new UnsafeList<int>(_items.Length, Allocator.Temp);
            indexMapping.Resize(_items.Length, NativeArrayOptions.ClearMemory);

            // Initialize with sentinel values
            for (int i = 0; i < indexMapping.Length; i++)
            {
                indexMapping[i] = -1;
            }

            // We need to keep track of the active indices
            var activeIndices = new UnsafeList<int>(_activeCount, Allocator.Temp);
            for (int i = 0; i < _items.Length; i++)
            {
                if (_activeFlags.IsSet(i))
                {
                    activeIndices.Add(i);
                }
            }

            // Ensure active indices are moved to the front of the new array
            for (int i = 0; i < activeIndices.Length; i++)
            {
                int oldIndex = activeIndices[i];
                if (i < newCapacity)
                {
                    indexMapping[oldIndex] = i;
                }
                else
                {
                    // This shouldn't happen if we calculated correctly, but just in case
                    Debug.LogWarning($"Pool '{Name}' couldn't fit all active items during shrink. Some items will be lost.");
                }
            }

            // Create new collections with the new capacity
            var newItems = new UnsafeList<T>(newCapacity, Allocator);
            newItems.Resize(newCapacity, NativeArrayOptions.ClearMemory);
            
            var newActiveFlags = new UnsafeBitArray(newCapacity, Allocator, NativeArrayOptions.ClearMemory);
            var newFreeIndices = new UnsafeList<int>(newCapacity - _activeCount, Allocator);

            // Set up index tracking
            int nextNewIndex = _activeCount; // Next available index after active items

            // Move items to their new positions
            for (int oldIndex = 0; oldIndex < _items.Length; oldIndex++)
            {
                int newIndex = indexMapping[oldIndex];
                
                if (newIndex >= 0) // This item has a mapping
                {
                    // Copy the item
                    newItems[newIndex] = _items[oldIndex];
                    
                    // Set active flag if it was active
                    if (_activeFlags.IsSet(oldIndex))
                    {
                        newActiveFlags.Set(newIndex, true);
                    }
                    else
                    {
                        // This shouldn't happen with our allocation scheme, but handle it just in case
                        newFreeIndices.Add(newIndex);
                    }
                }
            }

            // Add remaining free indices
            for (int i = _activeCount; i < newCapacity; i++)
            {
                if (!newFreeIndices.Contains(i))
                {
                    newFreeIndices.Add(i);
                }
            }

            // Dispose old collections
            _items.Dispose();
            _activeFlags.Dispose();
            _freeIndices.Dispose();

            // Update with the new collections
            _items = newItems;
            _activeFlags = newActiveFlags;
            _freeIndices = newFreeIndices;

            // Clean up temporary collections
            indexMapping.Dispose();
            activeIndices.Dispose();

            // Update last shrink time
            _lastShrinkTime = Time.realtimeSinceStartup;
            
            // Update registry capacity information
            NativePoolRegistry.Instance.UpdateCapacity(_poolId, newCapacity);

            return true;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the pool and releases all unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            // Set disposed flag first to prevent further operations
            _isDisposed = true;
            
            // Unregister from registry
            NativePoolRegistry.Instance.Unregister(_poolId);

            // Dispose all native collections
            if (_items.IsCreated)
            {
                _items.Dispose();
            }

            if (_freeIndices.IsCreated)
            {
                _freeIndices.Dispose();
            }

            if (_activeFlags.IsCreated)
            {
                _activeFlags.Dispose();
            }
        }

        #endregion

        #region IShrinkablePool Implementation

        /// <summary>
        /// Enables automatic shrinking of the pool.
        /// </summary>
        /// <param name="shrinkThreshold">Usage threshold below which shrinking occurs (0-1).</param>
        /// <param name="shrinkInterval">Minimum time in seconds between shrink operations.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if shrinkThreshold is not between 0 and 1 or if shrinkInterval is not positive.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        public void EnableAutoShrink(float shrinkThreshold = 0.3f, float shrinkInterval = 60f)
        {
            if (shrinkThreshold < 0f || shrinkThreshold > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(shrinkThreshold), "Shrink threshold must be between 0 and 1");
            }

            if (shrinkInterval <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(shrinkInterval), "Shrink interval must be positive");
            }

            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            _autoShrinkEnabled = true;
            _shrinkThreshold = shrinkThreshold;
            _shrinkInterval = shrinkInterval;
            _lastShrinkTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Disables automatic shrinking of the pool.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        public void DisableAutoShrink()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            _autoShrinkEnabled = false;
        }

        /// <summary>
        /// Shrinks the pool to the minimum capacity that can still hold all active items.
        /// </summary>
        /// <returns>True if the pool was shrunk, false otherwise.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        public bool ShrinkToMinimum()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            // Calculate new capacity - enough for active items plus a small buffer
            int newCapacity = Math.Max(_activeCount, _minimumCapacity);
            
            // If our current capacity is already at or below the minimum, don't shrink
            if (_items.Length <= newCapacity)
            {
                return false;
            }

            // Resize the pool
            EnsureCapacity(newCapacity);
            
            // Rebuild free indices
            _freeIndices.Clear();
            for (int i = 0; i < _items.Length; i++)
            {
                if (!_activeFlags.IsSet(i))
                {
                    _freeIndices.Add(i);
                }
            }

            // Update last shrink time
            _lastShrinkTime = Time.realtimeSinceStartup;

            return true;
        }

        /// <summary>
        /// Performs auto-shrink check if enough time has elapsed since the last check.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        public void PerformAutoShrinkCheck()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            if (!_autoShrinkEnabled)
            {
                return;
            }

            float timeSinceLastShrink = Time.realtimeSinceStartup - _lastShrinkTime;
            if (timeSinceLastShrink >= _shrinkInterval)
            {
                TryShrink(_shrinkThreshold);
            }
        }

        #endregion

        #region IPoolMetrics Implementation

        /// <summary>
        /// Resets usage metrics (peak count, total created, acquired, released counts).
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
        public void ResetMetrics()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }

            _peakActiveCount = _activeCount;
            _totalCreatedCount = 0;
            _totalAcquiredCount = 0;
            _totalReleasedCount = 0;
        }

        /// <summary>
        /// Gets the current pool usage as a formatted string.
        /// </summary>
        /// <returns>A string containing pool usage information.</returns>
        public string GetMetricsString()
        {
            if (_isDisposed)
            {
                return $"Pool '{Name}' (DISPOSED)";
            }

            return $"Pool '{Name}': {_activeCount}/{_items.Length} active items, {_peakActiveCount} peak, {_totalCreatedCount} created, {_totalAcquiredCount} acquired, {_totalReleasedCount} released";
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// Gets the internal state of the pool for debugging purposes.
        /// </summary>
        /// <returns>A string representation of the pool's internal state.</returns>
        public string GetDebugState()
        {
            if (_isDisposed)
            {
                return $"Pool '{Name}' has been disposed";
            }

            // Build a compact representation of active/inactive items
            var debugFlags = new System.Text.StringBuilder(_items.Length + 20);
            debugFlags.Append("[");
            for (int i = 0; i < _items.Length; i++)
            {
                debugFlags.Append(_activeFlags.IsSet(i) ? "A" : ".");
            }
            debugFlags.Append("]");

            // Create a condensed view of free indices 
            var freeIndicesStr = new System.Text.StringBuilder();
            freeIndicesStr.Append("[");
            for (int i = 0; i < Math.Min(_freeIndices.Length, 10); i++)
            {
                if (i > 0) freeIndicesStr.Append(", ");
                freeIndicesStr.Append(_freeIndices[i]);
            }
            if (_freeIndices.Length > 10)
            {
                freeIndicesStr.Append(", ...");
            }
            freeIndicesStr.Append("]");

            return $"Pool '{Name}': Capacity={_items.Length}, Active={_activeCount}, " +
                   $"Peak={_peakActiveCount}, ThreadingMode={_threadingMode}, " +
                   $"AutoShrink={_autoShrinkEnabled} (Threshold={_shrinkThreshold:F2}, Interval={_shrinkInterval:F1}s), " +
                   $"Layout={debugFlags}, FreeIndices={freeIndicesStr}";
        }

        /// <summary>
        /// Gets a detailed dump of the pool's contents for debugging.
        /// </summary>
        /// <param name="includeItems">Whether to include actual item data in the dump.</param>
        /// <returns>A string containing detailed pool information.</returns>
        public string GetDetailedDump(bool includeItems = false)
        {
            if (_isDisposed)
            {
                return $"Pool '{Name}' has been disposed";
            }

            var dump = new System.Text.StringBuilder();
            dump.AppendLine($"=== POOL DUMP: {Name} ===");
            dump.AppendLine($"Type: {typeof(T).FullName}");
            dump.AppendLine($"Item Size: {UnsafeUtility.SizeOf<T>()} bytes");
            dump.AppendLine($"Capacity: {_items.Length} items");
            dump.AppendLine($"Total Memory: {_items.Length * UnsafeUtility.SizeOf<T>()} bytes");
            dump.AppendLine($"Active Count: {_activeCount} items");
            dump.AppendLine($"Inactive Count: {_items.Length - _activeCount} items");
            dump.AppendLine($"Peak Usage: {_peakActiveCount} items");
            dump.AppendLine($"Total Created: {_totalCreatedCount} items");
            dump.AppendLine($"Total Acquired: {_totalAcquiredCount} operations");
            dump.AppendLine($"Total Released: {_totalReleasedCount} operations");
            dump.AppendLine($"Allocator: {Allocator}");
            dump.AppendLine($"Threading Mode: {_threadingMode}");
            dump.AppendLine($"Auto-Shrink: {(_autoShrinkEnabled ? "Enabled" : "Disabled")}");
            dump.AppendLine($"Shrink Threshold: {_shrinkThreshold:F2} (usage below this triggers shrink)");
            dump.AppendLine($"Shrink Interval: {_shrinkInterval:F1} seconds");
            dump.AppendLine($"Last Shrink Time: {_lastShrinkTime:F1} seconds since startup");
            dump.AppendLine($"Minimum Capacity: {_minimumCapacity} items");
            dump.AppendLine($"Maximum Capacity: {(_config.MaximumCapacity > 0 ? _config.MaximumCapacity.ToString() : "Unlimited")}");
            dump.AppendLine($"Growth Factor: {_growthFactor:F2}x");
            dump.AppendLine($"Reset On Release: {_config.ResetOnRelease}");
            dump.AppendLine($"Use Exponential Growth: {_config.UseExponentialGrowth}");
            dump.AppendLine($"Growth Increment: {_config.GrowthIncrement} items");
            dump.AppendLine();

            // Active flags visualization
            dump.Append("Active Flags: [");
            for (int i = 0; i < _items.Length; i++)
            {
                if (i > 0 && i % 50 == 0)
                {
                    dump.AppendLine();
                    dump.Append("              ");
                }
                dump.Append(_activeFlags.IsSet(i) ? "A" : ".");
            }
            dump.AppendLine("]");
            dump.AppendLine();

            // Free indices list
            dump.Append("Free Indices: [");
            for (int i = 0; i < _freeIndices.Length; i++)
            {
                if (i > 0) dump.Append(", ");
                if (i > 0 && i % 10 == 0)
                {
                    dump.AppendLine();
                    dump.Append("              ");
                }
                dump.Append(_freeIndices[i]);
            }
            dump.AppendLine("]");
            dump.AppendLine();

            // Item data if requested
            if (includeItems)
            {
                dump.AppendLine("Item Data:");
                for (int i = 0; i < _items.Length; i++)
                {
                    string status = _activeFlags.IsSet(i) ? "ACTIVE" : "inactive";
                    dump.AppendLine($"  [{i}] ({status}): {_items[i]}");
                }
            }

            dump.AppendLine("=====================");
            return dump.ToString();
        }

        #endregion
    }
}