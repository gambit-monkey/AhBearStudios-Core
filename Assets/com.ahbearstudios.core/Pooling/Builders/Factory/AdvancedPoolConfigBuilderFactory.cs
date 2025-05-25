using System;
using AhBearStudios.Core.Pooling.Configurations;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Factory for creating various advanced pool configuration builders.
    /// Part of the PoolConfigBuilderFactory that focuses on AdvancedPoolConfigBuilder creation.
    /// </summary>
    public static partial class PoolConfigBuilderFactory
    {
        /// <summary>
        /// Creates an advanced pool configuration builder with default settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new advanced pool configuration builder</returns>
        public static AdvancedPoolConfigBuilder Advanced(int initialCapacity = 32)
        {
            return new AdvancedPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
        }

        /// <summary>
        /// Creates an advanced pool configuration builder optimized for high performance
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new advanced pool configuration builder with high performance settings</returns>
        public static AdvancedPoolConfigBuilder AdvancedHighPerformance(int initialCapacity = 64)
        {
            return new AdvancedPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsHighPerformance()
                .WithMaximumCapacity(initialCapacity * 4);
        }

        /// <summary>
        /// Creates an advanced pool configuration builder with debug settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new advanced pool configuration builder with debug settings</returns>
        public static AdvancedPoolConfigBuilder AdvancedDebug(int initialCapacity = 16)
        {
            return new AdvancedPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsDebug();
        }

        /// <summary>
        /// Creates an advanced pool configuration builder optimized for memory efficiency
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new advanced pool configuration builder optimized for memory efficiency</returns>
        public static AdvancedPoolConfigBuilder AdvancedMemoryEfficient(int initialCapacity = 16)
        {
            return new AdvancedPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMinimumCapacity(Math.Max(1, initialCapacity / 2))
                .WithMaximumCapacity(initialCapacity * 2)
                .WithAutoShrink(true)
                .WithShrinkThreshold(0.5f)
                .WithShrinkInterval(10.0f)
                .WithExponentialGrowth(false)
                .WithGrowthIncrement(4)
                .WithMetricsCollection(false)
                .WithDetailedLogging(false)
                .WithDynamicResizing(false);
        }

        /// <summary>
        /// Creates an advanced pool configuration builder with monitoring capabilities
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new advanced pool configuration builder with monitoring settings</returns>
        public static AdvancedPoolConfigBuilder AdvancedWithMonitoring(int initialCapacity = 32)
        {
            return new AdvancedPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMetricsCollection(true)
                .WithDetailedLogging(true)
                .WithMonitoring(true, 5.0f)
                .WithDiagnostics(true)
                .WithProfiling(true)
                .WithHealthChecks(true)
                .WithStackTraceTracking(true);
        }

        /// <summary>
        /// Creates an advanced pool configuration builder optimized for dynamic loads
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new advanced pool configuration builder optimized for dynamic loads</returns>
        public static AdvancedPoolConfigBuilder AdvancedDynamic(int initialCapacity = 32)
        {
            return new AdvancedPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithDynamicResizing(true, 0.8f, 1.75f)
                .WithExponentialGrowth(true)
                .WithGrowthFactor(1.75f)
                .WithAutoShrink(true)
                .WithShrinkThreshold(0.4f)
                .WithShrinkInterval(15.0f)
                .WithMonitoring(true, 10.0f)
                .WithHealthChecks(true);
        }

        /// <summary>
        /// Creates an advanced pool configuration builder with leak detection
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new advanced pool configuration builder with leak detection settings</returns>
        public static AdvancedPoolConfigBuilder AdvancedWithLeakDetection(int initialCapacity = 16)
        {
            return new AdvancedPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithStackTraceTracking(true)
                .WithLeakWarnings(true)
                .WithStaleItemWarnings(true)
                .WithItemLifetime(60.0f)
                .WithMonitoring(true, 5.0f)
                .WithDetailedLogging(true)
                .WithDiagnostics(true);
        }

        /// <summary>
        /// Creates an advanced pool configuration builder from an existing configuration
        /// </summary>
        /// <param name="existingConfig">The existing configuration to copy settings from</param>
        /// <returns>A new advanced pool configuration builder initialized with existing settings</returns>
        public static AdvancedPoolConfigBuilder FromExistingAdvancedConfig(IPoolConfig existingConfig)
        {
            if (existingConfig == null)
            {
                throw new ArgumentNullException(nameof(existingConfig), "Existing configuration cannot be null");
            }

            var builder = new AdvancedPoolConfigBuilder()
                .WithInitialCapacity(existingConfig.InitialCapacity)
                .WithMinimumCapacity(existingConfig.MinimumCapacity)
                .WithMaximumCapacity(existingConfig.MaximumCapacity)
                .WithPrewarming(existingConfig.PrewarmOnInit)
                .WithExponentialGrowth(existingConfig.UseExponentialGrowth)
                .WithGrowthFactor(existingConfig.GrowthFactor)
                .WithGrowthIncrement(existingConfig.GrowthIncrement)
                .WithAutoShrink(existingConfig.EnableAutoShrink)
                .WithShrinkThreshold(existingConfig.ShrinkThreshold)
                .WithShrinkInterval(existingConfig.ShrinkInterval)
                .WithThreadingMode(existingConfig.ThreadingMode)
                .WithNativeAllocator(existingConfig.NativeAllocator)
                .WithWarningLogging(existingConfig.LogWarnings)
                .WithMetricsCollection(existingConfig.CollectMetrics)
                .WithDetailedLogging(existingConfig.DetailedLogging)
                .WithResetOnRelease(existingConfig.ResetOnRelease)
                .WithExceptionOnExceedingMaxCount(existingConfig.ThrowIfExceedingMaxCount);

            if (existingConfig is AdvancedPoolConfig advancedConfig)
            {
                // Copy advanced properties if the existing config is an AdvancedPoolConfig
                builder
                    .WithDynamicResizing(advancedConfig.AllowResize, advancedConfig.ResizeThreshold, advancedConfig.ResizeMultiplier)
                    .WithMonitoring(advancedConfig.EnableMonitoring, advancedConfig.MonitoringInterval)
                    .WithItemLifetime(advancedConfig.MaxItemLifetime)
                    .WithLeakWarnings(advancedConfig.WarnOnLeakedItems)
                    .WithStaleItemWarnings(advancedConfig.WarnOnStaleItems)
                    .WithStackTraceTracking(advancedConfig.TrackAcquireStackTraces)
                    .WithValidationOnRelease(advancedConfig.ValidateOnRelease)
                    .WithThreadSafety(advancedConfig.EnableThreadSafety)
                    .WithDiagnostics(advancedConfig.EnableDiagnostics)
                    .WithProfiling(advancedConfig.EnableProfiling)
                    .WithHealthChecks(advancedConfig.EnableHealthChecks)
                    .WithMaxInactiveOnShrink(advancedConfig.MaxInactiveOnShrink);
            }

            return builder;
        }
    }
}