using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when a buffer exhaustion occurs.
    /// </summary>
    public readonly record struct PoolBufferExhaustionMessage : IMessage
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
        public ushort TypeCode { get; init; } = MessageTypeCodes.PoolBufferExhaustionMessage;

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

        // Buffer exhaustion-specific properties
        /// <summary>
        /// Gets the name of the strategy where exhaustion occurred.
        /// </summary>
        public FixedString64Bytes StrategyName { get; init; }

        /// <summary>
        /// Gets the total number of exhaustion events.
        /// </summary>
        public int ExhaustionCount { get; init; }

        /// <summary>
        /// Initializes a new instance of the PoolBufferExhaustionMessage struct.
        /// </summary>
        public PoolBufferExhaustionMessage()
        {
            Id = default;
            TimestampTicks = default;
            Source = default;
            Priority = default;
            CorrelationId = default;
            StrategyName = default;
            ExhaustionCount = default;
        }

        /// <summary>
        /// Gets the timestamp when the exhaustion occurred.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new PoolBufferExhaustionMessage with the specified details.
        /// </summary>
        /// <param name="strategyName">Strategy where exhaustion occurred</param>
        /// <param name="exhaustionCount">Total exhaustion event count</param>
        /// <param name="source">Source component</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New PoolBufferExhaustionMessage instance</returns>
        public static PoolBufferExhaustionMessage Create(
            string strategyName,
            int exhaustionCount,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new PoolBufferExhaustionMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.PoolBufferExhaustionMessage,
                Source = source.IsEmpty ? "PoolingService" : source,
                Priority = MessagePriority.High, // Buffer exhaustion is high priority
                CorrelationId = correlationId,
                StrategyName = strategyName?.Length <= 64 ? strategyName : strategyName?[..64] ?? "Unknown",
                ExhaustionCount = exhaustionCount
            };
        }
    }
}