using System.Collections.Generic;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Targets;

namespace AhBearStudios.Core.Logging.Factories
{
    /// <summary>
    /// Interface for creating log target instances from configuration.
    /// Follows the Factory pattern as specified in the AhBearStudios Core Architecture.
    /// </summary>
    public interface ILogTargetFactory
    {
        /// <summary>
        /// Creates a log target instance from the provided configuration.
        /// </summary>
        /// <param name="config">The configuration for the log target</param>
        /// <returns>A new log target instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when configSo is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when target type is not supported</exception>
        ILogTarget CreateTarget(LogTargetConfig config);

        /// <summary>
        /// Creates multiple log target instances from the provided configurations.
        /// </summary>
        /// <param name="configs">The configurations for the log targets</param>
        /// <returns>A collection of new log target instances</returns>
        /// <exception cref="ArgumentNullException">Thrown when configs is null</exception>
        IReadOnlyList<ILogTarget> CreateTargets(IEnumerable<LogTargetConfig> configs);

        /// <summary>
        /// Creates a log target instance of the specified type with the given configuration.
        /// </summary>
        /// <typeparam name="T">The type of log target to create</typeparam>
        /// <param name="config">The configuration for the log target</param>
        /// <returns>A new log target instance of the specified type</returns>
        /// <exception cref="ArgumentNullException">Thrown when configSo is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when target type cannot be created</exception>
        T CreateTarget<T>(LogTargetConfig config) where T : class, ILogTarget;

        /// <summary>
        /// Registers a custom log target type with the factory.
        /// </summary>
        /// <param name="targetType">The string identifier for the target type</param>
        /// <param name="factory">The factory function to create instances of this type</param>
        /// <exception cref="ArgumentException">Thrown when targetType is null or empty</exception>
        /// <exception cref="ArgumentNullException">Thrown when factory is null</exception>
        void RegisterTargetType(string targetType, Func<LogTargetConfig, ILogTarget> factory);

        /// <summary>
        /// Registers a custom log target type with the factory using a generic type parameter.
        /// </summary>
        /// <typeparam name="T">The log target type to register</typeparam>
        /// <param name="targetType">The string identifier for the target type</param>
        /// <exception cref="ArgumentException">Thrown when targetType is null or empty</exception>
        void RegisterTargetType<T>(string targetType) where T : class, ILogTarget, new();

        /// <summary>
        /// Unregisters a custom log target type from the factory.
        /// </summary>
        /// <param name="targetType">The string identifier for the target type</param>
        /// <returns>True if the target type was unregistered, false if it was not found</returns>
        bool UnregisterTargetType(string targetType);

        /// <summary>
        /// Gets all registered target types.
        /// </summary>
        /// <returns>A collection of registered target type identifiers</returns>
        IReadOnlyList<string> GetRegisteredTargetTypes();

        /// <summary>
        /// Determines whether a target type is registered with the factory.
        /// </summary>
        /// <param name="targetType">The string identifier for the target type</param>
        /// <returns>True if the target type is registered, false otherwise</returns>
        bool IsTargetTypeRegistered(string targetType);

        /// <summary>
        /// Validates that a target configuration is valid for creation.
        /// </summary>
        /// <param name="config">The configuration to validate</param>
        /// <returns>A list of validation errors, empty if configuration is valid</returns>
        IReadOnlyList<string> ValidateTargetConfig(LogTargetConfig config);

        /// <summary>
        /// Creates a default log target suitable for the current environment.
        /// </summary>
        /// <returns>A default log target instance</returns>
        ILogTarget CreateDefaultTarget();
    }
}