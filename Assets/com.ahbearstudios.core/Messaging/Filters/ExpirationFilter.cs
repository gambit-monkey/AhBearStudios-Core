using System;
using MessagePipe;
using Unity.Profiling;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Services;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.Messaging.Filters;

/// <summary>
/// MessagePipe filter that prevents processing of expired messages.
/// Critical for Unity games to avoid processing stale game state,
/// player input events, animation triggers, and collision events.
/// </summary>
/// <typeparam name="TMessage">The message type implementing IMessage</typeparam>
public sealed class ExpirationFilter<TMessage> : MessageHandlerFilter<TMessage>
    where TMessage : IMessage
{
    private readonly IMessageMetadataService _metadataService;
    private readonly ILoggingService _logger;
    private readonly ProfilerMarker _filterMarker;
    private readonly bool _allowNullMetadata;
    
    private static readonly ProfilerMarker _staticFilterMarker = new("ExpirationFilter.Handle");

    /// <summary>
    /// Initializes a new ExpirationFilter with metadata service for expiration checking.
    /// </summary>
    /// <param name="metadataService">Service for metadata operations and expiration checking</param>
    /// <param name="logger">Optional logging service for debugging</param>
    /// <param name="allowNullMetadata">Whether to allow messages without metadata (default: true)</param>
    public ExpirationFilter(
        IMessageMetadataService metadataService, 
        ILoggingService logger = null,
        bool allowNullMetadata = true)
    {
        _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
        _logger = logger;
        _allowNullMetadata = allowNullMetadata;
        _filterMarker = new ProfilerMarker($"ExpirationFilter<{typeof(TMessage).Name}>.Handle");
    }

    /// <summary>
    /// Handles message filtering based on expiration status.
    /// Expired messages are filtered out to prevent processing stale game state.
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
                _logger?.LogWarning($"ExpirationFilter<{typeof(TMessage).Name}>: Received null message");
                return;
            }

            // Check if message has metadata for expiration checking
            if (message is IMessageWithMetadata messageWithMetadata)
            {
                var metadata = messageWithMetadata.Metadata;
                
                if (metadata == null)
                {
                    if (_allowNullMetadata)
                    {
                        _logger?.LogDebug($"ExpirationFilter<{typeof(TMessage).Name}>: Allowing message {message.Id} with null metadata");
                        next(message);
                    }
                    else
                    {
                        _logger?.LogWarning($"ExpirationFilter<{typeof(TMessage).Name}>: Filtering out message {message.Id} due to null metadata");
                    }
                    return;
                }

                // Check expiration using metadata service
                if (_metadataService.IsExpired(metadata))
                {
                    var expiresAt = _metadataService.GetExpiresAt(metadata);
                    _logger?.LogDebug($"ExpirationFilter<{typeof(TMessage).Name}>: Filtering out expired message {message.Id} (expired at {expiresAt})");
                    
                    // Message has expired - do not call next()
                    return;
                }

                _logger?.LogDebug($"ExpirationFilter<{typeof(TMessage).Name}>: Allowing non-expired message {message.Id}");
            }
            else
            {
                // Message doesn't implement IMessageWithMetadata
                if (_allowNullMetadata)
                {
                    _logger?.LogDebug($"ExpirationFilter<{typeof(TMessage).Name}>: Allowing message {message.Id} without metadata support");
                }
                else
                {
                    _logger?.LogWarning($"ExpirationFilter<{typeof(TMessage).Name}>: Filtering out message {message.Id} - no metadata support");
                    return;
                }
            }

            // Message is valid and not expired
            next(message);
        }
    }
}

/// <summary>
/// Interface for messages that carry metadata for expiration checking.
/// Implemented by messages that need TTL and expiration support.
/// </summary>
public interface IMessageWithMetadata
{
    /// <summary>
    /// Gets the metadata associated with this message.
    /// </summary>
    MessageMetadata Metadata { get; }
}