using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when a pool operation fails or encounters an error.
    /// Provides error details and context for debugging and alerting systems.
    /// </summary>
    public readonly record struct PoolOperationFailedMessage : IMessage
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

        // Pool operation-specific properties
        /// <summary>
        /// Gets the name of the pool where the operation failed.
        /// </summary>
        public FixedString64Bytes PoolName { get; init; }

        /// <summary>
        /// Gets the name of the strategy managing the pool.
        /// </summary>
        public FixedString64Bytes StrategyName { get; init; }

        /// <summary>
        /// Gets the type of operation that failed.
        /// </summary>
        public FixedString64Bytes OperationType { get; init; }

        /// <summary>
        /// Gets the error message describing what went wrong.
        /// </summary>
        public FixedString512Bytes ErrorMessage { get; init; }

        /// <summary>
        /// Gets the type name of the exception that occurred.
        /// </summary>
        public FixedString128Bytes ExceptionType { get; init; }

        /// <summary>
        /// Gets the current error count for this strategy.
        /// </summary>
        public int ErrorCount { get; init; }

        /// <summary>
        /// Gets the pool size at the time of failure.
        /// </summary>
        public int PoolSizeAtFailure { get; init; }

        /// <summary>
        /// Gets the number of active objects at the time of failure.
        /// </summary>
        public int ActiveObjectsAtFailure { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the timestamp when the operation failed.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new PoolOperationFailedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="strategyName">Name of the strategy</param>
        /// <param name="operationType">Type of operation</param>
        /// <param name="errorMessage">Error message</param>
        /// <param name="exceptionType">Exception type name</param>
        /// <param name="errorCount">Current error count</param>
        /// <param name="poolSizeAtFailure">Pool size at failure</param>
        /// <param name="activeObjectsAtFailure">Active objects at failure</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolOperationFailedMessage instance</returns>
        public static PoolOperationFailedMessage CreateFromFixedStrings(
            FixedString64Bytes poolName,
            FixedString64Bytes strategyName,
            FixedString64Bytes operationType,
            FixedString512Bytes errorMessage,
            FixedString128Bytes exceptionType,
            int errorCount,
            int poolSizeAtFailure,
            int activeObjectsAtFailure,
            Guid correlationId = default,
            FixedString64Bytes source = default)
        {
            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "PoolingService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("PoolOperationFailedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("PoolOperationFailed", $"{poolName}-{operationType}")
                : correlationId;
            
            return new PoolOperationFailedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.PoolOperationFailedMessage,
                Source = source.IsEmpty ? "PoolStrategy" : source,
                Priority = MessagePriority.High, // Errors should be processed with priority
                CorrelationId = finalCorrelationId,
                
                PoolName = poolName,
                StrategyName = strategyName,
                OperationType = operationType,
                ErrorMessage = errorMessage,
                ExceptionType = exceptionType,
                ErrorCount = errorCount,
                PoolSizeAtFailure = poolSizeAtFailure,
                ActiveObjectsAtFailure = activeObjectsAtFailure
            };
        }

        /// <summary>
        /// Creates a new PoolOperationFailedMessage with the specified details.
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="strategyName">Name of the strategy</param>
        /// <param name="operationType">Type of operation</param>
        /// <param name="error">The exception that occurred</param>
        /// <param name="errorCount">Current error count</param>
        /// <param name="poolSizeAtFailure">Pool size at failure</param>
        /// <param name="activeObjectsAtFailure">Active objects at failure</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <param name="source">Source component</param>
        /// <returns>New PoolOperationFailedMessage instance</returns>
        public static PoolOperationFailedMessage Create(
            string poolName,
            string strategyName,
            string operationType,
            Exception error,
            int errorCount,
            int poolSizeAtFailure,
            int activeObjectsAtFailure,
            Guid correlationId = default,
            string source = null)
        {
            return CreateFromFixedStrings(
                new FixedString64Bytes(poolName?.Length <= 64 ? poolName : poolName?[..64] ?? "Unknown"),
                new FixedString64Bytes(strategyName?.Length <= 64 ? strategyName : strategyName?[..64] ?? "Unknown"),
                new FixedString64Bytes(operationType?.Length <= 64 ? operationType : operationType?[..64] ?? "Unknown"),
                new FixedString512Bytes(error?.Message?.Length <= 512 ? error.Message : error?.Message?[..512] ?? "Unknown error"),
                new FixedString128Bytes(error?.GetType().Name?.Length <= 128 ? error.GetType().Name : error?.GetType().Name?[..128] ?? "Exception"),
                errorCount,
                poolSizeAtFailure,
                activeObjectsAtFailure,
                correlationId,
                new FixedString64Bytes(source ?? "PoolingService"));
        }

        #endregion
    }
}