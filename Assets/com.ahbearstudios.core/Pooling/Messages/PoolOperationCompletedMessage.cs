using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when a pool operation completes successfully.
    /// Provides performance metrics and completion status for monitoring.
    /// </summary>
    public readonly record struct PoolOperationCompletedMessage : IMessage
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
        public ushort TypeCode { get; init; } = MessageTypeCodes.PoolOperationCompletedMessage;

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

        // Pool operation-specific properties
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

        /// <summary>
        /// Initializes a new instance of the PoolOperationCompletedMessage struct.
        /// </summary>
        public PoolOperationCompletedMessage()
        {
            Id = default;
            TimestampTicks = default;
            Source = default;
            Priority = default;
            CorrelationId = default;
            PoolName = default;
            StrategyName = default;
            OperationType = default;
            DurationMs = default;
            PoolSizeAfter = default;
            ActiveObjectsAfter = default;
            IsSuccessful = default;
        }

        /// <summary>
        /// Gets the timestamp when the operation completed.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the operation duration as a TimeSpan.
        /// </summary>
        public TimeSpan Duration => TimeSpan.FromMilliseconds(DurationMs);

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
        /// <param name="source">Source component</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New PoolOperationCompletedMessage instance</returns>
        public static PoolOperationCompletedMessage Create(
            string poolName,
            string strategyName,
            string operationType,
            TimeSpan duration,
            int poolSizeAfter,
            int activeObjectsAfter,
            bool isSuccessful = true,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new PoolOperationCompletedMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.PoolOperationCompletedMessage,
                Source = source.IsEmpty ? "PoolStrategy" : source,
                Priority = MessagePriority.Low, // Performance monitoring, not critical
                CorrelationId = correlationId,
                PoolName = poolName?.Length <= 64 ? poolName : poolName?[..64] ?? "Unknown",
                StrategyName = strategyName?.Length <= 64 ? strategyName : strategyName?[..64] ?? "Unknown",
                OperationType = operationType?.Length <= 64 ? operationType : operationType?[..64] ?? "Unknown",
                DurationMs = duration.TotalMilliseconds,
                PoolSizeAfter = poolSizeAfter,
                ActiveObjectsAfter = activeObjectsAfter,
                IsSuccessful = isSuccessful
            };
        }
    }
}