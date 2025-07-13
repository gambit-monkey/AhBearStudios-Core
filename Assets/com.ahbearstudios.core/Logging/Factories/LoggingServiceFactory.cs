using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Services;
using AhBearStudios.Core.Logging.Targets;
using AhBearStudios.Core.Logging.Builders;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Factories
{
    /// <summary>
    /// Enhanced factory implementation for creating logging service instances from configuration.
    /// Works cohesively with ILogConfigBuilder and LogConfigBuilder to support all available log targets.
    /// Follows proper dependency injection and factory patterns with complete builder integration.
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
        /// <param name="formattingService">Service for formatting log messages (optional)</param>
        /// <param name="batchingService">Service for batching log operations (optional)</param>
        /// <exception cref="ArgumentNullException">Thrown when targetFactory is null</exception>
        public LoggingServiceFactory(
            ILogTargetFactory targetFactory,
            LogFormattingService formattingService = null,
            LogBatchingService batchingService = null)
        {
            _targetFactory = targetFactory ?? throw new ArgumentNullException(nameof(targetFactory));
            _formattingService = formattingService;
            _batchingService = batchingService;
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
            
            // If the default config has no targets, add a memory target
            if (config.TargetConfigs.Count == 0)
            {
                config = config with
                {
                    TargetConfigs = new List<LogTargetConfig>
                    {
                        new LogTargetConfig
                        {
                            Name = "DefaultMemory",
                            TargetType = "Memory",
                            MinimumLevel = LogLevel.Debug,
                            IsEnabled = true,
                            UseAsyncWrite = false,
                            Properties = new Dictionary<string, object>
                            {
                                ["MaxEntries"] = 1000
                            }
                        }
                    }.AsReadOnly()
                };
            }
            
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
                StructuredLogging = false, // Reduce allocations
                GlobalMinimumLevel = LogLevel.Warning // Reduce log volume
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
        public ILoggingService CreateLoggingServiceFromBuilder(Action<ILogConfigBuilder> builderAction)
        {
            if (builderAction == null)
                throw new ArgumentNullException(nameof(builderAction));

            var builder = new LogConfigBuilder();
            builderAction(builder);
            var config = builder.Build();

            return CreateLoggingService(config);
        }

        /// <inheritdoc />
        public ILoggingService CreateLoggingServiceForScenario(
            LoggingScenario scenario, 
            Action<ILogConfigBuilder> customizations = null)
        {
            var builder = new LogConfigBuilder();
            
            // Apply scenario-specific configuration
            switch (scenario)
            {
                case LoggingScenario.Production:
                    builder.ForProduction();
                    break;
                case LoggingScenario.Development:
                    builder.ForDevelopment();
                    break;
                case LoggingScenario.Testing:
                    builder.ForTesting();
                    break;
                case LoggingScenario.Staging:
                    builder.ForStaging();
                    break;
                case LoggingScenario.HighAvailability:
                    builder.ForHighAvailability();
                    break;
                case LoggingScenario.CloudDeployment:
                    builder.ForCloudDeployment();
                    break;
                case LoggingScenario.Mobile:
                    builder.ForMobile();
                    break;
                case LoggingScenario.PerformanceTesting:
                    builder.ForPerformanceTesting();
                    break;
                default:
                    throw new ArgumentException($"Unknown logging scenario: {scenario}", nameof(scenario));
            }

            // Apply any custom modifications
            customizations?.Invoke(builder);

            var config = builder.Build();
            return CreateLoggingService(config);
        }

        /// <inheritdoc />
        public ILoggingService CreateMinimalLoggingService()
        {
            var config = new LogConfigBuilder()
                .WithGlobalMinimumLevel(LogLevel.Info)
                .WithConsoleTarget("Fallback", LogLevel.Info)
                .Build();

            return CreateLoggingService(config);
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

            // Validate target types are registered
            foreach (var targetConfig in config.TargetConfigs)
            {
                if (!_targetFactory.IsTargetTypeRegistered(targetConfig.TargetType))
                {
                    var availableTypes = string.Join(", ", _targetFactory.GetRegisteredTargetTypes());
                    errors.Add($"Target type '{targetConfig.TargetType}' is not registered. Available types: {availableTypes}");
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

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> GetAvailableTargetTypes()
        {
            var targetTypes = _targetFactory.GetRegisteredTargetTypes();
            var descriptions = new Dictionary<string, string>();

            foreach (var targetType in targetTypes)
            {
                descriptions[targetType] = GetTargetTypeDescription(targetType);
            }

            return new ReadOnlyDictionary<string, string>(descriptions);
        }

        /// <summary>
        /// Creates a logging service with enhanced error handling and diagnostic capabilities.
        /// </summary>
        /// <param name="builderAction">Action to configure the logging builder</param>
        /// <param name="enableDiagnostics">Whether to enable detailed diagnostics</param>
        /// <returns>A logging service with diagnostic information</returns>
        public ILoggingService CreateLoggingServiceWithDiagnostics(
            Action<ILogConfigBuilder> builderAction, 
            bool enableDiagnostics = true)
        {
            if (builderAction == null)
                throw new ArgumentNullException(nameof(builderAction));

            var builder = new LogConfigBuilder();
            builderAction(builder);

            // Add diagnostic information if enabled
            if (enableDiagnostics)
            {
                builder.WithChannel("Diagnostics", LogLevel.Debug, true);
            }

            var config = builder.Build();
            var service = CreateLoggingService(config);

            if (enableDiagnostics)
            {
                // Log diagnostic information
                var targetCount = config.TargetConfigs.Count;
                var channelCount = config.ChannelConfigs.Count;
                var availableTargets = string.Join(", ", GetAvailableTargetTypes().Keys);

                service.LogInfo($"Logging service created with {targetCount} targets, {channelCount} channels", "Diagnostics");
                service.LogInfo($"Available target types: {availableTargets}", "Diagnostics");
                service.LogInfo($"High performance mode: {config.HighPerformanceMode}", "Diagnostics");
                service.LogInfo($"Batching enabled: {config.BatchingEnabled}", "Diagnostics");
            }

            return service;
        }

        /// <summary>
        /// Creates multiple logging services for different scenarios in one call.
        /// Useful for applications that need different logging configurations for different subsystems.
        /// </summary>
        /// <param name="scenarioConfigurations">Dictionary of scenario names to configurations</param>
        /// <returns>Dictionary of scenario names to logging services</returns>
        public IReadOnlyDictionary<string, ILoggingService> CreateMultipleLoggingServices(
            IReadOnlyDictionary<string, Action<ILogConfigBuilder>> scenarioConfigurations)
        {
            if (scenarioConfigurations == null)
                throw new ArgumentNullException(nameof(scenarioConfigurations));

            var services = new Dictionary<string, ILoggingService>();
            var errors = new List<string>();

            foreach (var kvp in scenarioConfigurations)
            {
                try
                {
                    var service = CreateLoggingServiceFromBuilder(kvp.Value);
                    services[kvp.Key] = service;
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to create service for scenario '{kvp.Key}': {ex.Message}");
                }
            }

            if (errors.Count > 0 && services.Count == 0)
            {
                throw new InvalidOperationException($"Failed to create any logging services: {string.Join("; ", errors)}");
            }

            return new ReadOnlyDictionary<string, ILoggingService>(services);
        }

        /// <summary>
        /// Creates a logging service optimized for specific performance characteristics.
        /// </summary>
        /// <param name="performanceProfile">The performance optimization profile</param>
        /// <param name="customizations">Optional additional customizations</param>
        /// <returns>A performance-optimized logging service</returns>
        public ILoggingService CreateOptimizedLoggingService(
            PerformanceProfile performanceProfile,
            Action<ILogConfigBuilder> customizations = null)
        {
            var builder = new LogConfigBuilder();

            switch (performanceProfile)
            {
                case PerformanceProfile.MaximumThroughput:
                    builder.WithGlobalMinimumLevel(LogLevel.Warning)
                           .WithHighPerformanceMode(true)
                           .WithBurstCompatibility(true)
                           .WithBatching(true, 1000)
                           .WithCaching(true, 10000)
                           .WithStructuredLogging(false)
                           .WithMemoryTarget("HighThroughput", 50000, LogLevel.Warning);
                    break;

                case PerformanceProfile.LowLatency:
                    builder.WithGlobalMinimumLevel(LogLevel.Info)
                           .WithHighPerformanceMode(true)
                           .WithBurstCompatibility(true)
                           .WithBatching(false) // Immediate processing
                           .WithCaching(false)  // No caching overhead
                           .WithConsoleTarget("LowLatency", LogLevel.Info);
                    break;

                case PerformanceProfile.MinimalMemory:
                    builder.WithGlobalMinimumLevel(LogLevel.Error)
                           .WithHighPerformanceMode(true)
                           .WithBatching(true, 50)
                           .WithCaching(true, 100)
                           .WithStructuredLogging(false)
                           .WithNullTarget("Minimal");
                    break;

                case PerformanceProfile.Balanced:
                    builder.WithGlobalMinimumLevel(LogLevel.Info)
                           .WithHighPerformanceMode(true)
                           .WithBatching(true, 200)
                           .WithCaching(true, 1000)
                           .WithMemoryTarget("Balanced", 5000, LogLevel.Info)
                           .WithConsoleTarget("BalancedConsole", LogLevel.Warning);
                    break;

                default:
                    throw new ArgumentException($"Unknown performance profile: {performanceProfile}", nameof(performanceProfile));
            }

            // Apply any custom modifications
            customizations?.Invoke(builder);

            var config = builder.Build();
            return CreateLoggingService(config);
        }

        /// <summary>
        /// Internal method for creating logging service instances with comprehensive error handling.
        /// </summary>
        /// <param name="config">The validated logging configuration</param>
        /// <param name="targetFactory">The target factory to use</param>
        /// <returns>A fully configured logging service instance</returns>
        private ILoggingService CreateLoggingServiceInternal(LoggingConfig config, ILogTargetFactory targetFactory)
        {
            try
            {
                // Create log targets from configuration using the target factory
                var targets = new List<ILogTarget>();
                var failedTargets = new List<string>();
                var createdTargetTypes = new HashSet<string>();

                foreach (var targetConfig in config.TargetConfigs.Where(t => t.IsEnabled))
                {
                    try
                    {
                        var target = targetFactory.CreateTarget(targetConfig);
                        targets.Add(target);
                        createdTargetTypes.Add(targetConfig.TargetType);
                    }
                    catch (Exception ex)
                    {
                        failedTargets.Add($"{targetConfig.Name} ({targetConfig.TargetType}): {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Failed to create target '{targetConfig.Name}': {ex.Message}");
                    }
                }

                // Ensure we have at least one target (graceful degradation)
                if (targets.Count == 0)
                {
                    try
                    {
                        var fallbackTarget = targetFactory.CreateDefaultTarget();
                        targets.Add(fallbackTarget);
                        System.Diagnostics.Debug.WriteLine("No targets created successfully, using fallback target");
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to create any log targets. Errors: {string.Join("; ", failedTargets)}. " +
                            $"Fallback target creation also failed: {ex.Message}");
                    }
                }

                // Create the logging service with all dependencies
                var loggingService = new LoggingService(
                    config,
                    targets,
                    _formattingService,
                    _batchingService,
                    healthCheckService: null, // Will be injected during bootstrap
                    alertService: null, // Will be injected during bootstrap
                    profilerService: null); // Will be injected during bootstrap

                // Log creation summary and any failures
                var successCount = targets.Count;
                var failureCount = failedTargets.Count;
                var totalAttempted = successCount + failureCount;

                if (failedTargets.Count > 0)
                {
                    var failureMessage = $"Logging service created with {successCount}/{totalAttempted} targets. " +
                                       $"Failed targets: {string.Join(", ", failedTargets)}";
                    loggingService.LogWarning(failureMessage, "Logging.Initialization", "LoggingServiceFactory");
                }
                else
                {
                    var successMessage = $"Logging service successfully created with {successCount} targets: " +
                                       $"{string.Join(", ", createdTargetTypes)}";
                    loggingService.LogInfo(successMessage, "Logging.Initialization", "LoggingServiceFactory");
                }

                return loggingService;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create logging service: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets a human-readable description for a target type.
        /// </summary>
        /// <param name="targetType">The target type</param>
        /// <returns>A description of the target type</returns>
        private static string GetTargetTypeDescription(string targetType)
        {
            return targetType switch
            {
                "Memory" => "In-memory circular buffer for debugging and testing",
                "File" => "File-based logging with configurable rotation",
                "Console" => "Standard console output (cross-platform)",
                "UnityConsole" => "Unity Debug.Log integration with color coding",
                "Serilog" => "Enterprise-grade structured logging with Serilog",
                "Null" => "Null target for testing or disabled scenarios",
                "Network" => "Remote logging to network endpoints",
                "Database" => "Structured log storage in databases",
                "Email" => "Critical alert notifications via email",
                "RollingFile" => "File logging with automatic rotation",
                _ => $"Custom target type: {targetType}"
            };
        }
    }
}