using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Profiling.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when the profiler service encounters an error.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    /// <remarks>
    /// This message is published by the profiler service when errors occur during profiling operations,
    /// replacing the direct event-based approach to maintain compliance with the messaging
    /// architecture guidelines.
    /// </remarks>
    public readonly record struct ProfilerErrorOccurredMessage : IMessage
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
        /// Gets the error message describing what went wrong.
        /// </summary>
        public FixedString512Bytes ErrorMessage { get; init; }

        /// <summary>
        /// Gets the exception type name if an exception was involved.
        /// </summary>
        public FixedString128Bytes ExceptionType { get; init; }

        /// <summary>
        /// Gets the operation that was being performed when the error occurred.
        /// </summary>
        public FixedString128Bytes Operation { get; init; }

        /// <summary>
        /// Gets the profiler tag associated with the error, if applicable.
        /// </summary>
        public ProfilerTag Tag { get; init; }

        /// <summary>
        /// Gets the severity level of the error.
        /// </summary>
        public ProfilerErrorSeverity Severity { get; init; }

        /// <summary>
        /// Gets the optional scope ID if the error occurred within a specific scope.
        /// </summary>
        public Guid ScopeId { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets a value indicating whether this error is critical.
        /// </summary>
        public bool IsCritical => Severity == ProfilerErrorSeverity.Critical;

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new ProfilerErrorOccurredMessage from an exception with proper validation and defaults.
        /// </summary>
        /// <param name="exception">The exception that occurred</param>
        /// <param name="operation">The operation being performed when the error occurred</param>
        /// <param name="tag">Optional profiler tag associated with the error</param>
        /// <param name="scopeId">Optional scope ID</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <param name="severity">Error severity level</param>
        /// <returns>New ProfilerErrorOccurredMessage instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when exception is null</exception>
        public static ProfilerErrorOccurredMessage CreateFromException(
            Exception exception,
            string operation = "Unknown",
            ProfilerTag tag = default,
            Guid scopeId = default,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            ProfilerErrorSeverity severity = ProfilerErrorSeverity.Error)
        {
            // Input validation
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            if (string.IsNullOrEmpty(operation))
                operation = "Unknown";

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "ProfilerService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("ProfilerErrorOccurredMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("ProfilerError", operation)
                : correlationId;

            var errorMessage = exception.Message ?? "Unknown error";
            var exceptionType = exception.GetType().Name;

            return new ProfilerErrorOccurredMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.ProfilingErrorOccurredMessage,
                Source = source.IsEmpty ? "ProfilerService" : source,
                Priority = severity == ProfilerErrorSeverity.Critical ? MessagePriority.Critical : MessagePriority.High,
                CorrelationId = finalCorrelationId,
                ErrorMessage = errorMessage.Length <= 512 ? errorMessage : errorMessage[..512],
                ExceptionType = exceptionType.Length <= 128 ? exceptionType : exceptionType[..128],
                Operation = operation.Length <= 128 ? operation : operation[..128],
                Tag = tag,
                Severity = severity,
                ScopeId = scopeId
            };
        }

        /// <summary>
        /// Creates a new ProfilerErrorOccurredMessage from an error message with proper validation and defaults.
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <param name="operation">The operation being performed when the error occurred</param>
        /// <param name="tag">Optional profiler tag associated with the error</param>
        /// <param name="scopeId">Optional scope ID</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <param name="severity">Error severity level</param>
        /// <returns>New ProfilerErrorOccurredMessage instance</returns>
        /// <exception cref="ArgumentException">Thrown when errorMessage is null or empty</exception>
        public static ProfilerErrorOccurredMessage Create(
            string errorMessage,
            string operation = "Unknown",
            ProfilerTag tag = default,
            Guid scopeId = default,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            ProfilerErrorSeverity severity = ProfilerErrorSeverity.Warning)
        {
            // Input validation
            if (string.IsNullOrEmpty(errorMessage))
                throw new ArgumentException("Error message cannot be null or empty", nameof(errorMessage));

            if (string.IsNullOrEmpty(operation))
                operation = "Unknown";

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "ProfilerService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("ProfilerErrorOccurredMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("ProfilerError", operation)
                : correlationId;

            return new ProfilerErrorOccurredMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.ProfilingErrorOccurredMessage,
                Source = source.IsEmpty ? "ProfilerService" : source,
                Priority = severity == ProfilerErrorSeverity.Critical ? MessagePriority.Critical : MessagePriority.High,
                CorrelationId = finalCorrelationId,
                ErrorMessage = errorMessage.Length <= 512 ? errorMessage : errorMessage[..512],
                ExceptionType = "None",
                Operation = operation.Length <= 128 ? operation : operation[..128],
                Tag = tag,
                Severity = severity,
                ScopeId = scopeId
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Error occurred message string representation</returns>
        public override string ToString()
        {
            return $"ProfilerError [{Severity}]: {ErrorMessage} in {Operation}" +
                   (Tag.IsEmpty ? "" : $" (Tag: {Tag.Name})") +
                   (ExceptionType.ToString() != "None" ? $" [Exception: {ExceptionType}]" : "");
        }

        #endregion
    }
}