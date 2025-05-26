using System;
using System.Collections.Generic;
using AhBearStudios.Core.Pooling.Services;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Diagnostics
{
    /// <summary>
    /// Service for monitoring and validating the health of object pools.
    /// Detects issues such as memory leaks, excessive allocations, and performance bottlenecks.
    /// </summary>
    public sealed class PoolHealthChecker : IPoolHealthChecker, IDisposable
    {
        #region Private Fields

        private readonly IPoolLogger _logger;
        private readonly IPoolMetrics _metrics;
        private readonly IPoolProfiler _profiler;
        private readonly IPoolDiagnostics _diagnostics;
        private readonly NativeParallelHashMap<Guid, PoolHealthState> _poolHealthStates;
        private readonly NativeList<PoolHealthIssue> _currentIssues;
        private readonly NativeList<PoolHealthIssue> _persistentIssues;
        
        // New fields for enhanced functionality
        private readonly Dictionary<Guid, List<PoolHistoryPoint>> _poolHealthHistory;
        private readonly Dictionary<string, HashSet<Guid>> _taggedPools;
        private readonly Dictionary<Guid, AdaptiveThresholds> _adaptiveThresholds;
        
        private float _lastCheckTime;
        private bool _isDisposed;
        private int _historySampleRate = 5; // Store a history point every X checks
        private int _checkCounter = 0;
        private const int MAX_HISTORY_POINTS_PER_POOL = 1000;

        #endregion

        #region Public Properties

        /// <inheritdoc/>
        public float CheckInterval { get; set; } = 60f;

        /// <inheritdoc/>
        public bool LogWarnings { get; set; } = true;

        /// <inheritdoc/>
        public bool AlertOnLeaks { get; set; } = true;

        /// <inheritdoc/>
        public bool AlertOnHighUsage { get; set; } = true;

        /// <inheritdoc/>
        public float HighUsageThreshold { get; set; } = 0.85f;

        /// <inheritdoc/>
        public bool AlertOnPerformanceIssues { get; set; } = true;

        /// <inheritdoc/>
        public float SlowAcquireThreshold { get; set; } = 1.0f;

        /// <inheritdoc/>
        public bool AlertOnFragmentation { get; set; } = true;
        
        /// <inheritdoc/>
        public bool AlertOnThreadContention { get; set; } = true;
        
        /// <inheritdoc/>
        public bool EnableAdaptiveThresholds { get; set; } = false;

        /// <inheritdoc/>
        public event Action<PoolHealthIssue> OnIssueDetected;

        /// <inheritdoc/>
        public event Action<Guid, int> OnIssueCountChanged;

        /// <inheritdoc/>
        public event Action<Guid> OnCriticalIssueDetected;

        #endregion

        #region Constructor and Initialization

        /// <summary>
        /// Initializes a new instance of the PoolHealthChecker class with the specified services
        /// </summary>
        /// <param name="logger">The pool logger service</param>
        /// <param name="metrics">The pool metrics service</param>
        /// <param name="profiler">The pool profiler service</param>
        /// <param name="diagnostics">The pool diagnostics service</param>
        public PoolHealthChecker(
            IPoolLogger logger,
            IPoolMetrics metrics,
            IPoolProfiler profiler,
            IPoolDiagnostics diagnostics)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

            // Initialize native collections using Collections v2
            _poolHealthStates = new NativeParallelHashMap<Guid, PoolHealthState>(64, Allocator.Persistent);
            _currentIssues = new NativeList<PoolHealthIssue>(32, Allocator.Persistent);
            _persistentIssues = new NativeList<PoolHealthIssue>(32, Allocator.Persistent);
            
            // Initialize new functionality collections
            _poolHealthHistory = new Dictionary<Guid, List<PoolHistoryPoint>>();
            _taggedPools = new Dictionary<string, HashSet<Guid>>();
            _adaptiveThresholds = new Dictionary<Guid, AdaptiveThresholds>();

            _lastCheckTime = Time.realtimeSinceStartup;
            LogWarnings = true;
            AlertOnLeaks = true;
            AlertOnHighUsage = true;
            AlertOnPerformanceIssues = true;
            AlertOnFragmentation = true;
            AlertOnThreadContention = true;
            EnableAdaptiveThresholds = false;
            HighUsageThreshold = 0.85f;
            SlowAcquireThreshold = 1.0f;
            CheckInterval = 60f;
        }

        /// <summary>
        /// Initializes a new instance of the PoolHealthChecker class using a service locator
        /// </summary>
        /// <param name="serviceLocator">Service locator for dependency injection</param>
        /// <exception cref="ArgumentNullException">Thrown if serviceLocator is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if required services cannot be resolved</exception>
        public PoolHealthChecker(IPoolingServiceLocator serviceLocator)
        {
            if (serviceLocator == null)
                throw new ArgumentNullException(nameof(serviceLocator));

            // Get required dependencies from service locator
            _logger = serviceLocator.GetService<IPoolLogger>() ?? 
                throw new InvalidOperationException("Failed to resolve IPoolLogger from service locator");
            
            _metrics = serviceLocator.GetService<IPoolMetrics>() ?? 
                throw new InvalidOperationException("Failed to resolve IPoolMetrics from service locator");
            
            _profiler = serviceLocator.GetService<IPoolProfiler>() ?? 
                throw new InvalidOperationException("Failed to resolve IPoolProfiler from service locator");
            
            _diagnostics = serviceLocator.GetService<IPoolDiagnostics>() ?? 
                throw new InvalidOperationException("Failed to resolve IPoolDiagnostics from service locator");

            // Initialize native collections using Collections v2
            _poolHealthStates = new NativeParallelHashMap<Guid, PoolHealthState>(64, Allocator.Persistent);
            _currentIssues = new NativeList<PoolHealthIssue>(32, Allocator.Persistent);
            _persistentIssues = new NativeList<PoolHealthIssue>(32, Allocator.Persistent);
            
            // Initialize new functionality collections
            _poolHealthHistory = new Dictionary<Guid, List<PoolHistoryPoint>>();
            _taggedPools = new Dictionary<string, HashSet<Guid>>();
            _adaptiveThresholds = new Dictionary<Guid, AdaptiveThresholds>();

            _lastCheckTime = Time.realtimeSinceStartup;
            LogWarnings = true;
            AlertOnLeaks = true;
            AlertOnHighUsage = true;
            AlertOnPerformanceIssues = true;
            AlertOnFragmentation = true;
            AlertOnThreadContention = true;
            EnableAdaptiveThresholds = false;
            HighUsageThreshold = 0.85f;
            SlowAcquireThreshold = 1.0f;
            CheckInterval = 60f;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes resources used by the health checker
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) 
                return;

            if (_poolHealthStates.IsCreated) 
                _poolHealthStates.Dispose();
            
            if (_currentIssues.IsCreated) 
                _currentIssues.Dispose();
            
            if (_persistentIssues.IsCreated) 
                _persistentIssues.Dispose();
            
            // Clear managed collections
            _poolHealthHistory.Clear();
            _taggedPools.Clear();
            _adaptiveThresholds.Clear();
            
            // Remove event subscriptions
            OnIssueDetected = null;
            OnIssueCountChanged = null;
            OnCriticalIssueDetected = null;

            _isDisposed = true;
            
            // Suppress finalization since we've properly disposed
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer to ensure resources are cleaned up if Dispose is not called
        /// </summary>
        ~PoolHealthChecker()
        {
            Dispose();
        }

        #endregion

        #region Helper Methods
        
        /// <summary>
        /// Throws an ObjectDisposedException if this instance has been disposed
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if instance has been disposed</exception>
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(PoolHealthChecker));
        }
        
        /// <summary>
        /// Clears non-persistent issues
        /// </summary>
        private void ClearNonPersistentIssues()
        {
            // Remove all non-persistent issues
            for (int i = _currentIssues.Length - 1; i >= 0; i--)
            {
                if (!_currentIssues[i].IsPersistent)
                {
                    _currentIssues.RemoveAtSwapBack(i);
                }
            }
        }

        /// <summary>
        /// Clears non-persistent issues for a specific pool
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        private void ClearNonPersistentIssuesForPool(string poolName)
        {
            // Remove non-persistent issues for a specific pool
            for (int i = _currentIssues.Length - 1; i >= 0; i--)
            {
                var issue = _currentIssues[i];
                if (!issue.IsPersistent && issue.PoolName == poolName)
                {
                    _currentIssues.RemoveAtSwapBack(i);
                }
            }
        }

        /// <summary>
        /// Gets a combined list of current and persistent issues
        /// </summary>
        /// <returns>List of all issues</returns>
        private List<PoolHealthIssue> GetAllIssues()
        {
            var result = new List<PoolHealthIssue>(_currentIssues.Length + _persistentIssues.Length);
            
            // Add all current issues
            for (int i = 0; i < _currentIssues.Length; i++)
            {
                result.Add(_currentIssues[i]);
            }
            
            // Add all persistent issues
            for (int i = 0; i < _persistentIssues.Length; i++)
            {
                if (!result.Contains(_persistentIssues[i]))
                {
                    result.Add(_persistentIssues[i]);
                }
            }
            
            return result;
        }

        /// <summary>
        /// Gets issues for a specific pool
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <returns>List of issues for the pool</returns>
        private List<PoolHealthIssue> GetIssuesForPool(string poolName)
        {
            var result = new List<PoolHealthIssue>();
            
            // Check current issues
            for (int i = 0; i < _currentIssues.Length; i++)
            {
                var issue = _currentIssues[i];
                if (issue.PoolName == poolName)
                {
                    result.Add(issue);
                }
            }
            
            // Check persistent issues
            for (int i = 0; i < _persistentIssues.Length; i++)
            {
                var issue = _persistentIssues[i];
                if (issue.PoolName == poolName && !result.Contains(issue))
                {
                    result.Add(issue);
                }
            }
            
            return result;
        }

        /// <summary>
        /// Adds an issue to the appropriate collection
        /// </summary>
        /// <param name="issue">Issue to add</param>
        private void AddIssue(PoolHealthIssue issue)
        {
            bool isNew = false;
            
            if (issue.IsPersistent)
            {
                // Check if we already have this issue
                bool found = false;
                for (int i = 0; i < _persistentIssues.Length; i++)
                {
                    if (_persistentIssues[i].Equals(issue))
                    {
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    _persistentIssues.Add(issue);
                    isNew = true;
                }
            }
            else
            {
                // Check if we already have this issue
                bool found = false;
                for (int i = 0; i < _currentIssues.Length; i++)
                {
                    if (_currentIssues[i].Equals(issue))
                    {
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    _currentIssues.Add(issue);
                    isNew = true;
                }
            }
            
            // Log warning if configured
            if (LogWarnings)
            {
                _logger.LogWarningInstance($"Pool Health Issue: {issue}");
            }
            
            // If this is a new issue, trigger events
            if (isNew)
            {
                // Trigger issue detected event
                OnIssueDetected?.Invoke(issue);
                
                // Get pool ID and trigger other events as needed
                Guid poolId = issue.GetPoolId();
                if (poolId != Guid.Empty)
                {
                    int issueCount = GetIssueCountForPool(poolId);
                    OnIssueCountChanged?.Invoke(poolId, issueCount);
                    
                    if (issue.Severity >= 2)
                    {
                        OnCriticalIssueDetected?.Invoke(poolId);
                    }
                }
                
                // Mark profiler events in development builds
                MarkProfilerEvents(issue);
            }
        }

        /// <summary>
        /// Records a history point for a pool if needed
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="metrics">Pool metrics</param>
        private void RecordPoolHistoryPoint(Guid poolId, Dictionary<string, object> metrics)
        {
            if (poolId == Guid.Empty || metrics == null || _checkCounter++ % _historySampleRate != 0)
                return;
                
            if (!_poolHealthHistory.TryGetValue(poolId, out var history))
            {
                history = new List<PoolHistoryPoint>();
                _poolHealthHistory[poolId] = history;
            }
            
            // Keep the history size reasonable
            if (history.Count >= MAX_HISTORY_POINTS_PER_POOL)
            {
                history.RemoveAt(0);
            }
            
            // Extract relevant metrics
            float usageRatio = metrics.TryGetValue("UsageRatio", out var usageObj) 
                ? Convert.ToSingle(usageObj) : 0f;
            int activeCount = metrics.TryGetValue("ActiveCount", out var activeObj)
                ? Convert.ToInt32(activeObj) : 0;
            int leakCount = metrics.TryGetValue("LeakedCount", out var leakObj)
                ? Convert.ToInt32(leakObj) : 0;
            float acquireTime = metrics.TryGetValue("AvgAcquireTime", out var acquireObj)
                ? Convert.ToSingle(acquireObj) : 0f;
            float fragmentationRatio = metrics.TryGetValue("FragmentationRatio", out var fragObj)
                ? Convert.ToSingle(fragObj) : 0f;
                
            // Create and add history point
            var point = new PoolHistoryPoint(
                DateTime.UtcNow,
                usageRatio,
                activeCount,
                leakCount,
                acquireTime,
                fragmentationRatio);
                
            history.Add(point);
        }

        /// <summary>
        /// Updates adaptive thresholds for a pool if enabled
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="metrics">Pool metrics</param>
        private void UpdateAdaptiveThresholds(Guid poolId, Dictionary<string, object> metrics)
        {
            if (poolId == Guid.Empty || metrics == null || !EnableAdaptiveThresholds)
                return;
                
            if (!_adaptiveThresholds.TryGetValue(poolId, out var thresholds) || !thresholds.Enabled)
                return;
                
            // Extract metric values
            float usageRatio = metrics.TryGetValue("UsageRatio", out var usageObj) 
                ? Convert.ToSingle(usageObj) : 0f;
            float acquireTime = metrics.TryGetValue("AvgAcquireTime", out var acquireObj)
                ? Convert.ToSingle(acquireObj) : 0f;
                
            float leakPercent = 0f;
            if (metrics.TryGetValue("LeakedCount", out var leakObj) && 
                metrics.TryGetValue("TotalCreated", out var createdObj))
            {
                int leakCount = Convert.ToInt32(leakObj);
                int totalCreated = Convert.ToInt32(createdObj);
                
                if (totalCreated > 0)
                {
                    leakPercent = (float)leakCount / totalCreated;
                }
            }
            
            // Update thresholds
            thresholds.AdjustThresholds(usageRatio, acquireTime, leakPercent);
            _adaptiveThresholds[poolId] = thresholds;
        }

        /// <summary>
        /// Gets adaptive thresholds for a pool or default values if not configured
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Adaptive thresholds</returns>
        private (float highUsage, float slowOperation, float leakPercent) GetEffectiveThresholds(Guid poolId)
        {
            if (poolId != Guid.Empty && 
                EnableAdaptiveThresholds && 
                _adaptiveThresholds.TryGetValue(poolId, out var thresholds) && 
                thresholds.Enabled && 
                thresholds.SampleCount > 10)
            {
                return (thresholds.HighUsageThreshold, thresholds.SlowOperationThreshold, thresholds.LeakThresholdPercent);
            }
            
            return (HighUsageThreshold, SlowAcquireThreshold, 0.05f);
        }

        /// <summary>
        /// Marks profiler events for severe health issues
        /// </summary>
        /// <param name="issue">The health issue</param>
        private void MarkProfilerEvents(PoolHealthIssue issue)
        {
#if ENABLE_PROFILER
            if (issue.Severity >= 2)
            {
                ProfilerMarker marker = 
                    new ProfilerMarker($"PoolHealth: {issue.PoolName} - {issue.IssueType}");
                marker.Begin();
                marker.End();
            }
#endif
        }

        /// <summary>
        /// Checks a pool's health based on its metrics
        /// </summary>
        /// <param name="metrics">Metrics dictionary for the pool</param>
        private void CheckPoolHealthInternal(Dictionary<string, object> metrics)
        {
            if (metrics == null || !metrics.TryGetValue("PoolId", out object poolIdObj))
                return;

            if (!(poolIdObj is Guid poolId) || poolId == Guid.Empty)
                return;

            string poolName = metrics.TryGetValue("PoolName", out object nameObj) 
                ? nameObj.ToString() : "Unknown Pool";

            // Clear non-persistent issues for this pool before checking
            ClearNonPersistentIssuesForPool(poolName);

            // Record history point
            RecordPoolHistoryPoint(poolId, metrics);

            // Update adaptive thresholds if enabled
            UpdateAdaptiveThresholds(poolId, metrics);

            // Get effective thresholds (adaptive or static based on configuration)
            var (highUsageThreshold, slowOpThreshold, leakThreshold) = GetEffectiveThresholds(poolId);

            // Extract metrics for analysis
            bool hasLeaks = metrics.TryGetValue("LeakedCount", out object leakObj) && 
                           Convert.ToInt32(leakObj) > 0;
            
            float usageRatio = metrics.TryGetValue("UsageRatio", out object usageObj) 
                ? Convert.ToSingle(usageObj) : 0f;
            
            float avgAcquireTime = metrics.TryGetValue("AvgAcquireTime", out object acquireObj) 
                ? Convert.ToSingle(acquireObj) : 0f;
            
            float fragmentationRatio = metrics.TryGetValue("FragmentationRatio", out object fragObj)
                ? Convert.ToSingle(fragObj) : 0f;
            
            int contentionCount = metrics.TryGetValue("ContentionCount", out object contentionObj)
                ? Convert.ToInt32(contentionObj) : 0;

            // Check for various health issues

            // 1. Check for memory leaks
            if (AlertOnLeaks && hasLeaks)
            {
                int leakCount = Convert.ToInt32(leakObj);
                float leakRatio = 0f;
                
                if (metrics.TryGetValue("TotalCreated", out object totalObj))
                {
                    int totalCreated = Convert.ToInt32(totalObj);
                    if (totalCreated > 0)
                        leakRatio = (float)leakCount / totalCreated;
                }
                
                // Determine severity based on leak ratio
                int severity = 1;
                if (leakRatio >= 0.2f) 
                    severity = 3; // Critical - more than 20% leaked
                else if (leakRatio >= 0.1f)
                    severity = 2; // High - more than 10% leaked
                
                AddIssue(new PoolHealthIssue(
                    poolId,
                    poolName,
                    "MemoryLeak",
                    $"Detected {leakCount} leaked objects (ratio: {leakRatio:P2})",
                    severity,
                    true)); // Persistent issue
            }

            // 2. Check for high usage
            if (AlertOnHighUsage && usageRatio > highUsageThreshold)
            {
                // Determine severity based on how close to capacity
                int severity = 1;
                if (usageRatio >= 0.98f)
                    severity = 3; // Critical - almost at capacity
                else if (usageRatio >= 0.95f)
                    severity = 2; // High - very close to capacity
                
                AddIssue(new PoolHealthIssue(
                    poolId,
                    poolName,
                    "HighUsage",
                    $"Pool usage at {usageRatio:P2} (threshold: {highUsageThreshold:P2})",
                    severity,
                    false)); // Non-persistent
            }

            // 3. Check for performance issues
            if (AlertOnPerformanceIssues && avgAcquireTime > slowOpThreshold)
            {
                // Determine severity based on how slow
                int severity = 1;
                if (avgAcquireTime >= slowOpThreshold * 10)
                    severity = 3; // Critical - extremely slow
                else if (avgAcquireTime >= slowOpThreshold * 3)
                    severity = 2; // High - very slow
                
                AddIssue(new PoolHealthIssue(
                    poolId,
                    poolName,
                    "SlowAcquire",
                    $"Slow acquire operation: {avgAcquireTime:F2}ms (threshold: {slowOpThreshold:F2}ms)",
                    severity,
                    false)); // Non-persistent
            }

            // 4. Check for fragmentation
            if (AlertOnFragmentation && fragmentationRatio > 0.2f)
            {
                // Determine severity based on fragmentation level
                int severity = 1;
                if (fragmentationRatio >= 0.5f)
                    severity = 3; // Critical - heavily fragmented
                else if (fragmentationRatio >= 0.3f) 
                    severity = 2; // High - significantly fragmented
                
                AddIssue(new PoolHealthIssue(
                    poolId,
                    poolName,
                    "Fragmentation",
                    $"Pool is fragmented: {fragmentationRatio:P2} wasted capacity",
                    severity,
                    false)); // Non-persistent
            }

            // 5. Check for thread contention
            if (AlertOnThreadContention && contentionCount > 0)
            {
                // Determine severity based on contention count
                int severity = 1;
                if (contentionCount >= 100)
                    severity = 3; // Critical - heavy contention
                else if (contentionCount >= 10)
                    severity = 2; // High - significant contention
                
                AddIssue(new PoolHealthIssue(
                    poolId,
                    poolName,
                    "ThreadContention",
                    $"Thread contention detected: {contentionCount} collisions",
                    severity,
                    false)); // Non-persistent
            }

            // Update health state for this pool
            UpdatePoolHealthState(poolId, metrics);
        }

        /// <summary>
        /// Updates the health state for a specific pool
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="metrics">Pool metrics</param>
        private void UpdatePoolHealthState(Guid poolId, Dictionary<string, object> metrics)
        {
            if (poolId == Guid.Empty || metrics == null)
                return;

            // Get current state or create new
            PoolHealthState state = _poolHealthStates.TryGetValue(poolId, out var existingState) 
                ? existingState 
                : new PoolHealthState();

            // Update state with latest metrics
            float usageRatio = metrics.TryGetValue("UsageRatio", out var usageObj) 
                ? Convert.ToSingle(usageObj) : 0f;
            
            int leakCount = metrics.TryGetValue("LeakedCount", out var leakObj)
                ? Convert.ToInt32(leakObj) : 0;
            
            float fragmentationRatio = metrics.TryGetValue("FragmentationRatio", out var fragObj)
                ? Convert.ToSingle(fragObj) : 0f;
            
            int contentionCount = metrics.TryGetValue("ContentionCount", out var contentionObj)
                ? Convert.ToInt32(contentionObj) : 0;

            // Update peak usage if higher
            state.PeakUsage = math.max(state.PeakUsage, usageRatio);
            
            // Update leak detection
            if (leakCount > state.LastLeakSize)
            {
                state.ConsecutiveLeakDetections++;
                state.LeakProbability = math.min(1f, state.LeakProbability + 0.2f);
            }
            else
            {
                state.ConsecutiveLeakDetections = 0;
                state.LeakProbability = math.max(0f, state.LeakProbability - 0.05f);
            }
            
            state.LastLeakSize = leakCount;
            
            // Update fragmentation tracking
            state.LastFragmentationRatio = fragmentationRatio;
            
            // Update thread contention
            state.ThreadContentionCount = contentionCount;
            
            // Update issue count
            state.IssueCount = GetIssueCountForPool(poolId);
            
            // Update timestamp
            state.LastCheckTimestamp = Time.realtimeSinceStartup;
            
            // Store updated state
            _poolHealthStates[poolId] = state;
        }
        
                /// <summary>
        /// Job for checking pool health in parallel
        /// </summary>
        [BurstCompile]
        private struct PoolHealthCheckJob : IJob
        {
            // Native collections for the job
            [ReadOnly] public NativeArray<Guid> PoolIds;
            [ReadOnly] public NativeArray<Dictionary<string, object>> PoolMetrics;
            [WriteOnly] public NativeList<PoolHealthIssue> DetectedIssues;
            
            // Thresholds and configuration
            public float HighUsageThreshold;
            public float SlowOperationThreshold;
            public float LeakThreshold;
            public bool AlertOnLeaks;
            public bool AlertOnHighUsage;
            public bool AlertOnPerformance;
            public bool AlertOnFragmentation;
            public bool AlertOnContention;

            public void Execute()
            {
                for (int i = 0; i < PoolIds.Length; i++)
                {
                    var poolId = PoolIds[i];
                    var metrics = PoolMetrics[i];
                    
                    ProcessPoolMetrics(poolId, metrics);
                }
            }
            
            private void ProcessPoolMetrics(Guid poolId, Dictionary<string, object> metrics)
            {
                if (poolId == Guid.Empty || metrics == null)
                    return;
                    
                string poolName = metrics.TryGetValue("PoolName", out object nameObj) 
                    ? nameObj.ToString() : "Unknown Pool";
                    
                // Extract metrics for analysis
                bool hasLeaks = metrics.TryGetValue("LeakedCount", out object leakObj) && 
                               Convert.ToInt32(leakObj) > 0;
                
                float usageRatio = metrics.TryGetValue("UsageRatio", out object usageObj) 
                    ? Convert.ToSingle(usageObj) : 0f;
                
                float avgAcquireTime = metrics.TryGetValue("AvgAcquireTime", out object acquireObj) 
                    ? Convert.ToSingle(acquireObj) : 0f;
                
                float fragmentationRatio = metrics.TryGetValue("FragmentationRatio", out object fragObj)
                    ? Convert.ToSingle(fragObj) : 0f;
                
                int contentionCount = metrics.TryGetValue("ContentionCount", out object contentionObj)
                    ? Convert.ToInt32(contentionObj) : 0;
                    
                // Check for issues and add to detected issues list
                CheckForIssues(poolId, poolName, hasLeaks, usageRatio, avgAcquireTime, 
                    fragmentationRatio, contentionCount, metrics);
            }
            
            private void CheckForIssues(
                Guid poolId, 
                string poolName, 
                bool hasLeaks, 
                float usageRatio, 
                float avgAcquireTime, 
                float fragmentationRatio, 
                int contentionCount, 
                Dictionary<string, object> metrics)
            {
                // Check for memory leaks
                if (AlertOnLeaks && hasLeaks)
                {
                    int leakCount = Convert.ToInt32(metrics["LeakedCount"]);
                    float leakRatio = 0f;
                    
                    if (metrics.TryGetValue("TotalCreated", out object totalObj))
                    {
                        int totalCreated = Convert.ToInt32(totalObj);
                        if (totalCreated > 0)
                            leakRatio = (float)leakCount / totalCreated;
                    }
                    
                    // Determine severity based on leak ratio
                    int severity = 1;
                    if (leakRatio >= 0.2f) 
                        severity = 3; // Critical - more than 20% leaked
                    else if (leakRatio >= 0.1f)
                        severity = 2; // High - more than 10% leaked
                    
                    DetectedIssues.Add(new PoolHealthIssue(
                        poolId,
                        poolName,
                        "MemoryLeak",
                        $"Detected {leakCount} leaked objects (ratio: {leakRatio:P2})",
                        severity,
                        true)); // Persistent issue
                }
                
                // Other checks follow the same pattern...
                // Add other issue checks as needed
            }
        }
        
        /// <summary>
        /// Schedules a health check job to run in the background
        /// </summary>
        /// <returns>JobHandle for the scheduled job</returns>
        private JobHandle ScheduleHealthCheckJob()
        {
            // Get all pool IDs and metrics
            var poolIds = _metrics.GetAllPoolIds();
            var metrics = new Dictionary<Guid, Dictionary<string, object>>();
            
            foreach (var id in poolIds)
            {
                var poolMetrics = _metrics.GetPoolMetrics(id);
                if (poolMetrics != null)
                    metrics[id] = poolMetrics;
            }
            
            // Prepare native arrays for the job
            var poolIdsArray = new NativeArray<Guid>(poolIds.Count, Allocator.TempJob);
            var metricsArray = new NativeArray<Dictionary<string, object>>(poolIds.Count, Allocator.TempJob);
            var issues = new NativeList<PoolHealthIssue>(32, Allocator.TempJob);
            
            for (int i = 0; i < poolIds.Count; i++)
            {
                var id = poolIds[i];
                poolIdsArray[i] = id;
                metricsArray[i] = metrics.TryGetValue(id, out var m) ? m : new Dictionary<string, object>();
            }
            
            // Create and schedule the job
            var job = new PoolHealthCheckJob
            {
                PoolIds = poolIdsArray,
                PoolMetrics = metricsArray,
                DetectedIssues = issues,
                HighUsageThreshold = HighUsageThreshold,
                SlowOperationThreshold = SlowAcquireThreshold,
                LeakThreshold = 0.05f,
                AlertOnLeaks = AlertOnLeaks,
                AlertOnHighUsage = AlertOnHighUsage,
                AlertOnPerformance = AlertOnPerformanceIssues,
                AlertOnFragmentation = AlertOnFragmentation,
                AlertOnContention = AlertOnThreadContention
            };
            
            var handle = job.Schedule();
            
            // Register cleanup for after job completes
            handle = UnityEngine.Jobs.LowLevel.Unsafe.JobsUtility.CombineDependencies(
                handle, 
                new DisposeNativeCollectionsJob
                {
                    PoolIds = poolIdsArray,
                    PoolMetrics = metricsArray,
                    Issues = issues
                }.Schedule(handle));
            
            return handle;
        }
        
        /// <summary>
        /// Job to dispose native collections after health check job completes
        /// </summary>
        [BurstCompile]
        private struct DisposeNativeCollectionsJob : IJob
        {
            [DeallocateOnJobCompletion] public NativeArray<Guid> PoolIds;
            [DeallocateOnJobCompletion] public NativeArray<Dictionary<string, object>> PoolMetrics;
            [DeallocateOnJobCompletion] public NativeList<PoolHealthIssue> Issues;
            
            public void Execute() { }
        }

        #endregion
        
        #region Implementation of IPoolHealthChecker

        /// <inheritdoc/>
        public void SetCheckInterval(float interval)
        {
            ThrowIfDisposed();
            
            if (interval <= 0)
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be positive");
                
            CheckInterval = interval;
        }

        /// <inheritdoc/>
        public void SetAlertFlags(bool alertOnLeaks, bool alertOnHighUsage, bool logWarnings)
        {
            ThrowIfDisposed();
            
            AlertOnLeaks = alertOnLeaks;
            AlertOnHighUsage = alertOnHighUsage;
            LogWarnings = logWarnings;
        }

        /// <inheritdoc/>
        public List<PoolHealthIssue> CheckAllPools()
        {
            ThrowIfDisposed();
            
            // Clear non-persistent issues before check
            ClearNonPersistentIssues();
            
            // Get all pools from metrics service
            var poolIds = _metrics.GetAllPoolIds();
            
            if (poolIds == null || poolIds.Count == 0)
                return new List<PoolHealthIssue>();
                
            // Check each pool
            foreach (var poolId in poolIds)
            {
                var metrics = _metrics.GetPoolMetrics(poolId);
                if (metrics != null)
                {
                    CheckPoolHealthInternal(metrics);
                }
            }
            
            // Return combined list of all issues
            return GetAllIssues();
        }

        /// <inheritdoc/>
        public List<PoolHealthIssue> CheckPoolHealth(IPool pool)
        {
            ThrowIfDisposed();
            
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            // Get metrics for the pool
            var metrics = _metrics.GetPoolMetrics(pool.Id);
            
            if (metrics == null)
                return new List<PoolHealthIssue>();
                
            // Clear non-persistent issues for this pool
            ClearNonPersistentIssuesForPool(pool.Name);
            
            // Check health
            CheckPoolHealthInternal(metrics);
            
            // Return issues for this pool
            return GetIssuesForPool(pool.Name);
        }

        /// <inheritdoc/>
        public Dictionary<string, object> GetPoolHealth(Guid poolId)
        {
            ThrowIfDisposed();
            
            if (poolId == Guid.Empty)
                return null;
                
            if (!_poolHealthStates.TryGetValue(poolId, out var state))
                return null;
                
            // Create dictionary with health data
            var result = new Dictionary<string, object>
            {
                { "LeakProbability", state.LeakProbability },
                { "LastLeakSize", state.LastLeakSize },
                { "PeakUsage", state.PeakUsage },
                { "UsageVariance", state.UsageVariance },
                { "LastCheckTimestamp", state.LastCheckTimestamp },
                { "ConsecutiveLeakDetections", state.ConsecutiveLeakDetections },
                { "IssueCount", state.IssueCount },
                { "ThreadContentionCount", state.ThreadContentionCount },
                { "LastFragmentationRatio", state.LastFragmentationRatio }
            };
            
            // Add adaptive thresholds if enabled
            if (EnableAdaptiveThresholds && _adaptiveThresholds.TryGetValue(poolId, out var thresholds))
            {
                result["AdaptiveHighUsageThreshold"] = thresholds.HighUsageThreshold;
                result["AdaptiveSlowOperationThreshold"] = thresholds.SlowOperationThreshold;
                result["AdaptiveLeakThresholdPercent"] = thresholds.LeakThresholdPercent;
                result["AdaptiveSampleCount"] = thresholds.SampleCount;
                result["AdaptiveThresholdsEnabled"] = thresholds.Enabled;
            }
            
            return result;
        }

        /// <inheritdoc/>
        public void Update()
        {
            ThrowIfDisposed();
            
            float currentTime = Time.realtimeSinceStartup;
            float timeSinceLastCheck = currentTime - _lastCheckTime;
            
            if (timeSinceLastCheck >= CheckInterval)
            {
                CheckAllPools();
                _lastCheckTime = currentTime;
            }
        }

        /// <inheritdoc/>
        public void ClearAllIssues()
        {
            ThrowIfDisposed();
            
            _currentIssues.Clear();
            _persistentIssues.Clear();
            
            // Reset issue counts in health states
            using (var keys = _poolHealthStates.GetKeyArray(Allocator.Temp))
            {
                foreach (var key in keys)
                {
                    if (_poolHealthStates.TryGetValue(key, out var state))
                    {
                        state.IssueCount = 0;
                        _poolHealthStates[key] = state;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void ClearIssuesForPool(Guid poolId)
        {
            ThrowIfDisposed();
            
            if (poolId == Guid.Empty)
                return;
                
            string poolName = null;
            
            // Find pool name from metrics
            var metrics = _metrics.GetPoolMetrics(poolId);
            if (metrics != null && metrics.TryGetValue("PoolName", out object nameObj))
            {
                poolName = nameObj.ToString();
            }
            
            if (string.IsNullOrEmpty(poolName))
                return;
                
            // Clear issues for this pool
            for (int i = _currentIssues.Length - 1; i >= 0; i--)
            {
                var issue = _currentIssues[i];
                if (issue.PoolName == poolName)
                {
                    _currentIssues.RemoveAtSwapBack(i);
                }
            }
            
            for (int i = _persistentIssues.Length - 1; i >= 0; i--)
            {
                var issue = _persistentIssues[i];
                if (issue.PoolName == poolName)
                {
                    _persistentIssues.RemoveAtSwapBack(i);
                }
            }
            
            // Reset issue count in health state
            if (_poolHealthStates.TryGetValue(poolId, out var state))
            {
                state.IssueCount = 0;
                _poolHealthStates[poolId] = state;
            }
        }

        /// <inheritdoc/>
        public List<PoolHealthIssue> GetCurrentIssues()
        {
            ThrowIfDisposed();
            
            return GetAllIssues();
        }

        /// <inheritdoc/>
        public int GetIssueCountForPool(Guid poolId)
        {
            ThrowIfDisposed();
            
            if (poolId == Guid.Empty)
                return 0;
                
            string poolName = null;
            
            // Find pool name from metrics
            var metrics = _metrics.GetPoolMetrics(poolId);
            if (metrics != null && metrics.TryGetValue("PoolName", out object nameObj))
            {
                poolName = nameObj.ToString();
            }
            
            if (string.IsNullOrEmpty(poolName))
                return 0;
                
            // Count issues for this pool
            int count = 0;
            
            for (int i = 0; i < _currentIssues.Length; i++)
            {
                if (_currentIssues[i].PoolName == poolName)
                {
                    count++;
                }
            }
            
            for (int i = 0; i < _persistentIssues.Length; i++)
            {
                if (_persistentIssues[i].PoolName == poolName)
                {
                    count++;
                }
            }
            
            return count;
        }

        /// <inheritdoc/>
        public void TagPool(Guid poolId, string tag)
        {
            ThrowIfDisposed();
            
            if (poolId == Guid.Empty || string.IsNullOrEmpty(tag))
                return;
                
            if (!_taggedPools.TryGetValue(tag, out var pools))
            {
                pools = new HashSet<Guid>();
                _taggedPools[tag] = pools;
            }
            
            pools.Add(poolId);
        }

        /// <inheritdoc/>
        public List<Guid> GetPoolsByTag(string tag)
        {
            ThrowIfDisposed();
            
            if (string.IsNullOrEmpty(tag) || !_taggedPools.TryGetValue(tag, out var pools))
                return new List<Guid>();
                
            return new List<Guid>(pools);
        }

        /// <inheritdoc/>
        public List<Dictionary<string, object>> GetHealthHistory(Guid poolId, int maxPoints = 100)
        {
            ThrowIfDisposed();
            
            if (poolId == Guid.Empty || !_poolHealthHistory.TryGetValue(poolId, out var history))
                return new List<Dictionary<string, object>>();
                
            // Convert history points to dictionaries
            var result = new List<Dictionary<string, object>>(
                Math.Min(history.Count, maxPoints));
                
            // Get the range of points to return
            int startIndex = history.Count <= maxPoints ? 0 : history.Count - maxPoints;
            
            for (int i = startIndex; i < history.Count; i++)
            {
                var point = history[i];
                result.Add(new Dictionary<string, object>
                {
                    { "Timestamp", point.Timestamp },
                    { "UsageRatio", point.UsageRatio },
                    { "ActiveCount", point.ActiveCount },
                    { "LeakCount", point.LeakCount },
                    { "AcquireTime", point.AcquireTime },
                    { "FragmentationRatio", point.FragmentationRatio }
                });
            }
            
            return result;
        }

        /// <inheritdoc/>
        public void SetAdaptiveThresholds(Guid poolId, bool enable)
        {
            ThrowIfDisposed();
            
            if (poolId == Guid.Empty)
                return;
                
            if (_adaptiveThresholds.TryGetValue(poolId, out var thresholds))
            {
                thresholds.Enabled = enable;
                _adaptiveThresholds[poolId] = thresholds;
            }
            else if (enable)
            {
                // Create new adaptive thresholds with initial values
                _adaptiveThresholds[poolId] = new AdaptiveThresholds
                {
                    HighUsageThreshold = HighUsageThreshold,
                    SlowOperationThreshold = SlowAcquireThreshold,
                    LeakThresholdPercent = 0.05f,
                    SampleCount = 0,
                    Enabled = true
                };
            }
        }

        /// <inheritdoc/>
        public Dictionary<string, object> ExportHealthReport(bool includeHistory = false)
        {
            ThrowIfDisposed();
            
            var report = new Dictionary<string, object>();
            
            // Add global settings
            report["CheckInterval"] = CheckInterval;
            report["LogWarnings"] = LogWarnings;
            report["AlertOnLeaks"] = AlertOnLeaks;
            report["AlertOnHighUsage"] = AlertOnHighUsage;
            report["HighUsageThreshold"] = HighUsageThreshold;
            report["AlertOnPerformanceIssues"] = AlertOnPerformanceIssues;
            report["SlowAcquireThreshold"] = SlowAcquireThreshold;
            report["AlertOnFragmentation"] = AlertOnFragmentation;
            report["AlertOnThreadContention"] = AlertOnThreadContention;
            report["EnableAdaptiveThresholds"] = EnableAdaptiveThresholds;
            
            // Add pools data
            var poolsData = new Dictionary<string, Dictionary<string, object>>();
            var poolIds = _metrics.GetAllPoolIds();
            
            foreach (var poolId in poolIds)
            {
                var metrics = _metrics.GetPoolMetrics(poolId);
                if (metrics == null) continue;
                
                string poolName = metrics.TryGetValue("PoolName", out object nameObj) 
                    ? nameObj.ToString() : poolId.ToString();
                    
                var poolData = new Dictionary<string, object>();
                
                // Add metrics
                foreach (var kvp in metrics)
                {
                    poolData[kvp.Key] = kvp.Value;
                }
                
                // Add health state
                var healthState = GetPoolHealth(poolId);
                if (healthState != null)
                {
                    foreach (var kvp in healthState)
                    {
                        poolData[$"Health_{kvp.Key}"] = kvp.Value;
                    }
                }
                
                // Add issues
                var issues = GetIssuesForPool(poolName);
                if (issues.Count > 0)
                {
                    var issuesList = new List<Dictionary<string, object>>();
                    foreach (var issue in issues)
                    {
                        issuesList.Add(new Dictionary<string, object>
                        {
                            { "Type", issue.IssueType },
                            { "Message", issue.Message },
                            { "Severity", issue.Severity },
                            { "IsPersistent", issue.IsPersistent },
                            { "Timestamp", issue.Timestamp }
                        });
                    }
                    poolData["Issues"] = issuesList;
                }
                
                // Add history if requested
                if (includeHistory && _poolHealthHistory.TryGetValue(poolId, out var history))
                {
                    var historyList = new List<Dictionary<string, object>>();
                    foreach (var point in history)
                    {
                        historyList.Add(new Dictionary<string, object>
                        {
                            { "Timestamp", point.Timestamp },
                            { "UsageRatio", point.UsageRatio },
                            { "ActiveCount", point.ActiveCount },
                            { "LeakCount", point.LeakCount },
                            { "AcquireTime", point.AcquireTime },
                            { "FragmentationRatio", point.FragmentationRatio }
                        });
                    }
                    poolData["History"] = historyList;
                }
                
                // Add to report
                poolsData[poolName] = poolData;
            }
            
            report["Pools"] = poolsData;
            
            // Add tag mapping
            var tagsMapping = new Dictionary<string, List<string>>();
            foreach (var kvp in _taggedPools)
            {
                var poolNames = new List<string>();
                foreach (var id in kvp.Value)
                {
                    var m = _metrics.GetPoolMetrics(id);
                    if (m != null && m.TryGetValue("PoolName", out object name))
                    {
                        poolNames.Add(name.ToString());
                    }
                    else
                    {
                        poolNames.Add(id.ToString());
                    }
                }
                tagsMapping[kvp.Key] = poolNames;
            }
            
            report["Tag"] = tagsMapping;
            
            return report;
        }

        #endregion
    }
}