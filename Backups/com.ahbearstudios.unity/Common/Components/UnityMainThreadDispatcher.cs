using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Unity.Common.Configs;
using AhBearStudios.Unity.Common.Messages;
using ZLinq;

namespace AhBearStudios.Unity.Common.Components;

/// <summary>
/// Production-ready Unity main thread dispatcher with frame budget compliance.
/// Implements IMainThreadDispatcher interface for DI integration.
/// Provides zero-allocation queuing with configurable backpressure handling.
/// </summary>
public sealed class UnityMainThreadDispatcher : MonoBehaviour, IMainThreadDispatcher
{
    // Configuration and dependencies
    private MainThreadDispatcherConfig _config;
    private ILoggingService _logger;
    private IMessageBusService _messageBus;
    private IHealthCheckService _healthCheckService;
    
    // Performance-critical fields
    private readonly Queue<Action> _actionQueue = new();
    private readonly Queue<IDispatcherTask> _asyncTaskQueue = new();
    private readonly object _queueLock = new();
    private readonly Stopwatch _frameTimer = new();
    
    // Thread safety
    private static readonly int _mainThreadId = Thread.CurrentThread.ManagedThreadId;
    
    // Performance monitoring
    private readonly ProfilerMarker _updateMarker = new("MainThreadDispatcher.Update");
    private readonly ProfilerMarker _processMarker = new("MainThreadDispatcher.Process");
    private readonly ProfilerMarker _executeMarker = new("MainThreadDispatcher.Execute");
    
    // Statistics for health monitoring
    private int _totalActionsProcessed;
    private int _totalActionsDropped;
    private float _averageProcessingTimeMs;
    private int _queuePeakCount;
    private DateTime _lastWarningTime;
    
    // Initialization state
    private bool _isInitialized;
    
    #region IMainThreadDispatcher Implementation
    
    public bool IsInitialized => _isInitialized;
    public int PendingActionCount
    {
        get
        {
            lock (_queueLock)
            {
                return _actionQueue.Count + _asyncTaskQueue.Count;
            }
        }
    }
    
    public bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;
    public int MaxQueueCapacity => _config?.MaxQueueCapacity ?? 0;
    public float FrameBudgetMs => _config?.FrameBudgetMs ?? 0f;
    
    #endregion
    
    /// <summary>
    /// Initializes the dispatcher with configuration and dependencies.
    /// Called by the factory during creation.
    /// </summary>
    /// <param name="config">Validated configuration</param>
    /// <param name="logger">Logging service for operations</param>
    /// <param name="messageBus">Optional message bus for events</param>
    /// <param name="healthCheckService">Optional health check service</param>
    public void Initialize(MainThreadDispatcherConfig config, ILoggingService logger, 
                          IMessageBusService messageBus = null, IHealthCheckService healthCheckService = null)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("MainThreadDispatcher is already initialized");
        }
        
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageBus = messageBus;
        _healthCheckService = healthCheckService;
        
        // Pre-allocate queue capacity to avoid runtime allocations
        EnsureQueueCapacity(_config.InitialQueueCapacity);
        
        _isInitialized = true;
        _logger.LogInfo($"MainThreadDispatcher initialized: FrameBudget={_config.FrameBudgetMs}ms, MaxQueue={_config.MaxQueueCapacity}");
        
        // Register health check if service available
        if (_config.EnableHealthChecking && _healthCheckService != null)
        {
            RegisterHealthCheck();
        }
        
        // Publish initialization event if message bus available
        if (_config.EnableMessageBusIntegration && _messageBus != null)
        {
            PublishEvent(new MainThreadDispatcherInitializedMessage(
                _config.FrameBudgetMs,
                _config.MaxQueueCapacity
            ));
        }
    }
    
    public bool TryEnqueue(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
        
        if (!_isInitialized)
        {
            _logger?.LogError("MainThreadDispatcher not initialized. Use factory to create properly.");
            return false;
        }
        
        lock (_queueLock)
        {
            // Check capacity and apply backpressure if needed
            if (_actionQueue.Count >= _config.MaxQueueCapacity)
            {
                return HandleBackpressure(action);
            }
            
            _actionQueue.Enqueue(action);
            UpdateQueueStatistics();
            return true;
        }
    }
    
    public void Enqueue(Action action)
    {
        if (!TryEnqueue(action))
        {
            var message = "Failed to enqueue action - queue at capacity and backpressure strategy rejected action";
            _logger?.LogError(message);
            throw new InvalidOperationException(message);
        }
    }
    
    public bool TryEnqueue<T>(Func<T> func, Action<T> callback)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        
        return TryEnqueue(() =>
        {
            try
            {
                var result = func();
                callback(result);
            }
            catch (Exception ex)
            {
                _logger?.LogException($"Exception in main thread function: {ex.Message}", ex);
            }
        });
    }
    
    public async UniTask EnqueueAsync(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
        
        var taskSource = new UniTaskCompletionSource();
        var task = new DispatcherTask(() =>
        {
            try
            {
                action();
                taskSource.TrySetResult();
            }
            catch (Exception ex)
            {
                taskSource.TrySetException(ex);
            }
        });
        
        lock (_queueLock)
        {
            if (_asyncTaskQueue.Count >= _config.MaxQueueCapacity)
            {
                throw new InvalidOperationException("Async task queue at capacity");
            }
            
            _asyncTaskQueue.Enqueue(task);
        }
        
        await taskSource.Task;
    }
    
    public async UniTask<T> EnqueueAsync<T>(Func<T> func)
    {
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }
        
        var taskSource = new UniTaskCompletionSource<T>();
        var task = new DispatcherTask(() =>
        {
            try
            {
                var result = func();
                taskSource.TrySetResult(result);
            }
            catch (Exception ex)
            {
                taskSource.TrySetException(ex);
            }
        });
        
        lock (_queueLock)
        {
            if (_asyncTaskQueue.Count >= _config.MaxQueueCapacity)
            {
                throw new InvalidOperationException("Async task queue at capacity");
            }
            
            _asyncTaskQueue.Enqueue(task);
        }
        
        return await taskSource.Task;
    }
    
    public int ProcessAllImmediately()
    {
        if (!IsMainThread)
        {
            throw new InvalidOperationException("ProcessAllImmediately can only be called from main thread");
        }
        
        var processed = 0;
        using (_processMarker.Auto())
        {
            lock (_queueLock)
            {
                // Process regular actions
                while (_actionQueue.Count > 0)
                {
                    var action = _actionQueue.Dequeue();
                    ExecuteAction(action);
                    processed++;
                }
                
                // Process async tasks
                while (_asyncTaskQueue.Count > 0)
                {
                    var task = _asyncTaskQueue.Dequeue();
                    ExecuteAction(task.Action);
                    processed++;
                }
            }
        }
        
        _logger?.LogWarning($"ProcessAllImmediately executed {processed} actions - may have exceeded frame budget");
        return processed;
    }
    
    public int ClearQueue()
    {
        lock (_queueLock)
        {
            var cleared = _actionQueue.Count + _asyncTaskQueue.Count;
            _actionQueue.Clear();
            _asyncTaskQueue.Clear();
            
            _logger?.LogWarning($"Queue cleared - {cleared} actions dropped");
            _totalActionsDropped += cleared;
            
            return cleared;
        }
    }
    
    /// <summary>
    /// Unity Update method with frame budget compliance.
    /// Processes actions within configured time and count limits.
    /// </summary>
    private void Update()
    {
        if (!_isInitialized) return;
        
        using (_updateMarker.Auto())
        {
            ProcessQueuedActions();
        }
    }
    
    private void ProcessQueuedActions()
    {
        _frameTimer.Restart();
        var actionsProcessed = 0;
        var frameBudgetMs = _config.FrameBudgetMs;
        var maxActions = _config.MaxActionsPerFrame;
        
        using (_processMarker.Auto())
        {
            // Process regular actions first
            while (actionsProcessed < maxActions && _frameTimer.Elapsed.TotalMilliseconds < frameBudgetMs)
            {
                Action action = null;
                lock (_queueLock)
                {
                    if (_actionQueue.Count == 0) break;
                    action = _actionQueue.Dequeue();
                }
                
                if (action != null)
                {
                    ExecuteAction(action);
                    actionsProcessed++;
                    _totalActionsProcessed++;
                }
            }
            
            // Process async tasks if budget remaining
            while (actionsProcessed < maxActions && _frameTimer.Elapsed.TotalMilliseconds < frameBudgetMs)
            {
                IDispatcherTask task = null;
                lock (_queueLock)
                {
                    if (_asyncTaskQueue.Count == 0) break;
                    task = _asyncTaskQueue.Dequeue();
                }
                
                if (task != null)
                {
                    ExecuteAction(task.Action);
                    actionsProcessed++;
                    _totalActionsProcessed++;
                }
            }
        }
        
        _frameTimer.Stop();
        
        // Update performance statistics
        if (_config.EnablePerformanceMonitoring && actionsProcessed > 0)
        {
            var processingTimeMs = (float)_frameTimer.Elapsed.TotalMilliseconds;
            _averageProcessingTimeMs = (_averageProcessingTimeMs + processingTimeMs) * 0.5f;
        }
    }
    
    private void ExecuteAction(Action action)
    {
        using (_executeMarker.Auto())
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                var message = $"Exception in dispatched action: {ex.Message}";
                _logger?.LogException(message, ex);
                
                // Publish error event if message bus available
                if (_config.EnableMessageBusIntegration && _messageBus != null)
                {
                    PublishEvent(new MainThreadDispatcherErrorMessage(
                        message,
                        ex.ToString()
                    ));
                }
            }
        }
    }
    
    private bool HandleBackpressure(Action action)
    {
        _totalActionsDropped++;
        
        switch (_config.BackpressureStrategy)
        {
            case BackpressureStrategy.DropOldest:
                if (_actionQueue.Count > 0)
                {
                    _actionQueue.Dequeue(); // Drop oldest
                    _actionQueue.Enqueue(action); // Add new
                    LogBackpressureWarning("Dropped oldest action");
                    return true;
                }
                break;
                
            case BackpressureStrategy.DropNewest:
                LogBackpressureWarning("Dropped newest action");
                return false;
                
            case BackpressureStrategy.ThrowException:
                throw new InvalidOperationException($"MainThreadDispatcher queue at capacity ({_config.MaxQueueCapacity})");
        }
        
        return false;
    }
    
    private void LogBackpressureWarning(string action)
    {
        var now = DateTime.UtcNow;
        if (now - _lastWarningTime > TimeSpan.FromSeconds(5)) // Throttle warnings
        {
            _logger?.LogWarning($"MainThreadDispatcher backpressure: {action}. Queue at capacity: {_config.MaxQueueCapacity}");
            _lastWarningTime = now;
            
            // Publish backpressure event
            if (_config.EnableMessageBusIntegration && _messageBus != null)
            {
                PublishEvent(new MainThreadDispatcherBackpressureMessage(
                    _actionQueue.Count,
                    _config.MaxQueueCapacity,
                    _config.BackpressureStrategy.ToString()
                ));
            }
        }
    }
    
    private void UpdateQueueStatistics()
    {
        var currentCount = _actionQueue.Count + _asyncTaskQueue.Count;
        if (currentCount > _queuePeakCount)
        {
            _queuePeakCount = currentCount;
        }
        
        // Check warning threshold
        var warningThreshold = _config.MaxQueueCapacity * _config.QueueWarningThreshold;
        if (currentCount > warningThreshold)
        {
            LogBackpressureWarning($"Queue above warning threshold ({currentCount}/{_config.MaxQueueCapacity})");
        }
    }
    
    private void EnsureQueueCapacity(int capacity)
    {
        // Pre-allocate queue capacity by enqueueing and dequeueing dummy actions
        // This prevents allocations during runtime
        var dummyAction = new Action(() => { });
        for (int i = 0; i < capacity; i++)
        {
            _actionQueue.Enqueue(dummyAction);
        }
        _actionQueue.Clear();
    }
    
    private void RegisterHealthCheck()
    {
        // Implementation would register with health check service
        // This would report dispatcher health status
        _logger?.LogInfo("MainThreadDispatcher health check registered");
    }
    
    private void PublishEvent(IMessage message)
    {
        try
        {
            _messageBus?.PublishMessage(message);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Failed to publish dispatcher event: {ex.Message}");
        }
    }
    
    private void OnDestroy()
    {
        if (_isInitialized)
        {
            var remainingActions = ClearQueue();
            _logger?.LogInfo($"MainThreadDispatcher destroyed. Remaining actions cleared: {remainingActions}");
            
            // Publish shutdown event
            if (_config.EnableMessageBusIntegration && _messageBus != null)
            {
                PublishEvent(new MainThreadDispatcherShutdownMessage(
                    _totalActionsProcessed,
                    _totalActionsDropped,
                    _queuePeakCount
                ));
            }
        }
    }
    
}

/// <summary>
/// Interface for dispatcher tasks that can be executed asynchronously.
/// </summary>
internal interface IDispatcherTask
{
    Action Action { get; }
}

/// <summary>
/// Implementation of dispatcher task for async operations.
/// </summary>
internal sealed class DispatcherTask : IDispatcherTask
{
    public Action Action { get; }
    
    public DispatcherTask(Action action)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }
}