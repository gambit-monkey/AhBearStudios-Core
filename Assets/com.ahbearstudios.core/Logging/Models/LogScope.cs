using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using System.Linq;
using Unity.Collections;
using AhBearStudios.Core.Logging.Services;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Implementation of a logging scope that provides contextual logging boundaries.
    /// Automatically adds context to all log messages within its lifetime.
    /// </summary>
    public sealed class LogScope : ILogScope
    {
        private readonly ILoggingService _loggingService;
        private readonly LogContextService _contextService;
        private readonly ConcurrentDictionary<string, object> _properties;
        private readonly ConcurrentBag<ILogScope> _children;
        private readonly DateTime _createdAt;
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

        /// <inheritdoc />
        public FixedString64Bytes Name { get; }

        /// <inheritdoc />
        public FixedString64Bytes CorrelationId { get; private set; }

        /// <inheritdoc />
        public string SourceContext { get; }

        /// <inheritdoc />
        public TimeSpan Elapsed => DateTime.UtcNow - _createdAt;

        /// <inheritdoc />
        public bool IsActive => !_disposed;

        /// <inheritdoc />
        public ILogScope Parent { get; }

        /// <inheritdoc />
        public IReadOnlyCollection<ILogScope> Children => _children.ToList().AsReadOnly();

        /// <summary>
        /// Gets the timestamp when this scope was created.
        /// </summary>
        public DateTime CreatedAt => _createdAt;

        /// <summary>
        /// Initializes a new instance of the LogScope.
        /// </summary>
        /// <param name="loggingService">The logging service to use for logging</param>
        /// <param name="scopeName">The name of the scope</param>
        /// <param name="correlationId">The correlation ID for the scope</param>
        /// <param name="sourceContext">The source context for the scope</param>
        /// <param name="properties">Initial properties for the scope</param>
        internal LogScope(
            ILoggingService loggingService,
            string scopeName,
            string correlationId = null,
            string sourceContext = null,
            IReadOnlyDictionary<string, object> properties = null)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            
            if (string.IsNullOrEmpty(scopeName))
                throw new ArgumentNullException(nameof(scopeName));
                
            ScopeId = Guid.NewGuid().ToString("N")[..8]; // Short scope ID
            Name = new FixedString64Bytes(scopeName);
            CorrelationId = string.IsNullOrEmpty(correlationId) 
                ? new FixedString64Bytes(Guid.NewGuid().ToString("N")[..8])
                : new FixedString64Bytes(correlationId);
            SourceContext = sourceContext ?? "LogScope";
            _createdAt = DateTime.UtcNow;
            Parent = _currentScope.Value;
            
            _properties = new ConcurrentDictionary<string, object>();
            _children = new ConcurrentBag<ILogScope>();
            
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
        /// Initializes a new instance of the LogScope for context-only scenarios.
        /// Used when only context tracking is needed without direct logging service access.
        /// </summary>
        /// <param name="contextService">The context service to use for context management</param>
        /// <param name="context">The log context to associate with this scope</param>
        internal LogScope(LogContextService contextService, LogContext context)
        {
            _contextService = contextService ?? throw new ArgumentNullException(nameof(contextService));
            
            if (context.Equals(LogContext.Empty))
                throw new ArgumentException("Context cannot be empty", nameof(context));
                
            ScopeId = Guid.NewGuid().ToString("N")[..8]; // Short scope ID
            Name = new FixedString64Bytes(context.Operation ?? "ContextScope");
            CorrelationId = context.CorrelationId;
            SourceContext = context.SourceContext ?? "LogContextService";
            _createdAt = DateTime.UtcNow;
            Parent = _currentScope.Value;
            
            _properties = new ConcurrentDictionary<string, object>();
            _children = new ConcurrentBag<ILogScope>();
            
            // Copy context properties
            if (context.Properties != null)
            {
                foreach (var kvp in context.Properties)
                {
                    _properties[kvp.Key] = kvp.Value;
                }
            }

            // Set this as the current scope for the thread
            _currentScope.Value = this;
        }

        /// <inheritdoc />
        public ILogScope BeginChild(string childName, FixedString64Bytes correlationId = default)
        {
            if (string.IsNullOrEmpty(childName))
                throw new ArgumentNullException(nameof(childName));
                
            var childCorrelationId = correlationId.IsEmpty 
                ? Guid.NewGuid().ToString("N")[..8] 
                : correlationId.ToString();
                
            var child = new LogScope(_loggingService, childName, childCorrelationId, SourceContext);
            _children.Add(child);
            return child;
        }

        /// <inheritdoc />
        public void SetProperty(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
                
            _properties[key] = value;
        }

        /// <inheritdoc />
        public object GetProperty(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
                
            _properties.TryGetValue(key, out var value);
            return value;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> GetAllProperties()
        {
            return _properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }


        /// <inheritdoc />
        public void LogDebug(string message)
        {
            if (_disposed) return;
            if (_loggingService != null)
            {
                _loggingService.LogDebug(message, CorrelationId, SourceContext);
            }
            // Context-only scopes don't directly log - they only provide context
        }

        /// <inheritdoc />
        public void LogInfo(string message)
        {
            if (_disposed) return;
            if (_loggingService != null)
            {
                _loggingService.LogInfo(message, CorrelationId, SourceContext);
            }
            // Context-only scopes don't directly log - they only provide context
        }

        /// <inheritdoc />
        public void LogWarning(string message)
        {
            if (_disposed) return;
            if (_loggingService != null)
            {
                _loggingService.LogWarning(message, CorrelationId, SourceContext);
            }
            // Context-only scopes don't directly log - they only provide context
        }

        /// <inheritdoc />
        public void LogError(string message)
        {
            if (_disposed) return;
            if (_loggingService != null)
            {
                _loggingService.LogError(message, CorrelationId, SourceContext);
            }
            // Context-only scopes don't directly log - they only provide context
        }

        /// <inheritdoc />
        public void LogCritical(string message)
        {
            if (_disposed) return;
            if (_loggingService != null)
            {
                _loggingService.LogCritical(message, CorrelationId, SourceContext);
            }
            // Context-only scopes don't directly log - they only provide context
        }

        /// <inheritdoc />
        public void LogException(Exception exception, string message = null)
        {
            if (_disposed) return;
            if (_loggingService != null)
            {
                _loggingService.LogException(message, exception, CorrelationId, SourceContext);
            }
            // Context-only scopes don't directly log - they only provide context
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

                if (_loggingService != null)
                {
                    _loggingService.LogDebug($"Scope '{Name}' completed", CorrelationId, SourceContext, scopeProperties);
                }
                else if (_contextService != null)
                {
                    // For context-only scopes, pop the context
                    _contextService.PopContext();
                }
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
                ["ScopeName"] = Name.ToString(),
                ["CorrelationId"] = CorrelationId.ToString(),
                ["CreatedAt"] = CreatedAt,
                ["Duration"] = Elapsed.TotalMilliseconds
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
                context["Parent.ScopeName"] = parentScope.Name.ToString();
                context["Parent.CorrelationId"] = parentScope.CorrelationId.ToString();
            }

            return context;
        }

        /// <summary>
        /// Returns a string representation of this scope.
        /// </summary>
        /// <returns>A string representation of the scope</returns>
        public override string ToString()
        {
            return $"LogScope[{ScopeId}] '{Name}' ({Elapsed.TotalMilliseconds:F0}ms)";
        }

        /// <summary>
        /// Creates a new LogScope instance with the specified logging service and parameters.
        /// </summary>
        /// <param name="loggingService">The logging service to use for logging</param>
        /// <param name="scopeName">The name of the scope</param>
        /// <param name="correlationId">The correlation ID for the scope</param>
        /// <param name="sourceContext">The source context for the scope</param>
        /// <param name="properties">Initial properties for the scope</param>
        /// <returns>A new LogScope instance</returns>
        public static LogScope Create(
            ILoggingService loggingService,
            string scopeName,
            string correlationId = null,
            string sourceContext = null,
            IReadOnlyDictionary<string, object> properties = null)
        {
            return new LogScope(loggingService, scopeName, correlationId, sourceContext, properties);
        }

        /// <summary>
        /// Creates a new LogScope instance for context-only scenarios.
        /// </summary>
        /// <param name="contextService">The context service to use for context management</param>
        /// <param name="context">The log context to associate with this scope</param>
        /// <returns>A new LogScope instance optimized for context tracking</returns>
        public static LogScope ForContext(LogContextService contextService, LogContext context)
        {
            return new LogScope(contextService, context);
        }

        /// <summary>
        /// Creates a child scope from an existing parent scope.
        /// </summary>
        /// <param name="parent">The parent scope</param>
        /// <param name="childName">The name of the child scope</param>
        /// <param name="correlationId">Optional correlation ID for the child scope</param>
        /// <returns>A new child LogScope instance</returns>
        public static LogScope ForChild(LogScope parent, string childName, FixedString64Bytes correlationId = default)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            return parent.BeginChild(childName, correlationId) as LogScope;
        }

        /// <summary>
        /// Creates a new LogScope instance for a specific operation.
        /// </summary>
        /// <param name="loggingService">The logging service to use for logging</param>
        /// <param name="operation">The operation name</param>
        /// <param name="correlationId">Optional correlation ID for the operation</param>
        /// <param name="properties">Optional properties for the operation</param>
        /// <returns>A new LogScope instance optimized for operation tracking</returns>
        public static LogScope ForOperation(
            ILoggingService loggingService,
            string operation,
            FixedString64Bytes correlationId = default,
            IReadOnlyDictionary<string, object> properties = null)
        {
            var correlationIdStr = correlationId.IsEmpty 
                ? Guid.NewGuid().ToString("N")[..8] 
                : correlationId.ToString();

            var operationProperties = new Dictionary<string, object>
            {
                ["OperationType"] = "Operation",
                ["StartTime"] = DateTime.UtcNow
            };

            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    operationProperties[kvp.Key] = kvp.Value;
                }
            }

            return new LogScope(loggingService, operation, correlationIdStr, "Operation", operationProperties);
        }

        /// <summary>
        /// Creates a new LogScope instance for background operations.
        /// </summary>
        /// <param name="loggingService">The logging service to use for logging</param>
        /// <param name="backgroundOperation">The background operation name</param>
        /// <param name="correlationId">Optional correlation ID for the background operation</param>
        /// <returns>A new LogScope instance optimized for background operation tracking</returns>
        public static LogScope ForBackgroundOperation(
            ILoggingService loggingService,
            string backgroundOperation,
            FixedString64Bytes correlationId = default)
        {
            var correlationIdStr = correlationId.IsEmpty 
                ? Guid.NewGuid().ToString("N")[..8] 
                : correlationId.ToString();

            var backgroundProperties = new Dictionary<string, object>
            {
                ["OperationType"] = "Background",
                ["StartTime"] = DateTime.UtcNow,
                ["ThreadId"] = Thread.CurrentThread.ManagedThreadId
            };

            return new LogScope(loggingService, backgroundOperation, correlationIdStr, "Background", backgroundProperties);
        }

        /// <summary>
        /// Creates a new LogScope instance for request operations.
        /// </summary>
        /// <param name="loggingService">The logging service to use for logging</param>
        /// <param name="requestId">The request identifier</param>
        /// <param name="operation">The operation name</param>
        /// <param name="userId">Optional user identifier</param>
        /// <returns>A new LogScope instance optimized for request tracking</returns>
        public static LogScope ForRequest(
            ILoggingService loggingService,
            string requestId,
            string operation,
            string userId = null)
        {
            if (string.IsNullOrEmpty(requestId))
                throw new ArgumentNullException(nameof(requestId));

            var requestProperties = new Dictionary<string, object>
            {
                ["RequestId"] = requestId,
                ["OperationType"] = "Request",
                ["StartTime"] = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(userId))
            {
                requestProperties["UserId"] = userId;
            }

            return new LogScope(loggingService, operation, requestId, "Request", requestProperties);
        }
    }
}