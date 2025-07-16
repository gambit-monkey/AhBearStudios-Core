using System.Collections.Generic;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Logging.Factories
{
    /// <summary>
    /// Factory interface for creating log formatter instances.
    /// Follows the Factory pattern as specified in the AhBearStudios Core Architecture.
    /// Supports all available formatter types and integrates with IProfilerService.
    /// </summary>
    public interface ILogFormatterFactory
    {
        /// <summary>
        /// Creates a log formatter instance from the provided configuration.
        /// </summary>
        /// <param name="config">The formatter configuration</param>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        /// <returns>A new log formatter instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when formatter type is not supported</exception>
        ILogFormatter CreateFormatter(FormatterConfig config, IProfilerService profilerService = null);

        /// <summary>
        /// Creates a log formatter instance of the specified type.
        /// </summary>
        /// <param name="formatterType">The type of formatter to create (e.g., "Json", "PlainText", "Xml")</param>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        /// <returns>A new log formatter instance</returns>
        /// <exception cref="ArgumentException">Thrown when formatterType is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when formatter type is not supported</exception>
        ILogFormatter CreateFormatter(string formatterType, IProfilerService profilerService = null);

        /// <summary>
        /// Creates a log formatter instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of log formatter to create</typeparam>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        /// <returns>A new log formatter instance of the specified type</returns>
        /// <exception cref="InvalidOperationException">Thrown when formatter type cannot be created</exception>
        T CreateFormatter<T>(IProfilerService profilerService = null) where T : class, ILogFormatter;

        /// <summary>
        /// Creates multiple log formatter instances from the provided configurations.
        /// </summary>
        /// <param name="configs">The formatter configurations</param>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        /// <returns>A collection of new log formatter instances</returns>
        /// <exception cref="ArgumentNullException">Thrown when configs is null</exception>
        IReadOnlyList<ILogFormatter> CreateFormatters(IEnumerable<FormatterConfig> configs, IProfilerService profilerService = null);

        /// <summary>
        /// Creates multiple log formatter instances of the specified types.
        /// </summary>
        /// <param name="formatterTypes">The types of formatters to create</param>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        /// <returns>A collection of new log formatter instances</returns>
        /// <exception cref="ArgumentNullException">Thrown when formatterTypes is null</exception>
        IReadOnlyList<ILogFormatter> CreateFormatters(IEnumerable<string> formatterTypes, IProfilerService profilerService = null);

        /// <summary>
        /// Registers a custom log formatter type with the factory.
        /// </summary>
        /// <param name="formatterType">The string identifier for the formatter type</param>
        /// <param name="factory">The factory function to create instances of this type</param>
        /// <exception cref="ArgumentException">Thrown when formatterType is null or empty</exception>
        /// <exception cref="ArgumentNullException">Thrown when factory is null</exception>
        void RegisterFormatterType(string formatterType, Func<IProfilerService, ILogFormatter> factory);

        /// <summary>
        /// Registers a custom log formatter type with the factory using a generic type parameter.
        /// </summary>
        /// <typeparam name="T">The log formatter type to register</typeparam>
        /// <param name="formatterType">The string identifier for the formatter type</param>
        /// <exception cref="ArgumentException">Thrown when formatterType is null or empty</exception>
        void RegisterFormatterType<T>(string formatterType) where T : class, ILogFormatter;

        /// <summary>
        /// Unregisters a custom log formatter type from the factory.
        /// </summary>
        /// <param name="formatterType">The string identifier for the formatter type</param>
        /// <returns>True if the formatter type was unregistered, false if it was not found</returns>
        bool UnregisterFormatterType(string formatterType);

        /// <summary>
        /// Gets all registered formatter types.
        /// </summary>
        /// <returns>A collection of registered formatter type identifiers</returns>
        IReadOnlyList<string> GetRegisteredFormatterTypes();

        /// <summary>
        /// Determines whether a formatter type is registered with the factory.
        /// </summary>
        /// <param name="formatterType">The string identifier for the formatter type</param>
        /// <returns>True if the formatter type is registered, false otherwise</returns>
        bool IsFormatterTypeRegistered(string formatterType);

        /// <summary>
        /// Validates that a formatter configuration is valid for creation.
        /// </summary>
        /// <param name="config">The formatter configuration to validate</param>
        /// <returns>A list of validation errors, empty if configuration is valid</returns>
        IReadOnlyList<string> ValidateFormatterConfig(FormatterConfig config);

        /// <summary>
        /// Validates that a formatter type is valid for creation.
        /// </summary>
        /// <param name="formatterType">The formatter type to validate</param>
        /// <returns>A list of validation errors, empty if formatter type is valid</returns>
        IReadOnlyList<string> ValidateFormatterType(string formatterType);

        /// <summary>
        /// Creates a default log formatter suitable for the current environment.
        /// </summary>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        /// <returns>A default log formatter instance</returns>
        ILogFormatter CreateDefaultFormatter(IProfilerService profilerService = null);

        /// <summary>
        /// Gets information about available formatter types and their capabilities.
        /// </summary>
        /// <returns>Dictionary mapping formatter types to their descriptions</returns>
        IReadOnlyDictionary<string, string> GetAvailableFormatterTypes();
    }
}