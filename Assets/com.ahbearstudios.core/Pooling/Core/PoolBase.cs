using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Configurations;
using Unity.Collections;
using Unity.Mathematics;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;
using AhBearStudios.Pooling.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace AhBearStudios.Pooling.Pools
{
    /// <summary>
    /// A modern, optimized pool implementation that serves as a base for specialized pool types.
    /// Uses composition over inheritance and provides thread-safety with Collections v2.
    /// </summary>
    public class PoolBase : IPool, IShrinkablePool, IDisposable
    {
        #region Fields and Properties

        private readonly IPoolConfig _config;
        private readonly IPoolLogger _logger;
        private readonly IPoolProfiler _profiler;
        private readonly IPoolDiagnostics _diagnostics;
        private readonly IPoolRegistry _registry;
        private readonly Type _itemType;

        private UnsafeParallelHashMap<int, bool> _activeItems;
        private float _lastShrinkTime;
        private bool _autoShrinkEnabled;
        private bool _isDisposed;

        /// <summary>
        /// Gets the unique identifier for this pool
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets the total number of items in the pool (active + inactive)
        /// </summary>
        public int TotalCount { get; protected set; }

        /// <summary>
        /// Gets the descriptive name of this pool
        /// </summary>
        public string PoolName { get; private set; }

        /// <summary>
        /// Gets whether this pool has been disposed
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Gets the type of items in the pool
        /// </summary>
        public Type ItemType => _itemType;

        /// <summary>
        /// Gets the number of active items
        /// </summary>
        public int ActiveCount { get; protected set; }

        /// <summary>
        /// Gets the number of inactive items
        /// </summary>
        public int InactiveCount => TotalCount - ActiveCount;

        /// <summary>
        /// Gets the peak number of simultaneously active items
        /// </summary>
        public int PeakUsage { get; protected set; }

        /// <summary>
        /// Gets the total number of items ever created by this pool
        /// </summary>
        public int TotalCreated { get; protected set; }

        /// <summary>
        /// Gets whether this pool has been properly created and initialized
        /// </summary>
        public bool IsCreated { get; protected set; }

        /// <summary>
        /// Gets the number of free items available in the pool
        /// </summary>
        public int FreeCount => TotalCount - ActiveCount;

        /// <summary>
        /// Gets whether the pool supports automatic shrinking
        /// </summary>
        public bool SupportsAutoShrink => true;

        /// <summary>
        /// Gets or sets the minimum capacity that the pool will maintain even when shrinking
        /// </summary>
        public int MinimumCapacity { get; set; }

        /// <summary>
        /// Gets or sets the maximum capacity that the pool can grow to
        /// </summary>
        public int MaximumCapacity { get; set; }

        /// <summary>
        /// Gets or sets the shrink interval in seconds.
        /// </summary>
        public float ShrinkInterval { get; set; }

        /// <summary>
        /// Gets or sets the growth factor when the pool needs to expand.
        /// </summary>
        public float GrowthFactor { get; set; }

        /// <summary>
        /// Gets or sets the shrink threshold.
        /// </summary>
        public float ShrinkThreshold { get; set; }

        /// <summary>
        /// Gets the threading mode for this pool.
        /// </summary>
        public PoolThreadingMode ThreadingMode => PoolThreadingMode.ThreadLocal;

        #endregion

        #region Constructor and Initialization

        /// <summary>
        /// Initializes a new instance of the PoolBase class with the specified configuration and services.
        /// </summary>
        /// <param name="config">Pool configuration</param>
        /// <param name="serviceLocator">Service locator for dependencies</param>
        /// <param name="name">Optional name for the pool</param>
        /// <param name="itemType">Type of items in the pool</param>
        public PoolBase(IPoolConfig config, IPoolingServiceLocator serviceLocator = null, string name = null, Type itemType = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _itemType = itemType ?? typeof(object);

            // Get services from service locator
            if (serviceLocator != null)
            {
                _logger = serviceLocator.GetService<IPoolLogger>();
                _profiler = serviceLocator.GetService<IPoolProfiler>();
                _diagnostics = serviceLocator.GetService<IPoolDiagnostics>();
                _registry = serviceLocator.GetService<IPoolRegistry>();
            }

            // Initialize core properties
            Id = Guid.NewGuid();
            PoolName = string.IsNullOrEmpty(name) ? $"Pool_{Id}" : name;
            
            // Initialize config-based properties
            MinimumCapacity = _config.MinimumCapacity;
            MaximumCapacity = _config.MaximumCapacity;
            ShrinkInterval = _config.ShrinkInterval;
            GrowthFactor = _config.GrowthFactor;
            ShrinkThreshold = _config.ShrinkThreshold;
            _autoShrinkEnabled = _config.EnableAutoShrink;

            // Initialize tracking collections
            _activeItems = new UnsafeParallelHashMap<int, bool>(64, Allocator.Persistent);
            _lastShrinkTime = Time.realtimeSinceStartup;
            
            // Register with diagnostics if available
            _diagnostics?.RegisterPool(this, PoolName);
            
            // Register with registry if available
            _registry?.RegisterPool(this, PoolName);
            
            IsCreated = true;
            
            _logger?.LogInfoInstance($"Pool '{PoolName}' created with MinCapacity={MinimumCapacity}, MaxCapacity={MaximumCapacity}, ShrinkInterval={ShrinkInterval}s");
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of the pool, cleaning up all unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            // Use profiler if available
            using (_profiler?.Sample("Dispose", Id, PoolName, ActiveCount, FreeCount))
            {
                // Record disposal with diagnostics
                if (_diagnostics != null)
                {
                    _diagnostics.RecordPoolDisposed(Id.ToFixedString64Bytes());
                }

                // Unregister from registry
                if (_registry != null)
                {
                    _registry.UnregisterPool(this);
                }

                // Dispose unmanaged resources
                if (_activeItems.IsCreated)
                {
                    _activeItems.Dispose();
                }

                // Log disposal
                _logger?.LogInfoInstance($"Pool '{PoolName}' disposed. Stats: Created={TotalCreated}, Active={ActiveCount}, Peak={PeakUsage}");

                // Mark as disposed
                _isDisposed = true;
            }
        }

        #endregion

        #region IShrinkablePool Implementation

        /// <summary>
        /// Determines if the pool can be shrunk based on current usage and thresholds.
        /// </summary>
        /// <returns>True if the pool can be shrunk, false otherwise</returns>
        public bool CanShrink()
        {
            if (_isDisposed || !IsCreated)
                return false;

            // Check if we have enough items to consider shrinking
            if (TotalCount <= MinimumCapacity)
                return false;

            // Check if we have a high enough percentage of inactive items
            float usageRatio = (float)ActiveCount / TotalCount;
            return usageRatio < ShrinkThreshold;
        }

        /// <summary>
        /// Shrinks the pool by removing inactive items.
        /// </summary>
        /// <param name="targetSize">Optional target size to shrink to</param>
        /// <returns>Number of items removed</returns>
        public int Shrink(int? targetSize = null)
        {
            if (_isDisposed || !IsCreated || !CanShrink())
                return 0;

            using (_profiler?.Sample("Shrink", Id, PoolName, ActiveCount, FreeCount))
            {
                int actualTargetSize = targetSize ?? math.max(MinimumCapacity, ActiveCount);
                actualTargetSize = math.max(actualTargetSize, MinimumCapacity);
                
                if (TotalCount <= actualTargetSize)
                    return 0;
                
                int itemsToRemove = TotalCount - actualTargetSize;
                int itemsRemoved = ShrinkInternal(itemsToRemove);
                
                // Update time of last shrink
                _lastShrinkTime = Time.realtimeSinceStartup;
                
                // Record shrink with diagnostics
                _diagnostics?.RecordPoolShrinkById(Id.ToFixedString64Bytes(), itemsRemoved);
                
                _logger?.LogInfoInstance($"Pool '{PoolName}' shrunk: removed {itemsRemoved} items, new size: {TotalCount}");
                
                return itemsRemoved;
            }
        }

        /// <summary>
        /// Internal implementation of shrinking logic.
        /// Must be overridden by derived classes.
        /// </summary>
        /// <param name="itemsToRemove">Number of items to remove</param>
        /// <returns>Actual number of items removed</returns>
        protected virtual int ShrinkInternal(int itemsToRemove)
        {
            // Base implementation doesn't actually remove anything
            // This needs to be implemented by derived classes based on their storage strategy
            return 0;
        }

        /// <summary>
        /// Checks and performs automatic shrinking if conditions are met.
        /// </summary>
        public void CheckAutoShrink()
        {
            if (_isDisposed || !IsCreated || !_autoShrinkEnabled)
                return;
            
            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - _lastShrinkTime >= ShrinkInterval && CanShrink())
            {
                Shrink();
            }
        }

        /// <summary>
        /// Enables or disables automatic shrinking of the pool.
        /// </summary>
        /// <param name="enabled">Whether to enable automatic shrinking</param>
        public void SetAutoShrink(bool enabled)
        {
            _autoShrinkEnabled = enabled;
            
            if (enabled)
            {
                _lastShrinkTime = Time.realtimeSinceStartup;
            }
            
            _logger?.LogInfoInstance($"Auto-shrink for pool '{PoolName}' {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Explicitly shrinks the pool to the specified capacity.
        /// </summary>
        /// <param name="targetCapacity">The target capacity to shrink to</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool ShrinkTo(int targetCapacity)
        {
            if (_isDisposed || !IsCreated)
                return false;
            
            targetCapacity = math.max(targetCapacity, MinimumCapacity);
            targetCapacity = math.max(targetCapacity, ActiveCount);
            
            if (TotalCount <= targetCapacity)
                return false;
            
            return InternalShrinkTo(targetCapacity);
        }

        /// <summary>
        /// Attempts to shrink the pool based on a threshold ratio.
        /// </summary>
        /// <param name="threshold">Threshold factor (0-1) determining when shrinking occurs</param>
        /// <returns>True if the pool was shrunk, false otherwise</returns>
        public bool TryShrink(float threshold)
        {
            if (_isDisposed || !IsCreated)
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

        /// <summary>
        /// Internal implementation of targeted shrinking.
        /// </summary>
        /// <param name="targetCapacity">Target capacity</param>
        /// <returns>True if shrinking occurred</returns>
        protected virtual bool InternalShrinkTo(int targetCapacity)
        {
            int itemsToRemove = TotalCount - targetCapacity;
            if (itemsToRemove <= 0)
                return false;
            
            int removed = ShrinkInternal(itemsToRemove);
            return removed > 0;
        }

        #endregion

        #region IPool Implementation

        /// <summary>
        /// Gets metrics for this pool.
        /// </summary>
        /// <returns>Dictionary of pool metrics</returns>
        public Dictionary<string, object> GetMetrics()
        {
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
                { "GrowthFactor", GrowthFactor }
            };
            
            return metrics;
        }

        /// <summary>
        /// Clears the pool, returning all active items to the inactive state.
        /// </summary>
        public void Clear()
        {
            if (_isDisposed || !IsCreated)
                return;
            
            using (_profiler?.Sample("Clear", Id, PoolName, ActiveCount, FreeCount))
            {
                // Record pool reset with diagnostics
                _diagnostics?.RecordPoolReset(Id.ToFixedString64Bytes());
                
                InternalClear();
                
                _logger?.LogInfoInstance($"Pool '{PoolName}' cleared. TotalCount={TotalCount}");
            }
        }

        /// <summary>
        /// Ensures the pool has at least the specified capacity.
        /// </summary>
        /// <param name="capacity">Required capacity</param>
        public void EnsureCapacity(int capacity)
        {
            if (_isDisposed || !IsCreated)
                return;
            
            capacity = math.min(capacity, MaximumCapacity);
            
            if (TotalCount >= capacity)
                return;
            
            int additionalCapacity = capacity - TotalCount;
            InternalExpand(additionalCapacity);
        }

        /// <summary>
        /// Sets the pool name. Used primarily for resolving naming conflicts during registration.
        /// </summary>
        /// <param name="newName">The new name for the pool</param>
        public void SetPoolName(string newName)
        {
            if (string.IsNullOrEmpty(newName))
                return;
            
            string oldName = PoolName;
            PoolName = newName;
            
            _logger?.LogInfoInstance($"Pool renamed from '{oldName}' to '{newName}'");
        }

        /// <summary>
        /// Internal implementation of clearing logic.
        /// Must be overridden by derived classes.
        /// </summary>
        protected virtual void InternalClear()
        {
            // Base implementation just resets tracking
            ActiveCount = 0;
            _activeItems.Clear();
        }

        /// <summary>
        /// Internal implementation of expansion logic.
        /// Must be overridden by derived classes.
        /// </summary>
        /// <param name="additionalCapacity">Additional capacity needed</param>
        protected virtual void InternalExpand(int additionalCapacity)
        {
            // Base implementation doesn't create any new items
            // This needs to be implemented by derived classes
        }

        #endregion
    }
}