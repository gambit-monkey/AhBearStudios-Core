using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when a pool expansion occurs.
    /// </summary>
    public readonly record struct PoolExpansionMessage : IMessage
    {
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

        // Pooling-specific properties
        /// <summary>
        /// Gets the name of the strategy that triggered the expansion.
        /// </summary>
        public string StrategyName { get; init; }

        /// <summary>
        /// Gets the previous pool size.
        /// </summary>
        public int OldSize { get; init; }

        /// <summary>
        /// Gets the new pool size after expansion.
        /// </summary>
        public int NewSize { get; init; }

        /// <summary>
        /// Gets the reason for the expansion.
        /// </summary>
        public string Reason { get; init; }

        /// <summary>
        /// Gets the timestamp when the expansion occurred.
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        /// Creates a new PoolExpansionMessage with the specified details.
        /// </summary>
        /// <param name="strategyName">Strategy that triggered the expansion</param>
        /// <param name="oldSize">Previous pool size</param>
        /// <param name="newSize">New pool size after expansion</param>
        /// <param name="reason">Reason for the expansion</param>
        /// <param name="source">Source component</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New PoolExpansionMessage instance</returns>
        public static PoolExpansionMessage Create(
            string strategyName,
            int oldSize,
            int newSize,
            string reason,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new PoolExpansionMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.PoolExpansion,
                Source = source.IsEmpty ? "PoolingStrategy" : source,
                Priority = MessagePriority.Normal,
                CorrelationId = correlationId,
                StrategyName = strategyName,
                OldSize = oldSize,
                NewSize = newSize,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}