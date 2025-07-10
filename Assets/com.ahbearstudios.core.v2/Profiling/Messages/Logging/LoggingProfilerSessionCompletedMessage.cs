
using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Attributes;
using Unity.Profiling;
using LogTag = AhBearStudios.Core.Logging.Tags.Tagging.LogTag;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when a logging profiler session completes.
    /// </summary>
    public struct LoggingProfilerSessionCompletedMessage : IMessage
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
        /// Gets the profiler tag associated with the completed session.
        /// </summary>
        public ProfilerTag Tag { get; }
        
        /// <summary>
        /// Gets the unique identifier for this profiling session.
        /// </summary>
        public Guid SessionId { get; }
        
        /// <summary>
        /// Gets the type of logging operation that was profiled.
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the log level associated with the operation.
        /// </summary>
        public LogLevel LogLevel { get; }
        
        /// <summary>
        /// Gets the log tag associated with the operation.
        /// </summary>
        public LogTag LogTag { get; }
        
        /// <summary>
        /// Gets the number of messages processed during the operation.
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
        /// Gets the duration of the operation in milliseconds.
        /// </summary>
        public double DurationMs { get; }
        
        /// <summary>
        /// Gets the custom metrics recorded during the session.
        /// </summary>
        public IReadOnlyDictionary<string, double> CustomMetrics { get; }
        
        /// <summary>
        /// Creates a new logging profiler session completed message.
        /// </summary>
        /// <param name="tag">Profiler tag for the session.</param>
        /// <param name="sessionId">Unique session identifier.</param>
        /// <param name="operationType">Type of logging operation.</param>
        /// <param name="logLevel">Log level for the operation.</param>
        /// <param name="logTag">Log tag for the operation.</param>
        /// <param name="messageCount">Number of messages processed.</param>
        /// <param name="messageLength">Length of the message(s) in characters.</param>
        /// <param name="targetName">Name of the target being written to.</param>
        /// <param name="formatterName">Name of the formatter being used.</param>
        /// <param name="durationMs">Duration in milliseconds.</param>
        /// <param name="customMetrics">Custom metrics recorded during the session.</param>
        public LoggingProfilerSessionCompletedMessage(
            ProfilerTag tag,
            Guid sessionId,
            string operationType,
            LogLevel logLevel,
            LogTag logTag,
            int messageCount,
            int messageLength,
            string targetName,
            string formatterName,
            double durationMs,
            IReadOnlyDictionary<string, double> customMetrics)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0x5001; // Unique type code for this message type
            Tag = tag;
            SessionId = sessionId;
            OperationType = operationType ?? string.Empty;
            LogLevel = logLevel;
            LogTag = logTag;
            MessageCount = messageCount;
            MessageLength = messageLength;
            TargetName = targetName;
            FormatterName = formatterName;
            DurationMs = durationMs;
            CustomMetrics = customMetrics ?? new Dictionary<string, double>();
        }
        
        /// <summary>
        /// Creates a new logging profiler session completed message (simplified constructor).
        /// </summary>
        /// <param name="tag">Profiler tag for the session.</param>
        /// <param name="operationType">Type of logging operation.</param>
        /// <param name="logLevel">Log level for the operation.</param>
        /// <param name="logTag">Log tag for the operation.</param>
        /// <param name="messageCount">Number of messages processed.</param>
        /// <param name="durationMs">Duration in milliseconds.</param>
        public LoggingProfilerSessionCompletedMessage(
            ProfilerTag tag,
            string operationType,
            LogLevel logLevel,
            LogTag logTag,
            int messageCount,
            double durationMs)
            : this(tag, Guid.NewGuid(), operationType, logLevel, logTag, messageCount, 0, null, null, durationMs, null)
        {
        }
    }
}