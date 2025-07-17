using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Targets;

namespace AhBearStudios.Core.Logging.Factories
{
    /// <summary>
    /// Factory implementation for creating log target instances from configuration.
    /// Follows the Factory pattern as specified in the AhBearStudios Core Architecture.
    /// </summary>
    public sealed class LogTargetFactory : ILogTargetFactory
    {
        private readonly Dictionary<string, Func<LogTargetConfig, ILogTarget>> _targetFactories;
        private readonly ILoggingService _loggingService;

        /// <summary>
        /// Initializes a new instance of the LogTargetFactory.
        /// </summary>
        /// <param name="loggingService">The logging service for internal logging</param>
        public LogTargetFactory(ILoggingService loggingService = null)
        {
            _loggingService = loggingService;
            _targetFactories = new Dictionary<string, Func<LogTargetConfig, ILogTarget>>(StringComparer.OrdinalIgnoreCase);
            
            RegisterDefaultTargetTypes();
        }

        /// <inheritdoc />
        public ILogTarget CreateTarget(LogTargetConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var validationErrors = ValidateTargetConfig(config);
            if (validationErrors.Count > 0)
            {
                var errorMessage = string.Join(Environment.NewLine, validationErrors);
                throw new InvalidOperationException($"Target configuration validation failed:{Environment.NewLine}{errorMessage}");
            }

            if (!_targetFactories.TryGetValue(config.TargetType, out var factory))
            {
                _loggingService?.LogError($"Unknown target type: {config.TargetType}");
                throw new InvalidOperationException($"Unknown target type: {config.TargetType}. Available types: {string.Join(", ", _targetFactories.Keys)}");
            }

            try
            {
                var target = factory(config);
                _loggingService?.LogInfo($"Created log target '{config.Name}' of type '{config.TargetType}'");
                return target;
            }
            catch (Exception ex)
            {
                _loggingService?.LogException($"Failed to create log target '{config.Name}' of type '{config.TargetType}'", ex);
                throw new InvalidOperationException($"Failed to create log target '{config.Name}' of type '{config.TargetType}'", ex);
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<ILogTarget> CreateTargets(IEnumerable<LogTargetConfig> configs)
        {
            if (configs == null)
                throw new ArgumentNullException(nameof(configs));

            var configList = configs.ToList();
            var targets = new List<ILogTarget>(configList.Count);

            foreach (var config in configList)
            {
                try
                {
                    targets.Add(CreateTarget(config));
                }
                catch (Exception ex)
                {
                    _loggingService?.LogException($"Failed to create target from config: {config?.Name ?? "Unknown"}", ex);
                    
                    // Continue creating other targets even if one fails
                    // This provides graceful degradation
                }
            }

            return targets.AsReadOnly();
        }

        /// <inheritdoc />
        public T CreateTarget<T>(LogTargetConfig config) where T : class, ILogTarget
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var target = CreateTarget(config);
            
            if (target is T typedTarget)
            {
                return typedTarget;
            }

            target?.Dispose();
            throw new InvalidOperationException($"Created target is not of type {typeof(T).Name}");
        }

        /// <inheritdoc />
        public void RegisterTargetType(string targetType, Func<LogTargetConfig, ILogTarget> factory)
        {
            if (string.IsNullOrWhiteSpace(targetType))
                throw new ArgumentException("Target type cannot be null or empty", nameof(targetType));
            
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _targetFactories[targetType] = factory;
            _loggingService?.LogInfo($"Registered log target type: {targetType}");
        }

        /// <inheritdoc />
        public void RegisterTargetType<T>(string targetType) where T : class, ILogTarget, new()
        {
            if (string.IsNullOrWhiteSpace(targetType))
                throw new ArgumentException("Target type cannot be null or empty", nameof(targetType));

            RegisterTargetType(targetType, config => new T());
        }

        /// <inheritdoc />
        public bool UnregisterTargetType(string targetType)
        {
            if (string.IsNullOrWhiteSpace(targetType))
                return false;

            var removed = _targetFactories.Remove(targetType);
            if (removed)
            {
                _loggingService?.LogInfo($"Unregistered log target type: {targetType}");
            }

            return removed;
        }

        /// <inheritdoc />
        public IReadOnlyList<string> GetRegisteredTargetTypes()
        {
            return _targetFactories.Keys.ToList().AsReadOnly();
        }

        /// <inheritdoc />
        public bool IsTargetTypeRegistered(string targetType)
        {
            if (string.IsNullOrWhiteSpace(targetType))
                return false;

            return _targetFactories.ContainsKey(targetType);
        }

        /// <inheritdoc />
        public IReadOnlyList<string> ValidateTargetConfig(LogTargetConfig config)
        {
            var errors = new List<string>();

            if (config == null)
            {
                errors.Add("Target configuration cannot be null");
                return errors.AsReadOnly();
            }

            // Use the config's own validation
            var configErrors = config.Validate();
            errors.AddRange(configErrors);

            // Additional factory-specific validation
            if (!IsTargetTypeRegistered(config.TargetType))
            {
                errors.Add($"Target type '{config.TargetType}' is not registered. Available types: {string.Join(", ", _targetFactories.Keys)}");
            }

            return errors.AsReadOnly();
        }

        /// <inheritdoc />
        public ILogTarget CreateDefaultTarget()
        {
            var defaultConfig = new LogTargetConfig
            {
                Name = "DefaultMemoryTarget",
                TargetType = "Memory",
                MinimumLevel = LogLevel.Debug,
                IsEnabled = true,
                BufferSize = 1000,
                FlushInterval = TimeSpan.FromMilliseconds(100),
                UseAsyncWrite = true
            };

            return CreateTarget(defaultConfig);
        }

        /// <summary>
        /// Registers the default target types that are available by default.
        /// </summary>
        private void RegisterDefaultTargetTypes()
        {
            // Register Memory target
            RegisterTargetType("Memory", config => new MemoryLogTarget(config));
            
            // Register File target
            RegisterTargetType("File", config => new FileLogTarget(config));
            
            // Register Console target (when available)
            RegisterTargetType("Console", config => new ConsoleLogTarget(config));
            
            // Register Null target for testing/disabled scenarios
            RegisterTargetType("Null", config => new NullLogTarget(config));
        }
    }
}