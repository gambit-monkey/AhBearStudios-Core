using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Logging.Messages
{
    /// <summary>
    /// Message published when a log target encounters an error.
    /// Replaces direct EventHandler usage for loose coupling through IMessageBus.
    /// </summary>
    public readonly record struct LoggingTargetErrorMessage : IMessage
    {
        #region IMessage Implementation
        /// <summary>
        /// Gets the unique identifier for this message.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets the timestamp when this message was created.
        /// </summary>
        public long TimestampTicks { get; init; }

        /// <summary>
        /// Gets the type code for this message type.
        /// </summary>
        public ushort TypeCode { get; init; }

        /// <summary>
        /// Gets the source system that published this message.
        /// </summary>
        public FixedString64Bytes Source { get; init; }

        /// <summary>
        /// Gets the priority level for message processing.
        /// </summary>
        public MessagePriority Priority { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; init; }

        #endregion

        #region Message-Specific Properties

        /// <summary>
        /// Gets the name of the log target that encountered the error.
        /// </summary>
        public FixedString64Bytes TargetName { get; init; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public FixedString512Bytes ErrorMessage { get; init; }

        /// <summary>
        /// Gets the exception type name.
        /// </summary>
        public FixedString128Bytes ExceptionType { get; init; }

        /// <summary>
        /// Gets the stack trace (truncated to fit in FixedString).
        /// </summary>
        public FixedString512Bytes StackTrace { get; init; }

        /// <summary>
        /// Gets the correlation ID for the original logging operation.
        /// </summary>
        public FixedString64Bytes LoggingCorrelationId { get; init; }

        /// <summary>
        /// Gets the severity of the error.
        /// </summary>
        public LogTargetErrorSeverity Severity { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new LoggingTargetErrorMessage with proper validation and defaults.
        /// </summary>
        /// <param name="targetName">The name of the target that encountered the error</param>
        /// <param name="exception">The exception that occurred</param>
        /// <param name="loggingCorrelationId">The correlation ID from the original logging operation</param>
        /// <param name="severity">The severity of the error</param>
        /// <param name="correlationId">The correlation ID for this message</param>
        /// <param name="source">Source component creating this message</param>
        /// <returns>New LoggingTargetErrorMessage instance</returns>
        public static LoggingTargetErrorMessage CreateFromException(
            FixedString64Bytes targetName,
            Exception exception,
            FixedString64Bytes loggingCorrelationId = default,
            LogTargetErrorSeverity severity = LogTargetErrorSeverity.Warning,
            Guid correlationId = default,
            FixedString64Bytes source = default)
        {
            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "LoggingSystem" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("LoggingTargetErrorMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("LoggingTargetError", targetName.ToString())
                : correlationId;
            
            FixedString512Bytes errorMessage;
            FixedString128Bytes exceptionType;
            FixedString512Bytes stackTrace;
            
            if (exception != null)
            {
                errorMessage = new FixedString512Bytes(exception.Message.Length > 511 
                    ? exception.Message.Substring(0, 511) 
                    : exception.Message);
                exceptionType = new FixedString128Bytes(exception.GetType().Name.Length > 127 
                    ? exception.GetType().Name.Substring(0, 127) 
                    : exception.GetType().Name);
                stackTrace = new FixedString512Bytes(exception.StackTrace?.Length > 511 
                    ? exception.StackTrace.Substring(0, 511) 
                    : exception.StackTrace ?? string.Empty);
            }
            else
            {
                errorMessage = new FixedString512Bytes("Unknown error");
                exceptionType = new FixedString128Bytes("Unknown");
                stackTrace = new FixedString512Bytes();
            }
            
            return new LoggingTargetErrorMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.LoggingTargetErrorMessage,
                Source = source.IsEmpty ? "LoggingSystem" : source,
                Priority = severity == LogTargetErrorSeverity.Critical ? MessagePriority.High : MessagePriority.Normal,
                CorrelationId = finalCorrelationId,
                
                TargetName = targetName,
                ErrorMessage = errorMessage,
                ExceptionType = exceptionType,
                StackTrace = stackTrace,
                LoggingCorrelationId = loggingCorrelationId,
                Severity = severity
            };
        }

        /// <summary>
        /// Creates a LoggingTargetErrorMessage from string parameters for convenience.
        /// </summary>
        /// <param name="targetName">The target name</param>
        /// <param name="errorMessage">The error message</param>
        /// <param name="loggingCorrelationId">The logging correlation ID</param>
        /// <param name="severity">The error severity</param>
        /// <param name="correlationId">The message correlation ID</param>
        /// <param name="source">Source component creating this message</param>
        /// <returns>A new LoggingTargetErrorMessage</returns>
        public static LoggingTargetErrorMessage Create(
            string targetName,
            string errorMessage,
            string loggingCorrelationId = null,
            LogTargetErrorSeverity severity = LogTargetErrorSeverity.Warning,
            Guid correlationId = default,
            string source = null)
        {
            return CreateFromException(
                new FixedString64Bytes(targetName ?? "Unknown"),
                new Exception(errorMessage ?? "Unknown error"),
                new FixedString64Bytes(loggingCorrelationId ?? string.Empty),
                severity,
                correlationId,
                new FixedString64Bytes(source ?? "LoggingSystem"));
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message.
        /// </summary>
        /// <returns>A formatted string</returns>
        public override string ToString()
        {
            var targetName = TargetName.IsEmpty ? "Unknown" : TargetName.ToString();
            var errorMessage = ErrorMessage.IsEmpty ? "Unknown error" : ErrorMessage.ToString();
            return $"LogTargetError: {targetName} - {errorMessage} (Severity: {Severity})";
        }

        #endregion
    }
}