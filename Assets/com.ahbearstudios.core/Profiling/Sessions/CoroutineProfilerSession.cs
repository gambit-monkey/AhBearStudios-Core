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
    /// A specialized profiler session for coroutine operations that captures additional coroutine metrics.
    /// Provides intelligent tag selection and lightweight null pattern for disabled profiling.
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
        private readonly bool _isNullSession;
        
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
        /// Creates a new coroutine profiler session with explicit tag
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
            RunnerName = runnerName ?? string.Empty;
            CoroutineId = coroutineId;
            CoroutineTag = coroutineTag ?? string.Empty;
            _coroutineMetrics = coroutineMetrics;
            _messageBus = messageBus;
            _isDisposed = false;
            _sessionId = Guid.NewGuid();
            _operationType = GetOperationTypeFromTag(tag.Name);
            _isNullSession = coroutineMetrics == null && messageBus == null;
            
            // Only create marker and start timing if this isn't a null session
            if (!_isNullSession)
            {
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
        }

        /// <summary>
        /// Factory method to create a session with appropriate tag based on parameters.
        /// Uses intelligent tag selection hierarchy for optimal profiling granularity.
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="runnerId">Runner identifier</param>
        /// <param name="runnerName">Runner name</param>
        /// <param name="coroutineId">Coroutine identifier</param>
        /// <param name="coroutineTag">Coroutine tag</param>
        /// <param name="coroutineMetrics">Coroutine metrics interface for recording</param>
        /// <param name="messageBus">Message bus for sending messages</param>
        /// <returns>A new coroutine profiler session with appropriate tag</returns>
        public static CoroutineProfilerSession Create(
            string operationType,
            Guid runnerId, 
            string runnerName, 
            int coroutineId, 
            string coroutineTag,
            ICoroutineMetrics coroutineMetrics,
            IMessageBus messageBus = null)
        {
            if (string.IsNullOrEmpty(operationType))
                operationType = "Unknown";

            // Choose the most specific tag available using hierarchy
            ProfilerTag tag = SelectOptimalTag(operationType, runnerId, runnerName, coroutineId, coroutineTag);
            
            return new CoroutineProfilerSession(
                tag, runnerId, runnerName, coroutineId, coroutineTag, coroutineMetrics, messageBus);
        }

        /// <summary>
        /// Factory method for creating sessions with runner interface
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="runner">Coroutine runner instance</param>
        /// <param name="coroutineId">Coroutine identifier</param>
        /// <param name="coroutineTag">Coroutine tag</param>
        /// <param name="coroutineMetrics">Coroutine metrics interface for recording</param>
        /// <param name="messageBus">Message bus for sending messages</param>
        /// <returns>A new coroutine profiler session</returns>
        public static CoroutineProfilerSession CreateFromRunner(
            string operationType,
            object runner,
            int coroutineId,
            string coroutineTag,
            ICoroutineMetrics coroutineMetrics,
            IMessageBus messageBus = null)
        {
            if (runner == null)
            {
                return Create(operationType, Guid.Empty, "Unknown", coroutineId, coroutineTag, coroutineMetrics, messageBus);
            }

            string runnerName = runner.GetType().Name;
            Guid runnerId = CreateDeterministicGuid($"{runnerName}_{runner.GetHashCode()}");
            
            return Create(operationType, runnerId, runnerName, coroutineId, coroutineTag, coroutineMetrics, messageBus);
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
                if (_isNullSession) return 0.0;
                
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
                if (_isNullSession) return 0L;
                
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
            if (string.IsNullOrEmpty(metricName) || _isNullSession)
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
        /// Records additional context about the coroutine operation
        /// </summary>
        /// <param name="memoryAllocated">Memory allocated during operation</param>
        /// <param name="isGCAllocation">Whether allocation was GC allocation</param>
        public void RecordMemoryAllocation(long memoryAllocated, bool isGCAllocation = false)
        {
            RecordMetric("MemoryAllocated", memoryAllocated);
            RecordMetric("IsGCAllocation", isGCAllocation ? 1.0 : 0.0);
        }

        /// <summary>
        /// Records cleanup time for completion operations
        /// </summary>
        /// <param name="cleanupTimeMs">Time spent on cleanup in milliseconds</param>
        public void RecordCleanupTime(double cleanupTimeMs)
        {
            RecordMetric("CleanupTime", cleanupTimeMs);
        }

        /// <summary>
        /// Records execution step information
        /// </summary>
        /// <param name="stepCount">Number of execution steps</param>
        /// <param name="yieldCount">Number of yield operations</param>
        public void RecordExecutionSteps(int stepCount, int yieldCount = 0)
        {
            RecordMetric("StepCount", stepCount);
            if (yieldCount > 0)
                RecordMetric("YieldCount", yieldCount);
        }

        /// <summary>
        /// End the profiler marker and record duration
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed || _isNullSession)
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
        /// Selects the most appropriate ProfilerTag based on available parameters
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="runnerId">Runner identifier</param>
        /// <param name="runnerName">Runner name</param>
        /// <param name="coroutineId">Coroutine identifier</param>
        /// <param name="coroutineTag">Coroutine tag</param>
        /// <returns>The most specific ProfilerTag available</returns>
        private static ProfilerTag SelectOptimalTag(string operationType, Guid runnerId, string runnerName, int coroutineId, string coroutineTag)
        {
            // Priority 1: Coroutine-specific tag (most specific)
            if (coroutineId > 0)
            {
                return CoroutineProfilerTags.ForCoroutine(operationType, coroutineId);
            }
            
            // Priority 2: Tagged coroutine operations
            if (!string.IsNullOrEmpty(coroutineTag))
            {
                return CoroutineProfilerTags.ForTag(operationType, coroutineTag);
            }
            
            // Priority 3: Runner with GUID (more specific than name)
            if (runnerId != Guid.Empty)
            {
                return CoroutineProfilerTags.ForRunner(operationType, runnerId);
            }
            
            // Priority 4: Runner by name
            if (!string.IsNullOrEmpty(runnerName) && runnerName != "Unknown")
            {
                return CoroutineProfilerTags.ForRunnerName(operationType, runnerName);
            }
            
            // Priority 5: Generic operation tag (least specific)
            return CoroutineProfilerTags.ForOperation(operationType);
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
            if (string.IsNullOrEmpty(tagName))
                return "Unknown";
                
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

        /// <summary>
        /// Creates a deterministic GUID from a string
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrEmpty(input))
                return Guid.Empty;
                
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return new Guid(hash);
            }
        }
    }
}