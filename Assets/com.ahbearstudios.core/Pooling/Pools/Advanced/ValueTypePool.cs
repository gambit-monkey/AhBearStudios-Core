using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;
using AhBearStudios.Pooling.Services;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Pooling.Pools.Advanced
{
    /// <summary>
    /// A high-performance pool for value types that minimizes GC allocations.
    /// Implements shrinking capabilities and comprehensive metrics tracking.
    /// </summary>
    /// <typeparam name="T">The value type to pool</typeparam>
    public struct ValueTypePool<T> : IPool<T>, IShrinkablePool, IPoolMetrics, IDisposable
        where T : struct
    {
        // Factory for creating new values
        private Func<T> _valueFactory;
        
        // Pool storage
        private T[] _items;
        private int _activeCount;
        private bool _isCreated;
        private bool _isDisposed;
        
        // Tracking and metrics
        private int _totalCreated;
        private int _peakActiveCount;
        private int _totalAcquiredCount;
        private int _totalReleasedCount;
        
        // Configuration
        private int _minimumCapacity;
        private int _maximumCapacity;
        private float _shrinkInterval;
        private float _lastShrinkTime;
        private float _growthFactor;
        private float _shrinkThreshold;
        private bool _enableAutoShrink;
        private readonly string _poolName;
        private readonly PoolThreadingMode _threadingMode;
        
        // Services
        private readonly PoolLogger _logger;
        private readonly IPoolDiagnostics _diagnostics;
        
        /// <summary>
        /// Creates a new value type pool
        /// </summary>
        /// <param name="valueFactory">Factory function to create new values</param>
        /// <param name="config">Optional pool configuration</param>
        /// <param name="poolName">Optional name for the pool, useful for diagnostics</param>
        public ValueTypePool(Func<T> valueFactory, PoolConfig config = null, string poolName = null)
        {
            if (valueFactory == null)
                throw new ArgumentNullException(nameof(valueFactory));
                
            _valueFactory = valueFactory;
            _poolName = poolName ?? $"ValueTypePool<{typeof(T).Name}>";
            
            // Initialize with empty array
            _items = Array.Empty<T>();
            _activeCount = 0;
            _isCreated = true;
            _isDisposed = false;
            
            // Initialize metrics
            _totalCreated = 0;
            _peakActiveCount = 0;
            _totalAcquiredCount = 0;
            _totalReleasedCount = 0;
            
            // Apply configuration
            var actualConfig = config ?? new PoolConfig();
            int initialCapacity = Math.Max(0, actualConfig.InitialCapacity);
            _minimumCapacity = initialCapacity;
            _maximumCapacity = actualConfig.MaximumCapacity;
            _shrinkInterval = actualConfig.ShrinkInterval;
            _lastShrinkTime = Time.realtimeSinceStartup;
            _growthFactor = actualConfig.GrowthFactor;
            _shrinkThreshold = actualConfig.ShrinkThreshold;
            _enableAutoShrink = actualConfig.EnableAutoShrink;
            _threadingMode = actualConfig.ThreadingMode;
            
            // Get services
            _logger = PoolingServices.HasService<PoolLogger>() 
                ? PoolingServices.GetService<PoolLogger>() 
                : null;
                
            _diagnostics = PoolingServices.HasService<IPoolDiagnostics>() 
                ? PoolingServices.GetService<IPoolDiagnostics>() 
                : null;
            
            // Register with diagnostics if available
            _diagnostics?.RegisterPool(this, _poolName);
            
            // Initialize the pool
            if (initialCapacity > 0)
            {
                EnsureCapacity(initialCapacity);
                
                if (actualConfig.PrewarmOnInit)
                {
                    PrewarmPool(initialCapacity);
                }
            }
            
            _logger?.LogDebugInstance($"Created {_poolName} with initial capacity {initialCapacity}");
        }
        
        #region IPool Implementation
        
        /// <inheritdoc/>
        public bool IsCreated => _isCreated;
        
        /// <inheritdoc/>
        public int TotalCount => _items?.Length ?? 0;
        
        /// <inheritdoc/>
        public int ActiveCount => _activeCount;
        
        /// <inheritdoc/>
        public int InactiveCount => (_items?.Length ?? 0) - _activeCount;
        
        /// <inheritdoc/>
        public int PeakUsage => _peakActiveCount;
        
        /// <inheritdoc/>
        public int TotalCreated => _totalCreated;
        
        /// <inheritdoc/>
        public Type ItemType => typeof(T);
        
        /// <inheritdoc/>
        public bool IsDisposed => _isDisposed;
        
        /// <inheritdoc/>
        public string PoolName => _poolName;
        
        /// <inheritdoc/>
        public PoolThreadingMode ThreadingMode => _threadingMode;
        
        /// <inheritdoc/>
        public void Clear()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);
            
            // Reset active count
            _activeCount = 0;
            
            _logger?.LogDebugInstance($"Cleared {_poolName}");
        }
        
        /// <inheritdoc/>
        public void EnsureCapacity(int capacity)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);
            
            if (capacity <= (_items?.Length ?? 0))
                return;
                
            // If we have a maximum capacity, enforce it
            if (_maximumCapacity > 0)
            {
                capacity = Math.Min(capacity, _maximumCapacity);
            }
            
            // Allocate new array and copy existing items
            T[] newItems = new T[capacity];
            if (_items != null && _items.Length > 0)
            {
                Array.Copy(_items, newItems, Math.Min(_items.Length, capacity));
            }
            
            // Fill remaining slots with new instances
            for (int i = (_items?.Length ?? 0); i < capacity; i++)
            {
                newItems[i] = CreateNewItem();
            }
            
            _items = newItems;
            
            _logger?.LogDebugInstance($"Expanded {_poolName} to capacity {capacity}");
        }
        
        /// <inheritdoc/>
        public Dictionary<string, object> GetMetrics()
        {
            if (_isDisposed)
                return new Dictionary<string, object>();
                
            var metrics = new Dictionary<string, object>
            {
                { "PoolName", _poolName },
                { "ItemType", typeof(T).Name },
                { "IsDisposed", _isDisposed },
                { "TotalCount", TotalCount },
                { "ActiveCount", ActiveCount },
                { "InactiveCount", InactiveCount },
                { "PeakActiveCount", _peakActiveCount },
                { "TotalCreatedCount", _totalCreated },
                { "TotalAcquiredCount", _totalAcquiredCount },
                { "TotalReleasedCount", _totalReleasedCount },
                { "CurrentCapacity", TotalCount },
                { "SupportsAutoShrink", SupportsAutoShrink },
                { "MinimumCapacity", _minimumCapacity },
                { "MaximumCapacity", _maximumCapacity },
                { "ShrinkInterval", _shrinkInterval },
                { "GrowthFactor", _growthFactor },
                { "ShrinkThreshold", _shrinkThreshold },
                { "ThreadingMode", _threadingMode.ToString() }
            };
            
            return metrics;
        }
        
        #endregion
        
        #region IPool<T> Implementation
        
        /// <inheritdoc/>
        public T Acquire()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);
                
            _diagnostics?.RecordAcquireStart(this);
            
            // If we have inactive items, return one
            if (_activeCount < (_items?.Length ?? 0))
            {
                T value = _items[_activeCount];
                _activeCount++;
                
                // Update tracking
                _totalAcquiredCount++;
                _peakActiveCount = Math.Max(_peakActiveCount, _activeCount);
                
                _diagnostics?.RecordAcquireComplete(this, _activeCount, null);
                
                return value;
            }
            
            // If we're here, we need to expand the pool
            int newCapacity = (_items?.Length ?? 0) > 0 
                ? (int)(_items.Length * _growthFactor)
                : Math.Max(4, _minimumCapacity);
                
            // If we have a maximum capacity, enforce it
            if (_maximumCapacity > 0 && newCapacity > _maximumCapacity)
            {
                newCapacity = _maximumCapacity;
                
                // If we're already at max capacity, we need to return a new item without storing it
                if (_activeCount >= newCapacity)
                {
                    _logger?.LogWarningInstance($"{_poolName} has reached maximum capacity of {_maximumCapacity}");
                    
                    T value = CreateNewItem();
                    _totalAcquiredCount++;
                    
                    _diagnostics?.RecordAcquireComplete(this, _activeCount, null);
                    
                    return value;
                }
            }
            
            EnsureCapacity(newCapacity);
            
            // Now we can return an item
            T item = _items[_activeCount];
            _activeCount++;
            
            // Update tracking
            _totalAcquiredCount++;
            _peakActiveCount = Math.Max(_peakActiveCount, _activeCount);
            
            _diagnostics?.RecordAcquireComplete(this, _activeCount, null);
            
            return item;
        }
        
        /// <inheritdoc/>
        public void Release(T value)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);
            
            // If there are no active items, ignore this call
            if (_activeCount <= 0)
            {
                _logger?.LogWarningInstance($"Attempted to release an item to {_poolName} when no items are active");
                return;
            }
            
            // Move the last active item to the released position
            _activeCount--;
            _items[_activeCount] = value;
            
            // Update tracking
            _totalReleasedCount++;
            
            _diagnostics?.RecordRelease(this, _activeCount, null);
            
            // Check if we should auto-shrink
            if (_enableAutoShrink && 
                Time.realtimeSinceStartup - _lastShrinkTime > _shrinkInterval)
            {
                TryShrink(_shrinkThreshold);
            }
        }
        
        /// <inheritdoc/>
        public List<T> AcquireMultiple(int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
                
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);
            
            var results = new List<T>(count);
            
            for (int i = 0; i < count; i++)
            {
                results.Add(Acquire());
            }
            
            return results;
        }
        
        /// <inheritdoc/>
        public void ReleaseMultiple(IEnumerable<T> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
                
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);
            
            foreach (var value in values)
            {
                Release(value);
            }
        }
        
        /// <inheritdoc/>
        public T AcquireAndSetup(Action<T> setupAction)
        {
            T value = Acquire();
            
            setupAction?.Invoke(value);
            
            return value;
        }
        
        #endregion
        
        #region IShrinkablePool Implementation
        
        /// <inheritdoc/>
        public bool SupportsAutoShrink => true;
        
        /// <inheritdoc/>
        public int MinimumCapacity 
        {
            get => _minimumCapacity;
            set => _minimumCapacity = Math.Max(0, value);
        }
        
        /// <inheritdoc/>
        public int MaximumCapacity 
        {
            get => _maximumCapacity;
            set => _maximumCapacity = value <= 0 ? 0 : Math.Max(_minimumCapacity, value);
        }
        
        /// <inheritdoc/>
        public float ShrinkInterval 
        {
            get => _shrinkInterval;
            set => _shrinkInterval = Math.Max(0, value);
        }
        
        /// <inheritdoc/>
        public float GrowthFactor 
        {
            get => _growthFactor;
            set => _growthFactor = Math.Max(1.1f, value);
        }
        
        /// <inheritdoc/>
        public float ShrinkThreshold 
        {
            get => _shrinkThreshold;
            set => _shrinkThreshold = Math.Clamp(value, 0.1f, 0.9f);
        }
        
        /// <inheritdoc/>
        public bool TryShrink(float threshold)
        {
            if (_isDisposed)
                return false;
                
            if (_items == null || _items.Length <= _minimumCapacity)
                return false;
            
            // Calculate usage
            float usage = (_items.Length > 0) ? (float)_activeCount / _items.Length : 0;
            
            // If usage is above threshold, don't shrink
            if (usage >= threshold)
                return false;
            
            // Calculate target capacity, ensuring it's at least the minimum
            int targetCapacity = Math.Max(
                _minimumCapacity,
                (int)Math.Ceiling(_activeCount / threshold)
            );
            
            // If target is already smaller than or equal to current, nothing to do
            if (targetCapacity >= _items.Length)
                return false;
            
            return ShrinkTo(targetCapacity);
        }
        
        /// <inheritdoc/>
        public void SetAutoShrink(bool enabled)
        {
            _enableAutoShrink = enabled;
            
            // If enabling, reset the last shrink time
            if (enabled)
            {
                _lastShrinkTime = Time.realtimeSinceStartup;
            }
        }
        
        /// <inheritdoc/>
        public bool ShrinkTo(int targetCapacity)
        {
            if (_isDisposed)
                return false;
                
            if (_items == null || _items.Length <= _minimumCapacity)
                return false;
            
            // Ensure we don't shrink below minimum or active count
            targetCapacity = Math.Max(targetCapacity, _minimumCapacity);
            targetCapacity = Math.Max(targetCapacity, _activeCount);
            
            // If we're not actually shrinking, return false
            if (targetCapacity >= _items.Length)
                return false;
            
            // Create new array with target capacity
            T[] newItems = new T[targetCapacity];
            
            // Copy active items
            Array.Copy(_items, newItems, _activeCount);
            
            // Replace the items array
            _items = newItems;
            
            // Update last shrink time
            _lastShrinkTime = Time.realtimeSinceStartup;
            
            _logger?.LogDebugInstance($"Shrunk {_poolName} from {_items.Length} to {targetCapacity}");
            
            return true;
        }
        
        #endregion
        
        #region IPoolMetrics Implementation
        
        /// <inheritdoc/>
        public int PeakActiveCount => _peakActiveCount;
        
        /// <inheritdoc/>
        public int TotalCreatedCount => _totalCreated;
        
        /// <inheritdoc/>
        public int TotalAcquiredCount => _totalAcquiredCount;
        
        /// <inheritdoc/>
        public int TotalReleasedCount => _totalReleasedCount;
        
        /// <inheritdoc/>
        public int CurrentActiveCount => _activeCount;
        
        /// <inheritdoc/>
        public int CurrentCapacity => _items?.Length ?? 0;
        
        /// <inheritdoc/>
        public void ResetMetrics()
        {
            _peakActiveCount = _activeCount;
            _totalAcquiredCount = 0;
            _totalReleasedCount = 0;
            // Note: We don't reset _totalCreated as that should reflect the lifetime of the pool
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isDisposed)
                return;
            
            _isDisposed = true;
            _isCreated = false;
            _items = null;
            _valueFactory = null;
            
            // Unregister from diagnostics if available
            _diagnostics?.UnregisterPool(this);
            
            _logger?.LogDebugInstance($"Disposed {_poolName}");
        }
        
        #endregion
        
        #region Additional Methods
        
        /// <summary>
        /// Prewarms the pool by creating the specified number of items
        /// </summary>
        /// <param name="count">Number of items to prewarm</param>
        public void PrewarmPool(int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);
                
            if (count <= 0)
                return;
            
            // Ensure we have capacity for the items
            EnsureCapacity(count);
            
            _logger?.LogDebugInstance($"Prewarmed {_poolName} with {count} items");
        }
        
        /// <summary>
        /// Creates a new item using the factory
        /// </summary>
        /// <returns>A new item</returns>
        private T CreateNewItem()
        {
            if (_valueFactory == null)
                throw new InvalidOperationException("Value factory is null");
            
            _totalCreated++;
            _diagnostics?.RecordCreate(this);
            
            return _valueFactory();
        }
        
        /// <summary>
        /// Gets the active values as a NativeArray
        /// </summary>
        /// <param name="allocator">Allocator to use for the NativeArray</param>
        /// <returns>NativeArray containing the active values</returns>
        public NativeArray<T> GetActiveValuesAsNativeArray(Allocator allocator)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);
                
            var array = new NativeArray<T>(_activeCount, allocator, NativeArrayOptions.UninitializedMemory);
            
            if (_activeCount > 0)
            {
                // Copy only the active values
                for (int i = 0; i < _activeCount; i++)
                {
                    array[i] = _items[i];
                }
            }
            
            return array;
        }
        
        /// <summary>
        /// Gets the inactive values as a NativeArray
        /// </summary>
        /// <param name="allocator">Allocator to use for the NativeArray</param>
        /// <returns>NativeArray containing the inactive values</returns>
        public NativeArray<T> GetInactiveValuesAsNativeArray(Allocator allocator)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);
                
            int inactiveCount = (_items?.Length ?? 0) - _activeCount;
            
            var array = new NativeArray<T>(inactiveCount, allocator, NativeArrayOptions.UninitializedMemory);
            
            if (inactiveCount > 0)
            {
                // Copy only the inactive values
                for (int i = 0; i < inactiveCount; i++)
                {
                    array[i] = _items[_activeCount + i];
                }
            }
            
            return array;
        }
        
        /// <summary>
        /// Gets all pool values as a NativeArray
        /// </summary>
        /// <param name="allocator">Allocator to use for the NativeArray</param>
        /// <returns>NativeArray containing all values</returns>
        public NativeArray<T> ToNativeArray(Allocator allocator)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);
                
            if (_items == null || _items.Length == 0)
                return new NativeArray<T>(0, allocator);
            
            var array = new NativeArray<T>(_items.Length, allocator, NativeArrayOptions.UninitializedMemory);
            
            // Copy all values
            for (int i = 0; i < _items.Length; i++)
            {
                array[i] = _items[i];
            }
            
            return array;
        }
        
        /// <summary>
        /// Acquires items directly into a pre-allocated NativeArray
        /// </summary>
        /// <param name="array">The NativeArray to fill</param>
        /// <param name="startIndex">The starting index in the array</param>
        /// <param name="count">The number of items to acquire</param>
        /// <returns>The number of items actually acquired</returns>
        public int AcquireToNativeArray(NativeArray<T> array, int startIndex, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(_poolName);
                
            if (!array.IsCreated)
                throw new ArgumentException("NativeArray is not created", nameof(array));
                
            if (startIndex < 0 || startIndex >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
                
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
                
            if (startIndex + count > array.Length)
                count = array.Length - startIndex;
            
            // Determine how many items we can actually acquire
            int available = Math.Min(count, (_items?.Length ?? 0) - _activeCount);
            int needToCreate = count - available;
            
            // If we have a maximum capacity, we need to check if we can create more
            if (_maximumCapacity > 0 && needToCreate > 0)
            {
                int canCreate = Math.Max(0, _maximumCapacity - (_items?.Length ?? 0));
                needToCreate = Math.Min(needToCreate, canCreate);
            }
            
            // Acquire existing items
            for (int i = 0; i < available; i++)
            {
                array[startIndex + i] = _items[_activeCount];
                _activeCount++;
                _totalAcquiredCount++;
            }
            
            // Create new items if needed
            if (needToCreate > 0)
            {
                // Expand the pool
                int newCapacity = (_items?.Length ?? 0) + needToCreate;
                newCapacity = Math.Min(newCapacity, _maximumCapacity > 0 ? _maximumCapacity : newCapacity);
                
                EnsureCapacity(newCapacity);
                
                // Now acquire the newly created items
                int additionalItems = Math.Min(needToCreate, (_items?.Length ?? 0) - _activeCount);
                
                for (int i = 0; i < additionalItems; i++)
                {
                    array[startIndex + available + i] = _items[_activeCount];
                    _activeCount++;
                    _totalAcquiredCount++;
                }
                
                available += additionalItems;
            }
            
            // Update peak tracking
            _peakActiveCount = Math.Max(_peakActiveCount, _activeCount);
            
            _diagnostics?.RecordAcquireComplete(this, _activeCount, null);
            
            return available;
        }
        
        #endregion
    }
}