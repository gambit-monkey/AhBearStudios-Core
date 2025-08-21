using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Logging.Messages
{
    /// <summary>
    /// Message published when a log target encounters an error.
    /// Replaces direct EventHandler usage for loose coupling through IMessageBus.
    /// </summary>
    public readonly struct LoggingTargetErrorMessage : IMessage
    {
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
        public ushort TypeCode { get; init; } = MessageTypeCodes.LoggingTargetErrorMessage;

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

        /// <summary>
        /// Initializes a new instance of the LoggingTargetErrorMessage struct.
        /// </summary>
        public LoggingTargetErrorMessage()
        {
            Id = default;
            TimestampTicks = default;
            Source = default;
            Priority = default;
            CorrelationId = default;
            TargetName = default;
            ErrorMessage = default;
            ExceptionType = default;
            StackTrace = default;
            LoggingCorrelationId = default;
            Severity = default;
        }

        /// <summary>
        /// Initializes a new instance of the LoggingTargetErrorMessage with parameters.
        /// </summary>
        /// <param name="targetName">The name of the target that encountered the error</param>
        /// <param name="exception">The exception that occurred</param>
        /// <param name="loggingCorrelationId">The correlation ID from the original logging operation</param>
        /// <param name="severity">The severity of the error</param>
        /// <param name="correlationId">The correlation ID for this message</param>
        public LoggingTargetErrorMessage(
            FixedString64Bytes targetName,
            Exception exception,
            FixedString64Bytes loggingCorrelationId = default,
            LogTargetErrorSeverity severity = LogTargetErrorSeverity.Warning,
            Guid correlationId = default)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = MessageTypeCodes.LoggingTargetErrorMessage;
            Source = new FixedString64Bytes("LoggingSystem");
            Priority = severity == LogTargetErrorSeverity.Critical ? MessagePriority.High : MessagePriority.Normal;
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId;
            
            TargetName = targetName;
            LoggingCorrelationId = loggingCorrelationId;
            Severity = severity;
            
            if (exception != null)
            {
                ErrorMessage = new FixedString512Bytes(exception.Message.Length > 511 
                    ? exception.Message.Substring(0, 511) 
                    : exception.Message);
                ExceptionType = new FixedString128Bytes(exception.GetType().Name.Length > 127 
                    ? exception.GetType().Name.Substring(0, 127) 
                    : exception.GetType().Name);
                StackTrace = new FixedString512Bytes(exception.StackTrace?.Length > 511 
                    ? exception.StackTrace.Substring(0, 511) 
                    : exception.StackTrace ?? string.Empty);
            }
            else
            {
                ErrorMessage = new FixedString512Bytes("Unknown error");
                ExceptionType = new FixedString128Bytes("Unknown");
                StackTrace = new FixedString512Bytes();
            }
        }

        /// <summary>
        /// Creates a LoggingTargetErrorMessage from string parameters for convenience.
        /// </summary>
        /// <param name="targetName">The target name</param>
        /// <param name="errorMessage">The error message</param>
        /// <param name="loggingCorrelationId">The logging correlation ID</param>
        /// <param name="severity">The error severity</param>
        /// <param name="correlationId">The message correlation ID</param>
        /// <returns>A new LoggingTargetErrorMessage</returns>
        public static LoggingTargetErrorMessage Create(
            string targetName,
            string errorMessage,
            string loggingCorrelationId = null,
            LogTargetErrorSeverity severity = LogTargetErrorSeverity.Warning,
            Guid correlationId = default)
        {
            return new LoggingTargetErrorMessage(
                new FixedString64Bytes(targetName ?? "Unknown"),
                new Exception(errorMessage ?? "Unknown error"),
                new FixedString64Bytes(loggingCorrelationId ?? string.Empty),
                severity,
                correlationId);
        }

        /// <summary>
        /// Returns a string representation of this message.
        /// </summary>
        /// <returns>A formatted string</returns>
        public override string ToString()
        {
            return $"LogTargetError: {TargetName} - {ErrorMessage} (Severity: {Severity})";
        }
    }

    /// <summary>
    /// Defines the severity levels for log target errors.
    /// </summary>
    public enum LogTargetErrorSeverity : byte
    {
        /// <summary>
        /// Informational - target recovered or minor issue.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Warning - target had an issue but continues operating.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Error - target failed to process but may recover.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Critical - target is non-functional and requires intervention.
        /// </summary>
        Critical = 3
    }
}