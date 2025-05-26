using System;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Interfaces;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Advanced pool configuration builder for creating AdvancedPoolConfig instances
    /// using the fluent builder pattern.
    /// </summary>
    public sealed class AdvancedPoolConfigBuilder : IPoolConfigBuilder<AdvancedPoolConfig, AdvancedPoolConfigBuilder>
    {
        private readonly AdvancedPoolConfig _config;

        /// <summary>
        /// Gets or sets the configuration ID
        /// </summary>
        public string ConfigId
        {
            get => _config.ConfigId;
            set => _config.ConfigId = value;
        }

        /// <summary>
        /// Creates a new advanced pool configuration builder with default settings
        /// </summary>
        public AdvancedPoolConfigBuilder()
        {
            _config = new AdvancedPoolConfig();
        }

        // Standard configuration methods
        public AdvancedPoolConfigBuilder WithInitialCapacity(int capacity)
        {
            _config.InitialCapacity = capacity;
            return this;
        }

        public AdvancedPoolConfigBuilder WithMaximumCapacity(int maximumCapacity)
        {
            _config.MaximumCapacity = maximumCapacity;
            return this;
        }
        
        public AdvancedPoolConfigBuilder WithMinimumCapacity(int minimumCapacity)
        {
            _config.MinimumCapacity = minimumCapacity;
            return this;
        }

        public AdvancedPoolConfigBuilder WithPrewarming(bool prewarm)
        {
            _config.PrewarmOnInit = prewarm;
            return this;
        }

        public AdvancedPoolConfigBuilder WithMetricsCollection(bool collectMetrics)
        {
            _config.CollectMetrics = collectMetrics;
            return this;
        }

        public AdvancedPoolConfigBuilder WithDetailedLogging(bool detailedLogging)
        {
            _config.DetailedLogging = detailedLogging;
            return this;
        }

        public AdvancedPoolConfigBuilder WithWarningLogging(bool logWarnings)
        {
            _config.LogWarnings = logWarnings;
            return this;
        }

        public AdvancedPoolConfigBuilder WithResetOnRelease(bool resetOnRelease)
        {
            _config.ResetOnRelease = resetOnRelease;
            return this;
        }

        public AdvancedPoolConfigBuilder WithThreadingMode(PoolThreadingMode threadingMode)
        {
            _config.ThreadingMode = threadingMode;
            return this;
        }

        public AdvancedPoolConfigBuilder WithAutoShrink(bool enableAutoShrink)
        {
            _config.EnableAutoShrink = enableAutoShrink;
            return this;
        }

        public AdvancedPoolConfigBuilder WithShrinkThreshold(float threshold)
        {
            _config.ShrinkThreshold = threshold;
            return this;
        }

        public AdvancedPoolConfigBuilder WithShrinkInterval(float intervalSeconds)
        {
            _config.ShrinkInterval = intervalSeconds;
            return this;
        }

        public AdvancedPoolConfigBuilder WithNativeAllocator(Allocator allocator)
        {
            _config.NativeAllocator = allocator;
            return this;
        }

        // Advanced configuration methods
        public AdvancedPoolConfigBuilder WithExponentialGrowth(bool useExponentialGrowth)
        {
            _config.UseExponentialGrowth = useExponentialGrowth;
            return this;
        }

        public AdvancedPoolConfigBuilder WithGrowthFactor(float growthFactor)
        {
            _config.GrowthFactor = growthFactor;
            return this;
        }

        public AdvancedPoolConfigBuilder WithGrowthIncrement(int increment)
        {
            _config.GrowthIncrement = increment;
            return this;
        }

        public AdvancedPoolConfigBuilder WithExceptionOnExceedingMaxCount(bool throwIfExceeding)
        {
            _config.ThrowIfExceedingMaxCount = throwIfExceeding;
            return this;
        }

        public AdvancedPoolConfigBuilder WithMaxInactiveOnShrink(int maxInactive)
        {
            _config.MaxInactiveOnShrink = maxInactive;
            return this;
        }

        public AdvancedPoolConfigBuilder WithDynamicResizing(bool allowResize, float threshold = 0.9f, float multiplier = 1.5f)
        {
            _config.AllowResize = allowResize;
            _config.ResizeThreshold = threshold;
            _config.ResizeMultiplier = multiplier;
            return this;
        }

        public AdvancedPoolConfigBuilder WithMonitoring(bool enableMonitoring, float interval = 10.0f)
        {
            _config.EnableMonitoring = enableMonitoring;
            _config.MonitoringInterval = interval;
            return this;
        }

        public AdvancedPoolConfigBuilder WithItemLifetime(float maxLifetime)
        {
            _config.MaxItemLifetime = maxLifetime;
            return this;
        }

        public AdvancedPoolConfigBuilder WithLeakWarnings(bool warnOnLeaks)
        {
            _config.WarnOnLeakedItems = warnOnLeaks;
            return this;
        }

        public AdvancedPoolConfigBuilder WithStaleItemWarnings(bool warnOnStale)
        {
            _config.WarnOnStaleItems = warnOnStale;
            return this;
        }

        public AdvancedPoolConfigBuilder WithStackTraceTracking(bool trackStackTraces)
        {
            _config.TrackAcquireStackTraces = trackStackTraces;
            return this;
        }

        public AdvancedPoolConfigBuilder WithValidationOnRelease(bool validate)
        {
            _config.ValidateOnRelease = validate;
            return this;
        }

        public AdvancedPoolConfigBuilder WithThreadSafety(bool enableThreadSafety)
        {
            _config.EnableThreadSafety = enableThreadSafety;
            return this;
        }

        public AdvancedPoolConfigBuilder WithDiagnostics(bool enableDiagnostics)
        {
            _config.EnableDiagnostics = enableDiagnostics;
            return this;
        }

        public AdvancedPoolConfigBuilder WithProfiling(bool enableProfiling)
        {
            _config.EnableProfiling = enableProfiling;
            return this;
        }

        public AdvancedPoolConfigBuilder WithHealthChecks(bool enableHealthChecks)
        {
            _config.EnableHealthChecks = enableHealthChecks;
            return this;
        }

        /// <summary>
        /// Initializes a builder from an existing AdvancedPoolConfig.
        /// </summary>
        /// <param name="config">The source configuration to copy settings from.</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
        public AdvancedPoolConfigBuilder FromExisting(AdvancedPoolConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Source configuration cannot be null");

            ConfigId = config.ConfigId;

            // Copy standard properties
            WithInitialCapacity(config.InitialCapacity);
            WithMinimumCapacity(config.MinimumCapacity);
            WithMaximumCapacity(config.MaximumCapacity);
            WithPrewarming(config.PrewarmOnInit);
            WithMetricsCollection(config.CollectMetrics);
            WithDetailedLogging(config.DetailedLogging);
            WithWarningLogging(config.LogWarnings);
            WithResetOnRelease(config.ResetOnRelease);
            WithThreadingMode(config.ThreadingMode);
            WithAutoShrink(config.EnableAutoShrink);
            WithShrinkThreshold(config.ShrinkThreshold);
            WithShrinkInterval(config.ShrinkInterval);
            WithNativeAllocator(config.NativeAllocator);
            WithExponentialGrowth(config.UseExponentialGrowth);
            WithGrowthFactor(config.GrowthFactor);
            WithGrowthIncrement(config.GrowthIncrement);
            WithExceptionOnExceedingMaxCount(config.ThrowIfExceedingMaxCount);

            // Copy advanced properties
            WithMaxInactiveOnShrink(config.MaxInactiveOnShrink);
            WithDynamicResizing(config.AllowResize, config.ResizeThreshold, config.ResizeMultiplier);
            WithMonitoring(config.EnableMonitoring, config.MonitoringInterval);
            WithItemLifetime(config.MaxItemLifetime);
            WithLeakWarnings(config.WarnOnLeakedItems);
            WithStaleItemWarnings(config.WarnOnStaleItems);
            WithStackTraceTracking(config.TrackAcquireStackTraces);
            WithValidationOnRelease(config.ValidateOnRelease);
            WithThreadSafety(config.EnableThreadSafety);
            WithDiagnostics(config.EnableDiagnostics);
            WithProfiling(config.EnableProfiling);
            WithHealthChecks(config.EnableHealthChecks);

            return this;
        }

        /// <summary>
        /// Configures the builder for high-performance mode with minimal overhead
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public AdvancedPoolConfigBuilder AsHighPerformance()
        {
            // Disable monitoring and diagnostics overhead
            _config.CollectMetrics = false;
            _config.DetailedLogging = false;
            _config.LogWarnings = false;
            _config.EnableMonitoring = false;
            _config.EnableDiagnostics = false;
            _config.EnableProfiling = false;
            _config.EnableHealthChecks = false;
    
            // Disable validation and tracking overhead
            _config.ValidateOnRelease = false;
            _config.TrackAcquireStackTraces = false;
            _config.WarnOnLeakedItems = false;
            _config.WarnOnStaleItems = false;
    
            // Configure for optimal performance
            _config.PrewarmOnInit = true;                // Prewarm to avoid runtime allocations
            _config.UseExponentialGrowth = true;         // Faster growth when needed
            _config.GrowthFactor = 2.0f;                 // Double size for fewer resize operations
            _config.EnableAutoShrink = false;            // Disable automatic shrinking
            _config.ResetOnRelease = false;              // Skip reset operations
            _config.ThrowIfExceedingMaxCount = false;    // Skip bounds checking
    
            return this;
        }
        
        /// <summary>
        /// Configures the builder for debug mode with extensive tracking and validation
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public AdvancedPoolConfigBuilder AsDebug()
        {
            // Enable monitoring and diagnostics
            _config.CollectMetrics = true;
            _config.DetailedLogging = true;
            _config.LogWarnings = true;
            _config.EnableMonitoring = true;
            _config.MonitoringInterval = 5.0f;  // More frequent monitoring in debug mode
    
            // Enable validation and safety checks
            _config.ValidateOnRelease = true;
            _config.EnableDiagnostics = true;
            _config.EnableProfiling = true;
            _config.EnableHealthChecks = true;
    
            // Enable leak detection and tracking
            _config.TrackAcquireStackTraces = true;
            _config.WarnOnLeakedItems = true;
            _config.WarnOnStaleItems = true;
    
            // Enable safety features
            _config.ResetOnRelease = true;
            _config.ThrowIfExceedingMaxCount = true;
    
            return this;
        }

        /// <summary>
        /// Builds and returns the completed configuration
        /// </summary>
        /// <returns>The completed configuration object</returns>
        public AdvancedPoolConfig Build()
        {
            return _config.Clone() as AdvancedPoolConfig;
        }
    }
}