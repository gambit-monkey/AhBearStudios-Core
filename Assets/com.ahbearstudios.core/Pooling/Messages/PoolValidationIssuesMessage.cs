using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when pool validation detects issues.
    /// Follows the IMessage pattern from CLAUDE.md guidelines.
    /// Used for monitoring pool health and triggering maintenance operations.
    /// </summary>
    public readonly record struct PoolValidationIssuesMessage : IMessage
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

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the timestamp when validation issues were detected.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new PoolValidationIssuesMessage with proper validation and defaults.
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
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolValidationIssuesMessage instance</returns>
        public static PoolValidationIssuesMessage CreateFromFixedStrings(
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

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "PoolingService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("PoolValidationIssuesMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("PoolValidation", poolName.ToString())
                : correlationId;
            
            return new PoolValidationIssuesMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.PoolValidationIssuesMessage,
                Source = source.IsEmpty ? "PoolingService" : source,
                Priority = messagePriority,
                CorrelationId = finalCorrelationId,
                
                PoolName = poolName,
                ObjectTypeName = objectTypeName,
                PoolId = poolId,
                IssueCount = issueCount,
                ObjectsValidated = objectsValidated,
                InvalidObjects = invalidObjects,
                CorruptedObjects = corruptedObjects,
                ErrorPercentage = errorPercentage,
                Severity = severity,
                PrimaryIssueDescription = primaryIssueDescription.IsEmpty 
                    ? new FixedString512Bytes($"Pool validation found {issueCount} issues")
                    : primaryIssueDescription,
                AutoCleanupPerformed = autoCleanupPerformed
            };
        }

        /// <summary>
        /// Creates a new PoolValidationIssuesMessage with the specified details.
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
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolValidationIssuesMessage instance</returns>
        public static PoolValidationIssuesMessage Create(
            string poolName,
            string objectTypeName,
            Guid poolId,
            int issueCount,
            int objectsValidated,
            int invalidObjects,
            int corruptedObjects,
            ValidationSeverity severity,
            string primaryIssueDescription = null,
            bool autoCleanupPerformed = false,
            Guid correlationId = default,
            string source = null)
        {
            return CreateFromFixedStrings(
                new FixedString64Bytes(poolName?.Length <= 64 ? poolName : poolName?[..64] ?? "Unknown"),
                new FixedString64Bytes(objectTypeName?.Length <= 64 ? objectTypeName : objectTypeName?[..64] ?? "Unknown"),
                poolId,
                issueCount,
                objectsValidated,
                invalidObjects,
                corruptedObjects,
                severity,
                new FixedString512Bytes(primaryIssueDescription?.Length <= 512 ? primaryIssueDescription : primaryIssueDescription?[..512] ?? ""),
                autoCleanupPerformed,
                correlationId,
                new FixedString64Bytes(source ?? "PoolingService"));
        }

        #endregion
    }
}