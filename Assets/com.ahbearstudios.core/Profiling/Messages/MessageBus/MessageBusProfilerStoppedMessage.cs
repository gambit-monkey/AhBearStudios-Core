using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when message bus profiler stops collecting metrics.
    /// </summary>
    public struct MessageBusProfilerStoppedMessage : IMessage
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
        /// Gets the name of the message bus profiler instance.
        /// </summary>
        public string ProfilerName { get; }
        
        /// <summary>
        /// Gets the identifier of the message bus being profiled.
        /// </summary>
        public Guid BusId { get; }
        
        /// <summary>
        /// Gets the name of the message bus being profiled.
        /// </summary>
        public string BusName { get; }
        
        /// <summary>
        /// Gets the total duration the profiler was active in milliseconds.
        /// </summary>
        public double TotalDurationMs { get; }
        
        /// <summary>
        /// Gets the total number of messages that were processed.
        /// </summary>
        public long TotalMessages { get; }
        
        /// <summary>
        /// Gets the total number of operations that were profiled.
        /// </summary>
        public long TotalOperations { get; }
        
        /// <summary>
        /// Gets summary statistics as a formatted string.
        /// </summary>
        public string Summary { get; }
        
        /// <summary>
        /// Creates a new message bus profiler stopped message.
        /// </summary>
        /// <param name="profilerName">Name of the profiler instance.</param>
        /// <param name="busId">Identifier of the message bus.</param>
        /// <param name="busName">Name of the message bus.</param>
        /// <param name="totalDurationMs">Total duration the profiler was active.</param>
        /// <param name="totalMessages">Total number of messages processed.</param>
        /// <param name="totalOperations">Total number of operations profiled.</param>
        /// <param name="summary">Summary statistics.</param>
        public MessageBusProfilerStoppedMessage(
            string profilerName, 
            Guid busId,
            string busName,
            double totalDurationMs, 
            long totalMessages,
            long totalOperations,
            string summary = "")
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0x6002; // Unique type code for this message type
            ProfilerName = profilerName ?? "MessageBusProfiler";
            BusId = busId;
            BusName = busName ?? "Unknown";
            TotalDurationMs = totalDurationMs;
            TotalMessages = totalMessages;
            TotalOperations = totalOperations;
            Summary = summary ?? string.Empty;
        }
    }
}