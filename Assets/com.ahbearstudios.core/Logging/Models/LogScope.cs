using System;
using System.Collections.Generic;
using System.Threading;

namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Implementation of a logging scope that provides contextual logging boundaries.
    /// Automatically adds context to all log messages within its lifetime.
    /// </summary>
    public sealed class LogScope : ILogScope
    {
        private readonly ILoggingService _loggingService;
        private readonly Dictionary<string, object> _properties;
        private volatile bool _disposed;
        private static readonly ThreadLocal<LogScope> _currentScope = new ThreadLocal<LogScope>();

        /// <summary>
        /// Gets the current active scope for this thread.
        /// </summary>
        public static LogScope Current => _currentScope.Value;

        /// <summary>
        /// Gets the unique identifier for this scope.
        /// </summary>
        public string ScopeId { get; }

        /// <summary>
        /// Gets the name of this scope.
        /// </summary>
        public string ScopeName { get; }

        /// <summary>
        /// Gets the correlation ID associated with this scope.
        /// </summary>
        public string CorrelationId { get; private set; }

        /// <summary>
        /// Gets the timestamp when this scope was created.
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Gets the properties associated with this scope.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties => _properties;

        /// <summary>
        /// Gets the parent scope, if any.
        /// </summary>
        public ILogScope Parent { get; }

        /// <summary>
        /// Gets whether this scope is still active.
        /// </summary>
        public bool IsActive => !_disposed;

        /// <summary>
        /// Initializes a new instance of the LogScope.
        /// </summary>
        /// <param name="loggingService">The logging service to use for logging</param>
        /// <param name="scopeName">The name of the scope</param>
        /// <param name="correlationId">The correlation ID for the scope</param>
        /// <param name="properties">Initial properties for the scope</param>
        internal LogScope(
            ILoggingService loggingService,
            string scopeName,
            string correlationId = null,
            IReadOnlyDictionary<string, object> properties = null)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            ScopeName = scopeName ?? throw new ArgumentNullException(nameof(scopeName));
            ScopeId = Guid.NewGuid().ToString("N")[..8]; // Short scope ID
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N");
            CreatedAt = DateTime.UtcNow;
            Parent = _currentScope.Value;
            
            _properties = new Dictionary<string, object>();
            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    _properties[kvp.Key] = kvp.Value;
                }
            }

            // Set this as the current scope for the thread
            _currentScope.Value = this;
        }

        /// <summary>
        /// Adds a property to this scope.
        /// </summary>
        /// <param name="key">The property key</param>
        /// <param name="value">The property value</param>
        /// <returns>This scope for method chaining</returns>
        public ILogScope WithProperty(string key, object value)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LogScope));

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            _properties[key] = value;
            return this;
        }

        /// <summary>
        /// Sets the correlation ID for this scope.
        /// </summary>
        /// <param name="correlationId">The correlation ID</param>
        /// <returns>This scope for method chaining</returns>
        public ILogScope WithCorrelationId(string correlationId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LogScope));

            CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
            return this;
        }

        /// <summary>
        /// Logs a debug message within this scope.
        /// </summary>
        /// <param name="message">The message to log</param>
        public void LogDebug(string message)
        {
            if (_disposed) return;
            _loggingService.LogDebug(message, CorrelationId, ScopeName);
        }

        /// <summary>
        /// Logs an informational message within this scope.
        /// </summary>
        /// <param name="message">The message to log</param>
        public void LogInfo(string message)
        {
            if (_disposed) return;
            _loggingService.LogInfo(message, CorrelationId, ScopeName);
        }

        /// <summary>
        /// Logs a warning message within this scope.
        /// </summary>
        /// <param name="message">The message to log</param>
        public void LogWarning(string message)
        {
            if (_disposed) return;
            _loggingService.LogWarning(message, CorrelationId, ScopeName);
        }

        /// <summary>
        /// Logs an error message within this scope.
        /// </summary>
        /// <param name="message">The message to log</param>
        public void LogError(string message)
        {
            if (_disposed) return;
            _loggingService.LogError(message, CorrelationId, ScopeName);
        }

        /// <summary>
        /// Logs a critical message within this scope.
        /// </summary>
        /// <param name="message">The message to log</param>
        public void LogCritical(string message)
        {
            if (_disposed) return;
            _loggingService.LogCritical(message, CorrelationId, ScopeName);
        }

        /// <summary>
        /// Logs an exception within this scope.
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="message">Additional context message</param>
        public void LogException(Exception exception, string message = null)
        {
            if (_disposed) return;
            _loggingService.LogException(exception, message, CorrelationId, ScopeName);
        }

        /// <summary>
        /// Disposes the scope and restores the previous scope.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                // Log scope completion
                var duration = DateTime.UtcNow - CreatedAt;
                var scopeProperties = new Dictionary<string, object>(_properties)
                {
                    ["ScopeId"] = ScopeId,
                    ["Duration"] = duration.TotalMilliseconds
                };

                _loggingService.LogDebug($"Scope '{ScopeName}' completed", scopeProperties);
            }
            catch
            {
                // Avoid throwing exceptions in Dispose
            }
            finally
            {
                _disposed = true;
                
                // Restore parent scope as current
                _currentScope.Value = Parent as LogScope;
            }
        }

        /// <summary>
        /// Gets the full context information for this scope including parent scopes.
        /// </summary>
        /// <returns>A dictionary containing the full scope context</returns>
        public Dictionary<string, object> GetFullContext()
        {
            var context = new Dictionary<string, object>
            {
                ["ScopeId"] = ScopeId,
                ["ScopeName"] = ScopeName,
                ["CorrelationId"] = CorrelationId,
                ["CreatedAt"] = CreatedAt,
                ["Duration"] = (DateTime.UtcNow - CreatedAt).TotalMilliseconds
            };

            // Add scope properties
            foreach (var kvp in _properties)
            {
                context[$"Scope.{kvp.Key}"] = kvp.Value;
            }

            // Add parent scope context
            if (Parent is LogScope parentScope)
            {
                context["Parent.ScopeId"] = parentScope.ScopeId;
                context["Parent.ScopeName"] = parentScope.ScopeName;
                context["Parent.CorrelationId"] = parentScope.CorrelationId;
            }

            return context;
        }

        /// <summary>
        /// Returns a string representation of this scope.
        /// </summary>
        /// <returns>A string representation of the scope</returns>
        public override string ToString()
        {
            var duration = DateTime.UtcNow - CreatedAt;
            return $"LogScope[{ScopeId}] '{ScopeName}' ({duration.TotalMilliseconds:F0}ms)";
        }
    }
}