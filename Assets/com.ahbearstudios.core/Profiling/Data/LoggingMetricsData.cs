using System;
using AhBearStudios.Core.Logging;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Logging.Tags;
using LogTag = AhBearStudios.Core.Logging.Tags.Tagging.LogTag;

namespace AhBearStudios.Core.Profiling.Data
{
    /// <summary>
    /// Burst-compatible metrics implementation for logging operations.
    /// Tracks comprehensive logging system performance including message processing,
    /// target operations, queue management, and memory usage.
    /// </summary>
    [BurstCompile]
    [GenerateTestsForBurstCompatibility]
    public struct LoggingMetricsData : ILoggingMetrics, IEquatable<LoggingMetricsData>
    {
        // Basic identification
        public FixedString128Bytes LoggingSystemName;
        public FixedString64Bytes SystemId;
        
        // Core message statistics
        private long _totalMessagesProcessed;
        private long _totalMessagesFailed;
        private int _currentQueueSize;
        private int _peakQueueSize;
        private int _lastBatchSize;
        
        // Processing time metrics (in nanoseconds for precision)
        private long _totalProcessingTimeNs;
        private long _peakProcessingTimeNs;
        private long _totalBatchProcessingTimeNs;
        private int _processingTimeSampleCount;
        private int _batchProcessingCount;
        
        // Flush operation metrics
        private long _totalFlushOperations;
        private long _failedFlushOperations;
        private long _totalFlushTimeNs;
        private int _flushSampleCount;
        
        // Target metrics
        private int _activeTargetCount;
        private long _totalTargetFailures;
        
        // Memory tracking
        private long _memoryUsageBytes;
        private long _peakMemoryUsageBytes;
        
        // Time tracking
        private float _creationTime;
        private float _lastOperationTime;
        private float _lastResetTime;
        
        // Alert thresholds
        private double _processingTimeThresholdMs;
        private double _queueSizeThreshold;
        private double _memoryThresholdBytes;
        
        // Configuration flags
        private bool _isEnabled;
        private bool _isCreated;
        
        /// <summary>
        /// Creates a new LoggingMetricsData with default values
        /// </summary>
        /// <param name="systemId">Logging system identifier</param>
        /// <param name="systemName">Logging system name</param>
        public LoggingMetricsData(FixedString64Bytes systemId, FixedString128Bytes systemName)
        {
            LoggingSystemName = systemName;
            SystemId = systemId;
            
            _totalMessagesProcessed = 0;
            _totalMessagesFailed = 0;
            _currentQueueSize = 0;
            _peakQueueSize = 0;
            _lastBatchSize = 0;
            
            _totalProcessingTimeNs = 0;
            _peakProcessingTimeNs = 0;
            _totalBatchProcessingTimeNs = 0;
            _processingTimeSampleCount = 0;
            _batchProcessingCount = 0;
            
            _totalFlushOperations = 0;
            _failedFlushOperations = 0;
            _totalFlushTimeNs = 0;
            _flushSampleCount = 0;
            
            _activeTargetCount = 0;
            _totalTargetFailures = 0;
            
            _memoryUsageBytes = 0;
            _peakMemoryUsageBytes = 0;
            
            _creationTime = 0;
            _lastOperationTime = 0;
            _lastResetTime = 0;
            
            _processingTimeThresholdMs = 100.0;
            _queueSizeThreshold = 1000;
            _memoryThresholdBytes = 50 * 1024 * 1024; // 50MB default
            
            _isEnabled = true;
            _isCreated = true;
        }
        
        #region ILoggingMetrics Implementation
        
        public long TotalMessagesProcessed => _totalMessagesProcessed;
        public long TotalMessagesFailed => _totalMessagesFailed;
        public int CurrentQueueSize => _currentQueueSize;
        public int PeakQueueSize => _peakQueueSize;
        public int LastBatchSize => _lastBatchSize;
        
        public double AverageProcessingTimeMs => 
            _processingTimeSampleCount > 0 ? 
                (_totalProcessingTimeNs / (double)_processingTimeSampleCount) / 1_000_000.0 : 0.0;
                
        public double PeakProcessingTimeMs => _peakProcessingTimeNs / 1_000_000.0;
        
        public double AverageBatchProcessingTimeMs => 
            _batchProcessingCount > 0 ? 
                (_totalBatchProcessingTimeNs / (double)_batchProcessingCount) / 1_000_000.0 : 0.0;
                
        public long TotalFlushOperations => _totalFlushOperations;
        public long FailedFlushOperations => _failedFlushOperations;
        
        public double AverageFlushTimeMs => 
            _flushSampleCount > 0 ? 
                (_totalFlushTimeNs / (double)_flushSampleCount) / 1_000_000.0 : 0.0;
                
        public int ActiveTargetCount => _activeTargetCount;
        public long TotalTargetFailures => _totalTargetFailures;
        public long MemoryUsageBytes => _memoryUsageBytes;
        public long PeakMemoryUsageBytes => _peakMemoryUsageBytes;
        
        public bool IsCreated => _isCreated;
        public bool IsEnabled 
        { 
            get => _isEnabled; 
            set => _isEnabled = value; 
        }
        
        #endregion
        
        #region Performance Calculations
        
        /// <summary>
        /// Gets the success rate as a percentage (0-100)
        /// </summary>
        public readonly float SuccessRate => 
            _totalMessagesProcessed > 0 ? 
                ((float)(_totalMessagesProcessed - _totalMessagesFailed) / _totalMessagesProcessed) * 100f : 
                100f;
                
        /// <summary>
        /// Gets the queue utilization percentage (0-100)
        /// </summary>
        public readonly float QueueUtilization => 
            _peakQueueSize > 0 ? ((float)_currentQueueSize / _peakQueueSize) * 100f : 0f;
            
        /// <summary>
        /// Gets the flush success rate as a percentage (0-100)
        /// </summary>
        public readonly float FlushSuccessRate => 
            _totalFlushOperations > 0 ? 
                ((float)(_totalFlushOperations - _failedFlushOperations) / _totalFlushOperations) * 100f : 
                100f;
                
        /// <summary>
        /// Gets the memory utilization percentage relative to peak (0-100)
        /// </summary>
        public readonly float MemoryUtilization => 
            _peakMemoryUsageBytes > 0 ? 
                ((float)_memoryUsageBytes / _peakMemoryUsageBytes) * 100f : 0f;
                
        /// <summary>
        /// Gets the system uptime in seconds
        /// </summary>
        public readonly float UptimeSeconds => 
            _creationTime > 0 ? _lastOperationTime - _creationTime : 0f;
            
        #endregion
        
        #region Recording Methods
        
        /// <summary>
        /// Records a message processing operation
        /// </summary>
        public readonly LoggingMetricsData RecordMessageProcessing(
            LogLevel level, 
            LogTag tag, 
            TimeSpan processingTime, 
            bool success, 
            int messageSize = 0)
        {
            var result = this;
            
            result._totalMessagesProcessed++;
            if (!success)
                result._totalMessagesFailed++;
                
            var timeNs = processingTime.Ticks * 100; // Convert to nanoseconds
            result._totalProcessingTimeNs += timeNs;
            result._peakProcessingTimeNs = math.max(result._peakProcessingTimeNs, timeNs);
            result._processingTimeSampleCount++;
            
            if (messageSize > 0)
                result._memoryUsageBytes += messageSize;
                
            return result;
        }
        
        /// <summary>
        /// Records a batch processing operation
        /// </summary>
        public readonly LoggingMetricsData RecordBatchProcessing(
            int batchSize, 
            TimeSpan processingTime, 
            int successCount, 
            int failureCount)
        {
            var result = this;
            
            result._lastBatchSize = batchSize;
            result._batchProcessingCount++;
            
            var timeNs = processingTime.Ticks * 100;
            result._totalBatchProcessingTimeNs += timeNs;
            
            result._totalMessagesProcessed += successCount;
            result._totalMessagesFailed += failureCount;
            
            return result;
        }
        
        /// <summary>
        /// Records a flush operation
        /// </summary>
        public readonly LoggingMetricsData RecordFlushOperation(
            TimeSpan flushTime, 
            int messagesFlushed, 
            bool success)
        {
            var result = this;
            
            result._totalFlushOperations++;
            if (!success)
                result._failedFlushOperations++;
                
            var timeNs = flushTime.Ticks * 100;
            result._totalFlushTimeNs += timeNs;
            result._flushSampleCount++;
            
            return result;
        }
        
        /// <summary>
        /// Records a target operation
        /// </summary>
        public readonly LoggingMetricsData RecordTargetOperation(
            string targetName, 
            string operationType, 
            TimeSpan duration, 
            bool success, 
            int dataSize = 0)
        {
            var result = this;
            
            if (!success)
                result._totalTargetFailures++;
                
            return result;
        }
        
        /// <summary>
        /// Updates queue metrics
        /// </summary>
        public readonly LoggingMetricsData UpdateQueueMetrics(int currentSize, int capacity)
        {
            var result = this;
            result._currentQueueSize = currentSize;
            result._peakQueueSize = math.max(result._peakQueueSize, currentSize);
            return result;
        }
        
        /// <summary>
        /// Records memory usage
        /// </summary>
        public readonly LoggingMetricsData RecordMemoryUsage(long memoryUsageBytes)
        {
            var result = this;
            result._memoryUsageBytes = memoryUsageBytes;
            result._peakMemoryUsageBytes = math.max(result._peakMemoryUsageBytes, memoryUsageBytes);
            return result;
        }
        
        /// <summary>
        /// Updates the last operation time
        /// </summary>
        public readonly LoggingMetricsData UpdateOperationTime(float currentTime)
        {
            var result = this;
            
            if (result._creationTime == 0)
                result._creationTime = currentTime;
                
            result._lastOperationTime = currentTime;
            return result;
        }
        
        #endregion
        
        #region ILoggingMetrics Methods (Not Implemented in Struct)
        
        // These methods are part of the interface but cannot be implemented in a struct
        // They would be implemented by a wrapper class or manager
        
        void ILoggingMetrics.RecordMessageProcessing(LogLevel level, LogTag tag, TimeSpan processingTime, bool success, int messageSize)
        {
            throw new NotImplementedException("Use RecordMessageProcessing that returns updated metrics");
        }
        
        void ILoggingMetrics.RecordBatchProcessing(int batchSize, TimeSpan processingTime, int successCount, int failureCount)
        {
            throw new NotImplementedException("Use RecordBatchProcessing that returns updated metrics");
        }
        
        void ILoggingMetrics.RecordFlushOperation(TimeSpan flushTime, int messagesFlushed, bool success)
        {
            throw new NotImplementedException("Use RecordFlushOperation that returns updated metrics");
        }
        
        void ILoggingMetrics.RecordTargetOperation(string targetName, string operationType, TimeSpan duration, bool success, int dataSize)
        {
            throw new NotImplementedException("Use RecordTargetOperation that returns updated metrics");
        }
        
        void ILoggingMetrics.UpdateQueueMetrics(int currentSize, int capacity)
        {
            throw new NotImplementedException("Use UpdateQueueMetrics that returns updated metrics");
        }
        
        void ILoggingMetrics.RecordMemoryUsage(long memoryUsageBytes)
        {
            throw new NotImplementedException("Use RecordMemoryUsage that returns updated metrics");
        }
        
        // Dictionary-returning methods not suitable for Burst compilation
        public System.Collections.Generic.Dictionary<string, object> GetLevelMetrics(LogLevel level)
        {
            throw new NotImplementedException("Dictionary operations not supported in Burst context");
        }
        
        public System.Collections.Generic.Dictionary<string, object> GetTagMetrics(LogTag tag)
        {
            throw new NotImplementedException("Dictionary operations not supported in Burst context");
        }
        
        public System.Collections.Generic.Dictionary<string, object> GetTargetMetrics(string targetName)
        {
            throw new NotImplementedException("Dictionary operations not supported in Burst context");
        }
        
        public System.Collections.Generic.Dictionary<string, object> GetAllMetrics()
        {
            throw new NotImplementedException("Dictionary operations not supported in Burst context");
        }
        
        public System.Collections.Generic.Dictionary<string, string> GetPerformanceSnapshot()
        {
            throw new NotImplementedException("Dictionary operations not supported in Burst context");
        }
        
        public double GetThroughput(int timePeriodSeconds = 60)
        {
            var uptimeSeconds = UptimeSeconds;
            return uptimeSeconds > 0 ? _totalMessagesProcessed / (double)math.min(uptimeSeconds, timePeriodSeconds) : 0.0;
        }
        
        public double GetErrorRate(int timePeriodSeconds = 60)
        {
            return _totalMessagesProcessed > 0 ? 
                (_totalMessagesFailed / (double)_totalMessagesProcessed) * 100.0 : 0.0;
        }
        
        public double GetQueueUtilization()
        {
            return QueueUtilization;
        }
        
        public void RegisterAlert(string metricName, double threshold, string alertType = "above")
        {
            throw new NotImplementedException("Alert registration not supported in Burst context");
        }
        
        public void RemoveAlert(string metricName)
        {
            throw new NotImplementedException("Alert removal not supported in Burst context");
        }
        
        public System.Collections.Generic.Dictionary<string, object> GetRegisteredAlerts()
        {
            throw new NotImplementedException("Dictionary operations not supported in Burst context");
        }
        
        public void Reset()
        {
            throw new NotImplementedException("Use Reset(float currentTime) that returns updated metrics");
        }
        
        public void ResetLevelMetrics(LogLevel level)
        {
            throw new NotImplementedException("Level-specific reset not supported in Burst context");
        }
        
        public void ResetTagMetrics(LogTag tag)
        {
            throw new NotImplementedException("Tag-specific reset not supported in Burst context");
        }
        
        public void ResetTargetMetrics(string targetName)
        {
            throw new NotImplementedException("Target-specific reset not supported in Burst context");
        }
        
        public System.Collections.Generic.Dictionary<string, object> GetConfiguration()
        {
            throw new NotImplementedException("Dictionary operations not supported in Burst context");
        }
        
        public void UpdateConfiguration(System.Collections.Generic.Dictionary<string, object> configuration)
        {
            throw new NotImplementedException("Dictionary operations not supported in Burst context");
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Returns a new LoggingMetricsData with reset statistics
        /// </summary>
        public readonly LoggingMetricsData Reset(float currentTime)
        {
            return new LoggingMetricsData(SystemId, LoggingSystemName)
            {
                _activeTargetCount = _activeTargetCount,
                _lastResetTime = currentTime,
                _creationTime = _creationTime,
                _isEnabled = _isEnabled,
                _isCreated = _isCreated,
                _processingTimeThresholdMs = _processingTimeThresholdMs,
                _queueSizeThreshold = _queueSizeThreshold,
                _memoryThresholdBytes = _memoryThresholdBytes
            };
        }
        
        /// <summary>
        /// Updates the active target count
        /// </summary>
        public readonly LoggingMetricsData WithActiveTargetCount(int count)
        {
            var result = this;
            result._activeTargetCount = count;
            return result;
        }
        
        /// <summary>
        /// Sets configuration thresholds
        /// </summary>
        public readonly LoggingMetricsData WithThresholds(
            double processingTimeMs = 100.0, 
            double queueSize = 1000, 
            double memoryBytes = 50 * 1024 * 1024)
        {
            var result = this;
            result._processingTimeThresholdMs = processingTimeMs;
            result._queueSizeThreshold = queueSize;
            result._memoryThresholdBytes = memoryBytes;
            return result;
        }
        
        #endregion
        
        #region IEquatable Implementation
        
        public bool Equals(LoggingMetricsData other)
        {
            return SystemId.Equals(other.SystemId) &&
                   LoggingSystemName.Equals(other.LoggingSystemName) &&
                   _totalMessagesProcessed == other._totalMessagesProcessed &&
                   _totalMessagesFailed == other._totalMessagesFailed &&
                   _currentQueueSize == other._currentQueueSize;
        }
        
        public override int GetHashCode()
        {
            return SystemId.GetHashCode();
        }
        
        #endregion
    }
    
    /// <summary>
    /// Represents a key-value pair for logging metrics in native collections
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct LoggingMetricsKeyValuePair
    {
        public FixedString64Bytes SystemId;
        public LoggingMetricsData Metrics;
    }
}