using System.Collections.Generic;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Builders;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Factories
{
    /// <summary>
    /// Factory interface for creating logging service instances from configuration.
    /// Provides cohesive integration with ILogConfigBuilder and LogConfigBuilder patterns.
    /// Completes the Builder → ConfigSo → Factory → Service pattern for the logging system.
    /// </summary>
    public interface ILoggingServiceFactory
    {
        /// <summary>
        /// Creates a logging service instance from the provided configuration.
        /// </summary>
        /// <param name="config">The logging configuration</param>
        /// <returns>A fully configured logging service instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when configSo is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        ILoggingService CreateLoggingService(LoggingConfig config);

        /// <summary>
        /// Creates a logging service instance using default configuration.
        /// </summary>
        /// <returns>A logging service instance with default settings</returns>
        ILoggingService CreateDefaultLoggingService();

        /// <summary>
        /// Creates a logging service instance optimized for high-performance scenarios.
        /// </summary>
        /// <param name="config">The logging configuration</param>
        /// <returns>A high-performance logging service instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when configSo is null</exception>
        ILoggingService CreateHighPerformanceLoggingService(LoggingConfig config);

        /// <summary>
        /// Creates a logging service instance optimized for development/debugging scenarios.
        /// </summary>
        /// <param name="config">The logging configuration</param>
        /// <returns>A development-optimized logging service instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when configSo is null</exception>
        ILoggingService CreateDevelopmentLoggingService(LoggingConfig config);

        /// <summary>
        /// Creates a logging service using a fluent builder configuration.
        /// This method bridges the gap between builder patterns and factory creation.
        /// </summary>
        /// <param name="builderAction">Action to configure the logging builder</param>
        /// <returns>A fully configured logging service instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when builderAction is null</exception>
        ILoggingService CreateLoggingServiceFromBuilder(Action<ILogConfigBuilder> builderAction);

        /// <summary>
        /// Creates a logging service for a specific scenario using preset configurations.
        /// </summary>
        /// <param name="scenario">The deployment scenario</param>
        /// <param name="customizations">Optional customizations to apply</param>
        /// <returns>A logging service configured for the specified scenario</returns>
        ILoggingService CreateLoggingServiceForScenario(LoggingScenario scenario, Action<ILogConfigBuilder> customizations = null);

        /// <summary>
        /// Creates a minimal logging service with basic console output.
        /// Useful as a fallback when configuration fails.
        /// </summary>
        /// <returns>A minimal logging service with console target</returns>
        ILoggingService CreateMinimalLoggingService();

        /// <summary>
        /// Validates that a configuration can be used to create a logging service.
        /// </summary>
        /// <param name="config">The configuration to validate</param>
        /// <returns>A list of validation errors, empty if configuration is valid</returns>
        IReadOnlyList<string> ValidateConfiguration(LoggingConfig config);

        /// <summary>
        /// Creates a logging service instance with custom target factory.
        /// </summary>
        /// <param name="config">The logging configuration</param>
        /// <param name="targetFactory">Custom target factory to use</param>
        /// <returns>A logging service instance using the custom target factory</returns>
        /// <exception cref="ArgumentNullException">Thrown when configSo or targetFactory is null</exception>
        ILoggingService CreateLoggingService(LoggingConfig config, ILogTargetFactory targetFactory);

        /// <summary>
        /// Gets information about available target types and their capabilities.
        /// </summary>
        /// <returns>Dictionary mapping target types to their descriptions</returns>
        IReadOnlyDictionary<string, string> GetAvailableTargetTypes();
    }
}