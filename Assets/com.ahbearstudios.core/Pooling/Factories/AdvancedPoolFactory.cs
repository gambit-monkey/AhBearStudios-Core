using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Pooling.Pools.Advanced;
using AhBearStudios.Core.Pooling.Services;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory for creating and managing advanced object pools with enhanced functionality,
    /// metrics tracking, and configuration options.
    /// </summary>
    public class AdvancedPoolFactory : IAdvancedPoolFactory
    {
        private readonly IPoolingServiceLocator _serviceLocator;
        private readonly Dictionary<string, IPool> _managedPools;
        private readonly string _factoryId;
        private FactoryError _lastError;
        private FactoryState _state;

        #region Events

        public event EventHandler<FactoryErrorEventArgs> FactoryError;
        public event EventHandler<PoolCreatedEventArgs> PoolCreated;
        public event EventHandler<PoolDestroyedEventArgs> PoolDestroyed;

        #endregion

        /// <inheritdoc/>
        public string FactoryId => _factoryId;

        /// <inheritdoc/>
        public Version ImplementationVersion => new Version(1, 0, 0);

        /// <inheritdoc/>
        public FactoryState State => _state;

        /// <inheritdoc/>
        public FactoryError LastError => _lastError;

        /// <inheritdoc/>
        public IPoolConfigRegistry ConfigRegistry { get; set; }

        /// <inheritdoc/>
        public IPoolingServiceLocator ServiceLocator
        {
            get => _serviceLocator;
            set => throw new NotSupportedException("ServiceLocator is read-only after construction");
        }

        /// <summary>
        /// Creates a new instance of AdvancedPoolFactory
        /// </summary>
        /// <param name="serviceLocator">Service locator for accessing services</param>
        public AdvancedPoolFactory(IPoolingServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
            _managedPools = new Dictionary<string, IPool>();
            _factoryId = $"AdvancedPoolFactory_{Guid.NewGuid():N}";
            _state = FactoryState.Created;

            ConfigRegistry = _serviceLocator.ConfigRegistry;
        }

        /// <inheritdoc/>
        public IAdvancedObjectPool<T> CreateAdvancedPool<T>(
            Func<T> factory,
            AdvancedPoolConfig config,
            IPoolMetrics metrics = null) where T : class
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (config == null) throw new ArgumentNullException(nameof(config));

            try
            {
                var pool = new AdvancedObjectPool<T>(
                    factory,
                    null, // Reset action
                    null, // Acquire action
                    null, // Destroy action
                    null, // Validator
                    config,
                    $"AdvPool_{typeof(T).Name}_{Guid.NewGuid():N}");

                if (metrics != null)
                {
                    // Register metrics if provided
                    _serviceLocator.GetService<IPoolDiagnostics>()?.RegisterPoolMetrics(pool, metrics);
                }

                TrackPool(pool);
                return pool;
            }
            catch (Exception ex)
            {
                _lastError = new FactoryError(FactoryErrorCode.CreationFailed, ex.Message);
                Debug.LogError($"Failed to create advanced pool: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public IAdvancedObjectPool<T> CreateAdvancedPool<T>(
            Func<T> factory,
            AdvancedPoolConfigBuilder configBuilder) where T : class
        {
            if (configBuilder == null) throw new ArgumentNullException(nameof(configBuilder));
            return CreateAdvancedPool(factory, configBuilder.Build() as AdvancedPoolConfig);
        }

        /// <inheritdoc/>
        public IAdvancedObjectPool<T> CreateAdvancedPool<T>(
            Func<T> factory,
            AdvancedPoolConfig config,
            IPoolRegistry registry,
            string poolName = null) where T : class
        {
            var pool = CreateAdvancedPool(factory, config);
            if (registry != null)
            {
                registry.RegisterPool(pool, poolName);
            }

            return pool;
        }

        /// <inheritdoc/>
        public IAdvancedObjectPool<T> GetOrCreateAdvancedPool<T>(
            string poolName,
            Func<T> factory,
            AdvancedPoolConfig config) where T : class
        {
            if (string.IsNullOrEmpty(poolName))
                throw new ArgumentException("Pool name cannot be null or empty", nameof(poolName));

            if (_managedPools.TryGetValue(poolName, out var existingPool))
            {
                return existingPool as IAdvancedObjectPool<T>;
            }

            var newPool = CreateAdvancedPool(factory, config);
            _managedPools[poolName] = newPool;
            return newPool;
        }

        /// <inheritdoc/>
        public bool ValidateAdvancedConfig(AdvancedPoolConfig config)
        {
            if (config == null) return false;

            try
            {
                // Validate configuration properties
                if (config.InitialCapacity < 0) return false;
                if (config.MaximumCapacity < 0) return false;
                if (config.MaximumCapacity > 0 && config.InitialCapacity > config.MaximumCapacity) return false;
                if (config.GrowthFactor <= 1.0f && config.UseExponentialGrowth) return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public IPoolConfigBuilder CreateConfigBuilder<T>() where T : class
        {
            return new AdvancedPoolConfigBuilder();
        }

        #region IPoolFactory Implementation

        /// <inheritdoc/>
        public IPool<T> CreatePool<T>(Func<T> factory, IPoolConfig config = null, Action<T> resetAction = null,
            string poolName = null)
            where T : class
        {
            // Convert to advanced config if needed
            var advancedConfig = config as AdvancedPoolConfig ?? new AdvancedPoolConfig();
            return CreateAdvancedPool(factory, advancedConfig);
        }

        /// <inheritdoc/>
        public bool Initialize(object initializer = null)
        {
            try
            {
                _state = FactoryState.Initializing;
                // Perform any initialization
                _state = FactoryState.Ready;
                return true;
            }
            catch (Exception ex)
            {
                _lastError = new FactoryError(FactoryErrorCode.InitializationFailed, ex.Message);
                _state = FactoryState.Error;
                return false;
            }
        }

        /// <inheritdoc/>
        public bool Reset()
        {
            try
            {
                foreach (var pool in _managedPools.Values)
                {
                    pool.Clear();
                }

                _managedPools.Clear();
                _lastError = default;
                _state = FactoryState.Ready;
                return true;
            }
            catch (Exception ex)
            {
                _lastError = new FactoryError(FactoryErrorCode.ResetFailed, ex.Message);
                return false;
            }
        }

        /// <inheritdoc/>
        public bool TrackPool(IPool pool)
        {
            if (pool == null) throw new ArgumentNullException(nameof(pool));

            var poolName = pool.GetType().Name + "_" + Guid.NewGuid().ToString("N");
            _managedPools[poolName] = pool;
            return true;
        }

        /// <inheritdoc/>
        public bool UntrackPool(IPool pool)
        {
            if (pool == null) throw new ArgumentNullException(nameof(pool));

            var key = _managedPools.FirstOrDefault(x => x.Value == pool).Key;
            return !string.IsNullOrEmpty(key) && _managedPools.Remove(key);
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<IPool> GetManagedPools()
        {
            return _managedPools.Values;
        }

        // Implement other IPoolFactory members as needed...

        #endregion

        #region Creation Strategy Management

        private readonly Dictionary<Type, Dictionary<Type, Func<object[], object>>> _creationStrategies =
            new Dictionary<Type, Dictionary<Type, Func<object[], object>>>();

        public void RegisterPoolCreationStrategy(Type poolInterfaceType, Type itemType,
            Func<object[], object> creationStrategy)
        {
            if (poolInterfaceType == null) throw new ArgumentNullException(nameof(poolInterfaceType));
            if (itemType == null) throw new ArgumentNullException(nameof(itemType));
            if (creationStrategy == null) throw new ArgumentNullException(nameof(creationStrategy));

            if (!_creationStrategies.ContainsKey(poolInterfaceType))
            {
                _creationStrategies[poolInterfaceType] = new Dictionary<Type, Func<object[], object>>();
            }

            _creationStrategies[poolInterfaceType][itemType] = creationStrategy;
        }

        public void RegisterPoolCreationStrategy<TPool, TItem>(Func<object[], TPool> creationStrategy)
            where TPool : IPool
        {
            RegisterPoolCreationStrategy(typeof(TPool), typeof(TItem), args => creationStrategy(args));
        }

        #endregion

        #region Pool Creation and Configuration

        public IPool<T> CreatePoolWithOptions<T>(PoolCreationOptions<T> options) where T : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var config = options.Config ?? new AdvancedPoolConfig();
            return CreateAdvancedPool(options.Factory, config as AdvancedPoolConfig);
        }

        public IPool CreatePoolWithConfig(Type itemType, IPoolConfig config, string poolName,
            params object[] factoryArgs)
        {
            if (itemType == null) throw new ArgumentNullException(nameof(itemType));
            if (config == null) throw new ArgumentNullException(nameof(config));

            foreach (var strategies in _creationStrategies.Values)
            {
                if (strategies.TryGetValue(itemType, out var strategy))
                {
                    var pool = strategy(factoryArgs);
                    if (pool != null)
                    {
                        OnPoolCreated(new PoolCreatedEventArgs(pool, poolName, config));
                        return pool as IPool;
                    }
                }
            }

            throw new InvalidOperationException($"No creation strategy found for type {itemType.Name}");
        }

        public bool CanCreatePoolFor(Type itemType, Type poolInterfaceType = null)
        {
            if (itemType == null) throw new ArgumentNullException(nameof(itemType));

            if (poolInterfaceType == null)
            {
                return _creationStrategies.Values.Any(strategies => strategies.ContainsKey(itemType));
            }

            return _creationStrategies.TryGetValue(poolInterfaceType, out var strategies) &&
                   strategies.ContainsKey(itemType);
        }

        public IPoolConfig GetOrCreateConfigFor<T>(IPoolConfig config = null)
        {
            if (config != null) return config;

            if (ConfigRegistry != null && ConfigRegistry.TryGetConfig<T>(typeof(T).Name, out var registeredConfig))
            {
                return registeredConfig;
            }

            return new AdvancedPoolConfig();
        }

        #endregion

        #region Extensions and Features

        private readonly Dictionary<Type, object> _extensions = new Dictionary<Type, object>();

        public TExtension GetExtension<TExtension>() where TExtension : class
        {
            return _extensions.TryGetValue(typeof(TExtension), out var extension) ? extension as TExtension : null;
        }

        public bool AddExtension<TExtension>(TExtension extension) where TExtension : class
        {
            if (extension == null) throw new ArgumentNullException(nameof(extension));
            _extensions[typeof(TExtension)] = extension;
            return true;
        }

        public bool SupportsFeature(string featureId)
        {
            if (string.IsNullOrEmpty(featureId)) return false;

            // Add feature support checks based on featureId
            return featureId switch
            {
                "AdvancedPooling" => true,
                "Metrics" => true,
                "AutoShrinking" => true,
                _ => false
            };
        }

        #endregion

        #region Lifecycle and Services

        public async Task<bool> InitializeAsync(object initializer = null)
        {
            try
            {
                _state = FactoryState.Initializing;
                await Task.Yield(); // Allow for async initialization tasks
                _state = FactoryState.Ready;
                return true;
            }
            catch (Exception ex)
            {
                _lastError = new FactoryError(FactoryErrorCode.InitializationFailed, ex.Message);
                _state = FactoryState.Error;
                OnFactoryError(new FactoryErrorEventArgs(_lastError));
                return false;
            }
        }

        public bool Shutdown(FactoryShutdownMode shutdownMode = FactoryShutdownMode.GracefulShutdown)
        {
            try
            {
                if (shutdownMode == FactoryShutdownMode.ForceShutdown)
                {
                    foreach (var pool in _managedPools.Values)
                    {
                        pool.Clear();
                        if (pool is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                }
                else
                {
                    foreach (var pool in _managedPools.Values)
                    {
                        // Check if pool has no active items using IPoolMetrics
                        if (pool is IPoolMetrics metrics && metrics.CurrentActiveCount == 0)
                        {
                            pool.Clear();
                            if (pool is IDisposable disposable)
                            {
                                disposable.Dispose();
                            }
                        }
                    }
                }

                _managedPools.Clear();
                _state = FactoryState.Shutdown;
                return true;
            }
            catch (Exception ex)
            {
                _lastError = new FactoryError(FactoryErrorCode.ShutdownFailed, ex.Message);
                OnFactoryError(new FactoryErrorEventArgs(_lastError));
                return false;
            }
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            // Store or use the service provider as needed
        }

        public bool UpdateConfiguration(IPoolConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            try
            {
                if (config is AdvancedPoolConfig advancedConfig)
                {
                    // Update factory's configuration
                    return ValidateAdvancedConfig(advancedConfig);
                }

                return false;
            }
            catch (Exception ex)
            {
                _lastError = new FactoryError(FactoryErrorCode.ConfigurationFailed, ex.Message);
                OnFactoryError(new FactoryErrorEventArgs(_lastError));
                return false;
            }
        }

        #endregion

        #region Diagnostics

        public string GenerateReport(FactoryReportOptions options = null)
        {
            options ??= new FactoryReportOptions();

            var report = new System.Text.StringBuilder();
            report.AppendLine($"Factory ID: {FactoryId}");
            report.AppendLine($"State: {State}");
            report.AppendLine($"Managed Pools: {_managedPools.Count}");

            if (options.IncludePoolStats)
            {
                foreach (var pool in _managedPools)
                {
                    if (pool.Value is IPoolMetrics metrics)
                    {
                        report.AppendLine($"Pool {pool.Key} Metrics:");
                        foreach (var metric in metrics.GetMetrics())
                        {
                            report.AppendLine($"  {metric.Key}: {metric.Value}");
                        }
                    }
                }
            }

            return report.ToString();
        }

        #endregion

        #region Event Handlers

        protected virtual void OnFactoryError(FactoryErrorEventArgs e)
        {
            FactoryError?.Invoke(this, e);
        }

        protected virtual void OnPoolCreated(PoolCreatedEventArgs e)
        {
            PoolCreated?.Invoke(this, e);
        }

        protected virtual void OnPoolDestroyed(PoolDestroyedEventArgs e)
        {
            PoolDestroyed?.Invoke(this, e);
        }

        #endregion

    }
}