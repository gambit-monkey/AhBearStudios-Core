using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when a network spike is detected.
    /// </summary>
    public readonly record struct PoolNetworkSpikeDetectedMessage : IMessage
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
        /// Gets the name of the strategy that detected the spike.
        /// </summary>
        public FixedString64Bytes StrategyName { get; init; }

        /// <summary>
        /// Gets the number of operations per second during the spike.
        /// </summary>
        public double OperationsPerSecond { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the timestamp when the spike was detected.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new PoolNetworkSpikeDetectedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="strategyName">Strategy that detected the spike</param>
        /// <param name="operationsPerSecond">Operations per second during spike</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolNetworkSpikeDetectedMessage instance</returns>
        public static PoolNetworkSpikeDetectedMessage CreateFromFixedStrings(
            FixedString64Bytes strategyName,
            double operationsPerSecond,
            Guid correlationId = default,
            FixedString64Bytes source = default)
        {
            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "PoolingService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("PoolNetworkSpikeDetectedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("PoolNetworkSpike", strategyName.ToString())
                : correlationId;
            
            return new PoolNetworkSpikeDetectedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.PoolNetworkSpikeDetectedMessage,
                Source = source.IsEmpty ? "PoolingService" : source,
                Priority = MessagePriority.High,
                CorrelationId = finalCorrelationId,
                
                StrategyName = strategyName,
                OperationsPerSecond = operationsPerSecond
            };
        }

        /// <summary>
        /// Creates a new PoolNetworkSpikeDetectedMessage with the specified details.
        /// </summary>
        /// <param name="strategyName">Strategy that detected the spike</param>
        /// <param name="operationsPerSecond">Operations per second during spike</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolNetworkSpikeDetectedMessage instance</returns>
        public static PoolNetworkSpikeDetectedMessage Create(
            string strategyName,
            double operationsPerSecond,
            Guid correlationId = default,
            string source = null)
        {
            return CreateFromFixedStrings(
                new FixedString64Bytes(strategyName?.Length <= 64 ? strategyName : strategyName?[..64] ?? "Unknown"),
                operationsPerSecond,
                correlationId,
                new FixedString64Bytes(source ?? "PoolingService"));
        }

        #endregion
    }
}