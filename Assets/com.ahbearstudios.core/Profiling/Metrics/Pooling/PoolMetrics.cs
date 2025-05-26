using System;
using System.Collections.Generic;
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
    /// Managed implementation of pool metrics tracking.
    /// Provides thread-safe tracking of performance and usage metrics for object pools.
    /// </summary>
    public class PoolMetrics : IPoolMetrics
    {
        // Thread safety
        private readonly ReaderWriterLockSlim _metricsLock;
        
        // Storage
        private readonly Dictionary<Guid, PoolMetricsData> _poolMetrics;
        private PoolMetricsData _globalMetrics;
        
        // Alert storage
        private readonly Dictionary<Guid, Dictionary<string, MetricAlert>> _poolAlerts;
        
        // Message bus for alerts
        private readonly IMessageBus _messageBus;
        
        // State
        private bool _isCreated;
        
        /// <summary>
        /// Whether the metrics tracker is created and initialized
        /// </summary>
        public bool IsCreated => _isCreated;
        
        /// <summary>
        /// Creates a new pool metrics tracker
        /// </summary>
        /// <param name="messageBus">Message bus for sending alerts</param>
        /// <param name="initialCapacity">Initial capacity for dictionary storage</param>
        public PoolMetrics(IMessageBus messageBus = null, int initialCapacity = 64)
        {
            // Create storage
            _poolMetrics = new Dictionary<Guid, PoolMetricsData>(initialCapacity);
            _poolAlerts = new Dictionary<Guid, Dictionary<string, MetricAlert>>();
            _metricsLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _messageBus = messageBus;
            
            // Initialize global metrics
            float currentTime = GetCurrentTime();
            _globalMetrics = new PoolMetricsData(default, new FixedString128Bytes("Global"))
            {
                CreationTime = currentTime,
                LastResetTime = currentTime
            };
            
            _isCreated = true;
        }
        
        /// <summary>
        /// Creates a new pool metrics tracker with a specific pool already configured
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="poolType">Type of pool</param>
        /// <param name="itemType">Type of items in the pool</param>
        /// <param name="messageBus">Message bus for sending alerts</param>
        /// <param name="estimatedItemSizeBytes">Estimated size of each item in bytes (0 for automatic estimation)</param>
        public PoolMetrics(
            Guid poolId,
            string poolName,
            Type poolType,
            Type itemType,
            IMessageBus messageBus = null,
            int estimatedItemSizeBytes = 0)
            : this(messageBus)
        {
            // Configure the initial pool
            string poolTypeName = poolType != null ? poolType.Name : "Unknown";
            
            // Estimate item size if not provided
            if (estimatedItemSizeBytes <= 0 && itemType != null)
            {
                estimatedItemSizeBytes = EstimateTypeSize(itemType);
            }
            
            UpdatePoolConfiguration(
                poolId,
                0, // Initial capacity will be set when first updated
                0, // No min capacity initially
                0, // No max capacity initially
                poolTypeName,
                estimatedItemSizeBytes);
        }
        
        // IPoolMetrics Implementation
        
        /// <summary>
        /// Gets metrics data for a specific pool
        /// </summary>
        public PoolMetricsData GetMetricsData(Guid poolId)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                    return metricsData;
                
                return default;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets metrics data for a specific pool with nullable return for error handling
        /// </summary>
        public PoolMetricsData? GetPoolMetrics(Guid poolId)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                    return metricsData;
                
                return null;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets global metrics data aggregated across all pools
        /// </summary>
        public PoolMetricsData GetGlobalMetricsData()
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
        /// Records an acquire operation for a pool
        /// </summary>
        public void RecordAcquire(Guid poolId, int activeCount, float acquireTimeMs)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure pool exists
                EnsurePoolMetricsExists(poolId);
                
                // Update metrics
                if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                {
                    float currentTime = GetCurrentTime();
                    var updatedMetrics = metricsData.RecordAcquire(activeCount, acquireTimeMs, currentTime);
                    _poolMetrics[poolId] = updatedMetrics;
                    
                    // Update global metrics
                    UpdateGlobalMetrics();
                    
                    // Check alerts
                    CheckAlerts(poolId, updatedMetrics);
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Records a release operation for a pool
        /// </summary>
        public void RecordRelease(Guid poolId, int activeCount, float releaseTimeMs, float lifetimeSeconds = 0)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure pool exists
                EnsurePoolMetricsExists(poolId);
                
                // Update metrics
                if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                {
                    float currentTime = GetCurrentTime();
                    var updatedMetrics = metricsData.RecordRelease(activeCount, lifetimeSeconds, releaseTimeMs, currentTime);
                    _poolMetrics[poolId] = updatedMetrics;
                    
                    // Update global metrics
                    UpdateGlobalMetrics();
                    
                    // Check alerts
                    CheckAlerts(poolId, updatedMetrics);
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Updates pool capacity and configuration
        /// </summary>
        public void UpdatePoolConfiguration(Guid poolId, int capacity, int minCapacity = 0, int maxCapacity = 0, string poolType = null, int itemSizeBytes = 0)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Get or create pool metrics
                PoolMetricsData metricsData;
                bool isNewPool = false;
                
                if (!_poolMetrics.TryGetValue(poolId, out metricsData))
                {
                    // Create new pool metrics
                    var poolIdStr = poolId.ToString();
                    var poolName = !string.IsNullOrEmpty(poolType) ? poolType : poolIdStr;
                    
                    metricsData = new PoolMetricsData(
                        new FixedString64Bytes(poolIdStr), 
                        new FixedString128Bytes(poolName))
                    {
                        CreationTime = GetCurrentTime(),
                        LastResetTime = GetCurrentTime()
                    };
                    
                    isNewPool = true;
                }
                
                // Update configuration values
                if (capacity > 0)
                {
                    // Only set InitialCapacity for new pools
                    if (isNewPool)
                        metricsData.InitialCapacity = capacity;
                    
                    metricsData = metricsData.WithCapacity(capacity);
                }
                
                if (minCapacity > 0)
                    metricsData.MinCapacity = minCapacity;
                
                if (maxCapacity > 0)
                    metricsData.MaxCapacity = maxCapacity;
                
                if (!string.IsNullOrEmpty(poolType))
                    metricsData.PoolType = new FixedString64Bytes(poolType);
                
                if (itemSizeBytes > 0)
                    metricsData = metricsData.WithItemSize(itemSizeBytes);
                
                // Store updated metrics
                _poolMetrics[poolId] = metricsData;
                
                // Update global metrics
                UpdateGlobalMetrics();
                
                // Check alerts
                CheckAlerts(poolId, metricsData);
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Simplified pool configuration update with just capacity
        /// </summary>
        public void UpdatePoolConfiguration(Guid poolId, int capacity)
        {
            UpdatePoolConfiguration(poolId, capacity, 0, 0, null, 0);
        }
        
        /// <summary>
        /// Gets metrics data for all tracked pools
        /// </summary>
        public Dictionary<Guid, PoolMetricsData> GetAllPoolMetrics()
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                // Create a copy to avoid returning the internal dictionary
                var result = new Dictionary<Guid, PoolMetricsData>(_poolMetrics.Count);
                foreach (var kvp in _poolMetrics)
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
        /// Reset statistics for a specific pool
        /// </summary>
        public void ResetPoolStats(Guid poolId)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                {
                    var resetMetrics = metricsData.Reset(GetCurrentTime());
                    _poolMetrics[poolId] = resetMetrics;
                    
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
        /// Reset statistics for all pools
        /// </summary>
        public void ResetAllPoolStats()
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                float currentTime = GetCurrentTime();
                var poolIds = new List<Guid>(_poolMetrics.Keys);
                
                foreach (var poolId in poolIds)
                {
                    if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                    {
                        var resetMetrics = metricsData.Reset(currentTime);
                        _poolMetrics[poolId] = resetMetrics;
                    }
                }
                
                // Reset global metrics
                _globalMetrics = _globalMetrics.Reset(currentTime);
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Reset all statistics (alias for ResetAllPoolStats)
        /// </summary>
        public void ResetStats()
        {
            ResetAllPoolStats();
        }
        
        /// <summary>
        /// Gets the cache hit ratio for a specific pool
        /// </summary>
        public float GetPoolHitRatio(Guid poolId)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                    return metricsData.CacheHitRatio;
                
                return 0;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Gets the overall efficiency for a specific pool
        /// </summary>
        public float GetPoolEfficiency(Guid poolId)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                    return metricsData.PoolEfficiency;
                
                return 0;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Records a create operation for the pool that adds new items
        /// </summary>
        public void RecordCreate(Guid poolId, int createdCount, long memoryOverheadBytes = 0)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure pool exists
                EnsurePoolMetricsExists(poolId);
                
                // Update metrics
                if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                {
                    // Update creation count
                    metricsData.TotalCreatedCount += createdCount;
                    
                    // Update memory tracking if provided
                    if (memoryOverheadBytes > 0)
                    {
                        metricsData.TotalMemoryBytes += memoryOverheadBytes;
                        metricsData.PeakMemoryBytes = Math.Max(metricsData.PeakMemoryBytes, metricsData.TotalMemoryBytes);
                    }
                    
                    // Update last operation time
                    metricsData.LastOperationTime = GetCurrentTime();
                    
                    // Store updated metrics
                    _poolMetrics[poolId] = metricsData;
                    
                    // Update global metrics
                    UpdateGlobalMetrics();
                    
                    // Check alerts
                    CheckAlerts(poolId, metricsData);
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Records metrics about pool fragmentation
        /// </summary>
        public void RecordFragmentation(Guid poolId, int fragmentCount, float fragmentationRatio)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure pool exists
                EnsurePoolMetricsExists(poolId);
                
                // Update metrics
                if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                {
                    metricsData.FragmentCount = fragmentCount;
                    metricsData.FragmentationRatio = fragmentationRatio;
                    
                    // Update last operation time
                    metricsData.LastOperationTime = GetCurrentTime();
                    
                    // Store updated metrics
                    _poolMetrics[poolId] = metricsData;
                    
                    // Check alerts
                    CheckAlerts(poolId, metricsData);
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Records information about operational success/failure
        /// </summary>
        public void RecordOperationResults(Guid poolId, int acquireSuccessCount = 0, int acquireFailureCount = 0, 
                                         int releaseSuccessCount = 0, int releaseFailureCount = 0)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure pool exists
                EnsurePoolMetricsExists(poolId);
                
                // Update metrics
                if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                {
                    // Update cache hits/misses
                    metricsData.CacheHits += acquireSuccessCount;
                    metricsData.CacheMisses += acquireFailureCount;
                    
                    // Update acquire/release counts
                    if (acquireSuccessCount > 0)
                        metricsData.TotalAcquiredCount += acquireSuccessCount;
                        
                    if (releaseSuccessCount > 0)
                        metricsData.TotalReleasedCount += releaseSuccessCount;
                        
                    // Update last operation time
                    metricsData.LastOperationTime = GetCurrentTime();
                    
                    // Store updated metrics
                    _poolMetrics[poolId] = metricsData;
                    
                    // Update global metrics
                    UpdateGlobalMetrics();
                    
                    // Check alerts
                    CheckAlerts(poolId, metricsData);
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Records a resize operation for the pool
        /// </summary>
        public void RecordResize(Guid poolId, int oldCapacity, int newCapacity, float resizeTimeMs = 0)
        {
            CheckInitialized();
            
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure pool exists
                EnsurePoolMetricsExists(poolId);
                
                // Update metrics
                if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                {
                    // Update capacity
                    metricsData = metricsData.WithCapacity(newCapacity);
                    
                    // Count resize operation
                    metricsData.TotalResizeOperations++;
                    
                    // Track memory changes if we have item size information
                    if (metricsData.EstimatedItemSizeBytes > 0)
                    {
                        int capacityDelta = newCapacity - oldCapacity;
                        long memoryDelta = capacityDelta * metricsData.EstimatedItemSizeBytes;
                        metricsData.TotalMemoryBytes += memoryDelta;
                        metricsData.PeakMemoryBytes = Math.Max(metricsData.PeakMemoryBytes, metricsData.TotalMemoryBytes);
                    }
                    
                    // Update last operation time
                    metricsData.LastOperationTime = GetCurrentTime();
                    
                    // Store updated metrics
                    _poolMetrics[poolId] = metricsData;
                    
                    // Update global metrics
                    UpdateGlobalMetrics();
                    
                    // Check alerts
                    CheckAlerts(poolId, metricsData);
                }
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Gets a performance snapshot of a specific pool suitable for display
        /// </summary>
        public Dictionary<string, string> GetPerformanceSnapshot(Guid poolId)
        {
            CheckInitialized();
            
            _metricsLock.EnterReadLock();
            try
            {
                var result = new Dictionary<string, string>();
                
                if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                {
                    // Add general info
                    result["Name"] = metricsData.Name.ToString();
                    result["Type"] = metricsData.PoolType.ToString();
                    result["UpTime"] = FormatTimeSpan(metricsData.UpTimeSeconds);
                    
                    // Add capacity and usage
                    result["Capacity"] = metricsData.Capacity.ToString();
                    result["ActiveCount"] = metricsData.ActiveCount.ToString();
                    result["FreeCount"] = metricsData.FreeCount.ToString();
                    result["UsageRatio"] = $"{metricsData.UsageRatio:P1}";
                    
                    // Add metrics
                    result["TotalCreated"] = metricsData.TotalCreatedCount.ToString();
                    result["TotalAcquired"] = metricsData.TotalAcquiredCount.ToString();
                    result["TotalReleased"] = metricsData.TotalReleasedCount.ToString();
                    result["LeakedItems"] = metricsData.LeakedItemCount.ToString();
                    result["PeakActive"] = metricsData.PeakActiveCount.ToString();
                    result["Resizes"] = metricsData.TotalResizeOperations.ToString();
                    
                    // Add performance metrics
                    result["AcquireTime"] = $"{metricsData.AverageAcquireTimeMs:F2} ms";
                    result["ReleaseTime"] = $"{metricsData.AverageReleaseTimeMs:F2} ms";
                    result["ItemLifetime"] = $"{metricsData.AverageLifetimeSeconds:F2} s";
                    result["CacheHitRatio"] = $"{metricsData.CacheHitRatio:P1}";
                    result["Efficiency"] = $"{metricsData.PoolEfficiency:P1}";
                    
                    // Add memory metrics
                    if (metricsData.EstimatedItemSizeBytes > 0)
                    {
                        result["MemoryUsage"] = FormatByteSize(metricsData.TotalMemoryBytes);
                        result["PeakMemory"] = FormatByteSize(metricsData.PeakMemoryBytes);
                        result["ItemSize"] = FormatByteSize(metricsData.EstimatedItemSizeBytes);
                    }
                }
                
                return result;
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Register an alert for a specific pool metric
        /// </summary>
        public void RegisterAlert(Guid poolId, string metricName, double threshold)
        {
            CheckInitialized();
            
            if (string.IsNullOrEmpty(metricName))
                return;
                
            _metricsLock.EnterWriteLock();
            try
            {
                // Ensure pool exists
                EnsurePoolMetricsExists(poolId);
                
                // Get or create alerts dictionary for this pool
                if (!_poolAlerts.TryGetValue(poolId, out var poolAlertDict))
                {
                    poolAlertDict = new Dictionary<string, MetricAlert>();
                    _poolAlerts[poolId] = poolAlertDict;
                }
                
                // Add or update the alert
                var alert = new MetricAlert 
                { 
                    PoolId = poolId,
                    MetricName = metricName,
                    Threshold = threshold,
                    LastTriggeredTime = 0,
                    CooldownPeriod = 5.0f, // 5 second default cooldown
                    CurrentCooldown = 0
                };
                
                poolAlertDict[metricName] = alert;
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
        /// Ensures a pool metrics record exists
        /// </summary>
        private void EnsurePoolMetricsExists(Guid poolId)
        {
            // This should only be called from within a write lock
            
            if (!_poolMetrics.ContainsKey(poolId))
            {
                var poolIdStr = poolId.ToString();
                var metricsData = new PoolMetricsData(
                    new FixedString64Bytes(poolIdStr),
                    new FixedString128Bytes(poolIdStr))
                {
                    CreationTime = GetCurrentTime(),
                    LastResetTime = GetCurrentTime()
                };
                
                _poolMetrics.Add(poolId, metricsData);
            }
        }
        
        /// <summary>
        /// Updates global metrics by aggregating all pool metrics
        /// </summary>
        private void UpdateGlobalMetrics()
        {
            // This should only be called from within a write lock
            
            float currentTime = GetCurrentTime();
            
            // Reset counters we're going to recalculate
            _globalMetrics.ActiveCount = 0;
            _globalMetrics.Capacity = 0;
            _globalMetrics.TotalCreatedCount = 0;
            _globalMetrics.TotalAcquiredCount = 0;
            _globalMetrics.TotalReleasedCount = 0;
            _globalMetrics.TotalMemoryBytes = 0;
            _globalMetrics.CacheHits = 0;
            _globalMetrics.CacheMisses = 0;
            _globalMetrics.OverflowAllocations = 0;
            _globalMetrics.TotalResizeOperations = 0;
            
            // Update time metrics
            _globalMetrics.LastOperationTime = currentTime;
            _globalMetrics.UpTimeSeconds = currentTime - _globalMetrics.CreationTime;
            
            // Aggregate metrics from all pools
            foreach (var poolMetrics in _poolMetrics.Values)
            {
                // Aggregate capacity and counts
                _globalMetrics.ActiveCount += poolMetrics.ActiveCount;
                _globalMetrics.Capacity += poolMetrics.Capacity;
                _globalMetrics.TotalCreatedCount += poolMetrics.TotalCreatedCount;
                _globalMetrics.TotalAcquiredCount += poolMetrics.TotalAcquiredCount;
                _globalMetrics.TotalReleasedCount += poolMetrics.TotalReleasedCount;
                _globalMetrics.TotalMemoryBytes += poolMetrics.TotalMemoryBytes;
                _globalMetrics.CacheHits += poolMetrics.CacheHits;
                _globalMetrics.CacheMisses += poolMetrics.CacheMisses;
                _globalMetrics.OverflowAllocations += poolMetrics.OverflowAllocations;
                _globalMetrics.TotalResizeOperations += poolMetrics.TotalResizeOperations;
                
                // Update peak metrics
                _globalMetrics.PeakActiveCount = Math.Max(_globalMetrics.PeakActiveCount, _globalMetrics.ActiveCount);
                _globalMetrics.PeakMemoryBytes = Math.Max(_globalMetrics.PeakMemoryBytes, _globalMetrics.TotalMemoryBytes);
            }
            
            // Update calculated metrics
            if (_globalMetrics.Capacity > 0)
                _globalMetrics.FragmentationRatio = 1.0f - (float)_globalMetrics.ActiveCount / _globalMetrics.Capacity;
        }
        
        /// <summary>
        /// Checks alerts for a specific pool
        /// </summary>
        private void CheckAlerts(Guid poolId, PoolMetricsData metricsData)
        {
            // Early return if no message bus or no alerts for this pool
            if (_messageBus == null || !_poolAlerts.TryGetValue(poolId, out var poolAlerts))
                return;
                
            float currentTime = GetCurrentTime();
            
            // Check each alert
            foreach (var alert in poolAlerts.Values)
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
                    
                    // Create and publish message
                    var message = new PoolMetricAlertMessage(
                        poolId, 
                        metricsData.Name.ToString(),
                        alert.MetricName, 
                        metricValue, 
                        alert.Threshold);
                        
                    _messageBus.PublishMessage(message);
                }
            }
            
            // Update cooldowns for all alerts
            UpdateAlertCooldowns(poolId, 1.0f / 30.0f); // Assume approximately 30 fps for cooldown
        }
        
        /// <summary>
        /// Updates cooldowns for alerts
        /// </summary>
        private void UpdateAlertCooldowns(Guid poolId, float deltaTime)
        {
            if (!_poolAlerts.TryGetValue(poolId, out var poolAlerts))
                return;
                
            foreach (var alert in poolAlerts.Values)
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
        private double GetMetricValue(PoolMetricsData metricsData, string metricName)
        {
            switch (metricName.ToLowerInvariant())
            {
                case "activeitems":
                case "activecount": 
                    return metricsData.ActiveCount;
                case "capacity": 
                    return metricsData.Capacity;
                case "freeitems":
                case "freecount": 
                    return metricsData.FreeCount;
                case "usageratio": 
                    return metricsData.UsageRatio;
                case "totalcreated": 
                    return metricsData.TotalCreatedCount;
                case "totalacquired": 
                    return metricsData.TotalAcquiredCount;
                case "totalreleased": 
                    return metricsData.TotalReleasedCount;
                case "leakeditems": 
                    return metricsData.LeakedItemCount;
                case "peakactive": 
                    return metricsData.PeakActiveCount;
                case "resizes": 
                    return metricsData.TotalResizeOperations;
                case "acquiretime": 
                    return metricsData.AverageAcquireTimeMs;
                case "releasetime": 
                    return metricsData.AverageReleaseTimeMs;
                case "itemlifetime": 
                    return metricsData.AverageLifetimeSeconds;
                case "cachehitratio": 
                    return metricsData.CacheHitRatio;
                case "efficiency": 
                    return metricsData.PoolEfficiency;
                case "fragmentationratio": 
                    return metricsData.FragmentationRatio;
                default: 
                    return 0;
            }
        }
        
        /// <summary>
        /// Checks if the metrics tracker is initialized
        /// </summary>
        private void CheckInitialized()
        {
            if (!_isCreated)
                throw new InvalidOperationException("PoolMetrics is not initialized");
        }
        
        /// <summary>
        /// Estimates the size of an object of a given type in bytes
        /// </summary>
        private int EstimateTypeSize(Type type)
        {
            if (type == null)
                return 0;
                
            // Some rough estimates for common types
            if (type == typeof(int) || type == typeof(float) || type == typeof(uint))
                return 4;
                
            if (type == typeof(long) || type == typeof(double) || type == typeof(ulong))
                return 8;
                
            if (type == typeof(bool) || type == typeof(byte))
                return 1;
                
            if (type == typeof(char) || type == typeof(short) || type == typeof(ushort))
                return 2;
                
            if (type == typeof(string))
                return 40; // Base size plus some average content
                
            if (type.IsValueType)
                return 16; // Basic struct size
                
            // Reference type - account for object header + basic fields
            return 24; 
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
        /// Represents a metric alert for pool metrics
        /// </summary>
        private class MetricAlert
        {
            /// <summary>
            /// Pool identifier
            /// </summary>
            public Guid PoolId;
            
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
    }
}