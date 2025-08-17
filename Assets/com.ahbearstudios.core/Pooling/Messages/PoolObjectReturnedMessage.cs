using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Pooling;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when an object is returned to a pool.
    /// Follows the IMessage pattern from CLAUDE.md guidelines.
    /// Used for monitoring and diagnostics of pool usage.
    /// </summary>
    public record struct PoolObjectReturnedMessage : IMessage
    {
        /// <summary>
        /// Gets the unique identifier for this message instance.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets the message type code for efficient routing and filtering.
        /// </summary>
        public ushort TypeCode => MessageTypeCodes.PoolObjectReturned;

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
        /// Gets the timestamp when the object was returned (UTC ticks).
        /// </summary>
        public long TimestampTicks { get; init; }

        /// <summary>
        /// Gets the current size of the pool after the return.
        /// </summary>
        public int PoolSizeAfter { get; init; }

        /// <summary>
        /// Gets the number of active objects after the return.
        /// </summary>
        public int ActiveObjectsAfter { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking related operations.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets the source that triggered the object return.
        /// </summary>
        public FixedString64Bytes Source { get; init; }

        /// <summary>
        /// Gets whether the object was valid when returned.
        /// </summary>
        public bool WasValidOnReturn { get; init; }

        /// <summary>
        /// Gets the DateTime representation of the timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new PoolObjectReturnedMessage.
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="objectTypeName">Type name of the pooled object</param>
        /// <param name="poolId">Pool unique identifier</param>
        /// <param name="objectId">Object unique identifier</param>
        /// <param name="poolSizeAfter">Pool size after return</param>
        /// <param name="activeObjectsAfter">Active objects count after return</param>
        /// <param name="wasValidOnReturn">Whether the object was valid when returned</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="source">Source that triggered the return</param>
        /// <returns>New message instance</returns>
        public static PoolObjectReturnedMessage Create(
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
            return new PoolObjectReturnedMessage
            {
                Id = Guid.NewGuid(),
                PoolName = poolName,
                ObjectTypeName = objectTypeName,
                PoolId = poolId,
                ObjectId = objectId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                PoolSizeAfter = poolSizeAfter,
                ActiveObjectsAfter = activeObjectsAfter,
                WasValidOnReturn = wasValidOnReturn,
                CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
                Source = source.IsEmpty ? new FixedString64Bytes("PoolingService") : source
            };
        }
    }
}