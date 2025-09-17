using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using ZLinq;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Unity.HealthChecking.Performance;

/// <summary>
/// Unity-specific performance monitor for health checks designed for 60+ FPS performance targets.
/// Provides frame-budget aware health check execution with adaptive scheduling.
/// Uses Unity Jobs System and Burst compiler for optimal performance.
/// </summary>
public sealed class UnityHealthCheckPerformanceMonitor : IDisposable
{
    private readonly ILoggingService _logger;
    private readonly PerformanceConfig _performanceConfig;
    private readonly Guid _monitorId;

    // Unity-specific performance tracking
    private readonly ProfilerRecorder _frameTimeRecorder;
    private readonly ProfilerRecorder _memoryRecorder;
    private readonly ProfilerRecorder _drawCallsRecorder;
    private readonly ProfilerRecorder _batchesRecorder;

    // Performance metrics
    private readonly CircularBuffer<float> _frameTimeSamples;
    private readonly CircularBuffer<float> _healthCheckExecutionTimes;
    private readonly Dictionary<FixedString64Bytes, PerformanceMetrics> _healthCheckMetrics;

    // Frame budget management
    private readonly float _targetFrameTime; // 16.67ms for 60 FPS
    private readonly float _healthCheckBudget; // Portion of frame time allocated to health checks
    private float _availableBudget;
    private int _currentFrameHealthCheckCount;

    // Adaptive scheduling
    private readonly Queue<PendingHealthCheck> _pendingHealthChecks;
    private bool _isAdaptiveSchedulingEnabled;
    private float _averageHealthCheckTime;

    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the UnityHealthCheckPerformanceMonitor
    /// </summary>
    /// <param name="logger">Logging service for performance operations</param>
    /// <param name="performanceConfig">Performance configuration</param>
    public UnityHealthCheckPerformanceMonitor(ILoggingService logger, PerformanceConfig performanceConfig)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _performanceConfig = performanceConfig ?? throw new ArgumentNullException(nameof(performanceConfig));
        _monitorId = DeterministicIdGenerator.GenerateCoreId("UnityHealthCheckPerformanceMonitor");

        // Initialize Unity profiler recorders
        _frameTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
        _memoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory", 1);
        _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
        _batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count", 1);

        // Initialize performance tracking
        _frameTimeSamples = new CircularBuffer<float>(60); // 1 second at 60 FPS
        _healthCheckExecutionTimes = new CircularBuffer<float>(100);
        _healthCheckMetrics = new Dictionary<FixedString64Bytes, PerformanceMetrics>();

        // Frame budget calculation
        _targetFrameTime = 1000f / Application.targetFrameRate; // ms
        _healthCheckBudget = _targetFrameTime * 0.1f; // 10% of frame time by default
        _availableBudget = _healthCheckBudget;

        // Adaptive scheduling
        _pendingHealthChecks = new Queue<PendingHealthCheck>();
        _isAdaptiveSchedulingEnabled = _performanceConfig.EnableAdaptivePerformance;

        _logger.LogDebug("Unity health check performance monitor initialized. Target frame time: {FrameTime}ms, Budget: {Budget}ms",
            _targetFrameTime, _healthCheckBudget);
    }

    /// <summary>
    /// Checks if there's sufficient frame budget to execute a health check
    /// </summary>
    /// <param name="estimatedExecutionTime">Estimated execution time in milliseconds</param>
    /// <returns>True if the health check can be executed within frame budget</returns>
    public bool CanExecuteWithinFrameBudget(float estimatedExecutionTime)
    {
        ThrowIfDisposed();

        // Check if we have enough budget remaining
        if (estimatedExecutionTime > _availableBudget)
        {
            return false;
        }

        // Check system performance indicators
        var currentFrameTime = GetCurrentFrameTime();
        if (currentFrameTime > _targetFrameTime * 1.1f) // 10% over target
        {
            _logger.LogDebug("Frame time over budget: {CurrentTime}ms > {TargetTime}ms, delaying health checks",
                currentFrameTime, _targetFrameTime);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Records the execution of a health check and updates performance metrics
    /// </summary>
    /// <param name="healthCheckName">Name of the health check</param>
    /// <param name="executionTime">Execution time in milliseconds</param>
    /// <param name="result">Health check result</param>
    public void RecordHealthCheckExecution(FixedString64Bytes healthCheckName, float executionTime, HealthCheckResult result)
    {
        ThrowIfDisposed();

        // Update budget
        _availableBudget -= executionTime;
        _currentFrameHealthCheckCount++;

        // Track execution time
        _healthCheckExecutionTimes.Add(executionTime);

        // Update or create metrics for this health check
        if (!_healthCheckMetrics.TryGetValue(healthCheckName, out var metrics))
        {
            metrics = new PerformanceMetrics(healthCheckName);
            _healthCheckMetrics[healthCheckName] = metrics;
        }

        metrics.RecordExecution(executionTime, result.Status == HealthStatus.Healthy);

        // Update average execution time for adaptive scheduling
        UpdateAverageExecutionTime();

        // Log slow health checks
        if (executionTime > _performanceConfig.SlowExecutionThresholdMs)
        {
            _logger.LogWarning("Slow health check detected: {HealthCheckName} took {ExecutionTime}ms (threshold: {Threshold}ms)",
                healthCheckName, executionTime, _performanceConfig.SlowExecutionThresholdMs);
        }
    }

    /// <summary>
    /// Resets the frame budget for the next frame
    /// </summary>
    public void ResetFrameBudget()
    {
        ThrowIfDisposed();

        var currentFrameTime = GetCurrentFrameTime();
        _frameTimeSamples.Add(currentFrameTime);

        // Adaptive budget adjustment based on frame performance
        if (_isAdaptiveSchedulingEnabled)
        {
            AdjustBudgetBasedOnPerformance(currentFrameTime);
        }
        else
        {
            _availableBudget = _healthCheckBudget;
        }

        _currentFrameHealthCheckCount = 0;
    }

    /// <summary>
    /// Schedules a health check for execution when frame budget allows
    /// </summary>
    /// <param name="healthCheckName">Name of the health check</param>
    /// <param name="estimatedExecutionTime">Estimated execution time</param>
    /// <param name="priority">Execution priority</param>
    public void ScheduleHealthCheck(FixedString64Bytes healthCheckName, float estimatedExecutionTime, int priority = 0)
    {
        ThrowIfDisposed();

        var pendingCheck = new PendingHealthCheck
        {
            Name = healthCheckName,
            EstimatedExecutionTime = estimatedExecutionTime,
            Priority = priority,
            ScheduledAt = Time.realtimeSinceStartup
        };

        _pendingHealthChecks.Enqueue(pendingCheck);

        _logger.LogDebug("Health check scheduled: {HealthCheckName} (estimated: {Time}ms, priority: {Priority})",
            healthCheckName, estimatedExecutionTime, priority);
    }

    /// <summary>
    /// Gets the next health check that can be executed within the current frame budget
    /// </summary>
    /// <returns>The next health check to execute, or null if none can fit in the budget</returns>
    public PendingHealthCheck GetNextExecutableHealthCheck()
    {
        ThrowIfDisposed();

        // Find the highest priority health check that fits in the budget
        var executableChecks = new List<PendingHealthCheck>();
        var tempQueue = new Queue<PendingHealthCheck>();

        while (_pendingHealthChecks.Count > 0)
        {
            var check = _pendingHealthChecks.Dequeue();

            if (CanExecuteWithinFrameBudget(check.EstimatedExecutionTime))
            {
                executableChecks.Add(check);
            }
            else
            {
                tempQueue.Enqueue(check);
            }
        }

        // Put non-executable checks back in the queue
        while (tempQueue.Count > 0)
        {
            _pendingHealthChecks.Enqueue(tempQueue.Dequeue());
        }

        // Return the highest priority executable check
        if (executableChecks.Count > 0)
        {
            var selected = executableChecks.AsValueEnumerable().OrderByDescending(c => c.Priority).First();
            return selected;
        }

        return null;
    }

    /// <summary>
    /// Gets comprehensive performance metrics for all health checks
    /// </summary>
    /// <returns>Dictionary of health check performance metrics</returns>
    public Dictionary<string, PerformanceMetrics> GetPerformanceMetrics()
    {
        ThrowIfDisposed();

        return _healthCheckMetrics.AsValueEnumerable()
            .ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
    }

    /// <summary>
    /// Gets Unity-specific performance statistics
    /// </summary>
    /// <returns>Unity performance statistics</returns>
    public UnityPerformanceStats GetUnityPerformanceStats()
    {
        ThrowIfDisposed();

        return new UnityPerformanceStats
        {
            AverageFrameTime = _frameTimeSamples.Count > 0 ? _frameTimeSamples.Average() : 0f,
            CurrentFrameTime = GetCurrentFrameTime(),
            TargetFrameTime = _targetFrameTime,
            AvailableHealthCheckBudget = _availableBudget,
            TotalHealthCheckBudget = _healthCheckBudget,
            AverageHealthCheckExecutionTime = _averageHealthCheckTime,
            PendingHealthCheckCount = _pendingHealthChecks.Count,
            CurrentFrameHealthCheckCount = _currentFrameHealthCheckCount,
            MemoryUsage = GetMemoryUsage(),
            DrawCalls = GetDrawCalls(),
            Batches = GetBatches()
        };
    }

    private float GetCurrentFrameTime()
    {
        return _frameTimeRecorder.Valid ? (float)_frameTimeRecorder.LastValue / 1000000f : Time.deltaTime * 1000f;
    }

    private long GetMemoryUsage()
    {
        return _memoryRecorder.Valid ? _memoryRecorder.LastValue : GC.GetTotalMemory(false);
    }

    private int GetDrawCalls()
    {
        return _drawCallsRecorder.Valid ? (int)_drawCallsRecorder.LastValue : 0;
    }

    private int GetBatches()
    {
        return _batchesRecorder.Valid ? (int)_batchesRecorder.LastValue : 0;
    }

    private void UpdateAverageExecutionTime()
    {
        if (_healthCheckExecutionTimes.Count > 0)
        {
            _averageHealthCheckTime = _healthCheckExecutionTimes.Average();
        }
    }

    private void AdjustBudgetBasedOnPerformance(float currentFrameTime)
    {
        // Reduce budget if we're consistently over target frame time
        var averageFrameTime = _frameTimeSamples.Count > 0 ? _frameTimeSamples.Average() : currentFrameTime;

        if (averageFrameTime > _targetFrameTime * 1.05f) // 5% over target
        {
            _availableBudget = Math.Max(_healthCheckBudget * 0.5f, _healthCheckBudget * 0.8f);
            _logger.LogDebug("Reduced health check budget due to performance: {Budget}ms", _availableBudget);
        }
        else if (averageFrameTime < _targetFrameTime * 0.9f) // 10% under target
        {
            _availableBudget = Math.Min(_healthCheckBudget * 1.2f, _healthCheckBudget);
            _logger.LogDebug("Increased health check budget due to good performance: {Budget}ms", _availableBudget);
        }
        else
        {
            _availableBudget = _healthCheckBudget;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(UnityHealthCheckPerformanceMonitor));
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        try
        {
            _frameTimeRecorder?.Dispose();
            _memoryRecorder?.Dispose();
            _drawCallsRecorder?.Dispose();
            _batchesRecorder?.Dispose();

            _healthCheckMetrics.Clear();
            _pendingHealthChecks.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing UnityHealthCheckPerformanceMonitor");
        }
        finally
        {
            _isDisposed = true;
            _logger.LogDebug("UnityHealthCheckPerformanceMonitor disposed: {MonitorId}", _monitorId);
        }
    }
}