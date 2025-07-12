using System.Collections.Generic;
using AhBearStudios.Core.Logging.Configs;

namespace AhBearStudios.Core.Logging.Factories
{
    /// <summary>
    /// Factory interface for creating logging service instances from configuration.
    /// Completes the Builder → Config → Factory → Service pattern for the logging system.
    /// </summary>
    public interface ILoggingServiceFactory
    {
        /// <summary>
        /// Creates a logging service instance from the provided configuration.
        /// </summary>
        /// <param name="config">The logging configuration</param>
        /// <returns>A fully configured logging service instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
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
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        ILoggingService CreateHighPerformanceLoggingService(LoggingConfig config);

        /// <summary>
        /// Creates a logging service instance optimized for development/debugging scenarios.
        /// </summary>
        /// <param name="config">The logging configuration</param>
        /// <returns>A development-optimized logging service instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        ILoggingService CreateDevelopmentLoggingService(LoggingConfig config);

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
        /// <exception cref="ArgumentNullException">Thrown when config or targetFactory is null</exception>
        ILoggingService CreateLoggingService(LoggingConfig config, ILogTargetFactory targetFactory);
    }
}