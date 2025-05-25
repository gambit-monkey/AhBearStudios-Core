using System;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Pooling.Builders
{
    /// <summary>
    /// Factory for creating various native pool configuration builders.
    /// Part of the PoolConfigBuilderFactory that focuses on NativePoolConfigBuilder creation.
    /// </summary>
    public static partial class PoolConfigBuilderFactory
    {
        /// <summary>
        /// Creates a native pool configuration builder with default settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new native pool configuration builder</returns>
        public static NativePoolConfigBuilder Native(int initialCapacity = 32)
        {
            return new NativePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
        }

        /// <summary>
        /// Creates a native pool configuration builder with specified initial capacity and allocator
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="allocator">Native allocator to use</param>
        /// <returns>A new native pool configuration builder with specified allocator</returns>
        public static NativePoolConfigBuilder Native(int initialCapacity, Allocator allocator)
        {
            return new NativePoolConfigBuilder(initialCapacity, allocator);
        }

        /// <summary>
        /// Creates a high-performance native pool configuration builder
        /// Optimizes for maximum speed by disabling safety checks and diagnostics.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new high-performance native pool configuration builder</returns>
        public static NativePoolConfigBuilder NativeHighPerformance(int initialCapacity = 32)
        {
            return new NativePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsHighPerformance();
        }

        /// <summary>
        /// Creates a job-compatible native pool configuration builder
        /// Optimizes for use with Unity's Job System in multithreaded scenarios.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new job-compatible native pool configuration builder</returns>
        public static NativePoolConfigBuilder NativeJobCompatible(int initialCapacity = 32)
        {
            return new NativePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsJobCompatible();
        }

        /// <summary>
        /// Creates a Burst-compatible native pool configuration builder
        /// Optimizes for use with Unity's Burst compiler for maximum performance.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new Burst-compatible native pool configuration builder</returns>
        public static NativePoolConfigBuilder NativeBurstCompatible(int initialCapacity = 32)
        {
            return new NativePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsBurstCompatible();
        }

        /// <summary>
        /// Creates a debug-friendly native pool configuration builder
        /// Enables all safety checks and extensive logging.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new debug-friendly native pool configuration builder</returns>
        public static NativePoolConfigBuilder NativeDebug(int initialCapacity = 16)
        {
            return new NativePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsDebug();
        }

        /// <summary>
        /// Creates a memory-efficient native pool configuration builder
        /// Optimizes for minimal memory footprint with automatic shrinking.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new memory-efficient native pool configuration builder</returns>
        public static NativePoolConfigBuilder NativeMemoryEfficient(int initialCapacity = 16)
        {
            return new NativePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsMemoryEfficient();
        }

        /// <summary>
        /// Creates a thread-safe native pool configuration builder
        /// Optimizes for manual multithreading scenarios outside the Job System.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new thread-safe native pool configuration builder</returns>
        public static NativePoolConfigBuilder NativeThreadSafe(int initialCapacity = 32)
        {
            return new NativePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsThreadSafe();
        }

        /// <summary>
        /// Creates a balanced native pool configuration builder
        /// Provides good performance while maintaining safety and monitoring.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new balanced native pool configuration builder</returns>
        public static NativePoolConfigBuilder NativeBalanced(int initialCapacity = 32)
        {
            return new NativePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsBalanced();
        }

        /// <summary>
        /// Creates a native pool configuration builder from an existing configuration
        /// </summary>
        /// <param name="existingConfig">The existing configuration to copy settings from</param>
        /// <returns>A new native pool configuration builder initialized with existing settings</returns>
        public static NativePoolConfigBuilder FromExistingNativeConfig(IPoolConfig existingConfig)
        {
            if (existingConfig == null)
            {
                throw new ArgumentNullException(nameof(existingConfig), "Existing configuration cannot be null");
            }

            // If the config is already a NativePoolConfig, we can use its ToBuilder method
            if (existingConfig is NativePoolConfig nativeConfig)
            {
                return nativeConfig.ToBuilder();
            }

            // Otherwise, create a new builder from the base config
            return new NativePoolConfigBuilder()
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
                .WithWarningLogging(existingConfig.LogWarnings)
                .WithMetricsCollection(existingConfig.CollectMetrics)
                .WithDetailedLogging(existingConfig.DetailedLogging)
                .WithResetOnRelease(existingConfig.ResetOnRelease);
        }

        /// <summary>
        /// Creates a native pool configuration builder and registers it with the provided registry
        /// </summary>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="configName">The name to register the configuration under</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new native pool configuration builder</returns>
        public static NativePoolConfigBuilder NativeWithRegistry(
            IPoolConfigRegistry registry,
            string configName,
            int initialCapacity = 32)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            if (string.IsNullOrEmpty(configName))
            {
                throw new ArgumentException("Config name cannot be null or empty", nameof(configName));
            }

            var builder = new NativePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfig(configName, config);

            // Return builder for further configuration if needed
            return new NativePoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a native pool configuration builder registered for a specific type
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new native pool configuration builder</returns>
        public static NativePoolConfigBuilder NativeForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 32) where T : class
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            var builder = new NativePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);

            // Return builder for further configuration if needed
            return new NativePoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a native pool configuration builder with both name and type registration
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="configName">The name to register the configuration under</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new native pool configuration builder</returns>
        public static NativePoolConfigBuilder NativeWithNameAndType<T>(
            IPoolConfigRegistry registry,
            string configName,
            int initialCapacity = 32) where T : class
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            if (string.IsNullOrEmpty(configName))
            {
                throw new ArgumentException("Config name cannot be null or empty", nameof(configName));
            }

            var builder = new NativePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            // Register when built
            var config = builder.Build();
            registry.RegisterConfig<T>(configName, config);

            // Return builder for further configuration if needed
            return new NativePoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a job-compatible native pool configuration builder for a specific type
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new job-compatible native pool configuration builder</returns>
        public static NativePoolConfigBuilder JobCompatibleForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 32) where T : class
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            var builder = new NativePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsJobCompatible();

            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);

            // Return builder for further configuration if needed
            return new NativePoolConfigBuilder(config);
        }
    }
}