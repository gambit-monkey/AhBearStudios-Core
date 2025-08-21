using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when a pool contraction occurs.
    /// </summary>
    public readonly record struct PoolContractionMessage : IMessage
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
        public ushort TypeCode { get; init; } = MessageTypeCodes.PoolContractionMessage;

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
        /// Gets the name of the strategy that triggered the contraction.
        /// </summary>
        public FixedString64Bytes StrategyName { get; init; }

        /// <summary>
        /// Gets the previous pool size.
        /// </summary>
        public int OldSize { get; init; }

        /// <summary>
        /// Gets the new pool size after contraction.
        /// </summary>
        public int NewSize { get; init; }

        /// <summary>
        /// Gets the reason for the contraction.
        /// </summary>
        public FixedString512Bytes Reason { get; init; }

        /// <summary>
        /// Initializes a new instance of the PoolContractionMessage struct.
        /// </summary>
        public PoolContractionMessage()
        {
            Id = default;
            TimestampTicks = default;
            Source = default;
            Priority = default;
            CorrelationId = default;
            StrategyName = default;
            OldSize = default;
            NewSize = default;
            Reason = default;
        }

        /// <summary>
        /// Gets the timestamp when the contraction occurred.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new PoolContractionMessage with the specified details.
        /// </summary>
        /// <param name="strategyName">Strategy that triggered the contraction</param>
        /// <param name="oldSize">Previous pool size</param>
        /// <param name="newSize">New pool size after contraction</param>
        /// <param name="reason">Reason for the contraction</param>
        /// <param name="source">Source component</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New PoolContractionMessage instance</returns>
        public static PoolContractionMessage Create(
            string strategyName,
            int oldSize,
            int newSize,
            string reason,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new PoolContractionMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.PoolContractionMessage,
                Source = source.IsEmpty ? "PoolingService" : source,
                Priority = MessagePriority.Normal,
                CorrelationId = correlationId,
                StrategyName = strategyName?.Length <= 64 ? strategyName : strategyName?[..64] ?? "Unknown",
                OldSize = oldSize,
                NewSize = newSize,
                Reason = reason?.Length <= 512 ? reason : reason?[..512] ?? "Unknown"
            };
        }
    }
}