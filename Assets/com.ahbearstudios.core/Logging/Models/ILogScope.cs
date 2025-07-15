using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Interface for log scopes that provide contextual logging boundaries.
    /// Scopes automatically add context to all log messages within their lifetime.
    /// </summary>
    public interface ILogScope : IDisposable
    {
        /// <summary>
        /// Gets the unique identifier for this scope.
        /// </summary>
        string ScopeId { get; }

        /// <summary>
        /// Gets the name of this scope.
        /// </summary>
        string ScopeName { get; }

        /// <summary>
        /// Gets the correlation ID associated with this scope.
        /// </summary>
        string CorrelationId { get; }

        /// <summary>
        /// Gets the timestamp when this scope was created.
        /// </summary>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Gets the properties associated with this scope.
        /// </summary>
        IReadOnlyDictionary<string, object> Properties { get; }

        /// <summary>
        /// Gets the parent scope, if any.
        /// </summary>
        ILogScope Parent { get; }

        /// <summary>
        /// Gets whether this scope is still active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Adds a property to this scope.
        /// </summary>
        /// <param name="key">The property key</param>
        /// <param name="value">The property value</param>
        /// <returns>This scope for method chaining</returns>
        ILogScope WithProperty(string key, object value);

        /// <summary>
        /// Sets the correlation ID for this scope.
        /// </summary>
        /// <param name="correlationId">The correlation ID</param>
        /// <returns>This scope for method chaining</returns>
        ILogScope WithCorrelationId(string correlationId);

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