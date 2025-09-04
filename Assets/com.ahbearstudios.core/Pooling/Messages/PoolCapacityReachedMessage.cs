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
    /// Message published when a pool reaches its capacity limits.
    /// Follows the IMessage pattern from CLAUDE.md guidelines.
    /// Used for monitoring pool health and triggering capacity management.
    /// </summary>
    public readonly record struct PoolCapacityReachedMessage : IMessage
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
        /// Gets the name of the pool that reached capacity.
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
        /// Gets the current capacity of the pool.
        /// </summary>
        public int CurrentCapacity { get; init; }

        /// <summary>
        /// Gets the maximum allowed capacity of the pool.
        /// </summary>
        public int MaxCapacity { get; init; }

        /// <summary>
        /// Gets the number of objects currently active.
        /// </summary>
        public int ActiveObjects { get; init; }

        /// <summary>
        /// Gets the capacity utilization percentage (0.0 to 1.0).
        /// </summary>
        public float UtilizationPercentage { get; init; }

        /// <summary>
        /// Gets the severity level of the capacity issue.
        /// </summary>
        public CapacitySeverity Severity { get; init; }

        /// <summary>
        /// Gets additional context about the capacity situation.
        /// </summary>
        public FixedString128Bytes Context { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the timestamp when capacity was reached.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new PoolCapacityReachedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="objectTypeName">Type name of pooled objects</param>
        /// <param name="poolId">Pool unique identifier</param>
        /// <param name="currentCapacity">Current pool capacity</param>
        /// <param name="maxCapacity">Maximum pool capacity</param>
        /// <param name="activeObjects">Number of active objects</param>
        /// <param name="severity">Severity of the capacity issue</param>
        /// <param name="context">Additional context about the situation</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolCapacityReachedMessage instance</returns>
        public static PoolCapacityReachedMessage CreateFromFixedStrings(
            FixedString64Bytes poolName,
            FixedString64Bytes objectTypeName,
            Guid poolId,
            int currentCapacity,
            int maxCapacity,
            int activeObjects,
            CapacitySeverity severity,
            FixedString128Bytes context = default,
            Guid correlationId = default,
            FixedString64Bytes source = default)
        {
            var utilizationPercentage = maxCapacity > 0 ? (float)currentCapacity / maxCapacity : 0f;

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "PoolingService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("PoolCapacityReachedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("PoolCapacityOperation", poolName.ToString())
                : correlationId;
            
            return new PoolCapacityReachedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.PoolCapacityReachedMessage,
                Source = source.IsEmpty ? "PoolingService" : source,
                Priority = MessagePriority.High,
                CorrelationId = finalCorrelationId,
                
                PoolName = poolName,
                ObjectTypeName = objectTypeName,
                PoolId = poolId,
                CurrentCapacity = currentCapacity,
                MaxCapacity = maxCapacity,
                ActiveObjects = activeObjects,
                UtilizationPercentage = utilizationPercentage,
                Severity = severity,
                Context = context.IsEmpty ? new FixedString128Bytes("Pool capacity limit reached") : context
            };
        }

        /// <summary>
        /// Creates a new PoolCapacityReachedMessage with the specified details.
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="objectTypeName">Type name of pooled objects</param>
        /// <param name="poolId">Pool unique identifier</param>
        /// <param name="currentCapacity">Current pool capacity</param>
        /// <param name="maxCapacity">Maximum pool capacity</param>
        /// <param name="activeObjects">Number of active objects</param>
        /// <param name="severity">Severity of the capacity issue</param>
        /// <param name="context">Additional context about the situation</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolCapacityReachedMessage instance</returns>
        public static PoolCapacityReachedMessage Create(
            string poolName,
            string objectTypeName,
            Guid poolId,
            int currentCapacity,
            int maxCapacity,
            int activeObjects,
            CapacitySeverity severity,
            string context = null,
            Guid correlationId = default,
            string source = null)
        {
            return CreateFromFixedStrings(
                new FixedString64Bytes(poolName?.Length <= 64 ? poolName : poolName?[..64] ?? "Unknown"),
                new FixedString64Bytes(objectTypeName?.Length <= 64 ? objectTypeName : objectTypeName?[..64] ?? "Unknown"),
                poolId,
                currentCapacity,
                maxCapacity,
                activeObjects,
                severity,
                new FixedString128Bytes(context?.Length <= 128 ? context : context?[..128] ?? "Pool capacity limit reached"),
                correlationId,
                new FixedString64Bytes(source ?? "PoolingService"));
        }

        #endregion
    }
}