using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ZLinq;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Pooling.Messages;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Pools;
using AhBearStudios.Core.Profiling;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Production-ready implementation of automatic pool scaling service.
    /// Monitors pool utilization and automatically adjusts pool sizes based on demand patterns.
    /// Uses Unity-optimized async patterns and zero-allocation operations for game performance.
    /// </summary>
    public sealed class PoolAutoScalingService : IPoolAutoScalingService
    {
        #region Private Fields
        
        private readonly ILoggingService _loggingService;
        private readonly IMessageBusService _messageBusService;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        
        private readonly ConcurrentDictionary<string, IObjectPool> _registeredPools;
        private readonly ConcurrentDictionary<string, PoolScalingMetrics> _scalingMetrics;
        
        private CancellationTokenSource _cancellationTokenSource;
        private volatile bool _disposed;
        private volatile bool _isAutoScalingActive;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the PoolAutoScalingService.
        /// </summary>
        /// <param name="loggingService">Logging service for system integration</param>
        /// <param name="messageBusService">Message bus service for event publishing</param>
        /// <param name="alertService">Alert service for critical notifications</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        public PoolAutoScalingService(
            ILoggingService loggingService,
            IMessageBusService messageBusService,
            IAlertService alertService = null,
            IProfilerService profilerService = null)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _alertService = alertService;
            _profilerService = profilerService;
            
            _registeredPools = new ConcurrentDictionary<string, IObjectPool>();
            _scalingMetrics = new ConcurrentDictionary<string, PoolScalingMetrics>();
        }
        
        #endregion
        
        #region Auto-Scaling Control
        
        /// <inheritdoc />
        public void StartAutoScaling(TimeSpan checkInterval)
        {
            ThrowIfDisposed();
            
            if (_isAutoScalingActive)
            {
                _loggingService.LogWarning("Auto-scaling is already running");
                return;
            }
            
            _cancellationTokenSource = new CancellationTokenSource();
            _isAutoScalingActive = true;
            
            // Start the scaling loop using UniTask
            PerformAutoScalingLoop(checkInterval).Forget();
            
            _loggingService.LogInfo($"Started automatic pool scaling with {checkInterval.TotalSeconds}s check interval");
        }
        
        /// <inheritdoc />
        public void StopAutoScaling()
        {
            ThrowIfDisposed();
            
            if (!_isAutoScalingActive)
                return;
            
            _isAutoScalingActive = false;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            
            _loggingService.LogInfo("Stopped automatic pool scaling");
        }
        
        /// <inheritdoc />
        public bool IsAutoScalingActive => _isAutoScalingActive;
        
        #endregion
        
        #region Pool Registration
        
        /// <inheritdoc />
        public void RegisterPool(string poolTypeName, IObjectPool pool)
        {
            ThrowIfDisposed();
            
            if (string.IsNullOrWhiteSpace(poolTypeName))
                throw new ArgumentException("Pool type name cannot be null or whitespace", nameof(poolTypeName));
            
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
            
            _registeredPools.TryAdd(poolTypeName, pool);
            _scalingMetrics.TryAdd(poolTypeName, new PoolScalingMetrics
            {
                CurrentCapacity = pool.Configuration.InitialCapacity
            });
            
            _loggingService.LogInfo($"Registered pool {poolTypeName} for auto-scaling");
        }
        
        /// <inheritdoc />
        public void UnregisterPool(string poolTypeName)
        {
            ThrowIfDisposed();
            
            if (string.IsNullOrWhiteSpace(poolTypeName))
                return;
            
            _registeredPools.TryRemove(poolTypeName, out _);
            _scalingMetrics.TryRemove(poolTypeName, out _);
            
            _loggingService.LogInfo($"Unregistered pool {poolTypeName} from auto-scaling");
        }
        
        #endregion
        
        #region Manual Scaling
        
        /// <inheritdoc />
        public async UniTask PerformScalingCheck()
        {
            ThrowIfDisposed();
            
            using var scope = _profilerService?.BeginScope("PoolAutoScaling.PerformScalingCheck");
            
            try
            {
                foreach (var kvp in _registeredPools)
                {
                    await PerformScalingCheck(kvp.Key);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Auto-scaling check failed", ex);
            }
        }
        
        /// <inheritdoc />
        public async UniTask PerformScalingCheck(string poolTypeName)
        {
            ThrowIfDisposed();
            
            if (!_registeredPools.TryGetValue(poolTypeName, out var pool))
            {
                _loggingService.LogWarning($"Pool {poolTypeName} not registered for auto-scaling");
                return;
            }
            
            if (!_scalingMetrics.TryGetValue(poolTypeName, out var metrics))
            {
                _loggingService.LogWarning($"No scaling metrics found for pool {poolTypeName}");
                return;
            }
            
            using var scope = _profilerService?.BeginScope($"PoolAutoScaling.PerformScalingCheck.{poolTypeName}");
            
            try
            {
                // Calculate utilization
                var statistics = pool.GetStatistics();
                var utilization = statistics.TotalCount > 0 
                    ? (double)statistics.ActiveCount / statistics.TotalCount 
                    : 0;
                
                metrics.RecordUtilization(utilization);
                
                // Determine scaling action
                var scalingAction = DetermineScalingAction(poolTypeName, pool, metrics);
                
                // Execute scaling if needed
                if (scalingAction != ScalingAction.None)
                {
                    await ExecutePoolScaling(poolTypeName, pool, metrics, scalingAction);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Scaling check failed for pool {poolTypeName}", ex);
            }
        }
        
        #endregion
        
        #region Statistics and Monitoring
        
        /// <inheritdoc />
        public Dictionary<string, object> GetAutoScalingStatistics()
        {
            ThrowIfDisposed();
            
            return _scalingMetrics.AsValueEnumerable()
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object)new
                    {
                        AverageUtilization = kvp.Value.AverageUtilization,
                        ConsecutiveHighUtilization = kvp.Value.ConsecutiveHighUtilization,
                        ConsecutiveLowUtilization = kvp.Value.ConsecutiveLowUtilization,
                        LastScaleUpTime = kvp.Value.LastScaleUpTime,
                        LastScaleDownTime = kvp.Value.LastScaleDownTime,
                        CurrentCapacity = kvp.Value.CurrentCapacity
                    });
        }
        
        /// <inheritdoc />
        public object GetPoolScalingMetrics(string poolTypeName)
        {
            ThrowIfDisposed();
            
            if (!_scalingMetrics.TryGetValue(poolTypeName, out var metrics))
                return null;
            
            return new
            {
                AverageUtilization = metrics.AverageUtilization,
                ConsecutiveHighUtilization = metrics.ConsecutiveHighUtilization,
                ConsecutiveLowUtilization = metrics.ConsecutiveLowUtilization,
                LastScaleUpTime = metrics.LastScaleUpTime,
                LastScaleDownTime = metrics.LastScaleDownTime,
                CurrentCapacity = metrics.CurrentCapacity
            };
        }
        
        #endregion
        
        #region Private Implementation
        
        /// <summary>
        /// Internal class to track pool scaling metrics.
        /// </summary>
        private class PoolScalingMetrics
        {
            private readonly List<double> _utilizationHistory = new(60); // Keep 60 samples
            
            public int ConsecutiveHighUtilization { get; set; }
            public int ConsecutiveLowUtilization { get; set; }
            public DateTime LastScaleUpTime { get; set; }
            public DateTime LastScaleDownTime { get; set; }
            public int CurrentCapacity { get; set; }
            public double AverageUtilization { get; private set; }
            
            public void RecordUtilization(double utilization)
            {
                _utilizationHistory.Add(utilization);
                if (_utilizationHistory.Count > 60)
                    _utilizationHistory.RemoveAt(0);
                
                AverageUtilization = _utilizationHistory.AsValueEnumerable().Average();
            }
        }
        
        /// <summary>
        /// Scaling action enumeration.
        /// </summary>
        private enum ScalingAction { None, ScaleUp, ScaleDown }
        
        /// <summary>
        /// Performs the auto-scaling loop using Unity-optimized async patterns.
        /// </summary>
        private async UniTaskVoid PerformAutoScalingLoop(TimeSpan checkInterval)
        {
            while (_isAutoScalingActive && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await PerformScalingCheck();
                    await UniTask.Delay(checkInterval, cancellationToken: _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break; // Expected when stopping
                }
                catch (Exception ex)
                {
                    _loggingService.LogException("Auto-scaling loop encountered an error", ex);
                    await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: _cancellationTokenSource.Token);
                }
            }
        }
        
        /// <summary>
        /// Determines the appropriate scaling action for a pool.
        /// </summary>
        private ScalingAction DetermineScalingAction(string poolTypeName, IObjectPool pool, PoolScalingMetrics metrics)
        {
            var statistics = pool.GetStatistics();
            var config = pool.Configuration;
            
            // High utilization threshold (scale up)
            if (metrics.AverageUtilization > 0.8) // 80% utilization
            {
                metrics.ConsecutiveHighUtilization++;
                metrics.ConsecutiveLowUtilization = 0;
                
                // Scale up conditions
                if (metrics.ConsecutiveHighUtilization >= 3 &&
                    statistics.TotalCount < config.MaxCapacity &&
                    (DateTime.UtcNow - metrics.LastScaleUpTime).TotalMinutes >= 2)
                {
                    return ScalingAction.ScaleUp;
                }
            }
            // Low utilization threshold (scale down)
            else if (metrics.AverageUtilization < 0.2) // 20% utilization
            {
                metrics.ConsecutiveLowUtilization++;
                metrics.ConsecutiveHighUtilization = 0;
                
                // Scale down conditions
                if (metrics.ConsecutiveLowUtilization >= 5 &&
                    statistics.TotalCount > config.InitialCapacity &&
                    (DateTime.UtcNow - metrics.LastScaleDownTime).TotalMinutes >= 5)
                {
                    return ScalingAction.ScaleDown;
                }
            }
            else
            {
                // Reset consecutive counters for moderate utilization
                metrics.ConsecutiveHighUtilization = Math.Max(0, metrics.ConsecutiveHighUtilization - 1);
                metrics.ConsecutiveLowUtilization = Math.Max(0, metrics.ConsecutiveLowUtilization - 1);
            }
            
            return ScalingAction.None;
        }
        
        /// <summary>
        /// Executes pool scaling operation.
        /// </summary>
        private async UniTask ExecutePoolScaling(string poolTypeName, IObjectPool pool, PoolScalingMetrics metrics, ScalingAction action)
        {
            try
            {
                var config = pool.Configuration;
                var currentSize = pool.GetStatistics().TotalCount;
                int targetSize;
                
                switch (action)
                {
                    case ScalingAction.ScaleUp:
                        // Calculate scale up size (increase by 50% or min 10 objects)
                        var scaleUpAmount = Math.Max(10, (int)(currentSize * 0.5));
                        targetSize = Math.Min(config.MaxCapacity, currentSize + scaleUpAmount);
                        
                        if (targetSize > currentSize)
                        {
                            _loggingService.LogInfo($"Scaling up pool {poolTypeName} from {currentSize} to {targetSize}");
                            
                            metrics.LastScaleUpTime = DateTime.UtcNow;
                            metrics.CurrentCapacity = targetSize;
                            
                            // Publish scaling message
                            PublishPoolExpansionMessage(poolTypeName, currentSize, targetSize);
                        }
                        break;
                        
                    case ScalingAction.ScaleDown:
                        // Calculate scale down size (decrease by 30% or min 5 objects)
                        var scaleDownAmount = Math.Max(5, (int)(currentSize * 0.3));
                        targetSize = Math.Max(config.InitialCapacity, currentSize - scaleDownAmount);
                        
                        if (targetSize < currentSize)
                        {
                            _loggingService.LogInfo($"Scaling down pool {poolTypeName} from {currentSize} to {targetSize}");
                            
                            // Trim excess objects
                            pool.TrimExcess();
                            
                            metrics.LastScaleDownTime = DateTime.UtcNow;
                            metrics.CurrentCapacity = targetSize;
                            
                            // Publish scaling message
                            PublishPoolContractionMessage(poolTypeName, currentSize, targetSize);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Failed to scale pool {poolTypeName}", ex);
                
                _alertService?.RaiseAlert(
                    $"Pool scaling failed for {poolTypeName}: {ex.Message}",
                    AlertSeverity.Warning,
                    $"PoolAutoScalingService.{poolTypeName}"
                );
            }
        }
        
        /// <summary>
        /// Publishes a pool expansion message.
        /// </summary>
        private void PublishPoolExpansionMessage(string poolTypeName, int previousSize, int newSize)
        {
            try
            {
                var message = PoolExpansionMessage.Create(
                    strategyName: poolTypeName,
                    oldSize: previousSize,
                    newSize: newSize,
                    reason: "Automatic scaling due to high utilization",
                    source: new FixedString64Bytes("PoolAutoScalingService")
                );
                
                _messageBusService.PublishMessageAsync(message).Forget();
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to publish pool expansion message", ex);
            }
        }
        
        /// <summary>
        /// Publishes a pool contraction message.
        /// </summary>
        private void PublishPoolContractionMessage(string poolTypeName, int previousSize, int newSize)
        {
            try
            {
                var message = PoolContractionMessage.Create(
                    strategyName: poolTypeName,
                    oldSize: previousSize,
                    newSize: newSize,
                    reason: "Automatic scaling due to low utilization",
                    source: new FixedString64Bytes("PoolAutoScalingService")
                );
                
                _messageBusService.PublishMessageAsync(message).Forget();
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to publish pool contraction message", ex);
            }
        }
        
        /// <summary>
        /// Throws ObjectDisposedException if the service has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PoolAutoScalingService));
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        /// <summary>
        /// Disposes the auto-scaling service and stops all operations.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                StopAutoScaling();
                _registeredPools.Clear();
                _scalingMetrics.Clear();
                _disposed = true;
            }
        }
        
        #endregion
    }
}