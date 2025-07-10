using System;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Logging;
using LogTag = AhBearStudios.Core.Logging.Tags.Tagging.LogTag;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when a logging profiler session starts.
    /// </summary>
    public struct LoggingProfilerSessionStartedMessage : IMessage
    {
        /// <summary>
        /// Gets the unique identifier for this message instance.
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks).
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type.
        /// </summary>
        public ushort TypeCode { get; }
        
        /// <summary>
        /// Gets the profiler tag associated with the started session.
        /// </summary>
        public ProfilerTag Tag { get; }
        
        /// <summary>
        /// Gets the unique identifier for this profiling session.
        /// </summary>
        public Guid SessionId { get; }
        
        /// <summary>
        /// Gets the type of logging operation being profiled.
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the log level for this operation.
        /// </summary>
        public LogLevel LogLevel { get; }
        
        /// <summary>
        /// Gets the log tag for this operation.
        /// </summary>
        public LogTag LogTag { get; }
        
        /// <summary>
        /// Gets the number of messages being processed.
        /// </summary>
        public int MessageCount { get; }
        
        /// <summary>
        /// Gets the length of the message(s) in characters.
        /// </summary>
        public int MessageLength { get; }
        
        /// <summary>
        /// Gets the name of the target being written to (if applicable).
        /// </summary>
        public string TargetName { get; }
        
        /// <summary>
        /// Gets the name of the formatter being used (if applicable).
        /// </summary>
        public string FormatterName { get; }
        
        /// <summary>
        /// Creates a new logging profiler session started message.
        /// </summary>
        /// <param name="tag">Profiler tag</param>
        /// <param name="sessionId">Unique session identifier</param>
        /// <param name="operationType">Type of logging operation</param>
        /// <param name="logLevel">Log level</param>
        /// <param name="logTag">Log tag</param>
        /// <param name="messageCount">Number of messages</param>
        /// <param name="messageLength">Message length</param>
        /// <param name="targetName">Target name (optional)</param>
        /// <param name="formatterName">Formatter name (optional)</param>
        public LoggingProfilerSessionStartedMessage(
            ProfilerTag tag,
            Guid sessionId,
            string operationType,
            LogLevel logLevel,
            LogTag logTag,
            int messageCount,
            int messageLength,
            string targetName,
            string formatterName)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0x5004; // Unique type code for this message type
            Tag = tag;
            SessionId = sessionId;
            OperationType = operationType ?? "Unknown";
            LogLevel = logLevel;
            LogTag = logTag;
            MessageCount = messageCount;
            MessageLength = messageLength;
            TargetName = targetName;
            FormatterName = formatterName;
        }
    }
}