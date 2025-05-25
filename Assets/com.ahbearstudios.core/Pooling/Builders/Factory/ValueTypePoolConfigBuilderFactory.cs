using System;
using AhBearStudios.Pooling.Configurations;
using Unity.Collections;

namespace AhBearStudios.Pooling.Builders
{
    /// <summary>
    /// Factory extension for creating value type pool configuration builders.
    /// Provides unified access to all specialized value type pool configuration options.
    /// </summary>
    public static partial class PoolConfigBuilderFactory
    {
        /// <summary>
        /// Creates a value type pool configuration builder with default settings
        /// </summary>
        /// <returns>A new value type pool configuration builder</returns>
        public static ValueTypePoolConfigBuilder ValueType()
        {
            return new ValueTypePoolConfigBuilder();
        }

        /// <summary>
        /// Creates a value type pool configuration builder optimized for high performance
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new value type pool configuration builder with high performance settings</returns>
        public static ValueTypePoolConfigBuilder ValueTypeHighPerformance(int initialCapacity = 128)
        {
            return new ValueTypePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(initialCapacity * 4)
                .WithZeroMemoryOnRelease(false)
                .WithStructLayout(StructLayoutType.Sequential)
                .WithInlineHandling(true)
                .WithBlittableOptimization(true);
        }

        /// <summary>
        /// Creates a value type pool configuration builder optimized for memory efficiency
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new value type pool configuration builder with memory-efficient settings</returns>
        public static ValueTypePoolConfigBuilder ValueTypeMemoryEfficient(int initialCapacity = 32)
        {
            return new ValueTypePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(initialCapacity * 2)
                .WithAutoShrink(true)
                .WithShrinkThreshold(0.4f)
                .WithShrinkInterval(5.0f)
                .WithExponentialGrowth(false)
                .WithZeroMemoryOnRelease(true);
        }

        /// <summary>
        /// Creates a value type pool configuration builder optimized for burst-compatible operations
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Native allocator to use</param>
        /// <returns>A new value type pool configuration builder optimized for Burst</returns>
        public static ValueTypePoolConfigBuilder ValueTypeBurstCompatible(int initialCapacity = 64, Allocator allocator = Allocator.Persistent)
        {
            return new ValueTypePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithBlittableOptimization(true)
                .WithStructLayout(StructLayoutType.Sequential)
                .WithNativeAllocator(allocator)
                .WithMemoryAlignment(16)
                .WithJobCompatibility(true);
        }

        /// <summary>
        /// Creates a value type pool configuration builder for debug purposes
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new value type pool configuration builder with debug settings</returns>
        public static ValueTypePoolConfigBuilder ValueTypeDebug(int initialCapacity = 32)
        {
            return new ValueTypePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithZeroMemoryOnRelease(true)
                .WithValidationChecks(true)
                .WithMetricsCollection(true)
                .WithDetailedLogging(true)
                .WithWarningLogging(true)
                .WithOverflowHandling(OverflowHandlingType.ThrowException);
        }

        /// <summary>
        /// Creates a value type pool configuration builder for temporary usage
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new value type pool configuration builder optimized for temporary usage</returns>
        public static ValueTypePoolConfigBuilder ValueTypeTemporary(int initialCapacity = 32)
        {
            return new ValueTypePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(initialCapacity * 2)
                .WithNativeAllocator(Allocator.TempJob)
                .WithAutomaticDisposal(true)
                .WithDisposeTimeout(30.0f);
        }

        /// <summary>
        /// Creates a value type pool configuration builder optimized for SIMD operations
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new value type pool configuration builder optimized for SIMD</returns>
        public static ValueTypePoolConfigBuilder ValueTypeSIMD(int initialCapacity = 128)
        {
            return new ValueTypePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithStructLayout(StructLayoutType.Sequential)
                .WithMemoryAlignment(32)
                .WithBlittableOptimization(true)
                .WithInlineHandling(true)
                .WithNativeAllocator(Allocator.Persistent);
        }

        /// <summary>
        /// Creates a value type pool configuration builder from an existing configuration
        /// </summary>
        /// <param name="existingConfig">The existing configuration to copy settings from</param>
        /// <returns>A new value type pool configuration builder initialized with existing settings</returns>
        /// <exception cref="ArgumentNullException">Thrown if existingConfig is null</exception>
        public static ValueTypePoolConfigBuilder FromExistingValueTypeConfig(IPoolConfig existingConfig)
        {
            if (existingConfig == null)
                throw new ArgumentNullException(nameof(existingConfig), "Existing configuration cannot be null");

            return new ValueTypePoolConfigBuilder(existingConfig);
        }

        /// <summary>
        /// Creates a high-performance value type pool configuration builder for a specific type
        /// Optimized for maximum throughput with SIMD optimization enabled.
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new high-performance value type pool configuration builder</returns>
        /// <exception cref="ArgumentNullException">Thrown if registry is null</exception>
        public static ValueTypePoolConfigBuilder ValueTypeHighPerformanceForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 128) where T : struct
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");

            var builder = ValueTypeHighPerformance(initialCapacity);
            
            // Register when built
            var config = builder.Build();
            registry.RegisterConfig(typeof(T).FullName, config);

            // Return builder for further configuration if needed
            return new ValueTypePoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a SIMD-optimized value type pool configuration builder for a specific type
        /// Ensures proper memory alignment and blittable type constraints.
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new SIMD-optimized value type pool configuration builder</returns>
        /// <exception cref="ArgumentNullException">Thrown if registry is null</exception>
        public static ValueTypePoolConfigBuilder ValueTypeSimdOptimizedForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 64) where T : struct
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");

            var builder = ValueTypeSIMD(initialCapacity);
            
            var config = builder.Build();
            registry.RegisterConfig(typeof(T).FullName, config);

            return new ValueTypePoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a Burst-compatible value type pool configuration builder for a specific type
        /// Ensures compatibility with Unity's Burst compiler.
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Native allocator to use</param>
        /// <returns>A new Burst-compatible value type pool configuration builder</returns>
        /// <exception cref="ArgumentNullException">Thrown if registry is null</exception>
        public static ValueTypePoolConfigBuilder ValueTypeBurstCompatibleForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 32,
            Allocator allocator = Allocator.Persistent) where T : struct
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");

            var builder = ValueTypeBurstCompatible(initialCapacity, allocator);

            var config = builder.Build();
            registry.RegisterConfig(typeof(T).FullName, config);

            return new ValueTypePoolConfigBuilder(config);
        }
    }
}