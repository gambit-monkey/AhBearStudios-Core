using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when a buffer exhaustion occurs.
    /// </summary>
    public readonly record struct PoolBufferExhaustionMessage : IMessage
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
        /// Gets the name of the strategy where exhaustion occurred.
        /// </summary>
        public FixedString64Bytes StrategyName { get; init; }

        /// <summary>
        /// Gets the total number of exhaustion events.
        /// </summary>
        public int ExhaustionCount { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the timestamp when the exhaustion occurred.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new PoolBufferExhaustionMessage with proper validation and defaults.
        /// </summary>
        /// <param name="strategyName">Strategy where exhaustion occurred</param>
        /// <param name="exhaustionCount">Total exhaustion event count</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolBufferExhaustionMessage instance</returns>
        public static PoolBufferExhaustionMessage CreateFromFixedStrings(
            FixedString64Bytes strategyName,
            int exhaustionCount,
            Guid correlationId = default,
            FixedString64Bytes source = default)
        {
            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "PoolingService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("PoolBufferExhaustionMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("PoolBufferExhaustion", strategyName.ToString())
                : correlationId;
            
            return new PoolBufferExhaustionMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.PoolBufferExhaustionMessage,
                Source = source.IsEmpty ? "PoolingService" : source,
                Priority = MessagePriority.High,
                CorrelationId = finalCorrelationId,
                
                StrategyName = strategyName,
                ExhaustionCount = exhaustionCount
            };
        }

        /// <summary>
        /// Creates a new PoolBufferExhaustionMessage with the specified details.
        /// </summary>
        /// <param name="strategyName">Strategy where exhaustion occurred</param>
        /// <param name="exhaustionCount">Total exhaustion event count</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolBufferExhaustionMessage instance</returns>
        public static PoolBufferExhaustionMessage Create(
            string strategyName,
            int exhaustionCount,
            Guid correlationId = default,
            string source = null)
        {
            return CreateFromFixedStrings(
                new FixedString64Bytes(strategyName?.Length <= 64 ? strategyName : strategyName?[..64] ?? "Unknown"),
                exhaustionCount,
                correlationId,
                new FixedString64Bytes(source ?? "PoolingService"));
        }

        #endregion
    }
}