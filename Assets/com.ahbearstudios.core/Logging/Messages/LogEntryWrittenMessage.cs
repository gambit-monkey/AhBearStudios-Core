using System;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Logging.Messages
{
    /// <summary>
    /// Message sent when a log message has been written to at least one target.
    /// Represents a message-based alternative to LogMessageWrittenEventArgs.
    /// </summary>
    public struct LogEntryWrittenMessage : IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance.
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks).
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type.
        /// </summary>
        public ushort TypeCode => 10101; // Assign appropriate code from your message registry
        
        /// <summary>
        /// Gets the log message that was written.
        /// </summary>
        public LogMessage LogMessage { get; }
        
        /// <summary>
        /// Gets the number of targets the message was written to.
        /// </summary>
        public int TargetCount { get; }
        
        /// <summary>
        /// Creates a new LogMessageWrittenMessage instance.
        /// </summary>
        /// <param name="logMessage">The log message that was written.</param>
        /// <param name="targetCount">The number of targets the message was written to.</param>
        public LogEntryWrittenMessage(LogMessage logMessage, int targetCount)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            LogMessage = logMessage;
            TargetCount = targetCount;
        }
    }
}