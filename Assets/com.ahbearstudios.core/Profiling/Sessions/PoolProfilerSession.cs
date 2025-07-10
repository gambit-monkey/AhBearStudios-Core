
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
    /// A specialized profiler session for pool operations that captures additional pool metrics.
    /// Implements intelligent tag selection based on available parameters for optimal profiling granularity.
    /// </summary>
    public class PoolProfilerSession : IProfilerSession
    {
        private readonly ProfilerMarker _marker;
        private readonly ProfilerTag _tag;
        private readonly IMessageBusService _messageBusService;
        private readonly Dictionary<string, double> _customMetrics = new Dictionary<string, double>();
        private bool _isDisposed;
        private long _startTimeNs;
        private long _endTimeNs;
        private readonly Guid _sessionId;
        private readonly bool _isNullSession;
        
        /// <summary>
        /// Pool identifier
        /// </summary>
        public readonly Guid PoolId;
        
        /// <summary>
        /// Pool name
        /// </summary>
        public readonly string PoolName;
        
        /// <summary>
        /// Number of active items at the time of profiling
        /// </summary>
        public readonly int ActiveCount;
        
        /// <summary>
        /// Number of free items at the time of profiling
        /// </summary>
        public readonly int FreeCount;

        /// <summary>
        /// Type of operation being performed
        /// </summary>
        public readonly string OperationType;
        
        /// <summary>
        /// The pool metrics interface for recording metrics
        /// </summary>
        private readonly IPoolMetrics _poolMetrics;
        
        /// <summary>
        /// Whether the operation completed successfully
        /// </summary>
        private bool _success = true;
        
        /// <summary>
        /// Creates a new pool profiler session with explicit tag
        /// </summary>
        /// <param name="tag">Profiler tag</param>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="operationType">Type of operation being performed</param>
        /// <param name="activeCount">Active item count</param>
        /// <param name="freeCount">Free item count</param>
        /// <param name="poolMetrics">Pool metrics interface for recording</param>
        /// <param name="messageBusService">Message bus for sending messages</param>
        public PoolProfilerSession(
            ProfilerTag tag, 
            Guid poolId, 
            string poolName,
            string operationType,
            int activeCount, 
            int freeCount,
            IPoolMetrics poolMetrics,
            IMessageBusService messageBusService = null)
        {
            _tag = tag;
            PoolId = poolId;
            PoolName = poolName ?? string.Empty;
            OperationType = operationType ?? string.Empty;
            ActiveCount = activeCount;
            FreeCount = freeCount;
            _poolMetrics = poolMetrics;
            _messageBusService = messageBusService;
            _isDisposed = false;
            _sessionId = Guid.NewGuid();
            _isNullSession = poolMetrics == null && messageBusService == null;
            
            // Only create marker and start timing if this isn't a null session
            if (!_isNullSession)
            {
                _marker = new ProfilerMarker(_tag.FullName);
                
                // Begin the profiler marker
                _marker.Begin();
                _startTimeNs = GetHighPrecisionTimestampNs();
                
                // Notify via message bus that session started
                if (_messageBusService != null)
                {
                    var message = new PoolProfilerSessionStartedMessage(
                        _tag, _sessionId, PoolId, PoolName, ActiveCount, FreeCount);

                    try
                    {
                        var publisher = _messageBusService.GetPublisher<PoolProfilerSessionStartedMessage>();
                        publisher?.Publish(message);
                    }
                    catch
                    {
                        // Silently handle publication errors during session start
                    }
                }
            }
        }

        /// <summary>
        /// Factory method to create a session with appropriate tag based on parameters.
        /// Uses intelligent tag selection hierarchy for optimal profiling granularity.
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="activeCount">Active item count</param>
        /// <param name="freeCount">Free item count</param>
        /// <param name="poolMetrics">Pool metrics interface for recording</param>
        /// <param name="messageBusService">Message bus for sending messages</param>
        /// <returns>A new pool profiler session with appropriate tag</returns>
        public static PoolProfilerSession Create(
            string operationType,
            Guid poolId,
            string poolName,
            int activeCount,
            int freeCount,
            IPoolMetrics poolMetrics,
            IMessageBusService messageBusService = null)
        {
            if (string.IsNullOrEmpty(operationType))
                operationType = "Unknown";

            // Choose the most specific tag available using hierarchy
            ProfilerTag tag = SelectOptimalTag(operationType, poolId, poolName);

            return new PoolProfilerSession(
                tag, poolId, poolName, operationType, activeCount, freeCount, poolMetrics, messageBusService);
        }

        /// <summary>
        /// Factory method for creating sessions with minimal parameters
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="poolMetrics">Pool metrics interface for recording</param>
        /// <param name="messageBusService">Message bus for sending messages</param>
        /// <returns>A new pool profiler session</returns>
        public static PoolProfilerSession CreateMinimal(
            string operationType,
            string poolName,
            IPoolMetrics poolMetrics,
            IMessageBusService messageBusService = null)
        {
            Guid poolId = string.IsNullOrEmpty(poolName) ? Guid.Empty : CreateDeterministicGuid(poolName);
            
            return Create(operationType, poolId, poolName, 0, 0, poolMetrics, messageBusService);
        }

        /// <summary>
        /// Factory method for creating sessions with just operation type
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="poolMetrics">Pool metrics interface for recording</param>
        /// <param name="messageBusService">Message bus for sending messages</param>
        /// <returns>A new pool profiler session</returns>
        public static PoolProfilerSession CreateGeneric(
            string operationType,
            IPoolMetrics poolMetrics,
            IMessageBusService messageBusService = null)
        {
            var tag = PoolProfilerTags.ForOperation(operationType);
            
            return new PoolProfilerSession(
                tag, Guid.Empty, "Generic", operationType, 0, 0, poolMetrics, messageBusService);
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
        /// Records pool capacity metrics
        /// </summary>
        /// <param name="totalCapacity">Total pool capacity</param>
        /// <param name="utilizationPercentage">Current utilization as percentage</param>
        public void RecordCapacityMetrics(int totalCapacity, double utilizationPercentage)
        {
            RecordMetric("TotalCapacity", totalCapacity);
            RecordMetric("UtilizationPercent", utilizationPercentage);
        }

        /// <summary>
        /// Records pool expansion metrics
        /// </summary>
        /// <param name="oldCapacity">Capacity before expansion</param>
        /// <param name="newCapacity">Capacity after expansion</param>
        /// <param name="expandedBy">Number of items added</param>
        public void RecordExpansion(int oldCapacity, int newCapacity, int expandedBy)
        {
            RecordMetric("OldCapacity", oldCapacity);
            RecordMetric("NewCapacity", newCapacity);
            RecordMetric("ExpandedBy", expandedBy);
        }

        /// <summary>
        /// Records pool shrinking metrics
        /// </summary>
        /// <param name="oldCapacity">Capacity before shrinking</param>
        /// <param name="newCapacity">Capacity after shrinking</param>
        /// <param name="removedCount">Number of items removed</param>
        public void RecordShrinking(int oldCapacity, int newCapacity, int removedCount)
        {
            RecordMetric("OldCapacity", oldCapacity);
            RecordMetric("NewCapacity", newCapacity);
            RecordMetric("RemovedCount", removedCount);
        }

        /// <summary>
        /// Records whether the operation was successful
        /// </summary>
        /// <param name="success">True if the operation was successful</param>
        public void RecordSuccess(bool success)
        {
            _success = success;
            RecordMetric("Success", success ? 1.0 : 0.0);
        }

        /// <summary>
        /// Records an error that occurred during the operation
        /// </summary>
        /// <param name="errorCode">Error code or identifier</param>
        public void RecordError(string errorCode)
        {
            _success = false;
            RecordMetric("Error", 1.0);
            if (!string.IsNullOrEmpty(errorCode))
            {
                RecordMetric($"ErrorCode_{errorCode}", 1.0);
            }
        }

        /// <summary>
        /// Records allocation information
        /// </summary>
        /// <param name="allocatedCount">Number of items allocated</param>
        /// <param name="fromPool">Whether allocation was from pool or created new</param>
        public void RecordAllocation(int allocatedCount, bool fromPool)
        {
            RecordMetric("AllocatedCount", allocatedCount);
            RecordMetric("FromPool", fromPool ? 1.0 : 0.0);
            RecordMetric("NewCreated", fromPool ? 0.0 : 1.0);
        }

        /// <summary>
        /// Records deallocation information
        /// </summary>
        /// <param name="deallocatedCount">Number of items deallocated</param>
        /// <param name="returnedToPool">Whether items were returned to pool or destroyed</param>
        public void RecordDeallocation(int deallocatedCount, bool returnedToPool)
        {
            RecordMetric("DeallocatedCount", deallocatedCount);
            RecordMetric("ReturnedToPool", returnedToPool ? 1.0 : 0.0);
            RecordMetric("Destroyed", returnedToPool ? 0.0 : 1.0);
        }

        /// <summary>
        /// Records pool pressure metrics
        /// </summary>
        /// <param name="pressure">Pool pressure (0.0 to 1.0, where 1.0 is maximum pressure)</param>
        /// <param name="missCount">Number of cache misses</param>
        public void RecordPoolPressure(double pressure, int missCount = 0)
        {
            RecordMetric("PoolPressure", pressure);
            if (missCount > 0)
                RecordMetric("CacheMisses", missCount);
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
            
            // Record pool-specific metrics
            RecordPoolMetrics();
            
            // Notify via message bus that session ended
            if (_messageBusService != null)
            {
                var message = new PoolProfilerSessionCompletedMessage(
                    _tag, _sessionId, PoolId, PoolName, ActiveCount, FreeCount, 
                    ElapsedMilliseconds, _customMetrics, OperationType);

                try
                {
                    var publisher = _messageBusService.GetPublisher<PoolProfilerSessionCompletedMessage>();
                    publisher?.Publish(message);
                }
                catch
                {
                    // Silently handle publication errors during session completion
                }
            }
        }

        /// <summary>
        /// Selects the most appropriate ProfilerTag based on available parameters
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <returns>The most specific ProfilerTag available</returns>
        private static ProfilerTag SelectOptimalTag(string operationType, Guid poolId, string poolName)
        {
            // Priority 1: Pool with GUID (most specific)
            if (poolId != Guid.Empty)
            {
                return PoolProfilerTags.ForPool(operationType, poolId);
            }

            // Priority 2: Pool by name
            if (!string.IsNullOrEmpty(poolName) && poolName != "Unknown" && poolName != "Generic")
            {
                return PoolProfilerTags.ForPoolName(operationType, poolName);
            }

            // Priority 3: Use predefined operation tags (least specific but still meaningful)
            return PoolProfilerTags.ForOperation(operationType);
        }
        
        /// <summary>
        /// Record metrics specific to pool operations
        /// </summary>
        private void RecordPoolMetrics()
        {
            // Only record metrics if we have a pool ID and metrics system
            if (PoolId != Guid.Empty && _poolMetrics != null)
            {
                var durationMs = (float)ElapsedMilliseconds;
                
                // Record appropriate metrics based on operation type
                switch (OperationType.ToLowerInvariant())
                {
                    case "acquire":
                        _poolMetrics.RecordAcquire(PoolId, ActiveCount, durationMs);
                        break;
                        
                    case "release":
                        _poolMetrics.RecordRelease(PoolId, ActiveCount, durationMs);
                        break;
                        
                    case "create":
                        _poolMetrics.RecordCreate(PoolId, FreeCount);
                        break;
                        
                    case "expand":
                        // Get expansion metrics if available
                        int newCapacity = ActiveCount + FreeCount;
                        int oldCapacity = newCapacity;
                        
                        if (_customMetrics.TryGetValue("OldCapacity", out double oldCap))
                            oldCapacity = (int)oldCap;
                        if (_customMetrics.TryGetValue("NewCapacity", out double newCap))
                            newCapacity = (int)newCap;
                            
                        _poolMetrics.RecordResize(PoolId, oldCapacity, newCapacity, durationMs);
                        break;
                        
                    case "shrink":
                        // Similar to expand
                        int newShrinkCapacity = ActiveCount + FreeCount;
                        int oldShrinkCapacity = newShrinkCapacity;
                        
                        if (_customMetrics.TryGetValue("OldCapacity", out double oldShrinkCap))
                            oldShrinkCapacity = (int)oldShrinkCap;
                        if (_customMetrics.TryGetValue("NewCapacity", out double newShrinkCap))
                            newShrinkCapacity = (int)newShrinkCap;
                            
                        _poolMetrics.RecordResize(PoolId, oldShrinkCapacity, newShrinkCapacity, durationMs);
                        break;
                        
                    case "clear":
                        // For clear operations, we can record as a release with high count
                        _poolMetrics.RecordRelease(PoolId, 0, durationMs);
                        break;
                        
                    default:
                        // For unknown operations, try to infer the best metric to record
                        if (ActiveCount > 0)
                            _poolMetrics.RecordAcquire(PoolId, ActiveCount, durationMs);
                        break;
                }
                
                // Update pool configuration with current state
                int totalCapacity = ActiveCount + FreeCount;
                if (totalCapacity > 0)
                {
                    _poolMetrics.UpdatePoolConfiguration(PoolId, totalCapacity);
                }
            }
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