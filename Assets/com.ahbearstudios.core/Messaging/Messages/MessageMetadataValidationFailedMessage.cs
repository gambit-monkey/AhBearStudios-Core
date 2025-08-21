using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when message metadata validation fails.
/// Used for error tracking and debugging metadata issues.
/// </summary>
public record struct MessageMetadataValidationFailedMessage : IMessage
{
    /// <inheritdoc/>
    public Guid Id { get; init; }

    /// <inheritdoc/>
    public long TimestampTicks { get; init; }

    /// <inheritdoc/>
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessageBusMetadataValidationFailedMessage;

    /// <inheritdoc/>
    public FixedString64Bytes Source { get; init; }

    /// <inheritdoc/>
    public MessagePriority Priority { get; init; }

    /// <inheritdoc/>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Gets the ID of the metadata that failed validation.
    /// </summary>
    public Guid MetadataId { get; init; }

    /// <summary>
    /// Gets the type of validation that failed.
    /// </summary>
    public FixedString64Bytes ValidationType { get; init; }

    /// <summary>
    /// Gets the reason for the validation failure.
    /// </summary>
    public FixedString512Bytes FailureReason { get; init; }

    /// <summary>
    /// Gets the field or property that failed validation.
    /// </summary>
    public FixedString64Bytes FailedField { get; init; }

    /// <summary>
    /// Gets the invalid value that caused the failure.
    /// </summary>
    public FixedString128Bytes InvalidValue { get; init; }

    /// <summary>
    /// Gets whether this is a critical validation failure.
    /// </summary>
    public bool IsCritical { get; init; }

    /// <summary>
    /// Gets whether the message can be retried after fixing the issue.
    /// </summary>
    public bool CanRetry { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessageMetadataValidationFailedMessage struct.
    /// </summary>
    public MessageMetadataValidationFailedMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        MetadataId = default;
        ValidationType = default;
        FailureReason = default;
        FailedField = default;
        InvalidValue = default;
        IsCritical = default;
        CanRetry = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new MessageMetadataValidationFailedMessage with default values.
    /// </summary>
    public static MessageMetadataValidationFailedMessage Create(
        Guid metadataId,
        string validationType,
        string failureReason,
        string failedField = null,
        string invalidValue = null,
        bool isCritical = false,
        bool canRetry = true,
        FixedString64Bytes source = default,
        Guid? correlationId = null)
    {
        return new MessageMetadataValidationFailedMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusMetadataValidationFailedMessage,
            Source = source,
            Priority = isCritical ? MessagePriority.High : MessagePriority.Normal,
            CorrelationId = correlationId ?? Guid.NewGuid(),
            MetadataId = metadataId,
            ValidationType = new FixedString64Bytes(validationType ?? "Unknown"),
            FailureReason = new FixedString512Bytes(failureReason ?? "Unknown validation failure"),
            FailedField = new FixedString64Bytes(failedField ?? string.Empty),
            InvalidValue = new FixedString128Bytes(invalidValue ?? string.Empty),
            IsCritical = isCritical,
            CanRetry = canRetry
        };
    }
}