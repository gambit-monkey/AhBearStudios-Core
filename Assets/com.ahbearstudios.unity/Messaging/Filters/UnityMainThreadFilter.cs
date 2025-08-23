using System;
using System.Threading;
using MessagePipe;
using Unity.Profiling;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Logging;
using AhBearStudios.Unity.Common.Components;

namespace AhBearStudios.Unity.Messaging.Filters;

/// <summary>
/// Unity-specific MessagePipe filter that ensures messages are processed on Unity's main thread.
/// Critical for Unity game development as most Unity APIs are not thread-safe and must be called from the main thread.
/// Automatically dispatches messages to the main thread when called from background threads.
/// </summary>
/// <typeparam name="TMessage">The message type implementing IMessage</typeparam>
public sealed class UnityMainThreadFilter<TMessage> : MessageHandlerFilter<TMessage>
    where TMessage : IMessage
{
    private readonly IMainThreadDispatcher _dispatcher;
    private readonly ILoggingService _logger;
    private readonly ProfilerMarker _filterMarker;
    private readonly bool _strictMode;
    private readonly bool _enableAutoDispatch;
    
    private static readonly ProfilerMarker _staticFilterMarker = new("UnityMainThreadFilter.Handle");
    private static readonly ProfilerMarker _threadCheckMarker = new("UnityMainThreadFilter.ThreadCheck");
    
    // Cache the main thread ID for efficient checking
    private static readonly int _mainThreadId = Thread.CurrentThread.ManagedThreadId;

    /// <summary>
    /// Initializes a new UnityMainThreadFilter with main thread enforcement.
    /// </summary>
    /// <param name="dispatcher">Main thread dispatcher service for enqueueing actions</param>
    /// <param name="strictMode">If true, throws exception when called from wrong thread (default: false)</param>
    /// <param name="enableAutoDispatch">If true, automatically dispatches to main thread (default: true)</param>
    /// <param name="logger">Optional logging service for thread violations</param>
    public UnityMainThreadFilter(
        IMainThreadDispatcher dispatcher,
        bool strictMode = false,
        bool enableAutoDispatch = true,
        ILoggingService logger = null)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _strictMode = strictMode;
        _enableAutoDispatch = enableAutoDispatch;
        _logger = logger;
        _filterMarker = new ProfilerMarker($"UnityMainThreadFilter<{typeof(TMessage).Name}>.Handle");
    }

    /// <summary>
    /// Gets whether the current execution is on Unity's main thread.
    /// </summary>
    public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;

    /// <summary>
    /// Handles message processing with Unity main thread enforcement.
    /// Ensures messages are processed on the main thread for Unity API safety.
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
                _logger?.LogWarning($"UnityMainThreadFilter<{typeof(TMessage).Name}>: Received null message");
                return;
            }

            if (IsCurrentThreadMainThread())
            {
                // Already on main thread, process immediately
                _logger?.LogDebug($"UnityMainThreadFilter<{typeof(TMessage).Name}>: Processing message {message.Id} on main thread");
                next(message);
            }
            else
            {
                // Not on main thread - handle based on configuration
                HandleOffMainThread(message, next);
            }
        }
    }

    /// <summary>
    /// Checks if the current thread is Unity's main thread.
    /// </summary>
    /// <returns>True if on main thread, false otherwise</returns>
    private bool IsCurrentThreadMainThread()
    {
        using (_threadCheckMarker.Auto())
        {
            return IsMainThread;
        }
    }

    /// <summary>
    /// Handles message processing when called from a background thread.
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <param name="next">The next handler in the filter chain</param>
    private void HandleOffMainThread(TMessage message, Action<TMessage> next)
    {
        var currentThreadId = Thread.CurrentThread.ManagedThreadId;
        var threadName = Thread.CurrentThread.Name ?? "Unknown";
        
        _logger?.LogWarning($"UnityMainThreadFilter<{typeof(TMessage).Name}>: Message {message.Id} received on background thread " +
                          $"(ID: {currentThreadId}, Name: {threadName}) instead of main thread (ID: {_mainThreadId})");

        if (_strictMode)
        {
            var errorMessage = $"UnityMainThreadFilter<{typeof(TMessage).Name}>: Strict mode violation - " +
                             $"message {message.Id} must be processed on Unity main thread (current: {currentThreadId}, main: {_mainThreadId})";
            
            _logger?.LogException(errorMessage, new InvalidOperationException(errorMessage));
            throw new InvalidOperationException(errorMessage);
        }

        if (_enableAutoDispatch)
        {
            _logger?.LogInfo($"UnityMainThreadFilter<{typeof(TMessage).Name}>: Auto-dispatching message {message.Id} to main thread");
            
            // Dispatch to Unity's main thread using dependency-injected dispatcher
            _dispatcher.Enqueue(() =>
            {
                _logger?.LogDebug($"UnityMainThreadFilter<{typeof(TMessage).Name}>: Processing dispatched message {message.Id} on main thread");
                next(message);
            });
        }
        else
        {
            _logger?.LogWarning($"UnityMainThreadFilter<{typeof(TMessage).Name}>: Dropping message {message.Id} - " +
                              "auto-dispatch disabled and not on main thread");
            // Message is dropped if auto-dispatch is disabled
        }
    }
}