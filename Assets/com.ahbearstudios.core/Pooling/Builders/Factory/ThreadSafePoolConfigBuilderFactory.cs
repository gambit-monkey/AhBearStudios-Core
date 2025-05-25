using System;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace AhBearStudios.Pooling.Builders
{
    /// <summary>
    /// Factory for creating various thread-safe pool configuration builders.
    /// Part of the PoolConfigBuilderFactory that focuses on ThreadSafePoolConfigBuilder creation.
    /// </summary>
    public static partial class PoolConfigBuilderFactory
    {
        /// <summary>
        /// Creates a thread-safe pool configuration builder with default settings
        /// </summary>
        /// <returns>A new thread-safe pool configuration builder</returns>
        public static ThreadSafePoolConfigBuilder ThreadSafe()
        {
            return new ThreadSafePoolConfigBuilder();
        }

        /// <summary>
        /// Creates a thread-safe pool configuration builder with specified initial capacity
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new thread-safe pool configuration builder with specified capacity</returns>
        public static ThreadSafePoolConfigBuilder ThreadSafe(int initialCapacity)
        {
            return new ThreadSafePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
        }

        /// <summary>
        /// Creates a high-performance thread-safe pool configuration builder
        /// Optimizes for maximum throughput in multi-threaded scenarios.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new high-performance thread-safe pool configuration builder</returns>
        public static ThreadSafePoolConfigBuilder ThreadSafeHighPerformance(int initialCapacity = 64)
        {
            return new ThreadSafePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsHighPerformance();
        }

        /// <summary>
        /// Creates a debug-friendly thread-safe pool configuration builder
        /// Enables extensive tracking, validation, and logging features.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new debug-friendly thread-safe pool configuration builder</returns>
        public static ThreadSafePoolConfigBuilder ThreadSafeDebug(int initialCapacity = 16)
        {
            return new ThreadSafePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsDebug();
        }

        /// <summary>
        /// Creates a memory-efficient thread-safe pool configuration builder
        /// Optimizes for reduced memory usage in multi-threaded scenarios.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new memory-efficient thread-safe pool configuration builder</returns>
        public static ThreadSafePoolConfigBuilder ThreadSafeMemoryEfficient(int initialCapacity = 8)
        {
            return new ThreadSafePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsMemoryEfficient();
        }

        /// <summary>
        /// Creates a thread-safe pool configuration builder optimized for high concurrency
        /// Ensures performance under high contention from multiple threads.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new high-concurrency thread-safe pool configuration builder</returns>
        public static ThreadSafePoolConfigBuilder ThreadSafeHighConcurrency(int initialCapacity = 32)
        {
            return new ThreadSafePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsHighConcurrency();
        }

        /// <summary>
        /// Creates a thread-safe pool configuration builder with optimized lock settings
        /// Reduces contention and improves performance in heavily contested scenarios.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new thread-safe pool configuration builder with optimized locks</returns>
        public static ThreadSafePoolConfigBuilder ThreadSafeWithOptimizedLocks(int initialCapacity = 32)
        {
            return new ThreadSafePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsWithOptimizedLocks();
        }

        /// <summary>
        /// Creates a thread-safe pool configuration builder with monitoring capabilities
        /// Enables tracking and diagnostics for performance analysis.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new thread-safe pool configuration builder with monitoring</returns>
        public static ThreadSafePoolConfigBuilder ThreadSafeWithMonitoring(int initialCapacity = 16)
        {
            return new ThreadSafePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsWithMonitoring();
        }

        /// <summary>
        /// Creates a thread-safe pool configuration builder with balanced settings
        /// Provides good performance while maintaining safety and monitoring.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new balanced thread-safe pool configuration builder</returns>
        public static ThreadSafePoolConfigBuilder ThreadSafeBalanced(int initialCapacity = 32)
        {
            return new ThreadSafePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsBalanced();
        }

        /// <summary>
        /// Creates a thread-safe pool configuration builder optimized for Unity Job System
        /// Ensures compatibility and performance with Unity's job system.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new job-optimized thread-safe pool configuration builder</returns>
        public static ThreadSafePoolConfigBuilder ThreadSafeJobOptimized(int initialCapacity = 32)
        {
            return new ThreadSafePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithOperationRetryCount(5)
                .WithLockFreeAlgorithms(true)
                .WithConcurrentCollections(true)
                .WithExponentialGrowth(true)
                .WithGrowthFactor(2.0f)
                .WithMaxSize(Math.Max(initialCapacity * 4, 256))
                .WithConcurrencyWarningThreshold(JobsUtility.MaxJobThreadCount * 16);
        }

        /// <summary>
        /// Creates a thread-safe pool configuration builder from an existing configuration
        /// </summary>
        /// <param name="existingConfig">The existing configuration to copy settings from</param>
        /// <returns>A new thread-safe pool configuration builder initialized with existing settings</returns>
        public static ThreadSafePoolConfigBuilder FromExistingThreadSafeConfig(IPoolConfig existingConfig)
        {
            if (existingConfig == null)
            {
                throw new ArgumentNullException(nameof(existingConfig), "Existing configuration cannot be null");
            }

            // If the config is already a ThreadSafePoolConfig, we can use its ToBuilder method
            if (existingConfig is ThreadSafePoolConfig threadSafeConfig)
            {
                return threadSafeConfig.ToBuilder();
            }

            // Otherwise, create a new builder with basic settings from the existing config
            return new ThreadSafePoolConfigBuilder()
                .WithInitialCapacity(existingConfig.InitialCapacity)
                .WithMaxSize(existingConfig.MaximumCapacity)
                .WithPrewarming(existingConfig.PrewarmOnInit)
                .WithExponentialGrowth(existingConfig.UseExponentialGrowth)
                .WithGrowthFactor(existingConfig.GrowthFactor)
                .WithGrowthIncrement(existingConfig.GrowthIncrement)
                .WithAutoShrink(existingConfig.EnableAutoShrink)
                .WithShrinkThreshold(existingConfig.ShrinkThreshold)
                .WithShrinkInterval(existingConfig.ShrinkInterval)
                .WithWarningLogging(existingConfig.LogWarnings)
                .WithMetricsCollection(existingConfig.CollectMetrics)
                .WithDetailedLogging(existingConfig.DetailedLogging)
                .WithResetOnRelease(existingConfig.ResetOnRelease)
                .WithNativeAllocator(existingConfig.NativeAllocator)
                .WithExceptionOnExceedingMaxCount(existingConfig.ThrowIfExceedingMaxCount);
        }

        /// <summary>
        /// Creates a thread-safe pool configuration builder and registers it with the provided registry
        /// </summary>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="configName">The name to register the configuration under</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new thread-safe pool configuration builder</returns>
        public static ThreadSafePoolConfigBuilder ThreadSafeWithRegistry(
            IPoolConfigRegistry registry,
            string configName,
            int initialCapacity = 16)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            if (string.IsNullOrEmpty(configName))
            {
                throw new ArgumentException("Config name cannot be null or empty", nameof(configName));
            }

            var builder = new ThreadSafePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfig(configName, config);

            // Return builder for further configuration if needed
            return new ThreadSafePoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a thread-safe pool configuration builder registered for a specific type
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new thread-safe pool configuration builder</returns>
        public static ThreadSafePoolConfigBuilder ThreadSafeForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 16) where T : class
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            var builder = new ThreadSafePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);

            // Return builder for further configuration if needed
            return new ThreadSafePoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a thread-safe pool configuration builder with both name and type registration
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="configName">The name to register the configuration under</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new thread-safe pool configuration builder</returns>
        public static ThreadSafePoolConfigBuilder ThreadSafeWithNameAndType<T>(
            IPoolConfigRegistry registry,
            string configName,
            int initialCapacity = 16) where T : class
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            if (string.IsNullOrEmpty(configName))
            {
                throw new ArgumentException("Config name cannot be null or empty", nameof(configName));
            }

            var builder = new ThreadSafePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfig<T>(configName, config);

            // Return builder for further configuration if needed
            return new ThreadSafePoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a high-concurrency thread-safe pool configuration builder for a specific type
        /// Optimized for scenarios with high contention from multiple threads.
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new high-concurrency thread-safe pool configuration builder</returns>
        public static ThreadSafePoolConfigBuilder ThreadSafeHighConcurrencyForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 64) where T : class
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            var builder = ThreadSafeHighConcurrency(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);

            // Return builder for further configuration if needed
            return new ThreadSafePoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a job-optimized thread-safe pool configuration builder for a specific type
        /// Optimized for use with Unity's job system.
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new job-optimized thread-safe pool configuration builder</returns>
        public static ThreadSafePoolConfigBuilder ThreadSafeJobOptimizedForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 32) where T : class
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            var builder = ThreadSafeJobOptimized(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);

            // Return builder for further configuration if needed
            return new ThreadSafePoolConfigBuilder(config);
        }
    }
}