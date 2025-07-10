using System;
using AhBearStudios.Core.MessageBus.Attributes;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when logging profiler stops collecting metrics.
    /// </summary>
    public struct LoggingProfilerStoppedMessage : IMessage
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
        /// Gets the name of the logging profiler instance.
        /// </summary>
        public string ProfilerName { get; }
        
        /// <summary>
        /// Gets the total duration the profiler was active in milliseconds.
        /// </summary>
        public double TotalDurationMs { get; }
        
        /// <summary>
        /// Gets the total number of sessions that were profiled.
        /// </summary>
        public long TotalSessions { get; }
        
        /// <summary>
        /// Gets summary statistics as a formatted string.
        /// </summary>
        public string Summary { get; }
        
        /// <summary>
        /// Creates a new logging profiler stopped message.
        /// </summary>
        /// <param name="profilerName">Name of the profiler instance.</param>
        /// <param name="totalDurationMs">Total duration the profiler was active.</param>
        /// <param name="totalSessions">Total number of profiled sessions.</param>
        /// <param name="summary">Summary statistics.</param>
        public LoggingProfilerStoppedMessage(
            string profilerName, 
            double totalDurationMs, 
            long totalSessions = 0, 
            string summary = "")
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0x5004; // Unique type code for this message type
            ProfilerName = profilerName ?? "LoggingProfiler";
            TotalDurationMs = totalDurationMs;
            TotalSessions = totalSessions;
            Summary = summary ?? string.Empty;
        }
    }
}