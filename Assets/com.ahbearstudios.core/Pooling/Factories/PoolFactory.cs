using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Pooling.Pools.Async;
using AhBearStudios.Core.Pooling.Pools.Native;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory for creating and managing various types of object pools.
    /// Supports managed objects, native unmanaged types, and Unity-specific objects.
    /// Implements the IPoolFactory interface to provide a standardized API for pool creation.
    /// </summary>
    public class PoolFactory : IPoolFactory
    {
        private readonly Dictionary<Type, IPoolFactory> _specificFactories = new Dictionary<Type, IPoolFactory>();
        private readonly Dictionary<Type, Dictionary<Type, Func<object[], object>>> _poolCreationStrategies = 
            new Dictionary<Type, Dictionary<Type, Func<object[], object>>>();
        private readonly IPoolConfigRegistry _configRegistry;
        private readonly IPoolDiagnostics _diagnostics;
        
        /// <summary>
        /// Gets the unique identifier for this factory
        /// </summary>
        public string FactoryId { get; } = "DefaultPoolFactory";
        
        /// <summary>
        /// Initializes a new instance of the PoolFactory class with default configuration
        /// </summary>
        public PoolFactory() : this(null, null)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the PoolFactory class with specified configuration registry and diagnostics
        /// </summary>
        /// <param name="configRegistry">Registry for pool configurations</param>
        /// <param name="diagnostics">Diagnostics for tracking pool performance</param>
        public PoolFactory(IPoolConfigRegistry configRegistry, IPoolDiagnostics diagnostics)
        {
            _configRegistry = configRegistry;
            _diagnostics = diagnostics;
            RegisterDefaultFactories();
        }
        
        /// <summary>
        /// Registers the default factories for common types
        /// </summary>
        public void RegisterDefaultFactories()
        {
            // Register standard creation strategies
            // This will be expanded with actual implementations
        }
        
        /// <summary>
        /// Creates a generic pool for the specified item type
        /// </summary>
        /// <typeparam name="T">Type of items the pool will manage</typeparam>
        /// <param name="factory">Function that creates new pool items</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="resetAction">Optional action to reset items when released</param>
        /// <param name="poolName">Name for the pool</param>
        /// <returns>A new pool instance</returns>
        public IPool<T> CreatePool<T>(Func<T> factory, IPoolConfig config = null, Action<T> resetAction = null, string poolName = null)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory), "Factory function cannot be null");
            
            // Get or create appropriate config
            config = GetOrCreateConfig<T>(config);
            
            // Generate unique pool name if not provided
            poolName = GetUniquePoolName<T>(poolName);
            
            // Create pool based on the poolable nature of T
            if (typeof(IPoolable).IsAssignableFrom(typeof(T)))
            {
                // Create a pool for IPoolable types with special handling
                return CreatePoolWithConfig(typeof(T), config, poolName, factory, resetAction) as IPool<T>;
            }
            
            // Find a specific factory for this type
            var specificFactory = GetFactoryFor<T>();
            if (specificFactory != null && specificFactory != this)
            {
                return specificFactory.CreatePool(factory, config, resetAction, poolName);
            }
            
            // Otherwise, create a standard managed pool (implementation would be ManagedPool<T>)
            // Actual implementation would go here
            // For now, using a placeholder that would be replaced with actual implementation
            throw new NotImplementedException($"Pool implementation for type {typeof(T).Name} is not yet available");
        }
        
        /// <summary>
        /// Creates a native pool for unmanaged types
        /// </summary>
        /// <typeparam name="T">Unmanaged type of items in the pool</typeparam>
        /// <param name="config">Pool configuration</param>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="poolName">Name for the pool</param>
        /// <returns>A new native pool instance</returns>
        public INativePool<T> CreateNativePool<T>(IPoolConfig config = null, int initialCapacity = 10, string poolName = null) where T : unmanaged
        {
            // Get or create appropriate config
            config = GetOrCreateConfig<T>(config);
            
            // Override initial capacity if specified
            if (initialCapacity > 0)
            {
                config.InitialCapacity = initialCapacity;
            }
            
            // Generate unique pool name if not provided
            poolName = GetUniquePoolName<T>(poolName);
            
            // Find a specific factory for this type
            var specificFactory = GetFactoryFor<T>();
            if (specificFactory != null && specificFactory != this)
            {
                return specificFactory.CreateNativePool<T>(config, initialCapacity, poolName);
            }
            
            // Otherwise, create a standard native pool (implementation would be NativePool<T>)
            // Actual implementation would go here
            // For now, using a placeholder that would be replaced with actual implementation
            throw new NotImplementedException($"Native pool implementation for type {typeof(T).Name} is not yet available");
        }
        
        /// <summary>
        /// Creates a Unity-specific pool for GameObject or Component types
        /// </summary>
        /// <typeparam name="T">Type of UnityEngine.Object in the pool</typeparam>
        /// <param name="prefab">Prefab to instantiate for new pool items</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="parent">Optional parent transform for pooled objects</param>
        /// <param name="resetAction">Optional action to reset items when released</param>
        /// <param name="poolName">Name for the pool</param>
        /// <returns>A new Unity-specific pool instance</returns>
        public IUnityPool<T> CreateUnityPool<T>(T prefab, IPoolConfig config = null, Transform parent = null, Action<T> resetAction = null, string poolName = null) where T : UnityEngine.Object
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab), "Prefab cannot be null");
            
            // Get or create appropriate config
            config = GetOrCreateConfig<T>(config);
            
            // Generate unique pool name if not provided
            poolName = GetUniquePoolName<T>(poolName);
            
            // Find a specific factory for this type
            var specificFactory = GetFactoryFor<T>();
            if (specificFactory != null && specificFactory != this)
            {
                return specificFactory.CreateUnityPool(prefab, config, parent, resetAction, poolName);
            }
            
            // Otherwise, create a standard Unity pool (implementation would be UnityPool<T>)
            // Actual implementation would go here
            // For now, using a placeholder that would be replaced with actual implementation
            throw new NotImplementedException($"Unity pool implementation for type {typeof(T).Name} is not yet available");
        }
        
        /// <summary>
        /// Creates an asynchronous pool for the specified item type
        /// </summary>
        /// <typeparam name="T">Type of items the pool will manage</typeparam>
        /// <param name="factory">Function that creates new pool items</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="resetAction">Optional action to reset items when released</param>
        /// <param name="poolName">Name for the pool</param>
        /// <returns>A new asynchronous pool instance</returns>
        public IAsyncPool<T> CreateAsyncPool<T>(Func<T> factory, IPoolConfig config = null, Action<T> resetAction = null, string poolName = null)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory), "Factory function cannot be null");
            
            // Get or create appropriate config
            config = GetOrCreateConfig<T>(config);
            
            // Generate unique pool name if not provided
            poolName = GetUniquePoolName<T>(poolName);
            
            // First create a standard pool, then wrap it in an async adapter
            var basePool = CreatePool(factory, config, resetAction, poolName);
            
            // Create an async adapter for the pool
            // Actual implementation would go here
            // For now, using a placeholder that would be replaced with actual implementation
            throw new NotImplementedException($"Async pool implementation for type {typeof(T).Name} is not yet available");
        }
        
        /// <summary>
        /// Creates an async adapter for an existing pool
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="basePool">Base pool to adapt</param>
        /// <param name="poolName">Optional pool name</param>
        /// <param name="asyncFactory">Optional async factory function</param>
        /// <param name="ownsPool">Whether the adapter should dispose the base pool</param>
        /// <returns>An async pool adapter</returns>
        public IAsyncPool<T> CreateAsyncAdapter<T>(IPool<T> basePool, string poolName = null, Func<CancellationToken, Task<T>> asyncFactory = null, bool ownsPool = false) where T : class
        {
            if (basePool == null)
                throw new ArgumentNullException(nameof(basePool), "Base pool cannot be null");
    
            // Generate unique pool name if not provided
            poolName = string.IsNullOrEmpty(poolName) ? $"AsyncAdapter_{basePool.PoolName}" : poolName;
    
            // Create and return the async adapter
            var adapter = new AsyncPoolAdapter<T>(basePool, asyncFactory, ownsPool);
    
            // Log creation
            LogInfo($"Created async adapter for pool '{basePool.PoolName}', adapter name: '{poolName}'");
    
            // Register with diagnostics if available
            if (_diagnostics != null)
            {
                _diagnostics.RegisterPool(adapter, poolName);
            }
    
            return adapter;
        }
        
        /// <summary>
        /// Checks if this factory can create a pool for the specified item type
        /// </summary>
        /// <param name="itemType">Type of items to pool</param>
        /// <param name="poolInterfaceType">Specific pool interface type to create (IPool, INativePool, IUnityPool)</param>
        /// <returns>True if this factory can create the requested pool type</returns>
        public bool CanCreatePoolFor(Type itemType, Type poolInterfaceType = null)
        {
            if (itemType == null)
                return false;
            
            // Check if there's a specific factory registered for this type
            if (_specificFactories.ContainsKey(itemType))
                return true;
            
            // Check if there's a creation strategy registered for this type
            if (poolInterfaceType != null && _poolCreationStrategies.TryGetValue(poolInterfaceType, out var strategies))
            {
                if (strategies.ContainsKey(itemType))
                    return true;
            }
            
            // For native pools, check if the type is unmanaged
            if (poolInterfaceType != null && poolInterfaceType == typeof(INativePool<>) && itemType.IsValueType && !itemType.IsPrimitive)
            {
                return true;
            }
            
            // For Unity pools, check if the type is derived from UnityEngine.Object
            if (poolInterfaceType != null && poolInterfaceType == typeof(IUnityPool<>) && typeof(UnityEngine.Object).IsAssignableFrom(itemType))
            {
                return true;
            }
            
            // Default to true for IPool<> on reference types
            return poolInterfaceType == null || poolInterfaceType == typeof(IPool<>);
        }
        
        /// <summary>
        /// Gets a pool configuration, either from the registry or by creating a new default one
        /// </summary>
        /// <typeparam name="T">Type of items the configuration is for</typeparam>
        /// <param name="config">Optional provided configuration</param>
        /// <returns>The configuration to use</returns>
        public IPoolConfig GetOrCreateConfig<T>(IPoolConfig config = null)
        {
            // If config is provided, use it
            if (config != null)
                return config;
    
            // If registry is available, get or create config
            if (_configRegistry != null)
            {
                // For reference types, use the registry method
                if (typeof(T).IsClass)
                {
                    // Use reflection to call the generic method with the correct constraint
                    var methodInfo = typeof(IPoolConfigRegistry).GetMethod(nameof(IPoolConfigRegistry.GetOrCreateConfigForType));
                    var genericMethod = methodInfo?.MakeGenericMethod(typeof(T));
                    if (genericMethod != null)
                    {
                        return (IPoolConfig)genericMethod.Invoke(_configRegistry, null);
                    }
                }
        
                // For value types, get a non-type-specific config or create a default one
                return _configRegistry.GetOrCreateConfig(typeof(T).Name);
            }
    
            // Otherwise, create a default config
            return CreateDefaultConfig();
        }
        
        /// <summary>
        /// Creates a default pool configuration
        /// </summary>
        /// <returns>A new default configuration</returns>
        public IPoolConfig CreateDefaultConfig()
        {
            // Actual implementation would create a specific configuration class
            // For now, using a placeholder that would be replaced with actual implementation
            throw new NotImplementedException("Default configuration implementation is not yet available");
        }
        
        /// <summary>
        /// Registers a specific factory for a type
        /// </summary>
        /// <param name="itemType">Type of items to pool</param>
        /// <param name="factory">Factory to register</param>
        public void RegisterFactory(Type itemType, IPoolFactory factory)
        {
            if (itemType == null)
                throw new ArgumentNullException(nameof(itemType));
            
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            
            _specificFactories[itemType] = factory;
        }
        
        /// <summary>
        /// Gets a factory for a specific type
        /// </summary>
        /// <typeparam name="T">Type of items to pool</typeparam>
        /// <returns>A factory that can create pools for the specified type</returns>
        public IPoolFactory GetFactoryFor<T>()
        {
            return GetFactoryFor(typeof(T));
        }
        
        /// <summary>
        /// Gets a factory for a specific type
        /// </summary>
        /// <param name="itemType">Type of items to pool</param>
        /// <returns>A factory that can create pools for the specified type</returns>
        public IPoolFactory GetFactoryFor(Type itemType)
        {
            if (itemType == null)
                throw new ArgumentNullException(nameof(itemType));
            
            if (_specificFactories.TryGetValue(itemType, out var factory))
                return factory;
            
            // If no specific factory is found, return this factory as the default
            return this;
        }
        
        /// <summary>
        /// Registers a custom pool creation strategy
        /// </summary>
        /// <param name="poolInterfaceType">Pool interface type (e.g., typeof(IPool&lt;&gt;))</param>
        /// <param name="itemType">Type of items to pool</param>
        /// <param name="creationStrategy">Function that creates the pool</param>
        public void RegisterPoolCreationStrategy(Type poolInterfaceType, Type itemType, Func<object[], object> creationStrategy)
        {
            if (poolInterfaceType == null)
                throw new ArgumentNullException(nameof(poolInterfaceType));
            
            if (itemType == null)
                throw new ArgumentNullException(nameof(itemType));
            
            if (creationStrategy == null)
                throw new ArgumentNullException(nameof(creationStrategy));
            
            // Ensure the dictionary exists
            if (!_poolCreationStrategies.TryGetValue(poolInterfaceType, out var strategies))
            {
                strategies = new Dictionary<Type, Func<object[], object>>();
                _poolCreationStrategies[poolInterfaceType] = strategies;
            }
            
            // Register the strategy
            strategies[itemType] = creationStrategy;
        }
        
        /// <summary>
        /// Registers a custom pool creation strategy for a specific generic type
        /// </summary>
        /// <typeparam name="TPool">Pool interface type</typeparam>
        /// <typeparam name="TItem">Type of items to pool</typeparam>
        /// <param name="creationStrategy">Function that creates the pool</param>
        public void RegisterPoolCreationStrategy<TPool, TItem>(Func<object[], TPool> creationStrategy) 
            where TPool : IPool
        {
            if (creationStrategy == null)
                throw new ArgumentNullException(nameof(creationStrategy));
            
            // Convert the generic strategy to a non-generic one
            Func<object[], object> adaptedStrategy = args => creationStrategy(args);
            
            // Register the strategy
            RegisterPoolCreationStrategy(typeof(TPool), typeof(TItem), adaptedStrategy);
        }
        
        /// <summary>
        /// Creates a pool based on the actual runtime type of the provided config
        /// </summary>
        /// <param name="itemType">Type of items to pool</param>
        /// <param name="config">Pool configuration</param>
        /// <param name="poolName">Name for the pool</param>
        /// <param name="factoryArgs">Additional arguments needed for pool creation</param>
        /// <returns>The created pool</returns>
        public IPool CreatePoolWithConfig(Type itemType, IPoolConfig config, string poolName, params object[] factoryArgs)
        {
            if (itemType == null)
                throw new ArgumentNullException(nameof(itemType));
            
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            // Try to find a creation strategy for IPool<>
            Type poolInterfaceType = typeof(IPool<>).MakeGenericType(itemType);
            if (_poolCreationStrategies.TryGetValue(poolInterfaceType, out var strategies) &&
                strategies.TryGetValue(itemType, out var creationStrategy))
            {
                // Combine config, name, and additional args
                object[] args = new object[factoryArgs.Length + 2];
                args[0] = config;
                args[1] = poolName;
                Array.Copy(factoryArgs, 0, args, 2, factoryArgs.Length);
                
                // Create the pool
                return (IPool)creationStrategy(args);
            }
            
            // If no strategy is found, try to create a specific type of pool based on item type
            if (typeof(UnityEngine.Object).IsAssignableFrom(itemType))
            {
                // For Unity objects, we'd need a prefab, which should be in factoryArgs
                if (factoryArgs.Length > 0 && factoryArgs[0] is UnityEngine.Object prefab)
                {
                    // Create a Unity pool
                    // Actual implementation would go here
                }
            }
            else if (itemType.IsValueType && !itemType.IsPrimitive)
            {
                // For value types, create a native pool
                // Actual implementation would go here
            }
            
            // For standard reference types, create a managed pool
            // Actual implementation would go here
            
            // If we can't create a pool, throw an exception
            throw new NotSupportedException($"No pool creation strategy found for type {itemType.Name}");
        }
        
        /// <summary>
        /// Generates a unique pool name for a specific type
        /// </summary>
        /// <typeparam name="T">Type of items in the pool</typeparam>
        /// <param name="poolName">Optional base name</param>
        /// <returns>A unique pool name</returns>
        private string GetUniquePoolName<T>(string poolName)
        {
            if (string.IsNullOrEmpty(poolName))
            {
                return $"Pool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }
            
            return poolName;
        }
        
        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">Message to log</param>
        private void LogInfo(string message)
        {
            Debug.Log($"[PoolFactory] {message}");
        }
        
        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Message to log</param>
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[PoolFactory] {message}");
        }
        
        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">Message to log</param>
        private void LogError(string message)
        {
            Debug.LogError($"[PoolFactory] {message}");
        }
    }
}