using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when a pool operation starts.
    /// Provides visibility into pool operation lifecycle for monitoring and debugging.
    /// </summary>
    public readonly record struct PoolOperationStartedMessage : IMessage
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
        /// Gets the name of the pool where the operation started.
        /// </summary>
        public FixedString64Bytes PoolName { get; init; }

        /// <summary>
        /// Gets the name of the strategy managing the pool.
        /// </summary>
        public FixedString64Bytes StrategyName { get; init; }

        /// <summary>
        /// Gets the type of operation that started (Get, Return, Maintenance, etc.).
        /// </summary>
        public FixedString64Bytes OperationType { get; init; }

        /// <summary>
        /// Gets the current pool statistics at operation start.
        /// </summary>
        public int PoolSizeAtStart { get; init; }

        /// <summary>
        /// Gets the number of active objects at operation start.
        /// </summary>
        public int ActiveObjectsAtStart { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the timestamp when the operation started.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new PoolOperationStartedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="strategyName">Name of the strategy</param>
        /// <param name="operationType">Type of operation</param>
        /// <param name="poolSizeAtStart">Pool size at start</param>
        /// <param name="activeObjectsAtStart">Active objects at start</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolOperationStartedMessage instance</returns>
        public static PoolOperationStartedMessage CreateFromFixedStrings(
            FixedString64Bytes poolName,
            FixedString64Bytes strategyName,
            FixedString64Bytes operationType,
            int poolSizeAtStart,
            int activeObjectsAtStart,
            Guid correlationId = default,
            FixedString64Bytes source = default)
        {
            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "PoolStrategy" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("PoolOperationStartedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("PoolOperation", poolName.ToString())
                : correlationId;
            
            return new PoolOperationStartedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.PoolOperationStartedMessage,
                Source = source.IsEmpty ? "PoolStrategy" : source,
                Priority = MessagePriority.Low,
                CorrelationId = finalCorrelationId,
                
                PoolName = poolName,
                StrategyName = strategyName,
                OperationType = operationType,
                PoolSizeAtStart = poolSizeAtStart,
                ActiveObjectsAtStart = activeObjectsAtStart
            };
        }

        /// <summary>
        /// Creates a new PoolOperationStartedMessage with the specified details.
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="strategyName">Name of the strategy</param>
        /// <param name="operationType">Type of operation</param>
        /// <param name="poolSizeAtStart">Pool size at start</param>
        /// <param name="activeObjectsAtStart">Active objects at start</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolOperationStartedMessage instance</returns>
        public static PoolOperationStartedMessage Create(
            string poolName,
            string strategyName,
            string operationType,
            int poolSizeAtStart,
            int activeObjectsAtStart,
            Guid correlationId = default,
            string source = null)
        {
            return CreateFromFixedStrings(
                new FixedString64Bytes(poolName?.Length <= 64 ? poolName : poolName?[..64] ?? "Unknown"),
                new FixedString64Bytes(strategyName?.Length <= 64 ? strategyName : strategyName?[..64] ?? "Unknown"),
                new FixedString64Bytes(operationType?.Length <= 64 ? operationType : operationType?[..64] ?? "Unknown"),
                poolSizeAtStart,
                activeObjectsAtStart,
                correlationId,
                new FixedString64Bytes(source ?? "PoolStrategy"));
        }

        #endregion
    }
}