using System;
using Unity.Collections;
using Unity.Burst;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Represents a native-compatible log message for use with Unity.Collections.
    /// This struct contains only unmanaged fields for optimal performance in native collections.
    /// For full feature logging, use LogMessage and convert to/from NativeLogMessage as needed.
    /// </summary>
    [BurstCompile]
    public readonly struct NativeLogMessage
    {
        /// <summary>
        /// Gets the unique identifier for this message.
        /// </summary>
        public readonly Guid Id;

        /// <summary>
        /// Gets the timestamp when this message was created.
        /// </summary>
        public readonly DateTime Timestamp;

        /// <summary>
        /// Gets the log level severity.
        /// </summary>
        public readonly LogLevel Level;

        /// <summary>
        /// Gets the channel this message belongs to using native string storage.
        /// </summary>
        public readonly FixedString64Bytes Channel;

        /// <summary>
        /// Gets the log message text using native string storage.
        /// </summary>
        public readonly FixedString512Bytes Message;

        /// <summary>
        /// Gets the correlation ID for tracking operations across system boundaries.
        /// </summary>
        public readonly FixedString128Bytes CorrelationId;

        /// <summary>
        /// Gets the source system or component that created this message.
        /// </summary>
        public readonly FixedString64Bytes Source;

        /// <summary>
        /// Gets the priority level for message processing.
        /// </summary>
        public readonly MessagePriority Priority;

        /// <summary>
        /// Gets the source context (typically the class name) where the message originated.
        /// </summary>
        public readonly FixedString128Bytes SourceContext;

        /// <summary>
        /// Gets the thread ID where the message was created.
        /// </summary>
        public readonly int ThreadId;

        /// <summary>
        /// Gets whether this message has an associated exception.
        /// </summary>
        public readonly bool HasException;

        /// <summary>
        /// Gets whether this message has structured properties.
        /// </summary>
        public readonly bool HasProperties;

        /// <summary>
        /// Initializes a new instance of the NativeLogMessage struct.
        /// </summary>
        /// <param name="id">The unique message identifier</param>
        /// <param name="timestamp">The message timestamp</param>
        /// <param name="level">The log level</param>
        /// <param name="channel">The channel name</param>
        /// <param name="message">The log message text</param>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="sourceContext">The source context</param>
        /// <param name="source">The source system or component</param>
        /// <param name="priority">The message priority level</param>
        /// <param name="threadId">The thread ID</param>
        /// <param name="hasException">Whether this message has an associated exception</param>
        /// <param name="hasProperties">Whether this message has structured properties</param>
        public NativeLogMessage(
            Guid id,
            DateTime timestamp,
            LogLevel level,
            FixedString64Bytes channel,
            FixedString512Bytes message,
            FixedString128Bytes correlationId = default,
            FixedString128Bytes sourceContext = default,
            FixedString64Bytes source = default,
            MessagePriority priority = MessagePriority.Normal,
            int threadId = 0,
            bool hasException = false,
            bool hasProperties = false)
        {
            Id = id;
            Timestamp = timestamp;
            Level = level;
            Channel = channel.IsEmpty ? new FixedString64Bytes("Default") : channel;
            Message = message;
            CorrelationId = correlationId;
            SourceContext = sourceContext;
            Source = source.IsEmpty ? new FixedString64Bytes("LoggingSystem") : source;
            Priority = priority;
            ThreadId = threadId == 0 ? Environment.CurrentManagedThreadId : threadId;
            HasException = hasException;
            HasProperties = hasProperties;
        }

        /// <summary>
        /// Creates a NativeLogMessage from a LogMessage.
        /// </summary>
        /// <param name="logMessage">The LogMessage to convert</param>
        /// <returns>A new NativeLogMessage instance</returns>
        public static NativeLogMessage FromLogMessage(in LogMessage logMessage)
        {
            return new NativeLogMessage(
                id: logMessage.Id,
                timestamp: logMessage.Timestamp,
                level: logMessage.Level,
                channel: logMessage.Channel,
                message: logMessage.Message,
                correlationId: logMessage.CorrelationId,
                sourceContext: logMessage.SourceContext,
                source: logMessage.Source,
                priority: logMessage.Priority,
                threadId: logMessage.ThreadId,
                hasException: logMessage.HasException,
                hasProperties: logMessage.HasProperties);
        }

        /// <summary>
        /// Converts this NativeLogMessage to a LogMessage.
        /// Note: Exception and Properties will be null in the resulting LogMessage.
        /// </summary>
        /// <returns>A new LogMessage instance</returns>
        public LogMessage ToLogMessage()
        {
            return new LogMessage(
                id: Id,
                timestamp: Timestamp,
                level: Level,
                channel: Channel,
                message: Message,
                correlationId: CorrelationId,
                sourceContext: SourceContext,
                source: Source,
                priority: Priority,
                threadId: ThreadId,
                exception: null, // Cannot be preserved in native struct
                properties: null); // Cannot be preserved in native struct
        }

        /// <summary>
        /// Creates a new NativeLogMessage with the current timestamp and a generated ID.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="channel">The channel name</param>
        /// <param name="message">The log message text</param>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="sourceContext">The source context</param>
        /// <param name="source">The source system or component</param>
        /// <param name="priority">The message priority level</param>
        /// <param name="threadId">The thread ID</param>
        /// <param name="hasException">Whether this message has an associated exception</param>
        /// <param name="hasProperties">Whether this message has structured properties</param>
        /// <returns>A new NativeLogMessage instance</returns>
        [BurstCompile]
        public static NativeLogMessage Create(
            LogLevel level,
            FixedString64Bytes channel,
            FixedString512Bytes message,
            FixedString128Bytes correlationId = default,
            FixedString128Bytes sourceContext = default,
            FixedString64Bytes source = default,
            MessagePriority priority = MessagePriority.Normal,
            int threadId = 0,
            bool hasException = false,
            bool hasProperties = false)
        {
            return new NativeLogMessage(
                id: Guid.NewGuid(),
                timestamp: DateTime.UtcNow,
                level: level,
                channel: channel.IsEmpty ? new FixedString64Bytes("Default") : channel,
                message: message,
                correlationId: correlationId,
                sourceContext: sourceContext,
                source: source.IsEmpty ? new FixedString64Bytes("LoggingSystem") : source,
                priority: priority,
                threadId: threadId == 0 ? Environment.CurrentManagedThreadId : threadId,
                hasException: hasException,
                hasProperties: hasProperties);
        }

        /// <summary>
        /// Determines if this message should be processed based on minimum level.
        /// </summary>
        /// <param name="minimumLevel">The minimum level to check against</param>
        /// <returns>True if the message should be processed</returns>
        [BurstCompile]
        public bool ShouldProcess(LogLevel minimumLevel)
        {
            return Level >= minimumLevel;
        }

        /// <summary>
        /// Gets the size in bytes of this native message.
        /// </summary>
        /// <returns>The size in bytes</returns>
        [BurstCompile]
        public int GetNativeSize()
        {
            return 16 + 8 + sizeof(LogLevel) + sizeof(MessagePriority) + // Guid (16 bytes) + DateTime (8 bytes) + LogLevel + MessagePriority
                   Channel.Length + Message.Length + 
                   CorrelationId.Length + SourceContext.Length + Source.Length +
                   sizeof(int) + sizeof(bool) + sizeof(bool); // ThreadId + HasException + HasProperties
        }

        /// <summary>
        /// Creates a formatted string representation of this native log message.
        /// </summary>
        /// <returns>A FixedString containing the formatted message</returns>
        [BurstCompile]
        public FixedString512Bytes FormatNative()
        {
            var result = new FixedString512Bytes();
            
            // Simple format: [Level] [Channel] Message
            result.Append('[');
            result.Append(Level.ToString());
            result.Append("] [");
            result.Append(Channel);
            result.Append("] ");
            result.Append(Message);
            
            return result;
        }

        /// <summary>
        /// Returns a string representation of this native log message.
        /// </summary>
        /// <returns>A formatted string representation</returns>
        public override string ToString()
        {
            return FormatNative().ToString();
        }

        /// <summary>
        /// Determines equality based on message ID.
        /// </summary>
        /// <param name="other">The other NativeLogMessage to compare</param>
        /// <returns>True if messages are equal</returns>
        public bool Equals(NativeLogMessage other)
        {
            return Id.Equals(other.Id);
        }

        /// <summary>
        /// Gets the hash code based on message ID.
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}