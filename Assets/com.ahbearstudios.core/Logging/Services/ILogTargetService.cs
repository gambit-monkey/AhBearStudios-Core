using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Logging.Targets;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Logging.Services
{
    /// <summary>
    /// Service interface for managing log targets in the logging system.
    /// Handles target registration, configuration, health monitoring, and lifecycle management.
    /// Follows the AhBearStudios Core Architecture patterns for service decomposition.
    /// </summary>
    public interface ILogTargetService : IDisposable
    {
        #region Target Registration and Management

        /// <summary>
        /// Registers a log target with the target service.
        /// </summary>
        /// <param name="target">The log target to register</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <exception cref="ArgumentNullException">Thrown when target is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown when service is disposed</exception>
        void RegisterTarget(ILogTarget target, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Unregisters a log target by name and disposes it properly.
        /// </summary>
        /// <param name="targetName">The name of the target to unregister</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>True if the target was found and unregistered successfully</returns>
        bool UnregisterTarget(string targetName, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Gets a registered target by name.
        /// </summary>
        /// <param name="targetName">The name of the target to retrieve</param>
        /// <returns>The target instance, or null if not found</returns>
        ILogTarget GetTarget(string targetName);

        /// <summary>
        /// Checks if a target with the specified name is registered.
        /// </summary>
        /// <param name="targetName">The name of the target to check</param>
        /// <returns>True if the target is registered</returns>
        bool HasTarget(string targetName);

        /// <summary>
        /// Gets all registered targets as a read-only collection.
        /// </summary>
        /// <returns>Read-only collection of all registered targets</returns>
        IReadOnlyCollection<ILogTarget> GetTargets();

        #endregion

        #region Target Configuration

        /// <summary>
        /// Sets the minimum log level for all registered targets.
        /// </summary>
        /// <param name="minimumLevel">The minimum level to set</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        void SetMinimumLevel(LogLevel minimumLevel, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Sets the minimum log level for a specific target.
        /// </summary>
        /// <param name="targetName">The name of the target to configure</param>
        /// <param name="minimumLevel">The minimum level to set</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>True if the target was found and configured successfully</returns>
        bool SetMinimumLevel(string targetName, LogLevel minimumLevel, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Sets the enabled state for all registered targets.
        /// </summary>
        /// <param name="enabled">Whether targets should be enabled</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        void SetEnabled(bool enabled, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Sets the enabled state for a specific target.
        /// </summary>
        /// <param name="targetName">The name of the target to configure</param>
        /// <param name="enabled">Whether the target should be enabled</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>True if the target was found and configured successfully</returns>
        bool SetEnabled(string targetName, bool enabled, FixedString64Bytes correlationId = default);

        #endregion

        #region Target Operations

        /// <summary>
        /// Writes a log message to all appropriate targets.
        /// </summary>
        /// <param name="logMessage">The log message to write</param>
        void WriteToTargets(LogMessage logMessage);

        /// <summary>
        /// Flushes all registered targets.
        /// </summary>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        void FlushAll(FixedString64Bytes correlationId = default);

        /// <summary>
        /// Flushes a specific target by name.
        /// </summary>
        /// <param name="targetName">The name of the target to flush</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>True if the target was found and flushed successfully</returns>
        bool Flush(string targetName, FixedString64Bytes correlationId = default);

        #endregion

        #region Health Monitoring

        /// <summary>
        /// Performs health checks on all registered targets.
        /// </summary>
        /// <returns>True if all targets are healthy</returns>
        bool PerformHealthCheck();

        /// <summary>
        /// Gets the health status of all registered targets.
        /// </summary>
        /// <returns>Dictionary mapping target names to their health status</returns>
        IReadOnlyDictionary<string, bool> GetHealthStatus();

        /// <summary>
        /// Validates the current configuration of all targets.
        /// </summary>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>Validation result with any errors or warnings</returns>
        ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default);

        #endregion
    }
}