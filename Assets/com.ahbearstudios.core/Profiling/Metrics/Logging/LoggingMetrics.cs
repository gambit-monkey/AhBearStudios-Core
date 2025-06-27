using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling.Data;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Messages;
using AhBearStudios.Core.MessageBus.Interfaces;
using Unity.Collections;
using UnityEngine;
using LogTag = AhBearStudios.Core.Logging.Tags.Tagging.LogTag;

namespace AhBearStudios.Core.Profiling.Metrics.Logging
{
    /// <summary>
    /// Managed implementation of logging metrics tracking.
    /// Provides thread-safe tracking of performance and usage metrics for logging systems.
    /// </summary>
    public class LoggingMetrics : ILoggingMetrics
    {
        // Thread safety
        private readonly ReaderWriterLockSlim _metricsLock;
        
        // Storage
        private readonly Dictionary<Guid, LoggingMetricsData> _loggingSystemMetrics;
        private LoggingMetricsData _globalMetrics;
        
        // Level and tag tracking
        private readonly Dictionary<LogLevel, long> _levelCounts;
        private readonly Dictionary<LogTag, long> _tagCounts;
        private readonly Dictionary<string, long> _targetCounts;
        
        // Alert storage - using simple struct instead of class
        private readonly Dictionary<Guid, Dictionary<string, AlertConfig>> _systemAlerts;
        private readonly Dictionary<string, AlertConfig> _globalAlerts;
        
        // Message bus for alerts
        private readonly IMessageBusService _messageBusService;
        
        // State
        private bool _isCreated;
        private bool _isEnabled;
        
        /// <summary>
        /// Simple alert configuration for logging metrics
        /// </summary>
        private struct AlertConfig
        {
            public string MetricName;
            public double Threshold;
            public string AlertType;
            public float LastTriggeredTime;
            public float CooldownPeriod;
            public float CurrentCooldown;
        }
        
        /// <summary>
        /// Whether the metrics tracker is created and initialized
        /// </summary>
        public bool IsCreated => _isCreated;
        
        /// <summary>
        /// Whether metrics collection is currently enabled
        /// </summary>
        public bool IsEnabled 
        { 
            get => _isEnabled; 
            set => _isEnabled = value; 
        }
        
        /// <summary>
        /// Creates a new logging metrics tracker
        /// </summary>
        /// <param name="messageBusService">Message bus for sending alerts</param>
        /// <param name="initialCapacity">Initial capacity for dictionary storage</param>
        public LoggingMetrics(IMessageBusService messageBusService = null, int initialCapacity = 16)
        {
            // Create storage
            _loggingSystemMetrics = new Dictionary<Guid, LoggingMetricsData>(initialCapacity);
            _systemAlerts = new Dictionary<Guid, Dictionary<string, AlertConfig>>();
            _globalAlerts = new Dictionary<string, AlertConfig>();
            _metricsLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _messageBusService = messageBusService;
    
            // Create tracking dictionaries
            _levelCounts = new Dictionary<LogLevel, long>();
            _tagCounts = new Dictionary<LogTag, long>();
            _targetCounts = new Dictionary<string, long>();
    
            // Initialize global metrics with current time
            float currentTime = GetCurrentTime();
            _globalMetrics = new LoggingMetricsData(
                new FixedString64Bytes("Global"), 
                new FixedString128Bytes("Global Logging System"),
                currentTime);
    
            _isCreated = true;
            _isEnabled = true;
        }
        
        /// <summary>
        /// Creates a new logging metrics tracker with a specific system already configured
        /// </summary>
        /// <param name="systemId">Logging system identifier</param>
        /// <param name="systemName">Logging system name</param>
        /// <param name="messageBusService">Message bus for sending alerts</param>
        public LoggingMetrics(
            Guid systemId,
            string systemName,
            IMessageBusService messageBusService = null)
            : this(messageBusService)
        {
            // Configure the initial logging system
            RegisterLoggingSystem(systemId, systemName);
        }
        
        // ILoggingMetrics Implementation
        
        #region Basic Properties
        
        public long TotalMessagesProcessed => _globalMetrics.TotalMessagesProcessed;
        public long TotalMessagesFailed => _globalMetrics.TotalMessagesFailed;
        public int CurrentQueueSize => _globalMetrics.CurrentQueueSize;
        public int PeakQueueSize => _globalMetrics.PeakQueueSize;
        public double AverageProcessingTimeMs => _globalMetrics.AverageProcessingTimeMs;
        public double PeakProcessingTimeMs => _globalMetrics.PeakProcessingTimeMs;
        public int LastBatchSize => _globalMetrics.LastBatchSize;
        public double AverageBatchProcessingTimeMs => _globalMetrics.AverageBatchProcessingTimeMs;
        public long TotalFlushOperations => _globalMetrics.TotalFlushOperations;
        public long FailedFlushOperations => _globalMetrics.FailedFlushOperations;
        public double AverageFlushTimeMs => _globalMetrics.AverageFlushTimeMs;
        public int ActiveTargetCount => _globalMetrics.ActiveTargetCount;
        public long TotalTargetFailures => _globalMetrics.TotalTargetFailures;
        public long MemoryUsageBytes => _globalMetrics.MemoryUsageBytes;
        public long PeakMemoryUsageBytes => _globalMetrics.PeakMemoryUsageBytes;
        
        #endregion
        
        #region Recording Methods
        
        /// <summary>
        /// Records metrics for a log message processing operation
        /// </summary>
        public void RecordMessageProcessing(LogLevel level, LogTag tag, TimeSpan processingTime, bool success, int messageSize = 0)
        {
            if (!_isEnabled)
                return;
                
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Update global metrics
                _globalMetrics = _globalMetrics.RecordMessageProcessing(level, tag, processingTime, success, messageSize);
                
                // Update level counts
                if (_levelCounts.TryGetValue(level, out var levelCount))
                    _levelCounts[level] = levelCount + 1;
                else
                    _levelCounts[level] = 1;
                
                // Update tag counts
                if (_tagCounts.TryGetValue(tag, out var tagCount))
                    _tagCounts[tag] = tagCount + 1;
                else
                    _tagCounts[tag] = 1;
                
                // Update operation time
                _globalMetrics = _globalMetrics.UpdateOperationTime(GetCurrentTime());
                
                // Check global alerts
                CheckGlobalAlerts();
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Records metrics for a batch processing operation
        /// </summary>
        public void RecordBatchProcessing(int batchSize, TimeSpan processingTime, int successCount, int failureCount)
        {
            if (!_isEnabled)
                return;
                
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Update global metrics
                _globalMetrics = _globalMetrics.RecordBatchProcessing(batchSize, processingTime, successCount, failureCount);
                
                // Update operation time
                _globalMetrics = _globalMetrics.UpdateOperationTime(GetCurrentTime());
                
                // Check global alerts
                CheckGlobalAlerts();
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Records metrics for a flush operation
        /// </summary>
        public void RecordFlushOperation(TimeSpan flushTime, int messagesFlushed, bool success)
        {
            if (!_isEnabled)
                return;
                
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Update global metrics
                _globalMetrics = _globalMetrics.RecordFlushOperation(flushTime, messagesFlushed, success);
                
                // Update operation time
                _globalMetrics = _globalMetrics.UpdateOperationTime(GetCurrentTime());
                
                // Check global alerts
                CheckGlobalAlerts();
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Records metrics for log target operations
        /// </summary>
        public void RecordTargetOperation(string targetName, string operationType, TimeSpan duration, bool success, int dataSize = 0)
        {
            if (!_isEnabled)
                return;
                
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Update global metrics
                _globalMetrics = _globalMetrics.RecordTargetOperation(targetName, operationType, duration, success, dataSize);
                
                // Update target counts
                if (!string.IsNullOrEmpty(targetName))
                {
                    if (_targetCounts.TryGetValue(targetName, out var targetCount))
                        _targetCounts[targetName] = targetCount + 1;
                    else
                        _targetCounts[targetName] = 1;
                }
                
                // Update operation time
                _globalMetrics = _globalMetrics.UpdateOperationTime(GetCurrentTime());
                
                // Check global alerts
                CheckGlobalAlerts();
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Updates the current queue size metrics
        /// </summary>
        public void UpdateQueueMetrics(int currentSize, int capacity)
        {
            if (!_isEnabled)
                return;
                
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Update global metrics
                _globalMetrics = _globalMetrics.UpdateQueueMetrics(currentSize, capacity);
                
                // Update operation time
                _globalMetrics = _globalMetrics.UpdateOperationTime(GetCurrentTime());
                
                // Check global alerts
                CheckGlobalAlerts();
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Records memory usage for the logging system
        /// </summary>
        public void RecordMemoryUsage(long memoryUsageBytes)
        {
            if (!_isEnabled)
                return;
                
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Update global metrics
                _globalMetrics = _globalMetrics.RecordMemoryUsage(memoryUsageBytes);
                
                // Update operation time
                _globalMetrics = _globalMetrics.UpdateOperationTime(GetCurrentTime());
                
                // Check global alerts
                CheckGlobalAlerts();
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        #endregion
        
        #region Query Methods
        
        /// <summary>
        /// Gets metrics for a specific log level
        /// </summary>
        public Dictionary<string, object> GetLevelMetrics(LogLevel level)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                var result = new Dictionary<string, object>();
                
                if (_levelCounts.TryGetValue(level, out var count))
                {
                    result["Level"] = level.ToString();
                    result["MessageCount"] = count;
                    result["Percentage"] = TotalMessagesProcessed > 0 ? 
                        (count / (double)TotalMessagesProcessed) * 100.0 : 0.0;
                }
                else
                {
                    result["Level"] = level.ToString();
                    result["MessageCount"] = 0L;
                    result["Percentage"] = 0.0;
                }
                
                return result;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets metrics for a specific log tag
        /// </summary>
        public Dictionary<string, object> GetTagMetrics(LogTag tag)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                var result = new Dictionary<string, object>();
                
                if (_tagCounts.TryGetValue(tag, out var count))
                {
                    result["Tag"] = tag.ToString();
                    result["MessageCount"] = count;
                    result["Percentage"] = TotalMessagesProcessed > 0 ? 
                        (count / (double)TotalMessagesProcessed) * 100.0 : 0.0;
                }
                else
                {
                    result["Tag"] = tag.ToString();
                    result["MessageCount"] = 0L;
                    result["Percentage"] = 0.0;
                }
                
                return result;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets metrics for a specific log target
        /// </summary>
        public Dictionary<string, object> GetTargetMetrics(string targetName)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                var result = new Dictionary<string, object>();
                
                if (!string.IsNullOrEmpty(targetName) && _targetCounts.TryGetValue(targetName, out var count))
                {
                    result["TargetName"] = targetName;
                    result["OperationCount"] = count;
                    result["IsActive"] = true;
                }
                else
                {
                    result["TargetName"] = targetName ?? "Unknown";
                    result["OperationCount"] = 0L;
                    result["IsActive"] = false;
                }
                
                return result;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets all logging metrics as a comprehensive dictionary
        /// </summary>
        public Dictionary<string, object> GetAllMetrics()
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                var result = new Dictionary<string, object>
                {
                    // Basic metrics
                    ["TotalMessagesProcessed"] = TotalMessagesProcessed,
                    ["TotalMessagesFailed"] = TotalMessagesFailed,
                    ["CurrentQueueSize"] = CurrentQueueSize,
                    ["PeakQueueSize"] = PeakQueueSize,
                    ["AverageProcessingTimeMs"] = AverageProcessingTimeMs,
                    ["PeakProcessingTimeMs"] = PeakProcessingTimeMs,
                    ["LastBatchSize"] = LastBatchSize,
                    ["AverageBatchProcessingTimeMs"] = AverageBatchProcessingTimeMs,
                    ["TotalFlushOperations"] = TotalFlushOperations,
                    ["FailedFlushOperations"] = FailedFlushOperations,
                    ["AverageFlushTimeMs"] = AverageFlushTimeMs,
                    ["ActiveTargetCount"] = ActiveTargetCount,
                    ["TotalTargetFailures"] = TotalTargetFailures,
                    ["MemoryUsageBytes"] = MemoryUsageBytes,
                    ["PeakMemoryUsageBytes"] = PeakMemoryUsageBytes,
                    
                    // Calculated metrics
                    ["SuccessRate"] = GetErrorRate(60),
                    ["ErrorRate"] = GetErrorRate(60),
                    ["Throughput"] = GetThroughput(60),
                    ["QueueUtilization"] = GetQueueUtilization(),
                    
                    // State info
                    ["IsEnabled"] = IsEnabled,
                    ["UptimeSeconds"] = _globalMetrics.UptimeSeconds,
                    
                    // Level breakdown
                    ["LevelCounts"] = new Dictionary<string, long>(_levelCounts.ToDictionary(
                        kvp => kvp.Key.ToString(), 
                        kvp => kvp.Value)),
                    
                    // Tag breakdown
                    ["TagCounts"] = new Dictionary<string, long>(_tagCounts.ToDictionary(
                        kvp => kvp.Key.ToString(), 
                        kvp => kvp.Value)),
                    
                    // Target breakdown
                    ["TargetCounts"] = new Dictionary<string, long>(_targetCounts)
                };
                
                return result;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets a performance snapshot suitable for display
        /// </summary>
        public Dictionary<string, string> GetPerformanceSnapshot()
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                var result = new Dictionary<string, string>
                {
                    ["UpTime"] = FormatTimeSpan(_globalMetrics.UptimeSeconds),
                    ["TotalMessages"] = TotalMessagesProcessed.ToString("N0"),
                    ["FailedMessages"] = TotalMessagesFailed.ToString("N0"),
                    ["SuccessRate"] = $"{(100.0 - GetErrorRate()):F1}%",
                    ["ErrorRate"] = $"{GetErrorRate():F1}%",
                    ["Throughput"] = $"{GetThroughput():F1} msg/s",
                    ["CurrentQueue"] = CurrentQueueSize.ToString("N0"),
                    ["PeakQueue"] = PeakQueueSize.ToString("N0"),
                    ["QueueUtilization"] = $"{GetQueueUtilization():F1}%",
                    ["AvgProcessingTime"] = $"{AverageProcessingTimeMs:F2} ms",
                    ["PeakProcessingTime"] = $"{PeakProcessingTimeMs:F2} ms",
                    ["LastBatchSize"] = LastBatchSize.ToString("N0"),
                    ["AvgBatchTime"] = $"{AverageBatchProcessingTimeMs:F2} ms",
                    ["TotalFlushes"] = TotalFlushOperations.ToString("N0"),
                    ["FailedFlushes"] = FailedFlushOperations.ToString("N0"),
                    ["AvgFlushTime"] = $"{AverageFlushTimeMs:F2} ms",
                    ["ActiveTargets"] = ActiveTargetCount.ToString(),
                    ["TargetFailures"] = TotalTargetFailures.ToString("N0"),
                    ["MemoryUsage"] = FormatByteSize(MemoryUsageBytes),
                    ["PeakMemory"] = FormatByteSize(PeakMemoryUsageBytes),
                    ["Status"] = IsEnabled ? "Enabled" : "Disabled"
                };
                
                return result;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets the throughput (messages per second) over the last time period
        /// </summary>
        public double GetThroughput(int timePeriodSeconds = 60)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                return _globalMetrics.GetThroughput(timePeriodSeconds);
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets the error rate (percentage of failed messages) over the last time period
        /// </summary>
        public double GetErrorRate(int timePeriodSeconds = 60)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                return _globalMetrics.GetErrorRate(timePeriodSeconds);
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets the queue utilization as a percentage of capacity
        /// </summary>
        public double GetQueueUtilization()
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                return _globalMetrics.GetQueueUtilization();
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        #endregion
        
        #region Alert Management
        
        /// <summary>
        /// Registers an alert for a specific logging metric
        /// </summary>
        public void RegisterAlert(string metricName, double threshold, string alertType = "above")
        {
            CheckInitialized();
            
            if (string.IsNullOrEmpty(metricName))
                return;
                
            _metricsLock.EnterWriteLock();
            try
            {
                var alert = new AlertConfig 
                { 
                    MetricName = metricName,
                    Threshold = threshold,
                    AlertType = alertType,
                    LastTriggeredTime = 0,
                    CooldownPeriod = 5.0f,
                    CurrentCooldown = 0
                };
                
                _globalAlerts[metricName] = alert;
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Removes a registered alert for a specific metric
        /// </summary>
        public void RemoveAlert(string metricName)
        {
            CheckInitialized();
            
            if (string.IsNullOrEmpty(metricName))
                return;
                
            _metricsLock.EnterWriteLock();
            try
            {
                _globalAlerts.Remove(metricName);
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Gets all currently registered alerts
        /// </summary>
        public Dictionary<string, object> GetRegisteredAlerts()
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                var result = new Dictionary<string, object>();
                
                foreach (var alert in _globalAlerts.Values)
                {
                    result[alert.MetricName] = new Dictionary<string, object>
                    {
                        ["MetricName"] = alert.MetricName,
                        ["Threshold"] = alert.Threshold,
                        ["AlertType"] = alert.AlertType,
                        ["LastTriggered"] = alert.LastTriggeredTime,
                        ["CooldownRemaining"] = alert.CurrentCooldown
                    };
                }
                
                return result;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        #endregion
        
        #region Reset Methods
        
        /// <summary>
        /// Resets all logging metrics to their initial state
        /// </summary>
        public void Reset()
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                float currentTime = GetCurrentTime();
                
                // Reset global metrics
                _globalMetrics = _globalMetrics.Reset(currentTime);
                
                // Clear tracking dictionaries
                _levelCounts.Clear();
                _tagCounts.Clear();
                _targetCounts.Clear();
                
                // Reset all system metrics
                var systemIds = new List<Guid>(_loggingSystemMetrics.Keys);
                foreach (var systemId in systemIds)
                {
                    if (_loggingSystemMetrics.TryGetValue(systemId, out var systemMetrics))
                    {
                        var resetMetrics = systemMetrics.Reset(currentTime);
                        _loggingSystemMetrics[systemId] = resetMetrics;
                    }
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Resets metrics for a specific log level
        /// </summary>
        public void ResetLevelMetrics(LogLevel level)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                _levelCounts.Remove(level);
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Resets metrics for a specific log tag
        /// </summary>
        public void ResetTagMetrics(LogTag tag)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                _tagCounts.Remove(tag);
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Resets metrics for a specific log target
        /// </summary>
        public void ResetTargetMetrics(string targetName)
        {
            CheckInitialized();
            
            if (string.IsNullOrEmpty(targetName))
                return;
                
            _metricsLock.EnterWriteLock();
            try
            {
                _targetCounts.Remove(targetName);
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        #endregion
        
        #region Configuration
        
        /// <summary>
        /// Gets the current configuration of the metrics system
        /// </summary>
        public Dictionary<string, object> GetConfiguration()
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                return new Dictionary<string, object>
                {
                    ["IsEnabled"] = IsEnabled,
                    ["IsCreated"] = IsCreated,
                    ["SystemCount"] = _loggingSystemMetrics.Count,
                    ["AlertCount"] = _globalAlerts.Count,
                    ["HasMessageBus"] = _messageBusService != null,
                    ["UptimeSeconds"] = _globalMetrics.UptimeSeconds
                };
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Updates the configuration of the metrics system
        /// </summary>
        public void UpdateConfiguration(Dictionary<string, object> configuration)
        {
            CheckInitialized();
            
            if (configuration == null)
                return;
                
            _metricsLock.EnterWriteLock();
            try
            {
                if (configuration.TryGetValue("IsEnabled", out var enabledValue) && enabledValue is bool enabled)
                {
                    IsEnabled = enabled;
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        #endregion
        
        #region System Management
        
        /// <summary>
        /// Registers a new logging system for tracking
        /// </summary>
        public void RegisterLoggingSystem(Guid systemId, string systemName)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                if (!_loggingSystemMetrics.ContainsKey(systemId))
                {
                    var systemMetrics = new LoggingMetricsData(
                        new FixedString64Bytes(systemId.ToString()),
                        new FixedString128Bytes(systemName ?? systemId.ToString()),
                        GetCurrentTime());
                    
                    _loggingSystemMetrics.Add(systemId, systemMetrics);
                    
                    // Update global target count
                    _globalMetrics = _globalMetrics.WithActiveTargetCount(_loggingSystemMetrics.Count);
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Gets metrics data for a specific logging system
        /// </summary>
        public LoggingMetricsData GetSystemMetrics(Guid systemId)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                if (_loggingSystemMetrics.TryGetValue(systemId, out var systemMetrics))
                    return systemMetrics;
                
                return default;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets metrics data for all tracked logging systems
        /// </summary>
        public Dictionary<Guid, LoggingMetricsData> GetAllSystemMetrics()
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                var result = new Dictionary<Guid, LoggingMetricsData>(_loggingSystemMetrics.Count);
                foreach (var kvp in _loggingSystemMetrics)
                {
                    result.Add(kvp.Key, kvp.Value);
                }
                
                return result;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Gets the current time in seconds
        /// </summary>
        private static float GetCurrentTime()
        {
#if UNITY_2019_3_OR_NEWER
            return Time.time;
#else
            return (float)DateTime.Now.TimeOfDay.TotalSeconds;
#endif
        }
        
        /// <summary>
        /// Checks if the metrics tracker is initialized
        /// </summary>
        private void CheckInitialized()
        {
            if (!_isCreated)
                throw new InvalidOperationException("LoggingMetrics is not initialized");
        }
        
        /// <summary>
        /// Checks global alerts using existing message types
        /// </summary>
        private void CheckGlobalAlerts()
        {
            if (_messageBusService == null || _globalAlerts.Count == 0)
                return;
                
            float currentTime = GetCurrentTime();
            var alertsToUpdate = new List<string>();
            
            foreach (var kvp in _globalAlerts)
            {
                var alert = kvp.Value;
                
                // Skip if on cooldown
                if (alert.CurrentCooldown > 0)
                    continue;
                    
                // Get the metric value
                double metricValue = GetGlobalMetricValue(alert.MetricName);
                
                // Check threshold
                bool shouldTrigger = alert.AlertType.ToLowerInvariant() == "above" ? 
                    metricValue >= alert.Threshold : 
                    metricValue <= alert.Threshold;
                    
                if (shouldTrigger)
                {
                    // Update alert state
                    alert.LastTriggeredTime = currentTime;
                    alert.CurrentCooldown = alert.CooldownPeriod;
                    alertsToUpdate.Add(kvp.Key);
                    
                    // Create and publish LoggingAlertMessage using existing message type
                    var loggingAlert = new LoggingAlertMessage(
                        ProfilerTag.GameplayUpdate,
                        LogLevel.Warning,
                        alert.MetricName,
                        metricValue,
                        alert.Threshold,
                        alert.AlertType);
                    
                    // Also create MetricAlertMessage for general metric alerts
                    var metricAlert = new MetricAlertMessage(
                        ProfilerTag.GameplayUpdate,
                        metricValue,
                        alert.Threshold);
                    
                    try
                    {
                        // Publish both messages to ensure compatibility
                        var loggingPublisher = _messageBusService.GetPublisher<LoggingAlertMessage>();
                        loggingPublisher?.Publish(loggingAlert);
                        
                        var metricPublisher = _messageBusService.GetPublisher<MetricAlertMessage>();
                        metricPublisher?.Publish(metricAlert);
                    }
                    catch
                    {
                        // Silently handle publication errors
                    }
                }
            }
            
            // Update the alerts that were triggered
            foreach (var alertKey in alertsToUpdate)
            {
                if (_globalAlerts.TryGetValue(alertKey, out var alert))
                {
                    _globalAlerts[alertKey] = alert;
                }
            }
            
            // Update cooldowns
            UpdateAlertCooldowns(1.0f / 30.0f); // Assume ~30fps
        }
        
        /// <summary>
        /// Updates cooldowns for alerts
        /// </summary>
        private void UpdateAlertCooldowns(float deltaTime)
        {
            var alertsToUpdate = new List<string>();
            
            foreach (var kvp in _globalAlerts)
            {
                var alert = kvp.Value;
                if (alert.CurrentCooldown > 0)
                {
                    alert.CurrentCooldown = Math.Max(0, alert.CurrentCooldown - deltaTime);
                    alertsToUpdate.Add(kvp.Key);
                }
            }
            
            // Update the alerts with new cooldown values
            foreach (var alertKey in alertsToUpdate)
            {
                if (_globalAlerts.TryGetValue(alertKey, out var alert))
                {
                    _globalAlerts[alertKey] = alert;
                }
            }
        }
        
        /// <summary>
        /// Gets a global metric value by name
        /// </summary>
        private double GetGlobalMetricValue(string metricName)
        {
            switch (metricName.ToLowerInvariant())
            {
                case "totalmessages":
                case "totalmessagesprocessed": 
                    return TotalMessagesProcessed;
                case "failedmessages":
                case "totalmessagesfailed": 
                    return TotalMessagesFailed;
                case "currentqueue":
                case "currentqueuesize": 
                    return CurrentQueueSize;
                case "peakqueue":
                case "peakqueuesize": 
                    return PeakQueueSize;
                case "processingtime":
                case "averageprocessingtime": 
                    return AverageProcessingTimeMs;
                case "peakprocessingtime": 
                    return PeakProcessingTimeMs;
                case "throughput": 
                    return GetThroughput();
                case "errorrate": 
                    return GetErrorRate();
                case "queueutilization": 
                    return GetQueueUtilization();
                case "memoryusage": 
                    return MemoryUsageBytes;
                case "peakmemory": 
                    return PeakMemoryUsageBytes;
                default: 
                    return 0;
            }
        }
        
        /// <summary>
        /// Formats a time span in seconds to a human-readable format
        /// </summary>
        private string FormatTimeSpan(float seconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            
            if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.TotalHours:F1} h";
                
            if (timeSpan.TotalMinutes >= 1)
                return $"{timeSpan.TotalMinutes:F1} m";
                
            return $"{timeSpan.TotalSeconds:F1} s";
        }
        
        /// <summary>
        /// Formats byte size to a human-readable format
        /// </summary>
        private string FormatByteSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < suffixes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            
            return $"{size:F1} {suffixes[order]}";
        }
        
        #endregion
    }
}