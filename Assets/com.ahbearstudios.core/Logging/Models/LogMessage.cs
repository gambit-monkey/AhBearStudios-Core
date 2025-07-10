using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Represents a log message with all associated metadata.
    /// Designed for high-performance scenarios with minimal allocations.
    /// Implements IMessage for integration with the messaging system.
    /// </summary>
    public readonly record struct LogMessage(
        Guid Id,
        DateTime Timestamp,
        LogLevel Level,
        string Channel,
        string Message,
        Exception Exception = null,
        string CorrelationId = null,
        IReadOnlyDictionary<string, object> Properties = null,
        string SourceContext = null,
        int ThreadId = 0) : IMessage
    {
        /// <summary>
        /// Gets the channel this message belongs to, defaulting to "Default" if null.
        /// </summary>
        public string Channel { get; } = Channel ?? "Default";

        /// <summary>
        /// Gets the log message text, defaulting to empty string if null.
        /// </summary>
        public string Message { get; } = Message ?? string.Empty;

        /// <summary>
        /// Gets the correlation ID for tracking operations across system boundaries, defaulting to empty string if null.
        /// </summary>
        public string CorrelationId { get; } = CorrelationId ?? string.Empty;

        /// <summary>
        /// Gets additional contextual properties for structured logging, defaulting to empty dictionary if null.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; } = Properties ?? new Dictionary<string, object>();

        /// <summary>
        /// Gets the source context (typically the class name) where the message originated, defaulting to empty string if null.
        /// </summary>
        public string SourceContext { get; } = SourceContext ?? string.Empty;

        /// <summary>
        /// Gets the timestamp as ticks for IMessage interface compatibility.
        /// </summary>
        public long TimestampTicks => Timestamp.Ticks;

        /// <summary>
        /// Gets the type code for IMessage interface compatibility.
        /// Uses a predefined type code for log messages.
        /// </summary>
        public ushort TypeCode => MessageTypeCodes.LogMessage;

        /// <summary>
        /// Creates a formatted string representation of this log message.
        /// </summary>
        /// <param name="format">The format template to use</param>
        /// <returns>The formatted log message</returns>
        public string Format(string format = null)
        {
            format ??= "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}";
            
            return format
                .Replace("{Timestamp:yyyy-MM-dd HH:mm:ss.fff}", Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                .Replace("{Level}", Level.ToString())
                .Replace("{Channel}", Channel)
                .Replace("{Message}", Message)
                .Replace("{CorrelationId}", CorrelationId)
                .Replace("{SourceContext}", SourceContext)
                .Replace("{ThreadId}", ThreadId.ToString());
        }

        /// <summary>
        /// Creates a new LogMessage with the current timestamp and a generated ID.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="channel">The channel name</param>
        /// <param name="message">The log message text</param>
        /// <param name="exception">The associated exception, if any</param>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="properties">Additional contextual properties</param>
        /// <param name="sourceContext">The source context</param>
        /// <param name="threadId">The thread ID</param>
        /// <returns>A new LogMessage instance</returns>
        public static LogMessage Create(
            LogLevel level,
            string channel,
            string message,
            Exception exception = null,
            string correlationId = null,
            IReadOnlyDictionary<string, object> properties = null,
            string sourceContext = null,
            int threadId = 0)
        {
            return new LogMessage(
                Id: Guid.NewGuid(),
                Timestamp: DateTime.UtcNow,
                Level: level,
                Channel: channel,
                Message: message,
                Exception: exception,
                CorrelationId: correlationId,
                Properties: properties,
                SourceContext: sourceContext,
                ThreadId: threadId == 0 ? Environment.CurrentManagedThreadId : threadId);
        }
    }
}