using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when a pool operation completes successfully.
    /// Provides performance metrics and completion status for monitoring.
    /// </summary>
    public readonly record struct PoolOperationCompletedMessage : IMessage
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
        /// Gets the name of the pool where the operation completed.
        /// </summary>
        public FixedString64Bytes PoolName { get; init; }

        /// <summary>
        /// Gets the name of the strategy managing the pool.
        /// </summary>
        public FixedString64Bytes StrategyName { get; init; }

        /// <summary>
        /// Gets the type of operation that completed.
        /// </summary>
        public FixedString64Bytes OperationType { get; init; }

        /// <summary>
        /// Gets the duration of the operation in milliseconds.
        /// </summary>
        public double DurationMs { get; init; }

        /// <summary>
        /// Gets the pool size after operation completion.
        /// </summary>
        public int PoolSizeAfter { get; init; }

        /// <summary>
        /// Gets the number of active objects after operation completion.
        /// </summary>
        public int ActiveObjectsAfter { get; init; }

        /// <summary>
        /// Gets whether the operation was successful.
        /// </summary>
        public bool IsSuccessful { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the timestamp when the operation completed.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the operation duration as a TimeSpan.
        /// </summary>
        public TimeSpan Duration => TimeSpan.FromMilliseconds(DurationMs);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new PoolOperationCompletedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="strategyName">Name of the strategy</param>
        /// <param name="operationType">Type of operation</param>
        /// <param name="durationMs">Operation duration in milliseconds</param>
        /// <param name="poolSizeAfter">Pool size after completion</param>
        /// <param name="activeObjectsAfter">Active objects after completion</param>
        /// <param name="isSuccessful">Whether operation was successful</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolOperationCompletedMessage instance</returns>
        public static PoolOperationCompletedMessage CreateFromFixedStrings(
            FixedString64Bytes poolName,
            FixedString64Bytes strategyName,
            FixedString64Bytes operationType,
            double durationMs,
            int poolSizeAfter,
            int activeObjectsAfter,
            bool isSuccessful = true,
            Guid correlationId = default,
            FixedString64Bytes source = default)
        {
            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "PoolStrategy" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("PoolOperationCompletedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("PoolOperation", poolName.ToString())
                : correlationId;
            
            return new PoolOperationCompletedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.PoolOperationCompletedMessage,
                Source = source.IsEmpty ? "PoolStrategy" : source,
                Priority = MessagePriority.Low,
                CorrelationId = finalCorrelationId,
                
                PoolName = poolName,
                StrategyName = strategyName,
                OperationType = operationType,
                DurationMs = durationMs,
                PoolSizeAfter = poolSizeAfter,
                ActiveObjectsAfter = activeObjectsAfter,
                IsSuccessful = isSuccessful
            };
        }

        /// <summary>
        /// Creates a new PoolOperationCompletedMessage with the specified details.
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="strategyName">Name of the strategy</param>
        /// <param name="operationType">Type of operation</param>
        /// <param name="duration">Operation duration</param>
        /// <param name="poolSizeAfter">Pool size after completion</param>
        /// <param name="activeObjectsAfter">Active objects after completion</param>
        /// <param name="isSuccessful">Whether operation was successful</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolOperationCompletedMessage instance</returns>
        public static PoolOperationCompletedMessage Create(
            string poolName,
            string strategyName,
            string operationType,
            TimeSpan duration,
            int poolSizeAfter,
            int activeObjectsAfter,
            bool isSuccessful = true,
            Guid correlationId = default,
            string source = null)
        {
            return CreateFromFixedStrings(
                new FixedString64Bytes(poolName?.Length <= 64 ? poolName : poolName?[..64] ?? "Unknown"),
                new FixedString64Bytes(strategyName?.Length <= 64 ? strategyName : strategyName?[..64] ?? "Unknown"),
                new FixedString64Bytes(operationType?.Length <= 64 ? operationType : operationType?[..64] ?? "Unknown"),
                duration.TotalMilliseconds,
                poolSizeAfter,
                activeObjectsAfter,
                isSuccessful,
                correlationId,
                new FixedString64Bytes(source ?? "PoolStrategy"));
        }

        #endregion
    }
}