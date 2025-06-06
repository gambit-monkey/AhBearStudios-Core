using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using AhBearStudios.Core.Profiling.Data;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Messages;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Core.Profiling.Metrics
{
    /// <summary>
    /// Implementation of coroutine metrics tracking.
    /// Provides thread-safe tracking of performance and usage metrics for coroutine runners.
    /// </summary>
    public class CoroutineMetrics : ICoroutineMetrics, IDisposable
    {
        // Thread safety
        private readonly ReaderWriterLockSlim _metricsLock;
        
        // Storage
        private readonly Dictionary<Guid, CoroutineMetricsData> _runnerMetrics;
        private readonly Dictionary<Guid, Dictionary<string, int>> _tagCounts;
        private CoroutineMetricsData _globalMetrics;
        
        // Alert storage
        private readonly Dictionary<Guid, Dictionary<string, MetricAlert>> _runnerAlerts;
        
        // Message bus for alerts
        private readonly IMessageBus _messageBus;
        
        // State
        private bool _isCreated;
        private bool _isDisposed;
        
        /// <summary>
        /// Whether the metrics tracker is created and initialized
        /// </summary>
        public bool IsCreated => _isCreated && !_isDisposed;
        
        /// <summary>
        /// Creates a new coroutine metrics tracker
        /// </summary>
        /// <param name="messageBus">Message bus for sending alerts</param>
        /// <param name="initialCapacity">Initial capacity for dictionary storage</param>
        public CoroutineMetrics(IMessageBus messageBus = null, int initialCapacity = 32)
        {
            // Create storage
            _runnerMetrics = new Dictionary<Guid, CoroutineMetricsData>(initialCapacity);
            _tagCounts = new Dictionary<Guid, Dictionary<string, int>>();
            _runnerAlerts = new Dictionary<Guid, Dictionary<string, MetricAlert>>();
            _metricsLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _messageBus = messageBus;
            
            // Initialize global metrics
            float currentTime = GetCurrentTime();
            _globalMetrics = new CoroutineMetricsData(default, new FixedString128Bytes("Global"))
            {
                CreationTime = currentTime,
                LastResetTime = currentTime
            };
            
            _isCreated = true;
        }
        
        // ICoroutineMetrics Implementation
        
        /// <summary>
        /// Gets metrics data for a specific coroutine runner
        /// </summary>
        public CoroutineMetricsData GetMetricsData(Guid runnerId)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                if (_runnerMetrics.TryGetValue(runnerId, out var metricsData))
                    return metricsData;
                
                return default;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets metrics data for a specific runner with nullable return for error handling
        /// </summary>
        public CoroutineMetricsData? GetRunnerMetrics(Guid runnerId)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                if (_runnerMetrics.TryGetValue(runnerId, out var metricsData))
                    return metricsData;
                
                return null;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets global metrics data aggregated across all runners
        /// </summary>
        public CoroutineMetricsData GetGlobalMetricsData()
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                return _globalMetrics;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Records a coroutine start operation for a runner
        /// </summary>
        public void RecordStart(Guid runnerId, float startupTimeMs, bool hasTag = false)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure runner exists
                EnsureRunnerMetricsExists(runnerId);
                
                // Update metrics
                if (_runnerMetrics.TryGetValue(runnerId, out var metricsData))
                {
                    float currentTime = GetCurrentTime();
                    var updatedMetrics = metricsData.RecordStart(startupTimeMs, hasTag, currentTime);
                    _runnerMetrics[runnerId] = updatedMetrics;
                    
                    // Update global metrics
                    UpdateGlobalMetrics();
                    
                    // Check alerts
                    CheckAlerts(runnerId, updatedMetrics);
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Records a coroutine completion for a runner
        /// </summary>
        public void RecordCompletion(Guid runnerId, float executionTimeMs, float cleanupTimeMs = 0f, bool hasTag = false)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure runner exists
                EnsureRunnerMetricsExists(runnerId);
                
                // Update metrics
                if (_runnerMetrics.TryGetValue(runnerId, out var metricsData))
                {
                    float currentTime = GetCurrentTime();
                    var updatedMetrics = metricsData.RecordCompletion(executionTimeMs, cleanupTimeMs, hasTag, currentTime);
                    _runnerMetrics[runnerId] = updatedMetrics;
                    
                    // Update global metrics
                    UpdateGlobalMetrics();
                    
                    // Check alerts
                    CheckAlerts(runnerId, updatedMetrics);
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Records a coroutine cancellation for a runner
        /// </summary>
        public void RecordCancellation(Guid runnerId, bool hasTag = false)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure runner exists
                EnsureRunnerMetricsExists(runnerId);
                
                // Update metrics
                if (_runnerMetrics.TryGetValue(runnerId, out var metricsData))
                {
                    float currentTime = GetCurrentTime();
                    var updatedMetrics = metricsData.RecordCancellation(hasTag, currentTime);
                    _runnerMetrics[runnerId] = updatedMetrics;
                    
                    // Update global metrics
                    UpdateGlobalMetrics();
                    
                    // Check alerts
                    CheckAlerts(runnerId, updatedMetrics);
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Records a coroutine failure for a runner
        /// </summary>
        public void RecordFailure(Guid runnerId, bool hasTag = false, bool isTimeout = false)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure runner exists
                EnsureRunnerMetricsExists(runnerId);
                
                // Update metrics
                if (_runnerMetrics.TryGetValue(runnerId, out var metricsData))
                {
                    float currentTime = GetCurrentTime();
                    var updatedMetrics = metricsData.RecordFailure(hasTag, isTimeout, currentTime);
                    _runnerMetrics[runnerId] = updatedMetrics;
                    
                    // Update global metrics
                    UpdateGlobalMetrics();
                    
                    // Check alerts
                    CheckAlerts(runnerId, updatedMetrics);
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Updates runner configuration and metadata
        /// </summary>
        public void UpdateRunnerConfiguration(Guid runnerId, string runnerName, string runnerType = null, int estimatedOverheadBytes = 0)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Get or create runner metrics
                CoroutineMetricsData metricsData;
                bool isNewRunner = false;
                
                if (!_runnerMetrics.TryGetValue(runnerId, out metricsData))
                {
                    // Create new runner metrics
                    var runnerIdStr = runnerId.ToString();
                    var name = !string.IsNullOrEmpty(runnerName) ? runnerName : runnerIdStr;
                    
                    metricsData = new CoroutineMetricsData(
                        new FixedString64Bytes(runnerIdStr), 
                        new FixedString128Bytes(name))
                    {
                        CreationTime = GetCurrentTime(),
                        LastResetTime = GetCurrentTime()
                    };
                    
                    isNewRunner = true;
                }
                
                // Update configuration values
                if (!string.IsNullOrEmpty(runnerName))
                    metricsData.RunnerName = new FixedString128Bytes(runnerName);
                
                if (!string.IsNullOrEmpty(runnerType))
                    metricsData.RunnerType = new FixedString64Bytes(runnerType);
                
                if (estimatedOverheadBytes > 0)
                    metricsData.EstimatedCoroutineOverheadBytes = estimatedOverheadBytes;
                
                // Store updated metrics
                _runnerMetrics[runnerId] = metricsData;
                
                // Update global metrics
                UpdateGlobalMetrics();
                
                // Check alerts
                CheckAlerts(runnerId, metricsData);
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Gets metrics data for all tracked runners
        /// </summary>
        public Dictionary<Guid, CoroutineMetricsData> GetAllRunnerMetrics()
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                // Create a copy to avoid returning the internal dictionary
                var result = new Dictionary<Guid, CoroutineMetricsData>(_runnerMetrics.Count);
                foreach (var kvp in _runnerMetrics)
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
        
        /// <summary>
        /// Reset statistics for a specific runner
        /// </summary>
        public void ResetRunnerStats(Guid runnerId)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                if (_runnerMetrics.TryGetValue(runnerId, out var metricsData))
                {
                    var resetMetrics = metricsData.Reset(GetCurrentTime());
                    _runnerMetrics[runnerId] = resetMetrics;
                    
                    // Clear tag counts for this runner
                    if (_tagCounts.ContainsKey(runnerId))
                        _tagCounts[runnerId].Clear();
                    
                    // Update global metrics
                    UpdateGlobalMetrics();
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Reset statistics for all runners
        /// </summary>
        public void ResetAllRunnerStats()
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                float currentTime = GetCurrentTime();
                var runnerIds = new List<Guid>(_runnerMetrics.Keys);
                
                foreach (var runnerId in runnerIds)
                {
                    if (_runnerMetrics.TryGetValue(runnerId, out var metricsData))
                    {
                        var resetMetrics = metricsData.Reset(currentTime);
                        _runnerMetrics[runnerId] = resetMetrics;
                    }
                }
                
                // Clear all tag counts
                _tagCounts.Clear();
                
                // Reset global metrics
                _globalMetrics = _globalMetrics.Reset(currentTime);
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Reset all statistics (alias for ResetAllRunnerStats)
        /// </summary>
        public void ResetStats()
        {
            ResetAllRunnerStats();
        }
        
        /// <summary>
        /// Gets the success rate for a specific runner
        /// </summary>
        public float GetRunnerSuccessRate(Guid runnerId)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                if (_runnerMetrics.TryGetValue(runnerId, out var metricsData))
                    return metricsData.SuccessRate;
                
                return 0;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets the overall efficiency for a specific runner
        /// </summary>
        public float GetRunnerEfficiency(Guid runnerId)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                if (_runnerMetrics.TryGetValue(runnerId, out var metricsData))
                    return metricsData.RunnerEfficiency;
                
                return 0;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Records tag usage for a runner
        /// </summary>
        public void RecordTagUsage(Guid runnerId, string tagName, bool increment = true)
        {
            if (string.IsNullOrEmpty(tagName))
                return;
                
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure runner exists
                EnsureRunnerMetricsExists(runnerId);
                
                // Get or create tag counts for this runner
                if (!_tagCounts.TryGetValue(runnerId, out var tagCounts))
                {
                    tagCounts = new Dictionary<string, int>();
                    _tagCounts[runnerId] = tagCounts;
                }
                
                // Update tag count
                if (!tagCounts.TryGetValue(tagName, out var currentCount))
                    currentCount = 0;
                
                if (increment)
                    currentCount++;
                else
                    currentCount = Math.Max(0, currentCount - 1);
                
                tagCounts[tagName] = currentCount;
                
                // Update unique tag count in metrics
                if (_runnerMetrics.TryGetValue(runnerId, out var metricsData))
                {
                    metricsData.UniqueTagCount = tagCounts.Count;
                    _runnerMetrics[runnerId] = metricsData;
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Records memory allocation for coroutines
        /// </summary>
        public void RecordMemoryAllocation(Guid runnerId, long allocatedBytes, bool isGCAllocation = false)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure runner exists
                EnsureRunnerMetricsExists(runnerId);
                
                // Update metrics
                if (_runnerMetrics.TryGetValue(runnerId, out var metricsData))
                {
                    metricsData.TotalMemoryBytes += allocatedBytes;
                    metricsData.PeakMemoryBytes = Math.Max(metricsData.PeakMemoryBytes, metricsData.TotalMemoryBytes);
                    
                    if (isGCAllocation)
                        metricsData.TotalGCAllocations += allocatedBytes;
                    
                    // Update last operation time
                    metricsData.LastOperationTime = GetCurrentTime();
                    
                    // Store updated metrics
                    _runnerMetrics[runnerId] = metricsData;
                    
                    // Check alerts
                    CheckAlerts(runnerId, metricsData);
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Gets a performance snapshot of a specific runner suitable for display
        /// </summary>
        public Dictionary<string, string> GetPerformanceSnapshot(Guid runnerId)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                var result = new Dictionary<string, string>();
                
                if (_runnerMetrics.TryGetValue(runnerId, out var metricsData))
                {
                    // Add general info
                    result["Name"] = metricsData.RunnerName.ToString();
                    result["Type"] = metricsData.RunnerType.ToString();
                    result["UpTime"] = FormatTimeSpan(metricsData.UpTimeSeconds);
                    
                    // Add coroutine counts
                    result["ActiveCoroutines"] = metricsData.ActiveCoroutines.ToString();
                    result["TotalStarted"] = metricsData.TotalCoroutinesStarted.ToString();
                    result["TotalCompleted"] = metricsData.TotalCoroutinesCompleted.ToString();
                    result["TotalCancelled"] = metricsData.TotalCoroutinesCancelled.ToString();
                    result["TotalFailed"] = metricsData.TotalCoroutinesFailed.ToString();
                    result["PeakActive"] = metricsData.PeakActiveCoroutines.ToString();
                    
                    // Add performance metrics
                    result["AvgExecutionTime"] = $"{metricsData.AverageExecutionTimeMs:F2} ms";
                    result["AvgStartupTime"] = $"{metricsData.AverageStartupTimeMs:F2} ms";
                    result["AvgCleanupTime"] = $"{metricsData.AverageCleanupTimeMs:F2} ms";
                    result["CoroutinesPerSec"] = $"{metricsData.CoroutinesPerSecond:F1}";
                    result["SuccessRate"] = $"{metricsData.SuccessRate:P1}";
                    result["Efficiency"] = $"{metricsData.RunnerEfficiency:P1}";
                    
                    // Add tag info
                    result["TaggedCoroutines"] = metricsData.TaggedCoroutines.ToString();
                    result["UntaggedCoroutines"] = metricsData.UntaggedCoroutines.ToString();
                    result["UniqueTagCount"] = metricsData.UniqueTagCount.ToString();
                    
                    // Add memory metrics
                    if (metricsData.TotalMemoryBytes > 0)
                    {
                        result["MemoryUsage"] = FormatByteSize(metricsData.TotalMemoryBytes);
                        result["PeakMemory"] = FormatByteSize(metricsData.PeakMemoryBytes);
                        result["GCAllocations"] = FormatByteSize(metricsData.TotalGCAllocations);
                    }
                    
                    // Add error counts
                    result["Exceptions"] = metricsData.ExceptionCount.ToString();
                    result["Timeouts"] = metricsData.TimeoutCount.ToString();
                }
                
                return result;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Register an alert for a specific runner metric
        /// </summary>
        public void RegisterAlert(Guid runnerId, string metricName, double threshold)
        {
            CheckInitialized();
            
            if (string.IsNullOrEmpty(metricName))
                return;
                
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure runner exists
                EnsureRunnerMetricsExists(runnerId);
                
                // Get or create alerts dictionary for this runner
                if (!_runnerAlerts.TryGetValue(runnerId, out var runnerAlertDict))
                {
                    runnerAlertDict = new Dictionary<string, MetricAlert>();
                    _runnerAlerts[runnerId] = runnerAlertDict;
                }
                
                // Add or update the alert
                var alert = new MetricAlert 
                { 
                    RunnerId = runnerId,
                    MetricName = metricName,
                    Threshold = threshold,
                    LastTriggeredTime = 0,
                    CooldownPeriod = 5.0f, // 5 second default cooldown
                    CurrentCooldown = 0
                };
                
                runnerAlertDict[metricName] = alert;
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        // Helper methods
        
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
        /// Ensures a runner metrics record exists
        /// </summary>
        private void EnsureRunnerMetricsExists(Guid runnerId)
        {
            // This should only be called from within a write lock
            
            if (!_runnerMetrics.ContainsKey(runnerId))
            {
                var runnerIdStr = runnerId.ToString();
                var metricsData = new CoroutineMetricsData(
                    new FixedString64Bytes(runnerIdStr),
                    new FixedString128Bytes(runnerIdStr))
                {
                    CreationTime = GetCurrentTime(),
                    LastResetTime = GetCurrentTime()
                };
                
                _runnerMetrics.Add(runnerId, metricsData);
            }
        }
        
        /// <summary>
        /// Updates global metrics by aggregating all runner metrics
        /// </summary>
        private void UpdateGlobalMetrics()
        {
            // This should only be called from within a write lock
            
            float currentTime = GetCurrentTime();
            
            // Reset counters we're going to recalculate
            _globalMetrics.ActiveCoroutines = 0;
            _globalMetrics.TotalCoroutinesStarted = 0;
            _globalMetrics.TotalCoroutinesCompleted = 0;
            _globalMetrics.TotalCoroutinesCancelled = 0;
            _globalMetrics.TotalCoroutinesFailed = 0;
            _globalMetrics.TaggedCoroutines = 0;
            _globalMetrics.UntaggedCoroutines = 0;
            _globalMetrics.TotalMemoryBytes = 0;
            _globalMetrics.TotalGCAllocations = 0;
            _globalMetrics.ExceptionCount = 0;
            _globalMetrics.TimeoutCount = 0;
            
            // Update time metrics
            _globalMetrics.LastOperationTime = currentTime;
            _globalMetrics.UpTimeSeconds = currentTime - _globalMetrics.CreationTime;
            
            // Aggregate metrics from all runners
            int runnerCount = 0;
            float totalAvgExecution = 0;
            float totalAvgStartup = 0;
            float totalAvgCleanup = 0;
            
            foreach (var runnerMetrics in _runnerMetrics.Values)
            {
                runnerCount++;
                
                // Aggregate counts
                _globalMetrics.ActiveCoroutines += runnerMetrics.ActiveCoroutines;
                _globalMetrics.TotalCoroutinesStarted += runnerMetrics.TotalCoroutinesStarted;
                _globalMetrics.TotalCoroutinesCompleted += runnerMetrics.TotalCoroutinesCompleted;
                _globalMetrics.TotalCoroutinesCancelled += runnerMetrics.TotalCoroutinesCancelled;
                _globalMetrics.TotalCoroutinesFailed += runnerMetrics.TotalCoroutinesFailed;
                _globalMetrics.TaggedCoroutines += runnerMetrics.TaggedCoroutines;
                _globalMetrics.UntaggedCoroutines += runnerMetrics.UntaggedCoroutines;
                _globalMetrics.TotalMemoryBytes += runnerMetrics.TotalMemoryBytes;
                _globalMetrics.TotalGCAllocations += runnerMetrics.TotalGCAllocations;
                _globalMetrics.ExceptionCount += runnerMetrics.ExceptionCount;
                _globalMetrics.TimeoutCount += runnerMetrics.TimeoutCount;
                
                // Aggregate averages (we'll divide by runner count later)
                totalAvgExecution += runnerMetrics.AverageExecutionTimeMs;
                totalAvgStartup += runnerMetrics.AverageStartupTimeMs;
                totalAvgCleanup += runnerMetrics.AverageCleanupTimeMs;
                
                // Update peak metrics
                _globalMetrics.PeakActiveCoroutines = Math.Max(_globalMetrics.PeakActiveCoroutines, _globalMetrics.ActiveCoroutines);
                _globalMetrics.PeakMemoryBytes = Math.Max(_globalMetrics.PeakMemoryBytes, _globalMetrics.TotalMemoryBytes);
            }
            
            // Calculate average metrics
            if (runnerCount > 0)
            {
                _globalMetrics.AverageExecutionTimeMs = totalAvgExecution / runnerCount;
                _globalMetrics.AverageStartupTimeMs = totalAvgStartup / runnerCount;
                _globalMetrics.AverageCleanupTimeMs = totalAvgCleanup / runnerCount;
            }
            
            // Calculate throughput
            if (_globalMetrics.UpTimeSeconds > 0)
                _globalMetrics.CoroutinesPerSecond = _globalMetrics.TotalCoroutinesStarted / _globalMetrics.UpTimeSeconds;
        }
        
        /// <summary>
        /// Checks alerts for a specific runner
        /// </summary>
        private void CheckAlerts(Guid runnerId, CoroutineMetricsData metricsData)
        {
            // Early return if no message bus or no alerts for this runner
            if (_messageBus == null || !_runnerAlerts.TryGetValue(runnerId, out var runnerAlerts))
                return;
                
            float currentTime = GetCurrentTime();
            
            // Check each alert
            foreach (var alert in runnerAlerts.Values)
            {
                // Skip if on cooldown
                if (alert.CurrentCooldown > 0)
                    continue;
                    
                // Get the metric value
                double metricValue = GetMetricValue(metricsData, alert.MetricName);
                
                // Check threshold
                if (metricValue >= alert.Threshold)
                {
                    // Trigger alert
                    alert.LastTriggeredTime = currentTime;
                    alert.CurrentCooldown = alert.CooldownPeriod;
                    
                    // Determine severity based on how much the threshold was exceeded
                    var severity = AlertSeverity.Warning;
                    double exceedanceRatio = metricValue / alert.Threshold;
                    if (exceedanceRatio > 2.0)
                        severity = AlertSeverity.Critical;
                    else if (exceedanceRatio > 1.5)
                        severity = AlertSeverity.Error;
                    
                    // Create and publish message
                    var message = new CoroutineMetricAlertMessage(
                        runnerId, 
                        metricsData.RunnerName.ToString(),
                        alert.MetricName, 
                        metricValue, 
                        alert.Threshold,
                        severity);
                        
                    _messageBus.PublishMessage(message);
                }
            }
            
            // Update cooldowns for all alerts
            UpdateAlertCooldowns(runnerId, 1.0f / 30.0f); // Assume approximately 30 fps for cooldown
        }
        
        /// <summary>
        /// Updates cooldowns for alerts
        /// </summary>
        private void UpdateAlertCooldowns(Guid runnerId, float deltaTime)
        {
            if (!_runnerAlerts.TryGetValue(runnerId, out var runnerAlerts))
                return;
                
            foreach (var alert in runnerAlerts.Values)
            {
                if (alert.CurrentCooldown > 0)
                {
                    alert.CurrentCooldown = Math.Max(0, alert.CurrentCooldown - deltaTime);
                }
            }
        }
        
        /// <summary>
        /// Gets a metric value from the metrics data by name
        /// </summary>
        private double GetMetricValue(CoroutineMetricsData metricsData, string metricName)
        {
            switch (metricName.ToLowerInvariant())
            {
                case "activecoroutines":
                case "active": 
                    return metricsData.ActiveCoroutines;
                case "totalstarted": 
                    return metricsData.TotalCoroutinesStarted;
                case "totalcompleted": 
                    return metricsData.TotalCoroutinesCompleted;
                case "totalcancelled": 
                    return metricsData.TotalCoroutinesCancelled;
                case "totalfailed": 
                    return metricsData.TotalCoroutinesFailed;
                case "peakactive": 
                    return metricsData.PeakActiveCoroutines;
                case "avgexecutiontime": 
                    return metricsData.AverageExecutionTimeMs;
                case "avgstartuptime": 
                    return metricsData.AverageStartupTimeMs;
                case "avgcleanuptime": 
                    return metricsData.AverageCleanupTimeMs;
                case "coroutinespersecond": 
                    return metricsData.CoroutinesPerSecond;
                case "successrate": 
                    return metricsData.SuccessRate;
                case "failurerate": 
                    return metricsData.FailureRate;
                case "efficiency": 
                    return metricsData.RunnerEfficiency;
                case "exceptions": 
                    return metricsData.ExceptionCount;
                case "timeouts": 
                    return metricsData.TimeoutCount;
                case "memorybytes": 
                    return metricsData.TotalMemoryBytes;
                case "gcallocations": 
                    return metricsData.TotalGCAllocations;
                default: 
                    return 0;
            }
        }
        
        /// <summary>
        /// Checks if the metrics tracker is initialized
        /// </summary>
        private void CheckInitialized()
        {
            if (!IsCreated)
                throw new InvalidOperationException("CoroutineMetrics is not initialized or has been disposed");
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
        
        /// <summary>
        /// Represents a metric alert for coroutine metrics
        /// </summary>
        private class MetricAlert
        {
            /// <summary>
            /// Runner identifier
            /// </summary>
            public Guid RunnerId;
            
            /// <summary>
            /// Name of the metric
            /// </summary>
            public string MetricName;
            
            /// <summary>
            /// Threshold value
            /// </summary>
            public double Threshold;
            
            /// <summary>
            /// Last time this alert was triggered
            /// </summary>
            public float LastTriggeredTime;
            
            /// <summary>
            /// Cooldown period in seconds
            /// </summary>
            public float CooldownPeriod;
            
            /// <summary>
            /// Current cooldown remaining
            /// </summary>
            public float CurrentCooldown;
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            _metricsLock?.EnterWriteLock();
            try
            {
                // Clear all collections
                _runnerMetrics?.Clear();
                _tagCounts?.Clear();
                _runnerAlerts?.Clear();
                
                _isCreated = false;
                _isDisposed = true;
            }
            finally
            {
                _metricsLock?.ExitWriteLock();
                _metricsLock?.Dispose();
            }
        }
    }
}