using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Builders;
using AhBearStudios.Core.Logging.Interfaces;

namespace AhBearStudios.Core.Logging.Configuration
{
    /// <summary>
    /// Default implementation of log configuration registry with thread safety
    /// </summary>
    public class DefaultLogConfigRegistry : ILogConfigRegistry
    {
        private static DefaultLogConfigRegistry s_instance;
        private static readonly object s_instanceLock = new object();

        public static DefaultLogConfigRegistry Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_instanceLock)
                    {
                        if (s_instance == null)
                        {
                            s_instance = new DefaultLogConfigRegistry();
                        }
                    }
                }
                return s_instance;
            }
        }

        private readonly Dictionary<string, ILogTargetConfig> _configsByName;
        private readonly Dictionary<Type, ILogTargetConfig> _configsByType;
        private readonly object _syncLock;

        private DefaultLogConfigRegistry()
        {
            _configsByName = new Dictionary<string, ILogTargetConfig>();
            _configsByType = new Dictionary<Type, ILogTargetConfig>();
            _syncLock = new object();
        }

        public void RegisterConfig(string name, ILogTargetConfig config)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            lock (_syncLock)
            {
                _configsByName[name] = config;
            }
        }

        public void RegisterConfigForType<T>(ILogTargetConfig config) where T : class
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            lock (_syncLock)
            {
                _configsByType[typeof(T)] = config;
            }
        }

        public ILogTargetConfig GetConfig(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            lock (_syncLock)
            {
                return _configsByName.TryGetValue(name, out var config) ? config : null;
            }
        }

        public TConfig GetConfig<TConfig>(string name) where TConfig : class, ILogTargetConfig
        {
            return GetConfig(name) as TConfig;
        }

        public ILogTargetConfig GetConfigForType<T>() where T : class
        {
            lock (_syncLock)
            {
                return _configsByType.TryGetValue(typeof(T), out var config) ? config : null;
            }
        }

        public bool TryGetConfig(string name, out ILogTargetConfig config)
        {
            config = null;
            if (string.IsNullOrEmpty(name))
                return false;

            lock (_syncLock)
            {
                return _configsByName.TryGetValue(name, out config);
            }
        }

        public bool HasConfig(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            lock (_syncLock)
            {
                return _configsByName.ContainsKey(name);
            }
        }

        public bool RemoveConfig(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            lock (_syncLock)
            {
                return _configsByName.Remove(name);
            }
        }

        public void ClearAll()
        {
            lock (_syncLock)
            {
                _configsByName.Clear();
                _configsByType.Clear();
            }
        }

        public ILogTargetConfig GetOrCreateConfig(string name)
        {
            if (TryGetConfig(name, out var config))
                return config;

            // Create a default config using the factory
            var defaultConfig = LogConfigBuilderFactory.SerilogFile().Build();
            defaultConfig.TargetName = name;
            RegisterConfig(name, defaultConfig);
            return defaultConfig;
        }
    }
}