using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when pool validation detects issues.
    /// Follows the IMessage pattern from CLAUDE.md guidelines.
    /// Used for monitoring pool health and triggering maintenance operations.
    /// </summary>
    public readonly record struct PoolValidationIssuesMessage : IMessage
    {
        /// <summary>
        /// Gets the unique identifier for this message instance.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets the message type code for efficient routing and filtering.
        /// </summary>
        public ushort TypeCode { get; init; } = MessageTypeCodes.PoolValidationIssuesMessage;

        /// <summary>
        /// Gets the name of the pool with validation issues.
        /// </summary>
        public FixedString64Bytes PoolName { get; init; }

        /// <summary>
        /// Gets the type name of the pooled objects.
        /// </summary>
        public FixedString64Bytes ObjectTypeName { get; init; }

        /// <summary>
        /// Gets the unique identifier of the pool.
        /// </summary>
        public Guid PoolId { get; init; }

        /// <summary>
        /// Gets the timestamp when validation issues were detected (UTC ticks).
        /// </summary>
        public long TimestampTicks { get; init; }

        /// <summary>
        /// Gets the number of validation issues found.
        /// </summary>
        public int IssueCount { get; init; }

        /// <summary>
        /// Gets the total number of objects validated.
        /// </summary>
        public int ObjectsValidated { get; init; }

        /// <summary>
        /// Gets the number of invalid objects found.
        /// </summary>
        public int InvalidObjects { get; init; }

        /// <summary>
        /// Gets the number of corrupted objects found.
        /// </summary>
        public int CorruptedObjects { get; init; }

        /// <summary>
        /// Gets the validation error percentage (0.0 to 1.0).
        /// </summary>
        public float ErrorPercentage { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking related operations.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets the source that triggered the validation.
        /// </summary>
        public FixedString64Bytes Source { get; init; }

        /// <summary>
        /// Gets the severity level of the validation issues.
        /// </summary>
        public ValidationSeverity Severity { get; init; }

        /// <summary>
        /// Gets a description of the primary validation issue.
        /// </summary>
        public FixedString512Bytes PrimaryIssueDescription { get; init; }

        /// <summary>
        /// Gets whether the pool was automatically cleaned up.
        /// </summary>
        public bool AutoCleanupPerformed { get; init; }

        /// <summary>
        /// Gets the priority level for message processing.
        /// </summary>
        public MessagePriority Priority { get; init; }

        /// <summary>
        /// Initializes a new instance of the PoolValidationIssuesMessage struct.
        /// </summary>
        public PoolValidationIssuesMessage()
        {
            Id = default;
            PoolName = default;
            ObjectTypeName = default;
            PoolId = default;
            TimestampTicks = default;
            IssueCount = default;
            ObjectsValidated = default;
            InvalidObjects = default;
            CorruptedObjects = default;
            ErrorPercentage = default;
            CorrelationId = default;
            Source = default;
            Severity = default;
            PrimaryIssueDescription = default;
            AutoCleanupPerformed = default;
            Priority = default;
        }

        /// <summary>
        /// Gets the DateTime representation of the timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new PoolValidationIssuesMessage.
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="objectTypeName">Type name of pooled objects</param>
        /// <param name="poolId">Pool unique identifier</param>
        /// <param name="issueCount">Number of validation issues</param>
        /// <param name="objectsValidated">Total objects validated</param>
        /// <param name="invalidObjects">Number of invalid objects</param>
        /// <param name="corruptedObjects">Number of corrupted objects</param>
        /// <param name="severity">Severity of validation issues</param>
        /// <param name="primaryIssueDescription">Description of the primary issue</param>
        /// <param name="autoCleanupPerformed">Whether cleanup was performed</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="source">Source that triggered validation</param>
        /// <returns>New message instance</returns>
        public static PoolValidationIssuesMessage Create(
            FixedString64Bytes poolName,
            FixedString64Bytes objectTypeName,
            Guid poolId,
            int issueCount,
            int objectsValidated,
            int invalidObjects,
            int corruptedObjects,
            ValidationSeverity severity,
            FixedString512Bytes primaryIssueDescription = default,
            bool autoCleanupPerformed = false,
            Guid correlationId = default,
            FixedString64Bytes source = default)
        {
            var errorPercentage = objectsValidated > 0 ? (float)issueCount / objectsValidated : 0f;

            // Set priority based on validation severity
            var messagePriority = severity switch
            {
                ValidationSeverity.Critical => MessagePriority.Critical,
                ValidationSeverity.Major => MessagePriority.High,
                ValidationSeverity.Moderate => MessagePriority.Normal,
                ValidationSeverity.Minor => MessagePriority.Low,
                _ => MessagePriority.Normal
            };

            return new PoolValidationIssuesMessage
            {
                Id = Guid.NewGuid(),
                TypeCode = MessageTypeCodes.PoolValidationIssuesMessage,
                PoolName = poolName,
                ObjectTypeName = objectTypeName,
                PoolId = poolId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                IssueCount = issueCount,
                ObjectsValidated = objectsValidated,
                InvalidObjects = invalidObjects,
                CorruptedObjects = corruptedObjects,
                ErrorPercentage = errorPercentage,
                Priority = messagePriority, // Priority based on validation severity
                Severity = severity,
                PrimaryIssueDescription = primaryIssueDescription.IsEmpty 
                    ? new FixedString512Bytes($"Pool validation found {issueCount} issues")
                    : primaryIssueDescription,
                AutoCleanupPerformed = autoCleanupPerformed,
                CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
                Source = source.IsEmpty ? new FixedString64Bytes("PoolingService") : source
            };
        }
    }

    /// <summary>
    /// Severity levels for pool validation issues.
    /// </summary>
    public enum ValidationSeverity : byte
    {
        /// <summary>
        /// Minor validation issues that don't affect functionality.
        /// </summary>
        Minor = 0,

        /// <summary>
        /// Moderate validation issues that may affect performance.
        /// </summary>
        Moderate = 1,

        /// <summary>
        /// Major validation issues that affect functionality.
        /// </summary>
        Major = 2,

        /// <summary>
        /// Critical validation issues requiring immediate action.
        /// </summary>
        Critical = 3
    }
}