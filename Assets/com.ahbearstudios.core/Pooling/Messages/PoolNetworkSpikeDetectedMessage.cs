using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when a network spike is detected.
    /// </summary>
    public readonly record struct PoolNetworkSpikeDetectedMessage : IMessage
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
        public ushort TypeCode { get; init; } = MessageTypeCodes.PoolNetworkSpikeDetectedMessage;

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

        // Network spike-specific properties
        /// <summary>
        /// Gets the name of the strategy that detected the spike.
        /// </summary>
        public FixedString64Bytes StrategyName { get; init; }

        /// <summary>
        /// Gets the number of operations per second during the spike.
        /// </summary>
        public double OperationsPerSecond { get; init; }

        /// <summary>
        /// Initializes a new instance of the PoolNetworkSpikeDetectedMessage struct.
        /// </summary>
        public PoolNetworkSpikeDetectedMessage()
        {
            Id = default;
            TimestampTicks = default;
            Source = default;
            Priority = default;
            CorrelationId = default;
            StrategyName = default;
            OperationsPerSecond = default;
        }

        /// <summary>
        /// Gets the timestamp when the spike was detected.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new PoolNetworkSpikeDetectedMessage with the specified details.
        /// </summary>
        /// <param name="strategyName">Strategy that detected the spike</param>
        /// <param name="operationsPerSecond">Operations per second during spike</param>
        /// <param name="source">Source component</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New PoolNetworkSpikeDetectedMessage instance</returns>
        public static PoolNetworkSpikeDetectedMessage Create(
            string strategyName,
            double operationsPerSecond,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            return new PoolNetworkSpikeDetectedMessage
            {
                Id = Guid.NewGuid(),
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.PoolNetworkSpikeDetectedMessage,
                Source = source.IsEmpty ? "PoolingService" : source,
                Priority = MessagePriority.High, // Network spikes are high priority
                CorrelationId = correlationId,
                StrategyName = strategyName?.Length <= 64 ? strategyName : strategyName?[..64] ?? "Unknown",
                OperationsPerSecond = operationsPerSecond
            };
        }
    }
}