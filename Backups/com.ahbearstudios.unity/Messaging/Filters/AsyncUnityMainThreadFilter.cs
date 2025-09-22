using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Unity.Profiling;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Logging;
using AhBearStudios.Unity.Common.Components;

namespace AhBearStudios.Unity.Messaging.Filters;

/// <summary>
/// Async version of UnityMainThreadFilter for async message handlers.
/// Ensures async message processing happens on Unity's main thread.
/// </summary>
/// <typeparam name="TMessage">The message type implementing IMessage</typeparam>
public sealed class AsyncUnityMainThreadFilter<TMessage> : AsyncMessageHandlerFilter<TMessage>
    where TMessage : IMessage
{
    private readonly IMainThreadDispatcher _dispatcher;
    private readonly ILoggingService _logger;
    private readonly ProfilerMarker _filterMarker;
    private readonly bool _strictMode;
    private readonly bool _enableAutoDispatch;
    
    private static readonly ProfilerMarker _staticFilterMarker = new("AsyncUnityMainThreadFilter.Handle");

    /// <summary>
    /// Initializes a new AsyncUnityMainThreadFilter with main thread enforcement.
    /// </summary>
    /// <param name="dispatcher">Main thread dispatcher service for enqueueing actions</param>
    /// <param name="strictMode">If true, throws exception when called from wrong thread (default: false)</param>
    /// <param name="enableAutoDispatch">If true, automatically dispatches to main thread (default: true)</param>
    /// <param name="logger">Optional logging service for thread violations</param>
    public AsyncUnityMainThreadFilter(
        IMainThreadDispatcher dispatcher,
        bool strictMode = false,
        bool enableAutoDispatch = true,
        ILoggingService logger = null)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _strictMode = strictMode;
        _enableAutoDispatch = enableAutoDispatch;
        _logger = logger;
        _filterMarker = new ProfilerMarker($"AsyncUnityMainThreadFilter<{typeof(TMessage).Name}>.Handle");
    }

    /// <summary>
    /// Handles message processing asynchronously with Unity main thread enforcement.
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="next">The next async handler in the filter chain</param>
    public override async UniTask HandleAsync(TMessage message, CancellationToken cancellationToken, Func<TMessage, CancellationToken, UniTask> next)
    {
        using (_staticFilterMarker.Auto())
        using (_filterMarker.Auto())
        {
            if (message == null)
            {
                _logger?.LogWarning($"AsyncUnityMainThreadFilter<{typeof(TMessage).Name}>: Received null message");
                return;
            }

            if (UnityMainThreadFilter<TMessage>.IsMainThread)
            {
                // Already on main thread, process immediately
                _logger?.LogDebug($"AsyncUnityMainThreadFilter<{typeof(TMessage).Name}>: Processing async message {message.Id} on main thread");
                await next(message, cancellationToken);
            }
            else
            {
                // Not on main thread - handle based on configuration
                await HandleOffMainThreadAsync(message, next, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Handles async message processing when called from a background thread.
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <param name="next">The next async handler in the filter chain</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async UniTask HandleOffMainThreadAsync(TMessage message, Func<TMessage, CancellationToken, UniTask> next, CancellationToken cancellationToken)
    {
        var currentThreadId = Thread.CurrentThread.ManagedThreadId;
        var threadName = Thread.CurrentThread.Name ?? "Unknown";
        
        _logger?.LogWarning($"AsyncUnityMainThreadFilter<{typeof(TMessage).Name}>: Async message {message.Id} received on background thread " +
                          $"(ID: {currentThreadId}, Name: {threadName})");

        if (_strictMode)
        {
            var errorMessage = $"AsyncUnityMainThreadFilter<{typeof(TMessage).Name}>: Strict mode violation - " +
                             $"async message {message.Id} must be processed on Unity main thread";
            
            _logger?.LogException(errorMessage, new InvalidOperationException(errorMessage));
            throw new InvalidOperationException(errorMessage);
        }

        if (_enableAutoDispatch)
        {
            _logger?.LogInfo($"AsyncUnityMainThreadFilter<{typeof(TMessage).Name}>: Auto-dispatching async message {message.Id} to main thread");
            
            // Use the async dispatcher method for proper async handling
            await _dispatcher.EnqueueAsync(async () =>
            {
                _logger?.LogDebug($"AsyncUnityMainThreadFilter<{typeof(TMessage).Name}>: Processing dispatched async message {message.Id} on main thread");
                await next(message, cancellationToken);
            });
        }
        else
        {
            _logger?.LogWarning($"AsyncUnityMainThreadFilter<{typeof(TMessage).Name}>: Dropping async message {message.Id} - " +
                              "auto-dispatch disabled and not on main thread");
            // Message is dropped if auto-dispatch is disabled
        }
    }
}