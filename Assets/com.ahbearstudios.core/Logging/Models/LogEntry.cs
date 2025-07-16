using System.Collections.Generic;
using Unity.Collections;
using Unity.Burst;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Represents a complete log entry with all associated metadata and context.
    /// Designed for high-performance scenarios with minimal allocations using Unity.Collections v2.
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

        // Non-Burst compatible fields for rich data (managed separately)
        private readonly Exception _exception;
        private readonly IReadOnlyDictionary<string, object> _properties;
        private readonly ILogScope _scope;

        /// <summary>
        /// Gets the associated exception, if any (not Burst-compatible).
        /// </summary>
        public Exception Exception => _exception;

        /// <summary>
        /// Gets additional contextual properties for structured logging (not Burst-compatible).
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties => _properties ?? EmptyProperties;

        /// <summary>
        /// Gets the log scope context, if any (not Burst-compatible).
        /// </summary>
        public ILogScope Scope => _scope;

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
            ILogScope scope = null)
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
            HasException = exception != null;
            HasProperties = properties != null && properties.Count > 0;
            HasScope = scope != null;
            _exception = exception;
            _properties = properties;
            _scope = scope;
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
        /// <returns>A new LogEntry instance</returns>
        [BurstCompile]
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
            ILogScope scope = null)
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
                scope: scope);
        }

        /// <summary>
        /// Creates a LogEntry from a LogMessage.
        /// </summary>
        /// <param name="logMessage">The log message to convert</param>
        /// <param name="scope">The log scope context</param>
        /// <returns>A new LogEntry instance</returns>
        public static LogEntry FromLogMessage(in LogMessage logMessage, ILogScope scope = null)
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
                scope: scope);
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
                scope: null);
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
                dictionary["Scope.Id"] = Scope?.ScopeId;
                dictionary["Scope.Name"] = Scope?.ScopeName;
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
        /// Disposes any managed resources (for IDisposable compliance).
        /// </summary>
        public void Dispose()
        {
            // Native strings are stack-allocated, no disposal needed
            // Managed properties are handled by GC
            // Scope is managed by the logging system
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
    }
}