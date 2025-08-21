using System;
using System.Threading;
using MessagePipe;
using Unity.Profiling;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.Messaging.Filters;

/// <summary>
/// MessagePipe filter that wraps a custom predicate function for filtering messages.
/// Allows integration of legacy filter functions with MessagePipe's filter system.
/// </summary>
/// <typeparam name="TMessage">The message type implementing IMessage</typeparam>
public sealed class CustomPredicateFilter<TMessage> : MessageHandlerFilter<TMessage>
    where TMessage : IMessage
{
    private readonly Func<TMessage, bool> _predicate;
    private readonly ILoggingService _logger;
    private readonly ProfilerMarker _filterMarker;
    
    private static readonly ProfilerMarker _staticFilterMarker = new("CustomPredicateFilter.Handle");

    /// <summary>
    /// Initializes a new CustomPredicateFilter with the specified predicate function.
    /// </summary>
    /// <param name="predicate">The predicate function to evaluate messages</param>
    /// <param name="logger">Optional logging service for debugging</param>
    public CustomPredicateFilter(Func<TMessage, bool> predicate, ILoggingService logger = null)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        _logger = logger;
        _filterMarker = new ProfilerMarker($"CustomPredicateFilter<{typeof(TMessage).Name}>.Handle");
    }

    /// <summary>
    /// Handles message filtering using the custom predicate function.
    /// Only messages that satisfy the predicate are passed to the next handler.
    /// </summary>
    /// <param name="message">The message to filter</param>
    /// <param name="next">The next handler in the filter chain</param>
    public override void Handle(TMessage message, Action<TMessage> next)
    {
        using (_staticFilterMarker.Auto())
        using (_filterMarker.Auto())
        {
            if (message == null)
            {
                _logger?.LogWarning($"CustomPredicateFilter<{typeof(TMessage).Name}>: Received null message");
                return;
            }

            try
            {
                // Evaluate the custom predicate
                if (_predicate(message))
                {
                    _logger?.LogDebug($"CustomPredicateFilter<{typeof(TMessage).Name}>: Predicate passed for message {message.Id}");
                    next(message);
                }
                else
                {
                    _logger?.LogDebug($"CustomPredicateFilter<{typeof(TMessage).Name}>: Predicate failed for message {message.Id}");
                    // Message is filtered out - do not call next()
                }
            }
            catch (Exception ex)
            {
                _logger?.LogException($"CustomPredicateFilter<{typeof(TMessage).Name}>: Exception in predicate evaluation for message {message.Id}", ex);
                // On predicate exception, do not process the message
                return;
            }
        }
    }
}

/// <summary>
/// Async version of CustomPredicateFilter for async message handlers.
/// </summary>
/// <typeparam name="TMessage">The message type implementing IMessage</typeparam>
public sealed class AsyncCustomPredicateFilter<TMessage> : AsyncMessageHandlerFilter<TMessage>
    where TMessage : IMessage
{
    private readonly Func<TMessage, bool> _predicate;
    private readonly ILoggingService _logger;
    private readonly ProfilerMarker _filterMarker;
    
    private static readonly ProfilerMarker _staticFilterMarker = new("AsyncCustomPredicateFilter.Handle");

    /// <summary>
    /// Initializes a new AsyncCustomPredicateFilter with the specified predicate function.
    /// </summary>
    /// <param name="predicate">The predicate function to evaluate messages</param>
    /// <param name="logger">Optional logging service for debugging</param>
    public AsyncCustomPredicateFilter(Func<TMessage, bool> predicate, ILoggingService logger = null)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        _logger = logger;
        _filterMarker = new ProfilerMarker($"AsyncCustomPredicateFilter<{typeof(TMessage).Name}>.Handle");
    }

    /// <summary>
    /// Handles message filtering using the custom predicate function asynchronously.
    /// Only messages that satisfy the predicate are passed to the next handler.
    /// </summary>
    /// <param name="message">The message to filter</param>
    /// <param name="next">The next async handler in the filter chain</param>
    public override async ValueTask HandleAsync(TMessage message, Func<TMessage, ValueTask> next, CancellationToken cancellationToken)
    {
        using (_staticFilterMarker.Auto())
        using (_filterMarker.Auto())
        {
            if (message == null)
            {
                _logger?.LogWarning($"AsyncCustomPredicateFilter<{typeof(TMessage).Name}>: Received null message");
                return;
            }

            try
            {
                // Evaluate the custom predicate
                if (_predicate(message))
                {
                    _logger?.LogDebug($"AsyncCustomPredicateFilter<{typeof(TMessage).Name}>: Predicate passed for message {message.Id}");
                    await next(message);
                }
                else
                {
                    _logger?.LogDebug($"AsyncCustomPredicateFilter<{typeof(TMessage).Name}>: Predicate failed for message {message.Id}");
                    // Message is filtered out - do not call next()
                }
            }
            catch (Exception ex)
            {
                _logger?.LogException($"AsyncCustomPredicateFilter<{typeof(TMessage).Name}>: Exception in predicate evaluation for message {message.Id}", ex);
                // On predicate exception, do not process the message
                return;
            }
        }
    }
}