using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when an object is returned to a pool.
    /// Follows the IMessage pattern from CLAUDE.md guidelines.
    /// Used for monitoring and diagnostics of pool usage.
    /// </summary>
    public readonly record struct PoolObjectReturnedMessage : IMessage
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
        /// Gets the name of the pool to which the object was returned.
        /// </summary>
        public FixedString64Bytes PoolName { get; init; }

        /// <summary>
        /// Gets the type name of the pooled object.
        /// </summary>
        public FixedString64Bytes ObjectTypeName { get; init; }

        /// <summary>
        /// Gets the unique identifier of the pool.
        /// </summary>
        public Guid PoolId { get; init; }

        /// <summary>
        /// Gets the unique identifier of the returned object.
        /// </summary>
        public Guid ObjectId { get; init; }

        /// <summary>
        /// Gets the current size of the pool after the return.
        /// </summary>
        public int PoolSizeAfter { get; init; }

        /// <summary>
        /// Gets the number of active objects after the return.
        /// </summary>
        public int ActiveObjectsAfter { get; init; }

        /// <summary>
        /// Gets whether the object was valid when returned.
        /// </summary>
        public bool WasValidOnReturn { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the timestamp when the object was returned.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new PoolObjectReturnedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="objectTypeName">Type name of the pooled object</param>
        /// <param name="poolId">Pool unique identifier</param>
        /// <param name="objectId">Object unique identifier</param>
        /// <param name="poolSizeAfter">Pool size after return</param>
        /// <param name="activeObjectsAfter">Active objects count after return</param>
        /// <param name="wasValidOnReturn">Whether the object was valid when returned</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolObjectReturnedMessage instance</returns>
        public static PoolObjectReturnedMessage CreateFromFixedStrings(
            FixedString64Bytes poolName,
            FixedString64Bytes objectTypeName,
            Guid poolId,
            Guid objectId,
            int poolSizeAfter,
            int activeObjectsAfter,
            bool wasValidOnReturn,
            Guid correlationId = default,
            FixedString64Bytes source = default)
        {
            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "PoolingService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("PoolObjectReturnedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("PoolObjectReturnOperation", poolName.ToString())
                : correlationId;
            
            return new PoolObjectReturnedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.PoolObjectReturnedMessage,
                Source = source.IsEmpty ? "PoolingService" : source,
                Priority = MessagePriority.Low,
                CorrelationId = finalCorrelationId,
                
                PoolName = poolName,
                ObjectTypeName = objectTypeName,
                PoolId = poolId,
                ObjectId = objectId,
                PoolSizeAfter = poolSizeAfter,
                ActiveObjectsAfter = activeObjectsAfter,
                WasValidOnReturn = wasValidOnReturn
            };
        }

        /// <summary>
        /// Creates a new PoolObjectReturnedMessage with the specified details.
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="objectTypeName">Type name of the pooled object</param>
        /// <param name="poolId">Pool unique identifier</param>
        /// <param name="objectId">Object unique identifier</param>
        /// <param name="poolSizeAfter">Pool size after return</param>
        /// <param name="activeObjectsAfter">Active objects count after return</param>
        /// <param name="wasValidOnReturn">Whether the object was valid when returned</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolObjectReturnedMessage instance</returns>
        public static PoolObjectReturnedMessage Create(
            string poolName,
            string objectTypeName,
            Guid poolId,
            Guid objectId,
            int poolSizeAfter,
            int activeObjectsAfter,
            bool wasValidOnReturn,
            Guid correlationId = default,
            string source = null)
        {
            return CreateFromFixedStrings(
                new FixedString64Bytes(poolName?.Length <= 64 ? poolName : poolName?[..64] ?? "Unknown"),
                new FixedString64Bytes(objectTypeName?.Length <= 64 ? objectTypeName : objectTypeName?[..64] ?? "Unknown"),
                poolId,
                objectId,
                poolSizeAfter,
                activeObjectsAfter,
                wasValidOnReturn,
                correlationId,
                new FixedString64Bytes(source ?? "PoolingService"));
        }

        #endregion
    }
}