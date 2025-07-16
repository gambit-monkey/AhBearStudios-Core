using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Pooling.Pools;

namespace AhBearStudios.Core.Logging.Services
{
    /// <summary>
    /// Service for managing logging context and scoped information.
    /// Provides hierarchical context management with thread-safe operations and correlation tracking.
    /// Supports push/pop context operations and automatic context inheritance.
    /// </summary>
    public sealed class LogContextService : IDisposable
    {
        private readonly ThreadLocal<Stack<LogContext>> _contextStack;
        private readonly Dictionary<string, object> _globalProperties;
        private readonly object _globalPropertiesLock = new object();
        private volatile bool _disposed = false;

        /// <summary>
        /// Gets the current logging context for the current thread.
        /// </summary>
        public LogContext CurrentContext
        {
            get
            {
                if (_disposed) return LogContext.Empty;
                
                var stack = _contextStack.Value;
                return stack.Count > 0 ? stack.Peek() : LogContext.Empty;
            }
        }

        /// <summary>
        /// Gets the depth of the current context stack.
        /// </summary>
        public int ContextDepth
        {
            get
            {
                if (_disposed) return 0;
                return _contextStack.Value?.Count ?? 0;
            }
        }

        /// <summary>
        /// Gets the global properties that are applied to all log entries.
        /// </summary>
        public IReadOnlyDictionary<string, object> GlobalProperties
        {
            get
            {
                lock (_globalPropertiesLock)
                {
                    return new Dictionary<string, object>(_globalProperties);
                }
            }
        }

        /// <summary>
        /// Event raised when a new context is pushed onto the stack.
        /// </summary>
        public event EventHandler<ContextChangedEventArgs> ContextPushed;

        /// <summary>
        /// Event raised when a context is popped from the stack.
        /// </summary>
        public event EventHandler<ContextChangedEventArgs> ContextPopped;

        /// <summary>
        /// Initializes a new instance of the LogContextService.
        /// </summary>
        public LogContextService()
        {
            _contextStack = new ThreadLocal<Stack<LogContext>>(() => new Stack<LogContext>());
            _globalProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Pushes a new context onto the context stack.
        /// </summary>
        /// <param name="contextName">The name of the context</param>
        /// <param name="properties">Optional properties to associate with the context</param>
        /// <param name="correlationId">Optional correlation ID for the context</param>
        /// <returns>A disposable scope that will pop the context when disposed</returns>
        public ILogScope PushContext(string contextName, IReadOnlyDictionary<string, object> properties = null, FixedString128Bytes correlationId = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LogContextService));

            if (string.IsNullOrEmpty(contextName))
                throw new ArgumentException("Context name cannot be null or empty", nameof(contextName));

            var parentContext = CurrentContext;
            var newContext = new LogContext(
                correlationId: correlationId.IsEmpty ? parentContext.CorrelationId : correlationId.ToString(),
                sourceContext: contextName,
                operation: contextName,
                properties: properties);

            _contextStack.Value.Push(newContext);
            
            OnContextPushed(new ContextChangedEventArgs(newContext, parentContext));
            
            return new LogScope(this, newContext);
        }

        /// <summary>
        /// Pushes a new context with correlation ID onto the context stack.
        /// </summary>
        /// <param name="contextName">The name of the context</param>
        /// <param name="correlationId">The correlation ID for the context</param>
        /// <param name="properties">Optional properties to associate with the context</param>
        /// <returns>A disposable scope that will pop the context when disposed</returns>
        public ILogScope PushContext(string contextName, FixedString128Bytes correlationId, IReadOnlyDictionary<string, object> properties = null)
        {
            return PushContext(contextName, properties, correlationId);
        }

        /// <summary>
        /// Pops the current context from the context stack.
        /// </summary>
        /// <returns>The popped context, or LogContext.Empty if no context was on the stack</returns>
        public LogContext PopContext()
        {
            if (_disposed) return LogContext.Empty;

            var stack = _contextStack.Value;
            if (stack.Count == 0) return LogContext.Empty;

            var poppedContext = stack.Pop();
            var newCurrentContext = stack.Count > 0 ? stack.Peek() : LogContext.Empty;
            
            OnContextPopped(new ContextChangedEventArgs(newCurrentContext, poppedContext));
            
            return poppedContext;
        }

        /// <summary>
        /// Clears all contexts from the current thread's context stack.
        /// </summary>
        public void ClearContexts()
        {
            if (_disposed) return;

            var stack = _contextStack.Value;
            while (stack.Count > 0)
            {
                PopContext();
            }
        }

        /// <summary>
        /// Gets all contexts in the current thread's context stack.
        /// </summary>
        /// <returns>An array of contexts from bottom to top of the stack</returns>
        public LogContext[] GetContextHierarchy()
        {
            if (_disposed) return Array.Empty<LogContext>();

            var stack = _contextStack.Value;
            var contexts = new LogContext[stack.Count];
            stack.CopyTo(contexts, 0);
            Array.Reverse(contexts); // Reverse to get bottom-to-top order
            return contexts;
        }

        /// <summary>
        /// Enriches a log entry with the current context information using hybrid approach.
        /// </summary>
        /// <param name="logEntry">The log entry to enrich</param>
        /// <param name="managedDataPool">The managed data pool for storing enriched data</param>
        /// <returns>A new log entry with context information applied</returns>
        public LogEntry EnrichLogEntry(LogEntry logEntry, ManagedLogDataPool managedDataPool = null)
        {
            if (_disposed) return logEntry;

            var currentContext = CurrentContext;
            if (currentContext.Equals(LogContext.Empty) && _globalProperties.Count == 0)
            {
                return logEntry;
            }

            // Merge properties from context hierarchy and global properties
            var enrichedProperties = new Dictionary<string, object>();
            
            // Add global properties first
            lock (_globalPropertiesLock)
            {
                foreach (var kvp in _globalProperties)
                {
                    enrichedProperties[kvp.Key] = kvp.Value;
                }
            }

            // Add context hierarchy properties
            var hierarchy = GetContextHierarchy();
            foreach (var context in hierarchy)
            {
                if (context.Properties != null)
                {
                    foreach (var kvp in context.Properties)
                    {
                        enrichedProperties[kvp.Key] = kvp.Value;
                    }
                }
            }

            // Add original log entry properties (these take precedence)
            if (logEntry.Properties != null)
            {
                foreach (var kvp in logEntry.Properties)
                {
                    enrichedProperties[kvp.Key] = kvp.Value;
                }
            }

            // Use correlation ID from context if log entry doesn't have one
            var correlationId = string.IsNullOrEmpty(logEntry.CorrelationId.ToString()) ? currentContext.CorrelationId : logEntry.CorrelationId;

            // Create new log entry with enriched data using hybrid approach
            return new LogEntry(
                logEntry.Id,
                logEntry.Timestamp,
                logEntry.Level,
                logEntry.Channel,
                logEntry.Message,
                correlationId,
                logEntry.SourceContext,
                logEntry.Source,
                logEntry.Priority,
                logEntry.ThreadId,
                logEntry.MachineName,
                logEntry.InstanceId,
                logEntry.Exception,
                enrichedProperties,
                currentContext.Equals(LogContext.Empty) ? null : new LogScope(this, currentContext),
                managedDataPool);
        }

        /// <summary>
        /// Sets a global property that will be applied to all log entries.
        /// </summary>
        /// <param name="key">The property key</param>
        /// <param name="value">The property value</param>
        public void SetGlobalProperty(string key, object value)
        {
            if (_disposed) return;

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Property key cannot be null or empty", nameof(key));

            lock (_globalPropertiesLock)
            {
                _globalProperties[key] = value;
            }
        }

        /// <summary>
        /// Removes a global property.
        /// </summary>
        /// <param name="key">The property key to remove</param>
        /// <returns>True if the property was removed, false if it wasn't found</returns>
        public bool RemoveGlobalProperty(string key)
        {
            if (_disposed) return false;

            if (string.IsNullOrEmpty(key)) return false;

            lock (_globalPropertiesLock)
            {
                return _globalProperties.Remove(key);
            }
        }

        /// <summary>
        /// Clears all global properties.
        /// </summary>
        public void ClearGlobalProperties()
        {
            if (_disposed) return;

            lock (_globalPropertiesLock)
            {
                _globalProperties.Clear();
            }
        }

        /// <summary>
        /// Creates a new correlation ID for use in logging contexts.
        /// </summary>
        /// <returns>A new correlation ID</returns>
        public static FixedString128Bytes CreateCorrelationId()
        {
            return new FixedString128Bytes(Guid.NewGuid().ToString("N"));
        }

        /// <summary>
        /// Raises the ContextPushed event.
        /// </summary>
        /// <param name="args">The event arguments</param>
        private void OnContextPushed(ContextChangedEventArgs args)
        {
            ContextPushed?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the ContextPopped event.
        /// </summary>
        /// <param name="args">The event arguments</param>
        private void OnContextPopped(ContextChangedEventArgs args)
        {
            ContextPopped?.Invoke(this, args);
        }

        /// <summary>
        /// Disposes the context service and clears all contexts.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            // Clear all contexts for all threads
            _contextStack?.Dispose();

            // Clear global properties
            lock (_globalPropertiesLock)
            {
                _globalProperties.Clear();
            }
        }
    }

    /// <summary>
    /// Event arguments for context change events.
    /// </summary>
    public sealed class ContextChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the new current context.
        /// </summary>
        public LogContext NewContext { get; }

        /// <summary>
        /// Gets the previous context.
        /// </summary>
        public LogContext PreviousContext { get; }

        /// <summary>
        /// Initializes a new instance of the ContextChangedEventArgs.
        /// </summary>
        /// <param name="newContext">The new current context</param>
        /// <param name="previousContext">The previous context</param>
        public ContextChangedEventArgs(LogContext newContext, LogContext previousContext)
        {
            NewContext = newContext;
            PreviousContext = previousContext;
        }
    }
}