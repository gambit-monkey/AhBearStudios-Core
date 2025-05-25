using System;
using AhBearStudios.Pooling.Builders;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;
using AhBearStudios.Pooling.Services;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Pooling.Configurations
{
    /// <summary>
    /// Configuration for managed object pools that work with standard C# classes.
    /// Provides specialized settings for managed object pooling with optimization for memory usage and performance.
    /// Compatible with Unity Collections v2 and Burst compiler.
    /// </summary>
    [Serializable]
    public sealed class ManagedObjectPoolConfig : IPoolConfig, IDisposable
    {
        #region IPoolConfig Implementation

        /// <summary>
        /// Gets or sets the unique identifier for this configuration.
        /// </summary>
        public string ConfigId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the initial capacity of the pool.
        /// </summary>
        public int InitialCapacity { get; set; } = 10;

        /// <summary>
        /// Gets or sets the minimum capacity the pool should maintain.
        /// </summary>
        public int MinimumCapacity { get; set; } = 5;

        /// <summary>
        /// Gets or sets the maximum size of the pool. Set to 0 for unlimited.
        /// </summary>
        public int MaximumCapacity { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether to prewarm the pool on initialization.
        /// </summary>
        public bool PrewarmOnInit { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to collect metrics for this pool.
        /// </summary>
        public bool CollectMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log detailed pool operations.
        /// </summary>
        public bool DetailedLogging { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to log warnings when the pool grows.
        /// </summary>
        public bool LogWarnings { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to call Reset() on objects when they are released.
        /// </summary>
        public bool ResetOnRelease { get; set; } = true;

        /// <summary>
        /// Gets or sets the threading mode for the pool.
        /// </summary>
        public PoolThreadingMode ThreadingMode { get; set; } = PoolThreadingMode.ThreadSafe;

        /// <summary>
        /// Gets or sets whether to automatically shrink the pool when usage drops.
        /// </summary>
        public bool EnableAutoShrink { get; set; } = true;

        /// <summary>
        /// Gets or sets the threshold ratio of used/total items below which the pool will shrink.
        /// </summary>
        public float ShrinkThreshold { get; set; } = 0.25f;

        /// <summary>
        /// Gets or sets the minimum time between auto-shrink operations in seconds.
        /// </summary>
        public float ShrinkInterval { get; set; } = 60f;

        /// <summary>
        /// Gets or sets the allocator to use for native containers.
        /// Uses Unity.Collections v2 Allocator enum.
        /// </summary>
        public Allocator NativeAllocator { get; set; } = Allocator.Persistent;

        /// <summary>
        /// Gets or sets whether to use exponential growth when expanding the pool.
        /// </summary>
        public bool UseExponentialGrowth { get; set; } = true;

        /// <summary>
        /// Gets or sets the growth factor when expanding the pool (for exponential growth).
        /// </summary>
        public float GrowthFactor { get; set; } = 2.0f;

        /// <summary>
        /// Gets or sets the linear growth increment when expanding the pool (for linear growth).
        /// </summary>
        public int GrowthIncrement { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to throw an exception when attempting to get an object 
        /// that would exceed the maximum pool size.
        /// </summary>
        public bool ThrowIfExceedingMaxCount { get; set; } = false;

        #endregion

        #region Managed Object Pool Specific Properties

        /// <summary>
        /// Gets or sets whether to call object lifecycle methods when acquiring/releasing.
        /// </summary>
        public bool CallLifecycleMethods { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to track stack traces for acquisition operations.
        /// </summary>
        public bool TrackStackTraces { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to validate items when they are released back to the pool.
        /// </summary>
        public bool ValidateOnRelease { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum time in seconds an item can remain active before triggering a warning.
        /// Set to 0 for no limit.
        /// </summary>
        public float MaxActiveTime { get; set; } = 0f;

        /// <summary>
        /// Gets or sets whether to maintain FIFO (First-In-First-Out) order when acquiring objects.
        /// </summary>
        public bool MaintainFifoOrder { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to force garbage collection after shrinking the pool.
        /// </summary>
        public bool ForceGcAfterShrink { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to use custom memory allocators for pooled objects.
        /// Only applicable for types implementing ICustomAllocatable.
        /// </summary>
        public bool UseCustomAllocators { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to use native collections for internal storage of managed objects.
        /// </summary>
        public bool UseNativeCollections { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to use safety handles for native collections.
        /// </summary>
        public bool UseSafetyHandles { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to track allocation sizes for profiling.
        /// </summary>
        public bool TrackAllocationSizes { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to clear or preserve object references on release.
        /// </summary>
        public bool ClearReferencesOnRelease { get; set; } = true;

        /// <summary>
        /// Gets or sets the disposal sentinel value for native containers.
        /// </summary>
        public byte DisposeSentinel { get; set; } = 0xFF;

        #endregion

        #region Private Fields

        private readonly IPoolLogger _logger;
        private readonly IPoolingServiceLocator _serviceLocator;
        private bool _isDisposed;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of ManagedObjectPoolConfig with default values.
        /// </summary>
        public ManagedObjectPoolConfig()
        {
            _logger = null;
            _serviceLocator = null;
        }

        /// <summary>
        /// Creates a new instance of ManagedObjectPoolConfig with dependency injection.
        /// </summary>
        /// <param name="logger">Logger service for pool operations</param>
        /// <param name="serviceLocator">Optional service locator for additional services</param>
        public ManagedObjectPoolConfig(IPoolLogger logger, IPoolingServiceLocator serviceLocator = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceLocator = serviceLocator ?? DefaultPoolingServices.Instance;

            if (_serviceLocator is DefaultPoolingServices defaultServices && !defaultServices.IsInitialized)
            {
                defaultServices.Initialize();
            }
        }

        /// <summary>
        /// Creates a new instance of ManagedObjectPoolConfig with the provided service locator.
        /// </summary>
        /// <param name="serviceLocator">The service locator to use</param>
        public ManagedObjectPoolConfig(IPoolingServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));

            if (_serviceLocator is DefaultPoolingServices defaultServices && !defaultServices.IsInitialized)
            {
                defaultServices.Initialize();
            }

            if (_serviceLocator.HasService<IPoolLogger>())
            {
                _logger = _serviceLocator.GetService<IPoolLogger>();
            }
        }

        /// <summary>
        /// Creates a new ManagedObjectPoolConfig by copying settings from another pool configuration.
        /// </summary>
        /// <param name="sourceConfig">The source configuration to copy settings from</param>
        /// <exception cref="ArgumentNullException">Thrown if source config is null</exception>
        public ManagedObjectPoolConfig(IPoolConfig sourceConfig)
        {
            if (sourceConfig == null)
                throw new ArgumentNullException(nameof(sourceConfig));

            CopyFromConfig(sourceConfig);

            if (sourceConfig is ManagedObjectPoolConfig managedConfig)
            {
                _serviceLocator = managedConfig._serviceLocator;
                _logger = managedConfig._logger;
            }
            else
            {
                _serviceLocator = DefaultPoolingServices.Instance;

                if (_serviceLocator is DefaultPoolingServices defaultServices && !defaultServices.IsInitialized)
                {
                    defaultServices.Initialize();
                }

                if (_serviceLocator.HasService<IPoolLogger>())
                {
                    _logger = _serviceLocator.GetService<IPoolLogger>();
                }
            }

            // Generate a new unique ID
            ConfigId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Creates a new ManagedObjectPoolConfig by copying settings from another pool configuration 
        /// with the provided service locator.
        /// </summary>
        /// <param name="sourceConfig">The source configuration to copy settings from</param>
        /// <param name="serviceLocator">The service locator to use</param>
        /// <exception cref="ArgumentNullException">Thrown if source config is null</exception>
        public ManagedObjectPoolConfig(IPoolConfig sourceConfig, IPoolingServiceLocator serviceLocator)
        {
            if (sourceConfig == null)
                throw new ArgumentNullException(nameof(sourceConfig));

            _serviceLocator = serviceLocator ?? DefaultPoolingServices.Instance;

            if (_serviceLocator is DefaultPoolingServices defaultServices && !defaultServices.IsInitialized)
            {
                defaultServices.Initialize();
            }

            if (_serviceLocator.HasService<IPoolLogger>())
            {
                _logger = _serviceLocator.GetService<IPoolLogger>();
            }

            CopyFromConfig(sourceConfig);

            // Generate a new unique ID
            ConfigId = Guid.NewGuid().ToString();
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of any resources used by this config.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            // Clean up any native resources or references if needed

            _isDisposed = true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Converts this configuration to a builder for further modification.
        /// </summary>
        /// <returns>A builder initialized with this configuration's values.</returns>
        public ManagedObjectPoolConfigBuilder ToBuilder()
        {
            return new ManagedObjectPoolConfigBuilder().FromExisting(this);
        }

        /// <summary>
        /// Registers this configuration with the provided registry under the specified name.
        /// </summary>
        /// <param name="registry">The registry to register with</param>
        /// <param name="configName">The name to register under</param>
        /// <returns>This configuration for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if registry is null</exception>
        /// <exception cref="ArgumentException">Thrown if config name is null or empty</exception>
        public ManagedObjectPoolConfig Register(IPoolConfigRegistry registry, string configName)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");

            if (string.IsNullOrEmpty(configName))
                throw new ArgumentException("Config name cannot be null or empty", nameof(configName));

            registry.RegisterConfig(configName, this);

            return this;
        }

        /// <summary>
        /// Registers this configuration with the default registry under the specified name.
        /// </summary>
        /// <param name="configName">The name to register under</param>
        /// <returns>This configuration for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown if config name is null or empty</exception>
        public ManagedObjectPoolConfig Register(string configName)
        {
            if (string.IsNullOrEmpty(configName))
                throw new ArgumentException("Config name cannot be null or empty", nameof(configName));

            if (_serviceLocator != null && _serviceLocator.HasService<IPoolConfigRegistry>())
            {
                var registry = _serviceLocator.GetService<IPoolConfigRegistry>();
                registry.RegisterConfig(configName, this);
            }
            else
            {
                var registry = DefaultPoolingServices.Instance.GetService<IPoolConfigRegistry>();
                if (registry == null)
                {
                    throw new InvalidOperationException(
                        "No IPoolConfigRegistry service available in the service locator");
                }

                registry.RegisterConfig(configName, this);
            }

            return this;
        }

        /// <summary>
        /// Registers this configuration with the provided registry for the specified type.
        /// </summary>
        /// <typeparam name="T">The type to register for</typeparam>
        /// <param name="registry">The registry to register with</param>
        /// <returns>This configuration for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if registry is null</exception>
        public ManagedObjectPoolConfig RegisterForType<T>(IPoolConfigRegistry registry) where T : class
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");

            registry.RegisterConfigForType<T>(this);

            return this;
        }

        /// <summary>
        /// Registers this configuration with the default registry for the specified type.
        /// </summary>
        /// <typeparam name="T">The type to register for</typeparam>
        /// <returns>This configuration for method chaining</returns>
        public ManagedObjectPoolConfig RegisterForType<T>() where T : class
        {
            if (_serviceLocator != null && _serviceLocator.HasService<IPoolConfigRegistry>())
            {
                var registry = _serviceLocator.GetService<IPoolConfigRegistry>();
                registry.RegisterConfigForType<T>(this);
            }
            else
            {
                var registry = DefaultPoolingServices.Instance.GetService<IPoolConfigRegistry>();
                if (registry == null)
                {
                    throw new InvalidOperationException(
                        "No IPoolConfigRegistry service available in the service locator");
                }

                registry.RegisterConfigForType<T>(this);
            }

            return this;
        }

        /// <summary>
        /// Creates a deep clone of this configuration.
        /// </summary>
        /// <returns>A new ManagedObjectPoolConfig with the same settings as this one</returns>
        public IPoolConfig Clone()
        {
            var clone = new ManagedObjectPoolConfig(_serviceLocator)
            {
                // Copy all IPoolConfig settings
                InitialCapacity = InitialCapacity,
                MinimumCapacity = MinimumCapacity,
                MaximumCapacity = MaximumCapacity,
                PrewarmOnInit = PrewarmOnInit,
                CollectMetrics = CollectMetrics,
                DetailedLogging = DetailedLogging,
                LogWarnings = LogWarnings,
                ResetOnRelease = ResetOnRelease,
                ThreadingMode = ThreadingMode,
                EnableAutoShrink = EnableAutoShrink,
                ShrinkThreshold = ShrinkThreshold,
                ShrinkInterval = ShrinkInterval,
                NativeAllocator = NativeAllocator,
                UseExponentialGrowth = UseExponentialGrowth,
                GrowthFactor = GrowthFactor,
                GrowthIncrement = GrowthIncrement,
                ThrowIfExceedingMaxCount = ThrowIfExceedingMaxCount,

                // Copy managed-specific settings
                CallLifecycleMethods = CallLifecycleMethods,
                TrackStackTraces = TrackStackTraces,
                ValidateOnRelease = ValidateOnRelease,
                MaxActiveTime = MaxActiveTime,
                MaintainFifoOrder = MaintainFifoOrder,
                ForceGcAfterShrink = ForceGcAfterShrink,
                UseCustomAllocators = UseCustomAllocators,
                UseNativeCollections = UseNativeCollections,
                UseSafetyHandles = UseSafetyHandles,
                TrackAllocationSizes = TrackAllocationSizes,
                ClearReferencesOnRelease = ClearReferencesOnRelease,
                DisposeSentinel = DisposeSentinel
            };

            // Generate a new unique ID
            clone.ConfigId = Guid.NewGuid().ToString();

            return clone;
        }

        /// <summary>
        /// Calculates the maximum number of items this pool can grow to.
        /// Takes into account unlimited pool configurations.
        /// </summary>
        /// <returns>The maximum capacity, or int.MaxValue for unlimited pools</returns>
        public int GetEffectiveMaximumSize()
        {
            return MaximumCapacity <= 0 ? int.MaxValue : MaximumCapacity;
        }

        /// <summary>
        /// Calculates the next size to grow to based on current size and growth settings.
        /// </summary>
        /// <param name="currentSize">The current size of the pool</param>
        /// <returns>The size to grow to next</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if current size is negative</exception>
        public int CalculateNextGrowthSize(int currentSize)
        {
            if (currentSize < 0)
                throw new ArgumentOutOfRangeException(nameof(currentSize), "Current size cannot be negative");

            int effectiveMaxSize = GetEffectiveMaximumSize();
            int newSize;

            if (UseExponentialGrowth)
            {
                // Use exponential growth with the configured factor
                newSize = Mathf.CeilToInt(currentSize * GrowthFactor);

                // Ensure we grow by at least 1
                if (newSize <= currentSize)
                    newSize = currentSize + 1;
            }
            else
            {
                // Use linear growth with the configured increment
                newSize = currentSize + GrowthIncrement;
            }

            // Respect maximum capacity
            return Mathf.Min(newSize, effectiveMaxSize);
        }

        /// <summary>
        /// Calculates the number of items to shrink to based on current size and shrink settings.
        /// </summary>
        /// <param name="currentSize">The current size of the pool</param>
        /// <param name="usedCount">The number of items currently in use</param>
        /// <returns>The size to shrink to, or -1 if no shrinking is needed</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if parameters are out of range</exception>
        /// <exception cref="ArgumentException">Thrown if used count exceeds current size</exception>
        public int CalculateShrinkSize(int currentSize, int usedCount)
        {
            if (currentSize < 0)
                throw new ArgumentOutOfRangeException(nameof(currentSize), "Current size cannot be negative");

            if (usedCount < 0)
                throw new ArgumentOutOfRangeException(nameof(usedCount), "Used count cannot be negative");

            if (usedCount > currentSize)
                throw new ArgumentException("Used count cannot exceed current size");

            // Don't shrink if auto-shrink is disabled or pool is empty
            if (!EnableAutoShrink || currentSize == 0)
                return -1;

            // Calculate usage ratio
            float usageRatio = (float)usedCount / currentSize;

            // Don't shrink if usage is above threshold or no items can be freed
            if (usageRatio >= ShrinkThreshold || usedCount == currentSize)
                return -1;

            // Calculate new size - don't go below initial capacity or minimum capacity
            int newSize = Mathf.Max(Mathf.Max(InitialCapacity, MinimumCapacity), Mathf.CeilToInt(usedCount * 1.5f));

            // Don't shrink if new size is greater than or equal to current size
            if (newSize >= currentSize)
                return -1;

            return newSize;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"ManagedObjectPoolConfig(Id={ConfigId}, Initial={InitialCapacity}, Max={MaximumCapacity})";
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Copies settings from a source configuration to this configuration.
        /// </summary>
        /// <param name="sourceConfig">The source configuration to copy from</param>
        private void CopyFromConfig(IPoolConfig sourceConfig)
        {
            // Copy basic IPoolConfig settings
            InitialCapacity = sourceConfig.InitialCapacity;
            MaximumCapacity = sourceConfig.MaximumCapacity;

            // Copy MinimumCapacity if available
            if (sourceConfig.GetType().GetProperty("MinimumCapacity") != null)
            {
                MinimumCapacity = (int)sourceConfig.GetType().GetProperty("MinimumCapacity").GetValue(sourceConfig);
            }

            PrewarmOnInit = sourceConfig.PrewarmOnInit;
            CollectMetrics = sourceConfig.CollectMetrics;
            DetailedLogging = sourceConfig.DetailedLogging;
            LogWarnings = sourceConfig.LogWarnings;
            ResetOnRelease = sourceConfig.ResetOnRelease;
            ThreadingMode = sourceConfig.ThreadingMode;
            EnableAutoShrink = sourceConfig.EnableAutoShrink;
            ShrinkThreshold = sourceConfig.ShrinkThreshold;
            ShrinkInterval = sourceConfig.ShrinkInterval;
            NativeAllocator = sourceConfig.NativeAllocator;
            UseExponentialGrowth = sourceConfig.UseExponentialGrowth;
            GrowthFactor = sourceConfig.GrowthFactor;
            GrowthIncrement = sourceConfig.GrowthIncrement;
            ThrowIfExceedingMaxCount = sourceConfig.ThrowIfExceedingMaxCount;

            // Copy managed-specific settings if the source is a ManagedObjectPoolConfig
            if (sourceConfig is ManagedObjectPoolConfig managedConfig)
            {
                CallLifecycleMethods = managedConfig.CallLifecycleMethods;
                TrackStackTraces = managedConfig.TrackStackTraces;
                ValidateOnRelease = managedConfig.ValidateOnRelease;
                MaxActiveTime = managedConfig.MaxActiveTime;
                MaintainFifoOrder = managedConfig.MaintainFifoOrder;
                ForceGcAfterShrink = managedConfig.ForceGcAfterShrink;
                UseCustomAllocators = managedConfig.UseCustomAllocators;
                UseNativeCollections = managedConfig.UseNativeCollections;
                UseSafetyHandles = managedConfig.UseSafetyHandles;
                TrackAllocationSizes = managedConfig.TrackAllocationSizes;
                ClearReferencesOnRelease = managedConfig.ClearReferencesOnRelease;
                DisposeSentinel = managedConfig.DisposeSentinel;
            }
        }

        #endregion
    }
}