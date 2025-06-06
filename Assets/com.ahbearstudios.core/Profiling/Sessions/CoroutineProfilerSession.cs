using System;
using System.Collections.Generic;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Messages;
using AhBearStudios.Core.Profiling.Tagging;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Sessions
{
    /// <summary>
    /// A specialized profiler session for coroutine operations that captures additional coroutine metrics
    /// </summary>
    public class CoroutineProfilerSession : IProfilerSession
    {
        private readonly ProfilerMarker _marker;
        private readonly ProfilerTag _tag;
        private readonly IMessageBus _messageBus;
        private readonly Dictionary<string, double> _customMetrics = new Dictionary<string, double>();
        private bool _isDisposed;
        private long _startTimeNs;
        private long _endTimeNs;
        private readonly Guid _sessionId;
        
        /// <summary>
        /// Runner identifier
        /// </summary>
        public readonly Guid RunnerId;
        
        /// <summary>
        /// Runner name
        /// </summary>
        public readonly string RunnerName;
        
        /// <summary>
        /// Coroutine identifier
        /// </summary>
        public readonly int CoroutineId;
        
        /// <summary>
        /// Coroutine tag
        /// </summary>
        public readonly string CoroutineTag;
        
        /// <summary>
        /// The coroutine metrics interface for recording metrics
        /// </summary>
        private readonly ICoroutineMetrics _coroutineMetrics;
        
        /// <summary>
        /// The operation type being profiled
        /// </summary>
        private readonly string _operationType;
        
        /// <summary>
        /// Whether the operation completed successfully
        /// </summary>
        private bool _success = true;
        
        /// <summary>
        /// Creates a new coroutine profiler session
        /// </summary>
        /// <param name="tag">Profiler tag</param>
        /// <param name="runnerId">Runner identifier</param>
        /// <param name="runnerName">Runner name</param>
        /// <param name="coroutineId">Coroutine identifier</param>
        /// <param name="coroutineTag">Coroutine tag</param>
        /// <param name="coroutineMetrics">Coroutine metrics interface for recording</param>
        /// <param name="messageBus">Message bus for sending messages</param>
        public CoroutineProfilerSession(
            ProfilerTag tag, 
            Guid runnerId, 
            string runnerName, 
            int coroutineId, 
            string coroutineTag,
            ICoroutineMetrics coroutineMetrics,
            IMessageBus messageBus = null)
        {
            _tag = tag;
            RunnerId = runnerId;
            RunnerName = runnerName;
            CoroutineId = coroutineId;
            CoroutineTag = coroutineTag;
            _coroutineMetrics = coroutineMetrics;
            _messageBus = messageBus;
            _isDisposed = false;
            _sessionId = Guid.NewGuid();
            _operationType = GetOperationTypeFromTag(tag.Name);
            _marker = new ProfilerMarker(_tag.FullName);
            
            // Begin the profiler marker
            _marker.Begin();
            _startTimeNs = GetHighPrecisionTimestampNs();
            
            // Notify via message bus that session started
            if (_messageBus != null)
            {
                var message = new CoroutineProfilerSessionStartedMessage(
                    _tag, _sessionId, RunnerId, RunnerName, CoroutineId, CoroutineTag);
                _messageBus.PublishMessage(message);
            }
        }

        /// <summary>
        /// Get the tag associated with this session
        /// </summary>
        public ProfilerTag Tag => _tag;
        
        /// <summary>
        /// Gets the elapsed time in milliseconds
        /// </summary>
        public double ElapsedMilliseconds
        {
            get
            {
                long currentTimeNs = _isDisposed ? _endTimeNs : GetHighPrecisionTimestampNs();
                return (currentTimeNs - _startTimeNs) / 1000000.0;
            }
        }
        
        /// <summary>
        /// Gets the elapsed time in nanoseconds
        /// </summary>
        public long ElapsedNanoseconds
        {
            get
            {
                return _isDisposed ? (_endTimeNs - _startTimeNs) : (GetHighPrecisionTimestampNs() - _startTimeNs);
            }
        }
        
        /// <summary>
        /// Indicates if this session has been disposed
        /// </summary>
        public bool IsDisposed => _isDisposed;
        
        /// <summary>
        /// Records a custom metric with this session
        /// </summary>
        public void RecordMetric(string metricName, double value)
        {
            if (string.IsNullOrEmpty(metricName))
                return;
                
            _customMetrics[metricName] = value;
        }
        
        /// <summary>
        /// Gets a dictionary of all custom metrics recorded with this session
        /// </summary>
        public IReadOnlyDictionary<string, double> GetMetrics()
        {
            return _customMetrics;
        }
        
        /// <summary>
        /// Marks the operation as failed
        /// </summary>
        /// <param name="isTimeout">Whether the failure was due to timeout</param>
        public void MarkAsFailed(bool isTimeout = false)
        {
            _success = false;
            RecordMetric("IsTimeout", isTimeout ? 1.0 : 0.0);
        }

        /// <summary>
        /// End the profiler marker and record duration
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _marker.End();
            _endTimeNs = GetHighPrecisionTimestampNs();
            _isDisposed = true;
            
            // Record coroutine-specific metrics
            RecordCoroutineMetrics();
            
            // Notify via message bus that session ended
            if (_messageBus != null)
            {
                var message = new CoroutineProfilerSessionCompletedMessage(
                    _tag, _sessionId, RunnerId, RunnerName, CoroutineId, CoroutineTag, 
                    ElapsedMilliseconds, _customMetrics, _operationType, _success);
                _messageBus.PublishMessage(message);
            }
        }
        
        /// <summary>
        /// Record metrics specific to coroutine operations
        /// </summary>
        private void RecordCoroutineMetrics()
        {
            // Only record metrics if we have a runner ID and metrics system
            if (RunnerId != Guid.Empty && _coroutineMetrics != null)
            {
                bool hasTag = !string.IsNullOrEmpty(CoroutineTag);
                float durationMs = (float)ElapsedMilliseconds;
                
                // Record appropriate metrics based on operation type
                switch (_operationType.ToLowerInvariant())
                {
                    case "start":
                        _coroutineMetrics.RecordStart(RunnerId, durationMs, hasTag);
                        break;
                    case "complete":
                        // Get cleanup time from custom metrics if available
                        float cleanupTime = 0f;
                        if (_customMetrics.TryGetValue("CleanupTime", out var cleanup))
                            cleanupTime = (float)cleanup;
                            
                        _coroutineMetrics.RecordCompletion(RunnerId, durationMs, cleanupTime, hasTag);
                        break;
                    case "cancel":
                        _coroutineMetrics.RecordCancellation(RunnerId, hasTag);
                        break;
                    case "timeout":
                    case "fail":
                        bool isTimeout = _operationType.ToLowerInvariant() == "timeout" || 
                                       (_customMetrics.TryGetValue("IsTimeout", out var timeoutValue) && timeoutValue > 0);
                        _coroutineMetrics.RecordFailure(RunnerId, hasTag, isTimeout);
                        break;
                }
                
                // Record memory allocation if provided
                if (_customMetrics.TryGetValue("MemoryAllocated", out var memoryValue))
                {
                    bool isGC = _customMetrics.TryGetValue("IsGCAllocation", out var gcValue) && gcValue > 0;
                    _coroutineMetrics.RecordMemoryAllocation(RunnerId, (long)memoryValue, isGC);
                }
            }
        }
        
        /// <summary>
        /// Extract operation type from a tag name
        /// </summary>
        private string GetOperationTypeFromTag(string tagName)
        {
            // Extract operation type (after the last dot)
            int lastDot = tagName.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < tagName.Length - 1)
            {
                return tagName.Substring(lastDot + 1);
            }
            return tagName;
        }
        
        /// <summary>
        /// Gets high precision timestamp in nanoseconds
        /// </summary>
        private static long GetHighPrecisionTimestampNs()
        {
            long timestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            long frequency = System.Diagnostics.Stopwatch.Frequency;
            return (long)((double)timestamp / frequency * 1_000_000_000);
        }
    }
}