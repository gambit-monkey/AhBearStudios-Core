using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Provides contextual information for log messages.
    /// Designed for high-performance scenarios with minimal allocations.
    /// Supports scoped context management and automatic context propagation.
    /// </summary>
    public readonly record struct LogContext
    {
        /// <summary>
        /// Gets the correlation ID for tracking operations across system boundaries.
        /// </summary>
        public string CorrelationId { get; }

        /// <summary>
        /// Gets the source context (typically the class name) where the message originated.
        /// </summary>
        public string SourceContext { get; }

        /// <summary>
        /// Gets the thread ID where the context was created.
        /// </summary>
        public int ThreadId { get; }

        /// <summary>
        /// Gets the timestamp when this context was created.
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Gets additional contextual properties for structured logging.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; }

        /// <summary>
        /// Gets the operation name or scope identifier.
        /// </summary>
        public string Operation { get; }

        /// <summary>
        /// Gets the user ID associated with this context, if any.
        /// </summary>
        public string UserId { get; }

        /// <summary>
        /// Gets the session ID associated with this context, if any.
        /// </summary>
        public string SessionId { get; }

        /// <summary>
        /// Gets the request ID for HTTP or service requests, if any.
        /// </summary>
        public string RequestId { get; }

        /// <summary>
        /// Gets the machine name where the context was created.
        /// </summary>
        public string MachineName { get; }

        /// <summary>
        /// Gets the application instance ID.
        /// </summary>
        public string InstanceId { get; }

        /// <summary>
        /// Initializes a new instance of the LogContext struct.
        /// </summary>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="sourceContext">The source context</param>
        /// <param name="threadId">The thread ID</param>
        /// <param name="createdAt">The creation timestamp</param>
        /// <param name="properties">Additional contextual properties</param>
        /// <param name="operation">The operation name</param>
        /// <param name="userId">The user ID</param>
        /// <param name="sessionId">The session ID</param>
        /// <param name="requestId">The request ID</param>
        /// <param name="machineName">The machine name</param>
        /// <param name="instanceId">The application instance ID</param>
        public LogContext(
            string correlationId = null,
            string sourceContext = null,
            int threadId = 0,
            DateTime createdAt = default,
            IReadOnlyDictionary<string, object> properties = null,
            string operation = null,
            string userId = null,
            string sessionId = null,
            string requestId = null,
            string machineName = null,
            string instanceId = null)
        {
            CorrelationId = correlationId ?? string.Empty;
            SourceContext = sourceContext ?? string.Empty;
            ThreadId = threadId == 0 ? Environment.CurrentManagedThreadId : threadId;
            CreatedAt = createdAt == default ? DateTime.UtcNow : createdAt;
            Properties = properties ?? EmptyProperties;
            Operation = operation ?? string.Empty;
            UserId = userId ?? string.Empty;
            SessionId = sessionId ?? string.Empty;
            RequestId = requestId ?? string.Empty;
            MachineName = machineName ?? Environment.MachineName;
            InstanceId = instanceId ?? ApplicationInstanceId;
        }

        /// <summary>
        /// Empty properties dictionary to avoid allocations.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, object> EmptyProperties = 
            new Dictionary<string, object>();

        /// <summary>
        /// Application instance ID generated at startup.
        /// </summary>
        private static readonly string ApplicationInstanceId = Guid.NewGuid().ToString("N")[..8];

        /// <summary>
        /// Thread-local storage for current context.
        /// </summary>
        private static readonly ThreadLocal<LogContext> CurrentContext = 
            new ThreadLocal<LogContext>(() => Empty);

        /// <summary>
        /// Gets an empty log context.
        /// </summary>
        public static LogContext Empty => new LogContext();

        /// <summary>
        /// Gets the current log context for the current thread.
        /// </summary>
        public static LogContext Current => CurrentContext.Value;

        /// <summary>
        /// Creates a new log context with the current timestamp and generated correlation ID.
        /// </summary>
        /// <param name="sourceContext">The source context</param>
        /// <param name="operation">The operation name</param>
        /// <param name="properties">Additional contextual properties</param>
        /// <returns>A new LogContext instance</returns>
        public static LogContext Create(
            string sourceContext = null,
            string operation = null,
            IReadOnlyDictionary<string, object> properties = null)
        {
            return new LogContext(
                correlationId: Guid.NewGuid().ToString("N"),
                sourceContext: sourceContext,
                operation: operation,
                properties: properties);
        }

        /// <summary>
        /// Creates a new log context from a method call, automatically capturing the caller information.
        /// </summary>
        /// <param name="operation">The operation name</param>
        /// <param name="properties">Additional contextual properties</param>
        /// <param name="memberName">The calling member name (automatically captured)</param>
        /// <param name="sourceFilePath">The source file path (automatically captured)</param>
        /// <param name="sourceLineNumber">The source line number (automatically captured)</param>
        /// <returns>A new LogContext instance</returns>
        public static LogContext FromCaller(
            string operation = null,
            IReadOnlyDictionary<string, object> properties = null,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string sourceFilePath = null,
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var sourceContext = memberName;
            if (!string.IsNullOrEmpty(sourceFilePath))
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
                sourceContext = $"{fileName}.{memberName}";
            }

            var contextProperties = new Dictionary<string, object>();
            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    contextProperties[kvp.Key] = kvp.Value;
                }
            }

            if (!string.IsNullOrEmpty(sourceFilePath))
            {
                contextProperties["SourceFile"] = sourceFilePath;
                contextProperties["SourceLine"] = sourceLineNumber;
            }

            return new LogContext(
                correlationId: Guid.NewGuid().ToString("N"),
                sourceContext: sourceContext,
                operation: operation ?? memberName,
                properties: contextProperties);
        }

        /// <summary>
        /// Creates a child context that inherits from the current context.
        /// </summary>
        /// <param name="operation">The child operation name</param>
        /// <param name="additionalProperties">Additional properties to merge</param>
        /// <returns>A new LogContext instance</returns>
        public static LogContext CreateChild(
            string operation = null,
            IReadOnlyDictionary<string, object> additionalProperties = null)
        {
            var current = Current;
            var mergedProperties = new Dictionary<string, object>();

            // Copy current properties
            if (current.Properties != null)
            {
                foreach (var kvp in current.Properties)
                {
                    mergedProperties[kvp.Key] = kvp.Value;
                }
            }

            // Add additional properties
            if (additionalProperties != null)
            {
                foreach (var kvp in additionalProperties)
                {
                    mergedProperties[kvp.Key] = kvp.Value;
                }
            }

            return new LogContext(
                correlationId: current.CorrelationId,
                sourceContext: current.SourceContext,
                operation: operation ?? current.Operation,
                properties: mergedProperties,
                userId: current.UserId,
                sessionId: current.SessionId,
                requestId: current.RequestId,
                machineName: current.MachineName,
                instanceId: current.InstanceId);
        }

        /// <summary>
        /// Sets the current context for the current thread.
        /// </summary>
        /// <param name="context">The context to set</param>
        /// <returns>A disposable scope that restores the previous context when disposed</returns>
        public static IDisposable SetCurrent(LogContext context)
        {
            var previous = CurrentContext.Value;
            CurrentContext.Value = context;
            return new ContextScope(previous);
        }

        /// <summary>
        /// Creates a new context with modified properties.
        /// </summary>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="sourceContext">The source context</param>
        /// <param name="operation">The operation name</param>
        /// <param name="userId">The user ID</param>
        /// <param name="sessionId">The session ID</param>
        /// <param name="requestId">The request ID</param>
        /// <param name="additionalProperties">Additional properties to merge</param>
        /// <returns>A new LogContext instance</returns>
        public LogContext WithProperties(
            string correlationId = null,
            string sourceContext = null,
            string operation = null,
            string userId = null,
            string sessionId = null,
            string requestId = null,
            IReadOnlyDictionary<string, object> additionalProperties = null)
        {
            var mergedProperties = new Dictionary<string, object>();

            // Copy existing properties
            if (Properties != null)
            {
                foreach (var kvp in Properties)
                {
                    mergedProperties[kvp.Key] = kvp.Value;
                }
            }

            // Add additional properties
            if (additionalProperties != null)
            {
                foreach (var kvp in additionalProperties)
                {
                    mergedProperties[kvp.Key] = kvp.Value;
                }
            }

            return new LogContext(
                correlationId: correlationId ?? CorrelationId,
                sourceContext: sourceContext ?? SourceContext,
                operation: operation ?? Operation,
                properties: mergedProperties,
                userId: userId ?? UserId,
                sessionId: sessionId ?? SessionId,
                requestId: requestId ?? RequestId,
                machineName: MachineName,
                instanceId: InstanceId);
        }

        /// <summary>
        /// Converts this context to a dictionary for structured logging.
        /// </summary>
        /// <returns>A dictionary representation of the context</returns>
        public Dictionary<string, object> ToDictionary()
        {
            var dictionary = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(CorrelationId))
                dictionary["CorrelationId"] = CorrelationId;

            if (!string.IsNullOrEmpty(SourceContext))
                dictionary["SourceContext"] = SourceContext;

            if (ThreadId != 0)
                dictionary["ThreadId"] = ThreadId;

            if (CreatedAt != default)
                dictionary["CreatedAt"] = CreatedAt;

            if (!string.IsNullOrEmpty(Operation))
                dictionary["Operation"] = Operation;

            if (!string.IsNullOrEmpty(UserId))
                dictionary["UserId"] = UserId;

            if (!string.IsNullOrEmpty(SessionId))
                dictionary["SessionId"] = SessionId;

            if (!string.IsNullOrEmpty(RequestId))
                dictionary["RequestId"] = RequestId;

            if (!string.IsNullOrEmpty(MachineName))
                dictionary["MachineName"] = MachineName;

            if (!string.IsNullOrEmpty(InstanceId))
                dictionary["InstanceId"] = InstanceId;

            // Add custom properties
            if (Properties != null)
            {
                foreach (var kvp in Properties)
                {
                    dictionary[kvp.Key] = kvp.Value;
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Disposable scope for context management.
        /// </summary>
        private sealed class ContextScope : IDisposable
        {
            private readonly LogContext _previousContext;
            private bool _disposed = false;

            public ContextScope(LogContext previousContext)
            {
                _previousContext = previousContext;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    CurrentContext.Value = _previousContext;
                    _disposed = true;
                }
            }
        }
    }
}