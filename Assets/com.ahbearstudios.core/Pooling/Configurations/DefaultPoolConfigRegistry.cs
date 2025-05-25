using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Diagnostics;
using AhBearStudios.Pooling.Services;

namespace AhBearStudios.Pooling.Configurations
{
    /// <summary>
    /// Registry for pool configurations that supports retrieval by both name and type,
    /// with memory optimization and thread safety. Implements singleton pattern for easy access.
    /// </summary>
    public class DefaultPoolConfigRegistry : IPoolConfigRegistry
    {
        #region Singleton Implementation

        // Singleton instance with thread safety
        private static DefaultPoolConfigRegistry s_instance;
        private static readonly object s_instanceLock = new object();

        /// <summary>
        /// Gets the singleton instance of the DefaultPoolConfigRegistry
        /// </summary>
        public static DefaultPoolConfigRegistry Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_instanceLock)
                    {
                        if (s_instance == null)
                        {
                            s_instance = new DefaultPoolConfigRegistry();
                        }
                    }
                }
                return s_instance;
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private DefaultPoolConfigRegistry()
        {
            _configsByName = new Dictionary<string, IPoolConfig>();
            _configsByType = new Dictionary<Type, IPoolConfig>();
            _configCache = new Dictionary<(string name, Type configType), IPoolConfig>();
            _syncLock = new object();
        }

        #endregion

        #region Fields

        // Dictionary storing configurations by name
        private readonly Dictionary<string, IPoolConfig> _configsByName;

        // Dictionary storing configurations by type
        private readonly Dictionary<Type, IPoolConfig> _configsByType;

        // Cache for faster lookup of typed configurations
        private readonly Dictionary<(string name, Type configType), IPoolConfig> _configCache;

        // Lock object for thread synchronization
        private readonly object _syncLock;

        // Service locator reference for accessing services
        private IPoolingServiceLocator ServiceLocator => DefaultPoolingServices.Instance;
        
        // Logger reference for logging operations
        private IPoolLogger Logger => ServiceLocator.GetService<IPoolLogger>() ?? PoolLogger.GetLogger("ConfigRegistry");

        #endregion

        #region Implementation

        /// <inheritdoc />
        public void RegisterConfig(string name, IPoolConfig config)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), "Config name cannot be null or empty");
                
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Config cannot be null");
                
            lock (_syncLock)
            {
                _configsByName[name] = config;
                ClearCacheForName(name);
                
                // Set the config ID if not already set
                if (string.IsNullOrEmpty(config.ConfigId))
                {
                    config.ConfigId = name;
                }
                
                Logger.LogDebugInstance($"Registered config '{name}' of type {config.GetType().Name}");
            }
        }

        /// <inheritdoc />
        public void RegisterConfigForType<T>(IPoolConfig config) where T : class
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Config cannot be null");
                
            lock (_syncLock)
            {
                var type = typeof(T);
                _configsByType[type] = config;
                ClearCacheForType(type);
                
                // Set the config ID if not already set
                if (string.IsNullOrEmpty(config.ConfigId))
                {
                    config.ConfigId = type.Name;
                }
                
                Logger.LogDebugInstance($"Registered config for type '{typeof(T).Name}' of config type {config.GetType().Name}");
            }
        }

        /// <inheritdoc />
        public void RegisterConfig<T>(string name, IPoolConfig config) where T : class
        {
            RegisterConfig(name, config);
            RegisterConfigForType<T>(config);
        }

        /// <inheritdoc />
        public IPoolConfig GetConfig(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
                
            lock (_syncLock)
            {
                if (_configsByName.TryGetValue(name, out IPoolConfig config))
                {
                    return config;
                }
            }
            
            return null;
        }

        /// <inheritdoc />
        public TConfig GetConfig<TConfig>(string name) where TConfig : class, IPoolConfig
        {
            if (string.IsNullOrEmpty(name))
                return null;
                
            lock (_syncLock)
            {
                // Try to get from cache first
                var cacheKey = (name, typeof(TConfig));
                if (_configCache.TryGetValue(cacheKey, out IPoolConfig cachedConfig))
                {
                    return cachedConfig as TConfig;
                }
                
                // Try to get from main registry
                if (_configsByName.TryGetValue(name, out IPoolConfig config))
                {
                    if (config is TConfig typedConfig)
                    {
                        // Cache for future use
                        _configCache[cacheKey] = typedConfig;
                        return typedConfig;
                    }
                }
            }
            
            return null;
        }

        /// <inheritdoc />
        public IPoolConfig GetConfigForType<TItem>() where TItem : class
        {
            lock (_syncLock)
            {
                if (_configsByType.TryGetValue(typeof(TItem), out IPoolConfig config))
                {
                    return config;
                }
            }
            
            return null;
        }

        /// <inheritdoc />
        public TConfig GetConfigForType<TItem, TConfig>()
            where TItem : class
            where TConfig : class, IPoolConfig
        {
            lock (_syncLock)
            {
                // Try to get from cache first
                var cacheKey = (typeof(TItem).Name, typeof(TConfig));
                if (_configCache.TryGetValue(cacheKey, out IPoolConfig cachedConfig))
                {
                    return cachedConfig as TConfig;
                }
                
                // Try to get from main registry
                if (_configsByType.TryGetValue(typeof(TItem), out IPoolConfig config))
                {
                    if (config is TConfig typedConfig)
                    {
                        // Cache for future use
                        _configCache[cacheKey] = typedConfig;
                        return typedConfig;
                    }
                }
            }
            
            return null;
        }

        /// <inheritdoc />
        public bool TryGetConfig(string name, out IPoolConfig config)
        {
            config = null;
            
            if (string.IsNullOrEmpty(name))
                return false;
                
            lock (_syncLock)
            {
                if (_configsByName.TryGetValue(name, out config))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <inheritdoc />
        public bool TryGetConfig<TConfig>(string name, out TConfig config)
            where TConfig : class, IPoolConfig
        {
            config = null;
            
            if (string.IsNullOrEmpty(name))
                return false;
                
            lock (_syncLock)
            {
                // Try to get from cache first
                var cacheKey = (name, typeof(TConfig));
                if (_configCache.TryGetValue(cacheKey, out IPoolConfig cachedConfig))
                {
                    config = cachedConfig as TConfig;
                    return config != null;
                }
                
                // Try to get from main registry
                if (_configsByName.TryGetValue(name, out IPoolConfig baseConfig))
                {
                    if (baseConfig is TConfig typedConfig)
                    {
                        // Cache for future use
                        _configCache[cacheKey] = typedConfig;
                        config = typedConfig;
                        return true;
                    }
                }
            }
            
            return false;
        }

        /// <inheritdoc />
        public bool TryGetConfigForType<TItem>(out IPoolConfig config) where TItem : class
        {
            config = null;
            
            lock (_syncLock)
            {
                if (_configsByType.TryGetValue(typeof(TItem), out config))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <inheritdoc />
        public bool TryGetConfigForType<TItem, TConfig>(out TConfig config)
            where TItem : class
            where TConfig : class, IPoolConfig
        {
            config = null;
            
            lock (_syncLock)
            {
                // Try to get from cache first
                var cacheKey = (typeof(TItem).Name, typeof(TConfig));
                if (_configCache.TryGetValue(cacheKey, out IPoolConfig cachedConfig))
                {
                    config = cachedConfig as TConfig;
                    return config != null;
                }
                
                // Try to get from main registry
                if (_configsByType.TryGetValue(typeof(TItem), out IPoolConfig baseConfig))
                {
                    if (baseConfig is TConfig typedConfig)
                    {
                        // Cache for future use
                        _configCache[cacheKey] = typedConfig;
                        config = typedConfig;
                        return true;
                    }
                }
            }
            
            return false;
        }

        /// <inheritdoc />
        public bool HasConfig(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
                
            lock (_syncLock)
            {
                return _configsByName.ContainsKey(name);
            }
        }

        /// <inheritdoc />
        public bool HasConfigForType<TItem>() where TItem : class
        {
            lock (_syncLock)
            {
                return _configsByType.ContainsKey(typeof(TItem));
            }
        }

        /// <inheritdoc />
        public bool RemoveConfig(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
                
            lock (_syncLock)
            {
                if (_configsByName.Remove(name))
                {
                    ClearCacheForName(name);
                    Logger.LogDebugInstance($"Removed config '{name}'");
                    return true;
                }
            }
            
            return false;
        }

        /// <inheritdoc />
        public bool RemoveConfigForType<TItem>() where TItem : class
        {
            lock (_syncLock)
            {
                var type = typeof(TItem);
                if (_configsByType.Remove(type))
                {
                    ClearCacheForType(type);
                    Logger.LogDebugInstance($"Removed config for type '{typeof(TItem).Name}'");
                    return true;
                }
            }
            
            return false;
        }

        /// <inheritdoc />
        public void ClearAll()
        {
            lock (_syncLock)
            {
                _configsByName.Clear();
                _configsByType.Clear();
                _configCache.Clear();
                Logger.LogDebugInstance("Cleared all configs");
            }
        }

        /// <inheritdoc />
        public IPoolConfig CreateDefaultConfigForType<TItem>() where TItem : class
        {
            var config = new PoolConfig
            {
                ConfigId = typeof(TItem).Name,
                InitialCapacity = 10,
                MaximumCapacity = 100,
                PrewarmOnInit = true,
                LogWarnings = true
            };
            
            Logger.LogDebugInstance($"Created default config for type '{typeof(TItem).Name}'");
            return config;
        }

        /// <inheritdoc />
        public IPoolConfig GetOrCreateConfig(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), "Config name cannot be null or empty");
                
            lock (_syncLock)
            {
                if (_configsByName.TryGetValue(name, out IPoolConfig config))
                {
                    return config;
                }
                
                // Create a new config with this name
                var newConfig = new PoolConfig
                {
                    ConfigId = name,
                    InitialCapacity = 10,
                    MaximumCapacity = 100,
                    PrewarmOnInit = true,
                    LogWarnings = true
                };
                
                RegisterConfig(name, newConfig);
                return newConfig;
            }
        }

        /// <inheritdoc />
        public IPoolConfig GetOrCreateConfigForType<TItem>() where TItem : class
        {
            lock (_syncLock)
            {
                if (_configsByType.TryGetValue(typeof(TItem), out IPoolConfig config))
                {
                    return config;
                }
                
                // Create a new config for this type
                var newConfig = CreateDefaultConfigForType<TItem>();
                
                RegisterConfigForType<TItem>(newConfig);
                return newConfig;
            }
        }

        // Clear cached lookups for a specific name
        private void ClearCacheForName(string name)
        {
            // Remove all cache entries for this name
            var keysToRemove = new List<(string name, Type configType)>();
            foreach (var key in _configCache.Keys)
            {
                if (key.name == name)
                {
                    keysToRemove.Add(key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _configCache.Remove(key);
            }
        }

        // Clear cached lookups for a specific type
        private void ClearCacheForType(Type type)
        {
            // Remove all cache entries for this type
            var keysToRemove = new List<(string name, Type configType)>();
            foreach (var key in _configCache.Keys)
            {
                if (key.name == type.Name || key.configType == type)
                {
                    keysToRemove.Add(key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _configCache.Remove(key);
            }
        }

        #endregion
    }
}