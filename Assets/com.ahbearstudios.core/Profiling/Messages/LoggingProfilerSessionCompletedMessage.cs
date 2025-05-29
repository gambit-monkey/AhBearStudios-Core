using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Attributes;

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
        public string LogTag { get; }
        
        /// <summary>
        /// Gets the number of messages processed during the operation.
        /// </summary>
        public int MessageCount { get; }
        
        /// <summary>
        /// Gets the duration of the operation in milliseconds.
        /// </summary>
        public double DurationMs { get; }
        
        /// <summary>
        /// Creates a new logging profiler session completed message.
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
            string logTag,
            int messageCount,
            double durationMs)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0x5001; // Unique type code for this message type
            Tag = tag;
            OperationType = operationType ?? string.Empty;
            LogLevel = logLevel;
            LogTag = logTag ?? string.Empty;
            MessageCount = messageCount;
            DurationMs = durationMs;
        }
    }
}