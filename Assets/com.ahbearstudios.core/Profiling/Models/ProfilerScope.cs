using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Profiling;
using AhBearStudios.Core.Profiling.Models;
using AhBearStudios.Core.Profiling.Messages;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Profiling.Models
{
    /// <summary>
    /// Represents a disposable profiling scope that integrates Unity's ProfilerMarker with custom profiling capabilities.
    /// Designed for zero-allocation performance monitoring that meets Unity's 60 FPS frame budget requirements.
    /// </summary>
    /// <remarks>
    /// ProfilerScope provides a unified profiling abstraction that:
    /// - Wraps Unity's ProfilerMarker internally for Unity Profiler integration
    /// - Tracks scope timing for custom metrics and threshold monitoring
    /// - Supports object pooling to minimize garbage collection impact
    /// - Enables correlation tracking across distributed profiling systems
    /// - Provides automatic threshold violation detection and alerting
    /// 
    /// This class follows the disposable pattern for automatic scope cleanup and
    /// integrates seamlessly with both Unity's built-in profiling and custom analytics.
    /// </remarks>
    public sealed class ProfilerScope : IDisposable
    {
        #region Private Fields

        private readonly ProfilerMarker _unityMarker;
        private readonly ProfilerTag _tag;
        private readonly IProfilerService _profilerService;
        private readonly IMessageBusService _messageBusService;
        private readonly FixedString64Bytes _source;
        private readonly Guid _correlationId;
        private readonly IReadOnlyDictionary<string, object> _metadata;
        private readonly long _startTicks;
        private readonly double _thresholdMs;
        private readonly bool _enableThresholdMonitoring;
        private bool _disposed;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the unique identifier for this profiling scope instance.
        /// Used for correlation tracking and debugging across profiling systems.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets the profiler tag associated with this scope.
        /// Provides consistent naming and categorization for profiling operations.
        /// </summary>
        public ProfilerTag Tag => _tag;

        /// <summary>
        /// Gets the source system or component that created this scope.
        /// Useful for filtering and categorizing profiling data by origin.
        /// </summary>
        public FixedString64Bytes Source => _source;

        /// <summary>
        /// Gets the correlation ID linking this scope to related operations.
        /// Enables tracing performance impacts across distributed systems.
        /// </summary>
        public Guid CorrelationId => _correlationId;

        /// <summary>
        /// Gets the metadata associated with this profiling scope.
        /// Provides additional context for performance analysis and debugging.
        /// </summary>
        public IReadOnlyDictionary<string, object> Metadata => _metadata;

        /// <summary>
        /// Gets the timestamp when this scope was created, in UTC ticks.
        /// Provides high precision timing for performance analysis.
        /// </summary>
        public long StartTicks => _startTicks;

        /// <summary>
        /// Gets the performance threshold for this scope in milliseconds.
        /// Scopes exceeding this duration will trigger threshold exceeded events.
        /// </summary>
        public double ThresholdMs => _thresholdMs;

        /// <summary>
        /// Gets a value indicating whether this scope is disposed.
        /// Used to prevent double-disposal and ensure proper cleanup.
        /// </summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// Gets the elapsed time since scope creation in milliseconds.
        /// Returns -1 if the scope has been disposed.
        /// </summary>
        public double ElapsedMs
        {
            get
            {
                if (_disposed) return -1;
                var elapsedTicks = DateTime.UtcNow.Ticks - _startTicks;
                return new TimeSpan(elapsedTicks).TotalMilliseconds;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this scope has exceeded its performance threshold.
        /// Only meaningful if threshold monitoring is enabled.
        /// </summary>
        public bool HasExceededThreshold
        {
            get
            {
                if (!_enableThresholdMonitoring) return false;
                return ElapsedMs > _thresholdMs;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ProfilerScope class with comprehensive profiling configuration.
        /// </summary>
        /// <param name="tag">Profiler tag for categorization and Unity marker creation</param>
        /// <param name="profilerService">Parent profiler service for metrics recording</param>
        /// <param name="messageBusService">Message bus service for publishing threshold events (optional)</param>
        /// <param name="source">Source system or component creating this scope</param>
        /// <param name="correlationId">Correlation ID for operation tracking</param>
        /// <param name="metadata">Additional metadata for context</param>
        /// <param name="thresholdMs">Performance threshold in milliseconds</param>
        /// <param name="enableThresholdMonitoring">Whether to monitor threshold violations</param>
        /// <exception cref="ArgumentException">Thrown when tag is empty</exception>
        /// <exception cref="ArgumentNullException">Thrown when profilerService is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is negative</exception>
        public ProfilerScope(
            ProfilerTag tag,
            IProfilerService profilerService,
            IMessageBusService messageBusService = null,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            IReadOnlyDictionary<string, object> metadata = null,
            double thresholdMs = 16.67, // 60 FPS frame budget default
            bool enableThresholdMonitoring = true)
        {
            if (tag.IsEmpty)
                throw new ArgumentException("Profiler tag cannot be empty", nameof(tag));
            
            if (profilerService == null)
                throw new ArgumentNullException(nameof(profilerService));
            
            if (thresholdMs < 0)
                throw new ArgumentOutOfRangeException(nameof(thresholdMs), "Threshold cannot be negative");

            // Initialize core properties
            _tag = tag;
            _profilerService = profilerService;
            _messageBusService = messageBusService;
            _source = source.IsEmpty ? "ProfilerService" : source;
            _correlationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("ProfilerScope", tag.Name.ToString())
                : correlationId;
            _metadata = metadata ?? new Dictionary<string, object>();
            _thresholdMs = thresholdMs;
            _enableThresholdMonitoring = enableThresholdMonitoring;
            _startTicks = DateTime.UtcNow.Ticks;
            
            // Generate unique ID for this scope instance
            Id = DeterministicIdGenerator.GenerateCoreId($"ProfilerScope:{tag.Name}-{_startTicks}");

            // Create and begin Unity ProfilerMarker for Unity Profiler integration
            _unityMarker = tag.CreateUnityMarker();
            _unityMarker.Begin();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually records a custom metric within this scope.
        /// Useful for tracking additional performance data during scope execution.
        /// </summary>
        /// <param name="metricName">Name of the custom metric</param>
        /// <param name="value">Metric value</param>
        /// <param name="unit">Unit of measurement (optional)</param>
        /// <param name="tags">Additional tags for the metric (optional)</param>
        /// <exception cref="ObjectDisposedException">Thrown when scope has been disposed</exception>
        /// <exception cref="ArgumentException">Thrown when metric name is null or empty</exception>
        public void RecordMetric(string metricName, double value, string unit = null, IReadOnlyDictionary<string, string> tags = null)
        {
            ThrowIfDisposed();
            
            if (string.IsNullOrEmpty(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            _profilerService?.RecordMetric(metricName, value, unit, tags);
        }

        /// <summary>
        /// Manually records a performance sample within this scope.
        /// Creates a metric snapshot with the current scope context.
        /// </summary>
        /// <param name="value">Performance value (typically in milliseconds)</param>
        /// <param name="unit">Unit of measurement (defaults to "ms")</param>
        /// <param name="tags">Additional tags for the sample (optional)</param>
        /// <exception cref="ObjectDisposedException">Thrown when scope has been disposed</exception>
        public void RecordSample(float value, string unit = "ms", IReadOnlyDictionary<string, string> tags = null)
        {
            ThrowIfDisposed();
            
            _profilerService?.RecordSample(_tag, value, unit);
        }

        /// <summary>
        /// Gets the current elapsed time for this scope without disposing it.
        /// Useful for intermediate performance measurements within long-running scopes.
        /// </summary>
        /// <returns>Elapsed time in milliseconds, or -1 if disposed</returns>
        public double GetElapsedTime()
        {
            return ElapsedMs;
        }

        /// <summary>
        /// Creates a nested scope within this scope for hierarchical profiling.
        /// The nested scope inherits the correlation ID and source from the parent.
        /// </summary>
        /// <param name="childTag">Profiler tag for the child scope</param>
        /// <param name="childMetadata">Additional metadata for the child scope</param>
        /// <param name="childThresholdMs">Performance threshold for child scope (optional)</param>
        /// <returns>New ProfilerScope instance representing the child scope</returns>
        /// <exception cref="ObjectDisposedException">Thrown when parent scope has been disposed</exception>
        /// <exception cref="ArgumentException">Thrown when child tag is empty</exception>
        public ProfilerScope CreateChildScope(
            ProfilerTag childTag,
            IReadOnlyDictionary<string, object> childMetadata = null,
            double? childThresholdMs = null)
        {
            ThrowIfDisposed();
            
            if (childTag.IsEmpty)
                throw new ArgumentException("Child profiler tag cannot be empty", nameof(childTag));

            return new ProfilerScope(
                tag: childTag,
                profilerService: _profilerService,
                source: _source,
                correlationId: _correlationId,
                metadata: childMetadata,
                thresholdMs: childThresholdMs ?? _thresholdMs,
                enableThresholdMonitoring: _enableThresholdMonitoring);
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases all resources used by the ProfilerScope and records final performance metrics.
        /// This method is called automatically when using the 'using' statement pattern.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                // Calculate final elapsed time
                var finalElapsedMs = ElapsedMs;

                // End the Unity ProfilerMarker
                _unityMarker.End();

                // Record the final performance sample
                if (_profilerService != null && !_tag.IsEmpty)
                {
                    _profilerService.RecordSample(_tag, (float)finalElapsedMs, "ms");
                }

                // Check threshold violation and publish message if necessary
                if (_enableThresholdMonitoring && finalElapsedMs > _thresholdMs && _messageBusService != null)
                {
                    try
                    {
                        // Publish ProfilerThresholdExceededMessage via message bus (CLAUDE.md compliant)
                        var thresholdMessage = ProfilerThresholdExceededMessage.Create(
                            tag: _tag,
                            elapsedMs: finalElapsedMs,
                            thresholdMs: _thresholdMs,
                            scopeId: Id,
                            source: _source,
                            correlationId: _correlationId,
                            priority: MessagePriority.High);

                        _messageBusService.PublishMessageAsync(thresholdMessage);
                    }
                    catch
                    {
                        // Swallow threshold message publishing exceptions to prevent disposal failures
                        // Threshold monitoring is best-effort and should not break profiling
                    }
                }
            }
            finally
            {
                _disposed = true;
            }
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new ProfilerScope with default settings optimized for Unity performance.
        /// Uses the standard 60 FPS frame budget (16.67ms) as the performance threshold.
        /// </summary>
        /// <param name="tag">Profiler tag for the scope</param>
        /// <param name="profilerService">Parent profiler service</param>
        /// <param name="source">Source system (optional)</param>
        /// <param name="correlationId">Correlation ID (auto-generated if not provided)</param>
        /// <returns>New ProfilerScope instance with Unity-optimized defaults</returns>
        /// <exception cref="ArgumentException">Thrown when tag is empty</exception>
        /// <exception cref="ArgumentNullException">Thrown when profilerService is null</exception>
        public static ProfilerScope CreateUnityOptimized(
            ProfilerTag tag,
            IProfilerService profilerService,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new ProfilerScope(
                tag: tag,
                profilerService: profilerService,
                source: source,
                correlationId: correlationId,
                metadata: null,
                thresholdMs: 16.67, // 60 FPS frame budget
                enableThresholdMonitoring: true);
        }

        /// <summary>
        /// Creates a new ProfilerScope optimized for production environments.
        /// Uses relaxed thresholds and minimal monitoring to reduce overhead.
        /// </summary>
        /// <param name="tag">Profiler tag for the scope</param>
        /// <param name="profilerService">Parent profiler service</param>
        /// <param name="source">Source system (optional)</param>
        /// <param name="correlationId">Correlation ID (auto-generated if not provided)</param>
        /// <returns>New ProfilerScope instance with production-optimized settings</returns>
        /// <exception cref="ArgumentException">Thrown when tag is empty</exception>
        /// <exception cref="ArgumentNullException">Thrown when profilerService is null</exception>
        public static ProfilerScope CreateProductionOptimized(
            ProfilerTag tag,
            IProfilerService profilerService,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new ProfilerScope(
                tag: tag,
                profilerService: profilerService,
                source: source,
                correlationId: correlationId,
                metadata: null,
                thresholdMs: 33.33, // More relaxed 30 FPS threshold for production
                enableThresholdMonitoring: true);
        }

        /// <summary>
        /// Creates a new ProfilerScope for development and debugging scenarios.
        /// Uses strict thresholds and comprehensive monitoring for detailed analysis.
        /// </summary>
        /// <param name="tag">Profiler tag for the scope</param>
        /// <param name="profilerService">Parent profiler service</param>
        /// <param name="metadata">Development metadata for debugging</param>
        /// <param name="source">Source system (optional)</param>
        /// <param name="correlationId">Correlation ID (auto-generated if not provided)</param>
        /// <returns>New ProfilerScope instance with development-optimized settings</returns>
        /// <exception cref="ArgumentException">Thrown when tag is empty</exception>
        /// <exception cref="ArgumentNullException">Thrown when profilerService is null</exception>
        public static ProfilerScope CreateDevelopmentScope(
            ProfilerTag tag,
            IProfilerService profilerService,
            IReadOnlyDictionary<string, object> metadata = null,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new ProfilerScope(
                tag: tag,
                profilerService: profilerService,
                source: source,
                correlationId: correlationId,
                metadata: metadata,
                thresholdMs: 8.33, // Strict 120 FPS threshold for development
                enableThresholdMonitoring: true);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Throws an ObjectDisposedException if this scope has been disposed.
        /// Used to guard against operations on disposed scopes.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when scope has been disposed</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ProfilerScope), "Cannot perform operations on a disposed ProfilerScope");
        }

        #endregion

        #region Object Overrides

        /// <summary>
        /// Returns a string representation of this profiler scope for debugging purposes.
        /// </summary>
        /// <returns>String representation including key scope information</returns>
        public override string ToString()
        {
            var status = _disposed ? "Disposed" : "Active";
            var elapsed = _disposed ? "N/A" : $"{ElapsedMs:F2}ms";
            return $"ProfilerScope [Tag={_tag.Name}, Status={status}, Elapsed={elapsed}, Threshold={_thresholdMs:F2}ms]";
        }

        /// <summary>
        /// Determines whether the specified object is equal to this profiler scope.
        /// Equality is based on the scope ID.
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>True if objects are equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            return obj is ProfilerScope other && Id.Equals(other.Id);
        }

        /// <summary>
        /// Returns the hash code for this profiler scope.
        /// Based on the scope ID for consistent hashing.
        /// </summary>
        /// <returns>Hash code for this scope</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion
    }
}