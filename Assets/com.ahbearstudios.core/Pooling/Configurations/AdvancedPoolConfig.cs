using System;
using AhBearStudios.Core.Pooling.Builders;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Configurations
{
    /// <summary>
    /// Advanced pool configuration with extended options for fine-grained control over pool behavior.
    /// Designed for high-performance Unity applications with full Burst and Unity Collections v2 compatibility.
    /// Implements <see cref="IPoolConfig"/> through composition for better architectural design.
    /// </summary>
    public sealed class AdvancedPoolConfig : IPoolConfig
    {
        #region IPoolConfig Implementation - Core Configuration
        
        /// <summary>
        /// Gets or sets the unique identifier for this pool configuration.
        /// </summary>
        public string ConfigId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the initial capacity of the pool.
        /// </summary>
        public int InitialCapacity { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets the minimum capacity the pool should maintain, preventing
        /// shrinking below this threshold.
        /// </summary>
        public int MinimumCapacity { get; set; } = 5;
        
        /// <summary>
        /// Gets or sets the maximum size of the pool (0 for unlimited).
        /// </summary>
        public int MaximumCapacity { get; set; } = 0;
        
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
        /// Gets or sets the threading mode for this pool.
        /// </summary>
        public PoolThreadingMode ThreadingMode { get; set; } = PoolThreadingMode.ThreadSafe;
        
        /// <summary>
        /// Gets or sets whether to automatically shrink the pool when usage drops.
        /// </summary>
        public bool EnableAutoShrink { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the threshold ratio of used/total items below which the pool will shrink.
        /// </summary>
        public float ShrinkThreshold { get; set; } = 0.25f;
        
        /// <summary>
        /// Gets or sets the minimum time between auto-shrink operations in seconds.
        /// </summary>
        public float ShrinkInterval { get; set; } = 60.0f;
        
        /// <summary>
        /// Gets or sets the native allocator to use for native pools.
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
        
        #region Advanced Configuration Properties
        
        /// <summary>
        /// Gets or sets the maximum number of inactive items to keep when shrinking.
        /// </summary>
        public int MaxInactiveOnShrink { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets whether the pool can resize dynamically based on usage patterns.
        /// </summary>
        public bool AllowResize { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the threshold that triggers a pool resize.
        /// Value should be between 0 and 1, representing the usage percentage.
        /// </summary>
        public float ResizeThreshold { get; set; } = 0.9f;
        
        /// <summary>
        /// Gets or sets the multiplier for pool resizing.
        /// Value should be greater than 1.0 to ensure growth.
        /// </summary>
        public float ResizeMultiplier { get; set; } = 1.5f;
        
        /// <summary>
        /// Gets or sets whether to enable advanced monitoring of pool usage.
        /// </summary>
        public bool EnableMonitoring { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the interval at which monitoring occurs in seconds.
        /// </summary>
        public float MonitoringInterval { get; set; } = 10.0f;
        
        /// <summary>
        /// Gets or sets the maximum lifetime of a pooled item in seconds (0 for unlimited).
        /// Used to detect long-lived or leaked objects.
        /// </summary>
        public float MaxItemLifetime { get; set; } = 0.0f;
        
        /// <summary>
        /// Gets or sets whether to warn about potential object leaks.
        /// </summary>
        public bool WarnOnLeakedItems { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to warn about stale items in the pool.
        /// </summary>
        public bool WarnOnStaleItems { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to track stack traces for item acquisition.
        /// Note: This has a performance impact and should be used primarily for debugging.
        /// </summary>
        public bool TrackAcquireStackTraces { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to validate items when they are released back to the pool.
        /// </summary>
        public bool ValidateOnRelease { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to enforce thread safety even in single-threaded mode.
        /// </summary>
        public bool EnableThreadSafety { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to enable additional diagnostic information.
        /// </summary>
        public bool EnableDiagnostics { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to enable performance profiling.
        /// </summary>
        public bool EnableProfiling { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to enable periodic health checks of the pool.
        /// </summary>
        public bool EnableHealthChecks { get; set; } = false;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Creates a new advanced pool configuration with default settings.
        /// </summary>
        public AdvancedPoolConfig()
        {
            ConfigId = Guid.NewGuid().ToString("N"); // More efficient format for GUIDs
        }
        
        /// <summary>
        /// Creates a new advanced pool configuration with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is negative</exception>
        public AdvancedPoolConfig(int initialCapacity) : this()
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            InitialCapacity = initialCapacity;
            MinimumCapacity = Math.Max(1, initialCapacity / 2);
        }
        
        /// <summary>
        /// Creates a new advanced pool configuration with the specified initial and maximum sizes.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is negative</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if maxSize is less than initialCapacity and not 0</exception>
        public AdvancedPoolConfig(int initialCapacity, int maxSize) : this(initialCapacity)
        {
            if (maxSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size cannot be negative");
                
            if (maxSize > 0 && maxSize < initialCapacity)
                throw new ArgumentOutOfRangeException(nameof(maxSize), 
                    "Max size must be greater than or equal to initial capacity, or 0 for unlimited");
                
            MaximumCapacity = maxSize;
        }
        
        /// <summary>
        /// Creates a new advanced pool configuration based on an existing pool configuration.
        /// </summary>
        /// <param name="baseConfig">The base configuration to extend</param>
        /// <exception cref="ArgumentNullException">Thrown if baseConfig is null</exception>
        public AdvancedPoolConfig(IPoolConfig baseConfig) : this()
        {
            if (baseConfig == null)
                throw new ArgumentNullException(nameof(baseConfig), "Base configuration cannot be null");
                
            // Copy all standard properties
            InitialCapacity = baseConfig.InitialCapacity;
            MinimumCapacity = baseConfig.MinimumCapacity;
            MaximumCapacity = baseConfig.MaximumCapacity;
            PrewarmOnInit = baseConfig.PrewarmOnInit;
            CollectMetrics = baseConfig.CollectMetrics;
            DetailedLogging = baseConfig.DetailedLogging;
            LogWarnings = baseConfig.LogWarnings;
            ResetOnRelease = baseConfig.ResetOnRelease;
            ThreadingMode = baseConfig.ThreadingMode;
            EnableAutoShrink = baseConfig.EnableAutoShrink;
            ShrinkThreshold = baseConfig.ShrinkThreshold;
            ShrinkInterval = baseConfig.ShrinkInterval;
            NativeAllocator = baseConfig.NativeAllocator;
            UseExponentialGrowth = baseConfig.UseExponentialGrowth;
            GrowthFactor = baseConfig.GrowthFactor;
            GrowthIncrement = baseConfig.GrowthIncrement;
            ThrowIfExceedingMaxCount = baseConfig.ThrowIfExceedingMaxCount;
            
            // Copy ConfigId if available
            if (!string.IsNullOrEmpty(baseConfig.ConfigId))
            {
                ConfigId = baseConfig.ConfigId;
            }
            
            // Copy advanced properties if the source is an AdvancedPoolConfig
            if (baseConfig is AdvancedPoolConfig advConfig)
            {
                MaxInactiveOnShrink = advConfig.MaxInactiveOnShrink;
                AllowResize = advConfig.AllowResize;
                ResizeThreshold = advConfig.ResizeThreshold;
                ResizeMultiplier = advConfig.ResizeMultiplier;
                EnableMonitoring = advConfig.EnableMonitoring;
                MonitoringInterval = advConfig.MonitoringInterval;
                MaxItemLifetime = advConfig.MaxItemLifetime;
                WarnOnLeakedItems = advConfig.WarnOnLeakedItems;
                WarnOnStaleItems = advConfig.WarnOnStaleItems;
                TrackAcquireStackTraces = advConfig.TrackAcquireStackTraces;
                ValidateOnRelease = advConfig.ValidateOnRelease;
                EnableThreadSafety = advConfig.EnableThreadSafety;
                EnableDiagnostics = advConfig.EnableDiagnostics;
                EnableProfiling = advConfig.EnableProfiling;
                EnableHealthChecks = advConfig.EnableHealthChecks;
            }
        }
        
        #endregion
        
        #region IPoolConfig Implementation
        
        /// <summary>
        /// Creates a deep copy of the configuration.
        /// </summary>
        /// <returns>A new instance of the configuration with the same settings.</returns>
        public IPoolConfig Clone()
        {
            // Using direct property assignment for better performance than reflection
            return new AdvancedPoolConfig
            {
                // Standard properties
                ConfigId = this.ConfigId,
                InitialCapacity = this.InitialCapacity,
                MinimumCapacity = this.MinimumCapacity,
                MaximumCapacity = this.MaximumCapacity,
                PrewarmOnInit = this.PrewarmOnInit,
                CollectMetrics = this.CollectMetrics,
                DetailedLogging = this.DetailedLogging,
                LogWarnings = this.LogWarnings,
                ResetOnRelease = this.ResetOnRelease,
                ThreadingMode = this.ThreadingMode,
                EnableAutoShrink = this.EnableAutoShrink,
                ShrinkThreshold = this.ShrinkThreshold,
                ShrinkInterval = this.ShrinkInterval,
                NativeAllocator = this.NativeAllocator,
                UseExponentialGrowth = this.UseExponentialGrowth,
                GrowthFactor = this.GrowthFactor,
                GrowthIncrement = this.GrowthIncrement,
                ThrowIfExceedingMaxCount = this.ThrowIfExceedingMaxCount,
                
                // Advanced properties
                MaxInactiveOnShrink = this.MaxInactiveOnShrink,
                AllowResize = this.AllowResize,
                ResizeThreshold = this.ResizeThreshold,
                ResizeMultiplier = this.ResizeMultiplier,
                EnableMonitoring = this.EnableMonitoring,
                MonitoringInterval = this.MonitoringInterval,
                MaxItemLifetime = this.MaxItemLifetime,
                WarnOnLeakedItems = this.WarnOnLeakedItems,
                WarnOnStaleItems = this.WarnOnStaleItems,
                TrackAcquireStackTraces = this.TrackAcquireStackTraces,
                ValidateOnRelease = this.ValidateOnRelease,
                EnableThreadSafety = this.EnableThreadSafety,
                EnableDiagnostics = this.EnableDiagnostics,
                EnableProfiling = this.EnableProfiling,
                EnableHealthChecks = this.EnableHealthChecks
            };
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Validates this configuration and adjusts values to ensure they are within acceptable ranges.
        /// </summary>
        /// <returns>True if the configuration is valid; otherwise, false.</returns>
        public bool Validate()
        {
            // Validate capacity settings
            if (InitialCapacity < 0)
                InitialCapacity = 0;
                
            if (MinimumCapacity < 0)
                MinimumCapacity = 0;
                
            if (MaximumCapacity < 0)
                MaximumCapacity = 0;
                
            if (MinimumCapacity > InitialCapacity)
                MinimumCapacity = InitialCapacity;
                
            if (MaximumCapacity > 0 && MaximumCapacity < InitialCapacity)
                InitialCapacity = MaximumCapacity;
                
            // Validate shrink settings
            if (ShrinkThreshold < 0.05f)
                ShrinkThreshold = 0.05f;
            else if (ShrinkThreshold > 0.95f)
                ShrinkThreshold = 0.95f;
                
            if (ShrinkInterval < 0)
                ShrinkInterval = 0;
                
            // Validate growth settings
            if (GrowthFactor < 1.1f)
                GrowthFactor = 1.1f;
                
            if (GrowthIncrement < 1)
                GrowthIncrement = 1;
                
            // Validate advanced settings
            if (ResizeThreshold < 0.1f)
                ResizeThreshold = 0.1f;
            else if (ResizeThreshold > 0.95f)
                ResizeThreshold = 0.95f;
                
            if (ResizeMultiplier < 1.1f)
                ResizeMultiplier = 1.1f;
                
            if (MonitoringInterval < 0.1f)
                MonitoringInterval = 0.1f;
                
            if (MaxInactiveOnShrink < 0)
                MaxInactiveOnShrink = 0;
                
            if (MaxItemLifetime < 0)
                MaxItemLifetime = 0;
                
            return true;
        }
        
        /// <summary>
        /// Registers this configuration with the specified registry.
        /// </summary>
        /// <param name="registry">The registry to register with.</param>
        /// <exception cref="ArgumentNullException">Thrown if registry is null.</exception>
        public void Register(IPoolConfigRegistry registry)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
                
            registry.RegisterConfig(ConfigId, this);
        }
        
        /// <summary>
        /// Registers this configuration for a specific item type.
        /// </summary>
        /// <typeparam name="T">Type of the items this configuration is for.</typeparam>
        /// <param name="registry">The registry to register with.</param>
        /// <exception cref="ArgumentNullException">Thrown if registry is null.</exception>
        public void RegisterForType<T>(IPoolConfigRegistry registry) where T : class
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
                
            registry.RegisterConfigForType<T>(this);
        }
        
        /// <summary>
        /// Converts this configuration to a builder for further modification.
        /// </summary>
        /// <returns>A builder initialized with this configuration's values.</returns>
        public AdvancedPoolConfigBuilder ToBuilder()
        {
            return new AdvancedPoolConfigBuilder().FromExisting(this);
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Creates a new builder for advanced pool configurations.
        /// </summary>
        /// <returns>A new builder instance.</returns>
        public static AdvancedPoolConfigBuilder CreateBuilder()
        {
            return new AdvancedPoolConfigBuilder();
        }
        
        /// <summary>
        /// Creates a new builder for advanced pool configurations with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <returns>A new builder instance.</returns>
        public static AdvancedPoolConfigBuilder CreateBuilder(int initialCapacity)
        {
            return new AdvancedPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
        }
        
        /// <summary>
        /// Creates a new builder for advanced pool configurations with the specified initial and maximum capacities.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <param name="maximumCapacity">Maximum pool size (0 for unlimited).</param>
        /// <returns>A new builder instance.</returns>
        public static AdvancedPoolConfigBuilder CreateBuilder(int initialCapacity, int maximumCapacity)
        {
            return new AdvancedPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaximumCapacity(maximumCapacity);
        }
        
        #endregion
    }
}