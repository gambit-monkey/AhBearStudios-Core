using System;
using Unity.Collections;
using Unity.Burst;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Represents a native-compatible log entry for use with Unity.Collections.
    /// This struct contains only unmanaged fields for optimal performance in native collections.
    /// For full feature logging, use LogEntry and convert to/from NativeLogEntry as needed.
    /// </summary>
    [BurstCompile]
    public readonly struct NativeLogEntry
    {
        /// <summary>
        /// Gets the unique identifier for this log entry.
        /// </summary>
        public readonly Guid Id;

        /// <summary>
        /// Gets the timestamp when this entry was created.
        /// </summary>
        public readonly DateTime Timestamp;

        /// <summary>
        /// Gets the log level severity.
        /// </summary>
        public readonly LogLevel Level;

        /// <summary>
        /// Gets the channel this entry belongs to using native string storage.
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
        /// Gets the source context (typically the class name) where the entry originated.
        /// </summary>
        public readonly FixedString128Bytes SourceContext;

        /// <summary>
        /// Gets the source system or component that created this entry.
        /// </summary>
        public readonly FixedString64Bytes Source;

        /// <summary>
        /// Gets the priority level for processing.
        /// </summary>
        public readonly MessagePriority Priority;

        /// <summary>
        /// Gets the thread ID where the entry was created.
        /// </summary>
        public readonly int ThreadId;

        /// <summary>
        /// Gets the machine name where the entry was created.
        /// </summary>
        public readonly FixedString64Bytes MachineName;

        /// <summary>
        /// Gets the application instance ID.
        /// </summary>
        public readonly FixedString64Bytes InstanceId;

        /// <summary>
        /// Gets whether this entry has an associated exception.
        /// </summary>
        public readonly bool HasException;

        /// <summary>
        /// Gets whether this entry has structured properties.
        /// </summary>
        public readonly bool HasProperties;

        /// <summary>
        /// Gets whether this entry has scope context.
        /// </summary>
        public readonly bool HasScope;

        /// <summary>
        /// Initializes a new instance of the NativeLogEntry struct.
        /// </summary>
        /// <param name="id">The unique entry identifier</param>
        /// <param name="timestamp">The entry timestamp</param>
        /// <param name="level">The log level</param>
        /// <param name="channel">The channel name</param>
        /// <param name="message">The log message text</param>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="sourceContext">The source context</param>
        /// <param name="source">The source system or component</param>
        /// <param name="priority">The priority level</param>
        /// <param name="threadId">The thread ID</param>
        /// <param name="machineName">The machine name</param>
        /// <param name="instanceId">The application instance ID</param>
        /// <param name="hasException">Whether this entry has an associated exception</param>
        /// <param name="hasProperties">Whether this entry has structured properties</param>
        /// <param name="hasScope">Whether this entry has scope context</param>
        public NativeLogEntry(
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
            FixedString64Bytes machineName = default,
            FixedString64Bytes instanceId = default,
            bool hasException = false,
            bool hasProperties = false,
            bool hasScope = false)
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
            MachineName = machineName.IsEmpty ? new FixedString64Bytes(Environment.MachineName) : machineName;
            InstanceId = instanceId.IsEmpty ? new FixedString64Bytes(GetInstanceId()) : instanceId;
            HasException = hasException;
            HasProperties = hasProperties;
            HasScope = hasScope;
        }

        /// <summary>
        /// Creates a NativeLogEntry from a LogEntry.
        /// </summary>
        /// <param name="logEntry">The LogEntry to convert</param>
        /// <returns>A new NativeLogEntry instance</returns>
        public static NativeLogEntry FromLogEntry(in LogEntry logEntry)
        {
            return new NativeLogEntry(
                id: logEntry.Id,
                timestamp: logEntry.Timestamp,
                level: logEntry.Level,
                channel: logEntry.Channel,
                message: logEntry.Message,
                correlationId: logEntry.CorrelationId,
                sourceContext: logEntry.SourceContext,
                source: logEntry.Source,
                priority: logEntry.Priority,
                threadId: logEntry.ThreadId,
                machineName: logEntry.MachineName,
                instanceId: logEntry.InstanceId,
                hasException: logEntry.HasException,
                hasProperties: logEntry.HasProperties,
                hasScope: logEntry.HasScope);
        }

        /// <summary>
        /// Converts this NativeLogEntry to a LogEntry.
        /// Note: Exception, Properties, and Scope will be null in the resulting LogEntry.
        /// </summary>
        /// <returns>A new LogEntry instance</returns>
        public LogEntry ToLogEntry()
        {
            return new LogEntry(
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
                machineName: MachineName,
                instanceId: InstanceId,
                exception: null, // Cannot be preserved in native struct
                properties: null, // Cannot be preserved in native struct
                scope: null); // Cannot be preserved in native struct
        }

        /// <summary>
        /// Creates a new NativeLogEntry with the current timestamp and a generated ID.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="channel">The channel name</param>
        /// <param name="message">The log message text</param>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="sourceContext">The source context</param>
        /// <param name="source">The source system or component</param>
        /// <param name="priority">The priority level</param>
        /// <param name="hasException">Whether this entry has an associated exception</param>
        /// <param name="hasProperties">Whether this entry has structured properties</param>
        /// <param name="hasScope">Whether this entry has scope context</param>
        /// <returns>A new NativeLogEntry instance</returns>
        [BurstCompile]
        public static NativeLogEntry Create(
            LogLevel level,
            FixedString64Bytes channel,
            FixedString512Bytes message,
            FixedString128Bytes correlationId = default,
            FixedString128Bytes sourceContext = default,
            FixedString64Bytes source = default,
            MessagePriority priority = MessagePriority.Normal,
            bool hasException = false,
            bool hasProperties = false,
            bool hasScope = false)
        {
            return new NativeLogEntry(
                id: Guid.NewGuid(),
                timestamp: DateTime.UtcNow,
                level: level,
                channel: channel.IsEmpty ? new FixedString64Bytes("Default") : channel,
                message: message,
                correlationId: correlationId,
                sourceContext: sourceContext,
                source: source.IsEmpty ? new FixedString64Bytes("LoggingSystem") : source,
                priority: priority,
                hasException: hasException,
                hasProperties: hasProperties,
                hasScope: hasScope);
        }

        /// <summary>
        /// Determines if this entry should be processed based on minimum level.
        /// </summary>
        /// <param name="minimumLevel">The minimum level to check against</param>
        /// <returns>True if the entry should be processed</returns>
        [BurstCompile]
        public bool ShouldProcess(LogLevel minimumLevel)
        {
            return Level >= minimumLevel;
        }

        /// <summary>
        /// Gets the size in bytes of the native portion of this entry.
        /// </summary>
        /// <returns>The size in bytes</returns>
        [BurstCompile]
        public int GetNativeSize()
        {
            return 16 + 8 + sizeof(LogLevel) + sizeof(MessagePriority) + // Guid (16 bytes) + DateTime (8 bytes) + LogLevel + MessagePriority
                   Channel.Length + Message.Length + 
                   CorrelationId.Length + SourceContext.Length + Source.Length +
                   MachineName.Length + InstanceId.Length +
                   sizeof(int) + sizeof(bool) + sizeof(bool) + sizeof(bool); // ThreadId + HasException + HasProperties + HasScope
        }

        /// <summary>
        /// Converts this entry to a LogMessage.
        /// </summary>
        /// <returns>A LogMessage representation of this entry</returns>
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
        /// Converts this entry to a NativeLogMessage.
        /// </summary>
        /// <returns>A NativeLogMessage representation of this entry</returns>
        public NativeLogMessage ToNativeLogMessage()
        {
            return new NativeLogMessage(
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
                hasException: HasException,
                hasProperties: HasProperties);
        }

        /// <summary>
        /// Gets the application instance ID.
        /// </summary>
        /// <returns>The application instance ID</returns>
        private static string GetInstanceId()
        {
            return Guid.NewGuid().ToString("N")[..8];
        }

        /// <summary>
        /// Returns a string representation of this log entry.
        /// </summary>
        /// <returns>A formatted string representation</returns>
        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}";
        }

        /// <summary>
        /// Determines equality based on entry ID.
        /// </summary>
        /// <param name="other">The other NativeLogEntry to compare</param>
        /// <returns>True if entries are equal</returns>
        public bool Equals(NativeLogEntry other)
        {
            return Id.Equals(other.Id);
        }

        /// <summary>
        /// Gets the hash code based on entry ID.
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}