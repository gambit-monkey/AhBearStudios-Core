using System;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Pooling.Configurations;

namespace AhBearStudios.Core.Pooling
{
    /// <summary>
    /// Factory extension for creating complex object pool configuration builders.
    /// Provides unified access to specialized complex object pool builders.
    /// </summary>
    public static partial class PoolConfigBuilderFactory
    {
        /// <summary>
        /// Creates a complex object pool configuration builder with default settings
        /// </summary>
        /// <returns>A new complex object pool configuration builder</returns>
        public static ComplexObjectPoolConfigBuilder Complex()
        {
            return new ComplexObjectPoolConfigBuilder();
        }

        /// <summary>
        /// Creates a complex object pool configuration builder with specific initial capacity
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new complex object pool configuration builder with specified capacity</returns>
        public static ComplexObjectPoolConfigBuilder Complex(int initialCapacity)
        {
            return new ComplexObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
        }

        /// <summary>
        /// Creates a complex object pool configuration builder with specific initial and maximum capacity
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum size of the pool</param>
        /// <returns>A new complex object pool configuration builder with specified capacities</returns>
        public static ComplexObjectPoolConfigBuilder Complex(int initialCapacity, int maxSize)
        {
            return new ComplexObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(maxSize);
        }

        /// <summary>
        /// Creates a complex object pool configuration builder optimized for high performance usage
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new complex object pool configuration builder optimized for performance</returns>
        public static ComplexObjectPoolConfigBuilder ComplexHighPerformance(int initialCapacity = 64)
        {
            return new ComplexObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsHighPerformance();
        }

        /// <summary>
        /// Creates a complex object pool configuration builder with debug settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new complex object pool configuration builder with debug settings</returns>
        public static ComplexObjectPoolConfigBuilder ComplexDebug(int initialCapacity = 16)
        {
            return new ComplexObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsDebug();
        }

        /// <summary>
        /// Creates a complex object pool configuration builder optimized for balanced usage
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new complex object pool configuration builder with balanced settings</returns>
        public static ComplexObjectPoolConfigBuilder ComplexBalanced(int initialCapacity = 32)
        {
            return new ComplexObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsBalanced();
        }

        /// <summary>
        /// Creates a complex object pool configuration builder optimized for memory efficiency
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new complex object pool configuration builder optimized for memory efficiency</returns>
        public static ComplexObjectPoolConfigBuilder ComplexMemoryEfficient(int initialCapacity = 16)
        {
            return new ComplexObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsMemoryEfficient();
        }

        /// <summary>
        /// Creates a complex object pool configuration builder optimized for dependency tracking
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new complex object pool configuration builder optimized for dependency tracking</returns>
        public static ComplexObjectPoolConfigBuilder ComplexDependencyTracking(int initialCapacity = 16)
        {
            return new ComplexObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsDependencyTracking();
        }

        /// <summary>
        /// Creates a complex object pool configuration builder optimized for asynchronous operations
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new complex object pool configuration builder optimized for async operations</returns>
        public static ComplexObjectPoolConfigBuilder ComplexAsyncOptimized(int initialCapacity = 32)
        {
            return new ComplexObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsAsyncOptimized();
        }

        /// <summary>
        /// Creates a complex object pool configuration builder with custom property storage settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="propertyCapacity">Initial capacity for property storage per object</param>
        /// <returns>A new complex object pool configuration builder with property storage configuration</returns>
        public static ComplexObjectPoolConfigBuilder ComplexWithPropertyStorage(int initialCapacity = 16, int propertyCapacity = 8)
        {
            return new ComplexObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithPropertyStorage(true, propertyCapacity);
        }

        /// <summary>
        /// Creates a complex object pool configuration builder optimized for thread safety
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new complex object pool configuration builder with thread-safe settings</returns>
        public static ComplexObjectPoolConfigBuilder ComplexThreadSafe(int initialCapacity = 32)
        {
            return new ComplexObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithThreadingMode(PoolThreadingMode.ThreadSafe)
                .WithPropertyStorage(false) // Property storage is not thread-safe
                .WithDependencyTracking(false); // Dependency tracking is not thread-safe
        }

        /// <summary>
        /// Creates a complex object pool configuration builder with validation features enabled
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new complex object pool configuration builder with validation features</returns>
        public static ComplexObjectPoolConfigBuilder ComplexValidating(int initialCapacity = 16)
        {
            return new ComplexObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithValidation(true)
                .WithRecreateOnValidationFailure(true)
                .WithDestroyInvalidObjects(true)
                .WithMetricsCollection(true);
        }

        /// <summary>
        /// Creates a complex object pool configuration builder from an existing configuration
        /// </summary>
        /// <param name="existingConfig">The existing configuration to copy settings from</param>
        /// <returns>A new complex object pool configuration builder initialized with existing settings</returns>
        public static ComplexObjectPoolConfigBuilder FromExistingComplexConfig(IPoolConfig existingConfig)
        {
            if (existingConfig == null)
            {
                throw new ArgumentNullException(nameof(existingConfig), "Existing configuration cannot be null");
            }

            var builder = new ComplexObjectPoolConfigBuilder()
                .WithInitialCapacity(existingConfig.InitialCapacity)
                .WithMaxSize(existingConfig.MaximumCapacity)
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

            // Copy complex-specific properties if the existing config is a ComplexObjectPoolConfig
            if (existingConfig is ComplexObjectPoolConfig complexConfig)
            {
                builder
                    .WithValidation(complexConfig.ValidateOnAcquire)
                    .WithRecreateOnValidationFailure(complexConfig.RecreateOnValidationFailure)
                    .WithDestroyInvalidObjects(complexConfig.DestroyInvalidObjects)
                    .WithClearPropertiesOnRelease(complexConfig.ClearPropertiesOnRelease)
                    .WithPropertyStorage(complexConfig.EnablePropertyStorage, complexConfig.InitialPropertyCapacity)
                    .WithDependencyTracking(complexConfig.EnableDependencyTracking, complexConfig.InitialDependencyCapacity)
                    .WithOperationTimingTracking(complexConfig.TrackOperationTimings)
                    .WithAsyncDisposal(complexConfig.UseAsyncDisposal);
            }

            return builder;
        }
    }
}