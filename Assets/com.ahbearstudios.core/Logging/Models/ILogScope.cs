using System;
using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Interface for log scopes that provide contextual logging boundaries.
    /// Scopes automatically add context to all log messages within their lifetime.
    /// </summary>
    public interface ILogScope : IDisposable
    {
        /// <summary>
        /// Scope name for identification.
        /// </summary>
        FixedString64Bytes Name { get; }

        /// <summary>
        /// Correlation ID for this scope.
        /// </summary>
        FixedString64Bytes CorrelationId { get; }

        /// <summary>
        /// Source context for this scope.
        /// </summary>
        string SourceContext { get; }

        /// <summary>
        /// Elapsed time since scope creation.
        /// </summary>
        TimeSpan Elapsed { get; }

        /// <summary>
        /// Whether the scope is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Parent scope (if any).
        /// </summary>
        ILogScope Parent { get; }

        /// <summary>
        /// Child scopes created within this scope.
        /// </summary>
        IReadOnlyCollection<ILogScope> Children { get; }

        /// <summary>
        /// Creates a child scope within this scope.
        /// </summary>
        /// <param name="childName">Name of the child scope</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Child logging scope</returns>
        ILogScope BeginChild(string childName, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Adds a property to this scope's context.
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Property value</param>
        void SetProperty(string key, object value);

        /// <summary>
        /// Gets a property from this scope's context.
        /// </summary>
        /// <param name="key">Property key</param>
        /// <returns>Property value or null if not found</returns>
        object GetProperty(string key);

        /// <summary>
        /// Gets all properties in this scope's context.
        /// </summary>
        /// <returns>Read-only dictionary of properties</returns>
        IReadOnlyDictionary<string, object> GetAllProperties();

        /// <summary>
        /// Logs a debug message within this scope.
        /// </summary>
        /// <param name="message">The message to log</param>
        void LogDebug(string message);

        /// <summary>
        /// Logs an informational message within this scope.
        /// </summary>
        /// <param name="message">The message to log</param>
        void LogInfo(string message);

        /// <summary>
        /// Logs a warning message within this scope.
        /// </summary>
        /// <param name="message">The message to log</param>
        void LogWarning(string message);

        /// <summary>
        /// Logs an error message within this scope.
        /// </summary>
        /// <param name="message">The message to log</param>
        void LogError(string message);

        /// <summary>
        /// Logs a critical message within this scope.
        /// </summary>
        /// <param name="message">The message to log</param>
        void LogCritical(string message);

        /// <summary>
        /// Logs an exception within this scope.
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="message">Additional context message</param>
        void LogException(Exception exception, string message = null);
    }
}