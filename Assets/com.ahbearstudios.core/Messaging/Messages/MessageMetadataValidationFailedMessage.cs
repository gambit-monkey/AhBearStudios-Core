using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when message metadata validation fails.
/// Used for error tracking and debugging metadata issues.
/// </summary>
public readonly record struct MessageMetadataValidationFailedMessage : IMessage
{
    #region IMessage Implementation

    /// <summary>
    /// Gets the unique identifier for this message instance.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the timestamp when this message was created, in UTC ticks.
    /// </summary>
    public long TimestampTicks { get; init; }

    /// <summary>
    /// Gets the message type code for efficient routing and filtering.
    /// </summary>
    public ushort TypeCode { get; init; }

    /// <summary>
    /// Gets the source system or component that created this message.
    /// </summary>
    public FixedString64Bytes Source { get; init; }

    /// <summary>
    /// Gets the priority level for message processing.
    /// </summary>
    public MessagePriority Priority { get; init; }

    /// <summary>
    /// Gets optional correlation ID for message tracing across systems.
    /// </summary>
    public Guid CorrelationId { get; init; }

    #endregion

    #region Message-Specific Properties

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

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessageMetadataValidationFailedMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="metadataId">The ID of the metadata that failed validation</param>
    /// <param name="validationType">The type of validation that failed</param>
    /// <param name="failureReason">The reason for the validation failure</param>
    /// <param name="failedField">The field or property that failed validation</param>
    /// <param name="invalidValue">The invalid value that caused the failure</param>
    /// <param name="isCritical">Whether this is a critical validation failure</param>
    /// <param name="canRetry">Whether the message can be retried after fixing the issue</param>
    /// <param name="source">Source system or component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageMetadataValidationFailedMessage instance</returns>
    public static MessageMetadataValidationFailedMessage CreateFromFixedStrings(
        Guid metadataId,
        FixedString64Bytes validationType,
        FixedString512Bytes failureReason,
        FixedString64Bytes failedField = default,
        FixedString128Bytes invalidValue = default,
        bool isCritical = false,
        bool canRetry = true,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageMetadata", null)
            : correlationId;

        return new MessageMetadataValidationFailedMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageMetadataValidationFailedMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusMetadataValidationFailedMessage,
            Source = source.IsEmpty ? "MessageMetadata" : source,
            Priority = isCritical ? MessagePriority.High : MessagePriority.Normal,
            CorrelationId = finalCorrelationId,
            MetadataId = metadataId,
            ValidationType = validationType,
            FailureReason = failureReason,
            FailedField = failedField,
            InvalidValue = invalidValue,
            IsCritical = isCritical,
            CanRetry = canRetry
        };
    }

    /// <summary>
    /// Creates a new instance of MessageMetadataValidationFailedMessage using string parameters.
    /// </summary>
    /// <param name="metadataId">The ID of the metadata that failed validation</param>
    /// <param name="validationType">The type of validation that failed</param>
    /// <param name="failureReason">The reason for the validation failure</param>
    /// <param name="failedField">The field or property that failed validation</param>
    /// <param name="invalidValue">The invalid value that caused the failure</param>
    /// <param name="isCritical">Whether this is a critical validation failure</param>
    /// <param name="canRetry">Whether the message can be retried after fixing the issue</param>
    /// <param name="source">Source system or component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageMetadataValidationFailedMessage instance</returns>
    public static MessageMetadataValidationFailedMessage Create(
        Guid metadataId,
        string validationType,
        string failureReason,
        string failedField = null,
        string invalidValue = null,
        bool isCritical = false,
        bool canRetry = true,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            metadataId,
            new FixedString64Bytes(validationType ?? "Unknown"),
            new FixedString512Bytes(failureReason ?? "Unknown validation failure"),
            new FixedString64Bytes(failedField ?? string.Empty),
            new FixedString128Bytes(invalidValue ?? string.Empty),
            isCritical,
            canRetry,
            source?.Length <= 64 ? source : source?[..64] ?? "MessageMetadata",
            correlationId);
    }

    #endregion
}