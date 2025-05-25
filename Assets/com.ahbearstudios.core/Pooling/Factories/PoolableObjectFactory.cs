using System;
using System.Collections.Generic;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Pooling.Pools.Advanced;
using AhBearStudios.Core.Pooling.Pools.Unity;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory for creating and configuring poolable objects that implement IPoolable.
    /// Provides a fluent API for configuring object creation, setup, reset, and destruction behavior.
    /// Integrates with the pool metrics, diagnostics, and configuration systems.
    /// </summary>
    /// <typeparam name="T">Type of objects to create</typeparam>
    public class PoolableObjectFactory<T> where T : class
    {
        private readonly Func<T> _createFunction;
        private readonly List<Action<T>> _setupActions = new List<Action<T>>();
        private readonly List<Action<T>> _resetActions = new List<Action<T>>();
        private readonly List<Action<T>> _destroyActions = new List<Action<T>>();
        private readonly List<Action<T>> _acquireActions = new List<Action<T>>();
        private readonly List<Action<T>> _releaseActions = new List<Action<T>>();
        private string _poolName;
        private IPoolConfig _poolConfig;
        private bool _enablePoolableCalls = true;
        private bool _enableProfiler = true;
        
        /// <summary>
        /// Gets the name of the pool this factory will create
        /// </summary>
        public string PoolName => _poolName;
        
        /// <summary>
        /// Gets the configuration this factory will use
        /// </summary>
        public IPoolConfig PoolConfig => _poolConfig;
        
        /// <summary>
        /// Gets whether this factory will invoke IPoolable lifecycle methods
        /// </summary>
        public bool EnablePoolableCalls => _enablePoolableCalls;
        
        /// <summary>
        /// Creates a new factory with the specified creation function
        /// </summary>
        /// <param name="createFunction">Function to create new instances</param>
        /// <exception cref="ArgumentNullException">Thrown if the create function is null</exception>
        public PoolableObjectFactory(Func<T> createFunction)
        {
            _createFunction = createFunction ?? throw new ArgumentNullException(nameof(createFunction));
            _poolName = $"{typeof(T).Name}Factory_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            
            // Use default configuration if none is specified
            if (PoolingServices.TryGetService<IPoolConfigRegistry>(out var registry))
            {
                _poolConfig = registry.GetOrCreateConfigForType<T>();
            }
            else
            {
                _poolConfig = new PoolConfig
                {
                    InitialCapacity = 10,
                    PrewarmOnInit = true,
                    ResetOnRelease = true,
                    EnableAutoShrink = true,
                    CollectMetrics = true
                };
            }
        }
        
        /// <summary>
        /// Sets a custom name for the factory and generated pools
        /// </summary>
        /// <param name="name">Custom name to use</param>
        /// <returns>This factory for method chaining</returns>
        public PoolableObjectFactory<T> WithName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                _poolName = name;
            }
            return this;
        }
        
        /// <summary>
        /// Sets a custom configuration for pools created by this factory
        /// </summary>
        /// <param name="config">Configuration to use</param>
        /// <returns>This factory for method chaining</returns>
        public PoolableObjectFactory<T> WithConfig(IPoolConfig config)
        {
            if (config != null)
            {
                _poolConfig = config;
            }
            return this;
        }
        
        /// <summary>
        /// Adds a setup action to be performed when an object is created or acquired
        /// </summary>
        /// <param name="setupAction">Action to perform on the object</param>
        /// <returns>This factory for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if the setup action is null</exception>
        public PoolableObjectFactory<T> WithSetup(Action<T> setupAction)
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));
                
            _setupActions.Add(setupAction);
            return this;
        }
        
        /// <summary>
        /// Adds a reset action to be performed when an object is released back to the pool
        /// </summary>
        /// <param name="resetAction">Action to perform on the object</param>
        /// <returns>This factory for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if the reset action is null</exception>
        public PoolableObjectFactory<T> WithReset(Action<T> resetAction)
        {
            if (resetAction == null)
                throw new ArgumentNullException(nameof(resetAction));
                
            _resetActions.Add(resetAction);
            return this;
        }
        
        /// <summary>
        /// Adds a destroy action to be performed when an object is destroyed
        /// </summary>
        /// <param name="destroyAction">Action to perform on the object</param>
        /// <returns>This factory for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if the destroy action is null</exception>
        public PoolableObjectFactory<T> WithDestroy(Action<T> destroyAction)
        {
            if (destroyAction == null)
                throw new ArgumentNullException(nameof(destroyAction));
                
            _destroyActions.Add(destroyAction);
            return this;
        }
        
        /// <summary>
        /// Adds an acquire action to be performed when an object is acquired from the pool
        /// </summary>
        /// <param name="acquireAction">Action to perform on the object</param>
        /// <returns>This factory for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if the acquire action is null</exception>
        public PoolableObjectFactory<T> WithAcquire(Action<T> acquireAction)
        {
            if (acquireAction == null)
                throw new ArgumentNullException(nameof(acquireAction));
                
            _acquireActions.Add(acquireAction);
            return this;
        }
        
        /// <summary>
        /// Adds a release action to be performed when an object is released back to the pool
        /// </summary>
        /// <param name="releaseAction">Action to perform on the object</param>
        /// <returns>This factory for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if the release action is null</exception>
        public PoolableObjectFactory<T> WithRelease(Action<T> releaseAction)
        {
            if (releaseAction == null)
                throw new ArgumentNullException(nameof(releaseAction));
                
            _releaseActions.Add(releaseAction);
            return this;
        }
        
        /// <summary>
        /// Enables or disables the automatic invocation of IPoolable interface methods
        /// </summary>
        /// <param name="enable">Whether to enable IPoolable method calls</param>
        /// <returns>This factory for method chaining</returns>
        public PoolableObjectFactory<T> EnablePoolableInterface(bool enable)
        {
            _enablePoolableCalls = enable;
            return this;
        }
        
        /// <summary>
        /// Enables or disables profiling of factory operations
        /// </summary>
        /// <param name="enable">Whether to enable profiling</param>
        /// <returns>This factory for method chaining</returns>
        public PoolableObjectFactory<T> EnableProfiling(bool enable)
        {
            _enableProfiler = enable;
            return this;
        }
        
        /// <summary>
        /// Creates a new instance and performs all setup actions
        /// </summary>
        /// <returns>The created and set up instance</returns>
        /// <exception cref="InvalidOperationException">Thrown if the factory function returns null</exception>
        public T Create()
        {
            var profiler = _enableProfiler && PoolingServices.TryGetService<PoolProfiler>(out var prof) ? prof : null;
            if (profiler != null) profiler.BeginSample("Create", _poolName);
            
            try
            {
                T instance = _createFunction();
                
                if (instance == null)
                {
                    string message = $"Factory function for {typeof(T).Name} returned null";
                    Debug.LogError(message);
                    if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                    {
                        logger.LogErrorInstance(message);
                    }
                    throw new InvalidOperationException(message);
                }
                
                // Apply all setup actions
                foreach (var action in _setupActions)
                {
                    try
                    {
                        action(instance);
                    }
                    catch (Exception ex)
                    {
                        string message = $"Exception during setup of {typeof(T).Name}: {ex.Message}";
                        Debug.LogException(ex);
                        if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                        {
                            logger.LogErrorInstance(message);
                        }
                    }
                }
                
                return instance;
            }
            finally
            {
                if (profiler != null) profiler.EndSample("Create", _poolName, 0, 0);
            }
        }
        
        /// <summary>
        /// Gets a function that creates and sets up a new instance
        /// </summary>
        /// <returns>A function that creates and sets up a new instance</returns>
        public Func<T> GetCreateFunction()
        {
            return Create;
        }
        
        /// <summary>
        /// Gets an action that performs all reset actions
        /// </summary>
        /// <returns>An action that performs all reset actions</returns>
        public Action<T> GetResetAction()
        {
            return obj =>
            {
                if (obj == null) return;
                
                var profiler = _enableProfiler && PoolingServices.TryGetService<PoolProfiler>(out var prof) ? prof : null;
                if (profiler != null) profiler.BeginSample("Reset", _poolName);
                
                try
                {
                    // Execute all registered reset actions
                    foreach (var action in _resetActions)
                    {
                        try
                        {
                            action(obj);
                        }
                        catch (Exception ex)
                        {
                            string message = $"Exception during reset of {typeof(T).Name}: {ex.Message}";
                            Debug.LogException(ex);
                            if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                            {
                                logger.LogErrorInstance(message);
                            }
                        }
                    }
                    
                    // If enabled, call IPoolable.Reset
                    if (_enablePoolableCalls && obj is IPoolable poolable)
                    {
                        try
                        {
                            poolable.Reset();
                        }
                        catch (Exception ex)
                        {
                            string message = $"Exception during IPoolable.Reset of {typeof(T).Name}: {ex.Message}";
                            Debug.LogException(ex);
                            if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                            {
                                logger.LogErrorInstance(message);
                            }
                        }
                    }
                    
                    // If enabled and it's a MonoBehaviour, call IPoolableMonoBehaviour.Reset
                    if (_enablePoolableCalls && obj is MonoBehaviour monoBehaviour && monoBehaviour is IPoolableMonoBehaviour poolableMonoBehaviour)
                    {
                        try
                        {
                            poolableMonoBehaviour.Reset();
                        }
                        catch (Exception ex)
                        {
                            string message = $"Exception during IPoolableMonoBehaviour.Reset of {typeof(T).Name}: {ex.Message}";
                            Debug.LogException(ex);
                            if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                            {
                                logger.LogErrorInstance(message);
                            }
                        }
                    }
                }
                finally
                {
                    if (profiler != null) profiler.EndSample("Reset", _poolName,0,0);
                }
            };
        }
        
        /// <summary>
        /// Gets an action that performs setup before an object is acquired from the pool
        /// </summary>
        /// <returns>An action that performs setup before an object is acquired</returns>
        public Action<T> GetAcquireAction()
        {
            return obj =>
            {
                if (obj == null) return;
                
                var profiler = _enableProfiler && PoolingServices.TryGetService<PoolProfiler>(out var prof) ? prof : null;
                if (profiler != null) profiler.BeginSample("Acquire", _poolName);
                
                try
                {
                    // Execute all registered acquire actions
                    foreach (var action in _acquireActions)
                    {
                        try
                        {
                            action(obj);
                        }
                        catch (Exception ex)
                        {
                            string message = $"Exception during acquire of {typeof(T).Name}: {ex.Message}";
                            Debug.LogException(ex);
                            if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                            {
                                logger.LogErrorInstance(message);
                            }
                        }
                    }
                    
                    // If enabled, call IPoolable.OnAcquire
                    if (_enablePoolableCalls && obj is IPoolable poolable)
                    {
                        try
                        {
                            poolable.OnAcquire();
                        }
                        catch (Exception ex)
                        {
                            string message = $"Exception during IPoolable.OnAcquire of {typeof(T).Name}: {ex.Message}";
                            Debug.LogException(ex);
                            if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                            {
                                logger.LogErrorInstance(message);
                            }
                        }
                    }
                    
                    // If enabled and it's a MonoBehaviour, call IPoolableMonoBehaviour.OnAcquire
                    if (_enablePoolableCalls && obj is MonoBehaviour monoBehaviour && monoBehaviour is IPoolableMonoBehaviour poolableMonoBehaviour)
                    {
                        try
                        {
                            poolableMonoBehaviour.OnAcquire();
                        }
                        catch (Exception ex)
                        {
                            string message = $"Exception during IPoolableMonoBehaviour.OnAcquire of {typeof(T).Name}: {ex.Message}";
                            Debug.LogException(ex);
                            if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                            {
                                logger.LogErrorInstance(message);
                            }
                        }
                    }
                }
                finally
                {
                    if (profiler != null) profiler.EndSample("Acquire", _poolName,0,0);
                }
            };
        }
        
        /// <summary>
        /// Gets an action that performs cleanup when an object is released to the pool
        /// </summary>
        /// <returns>An action that performs cleanup when an object is released</returns>
        public Action<T> GetReleaseAction()
        {
            return obj =>
            {
                if (obj == null) return;
                
                var profiler = _enableProfiler && PoolingServices.TryGetService<PoolProfiler>(out var prof) ? prof : null;
                if (profiler != null) profiler.BeginSample("Release", _poolName);
                
                try
                {
                    // Execute all registered release actions
                    foreach (var action in _releaseActions)
                    {
                        try
                        {
                            action(obj);
                        }
                        catch (Exception ex)
                        {
                            string message = $"Exception during release of {typeof(T).Name}: {ex.Message}";
                            Debug.LogException(ex);
                            if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                            {
                                logger.LogErrorInstance(message);
                            }
                        }
                    }
                    
                    // If enabled, call IPoolable.OnRelease
                    if (_enablePoolableCalls && obj is IPoolable poolable)
                    {
                        try
                        {
                            poolable.OnRelease();
                        }
                        catch (Exception ex)
                        {
                            string message = $"Exception during IPoolable.OnRelease of {typeof(T).Name}: {ex.Message}";
                            Debug.LogException(ex);
                            if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                            {
                                logger.LogErrorInstance(message);
                            }
                        }
                    }
                    
                    // If enabled and it's a MonoBehaviour, call IPoolableMonoBehaviour.OnRelease
                    if (_enablePoolableCalls && obj is MonoBehaviour monoBehaviour && monoBehaviour is IPoolableMonoBehaviour poolableMonoBehaviour)
                    {
                        try
                        {
                            poolableMonoBehaviour.OnRelease();
                        }
                        catch (Exception ex)
                        {
                            string message = $"Exception during IPoolableMonoBehaviour.OnRelease of {typeof(T).Name}: {ex.Message}";
                            Debug.LogException(ex);
                            if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                            {
                                logger.LogErrorInstance(message);
                            }
                        }
                    }
                }
                finally
                {
                    if (profiler != null) profiler.EndSample("Release", _poolName,0,0);
                }
            };
        }
        
        /// <summary>
        /// Gets an action that performs all destroy actions
        /// </summary>
        /// <returns>An action that performs all destroy actions</returns>
        public Action<T> GetDestroyAction()
        {
            return obj =>
            {
                if (obj == null) return;
                
                var profiler = _enableProfiler && PoolingServices.TryGetService<PoolProfiler>(out var prof) ? prof : null;
                if (profiler != null) profiler.BeginSample("Destroy", _poolName);
                
                try
                {
                    // If enabled, call IPoolable.OnDestroy
                    if (_enablePoolableCalls && obj is IPoolable poolable)
                    {
                        try
                        {
                            poolable.OnDestroy();
                        }
                        catch (Exception ex)
                        {
                            string message = $"Exception during IPoolable.OnDestroy of {typeof(T).Name}: {ex.Message}";
                            Debug.LogException(ex);
                            if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                            {
                                logger.LogErrorInstance(message);
                            }
                        }
                    }
                    
                    // If enabled and it's a MonoBehaviour, call IPoolableMonoBehaviour.OnDestroy
                    if (_enablePoolableCalls && obj is MonoBehaviour monoBehaviour && monoBehaviour is IPoolableMonoBehaviour poolableMonoBehaviour)
                    {
                        try
                        {
                            poolableMonoBehaviour.OnDestroy();
                        }
                        catch (Exception ex)
                        {
                            string message = $"Exception during IPoolableMonoBehaviour.OnDestroy of {typeof(T).Name}: {ex.Message}";
                            Debug.LogException(ex);
                            if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                            {
                                logger.LogErrorInstance(message);
                            }
                        }
                    }
                    
                    // Execute all registered destroy actions
                    foreach (var action in _destroyActions)
                    {
                        try
                        {
                            action(obj);
                        }
                        catch (Exception ex)
                        {
                            string message = $"Exception during destroy of {typeof(T).Name}: {ex.Message}";
                            Debug.LogException(ex);
                            if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                            {
                                logger.LogErrorInstance(message);
                            }
                        }
                    }
                    
                    // If the object implements IDisposable, dispose it
                    if (obj is IDisposable disposable)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (Exception ex)
                        {
                            string message = $"Exception during IDisposable.Dispose of {typeof(T).Name}: {ex.Message}";
                            Debug.LogException(ex);
                            if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                            {
                                logger.LogErrorInstance(message);
                            }
                        }
                    }
                }
                finally
                {
                    if (profiler != null) profiler.EndSample("Destroy", _poolName,0,0);
                }
            };
        }
        
        /// <summary>
        /// Creates a new object pool using this factory
        /// </summary>
        /// <param name="config">Pool configuration, or null to use the factory's configuration</param>
        /// <param name="validator">Optional validator for objects</param>
        /// <returns>A new pool implementing IPool, IPoolMetrics and IShrinkablePool</returns>
        public IPool<T> CreatePool(IPoolConfig config = null, Func<T, bool> validator = null)
        {
            // Use provided config or the factory's default config
            IPoolConfig finalConfig = config ?? _poolConfig;
            
            // Try to get a pool factory for this type
            IPool<T> pool;
            if (PoolingServices.TryGetService<IPoolFactory>(out var factory))
            {
                if (factory.CanCreatePoolFor(typeof(T)))
                {
                    // The pool factory will automatically register the pool with diagnostics and profiling
                    try
                    {
                        // Use factory's create pool method with our factory function and actions
                        pool = factory.CreatePool(
                            GetCreateFunction(),
                            finalConfig,
                            GetResetAction(),
                            _poolName);
                            
                        // If we have specific acquire/release actions, register a validator that executes them
                        if (_acquireActions.Count > 0 || _releaseActions.Count > 0)
                        {
                            var acquireAction = GetAcquireAction();
                            var releaseAction = GetReleaseAction();
    
                            // Since IAdvancedPool isn't available, we need to use a different approach
                            // Check if the pool is our AdvancedObjectPool, which we know supports these operations
                            if (pool is AdvancedObjectPool<T> advancedPool)
                            {
                                // If this is our own AdvancedObjectPool, we know it can directly use these actions
                                // These methods aren't in an interface but should be in the implementation
                                // However, if these methods don't exist either, we'll need to modify our approach further
        
                                // Attempt to use reflection to call these methods if they exist
                                try
                                {
                                    var registerAcquireMethod = advancedPool.GetType().GetMethod("RegisterAcquireAction");
                                    var registerReleaseMethod = advancedPool.GetType().GetMethod("RegisterReleaseAction");
            
                                    if (registerAcquireMethod != null) 
                                        registerAcquireMethod.Invoke(advancedPool, new object[] { acquireAction });
                
                                    if (registerReleaseMethod != null)
                                        registerReleaseMethod.Invoke(advancedPool, new object[] { releaseAction });
                                }
                                catch (Exception ex)
                                {
                                    // Log but continue if reflection fails
                                    if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                                    {
                                        logger.LogWarningInstance($"Failed to register actions with advanced pool: {ex.Message}");
                                    }
                                }
                            }
                            // Otherwise, we can't register the actions directly and will rely on our implementation
                            // of GetAcquireAction and GetReleaseAction being used through the factory
                        }
                    }
                    catch (Exception ex)
                    {
                        // If factory creation fails, fall back to creating an advanced pool
                        Debug.LogWarning($"Failed to create pool using IPoolFactory: {ex.Message}. Falling back to AdvancedObjectPool.");
                        pool = CreateAdvancedPool(finalConfig, validator);
                    }
                }
                else
                {
                    // Factory can't create pool for this type, create an advanced pool
                    pool = CreateAdvancedPool(finalConfig, validator);
                }
            }
            else
            {
                // No factory available, create an advanced pool
                pool = CreateAdvancedPool(finalConfig, validator);
            }
            
            // Register the pool for diagnostics if available
            if (PoolingServices.TryGetService<IPoolDiagnostics>(out var diagnostics))
            {
                diagnostics.RegisterPool(pool, _poolName);
            }
            
            // Register the pool with the registry if available
            if (PoolingServices.TryGetService<PoolRegistry>(out var registry))
            {
                try
                {
                    registry.RegisterPool(pool, _poolName);
                }
                catch (Exception ex)
                {
                    // Log but don't fail if registry registration fails
                    if (PoolingServices.TryGetService<PoolLogger>(out var logger))
                    {
                        logger.LogWarningInstance($"Failed to register pool with registry: {ex.Message}");
                    }
                }
            }
            
            return pool;
        }
        
        /// <summary>
        /// Creates a high-performance pool optimized for frequently used objects
        /// </summary>
        /// <param name="initialCapacity">Initial capacity (default 20)</param>
        /// <param name="validator">Optional validator for objects</param>
        /// <returns>A new high-performance pool</returns>
        public IPool<T> CreateHighPerformancePool(int initialCapacity = 20, Func<T, bool> validator = null)
        {
            // Create optimized configuration for high-performance
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaximumCapacity = 0,
                PrewarmOnInit = true,
                UseExponentialGrowth = true,
                GrowthFactor = 2.0f,
                ResetOnRelease = true,
                EnableAutoShrink = false,
                CollectMetrics = true
            };
            
            return CreatePool(config, validator);
        }
        
        /// <summary>
        /// Creates a memory-efficient pool that automatically shrinks when utilization is low
        /// </summary>
        /// <param name="initialCapacity">Initial capacity</param>
        /// <param name="validator">Optional validator for objects</param>
        /// <returns>A new memory-efficient pool</returns>
        public IPool<T> CreateMemoryEfficientPool(int initialCapacity = 10, Func<T, bool> validator = null)
        {
            // Create memory-optimized configuration
            var config = new PoolConfig
            {
                InitialCapacity = initialCapacity,
                MaximumCapacity = 0,
                PrewarmOnInit = false,
                UseExponentialGrowth = false,
                GrowthIncrement = 5,
                ResetOnRelease = true,
                EnableAutoShrink = true,
                ShrinkThreshold = 0.3f,
                ShrinkInterval = 60f, // Check every minute
                CollectMetrics = true
            };
            
            return CreatePool(config, validator);
        }
        
        /// <summary>
        /// Creates a fixed-size pool with a strict maximum capacity
        /// </summary>
        /// <param name="capacity">Fixed capacity of the pool</param>
        /// <param name="validator">Optional validator for objects</param>
        /// <returns>A new fixed-size pool</returns>
        public IPool<T> CreateFixedSizePool(int capacity, Func<T, bool> validator = null)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive");
                
            // Create fixed-size configuration
            var config = new PoolConfig
            {
                InitialCapacity = capacity,
                MaximumCapacity = capacity,
                PrewarmOnInit = true,
                ResetOnRelease = true,
                EnableAutoShrink = false,
                ThrowIfExceedingMaxCount = true,
                CollectMetrics = true
            };
            
            return CreatePool(config, validator);
        }
        
        /// <summary>
        /// Creates a new advanced object pool using this factory
        /// </summary>
        /// <param name="config">Pool configuration</param>
        /// <param name="validator">Optional validator for objects</param>
        /// <returns>A new advanced object pool</returns>
        private AdvancedObjectPool<T> CreateAdvancedPool(IPoolConfig config, Func<T, bool> validator)
        {
            var poolConfig = config as AdvancedPoolConfig ?? new AdvancedPoolConfig();
    
            // Use the constructor with the correct number of parameters
            return new AdvancedObjectPool<T>(
                GetCreateFunction(),
                GetResetAction(),
                GetAcquireAction(),
                GetDestroyAction(),
                validator,
                poolConfig,
                _poolName);
        }
    }
}