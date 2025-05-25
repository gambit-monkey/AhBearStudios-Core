using System;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Pooling.Builders
{
    /// <summary>
    /// Factory for creating GameObject pool configuration builders.
    /// This partial class handles all GameObject-specific pool configuration creation.
    /// </summary>
    public static partial class PoolConfigBuilderFactory
    {
        /// <summary>
        /// Creates a GameObject pool configuration builder with default settings
        /// </summary>
        /// <returns>A new GameObject pool configuration builder</returns>
        public static GameObjectPoolConfigBuilder GameObject()
        {
            return new GameObjectPoolConfigBuilder();
        }

        /// <summary>
        /// Creates a GameObject pool configuration builder with specified initial capacity
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new GameObject pool configuration builder with specified capacity</returns>
        public static GameObjectPoolConfigBuilder GameObject(int initialCapacity)
        {
            return new GameObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
        }

        /// <summary>
        /// Creates a high-performance GameObject pool configuration builder
        /// Optimizes for speed with minimal overhead for frequently used GameObjects.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new high-performance GameObject pool configuration builder</returns>
        public static GameObjectPoolConfigBuilder GameObjectHighPerformance(int initialCapacity = 64)
        {
            return new GameObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsHighPerformance();
        }

        /// <summary>
        /// Creates a debug-friendly GameObject pool configuration builder
        /// Enables comprehensive logging, metrics, and validation for development.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new debug-friendly GameObject pool configuration builder</returns>
        public static GameObjectPoolConfigBuilder GameObjectDebug(int initialCapacity = 16)
        {
            return new GameObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsDebug();
        }

        /// <summary>
        /// Creates a balanced GameObject pool configuration builder
        /// Provides a compromise between performance and safety features.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new balanced GameObject pool configuration builder</returns>
        public static GameObjectPoolConfigBuilder GameObjectBalanced(int initialCapacity = 32)
        {
            return new GameObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsBalanced();
        }

        /// <summary>
        /// Creates a memory-efficient GameObject pool configuration builder
        /// Optimizes for minimal memory usage with aggressive shrinking.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new memory-efficient GameObject pool configuration builder</returns>
        public static GameObjectPoolConfigBuilder GameObjectMemoryEfficient(int initialCapacity = 16)
        {
            return new GameObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsMemoryEfficient();
        }

        /// <summary>
        /// Creates a GameObject pool configuration builder optimized for UI elements
        /// Settings focus on efficient UI element pooling with appropriate transform handling.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new UI-optimized GameObject pool configuration builder</returns>
        public static GameObjectPoolConfigBuilder GameObjectUIOptimized(int initialCapacity = 32)
        {
            return new GameObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsUIOptimized();
        }

        /// <summary>
        /// Creates a GameObject pool configuration builder optimized for frequent use
        /// Settings focus on reusing GameObjects efficiently in dynamic environments.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new frequent-use GameObject pool configuration builder</returns>
        public static GameObjectPoolConfigBuilder GameObjectFrequentUse(int initialCapacity = 64)
        {
            return new GameObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsFrequentUse();
        }

        /// <summary>
        /// Creates a GameObject pool configuration builder optimized for scene transitions
        /// Optimizes for GameObjects that need to persist between scene loads.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new scene-transition GameObject pool configuration builder</returns>
        public static GameObjectPoolConfigBuilder GameObjectSceneTransition(int initialCapacity = 32)
        {
            return new GameObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsSceneTransition();
        }

        /// <summary>
        /// Creates a GameObject pool configuration builder from an existing configuration
        /// </summary>
        /// <param name="existingConfig">The existing configuration to copy settings from</param>
        /// <returns>A new GameObject pool configuration builder initialized with existing settings</returns>
        public static GameObjectPoolConfigBuilder FromExistingGameObjectConfig(IPoolConfig existingConfig)
        {
            if (existingConfig == null)
            {
                throw new ArgumentNullException(nameof(existingConfig), "Existing configuration cannot be null");
            }

            var builder = new GameObjectPoolConfigBuilder();
            
            // Copy base properties
            builder.WithInitialCapacity(existingConfig.InitialCapacity)
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

            // Copy GameObject-specific properties if available
            if (existingConfig is GameObjectPoolConfig gameObjectConfig)
            {
                builder
                    .WithDisableOnRelease(gameObjectConfig.DisableOnRelease)
                    .WithReparentOnRelease(gameObjectConfig.ReparentOnRelease)
                    .WithToggleActive(gameObjectConfig.ToggleActive)
                    .WithCallPoolEvents(gameObjectConfig.CallPoolEvents)
                    .WithValidateOnAcquire(gameObjectConfig.ValidateOnAcquire)
                    .WithActiveLayers(gameObjectConfig.ActiveLayer, gameObjectConfig.InactiveLayer)
                    .WithParentTransform(gameObjectConfig.ParentTransform)
                    .WithResetPositionOnRelease(gameObjectConfig.ResetPositionOnRelease)
                    .WithResetRotationOnRelease(gameObjectConfig.ResetRotationOnRelease)
                    .WithResetScaleOnRelease(gameObjectConfig.ResetScaleOnRelease);
            }

            return builder;
        }

        /// <summary>
        /// Creates a GameObject pool configuration builder and registers it with the provided registry
        /// </summary>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="configName">The name to register the configuration under</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new GameObject pool configuration builder</returns>
        public static GameObjectPoolConfigBuilder GameObjectWithRegistry(
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
            
            var builder = new GameObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
                
            // Register when built
            var config = builder.Build();
            registry.RegisterConfig(configName, config);
            
            // Return builder for further configuration if needed
            return new GameObjectPoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a GameObject pool configuration builder registered for a specific type
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new GameObject pool configuration builder</returns>
        public static GameObjectPoolConfigBuilder GameObjectForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 32) where T : Component
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }
            
            var builder = new GameObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
                
            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);
            
            // Return builder for further configuration if needed
            return new GameObjectPoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a GameObject pool configuration builder with both name and type registration
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="configName">The name to register the configuration under</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new GameObject pool configuration builder</returns>
        public static GameObjectPoolConfigBuilder GameObjectWithNameAndType<T>(
            IPoolConfigRegistry registry,
            string configName,
            int initialCapacity = 32) where T : Component
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }
            
            if (string.IsNullOrEmpty(configName))
            {
                throw new ArgumentException("Config name cannot be null or empty", nameof(configName));
            }
            
            var builder = new GameObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
                
            // Register when built
            var config = builder.Build();
            registry.RegisterConfig<T>(configName, config);
            
            // Return builder for further configuration if needed
            return new GameObjectPoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a high-performance GameObject pool configuration builder for a specific type
        /// Optimizes for speed with minimal overhead for frequently used GameObjects.
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new high-performance GameObject pool configuration builder</returns>
        public static GameObjectPoolConfigBuilder GameObjectHighPerformanceForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 64) where T : Component
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            var builder = GameObjectHighPerformance(initialCapacity);
            
            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);
            
            // Return builder for further configuration if needed
            return new GameObjectPoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a UI-optimized GameObject pool configuration builder for a specific type
        /// Settings focus on efficient UI element pooling with appropriate transform handling.
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new UI-optimized GameObject pool configuration builder</returns>
        public static GameObjectPoolConfigBuilder GameObjectUIOptimizedForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 32) where T : Component
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            var builder = GameObjectUIOptimized(initialCapacity);
            
            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);
            
            // Return builder for further configuration if needed
            return new GameObjectPoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a memory-efficient GameObject pool configuration builder for a specific type
        /// Optimizes for minimal memory usage with aggressive shrinking.
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new memory-efficient GameObject pool configuration builder</returns>
        public static GameObjectPoolConfigBuilder GameObjectMemoryEfficientForType<T>(
            IPoolConfigRegistry registry,
            int initialCapacity = 16) where T : Component
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }

            var builder = GameObjectMemoryEfficient(initialCapacity);
            
            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);
            
            // Return builder for further configuration if needed
            return new GameObjectPoolConfigBuilder(config);
        }

        /// <summary>
        /// Creates a GameObject pool configuration builder with parent transform for a specific type
        /// </summary>
        /// <typeparam name="T">The type to register the configuration for</typeparam>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="parentTransform">The transform to parent pooled objects to</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new GameObject pool configuration builder with parent transform</returns>
        public static GameObjectPoolConfigBuilder GameObjectWithParentForType<T>(
            IPoolConfigRegistry registry,
            Transform parentTransform,
            int initialCapacity = 32) where T : Component
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }
            
            if (parentTransform == null)
            {
                throw new ArgumentNullException(nameof(parentTransform), "Parent transform cannot be null");
            }
            
            var builder = new GameObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithParentTransform(parentTransform);
                
            // Register when built
            var config = builder.Build();
            registry.RegisterConfigForType<T>(config);
            
            // Return builder for further configuration if needed
            return new GameObjectPoolConfigBuilder(config);
        }
    }
}