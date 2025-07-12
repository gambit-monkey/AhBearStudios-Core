using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Services;
using AhBearStudios.Core.Logging.Targets;

namespace AhBearStudios.Core.Logging.Factories
{
    /// <summary>
    /// Factory implementation for creating logging service instances from configuration.
    /// Completes the Builder → Config → Factory → Service pattern for the logging system.
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