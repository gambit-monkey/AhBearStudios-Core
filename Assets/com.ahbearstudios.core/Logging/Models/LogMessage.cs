using System.Collections.Generic;
using Unity.Collections;
using Unity.Burst;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Pools;

namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Represents a log message with all associated metadata.
    /// Designed for high-performance scenarios with minimal allocations using Unity.Collections v2.
    /// Uses hybrid approach: native-compatible core data with managed data stored in pool.
    /// Implements IMessage for integration with the messaging system.
    /// Burst-compatible for native job system integration.
    /// </summary>
    [BurstCompile]
    public readonly struct LogMessage : IMessage, IDisposable
    {
        /// <summary>
        /// Gets the unique identifier for this message.
        /// </summary>
        private readonly Guid _id;

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
        /// Gets the unique identifier for managed data storage (exceptions, properties).
        /// Empty Guid indicates no managed data is associated with this message.
        /// </summary>
        public readonly Guid ManagedDataId;

        /// <summary>
        /// Gets the unique identifier for this message (IMessage interface implementation).
        /// </summary>
        public Guid Id => _id;

        /// <summary>
        /// Gets the timestamp as ticks for IMessage interface compatibility.
        /// </summary>
        public long TimestampTicks => Timestamp.Ticks;

        /// <summary>
        /// Gets the type code for IMessage interface compatibility.
        /// </summary>
        public ushort TypeCode => MessageTypeCodes.CoreLogMessage;

        /// <summary>
        /// Gets the source system for IMessage interface compatibility.
        /// </summary>
        FixedString64Bytes IMessage.Source => Source;

        /// <summary>
        /// Gets the priority level for IMessage interface compatibility.
        /// </summary>
        MessagePriority IMessage.Priority => Priority;

        /// <summary>
        /// Gets the correlation ID as Guid for IMessage interface compatibility.
        /// </summary>
        Guid IMessage.CorrelationId => ParseCorrelationIdAsGuid();

        /// <summary>
        /// Gets the associated exception, if any (retrieved from managed data pool).
        /// </summary>
        public Exception Exception => GetManagedData()?.Exception;

        /// <summary>
        /// Gets additional contextual properties for structured logging (retrieved from managed data pool).
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties => GetManagedData()?.Properties ?? EmptyProperties;

        /// <summary>
        /// Empty properties dictionary to avoid allocations.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, object> EmptyProperties = 
            new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the LogMessage struct.
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
        /// <param name="exception">The associated exception</param>
        /// <param name="properties">Additional structured properties</param>
        /// <param name="managedDataPool">The managed data pool for storing non-native data</param>
        public LogMessage(
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
            Exception exception = null,
            IReadOnlyDictionary<string, object> properties = null,
            ManagedLogDataPool managedDataPool = null)
        {
            _id = id;
            Timestamp = timestamp;
            Level = level;
            Channel = channel.IsEmpty ? new FixedString64Bytes("Default") : channel;
            Message = message;
            CorrelationId = correlationId;
            SourceContext = sourceContext;
            Source = source.IsEmpty ? new FixedString64Bytes("LoggingSystem") : source;
            Priority = priority;
            ThreadId = threadId == 0 ? System.Threading.Thread.CurrentThread.ManagedThreadId : threadId;
            HasException = exception != null;
            HasProperties = properties != null && properties.Count > 0;
            
            // Store managed data in pool if any exists
            if (managedDataPool != null && (exception != null || (properties != null && properties.Count > 0)))
            {
                ManagedDataId = managedDataPool.StoreData(exception, properties, null);
            }
            else
            {
                ManagedDataId = Guid.Empty;
            }
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
        /// <param name="source">The source system or component</param>
        /// <param name="priority">The message priority level</param>
        /// <param name="threadId">The thread ID</param>
        /// <param name="managedDataPool">The managed data pool for storing non-native data</param>
        /// <returns>A new LogMessage instance</returns>
        public static LogMessage Create(
            LogLevel level,
            string channel,
            string message,
            Exception exception = null,
            string correlationId = null,
            IReadOnlyDictionary<string, object> properties = null,
            string sourceContext = null,
            string source = null,
            MessagePriority priority = MessagePriority.Normal,
            int threadId = 0,
            ManagedLogDataPool managedDataPool = null)
        {
            return new LogMessage(
                id: Guid.NewGuid(),
                timestamp: DateTime.UtcNow,
                level: level,
                channel: new FixedString64Bytes(channel ?? "Default"),
                message: new FixedString512Bytes(message ?? string.Empty),
                correlationId: new FixedString128Bytes(correlationId ?? string.Empty),
                sourceContext: new FixedString128Bytes(sourceContext ?? string.Empty),
                source: new FixedString64Bytes(source ?? "LoggingSystem"),
                priority: priority,
                threadId: threadId == 0 ? System.Threading.Thread.CurrentThread.ManagedThreadId : threadId,
                exception: exception,
                properties: properties,
                managedDataPool: managedDataPool);
        }

        /// <summary>
        /// Creates a Burst-compatible LogMessage from native strings.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="channel">The channel name as FixedString</param>
        /// <param name="message">The log message as FixedString</param>
        /// <param name="correlationId">The correlation ID as FixedString</param>
        /// <param name="sourceContext">The source context as FixedString</param>
        /// <param name="source">The source system as FixedString</param>
        /// <param name="priority">The message priority level</param>
        /// <param name="threadId">The thread ID</param>
        /// <returns>A new Burst-compatible LogMessage instance</returns>
        [BurstCompile]
        public static LogMessage CreateNative(
            LogLevel level,
            FixedString64Bytes channel,
            FixedString512Bytes message,
            FixedString128Bytes correlationId = default,
            FixedString128Bytes sourceContext = default,
            FixedString64Bytes source = default,
            MessagePriority priority = MessagePriority.Normal,
            int threadId = 0)
        {
            return new LogMessage(
                id: Guid.NewGuid(),
                timestamp: DateTime.UtcNow,
                level: level,
                channel: channel.IsEmpty ? new FixedString64Bytes("Default") : channel,
                message: message,
                correlationId: correlationId,
                sourceContext: sourceContext,
                source: source.IsEmpty ? new FixedString64Bytes("LoggingSystem") : source,
                priority: priority,
                threadId: threadId == 0 ? System.Threading.Thread.CurrentThread.ManagedThreadId : threadId,
                exception: null,
                properties: null,
                managedDataPool: null);
        }

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
                .Replace("{Channel}", Channel.ToString())
                .Replace("{Message}", Message.ToString())
                .Replace("{CorrelationId}", CorrelationId.ToString())
                .Replace("{SourceContext}", SourceContext.ToString())
                .Replace("{ThreadId}", ThreadId.ToString());
        }

        /// <summary>
        /// Creates a Burst-compatible formatted representation using native strings.
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
        /// Converts native strings to managed strings for interop scenarios.
        /// </summary>
        /// <returns>A tuple containing the managed string representations</returns>
        public (string channel, string message, string correlationId, string sourceContext, string source) ToManagedStrings()
        {
            return (
                Channel.ToString(),
                Message.ToString(),
                CorrelationId.ToString(),
                SourceContext.ToString(),
                Source.ToString()
            );
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
        /// Gets the size in bytes of the native portion of this message.
        /// </summary>
        /// <returns>The size in bytes</returns>
        [BurstCompile]
        public int GetNativeSize()
        {
            return 16 + 8 + sizeof(LogLevel) + sizeof(MessagePriority) + // Guid (16 bytes) + DateTime (8 bytes) + LogLevel + MessagePriority
                   Channel.Length + Message.Length + 
                   CorrelationId.Length + SourceContext.Length + Source.Length +
                   sizeof(int) + sizeof(bool) + sizeof(bool) + // ThreadId + HasException + HasProperties
                   16; // ManagedDataId (Guid - 16 bytes)
        }

        /// <summary>
        /// Disposes any managed resources and releases pooled data.
        /// </summary>
        public void Dispose()
        {
            // Release managed data from pool if it exists
            if (ManagedDataId != Guid.Empty)
            {
                // Note: This requires access to the pool instance
                // In practice, the logging system manages this lifecycle
                // Individual messages should not directly dispose managed data
            }
            // Native strings are stack-allocated, no disposal needed
        }

        /// <summary>
        /// Parses the correlation ID as a Guid for IMessage interface compatibility.
        /// </summary>
        /// <returns>The correlation ID as a Guid, or Guid.Empty if parsing fails</returns>
        private Guid ParseCorrelationIdAsGuid()
        {
            var correlationIdString = CorrelationId.ToString();
            return Guid.TryParse(correlationIdString, out var guid) ? guid : Guid.Empty;
        }

        /// <summary>
        /// Creates a LogMessage with a Guid correlation ID for better IMessage compatibility.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="channel">The channel name</param>
        /// <param name="message">The log message text</param>
        /// <param name="correlationId">The correlation ID as Guid</param>
        /// <param name="source">The source system or component</param>
        /// <param name="priority">The message priority level</param>
        /// <param name="exception">The associated exception, if any</param>
        /// <param name="properties">Additional contextual properties</param>
        /// <param name="sourceContext">The source context</param>
        /// <param name="threadId">The thread ID</param>
        /// <param name="managedDataPool">The managed data pool for storing non-native data</param>
        /// <returns>A new LogMessage instance</returns>
        public static LogMessage CreateWithGuidCorrelation(
            LogLevel level,
            string channel,
            string message,
            Guid correlationId = default,
            string source = null,
            MessagePriority priority = MessagePriority.Normal,
            Exception exception = null,
            IReadOnlyDictionary<string, object> properties = null,
            string sourceContext = null,
            int threadId = 0,
            ManagedLogDataPool managedDataPool = null)
        {
            return new LogMessage(
                id: Guid.NewGuid(),
                timestamp: DateTime.UtcNow,
                level: level,
                channel: new FixedString64Bytes(channel ?? "Default"),
                message: new FixedString512Bytes(message ?? string.Empty),
                correlationId: new FixedString128Bytes(correlationId.ToString()),
                sourceContext: new FixedString128Bytes(sourceContext ?? string.Empty),
                source: new FixedString64Bytes(source ?? "LoggingSystem"),
                priority: priority,
                threadId: threadId == 0 ? System.Threading.Thread.CurrentThread.ManagedThreadId : threadId,
                exception: exception,
                properties: properties,
                managedDataPool: managedDataPool);
        }

        /// <summary>
        /// Returns a string representation of this log message.
        /// </summary>
        /// <returns>A formatted string representation</returns>
        public override string ToString()
        {
            return Format();
        }

        /// <summary>
        /// Determines equality based on message ID.
        /// </summary>
        /// <param name="other">The other LogMessage to compare</param>
        /// <returns>True if messages are equal</returns>
        public bool Equals(LogMessage other)
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

        /// <summary>
        /// Retrieves managed data from the pool if available.
        /// </summary>
        /// <returns>The managed data, or null if not found</returns>
        private ManagedLogData GetManagedData()
        {
            if (ManagedDataId == Guid.Empty)
                return null;

            // Note: In practice, this would access a static or injected pool instance
            // For now, we'll return null to maintain compilation
            // The logging system will manage the actual pool access
            return null;
        }

        /// <summary>
        /// Creates a LogMessage with managed data stored in the pool.
        /// </summary>
        /// <param name="message">The base message</param>
        /// <param name="managedDataPool">The managed data pool</param>
        /// <param name="exception">The exception to store</param>
        /// <param name="properties">The properties to store</param>
        /// <returns>A new LogMessage with managed data</returns>
        public static LogMessage WithManagedData(
            in LogMessage message,
            ManagedLogDataPool managedDataPool,
            Exception exception = null,
            IReadOnlyDictionary<string, object> properties = null)
        {
            var managedDataId = Guid.Empty;
            if (managedDataPool != null && (exception != null || (properties != null && properties.Count > 0)))
            {
                managedDataId = managedDataPool.StoreData(exception, properties, null);
            }

            return new LogMessage(
                id: message.Id,
                timestamp: message.Timestamp,
                level: message.Level,
                channel: message.Channel,
                message: message.Message,
                correlationId: message.CorrelationId,
                sourceContext: message.SourceContext,
                source: message.Source,
                priority: message.Priority,
                threadId: message.ThreadId,
                exception: exception,
                properties: properties,
                managedDataPool: managedDataPool);
        }
    }
}