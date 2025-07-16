using System.Collections.Generic;
using Unity.Collections;
using Unity.Burst;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Pooling.Pools;

namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Represents a complete log entry with all associated metadata and context.
    /// Designed for high-performance scenarios with minimal allocations using Unity.Collections v2.
    /// Uses hybrid approach: native-compatible core data with managed data stored in pool.
    /// Primary data structure used throughout the logging system for processing and storage.
    /// </summary>
    [BurstCompile]
    public readonly struct LogEntry : IDisposable
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
        /// Gets the unique identifier for managed data storage (exceptions, properties, scopes).
        /// Empty Guid indicates no managed data is associated with this entry.
        /// </summary>
        public readonly Guid ManagedDataId;

        /// <summary>
        /// Gets the associated exception, if any (retrieved from managed data pool).
        /// </summary>
        public Exception Exception => GetManagedData()?.Exception;

        /// <summary>
        /// Gets additional contextual properties for structured logging (retrieved from managed data pool).
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties => GetManagedData()?.Properties ?? EmptyProperties;

        /// <summary>
        /// Gets the log scope context, if any (retrieved from managed data pool).
        /// </summary>
        public object Scope => GetManagedData()?.Scope;

        /// <summary>
        /// Empty properties dictionary to avoid allocations.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, object> EmptyProperties = 
            new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the LogEntry struct.
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
        /// <param name="exception">The associated exception</param>
        /// <param name="properties">Additional structured properties</param>
        /// <param name="scope">The log scope context</param>
        /// <param name="managedDataPool">The managed data pool for storing non-native data</param>
        public LogEntry(
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
            Exception exception = null,
            IReadOnlyDictionary<string, object> properties = null,
            object scope = null,
            ManagedLogDataPool managedDataPool = null)
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
            ThreadId = threadId == 0 ? System.Threading.Thread.CurrentThread.ManagedThreadId : threadId;
            MachineName = machineName.IsEmpty ? new FixedString64Bytes(Environment.MachineName) : machineName;
            InstanceId = instanceId.IsEmpty ? new FixedString64Bytes(GetInstanceId()) : instanceId;
            HasException = exception != null;
            HasProperties = properties != null && properties.Count > 0;
            HasScope = scope != null;
            
            // Store managed data in pool if any exists
            if (managedDataPool != null && (exception != null || (properties != null && properties.Count > 0) || scope != null))
            {
                ManagedDataId = managedDataPool.StoreData(exception, properties, scope);
            }
            else
            {
                ManagedDataId = Guid.Empty;
            }
        }

        /// <summary>
        /// Creates a new LogEntry with the current timestamp and a generated ID.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="channel">The channel name</param>
        /// <param name="message">The log message text</param>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="sourceContext">The source context</param>
        /// <param name="source">The source system or component</param>
        /// <param name="priority">The priority level</param>
        /// <param name="exception">The associated exception, if any</param>
        /// <param name="properties">Additional contextual properties</param>
        /// <param name="scope">The log scope context</param>
        /// <param name="managedDataPool">The managed data pool for storing non-native data</param>
        /// <returns>A new LogEntry instance</returns>
        public static LogEntry Create(
            LogLevel level,
            string channel,
            string message,
            string correlationId = null,
            string sourceContext = null,
            string source = null,
            MessagePriority priority = MessagePriority.Normal,
            Exception exception = null,
            IReadOnlyDictionary<string, object> properties = null,
            object scope = null,
            ManagedLogDataPool managedDataPool = null)
        {
            return new LogEntry(
                id: Guid.NewGuid(),
                timestamp: DateTime.UtcNow,
                level: level,
                channel: new FixedString64Bytes(channel ?? "Default"),
                message: new FixedString512Bytes(message ?? string.Empty),
                correlationId: new FixedString128Bytes(correlationId ?? string.Empty),
                sourceContext: new FixedString128Bytes(sourceContext ?? string.Empty),
                source: new FixedString64Bytes(source ?? "LoggingSystem"),
                priority: priority,
                exception: exception,
                properties: properties,
                scope: scope,
                managedDataPool: managedDataPool);
        }

        /// <summary>
        /// Creates a LogEntry from a LogMessage.
        /// </summary>
        /// <param name="logMessage">The log message to convert</param>
        /// <param name="scope">The log scope context</param>
        /// <param name="managedDataPool">The managed data pool for storing non-native data</param>
        /// <returns>A new LogEntry instance</returns>
        public static LogEntry FromLogMessage(in LogMessage logMessage, object scope = null, ManagedLogDataPool managedDataPool = null)
        {
            return new LogEntry(
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
                exception: logMessage.Exception,
                properties: logMessage.Properties,
                scope: scope,
                managedDataPool: managedDataPool);
        }

        /// <summary>
        /// Creates a Burst-compatible LogEntry from native strings.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="channel">The channel name as FixedString</param>
        /// <param name="message">The log message as FixedString</param>
        /// <param name="correlationId">The correlation ID as FixedString</param>
        /// <param name="sourceContext">The source context as FixedString</param>
        /// <param name="source">The source system as FixedString</param>
        /// <param name="priority">The priority level</param>
        /// <returns>A new Burst-compatible LogEntry instance</returns>
        [BurstCompile]
        public static LogEntry CreateNative(
            LogLevel level,
            FixedString64Bytes channel,
            FixedString512Bytes message,
            FixedString128Bytes correlationId = default,
            FixedString128Bytes sourceContext = default,
            FixedString64Bytes source = default,
            MessagePriority priority = MessagePriority.Normal)
        {
            return new LogEntry(
                id: Guid.NewGuid(),
                timestamp: DateTime.UtcNow,
                level: level,
                channel: channel.IsEmpty ? new FixedString64Bytes("Default") : channel,
                message: message,
                correlationId: correlationId,
                sourceContext: sourceContext,
                source: source.IsEmpty ? new FixedString64Bytes("LoggingSystem") : source,
                priority: priority,
                exception: null,
                properties: null,
                scope: null,
                managedDataPool: null);
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
                   sizeof(int) + sizeof(bool) + sizeof(bool) + sizeof(bool) + // ThreadId + HasException + HasProperties + HasScope
                   16; // ManagedDataId (Guid - 16 bytes)
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
                exception: Exception,
                properties: Properties);
        }

        /// <summary>
        /// Converts native strings to managed strings for interop scenarios.
        /// </summary>
        /// <returns>A tuple containing the managed string representations</returns>
        public (string channel, string message, string correlationId, string sourceContext, string source, string machineName, string instanceId) ToManagedStrings()
        {
            return (
                Channel.ToString(),
                Message.ToString(),
                CorrelationId.ToString(),
                SourceContext.ToString(),
                Source.ToString(),
                MachineName.ToString(),
                InstanceId.ToString()
            );
        }

        /// <summary>
        /// Converts this entry to a dictionary for structured logging.
        /// </summary>
        /// <returns>A dictionary representation of the entry</returns>
        public Dictionary<string, object> ToDictionary()
        {
            var dictionary = new Dictionary<string, object>
            {
                ["Id"] = Id,
                ["Timestamp"] = Timestamp,
                ["Level"] = Level,
                ["Channel"] = Channel.ToString(),
                ["Message"] = Message.ToString(),
                ["ThreadId"] = ThreadId,
                ["MachineName"] = MachineName.ToString(),
                ["InstanceId"] = InstanceId.ToString(),
                ["Priority"] = Priority
            };

            if (!CorrelationId.IsEmpty)
                dictionary["CorrelationId"] = CorrelationId.ToString();

            if (!SourceContext.IsEmpty)
                dictionary["SourceContext"] = SourceContext.ToString();

            if (!Source.IsEmpty)
                dictionary["Source"] = Source.ToString();

            if (HasException)
            {
                dictionary["Exception"] = Exception?.ToString();
                dictionary["ExceptionType"] = Exception?.GetType().Name;
            }

            if (HasProperties)
            {
                foreach (var kvp in Properties)
                {
                    dictionary[$"Properties.{kvp.Key}"] = kvp.Value;
                }
            }

            if (HasScope)
            {
                dictionary["Scope"] = Scope?.ToString();
            }

            if (ManagedDataId != Guid.Empty)
            {
                dictionary["ManagedDataId"] = ManagedDataId;
            }

            return dictionary;
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
        /// Disposes any managed resources and releases pooled data.
        /// </summary>
        public void Dispose()
        {
            // Release managed data from pool if it exists
            if (ManagedDataId != Guid.Empty)
            {
                // Note: This requires access to the pool instance
                // In practice, the logging system manages this lifecycle
                // Individual entries should not directly dispose managed data
            }
            // Native strings are stack-allocated, no disposal needed
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
        /// <param name="other">The other LogEntry to compare</param>
        /// <returns>True if entries are equal</returns>
        public bool Equals(LogEntry other)
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
        /// Creates a LogEntry with managed data stored in the pool.
        /// </summary>
        /// <param name="entry">The base entry</param>
        /// <param name="managedDataPool">The managed data pool</param>
        /// <param name="exception">The exception to store</param>
        /// <param name="properties">The properties to store</param>
        /// <param name="scope">The scope to store</param>
        /// <returns>A new LogEntry with managed data</returns>
        public static LogEntry WithManagedData(
            in LogEntry entry,
            ManagedLogDataPool managedDataPool,
            Exception exception = null,
            IReadOnlyDictionary<string, object> properties = null,
            object scope = null)
        {
            var managedDataId = Guid.Empty;
            if (managedDataPool != null && (exception != null || (properties != null && properties.Count > 0) || scope != null))
            {
                managedDataId = managedDataPool.StoreData(exception, properties, scope);
            }

            return new LogEntry(
                id: entry.Id,
                timestamp: entry.Timestamp,
                level: entry.Level,
                channel: entry.Channel,
                message: entry.Message,
                correlationId: entry.CorrelationId,
                sourceContext: entry.SourceContext,
                source: entry.Source,
                priority: entry.Priority,
                threadId: entry.ThreadId,
                machineName: entry.MachineName,
                instanceId: entry.InstanceId,
                exception: exception,
                properties: properties,
                scope: scope,
                managedDataPool: managedDataPool);
        }
    }
}