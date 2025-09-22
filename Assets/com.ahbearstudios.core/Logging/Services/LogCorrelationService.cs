using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Pooling.Pools;

namespace AhBearStudios.Core.Logging.Services
{
    /// <summary>
    /// Service for managing correlation IDs and distributed tracing across log entries.
    /// Provides centralized correlation ID management with thread-safe operations and automatic propagation.
    /// Supports hierarchical correlation tracking and cross-system tracing capabilities.
    /// </summary>
    public sealed class LogCorrelationService : ILogCorrelationService
    {
        private readonly ThreadLocal<CorrelationContext> _threadLocalContext;
        private readonly ConcurrentDictionary<string, CorrelationInfo> _correlationRegistry;
        private readonly Timer _cleanupTimer;
        private readonly object _registryLock = new object();
        private volatile bool _disposed = false;

        /// <summary>
        /// Gets the maximum age for correlation entries before cleanup.
        /// </summary>
        public TimeSpan MaxCorrelationAge { get; }

        /// <summary>
        /// Gets the cleanup interval for expired correlations.
        /// </summary>
        public TimeSpan CleanupInterval { get; }

        /// <summary>
        /// Gets the current correlation ID for the current thread.
        /// </summary>
        public FixedString128Bytes CurrentCorrelationId
        {
            get
            {
                if (_disposed) return default;
                return _threadLocalContext.Value?.CorrelationId ?? default;
            }
        }

        /// <summary>
        /// Gets the current correlation context for the current thread.
        /// </summary>
        public CorrelationContext CurrentContext
        {
            get
            {
                if (_disposed) return CorrelationContext.Empty;
                return _threadLocalContext.Value ?? CorrelationContext.Empty;
            }
        }

        /// <summary>
        /// Gets the number of active correlations being tracked.
        /// </summary>
        public int ActiveCorrelationCount => _correlationRegistry.Count;

        /// <summary>
        /// Gets correlation performance metrics.
        /// </summary>
        public CorrelationMetrics Metrics { get; private set; }

        /// <summary>
        /// Event raised when a new correlation is started.
        /// </summary>
        public event EventHandler<CorrelationStartedEventArgs> CorrelationStarted;

        /// <summary>
        /// Event raised when a correlation is completed.
        /// </summary>
        public event EventHandler<CorrelationCompletedEventArgs> CorrelationCompleted;

        /// <summary>
        /// Initializes a new instance of the LogCorrelationService.
        /// </summary>
        /// <param name="maxCorrelationAge">The maximum age for correlation entries before cleanup</param>
        /// <param name="cleanupInterval">The cleanup interval for expired correlations</param>
        public LogCorrelationService(
            TimeSpan maxCorrelationAge = default,
            TimeSpan cleanupInterval = default)
        {
            MaxCorrelationAge = maxCorrelationAge == default ? TimeSpan.FromHours(1) : maxCorrelationAge;
            CleanupInterval = cleanupInterval == default ? TimeSpan.FromMinutes(10) : cleanupInterval;

            _threadLocalContext = new ThreadLocal<CorrelationContext>();
            _correlationRegistry = new ConcurrentDictionary<string, CorrelationInfo>();
            Metrics = new CorrelationMetrics();

            // Start cleanup timer
            _cleanupTimer = new Timer(CleanupExpiredCorrelations, null, CleanupInterval, CleanupInterval);
        }

        /// <summary>
        /// Starts a new correlation for the current thread.
        /// </summary>
        /// <param name="operationName">The name of the operation being correlated</param>
        /// <param name="parentCorrelationId">Optional parent correlation ID</param>
        /// <param name="properties">Optional properties to associate with the correlation</param>
        /// <returns>A disposable correlation scope</returns>
        public ICorrelationScope StartCorrelation(
            string operationName,
            FixedString128Bytes parentCorrelationId = default,
            IReadOnlyDictionary<string, object> properties = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LogCorrelationService));

            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

            var correlationId = GenerateCorrelationId();
            var context = new CorrelationContext(
                correlationId,
                operationName,
                parentCorrelationId,
                properties);

            _threadLocalContext.Value = context;

            // Register the correlation
            var correlationInfo = CorrelationInfo.FromStrings(
                correlationId.ToString(),
                parentCorrelationId.IsEmpty ? null : parentCorrelationId.ToString(),
                null,
                operationName,
                null,
                null,
                null,
                "LoggingSystem",
                0);

            _correlationRegistry.TryAdd(correlationId.ToString(), correlationInfo);
            Metrics.IncrementStartedCorrelations();

            OnCorrelationStarted(new CorrelationStartedEventArgs(correlationInfo));

            return new CorrelationScope(this, context);
        }

        /// <summary>
        /// Starts a new correlation with a specific correlation ID.
        /// </summary>
        /// <param name="correlationId">The correlation ID to use</param>
        /// <param name="operationName">The name of the operation being correlated</param>
        /// <param name="parentCorrelationId">Optional parent correlation ID</param>
        /// <param name="properties">Optional properties to associate with the correlation</param>
        /// <returns>A disposable correlation scope</returns>
        public ICorrelationScope StartCorrelation(
            FixedString128Bytes correlationId,
            string operationName,
            FixedString128Bytes parentCorrelationId = default,
            IReadOnlyDictionary<string, object> properties = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LogCorrelationService));

            if (correlationId.IsEmpty)
                throw new ArgumentException("Correlation ID cannot be empty", nameof(correlationId));

            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

            var context = new CorrelationContext(
                correlationId,
                operationName,
                parentCorrelationId,
                properties);

            _threadLocalContext.Value = context;

            // Register the correlation
            var correlationInfo = CorrelationInfo.FromStrings(
                correlationId.ToString(),
                parentCorrelationId.IsEmpty ? null : parentCorrelationId.ToString(),
                null,
                operationName,
                null,
                null,
                null,
                "LoggingSystem",
                0);

            _correlationRegistry.TryAdd(correlationId.ToString(), correlationInfo);
            Metrics.IncrementStartedCorrelations();

            OnCorrelationStarted(new CorrelationStartedEventArgs(correlationInfo));

            return new CorrelationScope(this, context);
        }

        /// <summary>
        /// Continues an existing correlation from another thread or system.
        /// </summary>
        /// <param name="correlationId">The correlation ID to continue</param>
        /// <param name="operationName">The name of the operation being correlated</param>
        /// <param name="properties">Optional properties to associate with the correlation</param>
        /// <returns>A disposable correlation scope</returns>
        public ICorrelationScope ContinueCorrelation(
            FixedString128Bytes correlationId,
            string operationName,
            IReadOnlyDictionary<string, object> properties = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LogCorrelationService));

            if (correlationId.IsEmpty)
                throw new ArgumentException("Correlation ID cannot be empty", nameof(correlationId));

            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

            var context = new CorrelationContext(
                correlationId,
                operationName,
                default,
                properties);

            _threadLocalContext.Value = context;
            Metrics.IncrementContinuedCorrelations();

            return new CorrelationScope(this, context);
        }

        /// <summary>
        /// Completes the current correlation for the current thread.
        /// </summary>
        /// <param name="success">Whether the operation completed successfully</param>
        /// <param name="properties">Optional completion properties</param>
        public void CompleteCorrelation(bool success = true, IReadOnlyDictionary<string, object> properties = null)
        {
            if (_disposed) return;

            var context = _threadLocalContext.Value;
            if (context == null || context.CorrelationId.IsEmpty) return;

            var correlationId = context.CorrelationId.ToString();
            if (_correlationRegistry.TryRemove(correlationId, out var correlationInfo))
            {
                var completedInfo = new CorrelationCompletedInfo(
                    correlationInfo,
                    DateTime.UtcNow,
                    success,
                    properties);

                Metrics.IncrementCompletedCorrelations();
                OnCorrelationCompleted(new CorrelationCompletedEventArgs(completedInfo));
            }

            // Clear the thread local context
            _threadLocalContext.Value = null;
        }

        /// <summary>
        /// Gets correlation information for a specific correlation ID.
        /// </summary>
        /// <param name="correlationId">The correlation ID to look up</param>
        /// <returns>The correlation information, or null if not found</returns>
        public CorrelationInfo? GetCorrelationInfo(FixedString128Bytes correlationId)
        {
            if (_disposed || correlationId.IsEmpty) return null;

            _correlationRegistry.TryGetValue(correlationId.ToString(), out var correlationInfo);
            return correlationInfo;
        }

        /// <summary>
        /// Gets all active correlations.
        /// </summary>
        /// <returns>A dictionary of correlation IDs to correlation information</returns>
        public IReadOnlyDictionary<string, CorrelationInfo> GetActiveCorrelations()
        {
            if (_disposed) return new Dictionary<string, CorrelationInfo>();

            return new Dictionary<string, CorrelationInfo>(_correlationRegistry);
        }

        /// <summary>
        /// Enriches a log entry with correlation information.
        /// </summary>
        /// <param name="logEntry">The log entry to enrich</param>
        /// <returns>A new log entry with correlation information applied</returns>
        public LogEntry EnrichLogEntry(LogEntry logEntry)
        {
            return EnrichLogEntry(logEntry, null);
        }

        /// <summary>
        /// Enriches a log entry with correlation information using hybrid approach.
        /// </summary>
        /// <param name="logEntry">The log entry to enrich</param>
        /// <param name="managedDataPool">The managed data pool for storing enriched data</param>
        /// <returns>A new log entry with correlation information applied</returns>
        public LogEntry EnrichLogEntry(LogEntry logEntry, ManagedLogDataPool managedDataPool = null)
        {
            if (_disposed) return logEntry;

            var context = _threadLocalContext.Value;
            if (context == null || context.CorrelationId.IsEmpty) return logEntry;

            // Use the correlation ID from context if the log entry doesn't have one
            var correlationId = logEntry.CorrelationId.IsEmpty ? context.CorrelationId : logEntry.CorrelationId;

            // Merge properties from correlation context
            var enrichedProperties = new Dictionary<string, object>();
            
            // Add original log entry properties first
            if (logEntry.Properties != null)
            {
                foreach (var kvp in logEntry.Properties)
                {
                    enrichedProperties[kvp.Key] = kvp.Value;
                }
            }

            // Add correlation properties
            if (context.Properties != null)
            {
                foreach (var kvp in context.Properties)
                {
                    enrichedProperties[$"Correlation.{kvp.Key}"] = kvp.Value;
                }
            }

            // Add correlation metadata
            enrichedProperties["Correlation.OperationName"] = context.OperationName;
            if (!context.ParentCorrelationId.IsEmpty)
            {
                enrichedProperties["Correlation.ParentId"] = context.ParentCorrelationId.ToString();
            }

            // Create new log entry with enriched data using hybrid approach
            return new LogEntry(
                logEntry.Id,
                logEntry.TimestampTicks,
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
                logEntry.Scope,
                managedDataPool);
        }

        /// <summary>
        /// Generates a new correlation ID.
        /// </summary>
        /// <returns>A new correlation ID</returns>
        public static FixedString128Bytes GenerateCorrelationId()
        {
            return new FixedString128Bytes(Guid.NewGuid().ToString("N"));
        }

        /// <summary>
        /// Gets the current correlation performance metrics.
        /// </summary>
        /// <returns>A snapshot of current metrics</returns>
        public CorrelationMetrics GetMetrics()
        {
            return Metrics.CreateSnapshot();
        }

        /// <summary>
        /// Resets the correlation performance metrics.
        /// </summary>
        public void ResetMetrics()
        {
            Metrics = new CorrelationMetrics();
        }

        /// <summary>
        /// Cleans up expired correlations.
        /// </summary>
        /// <param name="state">Timer state</param>
        private void CleanupExpiredCorrelations(object state)
        {
            if (_disposed) return;

            var cutoffTime = DateTime.UtcNow - MaxCorrelationAge;
            var expiredKeys = new List<string>();

            foreach (var kvp in _correlationRegistry)
            {
                if (kvp.Value.CreatedAt < cutoffTime)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                if (_correlationRegistry.TryRemove(key, out var correlationInfo))
                {
                    Metrics.IncrementExpiredCorrelations();
                }
            }
        }

        /// <summary>
        /// Raises the CorrelationStarted event.
        /// </summary>
        /// <param name="args">The event arguments</param>
        private void OnCorrelationStarted(CorrelationStartedEventArgs args)
        {
            CorrelationStarted?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the CorrelationCompleted event.
        /// </summary>
        /// <param name="args">The event arguments</param>
        private void OnCorrelationCompleted(CorrelationCompletedEventArgs args)
        {
            CorrelationCompleted?.Invoke(this, args);
        }

        /// <summary>
        /// Disposes the correlation service and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            // Stop the cleanup timer
            _cleanupTimer?.Dispose();

            // Complete any active correlations
            foreach (var kvp in _correlationRegistry)
            {
                var completedInfo = new CorrelationCompletedInfo(
                    kvp.Value,
                    DateTime.UtcNow,
                    false,
                    new Dictionary<string, object> { ["Reason"] = "Service disposed" });

                OnCorrelationCompleted(new CorrelationCompletedEventArgs(completedInfo));
            }

            // Clear the registry
            _correlationRegistry.Clear();

            // Dispose thread local storage
            _threadLocalContext?.Dispose();
        }
    }

    /// <summary>
    /// Represents correlation context information for a thread.
    /// </summary>
    public sealed class CorrelationContext
    {
        /// <summary>
        /// Gets an empty correlation context.
        /// </summary>
        public static readonly CorrelationContext Empty = new CorrelationContext(default, string.Empty, default, null);

        /// <summary>
        /// Gets the correlation ID.
        /// </summary>
        public FixedString128Bytes CorrelationId { get; }

        /// <summary>
        /// Gets the operation name.
        /// </summary>
        public string OperationName { get; }

        /// <summary>
        /// Gets the parent correlation ID.
        /// </summary>
        public FixedString128Bytes ParentCorrelationId { get; }

        /// <summary>
        /// Gets the correlation properties.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; }

        /// <summary>
        /// Initializes a new instance of the CorrelationContext.
        /// </summary>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="operationName">The operation name</param>
        /// <param name="parentCorrelationId">The parent correlation ID</param>
        /// <param name="properties">The correlation properties</param>
        public CorrelationContext(
            FixedString128Bytes correlationId,
            string operationName,
            FixedString128Bytes parentCorrelationId,
            IReadOnlyDictionary<string, object> properties)
        {
            CorrelationId = correlationId;
            OperationName = operationName ?? string.Empty;
            ParentCorrelationId = parentCorrelationId;
            Properties = properties ?? new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Interface for correlation scopes.
    /// </summary>
    public interface ICorrelationScope : IDisposable
    {
        /// <summary>
        /// Gets the correlation ID for this scope.
        /// </summary>
        FixedString128Bytes CorrelationId { get; }

        /// <summary>
        /// Gets the operation name for this scope.
        /// </summary>
        string OperationName { get; }

        /// <summary>
        /// Gets the correlation context for this scope.
        /// </summary>
        CorrelationContext Context { get; }
    }

    /// <summary>
    /// Implementation of ICorrelationScope.
    /// </summary>
    internal sealed class CorrelationScope : ICorrelationScope
    {
        private readonly LogCorrelationService _correlationService;
        private readonly CorrelationContext _context;
        private bool _disposed = false;

        /// <inheritdoc />
        public FixedString128Bytes CorrelationId => _context.CorrelationId;

        /// <inheritdoc />
        public string OperationName => _context.OperationName;

        /// <inheritdoc />
        public CorrelationContext Context => _context;

        /// <summary>
        /// Initializes a new instance of the CorrelationScope.
        /// </summary>
        /// <param name="correlationService">The correlation service</param>
        /// <param name="context">The correlation context</param>
        public CorrelationScope(LogCorrelationService correlationService, CorrelationContext context)
        {
            _correlationService = correlationService ?? throw new ArgumentNullException(nameof(correlationService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            // Complete the correlation when the scope is disposed
            _correlationService.CompleteCorrelation(true);
        }
    }

    /// <summary>
    /// Performance metrics for correlation operations.
    /// </summary>
    public sealed class CorrelationMetrics
    {
        private volatile int _startedCorrelations = 0;
        private volatile int _completedCorrelations = 0;
        private volatile int _continuedCorrelations = 0;
        private volatile int _expiredCorrelations = 0;
        private readonly DateTime _startTime = DateTime.UtcNow;

        /// <summary>
        /// Gets the total number of started correlations.
        /// </summary>
        public int StartedCorrelations => _startedCorrelations;

        /// <summary>
        /// Gets the total number of completed correlations.
        /// </summary>
        public int CompletedCorrelations => _completedCorrelations;

        /// <summary>
        /// Gets the total number of continued correlations.
        /// </summary>
        public int ContinuedCorrelations => _continuedCorrelations;

        /// <summary>
        /// Gets the total number of expired correlations.
        /// </summary>
        public int ExpiredCorrelations => _expiredCorrelations;

        /// <summary>
        /// Gets the completion rate (completed / started).
        /// </summary>
        public double CompletionRate => _startedCorrelations > 0 ? (double)_completedCorrelations / _startedCorrelations : 0.0;

        /// <summary>
        /// Gets the total uptime of the correlation service.
        /// </summary>
        public TimeSpan Uptime => DateTime.UtcNow - _startTime;

        internal void IncrementStartedCorrelations() => Interlocked.Increment(ref _startedCorrelations);
        internal void IncrementCompletedCorrelations() => Interlocked.Increment(ref _completedCorrelations);
        internal void IncrementContinuedCorrelations() => Interlocked.Increment(ref _continuedCorrelations);
        internal void IncrementExpiredCorrelations() => Interlocked.Increment(ref _expiredCorrelations);

        /// <summary>
        /// Creates a snapshot of the current metrics.
        /// </summary>
        /// <returns>A new CorrelationMetrics instance with current values</returns>
        public CorrelationMetrics CreateSnapshot()
        {
            return new CorrelationMetrics
            {
                _startedCorrelations = _startedCorrelations,
                _completedCorrelations = _completedCorrelations,
                _continuedCorrelations = _continuedCorrelations,
                _expiredCorrelations = _expiredCorrelations
            };
        }
    }

    /// <summary>
    /// Event arguments for correlation started events.
    /// </summary>
    public sealed class CorrelationStartedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the correlation information.
        /// </summary>
        public CorrelationInfo CorrelationInfo { get; }

        /// <summary>
        /// Initializes a new instance of the CorrelationStartedEventArgs.
        /// </summary>
        /// <param name="correlationInfo">The correlation information</param>
        public CorrelationStartedEventArgs(CorrelationInfo correlationInfo)
        {
            CorrelationInfo = correlationInfo;
        }
    }

    /// <summary>
    /// Event arguments for correlation completed events.
    /// </summary>
    public sealed class CorrelationCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the correlation completion information.
        /// </summary>
        public CorrelationCompletedInfo CompletedInfo { get; }

        /// <summary>
        /// Initializes a new instance of the CorrelationCompletedEventArgs.
        /// </summary>
        /// <param name="completedInfo">The correlation completion information</param>
        public CorrelationCompletedEventArgs(CorrelationCompletedInfo completedInfo)
        {
            CompletedInfo = completedInfo ?? throw new ArgumentNullException(nameof(completedInfo));
        }
    }

    /// <summary>
    /// Information about a completed correlation.
    /// </summary>
    public sealed class CorrelationCompletedInfo
    {
        /// <summary>
        /// Gets the original correlation information.
        /// </summary>
        public CorrelationInfo CorrelationInfo { get; }

        /// <summary>
        /// Gets the completion time.
        /// </summary>
        public DateTime CompletionTime { get; }

        /// <summary>
        /// Gets whether the operation completed successfully.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the completion duration.
        /// </summary>
        public TimeSpan Duration => CompletionTime - CorrelationInfo.CreatedAt;

        /// <summary>
        /// Gets the completion properties.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; }

        /// <summary>
        /// Initializes a new instance of the CorrelationCompletedInfo.
        /// </summary>
        /// <param name="correlationInfo">The original correlation information</param>
        /// <param name="completionTime">The completion time</param>
        /// <param name="success">Whether the operation completed successfully</param>
        /// <param name="properties">The completion properties</param>
        public CorrelationCompletedInfo(
            CorrelationInfo correlationInfo,
            DateTime completionTime,
            bool success,
            IReadOnlyDictionary<string, object> properties)
        {
            CorrelationInfo = correlationInfo;
            CompletionTime = completionTime;
            Success = success;
            Properties = properties ?? new Dictionary<string, object>();
        }
    }
}