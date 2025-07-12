using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Factories;
using AhBearStudios.Core.Logging.Services;
using AhBearStudios.Core.Logging.Targets;

namespace AhBearStudios.Core.Logging.Factories
{
    /// <summary>
    /// Enhanced factory implementation for creating logging service instances from configuration.
    /// Supports all available log targets and provides comprehensive target creation capabilities.
    /// </summary>
    public sealed class LoggingServiceFactory : ILoggingServiceFactory
    {
        private readonly ILogTargetFactory _targetFactory;
        private readonly LogFormattingService _formattingService;
        private readonly LogBatchingService _batchingService;

        /// <summary>
        /// Initializes a new instance of the LoggingServiceFactory.
        /// </summary>
        /// <param name="targetFactory">Factory for creating log targets</param>
        /// <param name="formattingService">Service for formatting log messages</param>
        /// <param name="batchingService">Service for batching log operations</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null</exception>
        public LoggingServiceFactory(
            ILogTargetFactory targetFactory,
            LogFormattingService formattingService = null,
            LogBatchingService batchingService = null)
        {
            _targetFactory = targetFactory ?? throw new ArgumentNullException(nameof(targetFactory));
            _formattingService = formattingService;
            _batchingService = batchingService;
            
            // Ensure all known target types are registered
            RegisterAllKnownTargetTypes();
        }

        /// <inheritdoc />
        public ILoggingService CreateLoggingService(LoggingConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var validationErrors = ValidateConfiguration(config);
            if (validationErrors.Count > 0)
            {
                var errorMessage = string.Join(Environment.NewLine, validationErrors);
                throw new InvalidOperationException($"Configuration validation failed:{Environment.NewLine}{errorMessage}");
            }

            return CreateLoggingServiceInternal(config, _targetFactory);
        }

        /// <inheritdoc />
        public ILoggingService CreateDefaultLoggingService()
        {
            var config = LoggingConfig.Default;
            return CreateLoggingService(config);
        }

        /// <inheritdoc />
        public ILoggingService CreateHighPerformanceLoggingService(LoggingConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // Override configuration for high-performance settings
            var highPerfConfig = config with
            {
                HighPerformanceMode = true,
                BurstCompatibility = true,
                BatchingEnabled = true,
                CachingEnabled = true,
                StructuredLogging = false // Reduce allocations
            };

            return CreateLoggingService(highPerfConfig);
        }

        /// <inheritdoc />
        public ILoggingService CreateDevelopmentLoggingService(LoggingConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // Override configuration for development settings
            var devConfig = config with
            {
                GlobalMinimumLevel = LogLevel.Debug,
                StructuredLogging = true,
                IncludeTimestamps = true,
                HighPerformanceMode = false, // Favor readability over performance
                BatchingEnabled = false // Immediate output for debugging
            };

            return CreateLoggingService(devConfig);
        }

        /// <inheritdoc />
        public IReadOnlyList<string> ValidateConfiguration(LoggingConfig config)
        {
            if (config == null)
                return new List<string> { "Configuration cannot be null." }.AsReadOnly();

            var errors = new List<string>();

            // Use the configuration's built-in validation
            var configErrors = config.Validate();
            errors.AddRange(configErrors);

            // Additional factory-specific validation
            if (config.TargetConfigs.Count == 0)
            {
                errors.Add("At least one log target must be configured.");
            }

            // Validate each target configuration can be created
            foreach (var targetConfig in config.TargetConfigs)
            {
                try
                {
                    var targetErrors = _targetFactory.ValidateTargetConfig(targetConfig);
                    errors.AddRange(targetErrors.Select(e => $"Target '{targetConfig.Name}': {e}"));
                }
                catch (Exception ex)
                {
                    errors.Add($"Target '{targetConfig.Name}' validation failed: {ex.Message}");
                }
            }

            return errors.AsReadOnly();
        }

        /// <inheritdoc />
        public ILoggingService CreateLoggingService(LoggingConfig config, ILogTargetFactory targetFactory)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (targetFactory == null)
                throw new ArgumentNullException(nameof(targetFactory));

            var validationErrors = config.Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = string.Join(Environment.NewLine, validationErrors);
                throw new InvalidOperationException($"Configuration validation failed:{Environment.NewLine}{errorMessage}");
            }

            return CreateLoggingServiceInternal(config, targetFactory);
        }

        /// <summary>
        /// Registers all known target types with the target factory.
        /// </summary>
        private void RegisterAllKnownTargetTypes()
        {
            // Register all standard target types
            RegisterTargetIfNotExists("Memory", config => CreateMemoryTarget(config));
            RegisterTargetIfNotExists("File", config => CreateFileTarget(config));
            RegisterTargetIfNotExists("Console", config => CreateConsoleTarget(config));
            RegisterTargetIfNotExists("UnityConsole", config => CreateUnityConsoleTarget(config));
            RegisterTargetIfNotExists("Serilog", config => CreateSerilogTarget(config));
            RegisterTargetIfNotExists("Null", config => CreateNullTarget(config));
            RegisterTargetIfNotExists("Network", config => CreateNetworkTarget(config));
            RegisterTargetIfNotExists("Database", config => CreateDatabaseTarget(config));
            RegisterTargetIfNotExists("Email", config => CreateEmailTarget(config));
            RegisterTargetIfNotExists("RollingFile", config => CreateRollingFileTarget(config));
        }

        /// <summary>
        /// Registers a target type if it's not already registered.
        /// </summary>
        private void RegisterTargetIfNotExists(string targetType, Func<LogTargetConfig, ILogTarget> factory)
        {
            if (!_targetFactory.IsTargetTypeRegistered(targetType))
            {
                _targetFactory.RegisterTargetType(targetType, factory);
            }
        }

        /// <summary>
        /// Creates a Memory target instance.
        /// </summary>
        private ILogTarget CreateMemoryTarget(LogTargetConfig config)
        {
            try
            {
                var memoryTargetType = Type.GetType("AhBearStudios.Core.Logging.Targets.MemoryLogTarget");
                if (memoryTargetType != null)
                {
                    return (ILogTarget)Activator.CreateInstance(memoryTargetType, config);
                }
                
                return CreateFallbackTarget(config, "Memory target not available");
            }
            catch (Exception ex)
            {
                return CreateFallbackTarget(config, $"Failed to create Memory target: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a File target instance.
        /// </summary>
        private ILogTarget CreateFileTarget(LogTargetConfig config)
        {
            try
            {
                var fileTargetType = Type.GetType("AhBearStudios.Core.Logging.Targets.FileLogTarget");
                if (fileTargetType != null)
                {
                    return (ILogTarget)Activator.CreateInstance(fileTargetType, config);
                }
                
                return CreateFallbackTarget(config, "File target not available");
            }
            catch (Exception ex)
            {
                return CreateFallbackTarget(config, $"Failed to create File target: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a Console target instance.
        /// </summary>
        private ILogTarget CreateConsoleTarget(LogTargetConfig config)
        {
            try
            {
                var consoleTargetType = Type.GetType("AhBearStudios.Core.Logging.Targets.ConsoleLogTarget");
                if (consoleTargetType != null)
                {
                    return (ILogTarget)Activator.CreateInstance(consoleTargetType, config);
                }
                
                return CreateFallbackTarget(config, "Console target not available");
            }
            catch (Exception ex)
            {
                return CreateFallbackTarget(config, $"Failed to create Console target: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a Unity Console target instance.
        /// </summary>
        private ILogTarget CreateUnityConsoleTarget(LogTargetConfig config)
        {
            try
            {
                var unityTargetType = Type.GetType("AhBearStudios.Unity.Logging.Targets.UnityConsoleLogTarget");
                if (unityTargetType != null)
                {
                    return (ILogTarget)Activator.CreateInstance(unityTargetType, config);
                }
                
                // Fallback to standard console if Unity target not available
                return CreateConsoleTarget(config);
            }
            catch (Exception ex)
            {
                return CreateConsoleTarget(config);
            }
        }

        /// <summary>
        /// Creates a Serilog target instance.
        /// </summary>
        private ILogTarget CreateSerilogTarget(LogTargetConfig config)
        {
            try
            {
                var serilogTargetType = Type.GetType("AhBearStudios.Core.Logging.Targets.SerilogTarget");
                if (serilogTargetType != null)
                {
                    return (ILogTarget)Activator.CreateInstance(serilogTargetType, config);
                }
                
                // Fallback to file target if Serilog not available
                return CreateFileTarget(config);
            }
            catch (Exception ex)
            {
                return CreateFileTarget(config);
            }
        }

        /// <summary>
        /// Creates a Null target instance.
        /// </summary>
        private ILogTarget CreateNullTarget(LogTargetConfig config)
        {
            try
            {
                var nullTargetType = Type.GetType("AhBearStudios.Core.Logging.Targets.NullLogTarget");
                if (nullTargetType != null)
                {
                    return (ILogTarget)Activator.CreateInstance(nullTargetType, config);
                }
                
                return CreateFallbackTarget(config, "Null target not available");
            }
            catch (Exception ex)
            {
                return CreateFallbackTarget(config, $"Failed to create Null target: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a Network target instance.
        /// </summary>
        private ILogTarget CreateNetworkTarget(LogTargetConfig config)
        {
            try
            {
                var networkTargetType = Type.GetType("AhBearStudios.Core.Logging.Targets.NetworkLogTarget");
                if (networkTargetType != null)
                {
                    return (ILogTarget)Activator.CreateInstance(networkTargetType, config);
                }
                
                // Fallback to memory target if network target not available
                return CreateMemoryTarget(config);
            }
            catch (Exception ex)
            {
                return CreateMemoryTarget(config);
            }
        }

        /// <summary>
        /// Creates a Database target instance.
        /// </summary>
        private ILogTarget CreateDatabaseTarget(LogTargetConfig config)
        {
            try
            {
                var dbTargetType = Type.GetType("AhBearStudios.Core.Logging.Targets.DatabaseLogTarget");
                if (dbTargetType != null)
                {
                    return (ILogTarget)Activator.CreateInstance(dbTargetType, config);
                }
                
                // Fallback to file target if database target not available
                return CreateFileTarget(config);
            }
            catch (Exception ex)
            {
                return CreateFileTarget(config);
            }
        }

        /// <summary>
        /// Creates an Email target instance.
        /// </summary>
        private ILogTarget CreateEmailTarget(LogTargetConfig config)
        {
            try
            {
                var emailTargetType = Type.GetType("AhBearStudios.Core.Logging.Targets.EmailLogTarget");
                if (emailTargetType != null)
                {
                    return (ILogTarget)Activator.CreateInstance(emailTargetType, config);
                }
                
                // Fallback to memory target if email target not available
                return CreateMemoryTarget(config);
            }
            catch (Exception ex)
            {
                return CreateMemoryTarget(config);
            }
        }

        /// <summary>
        /// Creates a Rolling File target instance.
        /// </summary>
        private ILogTarget CreateRollingFileTarget(LogTargetConfig config)
        {
            try
            {
                var rollingFileTargetType = Type.GetType("AhBearStudios.Core.Logging.Targets.RollingFileLogTarget");
                if (rollingFileTargetType != null)
                {
                    return (ILogTarget)Activator.CreateInstance(rollingFileTargetType, config);
                }
                
                // Fallback to regular file target
                return CreateFileTarget(config);
            }
            catch (Exception ex)
            {
                return CreateFileTarget(config);
            }
        }

        /// <summary>
        /// Creates a fallback target when the requested target type is not available.
        /// </summary>
        private ILogTarget CreateFallbackTarget(LogTargetConfig config, string reason)
        {
            // Create a simple in-memory fallback target
            return CreateNullTarget(config with { Name = $"{config.Name}_Fallback" });
        }

        /// <summary>
        /// Internal method for creating logging service instances.
        /// </summary>
        /// <param name="config">The validated logging configuration</param>
        /// <param name="targetFactory">The target factory to use</param>
        /// <returns>A fully configured logging service instance</returns>
        private ILoggingService CreateLoggingServiceInternal(LoggingConfig config, ILogTargetFactory targetFactory)
        {
            try
            {
                // Create log targets from configuration
                var targets = new List<ILogTarget>();
                foreach (var targetConfig in config.TargetConfigs.Where(t => t.IsEnabled))
                {
                    try
                    {
                        var target = targetFactory.CreateTarget(targetConfig);
                        targets.Add(target);
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue with other targets for graceful degradation
                        System.Diagnostics.Debug.WriteLine($"Failed to create target '{targetConfig.Name}': {ex.Message}");
                    }
                }

                // Ensure we have at least one target
                if (targets.Count == 0)
                {
                    // Create a fallback console target
                    var fallbackTarget = targetFactory.CreateDefaultTarget();
                    targets.Add(fallbackTarget);
                }

                // Create the logging service with dependencies
                var loggingService = new LoggingService(
                    config,
                    targets,
                    _formattingService,
                    _batchingService,
                    healthCheckService: null, // Will be injected during bootstrap
                    alertService: null, // Will be injected during bootstrap
                    profilerService: null); // Will be injected during bootstrap

                // Register targets with the service
                foreach (var target in targets)
                {
                    loggingService.RegisterTarget(target);
                }

                return loggingService;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create logging service: {ex.Message}", ex);
            }
        }
    }
}

        /// <inheritdoc />
        public ILoggingService CreateLoggingService(LoggingConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var validationErrors = ValidateConfiguration(config);
            if (validationErrors.Count > 0)
            {
                var errorMessage = string.Join(Environment.NewLine, validationErrors);
                throw new InvalidOperationException($"Configuration validation failed:{Environment.NewLine}{errorMessage}");
            }

            return CreateLoggingServiceInternal(config, _targetFactory);
        }

        /// <inheritdoc />
        public ILoggingService CreateDefaultLoggingService()
        {
            var config = LoggingConfig.Default;
            return CreateLoggingService(config);
        }

        /// <inheritdoc />
        public ILoggingService CreateHighPerformanceLoggingService(LoggingConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // Override configuration for high-performance settings
            var highPerfConfig = config with
            {
                HighPerformanceMode = true,
                BurstCompatibility = true,
                BatchingEnabled = true,
                CachingEnabled = true,
                StructuredLogging = false // Reduce allocations
            };

            return CreateLoggingService(highPerfConfig);
        }

        /// <inheritdoc />
        public ILoggingService CreateDevelopmentLoggingService(LoggingConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // Override configuration for development settings
            var devConfig = config with
            {
                GlobalMinimumLevel = LogLevel.Debug,
                StructuredLogging = true,
                IncludeTimestamps = true,
                HighPerformanceMode = false, // Favor readability over performance
                BatchingEnabled = false // Immediate output for debugging
            };

            return CreateLoggingService(devConfig);
        }

        /// <inheritdoc />
        public IReadOnlyList<string> ValidateConfiguration(LoggingConfig config)
        {
            if (config == null)
                return new List<string> { "Configuration cannot be null." }.AsReadOnly();

            var errors = new List<string>();

            // Use the configuration's built-in validation
            var configErrors = config.Validate();
            errors.AddRange(configErrors);

            // Additional factory-specific validation
            if (config.TargetConfigs.Count == 0)
            {
                errors.Add("At least one log target must be configured.");
            }

            // Validate each target configuration can be created
            foreach (var targetConfig in config.TargetConfigs)
            {
                try
                {
                    var targetErrors = _targetFactory.ValidateTargetConfig(targetConfig);
                    errors.AddRange(targetErrors.Select(e => $"Target '{targetConfig.Name}': {e}"));
                }
                catch (Exception ex)
                {
                    errors.Add($"Target '{targetConfig.Name}' validation failed: {ex.Message}");
                }
            }

            return errors.AsReadOnly();
        }

        /// <inheritdoc />
        public ILoggingService CreateLoggingService(LoggingConfig config, ILogTargetFactory targetFactory)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (targetFactory == null)
                throw new ArgumentNullException(nameof(targetFactory));

            var validationErrors = config.Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = string.Join(Environment.NewLine, validationErrors);
                throw new InvalidOperationException($"Configuration validation failed:{Environment.NewLine}{errorMessage}");
            }

            return CreateLoggingServiceInternal(config, targetFactory);
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
            
            // Register standard Console target (not Unity-specific)
            RegisterTargetType("Console", config => new ConsoleLogTarget(config));
            
            // Register Unity Console target (Unity-specific)
            RegisterTargetType("UnityConsole", config => CreateUnityConsoleTarget(config));
            
            // Register Serilog target (enterprise logging)
            RegisterTargetType("Serilog", config => CreateSerilogTarget(config));
            
            // Register Null target for testing/disabled scenarios
            RegisterTargetType("Null", config => new NullLogTarget(config));
            
            // Register Network target for remote logging
            RegisterTargetType("Network", config => CreateNetworkTarget(config));
            
            // Register Database target for structured storage
            RegisterTargetType("Database", config => CreateDatabaseTarget(config));
            
            // Register Email target for critical alerts
            RegisterTargetType("Email", config => CreateEmailTarget(config));
            
            // Register Rolling File target for production scenarios
            RegisterTargetType("RollingFile", config => CreateRollingFileTarget(config));
        }

        /// <summary>
        /// Creates a Unity Console target with Unity-specific configuration.
        /// </summary>
        private ILogTarget CreateUnityConsoleTarget(LogTargetConfig config)
        {
            try
            {
                // In a real implementation, this would create UnityConsoleLogTarget
                // For now, we'll use reflection or factory patterns
                var unityTargetType = Type.GetType("AhBearStudios.Unity.Logging.Targets.UnityConsoleLogTarget");
                if (unityTargetType != null)
                {
                    return (ILogTarget)Activator.CreateInstance(unityTargetType, config);
                }
                
                // Fallback to standard console if Unity target not available
                return new ConsoleLogTarget(config);
            }
            catch (Exception ex)
            {
                _loggingService?.LogException(ex, "Failed to create Unity Console target, falling back to standard console");
                return new ConsoleLogTarget(config);
            }
        }

        /// <summary>
        /// Creates a Serilog target with enterprise-grade configuration.
        /// </summary>
        private ILogTarget CreateSerilogTarget(LogTargetConfig config)
        {
            try
            {
                // In a real implementation, this would create SerilogTarget
                var serilogTargetType = Type.GetType("AhBearStudios.Core.Logging.Targets.SerilogTarget");
                if (serilogTargetType != null)
                {
                    return (ILogTarget)Activator.CreateInstance(serilogTargetType, config);
                }
                
                // Fallback to file target if Serilog not available
                return new FileLogTarget(config);
            }
            catch (Exception ex)
            {
                _loggingService?.LogException(ex, "Failed to create Serilog target, falling back to file target");
                return new FileLogTarget(config);
            }
        }

        /// <summary>
        /// Creates a Network target for remote logging.
        /// </summary>
        private ILogTarget CreateNetworkTarget(LogTargetConfig config)
        {
            try
            {
                // In a real implementation, this would create NetworkLogTarget
                var networkTargetType = Type.GetType("AhBearStudios.Core.Logging.Targets.NetworkLogTarget");
                if (networkTargetType != null)
                {
                    return (ILogTarget)Activator.CreateInstance(networkTargetType, config);
                }
                
                // Fallback to memory target if network target not available
                return new MemoryLogTarget(config);
            }
            catch (Exception ex)
            {
                _loggingService?.LogException(ex, "Failed to create Network target, falling back to memory target");
                return new MemoryLogTarget(config);
            }
        }

        /// <summary>
        /// Creates a Database target for structured log storage.
        /// </summary>
        private ILogTarget CreateDatabaseTarget(LogTargetConfig config)
        {
            try
            {
                // In a real implementation, this would create DatabaseLogTarget
                var dbTargetType = Type.GetType("AhBearStudios.Core.Logging.Targets.DatabaseLogTarget");
                if (dbTargetType != null)
                {
                    return (ILogTarget)Activator.CreateInstance(dbTargetType, config);
                }
                
                // Fallback to file target if database target not available
                return new FileLogTarget(config);
            }
            catch (Exception ex)
            {
                _loggingService?.LogException(ex, "Failed to create Database target, falling back to file target");
                return new FileLogTarget(config);
            }
        }

        /// <summary>
        /// Creates an Email target for critical alerts.
        /// </summary>
        private ILogTarget CreateEmailTarget(LogTargetConfig config)
        {
            try
            {
                // In a real implementation, this would create EmailLogTarget
                var emailTargetType = Type.GetType("AhBearStudios.Core.Logging.Targets.EmailLogTarget");
                if (emailTargetType != null)
                {
                    return (ILogTarget)Activator.CreateInstance(emailTargetType, config);
                }
                
                // Fallback to memory target if email target not available
                return new MemoryLogTarget(config);
            }
            catch (Exception ex)
            {
                _loggingService?.LogException(ex, "Failed to create Email target, falling back to memory target");
                return new MemoryLogTarget(config);
            }
        }

        /// <summary>
        /// Creates a Rolling File target for production log rotation.
        /// </summary>
        private ILogTarget CreateRollingFileTarget(LogTargetConfig config)
        {
            try
            {
                // In a real implementation, this would create RollingFileLogTarget
                var rollingFileTargetType = Type.GetType("AhBearStudios.Core.Logging.Targets.RollingFileLogTarget");
                if (rollingFileTargetType != null)
                {
                    return (ILogTarget)Activator.CreateInstance(rollingFileTargetType, config);
                }
                
                // Fallback to regular file target
                return new FileLogTarget(config);
            }
            catch (Exception ex)
            {
                _loggingService?.LogException(ex, "Failed to create Rolling File target, falling back to file target");
                return new FileLogTarget(config);
            }
        }
    }
}