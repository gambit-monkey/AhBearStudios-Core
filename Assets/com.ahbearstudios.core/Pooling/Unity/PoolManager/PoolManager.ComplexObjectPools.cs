using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Core.Pooling.Core;
using AhBearStudios.Pooling.Core.Pooling.Advanced;
using Unity.Collections;

namespace AhBearStudios.Pooling.Core.Pooling
{
    /// <summary>
    /// Partial class for PoolManager that handles complex object pools with extended functionality
    /// </summary>
    public partial class PoolManager
    {
        /// <summary>
        /// Creates a complex object pool with standard configuration
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="onAcquireAction">Optional action to execute when objects are acquired</param>
        /// <param name="onReleaseAction">Optional action to execute when objects are released</param>
        /// <param name="validateAction">Optional function to validate objects before acquisition</param>
        /// <param name="prioritySelector">Optional function to determine object priority</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ComplexObjectPool</returns>
        public ComplexObjectPool<T> CreateComplexPool<T>(
            Func<T> factory,
            AdvancedPoolConfig config,
            Action<T> resetAction = null,
            Action<T> onAcquireAction = null,
            Action<T> onReleaseAction = null,
            Func<T, bool> validateAction = null,
            Func<T, int> prioritySelector = null,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            string name = poolName ?? $"ComplexPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is ComplexObjectPool<T> complexPool)
                {
                    _logger?.LogInfo($"Returning existing pool: {name}");
                    return complexPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not a ComplexObjectPool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfo($"Creating new ComplexObjectPool<{typeof(T).Name}>: {name}");
            
            // Create new pool with full configuration
            var pool = new ComplexObjectPool<T>(
                factory,
                config,
                resetAction,
                validateAction,
                onAcquireAction,
                onReleaseAction,
                prioritySelector,
                name);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a complex object pool with specified parameters
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="onAcquireAction">Optional action to execute when objects are acquired</param>
        /// <param name="onReleaseAction">Optional action to execute when objects are released</param>
        /// <param name="validateAction">Optional function to validate objects before acquisition</param>
        /// <param name="prioritySelector">Optional function to determine object priority</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new ComplexObjectPool</returns>
        public ComplexObjectPool<T> CreateComplexPool<T>(
            Func<T> factory,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            Action<T> resetAction = null,
            Action<T> onAcquireAction = null,
            Action<T> onReleaseAction = null,
            Func<T, bool> validateAction = null,
            Func<T, int> prioritySelector = null,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be non-negative");

            // Create config with specified parameters
            var config = new AdvancedPoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                Prewarm = prewarm,
                ValidateOnRelease = validateAction != null
            };

            return CreateComplexPool(
                factory,
                config,
                resetAction,
                onAcquireAction,
                onReleaseAction,
                validateAction,
                prioritySelector,
                poolName);
        }

        /// <summary>
        /// Creates a performance-optimized complex pool
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new optimized ComplexObjectPool</returns>
        public ComplexObjectPool<T> CreateOptimizedComplexPool<T>(
            Func<T> factory,
            int initialCapacity = 20,
            int maxSize = 100,
            bool prewarm = true,
            Action<T> resetAction = null,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"OptimizedComplexPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating optimized ComplexObjectPool<{typeof(T).Name}>: {name}");
            
            // Create optimized config
            var config = new AdvancedPoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                Prewarm = prewarm,
                EnableMonitoring = false,
                EnableProfiling = false,
                TrackAcquireStackTraces = false,
                EnableThreadSafety = false,
                ValidateOnRelease = false
            };
            
            return CreateComplexPool(
                factory,
                config,
                resetAction,
                null,
                null,
                null,
                null,
                name);
        }

        /// <summary>
        /// Creates a thread-safe complex pool
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new thread-safe ComplexObjectPool</returns>
        public ComplexObjectPool<T> CreateThreadSafeComplexPool<T>(
            Func<T> factory,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            Action<T> resetAction = null,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"ThreadSafeComplexPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating thread-safe ComplexObjectPool<{typeof(T).Name}>: {name}");
            
            // Create thread-safe config
            var config = new AdvancedPoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                Prewarm = prewarm,
                EnableThreadSafety = true
            };
            
            return CreateComplexPool(
                factory,
                config,
                resetAction,
                null,
                null,
                null,
                null,
                name);
        }

        /// <summary>
        /// Creates a complex pool with automatic resizing
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="resizeThreshold">Threshold at which to resize the pool (0.0-1.0)</param>
        /// <param name="resizeMultiplier">Multiplier for pool expansion</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new auto-scaling ComplexObjectPool</returns>
        public ComplexObjectPool<T> CreateAutoScalingComplexPool<T>(
            Func<T> factory,
            int initialCapacity = 10,
            float resizeThreshold = 0.8f,
            float resizeMultiplier = 2.0f,
            int maxSize = 0,
            Action<T> resetAction = null,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (resizeThreshold <= 0 || resizeThreshold >= 1)
                throw new ArgumentOutOfRangeException(nameof(resizeThreshold), "Resize threshold must be between 0 and 1");

            if (resizeMultiplier <= 1)
                throw new ArgumentOutOfRangeException(nameof(resizeMultiplier), "Resize multiplier must be greater than 1");

            string name = poolName ?? $"AutoScalingComplexPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating auto-scaling ComplexObjectPool<{typeof(T).Name}>: {name}");
            
            // Create auto-scaling config
            var config = new AdvancedPoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                Prewarm = true,
                AllowResize = true,
                ResizeThreshold = resizeThreshold,
                ResizeMultiplier = resizeMultiplier
            };
            
            return CreateComplexPool(
                factory,
                config,
                resetAction,
                null,
                null,
                null,
                null,
                name);
        }

        /// <summary>
        /// Creates a complex pool with monitoring and diagnostics
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="monitoringInterval">Interval for health monitoring in seconds</param>
        /// <param name="maxItemLifetime">Maximum lifetime for an item in seconds (0 for unlimited)</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new monitored ComplexObjectPool</returns>
        public ComplexObjectPool<T> CreateMonitoredComplexPool<T>(
            Func<T> factory,
            int initialCapacity = 10,
            int maxSize = 0,
            Action<T> resetAction = null,
            float monitoringInterval = 30.0f,
            float maxItemLifetime = 0,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"MonitoredComplexPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating monitored ComplexObjectPool<{typeof(T).Name}>: {name}");
            
            // Create monitored config
            var config = new AdvancedPoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                Prewarm = true,
                EnableMonitoring = true,
                MonitoringInterval = monitoringInterval,
                MaxItemLifetime = maxItemLifetime,
                WarnOnLeakedItems = true,
                WarnOnStaleItems = true,
                EnableDiagnostics = true,
                EnableHealthChecks = true
            };
            
            return CreateComplexPool(
                factory,
                config,
                resetAction,
                null,
                null,
                null,
                null,
                name);
        }

        /// <summary>
        /// Creates a complex pool with priority-based acquisition
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="prioritySelector">Function to determine object priority</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new priority-based ComplexObjectPool</returns>
        public ComplexObjectPool<T> CreatePriorityComplexPool<T>(
            Func<T> factory,
            Func<T, int> prioritySelector,
            int initialCapacity = 10,
            int maxSize = 0,
            Action<T> resetAction = null,
            string poolName = null) where T : class
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (prioritySelector == null)
                throw new ArgumentNullException(nameof(prioritySelector));

            string name = poolName ?? $"PriorityComplexPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfo($"Creating priority-based ComplexObjectPool<{typeof(T).Name}>: {name}");
            
            var config = new AdvancedPoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                Prewarm = true
            };
            
            return CreateComplexPool(
                factory,
                config,
                resetAction,
                null,
                null,
                null,
                prioritySelector,
                name);
        }

        /// <summary>
        /// Extension method to set a custom validator for a ComplexObjectPool
        /// </summary>
        /// <typeparam name="T">Type of objects in the pool</typeparam>
        /// <param name="pool">Pool to modify</param>
        /// <param name="validateAction">Validation function</param>
        public void SetValidator<T>(this ComplexObjectPool<T> pool, Func<T, bool> validateAction) where T : class
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            _logger?.LogInfo($"Setting validator on pool: {pool.PoolName}");
            
            // Implementation would depend on the ComplexObjectPool implementation
            // This is a placeholder for the extension method
        }

        /// <summary>
        /// Extension method to set a custom priority selector for a ComplexObjectPool
        /// </summary>
        /// <typeparam name="T">Type of objects in the pool</typeparam>
        /// <param name="pool">Pool to modify</param>
        /// <param name="prioritySelector">Priority selection function</param>
        public void SetPrioritySelector<T>(this ComplexObjectPool<T> pool, Func<T, int> prioritySelector) where T : class
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            _logger?.LogInfo($"Setting priority selector on pool: {pool.PoolName}");
            
            // Implementation would depend on the ComplexObjectPool implementation
            // This is a placeholder for the extension method
        }

        /// <summary>
        /// Extension method to set lifecycle actions for a ComplexObjectPool
        /// </summary>
        /// <typeparam name="T">Type of objects in the pool</typeparam>
        /// <param name="pool">Pool to modify</param>
        /// <param name="resetAction">Reset action</param>
        /// <param name="onAcquireAction">Action to execute on acquisition</param>
        /// <param name="onReleaseAction">Action to execute on release</param>
        public void SetLifecycleActions<T>(
            this ComplexObjectPool<T> pool, 
            Action<T> resetAction = null,
            Action<T> onAcquireAction = null, 
            Action<T> onReleaseAction = null) where T : class
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            _logger?.LogInfo($"Setting lifecycle actions on pool: {pool.PoolName}");
            
            // Implementation would depend on the ComplexObjectPool implementation
            // This is a placeholder for the extension method
        }

        /// <summary>
        /// Extension method to enable or disable monitoring for a ComplexObjectPool
        /// </summary>
        /// <typeparam name="T">Type of objects in the pool</typeparam>
        /// <param name="pool">Pool to modify</param>
        /// <param name="enableMonitoring">Whether to enable monitoring</param>
        /// <param name="monitoringInterval">Monitoring interval in seconds</param>
        public void SetMonitoring<T>(
            this ComplexObjectPool<T> pool, 
            bool enableMonitoring,
            float monitoringInterval = 30.0f) where T : class
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            _logger?.LogInfo($"Setting monitoring={enableMonitoring} on pool: {pool.PoolName}");
            
            // Implementation would depend on the ComplexObjectPool implementation
            // This is a placeholder for the extension method
        }
    }
}