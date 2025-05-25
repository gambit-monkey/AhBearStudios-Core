using System;
using AhBearStudios.Pooling.Core.Pooling.Core;
using AhBearStudios.Pooling.Core.Pooling.Advanced;

namespace AhBearStudios.Pooling.Core.Pooling
{
    /// <summary>
    /// Partial class for PoolManager that handles advanced object pool creation and management
    /// </summary>
    public partial class PoolManager
    {
        /// <summary>
        /// Creates an advanced object pool with specified parameters
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="validateAction">Optional function to validate objects before acquisition</param>
        /// <param name="disposeAction">Optional action to dispose objects when pool is destroyed</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new AdvancedObjectPool</returns>
        public AdvancedObjectPool<T> CreateAdvancedPool<T>(
            Func<T> factory,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            Action<T> resetAction = null,
            Func<T, bool> validateAction = null,
            Action<T> disposeAction = null,
            string poolName = null)
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
                PrewarmOnInit = prewarm,
                ValidateOnRelease = validateAction != null
            };

            return CreateAdvancedPool(factory, config, resetAction, validateAction, disposeAction, poolName);
        }

        /// <summary>
        /// Creates an advanced object pool with custom configuration
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="validateAction">Optional function to validate objects before acquisition</param>
        /// <param name="disposeAction">Optional action to dispose objects when pool is destroyed</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new AdvancedObjectPool</returns>
        public AdvancedObjectPool<T> CreateAdvancedPool<T>(
            Func<T> factory,
            AdvancedPoolConfig config,
            Action<T> resetAction = null,
            Func<T, bool> validateAction = null,
            Action<T> disposeAction = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            string name = poolName ?? $"AdvancedPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            // Check if pool already exists
            if (Registry.HasPool(name))
            {
                var existingPool = Registry.GetPool(name);
                if (existingPool is AdvancedObjectPool<T> advancedPool)
                {
                    _logger?.LogInfoInstance($"Returning existing pool: {name}");
                    return advancedPool;
                }
                
                throw new InvalidOperationException($"Pool with name {name} already exists but is not an AdvancedObjectPool<{typeof(T).Name}>");
            }
            
            _logger?.LogInfoInstance($"Creating new AdvancedObjectPool<{typeof(T).Name}>: {name}");
            
            // Create new pool with full configuration
            var pool = new AdvancedObjectPool<T>(
                factory,
                resetAction,
                null, // onAcquireAction is not exposed in this method
                disposeAction,
                validateAction,
                config,
                name);
            
            // Register the pool
            Registry.RegisterPool(pool, name);
            
            return pool;
        }

        /// <summary>
        /// Creates a high-performance advanced object pool optimized for frequent use
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new high-performance AdvancedObjectPool</returns>
        public AdvancedObjectPool<T> CreateHighPerformanceAdvancedPool<T>(
            Func<T> factory,
            int initialCapacity = 20,
            int maxSize = 100,
            bool prewarm = true,
            Action<T> resetAction = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"HighPerfAdvancedPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfoInstance($"Creating high-performance AdvancedObjectPool<{typeof(T).Name}>: {name}");
            
            // Create optimized config
            var config = new AdvancedPoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = prewarm,
                EnableMonitoring = false,
                EnableProfiling = false,
                TrackAcquireStackTraces = false,
                ValidateOnRelease = false,
                EnableThreadSafety = false
            };
            
            return CreateAdvancedPool(
                factory,
                config,
                resetAction,
                null,
                null,
                name);
        }

        /// <summary>
        /// Creates a thread-safe advanced object pool
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="prewarm">Whether to prewarm the pool</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="validateAction">Optional function to validate objects before acquisition</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new thread-safe AdvancedObjectPool</returns>
        public AdvancedObjectPool<T> CreateThreadSafeAdvancedPool<T>(
            Func<T> factory,
            int initialCapacity = 10,
            int maxSize = 0,
            bool prewarm = true,
            Action<T> resetAction = null,
            Func<T, bool> validateAction = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"ThreadSafeAdvancedPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfoInstance($"Creating thread-safe AdvancedObjectPool<{typeof(T).Name}>: {name}");
            
            // Create thread-safe config
            var config = new AdvancedPoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = prewarm,
                EnableThreadSafety = true
            };
            
            return CreateAdvancedPool(
                factory,
                config,
                resetAction,
                validateAction,
                null,
                name);
        }

        /// <summary>
        /// Creates an expandable object pool that automatically resizes when needed
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="expansionFactor">Factor by which to expand the pool when it's full</param>
        /// <param name="maxExpansions">Maximum number of expansions (0 for unlimited)</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new expandable AdvancedObjectPool</returns>
        public AdvancedObjectPool<T> CreateExpandablePool<T>(
            Func<T> factory,
            int initialCapacity = 10,
            float expansionFactor = 2.0f,
            int maxExpansions = 0,
            Action<T> resetAction = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (expansionFactor <= 1.0f)
                throw new ArgumentOutOfRangeException(nameof(expansionFactor), "Expansion factor must be greater than 1.0");

            string name = poolName ?? $"ExpandablePool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfoInstance($"Creating expandable pool: {name}");
            
            int maxSize = 0;
            if (maxExpansions > 0)
            {
                maxSize = (int)(initialCapacity * Math.Pow(expansionFactor, maxExpansions));
            }
            
            // Create expandable config
            var config = new AdvancedPoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = true,
                AllowResize = true,
                ResizeThreshold = 0.9f,
                ResizeMultiplier = expansionFactor,
                EnableMonitoring = true
            };
            
            return CreateAdvancedPool(
                factory,
                config,
                resetAction,
                null,
                null,
                name);
        }
        
        /// <summary>
        /// Creates an advanced monitored pool with health checks and diagnostics
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="validateAction">Optional function to validate objects before acquisition</param>
        /// <param name="monitoringInterval">Interval for monitoring checks in seconds</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new monitored AdvancedObjectPool</returns>
        public AdvancedObjectPool<T> CreateMonitoredAdvancedPool<T>(
            Func<T> factory,
            int initialCapacity = 10,
            int maxSize = 0,
            Action<T> resetAction = null,
            Func<T, bool> validateAction = null,
            float monitoringInterval = 30.0f,
            string poolName = null)
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"MonitoredAdvancedPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfoInstance($"Creating monitored AdvancedObjectPool<{typeof(T).Name}>: {name}");
            
            // Create monitored config
            var config = new AdvancedPoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = true,
                EnableMonitoring = true,
                MonitoringInterval = monitoringInterval,
                WarnOnLeakedItems = true,
                WarnOnStaleItems = true,
                EnableDiagnostics = true,
                EnableHealthChecks = true,
                ValidateOnRelease = validateAction != null
            };
            
            return CreateAdvancedPool(
                factory,
                config,
                resetAction,
                validateAction,
                null,
                name);
        }

        /// <summary>
        /// Creates an auto-cleaning advanced pool that periodically removes unused objects
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="shrinkInterval">Interval in seconds between shrink operations</param>
        /// <param name="maxInactiveCount">Maximum number of inactive objects to keep</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="disposeAction">Optional action to dispose objects when removed from pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new auto-cleaning AdvancedObjectPool</returns>
        public AdvancedObjectPool<T> CreateAutoCleaningAdvancedPool<T>(
            Func<T> factory,
            int initialCapacity = 10,
            int maxSize = 0,
            float shrinkInterval = 300.0f,
            int maxInactiveCount = 32,
            Action<T> resetAction = null,
            Action<T> disposeAction = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            string name = poolName ?? $"AutoCleaningAdvancedPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfoInstance($"Creating auto-cleaning AdvancedObjectPool<{typeof(T).Name}>: {name}");
            
            // Create auto-cleaning config
            var config = new AdvancedPoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = true,
                AutoShrink = true,
                AutoShrinkInterval = shrinkInterval,
                MaxInactiveOnShrink = maxInactiveCount
            };
            
            return CreateAdvancedPool(
                factory,
                config,
                resetAction,
                null,
                disposeAction,
                name);
        }

        /// <summary>
        /// Creates an advanced validation pool that ensures objects meet certain criteria
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="validateAction">Function to validate objects before acquisition</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="resetAction">Optional action to reset objects when returned to pool</param>
        /// <param name="disposeAction">Optional action to dispose objects when removed from pool</param>
        /// <param name="poolName">Optional name for the pool</param>
        /// <returns>A new validation AdvancedObjectPool</returns>
        public AdvancedObjectPool<T> CreateValidationAdvancedPool<T>(
            Func<T> factory,
            Func<T, bool> validateAction,
            int initialCapacity = 10,
            int maxSize = 0,
            Action<T> resetAction = null,
            Action<T> disposeAction = null,
            string poolName = null)
        {
            EnsureInitialized();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (validateAction == null)
                throw new ArgumentNullException(nameof(validateAction));

            string name = poolName ?? $"ValidationAdvancedPool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            
            _logger?.LogInfoInstance($"Creating validation AdvancedObjectPool<{typeof(T).Name}>: {name}");
            
            // Create validation config
            var config = new AdvancedPoolConfig
            {
                InitialCapacity = initialCapacity,
                MaxSize = maxSize,
                PrewarmOnInit = true,
                ValidateOnRelease = true
            };
            
            return CreateAdvancedPool(
                factory,
                config,
                resetAction,
                validateAction,
                disposeAction,
                name);
        }
    }
}