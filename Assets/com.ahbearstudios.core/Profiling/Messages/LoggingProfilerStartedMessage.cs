using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when logging profiler starts collecting metrics.
    /// </summary>
    public struct LoggingProfilerStartedMessage : IMessage
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
        /// Gets the configuration details for the profiler.
        /// </summary>
        public string Configuration { get; }
        
        /// <summary>
        /// Creates a new logging profiler started message.
        /// </summary>
        /// <param name="profilerName">Name of the profiler instance.</param>
        /// <param name="configuration">Configuration details.</param>
        public LoggingProfilerStartedMessage(string profilerName, string configuration = "")
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0x5003; // Unique type code for this message type
            ProfilerName = profilerName ?? "LoggingProfiler";
            Configuration = configuration ?? string.Empty;
        }
    }
}