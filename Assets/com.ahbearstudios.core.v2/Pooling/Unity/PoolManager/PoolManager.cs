using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Collections;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Pooling.Factories;
using AhBearStudios.Core.Pooling.Pools.Native;
using AhBearStudios.Core.Pooling.Unity;
using UnityUtils;

namespace AhBearStudios.Core.Pooling.Unity
{
    /// <summary>
    /// Singleton manager for object pools with centralized creation, management, 
    /// and monitoring of different types of object pools
    /// </summary>
    public partial class PoolManager : MonoBehaviour
    {
        #region Singleton

        private static PoolManager _instance;

        /// <summary>
        /// Gets the singleton instance of the PoolManager
        /// </summary>
        public static PoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find an existing instance
                    _instance = FindObjectOfType<PoolManager>();

                    // If no instance exists, create a new one
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("PoolManager");
                        _instance = obj.AddComponent<PoolManager>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the registry of managed object pools
        /// </summary>
        public PoolRegistry Registry { get; private set; }

        /// <summary>
        /// Gets the registry of native object pools
        /// </summary>
        public NativePoolRegistry NativeRegistry { get; private set; }

        #endregion

        #region Private Fields

        private bool _initialized = false;
        private bool _isDisposing = false;
        private bool _collectMetrics = true;
        private bool _safetyChecksEnabled = true;
        private ILogger _logger;
        private IPoolDiagnostics _diagnostics;
        private ICoroutineRunner _coroutineRunner;
        private Dictionary<Type, IPoolFactory> _poolFactories;
        private PoolConfigRegistry _configRegistry;

        #endregion

        #region Unity Lifecycle Methods

        /// <summary>
        /// Initializes the PoolManager when the component awakens
        /// </summary>
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Called when the manager is enabled
        /// </summary>
        private void OnEnable()
        {
            if (_initialized && _isDisposing)
            {
                _isDisposing = false;
            }
        }

        /// <summary>
        /// Called when the manager is disabled
        /// </summary>
        private void OnDisable()
        {
            if (_initialized && !_isDisposing)
            {
                CleanupTemporaryResources();
            }
        }

        /// <summary>
        /// Called when the manager is destroyed
        /// </summary>
        private void OnDestroy()
        {
            if (_initialized && !_isDisposing)
            {
                _isDisposing = true;
                Dispose();
            }
        }

        /// <summary>
        /// Update is called once per frame
        /// </summary>
        private void Update()
        {
            if (_initialized)
            {
                PerformHealthCheck();
                PerformAutoShrink();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the PoolManager's services and registries
        /// </summary>
        private void Initialize()
        {
            if (_initialized)
                return;

            // Initialize core services
            _logger = new UnityLogger();
            _coroutineRunner = new UnityCoroutineRunner(this);
            _logger.LogInfo("Initializing PoolManager...");

            // Create registries and factories
            Registry = new PoolRegistry();
            NativeRegistry = new NativePoolRegistry();
            _poolFactories = new Dictionary<Type, IPoolFactory>();
            _configRegistry = new PoolConfigRegistry();

            // Initialize services and factories
            InitializeServices();
            InitializePoolFactories();

            // Setup diagnostics and monitoring
            SetupDiagnostics();

            _initialized = true;
            _logger.LogInfo("PoolManager initialized successfully");
        }

        /// <summary>
        /// Initializes the core services
        /// </summary>
        private void InitializeServices()
        {
            _logger.LogInfo("Initializing PoolManager services...");
            // Additional service initialization can be done here
        }

        /// <summary>
        /// Initializes and registers the pool factories
        /// </summary>
        private void InitializePoolFactories()
        {
            _logger.LogInfo("Initializing pool factories...");
            
            // Register default factories
            RegisterDefaultFactories();
        }

        /// <summary>
        /// Registers the default pool factories
        /// </summary>
        private void RegisterDefaultFactories()
        {
            // Register factories for different pool types
            // Managed object factories
            RegisterFactory(new ManagedPoolFactory());
            RegisterFactory(new ThreadSafePoolFactory());
            RegisterFactory(new ThreadLocalPoolFactory());
            RegisterFactory(new ValueTypePoolFactory());
            
            // Unity-specific factories
            RegisterFactory(new GameObjectPoolFactory());
            RegisterFactory(new ComponentPoolFactory());
            RegisterFactory(new ParticleSystemPoolFactory());
            
            // Native factories
            RegisterFactory(new NativePoolFactory());
            RegisterFactory(new BurstCompatiblePoolFactory());
            RegisterFactory(new JobCompatiblePoolFactory());
            
            // Advanced factories
            RegisterFactory(new AdvancedPoolFactory());
            RegisterFactory(new ComplexPoolFactory());
            RegisterFactory(new SemaphorePoolFactory());
            RegisterFactory(new AsyncPoolFactory());
        }

        /// <summary>
        /// Registers a pool factory
        /// </summary>
        /// <param name="factory">Factory to register</param>
        public void RegisterFactory(IPoolFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
                
            _poolFactories[factory.ItemType] = factory;
            _logger?.LogInfo($"Registered pool factory: {factory.FactoryId} for type {factory.ItemType.Name}");
        }

        /// <summary>
        /// Gets a factory for the specified type
        /// </summary>
        /// <typeparam name="T">Type of items to create pools for</typeparam>
        /// <returns>Pool factory for the specified type</returns>
        public IPoolFactory GetFactoryFor<T>()
        {
            var type = typeof(T);
            if (_poolFactories.TryGetValue(type, out var factory))
                return factory;
                
            // Look for compatible factories
            foreach (var pair in _poolFactories)
            {
                if (pair.Value.CanCreatePoolFor(type))
                    return pair.Value;
            }
            
            return null;
        }

        /// <summary>
        /// Sets up the diagnostics system
        /// </summary>
        private void SetupDiagnostics()
        {
            _logger.LogInfo("Setting up pool diagnostics...");
            _diagnostics = new PoolDiagnostics(_logger);
        }

        /// <summary>
        /// Performs periodic health check on pools
        /// </summary>
        private void PerformHealthCheck()
        {
            // Health check functionality
        }

        /// <summary>
        /// Performs auto-shrink operation for pools that support it
        /// </summary>
        private void PerformAutoShrink()
        {
            // Auto-shrink functionality
        }

        /// <summary>
        /// Ensures that the PoolManager is initialized
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_initialized)
                Initialize();
        }

        /// <summary>
        /// Cleans up temporary resources
        /// </summary>
        private void CleanupTemporaryResources()
        {
            if (Registry != null)
            {
                _logger?.LogInfo("Cleaning up temporary resources...");
                // Clean-up code here
            }
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Creates a default pool configuration
        /// </summary>
        /// <returns>A new PoolConfig with default settings</returns>
        public PoolConfig CreateDefaultPoolConfig()
        {
            return new PoolConfig
            {
                InitialCapacity = 10,
                MaxSize = 0,
                PrewarmOnInit = true,
                UseExponentialGrowth = true,
                GrowthFactor = 2.0f,
                EnableAutoShrink = false,
                CollectMetrics = _collectMetrics,
                ResetOnRelease = true,
                LogWarnings = true
            };
        }

        /// <summary>
        /// Creates a pool configuration for a specific type
        /// </summary>
        /// <typeparam name="T">Type to create configuration for</typeparam>
        /// <returns>A pool configuration suitable for the specified type</returns>
        public PoolConfig CreateConfigForType<T>()
        {
            // Get from registry if exists
            if (_configRegistry.TryGetConfigForType<T>(out var existingConfig))
            {
                return existingConfig as PoolConfig;
            }
            
            // Create new config with type-specific settings
            var config = CreateDefaultPoolConfig();
            
            // Adjust for value types
            if (typeof(T).IsValueType)
            {
                config.InitialCapacity = 20;
                config.UseExponentialGrowth = true;
                
                // For unmanaged types, set native allocator
                if (typeof(T).IsUnmanagedType())
                {
                    config.NativeAllocator = Allocator.Persistent;
                }
            }
            
            // Adjust for Unity components
            if (typeof(UnityEngine.Component).IsAssignableFrom(typeof(T)))
            {
                config.ResetOnRelease = true;
                config.InitialCapacity = 5;
            }
            
            // Adjust for GameObjects
            if (typeof(T) == typeof(GameObject))
            {
                config.ResetOnRelease = true;
                config.InitialCapacity = 5;
            }
            
            // Register the config
            _configRegistry.RegisterConfigForType<T>(config);
            
            return config;
        }

        /// <summary>
        /// Gets statistics for all registered pools
        /// </summary>
        /// <returns>Dictionary containing pool statistics</returns>
        public Dictionary<string, Dictionary<string, object>> GetAllPoolStats()
        {
            EnsureInitialized();
            
            var results = new Dictionary<string, Dictionary<string, object>>();
            
            // Collect managed pool stats
            if (Registry != null)
            {
                foreach (var poolName in Registry.GetAllPoolNames())
                {
                    var pool = Registry.GetPool(poolName);
                    if (pool != null && pool.CollectsMetrics)
                    {
                        results[poolName] = pool.GetMetrics();
                    }
                }
            }
            
            // Collect native pool stats
            if (NativeRegistry != null)
            {
                foreach (var pool in NativeRegistry.GetAllPools())
                {
                    if (pool != null && pool.CollectsMetrics)
                    {
                        results[pool.PoolName] = pool.GetMetrics();
                    }
                }
            }
            
            return results;
        }

        /// <summary>
        /// Gets global metrics about the pool manager
        /// </summary>
        /// <returns>Dictionary containing global metrics</returns>
        public Dictionary<string, object> GetGlobalMetrics()
        {
            EnsureInitialized();
            
            var metrics = new Dictionary<string, object>
            {
                ["TotalManagedPools"] = Registry?.PoolCount ?? 0,
                ["TotalNativePools"] = NativeRegistry?.PoolCount ?? 0,
                ["Initialized"] = _initialized,
                ["IsDisposing"] = _isDisposing,
                ["CollectMetrics"] = _collectMetrics,
                ["SafetyChecksEnabled"] = _safetyChecksEnabled
            };
            
            return metrics;
        }

        #endregion

        #region Pool Creation

        /// <summary>
        /// Gets or creates a default pool for the specified type
        /// </summary>
        /// <typeparam name="T">Type of objects to pool</typeparam>
        /// <param name="factory">Optional factory function to create new instances</param>
        /// <returns>A pool for the specified type</returns>
        public IPool<T> GetOrCreateDefaultPool<T>(Func<T> factory = null)
        {
            EnsureInitialized();
            
            string poolName = GetUniquePoolName<T>();
            
            // Check if pool already exists
            if (Registry.HasPool(poolName))
            {
                var existingPool = Registry.GetPool(poolName);
                if (existingPool is IPool<T> typedPool)
                {
                    return typedPool;
                }
            }
            
            // Create factory if not provided
            factory ??= () => Activator.CreateInstance<T>();
            
            // Get appropriate factory for this type
            var poolFactory = GetFactoryFor<T>();
            if (poolFactory != null)
            {
                // Create config
                var config = CreateConfigForType<T>();
                
                // Create pool using factory
                var pool = poolFactory.CreatePool(config, poolName);
                
                // Register if needed
                if (pool is IPool<T> typedPool && !Registry.HasPool(poolName))
                {
                    Registry.RegisterPool(typedPool, poolName);
                    return typedPool;
                }
                
                return pool as IPool<T>;
            }
            
            // Fallback to managed pool
            return CreateManagedPool(factory, poolName: poolName);
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Disposes the PoolManager and all managed resources
        /// </summary>
        public void Dispose()
        {
            if (!_initialized || _isDisposing)
                return;
                
            _isDisposing = true;
            _logger?.LogInfo("Disposing PoolManager...");
            
            try
            {
                // Dispose all registered pools
                if (Registry != null)
                {
                    Registry.DisposeAllPools();
                }
                
                // Dispose all native pools
                if (NativeRegistry != null)
                {
                    NativeRegistry.DisposeAllPools();
                }
                
                // Clear registries
                Registry = null;
                NativeRegistry = null;
                _poolFactories?.Clear();
                _poolFactories = null;
                _configRegistry = null;
                
                // Clean up diagnostics
                _diagnostics = null;
                
                _initialized = false;
                _logger?.LogInfo("PoolManager disposed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error during PoolManager disposal: {ex.Message}");
            }
        }

        /// <summary>
        /// Configures safety checks for pools
        /// </summary>
        /// <param name="enabled">Whether safety checks are enabled</param>
        public void SetSafetyChecks(bool enabled)
        {
            _safetyChecksEnabled = enabled;
        }

        /// <summary>
        /// Validates an operation based on a condition
        /// </summary>
        /// <param name="condition">Condition to check</param>
        /// <param name="message">Message to display if condition fails</param>
        /// <exception cref="InvalidOperationException">Thrown if condition is false and safety checks are enabled</exception>
        internal void ValidateOperation(bool condition, string message)
        {
            if (_safetyChecksEnabled && !condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Checks if the manager is initialized and throws an exception if not
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if manager is not initialized</exception>
        internal void CheckInitialized()
        {
            ValidateOperation(_initialized, "PoolManager is not initialized");
        }

        /// <summary>
        /// Gets a unique pool name for the specified type
        /// </summary>
        /// <typeparam name="T">Type to get a pool name for</typeparam>
        /// <param name="poolName">Optional base name for the pool</param>
        /// <returns>A unique pool name</returns>
        public string GetUniquePoolName<T>(string poolName = null)
        {
            if (string.IsNullOrEmpty(poolName))
            {
                poolName = $"Pool_{typeof(T).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }
            return poolName;
        }

        #endregion
    }
}