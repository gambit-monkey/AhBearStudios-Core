using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ZLinq;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Pools;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Production-ready implementation of pool performance monitoring service.
    /// Tracks operation performance against budgets and provides comprehensive metrics.
    /// Uses Unity-optimized patterns and zero-allocation operations for game performance.
    /// </summary>
    public sealed class PoolPerformanceMonitorService : IPoolPerformanceMonitorService
    {
        #region Private Fields
        
        private readonly ILoggingService _loggingService;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        
        private readonly ConcurrentDictionary<string, IObjectPool> _registeredPools;
        private readonly ConcurrentDictionary<string, PerformanceBudgetTracker> _performanceTrackers;
        
        private volatile bool _disposed;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the PoolPerformanceMonitorService.
        /// </summary>
        /// <param name="loggingService">Logging service for system integration</param>
        /// <param name="alertService">Alert service for critical notifications</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        public PoolPerformanceMonitorService(
            ILoggingService loggingService,
            IAlertService alertService = null,
            IProfilerService profilerService = null)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _alertService = alertService;
            _profilerService = profilerService;
            
            _registeredPools = new ConcurrentDictionary<string, IObjectPool>();
            _performanceTrackers = new ConcurrentDictionary<string, PerformanceBudgetTracker>();
        }
        
        #endregion
        
        #region Pool Registration
        
        /// <inheritdoc />
        public void RegisterPool(string poolTypeName, IObjectPool pool, PerformanceBudget budget = null)
        {
            ThrowIfDisposed();
            
            if (string.IsNullOrWhiteSpace(poolTypeName))
                throw new ArgumentException("Pool type name cannot be null or whitespace", nameof(poolTypeName));
            
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
            
            _registeredPools.TryAdd(poolTypeName, pool);
            
            var effectiveBudget = budget ?? pool.Configuration?.PerformanceBudget ?? PerformanceBudget.For60FPS();
            _performanceTrackers.TryAdd(poolTypeName, new PerformanceBudgetTracker(effectiveBudget));
            
            _loggingService.LogInfo($"Registered pool {poolTypeName} for performance monitoring with {effectiveBudget.TargetFrameRate} FPS budget");
        }
        
        /// <inheritdoc />
        public void UnregisterPool(string poolTypeName)
        {
            ThrowIfDisposed();
            
            if (string.IsNullOrWhiteSpace(poolTypeName))
                return;
            
            _registeredPools.TryRemove(poolTypeName, out _);
            _performanceTrackers.TryRemove(poolTypeName, out _);
            
            _loggingService.LogInfo($"Unregistered pool {poolTypeName} from performance monitoring");
        }
        
        #endregion
        
        #region Performance Monitoring
        
        /// <inheritdoc />
        public async UniTask<T> ExecuteWithPerformanceBudget<T>(
            string poolTypeName,
            string operationType,
            Func<UniTask<T>> operation,
            PerformanceBudget budget = null)
        {
            ThrowIfDisposed();
            
            var tracker = GetOrCreatePerformanceTracker(poolTypeName, budget);
            
            if (!tracker.Budget.EnablePerformanceMonitoring)
            {
                // Performance monitoring disabled, execute directly
                return await operation();
            }
            
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            using var scope = _profilerService?.BeginScope($"PoolPerformance.{poolTypeName}.{operationType}");
            
            try
            {
                var result = await operation();
                
                stopwatch.Stop();
                var operationTime = stopwatch.Elapsed;
                
                // Record performance metrics
                tracker.RecordOperation(operationTime, operationType);
                
                // Check for budget violations and log warnings
                if (tracker.Budget.LogPerformanceWarnings)
                {
                    var budgetLimit = GetBudgetLimitForOperationType(operationType, tracker.Budget);
                    if (operationTime > budgetLimit)
                    {
                        var violationMessage = $"Performance budget violated for {poolTypeName}.{operationType}: " +
                                             $"{operationTime.TotalMilliseconds:F2}ms > {budgetLimit.TotalMilliseconds:F2}ms limit";
                        
                        _loggingService.LogWarning(violationMessage);
                        
                        // Raise alert for significant violations
                        if (operationTime.TotalMilliseconds > budgetLimit.TotalMilliseconds * 2)
                        {
                            _alertService?.RaiseAlert(
                                violationMessage,
                                AlertSeverity.Warning,
                                $"PoolPerformanceMonitor.PerformanceBudget.{poolTypeName}"
                            );
                        }
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Still record the operation time even if it failed
                tracker.RecordOperation(stopwatch.Elapsed, operationType);
                throw;
            }
        }
        
        /// <inheritdoc />
        public async UniTask ExecuteWithPerformanceBudget(
            string poolTypeName,
            string operationType,
            Func<UniTask> operation,
            PerformanceBudget budget = null)
        {
            await ExecuteWithPerformanceBudget(poolTypeName, operationType, async () =>
            {
                await operation();
                return true; // Dummy return value
            }, budget);
        }
        
        /// <inheritdoc />
        public void RecordPerformanceMetric(string poolTypeName, string operationType, TimeSpan operationTime)
        {
            ThrowIfDisposed();
            
            var tracker = GetOrCreatePerformanceTracker(poolTypeName);
            tracker.RecordOperation(operationTime, operationType);
        }
        
        #endregion
        
        #region Statistics and Health
        
        /// <inheritdoc />
        public Dictionary<string, object> GetPerformanceStatistics()
        {
            ThrowIfDisposed();
            
            return _performanceTrackers.AsValueEnumerable()
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object)new
                    {
                        TotalOperations = kvp.Value.TotalOperations,
                        BudgetViolations = kvp.Value.BudgetViolations,
                        ViolationRatePercentage = kvp.Value.GetViolationRate(),
                        AverageOperationTimeMs = kvp.Value.GetAverageOperationTime().TotalMilliseconds,
                        MaxOperationTimeMs = kvp.Value.MaxOperationTime.TotalMilliseconds,
                        LastViolationTime = kvp.Value.LastViolationTime,
                        TargetFrameRate = kvp.Value.Budget.TargetFrameRate,
                        MaxOperationTimeBudgetMs = kvp.Value.Budget.MaxOperationTime.TotalMilliseconds,
                        FrameTimePercentage = kvp.Value.Budget.FrameTimePercentage
                    });
        }
        
        /// <inheritdoc />
        public object GetPoolPerformanceStatistics(string poolTypeName)
        {
            ThrowIfDisposed();
            
            if (!_performanceTrackers.TryGetValue(poolTypeName, out var tracker))
                return null;
            
            return new
            {
                TotalOperations = tracker.TotalOperations,
                BudgetViolations = tracker.BudgetViolations,
                ViolationRatePercentage = tracker.GetViolationRate(),
                AverageOperationTimeMs = tracker.GetAverageOperationTime().TotalMilliseconds,
                MaxOperationTimeMs = tracker.MaxOperationTime.TotalMilliseconds,
                LastViolationTime = tracker.LastViolationTime,
                TargetFrameRate = tracker.Budget.TargetFrameRate,
                MaxOperationTimeBudgetMs = tracker.Budget.MaxOperationTime.TotalMilliseconds,
                FrameTimePercentage = tracker.Budget.FrameTimePercentage
            };
        }
        
        /// <inheritdoc />
        public bool IsPerformanceAcceptable()
        {
            ThrowIfDisposed();
            
            foreach (var kvp in _performanceTrackers)
            {
                var tracker = kvp.Value;
                var violationRate = tracker.GetViolationRate();
                
                // Consider performance unacceptable if more than 10% of operations violate the budget
                if (violationRate > 10.0)
                {
                    _loggingService.LogWarning($"Pool {kvp.Key} has high performance budget violation rate: {violationRate:F1}%");
                    return false;
                }
                
                // Check if average operation time is approaching the budget limit
                var averageTime = tracker.GetAverageOperationTime();
                var budgetLimit = tracker.Budget.MaxOperationTime;
                
                if (averageTime.TotalMilliseconds > budgetLimit.TotalMilliseconds * 0.8) // 80% of budget
                {
                    _loggingService.LogWarning($"Pool {kvp.Key} average operation time is approaching budget limit: " +
                                             $"{averageTime.TotalMilliseconds:F2}ms (limit: {budgetLimit.TotalMilliseconds:F2}ms)");
                    return false;
                }
            }
            
            return true;
        }
        
        /// <inheritdoc />
        public string[] GetPoolsWithPerformanceIssues()
        {
            ThrowIfDisposed();
            
            return _performanceTrackers.AsValueEnumerable()
                .Where(kvp =>
                {
                    var tracker = kvp.Value;
                    var violationRate = tracker.GetViolationRate();
                    var averageTime = tracker.GetAverageOperationTime();
                    var budgetLimit = tracker.Budget.MaxOperationTime;
                    
                    return violationRate > 10.0 || 
                           averageTime.TotalMilliseconds > budgetLimit.TotalMilliseconds * 0.8;
                })
                .Select(kvp => kvp.Key)
                .ToArray();
        }
        
        #endregion
        
        #region Budget Management
        
        /// <inheritdoc />
        public void UpdatePerformanceBudget(string poolTypeName, PerformanceBudget budget)
        {
            ThrowIfDisposed();
            
            if (budget == null)
                throw new ArgumentNullException(nameof(budget));
            
            if (_performanceTrackers.TryGetValue(poolTypeName, out var existingTracker))
            {
                // Remove the old tracker and create a new one with the updated budget
                _performanceTrackers.TryRemove(poolTypeName, out _);
                _performanceTrackers.TryAdd(poolTypeName, new PerformanceBudgetTracker(budget));
                
                _loggingService.LogInfo($"Updated performance budget for pool {poolTypeName} to {budget.TargetFrameRate} FPS");
            }
            else
            {
                _loggingService.LogWarning($"Cannot update performance budget: pool {poolTypeName} not registered");
            }
        }
        
        /// <inheritdoc />
        public PerformanceBudget GetPerformanceBudget(string poolTypeName)
        {
            ThrowIfDisposed();
            
            return _performanceTrackers.TryGetValue(poolTypeName, out var tracker) ? tracker.Budget : null;
        }
        
        #endregion
        
        #region Private Implementation
        
        /// <summary>
        /// Internal class to track performance budget violations.
        /// </summary>
        private class PerformanceBudgetTracker
        {
            private long _totalOperations;
            private long _budgetViolations;
            
            public PerformanceBudget Budget { get; }
            public long TotalOperations => _totalOperations;
            public long BudgetViolations => _budgetViolations;
            public TimeSpan TotalOperationTime { get; private set; }
            public TimeSpan MaxOperationTime { get; private set; }
            public DateTime LastViolationTime { get; private set; }
            
            public PerformanceBudgetTracker(PerformanceBudget budget)
            {
                Budget = budget ?? throw new ArgumentNullException(nameof(budget));
            }
            
            public void RecordOperation(TimeSpan operationTime, string operationType)
            {
                Interlocked.Increment(ref _totalOperations);
                
                lock (this)
                {
                    TotalOperationTime = TotalOperationTime.Add(operationTime);
                    
                    if (operationTime > MaxOperationTime)
                        MaxOperationTime = operationTime;
                }
                
                // Check if operation exceeded budget
                var budgetLimit = GetBudgetLimitForOperation(operationType);
                if (operationTime > budgetLimit)
                {
                    Interlocked.Increment(ref _budgetViolations);
                    LastViolationTime = DateTime.UtcNow;
                }
            }
            
            private TimeSpan GetBudgetLimitForOperation(string operationType)
            {
                return operationType.ToLowerInvariant() switch
                {
                    "get" or "return" => Budget.MaxOperationTime,
                    "validation" => Budget.MaxValidationTime,
                    "expansion" => Budget.MaxExpansionTime,
                    "contraction" => Budget.MaxContractionTime,
                    _ => Budget.MaxOperationTime
                };
            }
            
            public double GetViolationRate() => TotalOperations > 0 ? (double)BudgetViolations / TotalOperations * 100 : 0;
            
            public TimeSpan GetAverageOperationTime() => TotalOperations > 0 ? 
                new TimeSpan(TotalOperationTime.Ticks / TotalOperations) : TimeSpan.Zero;
        }
        
        /// <summary>
        /// Gets or creates a performance tracker for a pool type.
        /// </summary>
        private PerformanceBudgetTracker GetOrCreatePerformanceTracker(string poolTypeName, PerformanceBudget budget = null)
        {
            return _performanceTrackers.GetOrAdd(poolTypeName, typeName =>
            {
                var effectiveBudget = budget ?? PerformanceBudget.For60FPS(); // Default to 60 FPS budget
                _loggingService.LogInfo($"Created performance tracker for pool type: {typeName} with {effectiveBudget.TargetFrameRate} FPS budget");
                return new PerformanceBudgetTracker(effectiveBudget);
            });
        }
        
        /// <summary>
        /// Gets the budget limit for a specific operation type.
        /// </summary>
        private TimeSpan GetBudgetLimitForOperationType(string operationType, PerformanceBudget budget)
        {
            return operationType.ToLowerInvariant() switch
            {
                "get" or "return" => budget.MaxOperationTime,
                "validation" => budget.MaxValidationTime,
                "expansion" => budget.MaxExpansionTime,
                "contraction" => budget.MaxContractionTime,
                _ => budget.MaxOperationTime
            };
        }
        
        /// <summary>
        /// Throws ObjectDisposedException if the service has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PoolPerformanceMonitorService));
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        /// <summary>
        /// Disposes the performance monitor service and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _registeredPools.Clear();
                _performanceTrackers.Clear();
                _disposed = true;
            }
        }
        
        #endregion
    }
}