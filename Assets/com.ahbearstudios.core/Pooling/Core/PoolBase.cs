using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.DependencyInjection;
using AhBearStudios.Core.Pooling.Interfaces;
using AhBearStudios.Core.Profiling;
using Unity.Profiling;

namespace AhBearStudios.Core.Pooling
{
    /// <summary>
    /// Base implementation for object pools using composition and dependency injection.
    /// Provides core functionality for pool management with full logging and profiling support.
    /// </summary>
    public abstract class PoolBase : IShrinkablePool
    {
        #region Fields

        private readonly IPoolConfig _config;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly Type _itemType;
        
        private UnsafeHashMap<int, bool> _activeItems;
        private float _lastShrinkTime;
        private bool _autoShrinkEnabled;
        private bool _isDisposed;
        
        // Profiler tag
        private ProfilerTag _shrinkTag;
        private ProfilerTag _clearTag;
        private ProfilerTag _expandTag;
        private ProfilerTag _disposeTag;

        #endregion

        #region Properties

        /// <inheritdoc/>
        public Guid Id { get; }

        /// <inheritdoc/>
        public int TotalCount { get; protected set; }

        /// <inheritdoc/>
        public string PoolName { get; private set; }

        /// <inheritdoc/>
        public bool IsDisposed => _isDisposed;

        /// <inheritdoc/>
        public Type ItemType => _itemType;

        /// <inheritdoc/>
        public int ActiveCount { get; protected set; }

        /// <inheritdoc/>
        public int InactiveCount => TotalCount - ActiveCount;

        /// <inheritdoc/>
        public int PeakUsage { get; protected set; }

        /// <inheritdoc/>
        public int TotalCreated { get; protected set; }

        /// <inheritdoc/>
        public bool IsCreated { get; protected set; }

        /// <inheritdoc/>
        public bool SupportsAutoShrink => true;

        /// <inheritdoc/>
        public int MinimumCapacity { get; set; }

        /// <inheritdoc/>
        public int MaximumCapacity { get; set; }

        /// <inheritdoc/>
        public float ShrinkInterval { get; set; }

        /// <inheritdoc/>
        public float GrowthFactor { get; set; }

        /// <inheritdoc/>
        public float ShrinkThreshold { get; set; }

        /// <inheritdoc/>
        public PoolThreadingMode ThreadingMode { get; protected set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the PoolBase class with dependency injection.
        /// </summary>
        /// <param name="config">Pool configuration</param>
        /// <param name="injector">Dependency injector for resolving services</param>
        /// <param name="name">Optional name for the pool</param>
        /// <param name="itemType">Type of items in the pool</param>
        protected PoolBase(IPoolConfig config, IDependencyInjector injector, string name = null, Type itemType = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _itemType = itemType ?? typeof(object);
            
            if (injector == null)
                throw new ArgumentNullException(nameof(injector));
            
            // Resolve dependencies
            _logger = injector.Resolve<IBurstLogger>();
            _profiler = injector.Resolve<IProfiler>();
            
            // Initialize core properties
            Id = Guid.NewGuid();
            PoolName = string.IsNullOrEmpty(name) ? $"Pool_{Id:N}" : name;
            
            // Initialize config-based properties
            MinimumCapacity = _config.MinimumCapacity;
            MaximumCapacity = _config.MaximumCapacity;
            ShrinkInterval = _config.ShrinkInterval;
            GrowthFactor = _config.GrowthFactor;
            ShrinkThreshold = _config.ShrinkThreshold;
            _autoShrinkEnabled = _config.EnableAutoShrink;
            ThreadingMode = PoolThreadingMode.ThreadLocal;
            
            // Initialize tracking collections
            _activeItems = new UnsafeHashMap<int, bool>(64, Allocator.Persistent);
            _lastShrinkTime = Time.realtimeSinceStartup;
            
            // Create profiler tag
            _shrinkTag = new ProfilerTag(new ProfilerCategory("Pooling"), $"{PoolName}.Shrink");
            _clearTag = new ProfilerTag(new ProfilerCategory("Pooling"), $"{PoolName}.Clear");
            _expandTag = new ProfilerTag(new ProfilerCategory("Pooling"), $"{PoolName}.Expand");
            _disposeTag = new ProfilerTag(new ProfilerCategory("Pooling"), $"{PoolName}.Dispose");
            
            IsCreated = true;
            
            _logger.Log(LogLevel.Info, 
                $"Pool '{PoolName}' created with MinCapacity={MinimumCapacity}, MaxCapacity={MaximumCapacity}, ShrinkInterval={ShrinkInterval}s", 
                "Pooling");
        }

        #endregion

        #region IDisposable Implementation

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the pool resources
        /// </summary>
        /// <param name="disposing">Whether disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            using (_profiler?.BeginScope(_disposeTag))
            {
                if (disposing)
                {
                    // Dispose managed resources
                    DisposeItems();
                }
                
                // Dispose unmanaged resources
                if (_activeItems.IsCreated)
                {
                    _activeItems.Dispose();
                }
                
                _logger?.Log(LogLevel.Info, 
                    $"Pool '{PoolName}' disposed. Stats: Created={TotalCreated}, Active={ActiveCount}, Peak={PeakUsage}", 
                    "Pooling");
                
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Disposes all items in the pool
        /// </summary>
        protected abstract void DisposeItems();

        #endregion

        #region IShrinkablePool Implementation

        /// <inheritdoc/>
        public bool TryShrink(float threshold)
        {
            ThrowIfDisposed();
            
            if (!IsCreated)
                return false;
            
            // Calculate current usage ratio
            float usageRatio = TotalCount > 0 ? (float)ActiveCount / TotalCount : 0;
            
            // If usage is below threshold, shrink the pool
            if (usageRatio < threshold)
            {
                // Target a size that's appropriate based on active items
                int targetSize = math.max(MinimumCapacity, 
                    (int)(ActiveCount * (1 + threshold))); // Add some buffer
                
                return ShrinkTo(targetSize);
            }
            
            return false;
        }

        /// <inheritdoc/>
        public bool ShrinkTo(int targetCapacity)
        {
            ThrowIfDisposed();
            
            if (!IsCreated)
                return false;
            
            targetCapacity = math.max(targetCapacity, MinimumCapacity);
            targetCapacity = math.max(targetCapacity, ActiveCount);
            
            if (TotalCount <= targetCapacity)
                return false;
            
            using (_profiler.BeginScope(_shrinkTag))
            {
                int itemsToRemove = TotalCount - targetCapacity;
                int itemsRemoved = ShrinkInternal(itemsToRemove);
                
                if (itemsRemoved > 0)
                {
                    _lastShrinkTime = Time.realtimeSinceStartup;
                    
                    _logger.Log(LogLevel.Debug, 
                        $"Pool '{PoolName}' shrunk: removed {itemsRemoved} items, new size: {TotalCount}", 
                        "Pooling");
                    
                    return true;
                }
                
                return false;
            }
        }

        /// <inheritdoc/>
        public void SetAutoShrink(bool enabled)
        {
            _autoShrinkEnabled = enabled;
            
            if (enabled)
            {
                _lastShrinkTime = Time.realtimeSinceStartup;
            }
            
            _logger.Log(LogLevel.Debug, 
                $"Auto-shrink for pool '{PoolName}' {(enabled ? "enabled" : "disabled")}", 
                "Pooling");
        }

        /// <summary>
        /// Checks and performs automatic shrinking if conditions are met
        /// </summary>
        public void CheckAutoShrink()
        {
            if (!_autoShrinkEnabled || _isDisposed || !IsCreated)
                return;
            
            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - _lastShrinkTime >= ShrinkInterval)
            {
                TryShrink(ShrinkThreshold);
            }
        }

        /// <summary>
        /// Internal implementation of shrinking logic
        /// </summary>
        /// <param name="itemsToRemove">Number of items to remove</param>
        /// <returns>Actual number of items removed</returns>
        protected abstract int ShrinkInternal(int itemsToRemove);

        #endregion

        #region IPool Implementation

        /// <inheritdoc/>
        public Dictionary<string, object> GetMetrics()
        {
            ThrowIfDisposed();
            
            var metrics = new Dictionary<string, object>
            {
                { "Id", Id.ToString() },
                { "PoolName", PoolName },
                { "ItemType", ItemType.Name },
                { "TotalCount", TotalCount },
                { "ActiveCount", ActiveCount },
                { "InactiveCount", InactiveCount },
                { "PeakUsage", PeakUsage },
                { "TotalCreated", TotalCreated },
                { "MinimumCapacity", MinimumCapacity },
                { "MaximumCapacity", MaximumCapacity },
                { "AutoShrink", _autoShrinkEnabled },
                { "ShrinkThreshold", ShrinkThreshold },
                { "ShrinkInterval", ShrinkInterval },
                { "GrowthFactor", GrowthFactor },
                { "ThreadingMode", ThreadingMode.ToString() }
            };
            
            return metrics;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            ThrowIfDisposed();
            
            if (!IsCreated)
                return;
            
            using (_profiler.BeginScope(_clearTag))
            {
                InternalClear();
                
                _logger.Log(LogLevel.Debug, 
                    $"Pool '{PoolName}' cleared. TotalCount={TotalCount}", 
                    "Pooling");
            }
        }

        /// <inheritdoc/>
        public void EnsureCapacity(int capacity)
        {
            ThrowIfDisposed();
            
            if (!IsCreated)
                return;
            
            capacity = math.clamp(capacity, MinimumCapacity, MaximumCapacity);
            
            if (TotalCount >= capacity)
                return;
            
            using (_profiler.BeginScope(_expandTag))
            {
                int additionalCapacity = capacity - TotalCount;
                InternalExpand(additionalCapacity);
                
                _logger.Log(LogLevel.Debug, 
                    $"Pool '{PoolName}' expanded by {additionalCapacity} items. New capacity: {TotalCount}", 
                    "Pooling");
            }
        }

        /// <inheritdoc/>
        public void SetPoolName(string newName)
        {
            if (string.IsNullOrEmpty(newName))
                throw new ArgumentNullException(nameof(newName));
            
            string oldName = PoolName;
            PoolName = newName;
            
            // Update profiler tag
            _shrinkTag = new ProfilerTag(new ProfilerCategory("Pooling"), $"{PoolName}.Shrink");
            _clearTag = new ProfilerTag(new ProfilerCategory("Pooling"), $"{PoolName}.Clear");
            _expandTag = new ProfilerTag(new ProfilerCategory("Pooling"), $"{PoolName}.Expand");
            _disposeTag = new ProfilerTag(new ProfilerCategory("Pooling"), $"{PoolName}.Dispose");
            
            _logger.Log(LogLevel.Debug, 
                $"Pool renamed from '{oldName}' to '{newName}'", 
                "Pooling");
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Internal implementation of clearing logic
        /// </summary>
        protected virtual void InternalClear()
        {
            ActiveCount = 0;
            _activeItems.Clear();
        }

        /// <summary>
        /// Internal implementation of expansion logic
        /// </summary>
        /// <param name="additionalCapacity">Additional capacity needed</param>
        protected abstract void InternalExpand(int additionalCapacity);

        /// <summary>
        /// Marks an item as active
        /// </summary>
        /// <param name="index">Index of the item</param>
        protected void MarkItemActive(int index)
        {
            if (_activeItems.IsCreated)
            {
                _activeItems[index] = true;
            }
            
            ActiveCount++;
            PeakUsage = math.max(PeakUsage, ActiveCount);
        }

        /// <summary>
        /// Marks an item as inactive
        /// </summary>
        /// <param name="index">Index of the item</param>
        protected void MarkItemInactive(int index)
        {
            if (_activeItems.IsCreated)
            {
                _activeItems.Remove(index);
            }
            
            ActiveCount--;
        }

        /// <summary>
        /// Throws if the pool is disposed
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(PoolBase), $"Pool '{PoolName}' has been disposed");
            }
        }

        #endregion
    }
}