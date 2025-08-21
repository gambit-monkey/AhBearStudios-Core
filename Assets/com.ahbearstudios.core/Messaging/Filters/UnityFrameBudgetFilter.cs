using System;
using MessagePipe;
using Unity.Profiling;
using UnityEngine;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.Messaging.Filters;

/// <summary>
/// Unity-specific MessagePipe filter that enforces frame budget compliance for 60+ FPS performance.
/// Defers message processing when frame time budget is exceeded to maintain smooth gameplay.
/// Essential for Unity game development to prevent frame drops and maintain consistent performance.
/// </summary>
/// <typeparam name="TMessage">The message type implementing IMessage</typeparam>
public sealed class UnityFrameBudgetFilter<TMessage> : MessageHandlerFilter<TMessage>
    where TMessage : IMessage
{
    private readonly ILoggingService _logger;
    private readonly ProfilerMarker _filterMarker;
    private readonly float _maxFrameTimeMs;
    private readonly bool _enableDeferral;
    
    private static readonly ProfilerMarker _staticFilterMarker = new("UnityFrameBudgetFilter.Handle");
    private static readonly ProfilerMarker _frameBudgetMarker = new("UnityFrameBudgetFilter.FrameBudgetCheck");

    /// <summary>
    /// Initializes a new UnityFrameBudgetFilter with frame budget enforcement.
    /// </summary>
    /// <param name="targetFrameRate">Target frame rate for budget calculation (default: 60 FPS)</param>
    /// <param name="budgetUsageThreshold">Percentage of frame budget to allow (0.0 to 1.0, default: 0.8 = 80%)</param>
    /// <param name="enableDeferral">Whether to defer messages when budget is exceeded (default: true)</param>
    /// <param name="logger">Optional logging service for budget violations</param>
    public UnityFrameBudgetFilter(
        int targetFrameRate = 60,
        float budgetUsageThreshold = 0.8f,
        bool enableDeferral = true,
        ILoggingService logger = null)
    {
        if (targetFrameRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetFrameRate), "Target frame rate must be positive");
        if (budgetUsageThreshold is < 0.0f or > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(budgetUsageThreshold), "Budget usage threshold must be between 0.0 and 1.0");

        _maxFrameTimeMs = (1000.0f / targetFrameRate) * budgetUsageThreshold;
        _enableDeferral = enableDeferral;
        _logger = logger;
        _filterMarker = new ProfilerMarker($"UnityFrameBudgetFilter<{typeof(TMessage).Name}>.Handle");
    }

    /// <summary>
    /// Handles message processing with Unity frame budget enforcement.
    /// Processes messages immediately if frame budget allows, otherwise defers to next frame.
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <param name="next">The next handler in the filter chain</param>
    public override void Handle(TMessage message, Action<TMessage> next)
    {
        using (_staticFilterMarker.Auto())
        using (_filterMarker.Auto())
        {
            if (message == null)
            {
                _logger?.LogWarning($"UnityFrameBudgetFilter<{typeof(TMessage).Name}>: Received null message");
                return;
            }

            // Check frame budget before processing
            if (!IsFrameBudgetAvailable())
            {
                if (_enableDeferral)
                {
                    _logger?.LogDebug($"UnityFrameBudgetFilter<{typeof(TMessage).Name}>: Frame budget exceeded, deferring message {message.Id} to next frame");
                    
                    // Defer message processing to next frame using Unity's main thread
                    UnityMainThreadDispatcher.Enqueue(() => 
                    {
                        _logger?.LogDebug($"UnityFrameBudgetFilter<{typeof(TMessage).Name}>: Processing deferred message {message.Id}");
                        next(message);
                    });
                    return;
                }
                else
                {
                    _logger?.LogWarning($"UnityFrameBudgetFilter<{typeof(TMessage).Name}>: Frame budget exceeded, dropping message {message.Id}");
                    return; // Drop the message if deferral is disabled
                }
            }

            // Frame budget is available, process immediately
            _logger?.LogDebug($"UnityFrameBudgetFilter<{typeof(TMessage).Name}>: Processing message {message.Id} within frame budget");
            next(message);
        }
    }

    /// <summary>
    /// Checks if sufficient frame budget is available for message processing.
    /// </summary>
    /// <returns>True if frame budget allows processing, false otherwise</returns>
    private bool IsFrameBudgetAvailable()
    {
        using (_frameBudgetMarker.Auto())
        {
            // Use Unity's unscaled delta time to get the actual frame time
            var currentFrameTimeMs = Time.unscaledDeltaTime * 1000.0f;
            
            var budgetRemaining = _maxFrameTimeMs - currentFrameTimeMs;
            var budgetAvailable = budgetRemaining > 0.0f;
            
            if (!budgetAvailable)
            {
                _logger?.LogDebug($"UnityFrameBudgetFilter<{typeof(TMessage).Name}>: Frame budget exceeded - " +
                                $"current: {currentFrameTimeMs:F2}ms, max: {_maxFrameTimeMs:F2}ms, " +
                                $"remaining: {budgetRemaining:F2}ms");
            }
            
            return budgetAvailable;
        }
    }
}

/// <summary>
/// Unity main thread dispatcher for deferred message processing.
/// Ensures messages are processed on Unity's main thread during the next Update cycle.
/// </summary>
public static class UnityMainThreadDispatcher
{
    private static readonly System.Collections.Concurrent.ConcurrentQueue<Action> _executionQueue = new();
    private static volatile bool _isInitialized = false;

    /// <summary>
    /// Enqueues an action to be executed on Unity's main thread during the next Update cycle.
    /// </summary>
    /// <param name="action">The action to execute on the main thread</param>
    public static void Enqueue(Action action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        _executionQueue.Enqueue(action);
        
        // Ensure the dispatcher is initialized
        if (!_isInitialized)
        {
            Initialize();
        }
    }

    /// <summary>
    /// Initializes the main thread dispatcher (called automatically when needed).
    /// </summary>
    private static void Initialize()
    {
        if (_isInitialized)
            return;

        // In a real implementation, this would create a MonoBehaviour
        // that processes the queue during Update()
        // For now, we'll just mark as initialized
        _isInitialized = true;
    }

    /// <summary>
    /// Processes all queued actions (should be called from Unity's Update method).
    /// </summary>
    public static void ProcessQueue()
    {
        while (_executionQueue.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}