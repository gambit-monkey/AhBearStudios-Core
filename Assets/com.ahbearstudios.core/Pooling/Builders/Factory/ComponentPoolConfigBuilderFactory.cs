using System;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Pooling.Builders
{
    /// <summary>
    /// Factory extension for creating component pool configuration builders.
    /// Provides unified access to specialized component pool builders.
    /// </summary>
    public static partial class PoolConfigBuilderFactory
    {
        /// <summary>
        /// Creates a component pool configuration builder with default settings
        /// </summary>
        /// <returns>A new component pool configuration builder</returns>
        public static ComponentPoolConfigBuilder Component()
        {
            return new ComponentPoolConfigBuilder();
        }

        /// <summary>
        /// Creates a component pool configuration builder with specific initial capacity
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new component pool configuration builder with specified capacity</returns>
        public static ComponentPoolConfigBuilder Component(int initialCapacity)
        {
            return new ComponentPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
        }

        /// <summary>
        /// Creates a component pool configuration builder with specific initial and maximum capacity
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum size of the pool</param>
        /// <returns>A new component pool configuration builder with specified capacities</returns>
        public static ComponentPoolConfigBuilder Component(int initialCapacity, int maxSize)
        {
            return new ComponentPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMaxSize(maxSize);
        }

        /// <summary>
        /// Creates a component pool configuration builder with specific parent transform
        /// </summary>
        /// <param name="parentTransform">Parent transform for pooled components</param>
        /// <returns>A new component pool configuration builder with specified parent transform</returns>
        public static ComponentPoolConfigBuilder Component(Transform parentTransform)
        {
            return new ComponentPoolConfigBuilder()
                .WithParentTransform(parentTransform);
        }

        /// <summary>
        /// Creates a component pool configuration builder with specific initial capacity and parent transform
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="parentTransform">Parent transform for pooled components</param>
        /// <returns>A new component pool configuration builder with specified capacity and parent</returns>
        public static ComponentPoolConfigBuilder Component(int initialCapacity, Transform parentTransform)
        {
            return new ComponentPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithParentTransform(parentTransform);
        }

        /// <summary>
        /// Creates a component pool configuration builder optimized for high performance usage
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new component pool configuration builder optimized for performance</returns>
        public static ComponentPoolConfigBuilder ComponentHighPerformance(int initialCapacity = 64)
        {
            return new ComponentPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsHighPerformance();
        }

        /// <summary>
        /// Creates a component pool configuration builder with debug settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new component pool configuration builder with debug settings</returns>
        public static ComponentPoolConfigBuilder ComponentDebug(int initialCapacity = 16)
        {
            return new ComponentPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsDebug();
        }

        /// <summary>
        /// Creates a component pool configuration builder optimized for balanced usage
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new component pool configuration builder with balanced settings</returns>
        public static ComponentPoolConfigBuilder ComponentBalanced(int initialCapacity = 32)
        {
            return new ComponentPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsBalanced();
        }

        /// <summary>
        /// Creates a component pool configuration builder optimized for memory efficiency
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new component pool configuration builder optimized for memory efficiency</returns>
        public static ComponentPoolConfigBuilder ComponentMemoryEfficient(int initialCapacity = 16)
        {
            return new ComponentPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsMemoryEfficient();
        }

        /// <summary>
        /// Creates a component pool configuration builder optimized for UI components
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="parent">Parent transform for UI components</param>
        /// <returns>A new component pool configuration builder optimized for UI components</returns>
        public static ComponentPoolConfigBuilder ComponentUIOptimized(int initialCapacity = 16, Transform parent = null)
        {
            var builder = new ComponentPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsUIOptimized();
                
            if (parent != null)
            {
                builder.WithParentTransform(parent);
            }
            
            return builder;
        }

        /// <summary>
        /// Creates a component pool configuration builder optimized for frequent use scenarios
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new component pool configuration builder optimized for frequent use</returns>
        public static ComponentPoolConfigBuilder ComponentFrequentUse(int initialCapacity = 32)
        {
            return new ComponentPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsFrequentUse();
        }

        /// <summary>
        /// Creates a component pool configuration builder with lifecycle method handling disabled
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new component pool configuration builder with lifecycle methods disabled</returns>
        public static ComponentPoolConfigBuilder ComponentNoLifecycleMethods(int initialCapacity = 32)
        {
            return new ComponentPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithLifecycleMethods(false)
                .WithDisableOnRelease(true)
                .WithHierarchyDetachment(false);
        }

        /// <summary>
        /// Creates a component pool configuration builder with hierarchy management optimizations
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="detachFromHierarchy">Whether to detach components from hierarchy when released</param>
        /// <returns>A new component pool configuration builder with hierarchy optimizations</returns>
        public static ComponentPoolConfigBuilder ComponentHierarchyOptimized(int initialCapacity = 32, bool detachFromHierarchy = true)
        {
            return new ComponentPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithHierarchyDetachment(detachFromHierarchy)
                .WithDisableOnRelease(true)
                .WithWarningLogging(false)
                .WithAutoShrink(false);
        }

        /// <summary>
        /// Creates a component pool configuration builder from an existing configuration
        /// </summary>
        /// <param name="existingConfig">The existing configuration to copy settings from</param>
        /// <returns>A new component pool configuration builder initialized with existing settings</returns>
        public static ComponentPoolConfigBuilder FromExistingComponentConfig(IPoolConfig existingConfig)
        {
            if (existingConfig == null)
            {
                throw new ArgumentNullException(nameof(existingConfig), "Existing configuration cannot be null");
            }

            var builder = new ComponentPoolConfigBuilder()
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

            // Copy component-specific properties if the existing config is a ComponentPoolConfig
            if (existingConfig is ComponentPoolConfig componentConfig)
            {
                builder
                    .WithComponentReset(componentConfig.ResetComponentOnRelease)
                    .WithHierarchyDetachment(componentConfig.DetachFromHierarchy)
                    .WithDisableOnRelease(componentConfig.DisableOnRelease)
                    .WithLifecycleMethods(componentConfig.InvokeLifecycleMethods);
                
                if (componentConfig.UseParentTransform && componentConfig.ParentTransform != null)
                {
                    builder.WithParentTransform(componentConfig.ParentTransform);
                }
            }

            return builder;
        }

        /// <summary>
        /// Creates a component pool configuration builder and registers it with the provided registry
        /// </summary>
        /// <param name="registry">The registry to register the configuration with</param>
        /// <param name="configName">The name to register the configuration under</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new component pool configuration builder</returns>
        public static ComponentPoolConfigBuilder ComponentWithRegistry(IPoolConfigRegistry registry, string configName, int initialCapacity = 16)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
            }
            
            if (string.IsNullOrEmpty(configName))
            {
                throw new ArgumentException("Config name cannot be null or empty", nameof(configName));
            }
            
            var builder = new ComponentPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
                
            // Register when built
            var config = builder.Build();
            registry.RegisterConfig(configName, config);
            
            // Return builder for further configuration if needed
            return new ComponentPoolConfigBuilder(config);
        }
    }
}