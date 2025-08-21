using System;
using Unity.Collections;
using Unity.Profiling;
using ZLinq;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Services;

/// <summary>
/// Service implementation for MessageMetadata business logic and operations.
/// Provides validation, querying, and utility methods for metadata.
/// </summary>
public sealed class MessageMetadataService : IMessageMetadataService
{
    private readonly ProfilerMarker _validateMarker = new("MessageMetadataService.IsValid");
    private readonly ProfilerMarker _estimateSizeMarker = new("MessageMetadataService.EstimateSize");

    /// <inheritdoc/>
    public bool IsValid(MessageMetadata metadata)
    {
        using (_validateMarker.Auto())
        {
            if (metadata == null)
                return false;

            try
            {
                // Basic validation
                if (metadata.MessageId == Guid.Empty)
                    return false;

                if (metadata.DeliveryAttempts < 0 || metadata.MaxDeliveryAttempts < 0)
                    return false;

                if (metadata.TimeToLive < TimeSpan.Zero || metadata.DeliveryDelay < TimeSpan.Zero)
                    return false;

                // Check expiration consistency
                if (metadata.TimeToLive > TimeSpan.Zero && metadata.ExpiresAtTicks == 0)
                    return false;

                if (metadata.ExpiresAtTicks > 0 && metadata.ExpiresAtTicks <= metadata.CreatedAtTicks)
                    return false;

                // Check delivery attempts consistency
                if (metadata.DeliveryAttempts > metadata.MaxDeliveryAttempts)
                    return false;

                // All validations passed
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsExpired(MessageMetadata metadata)
    {
        if (metadata == null)
            return false;

        return metadata.ExpiresAtTicks > 0 && DateTime.UtcNow.Ticks > metadata.ExpiresAtTicks;
    }

    /// <inheritdoc/>
    public bool IsReadyForDelivery(MessageMetadata metadata)
    {
        if (metadata == null)
            return false;

        var createdAt = new DateTime(metadata.CreatedAtTicks, DateTimeKind.Utc);
        return DateTime.UtcNow >= createdAt.Add(metadata.DeliveryDelay);
    }

    /// <inheritdoc/>
    public T GetCustomProperty<T>(MessageMetadata metadata, string key)
    {
        if (metadata == null || string.IsNullOrEmpty(key))
            return default(T);

        if (metadata.CustomProperties == null || !metadata.CustomProperties.TryGetValue(key, out var value))
            return default(T);

        try
        {
            return (T)value;
        }
        catch
        {
            return default(T);
        }
    }

    /// <inheritdoc/>
    public string GetCustomHeader(MessageMetadata metadata, string key)
    {
        if (metadata == null || string.IsNullOrEmpty(key))
            return null;

        if (metadata.CustomHeaders == null)
            return null;

        return metadata.CustomHeaders.TryGetValue(key, out var value) ? value : null;
    }


    /// <inheritdoc/>
    public bool HasCustomProperties(MessageMetadata metadata)
    {
        return metadata?.CustomProperties != null && metadata.CustomProperties.Count > 0;
    }

    /// <inheritdoc/>
    public bool HasCustomHeaders(MessageMetadata metadata)
    {
        return metadata?.CustomHeaders != null && metadata.CustomHeaders.Count > 0;
    }

    /// <inheritdoc/>
    public int EstimateSize(MessageMetadata metadata)
    {
        using (_estimateSizeMarker.Auto())
        {
            if (metadata == null)
                return 0;

            int size = 0;

            // GUIDs (16 bytes each)
            size += 16 * 3; // MessageId, CorrelationId, ConversationId

            // Basic types
            size += sizeof(ushort); // TypeCode
            size += 1; // MessagePriority (byte enum)
            size += sizeof(long) * 2; // Timestamps
            size += sizeof(int) * 3; // Delivery counters
            size += sizeof(bool) * 5; // Boolean flags
            size += 1; // MessageDeliveryMode (byte enum)
            size += 1; // MessageRoutingStrategy (byte enum)
            size += 1; // MessageSecurityLevel (byte enum)

            // TimeSpans (stored as ticks)
            size += sizeof(long) * 2; // TimeToLive, DeliveryDelay

            // Fixed strings (using their lengths)
            size += metadata.Source.Length;
            size += metadata.Destination.Length;
            size += metadata.Category.Length;
            size += metadata.ReplyTo.Length;
            size += metadata.DeadLetterQueue.Length;
            size += metadata.SecurityToken.Length;


            // Custom properties estimate
            if (metadata.CustomProperties != null)
                size += metadata.CustomProperties.Count * 64;

            // Custom headers estimate
            if (metadata.CustomHeaders != null)
                size += metadata.CustomHeaders.Count * 128;

            return size;
        }
    }

    /// <inheritdoc/>
    public string ToSummary(MessageMetadata metadata)
    {
        if (metadata == null)
            return "MessageMetadata[null]";

        return $"MessageMetadata[{metadata.MessageId:D}]: " +
               $"Priority={metadata.Priority}, " +
               $"Source={metadata.Source}, " +
               $"Destination={metadata.Destination}, " +
               $"CorrelationId={metadata.CorrelationId:D}, " +
               $"DeliveryMode={metadata.DeliveryMode}, " +
               $"Attempts={metadata.DeliveryAttempts}/{metadata.MaxDeliveryAttempts}, " +
               $"TTL={metadata.TimeToLive.TotalSeconds:F2}s, " +
               $"CustomProps={(metadata.CustomProperties?.Count ?? 0)}";
    }

    /// <inheritdoc/>
    public DateTime GetCreatedAt(MessageMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        return new DateTime(metadata.CreatedAtTicks, DateTimeKind.Utc);
    }

    /// <inheritdoc/>
    public DateTime? GetExpiresAt(MessageMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        return metadata.ExpiresAtTicks > 0 
            ? new DateTime(metadata.ExpiresAtTicks, DateTimeKind.Utc) 
            : null;
    }
}