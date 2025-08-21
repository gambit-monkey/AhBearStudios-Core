using System;
using MessagePipe;
using Unity.Profiling;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.Messaging.Filters;

/// <summary>
/// MessagePipe filter that filters messages based on priority levels.
/// Essential for maintaining 60+ FPS by ensuring only high-priority messages
/// are processed during frame budget constraints.
/// </summary>
/// <typeparam name="TMessage">The message type implementing IMessage</typeparam>
public sealed class MessagePriorityFilter<TMessage> : MessageHandlerFilter<TMessage>
    where TMessage : IMessage
{
    private readonly MessagePriority _minPriority;
    private readonly ILoggingService _logger;
    private readonly ProfilerMarker _filterMarker;
    
    private static readonly ProfilerMarker _staticFilterMarker = new("MessagePriorityFilter.Handle");

    /// <summary>
    /// Initializes a new MessagePriorityFilter with the specified minimum priority threshold.
    /// </summary>
    /// <param name="minPriority">Minimum priority level required for message processing</param>
    /// <param name="logger">Optional logging service for debugging</param>
    public MessagePriorityFilter(MessagePriority minPriority, ILoggingService logger = null)
    {
        _minPriority = minPriority;
        _logger = logger;
        _filterMarker = new ProfilerMarker($"MessagePriorityFilter<{typeof(TMessage).Name}>.Handle");
    }

    /// <summary>
    /// Handles message filtering based on priority level.
    /// Only messages meeting or exceeding the minimum priority are processed.
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
                _logger?.LogWarning($"MessagePriorityFilter<{typeof(TMessage).Name}>: Received null message");
                return;
            }

            // Zero-allocation priority comparison for performance
            if (message.Priority >= _minPriority)
            {
                _logger?.LogDebug($"MessagePriorityFilter<{typeof(TMessage).Name}>: Allowing message {message.Id} with priority {message.Priority} (>= {_minPriority})");
                next(message);
            }
            else
            {
                _logger?.LogDebug($"MessagePriorityFilter<{typeof(TMessage).Name}>: Filtering out message {message.Id} with priority {message.Priority} (< {_minPriority})");
                // Message is filtered out - do not call next()
            }
        }
    }
}