using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;
using AhBearStudios.Pooling.Services;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace AhBearStudios.Pooling.Pools.Native
{
    /// <summary>
    /// A high-performance, Burst-compatible native pool implementation designed for
    /// maximum performance with Unity's Burst compiler and job system.
    /// This pool optimizes for Burst performance by avoiding managed references and
    /// ensuring all operations are Burst-compatible.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to pool</typeparam>
    [GenerateTestsForBurstCompatibility]
    public class BurstCompatibleNativePool<T> : IBurstCompatibleNativePool<T>, IDisposable where T : unmanaged
    {
        // Core pool data using Unity Collections v2
        private UnsafeList<T> _items;
        private UnsafeList<int> _activeIndices;
        private UnsafeList<int> _inactiveIndices;
        private UnsafeList<byte> _activeFlags;

        // Safety handles for parallelism and job system compatibility
        private AtomicSafetyHandle _safetyHandle;
        private AtomicSafetyHandle _activeIndicesSafetyHandle;

        // Default value for newly created items
        private readonly T _defaultValue;

        // Configuration
        private readonly BurstCompatiblePoolConfig _config;

        // Pool metrics
        private int _totalCreated;
        private int _peakUsage;
        private bool _isDisposed;
        private float _lastShrinkTime;

        // Services
        private readonly PoolLogger _logger;
        private readonly IPoolDiagnostics _diagnostics;
        
        // Pool ID for registry integration
        private int _poolId;

        /// <inheritdoc />
        public bool IsCreated => _items.IsCreated && !_isDisposed;

        /// <inheritdoc />
        public bool IsDisposed => _isDisposed;

        /// <inheritdoc />
        public int Capacity => _items.IsCreated ? _items.Length : 0;

        /// <inheritdoc />
        public int ActiveCount => _activeIndices.IsCreated ? _activeIndices.Length : 0;

        /// <inheritdoc />
        public int InactiveCount => _inactiveIndices.IsCreated ? _inactiveIndices.Length : 0;

        /// <inheritdoc />
        public int TotalCount => Capacity;

        /// <inheritdoc />
        public int TotalCreated => _totalCreated;

        /// <inheritdoc />
        public int PeakUsage => _peakUsage;

        /// <inheritdoc />
        public Allocator Allocator { get; }

        /// <inheritdoc />
        public Type ItemType => typeof(T);

        /// <inheritdoc />
        public string PoolName { get; }

        /// <inheritdoc />
        public PoolThreadingMode ThreadingMode => PoolThreadingMode.JobCompatible;

        /// <inheritdoc />
        public bool SupportsAutoShrink => _config.EnableAutoShrink;

        /// <inheritdoc />
        public int MinimumCapacity { get; set; }

        /// <inheritdoc />
        public int MaximumCapacity { get; set; }

        /// <inheritdoc />
        public float ShrinkInterval { get; set; }

        /// <inheritdoc />
        public float GrowthFactor { get; set; }

        /// <inheritdoc />
        public float ShrinkThreshold { get; set; }

        /// <inheritdoc />
        public int PeakActiveCount => _peakUsage;

        /// <inheritdoc />
        public int TotalCreatedCount => _totalCreated;

        /// <inheritdoc />
        public int TotalAcquiredCount => _totalCreated;

        /// <inheritdoc />
        public int TotalReleasedCount => _totalCreated - ActiveCount;

        /// <inheritdoc />
        public int CurrentActiveCount => ActiveCount;

        /// <inheritdoc />
        public int CurrentCapacity => Capacity;

        /// <inheritdoc />
        public bool AutoShrinkEnabled => _config.EnableAutoShrink;

        /// <inheritdoc />
        public float LastShrinkTime => _lastShrinkTime;

        /// <summary>
        /// Creates a new BurstCompatibleNativePool with default settings.
        /// </summary>
        public BurstCompatibleNativePool() : this(16, Allocator.Persistent)
        {
        }

        /// <summary>
        /// Creates a new BurstCompatibleNativePool with specified capacity and allocator.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Allocator to use for native collections</param>
        /// <param name="defaultValue">Optional default value for new items</param>
        /// <param name="poolName">Optional custom name for the pool</param>
        public BurstCompatibleNativePool(int initialCapacity, Allocator allocator, T defaultValue = default,
            string poolName = null)
        {
            // Store parameters
            Allocator = allocator;
            _defaultValue = defaultValue;
            PoolName = poolName ?? $"BurstCompatibleNativePool<{typeof(T).Name}>";

            // Create configuration using Burst-optimized settings
            _config = new BurstCompatiblePoolConfig(initialCapacity, allocator)
            {
                ThreadingMode = PoolThreadingMode.JobCompatible,
                UseSafetyChecks = true, // Needed for job compatibility
                EnsureBurstCompatibility = true,
                OptimizeMemoryLayout = true
            };

            // Initialize IShrinkablePool properties
            MinimumCapacity = Math.Max(0, initialCapacity / 2);
            MaximumCapacity = int.MaxValue;
            ShrinkInterval = 60f; // 1 minute
            GrowthFactor = _config.UseExponentialGrowth ? _config.GrowthFactor : 1.5f;
            ShrinkThreshold = 0.25f; // Shrink when usage is below 25%

            // Initialize core collections with Unity Collections v2
            _items = new UnsafeList<T>(initialCapacity, allocator);
            _activeIndices = new UnsafeList<int>(0, allocator);
            _inactiveIndices = new UnsafeList<int>(initialCapacity, allocator);
            _activeFlags = new UnsafeList<byte>(initialCapacity, allocator);

            // Initialize with initial capacity
            _items.Resize(initialCapacity, NativeArrayOptions.ClearMemory);
            _activeFlags.Resize(initialCapacity, NativeArrayOptions.ClearMemory);

            // Populate inactive indices
            for (int i = 0; i < initialCapacity; i++)
            {
                _items[i] = _defaultValue;
                _inactiveIndices.Add(i);
            }

            // Create safety handles for job system compatibility
            if (_config.UseSafetyChecks)
            {
                _safetyHandle = AtomicSafetyHandle.Create();
                _activeIndicesSafetyHandle = AtomicSafetyHandle.Create();

                // Configure safety handle permissions
                AtomicSafetyHandle.SetAllowReadOrWriteAccess(_safetyHandle, true);
                AtomicSafetyHandle.SetAllowReadOrWriteAccess(_activeIndicesSafetyHandle, true);
            }

            // Get services if available
            _logger = PoolingServices.TryGetService<PoolLogger>(out var logger) ? logger : null;
            _diagnostics = PoolingServices.TryGetService<IPoolDiagnostics>(out var diagnostics) ? diagnostics : null;

            // Register with diagnostics
            if (_config.CollectMetrics && _diagnostics != null)
            {
                _diagnostics.RegisterPool(this, PoolName, UnsafeUtility.SizeOf<T>());
            }
            
            // Register with the NativePoolRegistry
            _poolId = NativePoolRegistry.Instance.Register(this).PoolId;

            // Set initial shrink time
            _lastShrinkTime = Time.realtimeSinceStartup;

            if (_config.DetailedLogging && _logger != null)
            {
                _logger.LogInfoInstance(
                    $"BurstCompatibleNativePool created: {PoolName} (Type: {typeof(T).Name}, Initial capacity: {initialCapacity})");
            }
        }

        /// <inheritdoc />
        public T Acquire()
        {
            CheckDisposed();

            // Lock for thread safety
            AtomicSafetyHandle.CheckWriteAndThrow(_safetyHandle);

            // Get an available index, expanding the pool if necessary
            int index = AcquireIndex();

            // Record metrics
            _diagnostics?.RecordAcquireStart(this);

            // Update peak usage
            if (ActiveCount > _peakUsage)
            {
                _peakUsage = ActiveCount;
            }

            // Get the item at the acquired index
            T item = _items[index];

            // Notify diagnostics
            _diagnostics?.RecordAcquireComplete(this, ActiveCount, null);

            return item;
        }

        /// <inheritdoc />
        [BurstCompile]
        public int AcquireIndex()
        {
            CheckDisposed();

            // Lock for thread safety
            AtomicSafetyHandle.CheckWriteAndThrow(_safetyHandle);
            AtomicSafetyHandle.CheckWriteAndThrow(_activeIndicesSafetyHandle);

            int index;

            // If no inactive indices are available, expand the pool
            if (_inactiveIndices.Length == 0)
            {
                // Calculate new capacity
                int newCapacity;
                if (_config.UseExponentialGrowth)
                {
                    newCapacity = Math.Max(_items.Length + 1, (int)(_items.Length * GrowthFactor));
                }
                else
                {
                    newCapacity = _items.Length + _config.GrowthIncrement;
                }

                // Cap at maximum capacity if needed
                if (MaximumCapacity > 0 && newCapacity > MaximumCapacity)
                {
                    if (_config.ThrowIfExceedingMaxCount)
                    {
                        throw new InvalidOperationException(
                            $"Pool {PoolName} reached maximum capacity of {MaximumCapacity}");
                    }

                    newCapacity = MaximumCapacity;
                }

                // Expand collections
                int oldLength = _items.Length;
                _items.Resize(newCapacity, NativeArrayOptions.ClearMemory);
                _activeFlags.Resize(newCapacity, NativeArrayOptions.ClearMemory);

                // Initialize new elements with default value
                for (int i = oldLength; i < newCapacity; i++)
                {
                    _items[i] = _defaultValue;
                    _inactiveIndices.Add(i);
                }
                
                // Update registry capacity information
                NativePoolRegistry.Instance.UpdateCapacity(_poolId, newCapacity);

                if (_config.DetailedLogging && _logger != null)
                {
                    _logger.LogInfoInstance($"Pool {PoolName} expanded from {oldLength} to {newCapacity}");
                }
            }

            // Get the first inactive index and mark it as active
            index = _inactiveIndices[_inactiveIndices.Length - 1];
            _inactiveIndices.RemoveAt(_inactiveIndices.Length - 1);
            _activeIndices.Add(index);

            // Update the active flag
            _activeFlags[index] = 1;

            // Count total created items
            _totalCreated++;
            
            // Update registry active count information
            NativePoolRegistry.Instance.UpdateActiveCount(_poolId, ActiveCount);

            return index;
        }

        /// <inheritdoc />
        public T AcquireAndSetup(Action<T> setupAction)
        {
            CheckDisposed();

            if (setupAction == null)
            {
                return Acquire();
            }

            T item = Acquire();

            try
            {
                // Apply setup action
                setupAction(item);
                return item;
            }
            catch (Exception)
            {
                // If setup fails, release the item and rethrow
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

        /// <inheritdoc />
        public List<T> AcquireMultiple(int count)
        {
            CheckDisposed();

            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
            }

            var items = new List<T>(count);

            try
            {
                for (int i = 0; i < count; i++)
                {
                    items.Add(Acquire());
                }

                return items;
            }
            catch (Exception)
            {
                // If acquiring fails, return any successfully acquired items back to the pool
                foreach (var item in items)
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
        }

        /// <inheritdoc />
        public void Release(T item)
        {
            CheckDisposed();

            // Lock for thread safety
            AtomicSafetyHandle.CheckWriteAndThrow(_safetyHandle);

            // Try to find the item in the active list
            for (int i = 0; i < _activeIndices.Length; i++)
            {
                int index = _activeIndices[i];
                if (EqualityComparer<T>.Default.Equals(_items[index], item))
                {
                    Release(index);
                    _diagnostics?.RecordRelease(this, ActiveCount, null);
                    return;
                }
            }

            if (_config.LogWarnings && _logger != null)
            {
                _logger.LogWarningInstance($"Attempted to release item not found in pool {PoolName}");
            }
        }

        /// <inheritdoc />
        public void ReleaseMultiple(IEnumerable<T> items)
        {
            CheckDisposed();

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (var item in items)
            {
                Release(item);
            }
        }

        /// <inheritdoc />
        [BurstCompile]
        public void Release(int index)
        {
            CheckDisposed();

            // Lock for thread safety
            AtomicSafetyHandle.CheckWriteAndThrow(_safetyHandle);
            AtomicSafetyHandle.CheckWriteAndThrow(_activeIndicesSafetyHandle);

            // Validate index
            if (index < 0 || index >= _items.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");
            }

            // Check if the index is actually active
            if (_activeFlags[index] == 0)
            {
                if (_config.LogWarnings && _logger != null)
                {
                    _logger.LogWarningInstance($"Attempted to release inactive index {index} in pool {PoolName}");
                }

                return;
            }

            // Remove from active indices
            for (int i = 0; i < _activeIndices.Length; i++)
            {
                if (_activeIndices[i] == index)
                {
                    _activeIndices.RemoveAt(i);
                    break;
                }
            }

            // Reset item if configured
            if (_config.ResetOnRelease)
            {
                _items[index] = _defaultValue;
            }

            // Mark as inactive
            _activeFlags[index] = 0;
            _inactiveIndices.Add(index);
            
            // Update registry active count information
            NativePoolRegistry.Instance.UpdateActiveCount(_poolId, ActiveCount);

            // Check if auto-shrink is needed
            if (_config.EnableAutoShrink &&
                Time.realtimeSinceStartup - _lastShrinkTime > ShrinkInterval)
            {
                TryShrink(ShrinkThreshold);
            }

            _diagnostics?.RecordRelease(this, ActiveCount, null);
        }

        /// <inheritdoc />
        [BurstCompile]
        public T GetValue(int index)
        {
            CheckDisposed();

            // Allow read access without exclusive locks
            AtomicSafetyHandle.CheckReadAndThrow(_safetyHandle);

            if (index < 0 || index >= _items.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");
            }

            return _items[index];
        }

        /// <inheritdoc />
        [BurstCompile]
        public void SetValue(int index, T value)
        {
            CheckDisposed();

            // Lock for thread safety
            AtomicSafetyHandle.CheckWriteAndThrow(_safetyHandle);

            if (index < 0 || index >= _items.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");
            }

            // Only allow setting values for active indices
            if (_activeFlags[index] == 0)
            {
                throw new InvalidOperationException(
                    $"Cannot set value for inactive index {index} in pool {PoolName}");
            }

            _items[index] = value;
        }

        /// <inheritdoc />
        [BurstCompile]
        public bool IsIndexActive(int index)
        {
            CheckDisposed();

            // Allow read access without exclusive locks
            AtomicSafetyHandle.CheckReadAndThrow(_safetyHandle);

            if (index < 0 || index >= _items.Length)
            {
                return false;
            }

            return _activeFlags[index] != 0;
        }

        /// <inheritdoc />
        [BurstCompile]
        public T Get(int index)
        {
            return GetValue(index);
        }

        /// <inheritdoc />
        [BurstCompile]
        public void Set(int index, T value)
        {
            SetValue(index, value);
        }

        /// <inheritdoc />
        [BurstCompile]
        public bool IsActive(int index)
        {
            return IsIndexActive(index);
        }

        /// <inheritdoc />
        [BurstCompile]
        public int GetActiveCount()
        {
            CheckDisposed();

            // Allow read access without exclusive locks
            AtomicSafetyHandle.CheckReadAndThrow(_activeIndicesSafetyHandle);

            return _activeIndices.Length;
        }

        /// <inheritdoc />
        public UnsafeList<int> GetActiveIndices(Allocator allocator)
        {
            CheckDisposed();

            // Allow read access without exclusive locks
            AtomicSafetyHandle.CheckReadAndThrow(_activeIndicesSafetyHandle);

            var result = new UnsafeList<int>(_activeIndices.Length, allocator);
            result.AddRange(_activeIndices);
            return result;
        }

        /// <inheritdoc />
        [BurstCompile]
        public int GetActiveIndicesUnsafe(ref UnsafeList<int> indices)
        {
            CheckDisposed();

            // Allow read access without exclusive locks
            AtomicSafetyHandle.CheckReadAndThrow(_activeIndicesSafetyHandle);

            if (!indices.IsCreated)
            {
                throw new ArgumentException("Indices list is not created", nameof(indices));
            }

            indices.Clear();
            indices.AddRange(_activeIndices);
            return _activeIndices.Length;
        }

        /// <inheritdoc />
        [BurstCompile]
        public unsafe int GetActiveIndicesUnsafePtr(int* indicesPtr, int maxLength)
        {
            CheckDisposed();

            // Allow read access without exclusive locks
            AtomicSafetyHandle.CheckReadAndThrow(_activeIndicesSafetyHandle);

            if (indicesPtr == null)
            {
                throw new ArgumentNullException(nameof(indicesPtr));
            }

            int count = Math.Min(_activeIndices.Length, maxLength);

            for (int i = 0; i < count; i++)
            {
                indicesPtr[i] = _activeIndices[i];
            }

            return count;
        }

        /// <summary>
        /// Gets a handle that provides direct access to the pool for use in jobs.
        /// Uses the registry-based handle for job and Burst compatibility.
        /// </summary>
        /// <returns>A handle for using the pool in jobs</returns>
        [BurstCompile]
        public NativePoolHandle GetHandle()
        {
            CheckDisposed();
            return new NativePoolHandle(_poolId);
        }

        /// <summary>
        /// Gets a read-only handle that provides direct access to the pool for use in jobs.
        /// Only allows reading of pool data, for thread-safe parallel operations.
        /// </summary>
        /// <returns>A read-only handle for using the pool in jobs</returns>
        [BurstCompile]
        public NativePoolReadHandle GetReadOnlyHandle()
        {
            CheckDisposed();
            return new NativePoolReadHandle(_poolId);
        }

        /// <inheritdoc />
        public void Clear()
        {
            CheckDisposed();

            // Lock for thread safety
            AtomicSafetyHandle.CheckWriteAndThrow(_safetyHandle);
            AtomicSafetyHandle.CheckWriteAndThrow(_activeIndicesSafetyHandle);

            // Reset active flags
            for (int i = 0; i < _activeFlags.Length; i++)
            {
                _activeFlags[i] = 0;
            }

            // Clear active indices
            _activeIndices.Clear();

            // Refill inactive indices
            _inactiveIndices.Clear();
            for (int i = 0; i < _items.Length; i++)
            {
                _inactiveIndices.Add(i);

                // Reset items if needed
                if (_config.ResetOnRelease)
                {
                    _items[i] = _defaultValue;
                }
            }
            
            // Update registry active count information
            NativePoolRegistry.Instance.UpdateActiveCount(_poolId, 0);

            if (_config.DetailedLogging && _logger != null)
            {
                _logger.LogInfoInstance($"Pool {PoolName} cleared");
            }
        }

        /// <inheritdoc />
        public void EnsureCapacity(int capacity)
        {
            CheckDisposed();

            // Lock for thread safety
            AtomicSafetyHandle.CheckWriteAndThrow(_safetyHandle);
            AtomicSafetyHandle.CheckWriteAndThrow(_activeIndicesSafetyHandle);

            if (capacity <= _items.Length)
            {
                return;
            }

            int oldLength = _items.Length;

            // Resize collections
            _items.Resize(capacity, NativeArrayOptions.ClearMemory);
            _activeFlags.Resize(capacity, NativeArrayOptions.ClearMemory);

            // Initialize new elements
            for (int i = oldLength; i < capacity; i++)
            {
                _items[i] = _defaultValue;
                _inactiveIndices.Add(i);
            }
            
            // Update registry capacity information
            NativePoolRegistry.Instance.UpdateCapacity(_poolId, capacity);

            if (_config.DetailedLogging && _logger != null)
            {
                _logger.LogInfoInstance($"Pool {PoolName} capacity ensured from {oldLength} to {capacity}");
            }
        }

        /// <inheritdoc />
        public Dictionary<string, object> GetMetrics()
        {
            if (_isDisposed)
            {
                return new Dictionary<string, object>();
            }

            return new Dictionary<string, object>
            {
                ["PoolName"] = PoolName,
                ["Type"] = typeof(T).Name,
                ["ItemSize"] = UnsafeUtility.SizeOf<T>(),
                ["Capacity"] = Capacity,
                ["ActiveCount"] = ActiveCount,
                ["InactiveCount"] = InactiveCount,
                ["PeakUsage"] = PeakUsage,
                ["TotalCreated"] = TotalCreated,
                ["UtilizationRatio"] = Capacity > 0 ? (float)ActiveCount / Capacity : 0f,
                ["IsNative"] = true,
                ["Allocator"] = Allocator.ToString(),
                ["ThreadingMode"] = ThreadingMode.ToString(),
                ["CollectionDate"] = DateTime.UtcNow,
                ["IsJobCompatible"] = true,
                ["IsBurstCompatible"] = true,
                ["UsesSafetyHandles"] = _config.UseSafetyChecks,
                ["BurstOptimized"] = true,
                ["PoolId"] = _poolId,
                ["EstimatedMemoryUsageBytes"] =
                    UnsafeUtility.SizeOf<T>() * Capacity +
                    sizeof(int) * (_activeIndices.Length + _inactiveIndices.Length) +
                    _activeFlags.Length
            };
        }

        /// <inheritdoc />
        [BurstCompile]
        public bool TryShrink(float threshold)
        {
            CheckDisposed();

            // Lock for thread safety
            AtomicSafetyHandle.CheckWriteAndThrow(_safetyHandle);
            AtomicSafetyHandle.CheckWriteAndThrow(_activeIndicesSafetyHandle);

            if (!_config.EnableAutoShrink || ActiveCount == 0 || Capacity <= MinimumCapacity)
            {
                return false;
            }

            float utilization = (float)ActiveCount / Capacity;

            // Only shrink if utilization is below threshold
            if (utilization > threshold)
            {
                return false;
            }

            // Calculate target capacity based on active count and growth factor
            int targetCapacity = Math.Max(
                MinimumCapacity,
                (int)(ActiveCount * GrowthFactor)
            );

            if (targetCapacity >= Capacity)
            {
                return false;
            }

            return ShrinkTo(targetCapacity);
        }

        /// <inheritdoc />
        [BurstCompile]
        public bool ShrinkTo(int targetCapacity)
        {
            CheckDisposed();

            // Lock for thread safety
            AtomicSafetyHandle.CheckWriteAndThrow(_safetyHandle);
            AtomicSafetyHandle.CheckWriteAndThrow(_activeIndicesSafetyHandle);

            // Can't shrink below minimum capacity or active count
            targetCapacity = Math.Max(targetCapacity, MinimumCapacity);
            targetCapacity = Math.Max(targetCapacity, ActiveCount);

            if (targetCapacity >= Capacity)
            {
                return false;
            }

            // Create new collections with target capacity
            var newItems = new UnsafeList<T>(targetCapacity, Allocator);
            var newActiveFlags = new UnsafeList<byte>(targetCapacity, Allocator);

            newItems.Resize(targetCapacity, NativeArrayOptions.ClearMemory);
            newActiveFlags.Resize(targetCapacity, NativeArrayOptions.ClearMemory);

            // Copy active items to the new list
            for (int i = 0; i < _activeIndices.Length; i++)
            {
                int oldIndex = _activeIndices[i];
                int newIndex = i;

                newItems[newIndex] = _items[oldIndex];
                newActiveFlags[newIndex] = 1;

                // Update the active index
                _activeIndices[i] = newIndex;
            }

            // Initialize inactive items
            for (int i = _activeIndices.Length; i < targetCapacity; i++)
            {
                newItems[i] = _defaultValue;
            }

            // Rebuild inactive indices
            _inactiveIndices.Clear();
            for (int i = _activeIndices.Length; i < targetCapacity; i++)
            {
                _inactiveIndices.Add(i);
            }

            // Dispose old collections
            _items.Dispose();
            _activeFlags.Dispose();

            // Assign new collections
            _items = newItems;
            _activeFlags = newActiveFlags;

            // Update shrink time
            _lastShrinkTime = Time.realtimeSinceStartup;
            
            // Update registry capacity information
            NativePoolRegistry.Instance.UpdateCapacity(_poolId, targetCapacity);

            if (_config.DetailedLogging && _logger != null)
            {
                _logger.LogInfoInstance($"Pool {PoolName} shrunk from {Capacity} to {targetCapacity}");
            }

            // Notify diagnostics
            _diagnostics?.RecordShrink(this, Capacity - targetCapacity);

            return true;
        }

        /// <inheritdoc />
        public void SetAutoShrink(bool enabled)
        {
            CheckDisposed();

            if (_config is { } config)
            {
                config.EnableAutoShrink = enabled;
            }
        }

        /// <inheritdoc />
        public void ResetMetrics()
        {
            CheckDisposed();

            _peakUsage = ActiveCount;
            _totalCreated = ActiveCount;
        }

        /// <summary>
        /// Checks if this pool has been disposed and throws an exception if it has.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed</exception>
        public void CheckDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(PoolName);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_isDisposed)
            {
                // Unregister from registry
                NativePoolRegistry.Instance.Unregister(_poolId);
                
                // Dispose collections
                if (_items.IsCreated) _items.Dispose();
                if (_activeIndices.IsCreated) _activeIndices.Dispose();
                if (_inactiveIndices.IsCreated) _inactiveIndices.Dispose();
                if (_activeFlags.IsCreated) _activeFlags.Dispose();

                // Dispose safety handles
                if (_config.UseSafetyChecks)
                {
                    AtomicSafetyHandle.Release(_safetyHandle);
                    AtomicSafetyHandle.Release(_activeIndicesSafetyHandle);
                }

                // Unregister from diagnostics
                _diagnostics?.UnregisterPool(this);

                if (_config.DetailedLogging && _logger != null)
                {
                    _logger.LogInfoInstance($"Pool {PoolName} disposed");
                }

                _isDisposed = true;
            }
        }
    }
}