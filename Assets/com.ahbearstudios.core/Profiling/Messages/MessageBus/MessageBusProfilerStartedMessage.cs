using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when message bus profiler starts collecting metrics.
    /// </summary>
    public struct MessageBusProfilerStartedMessage : IMessage
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
        /// Gets the configuration details for the profiler.
        /// </summary>
        public string Configuration { get; }
        
        /// <summary>
        /// Creates a new message bus profiler started message.
        /// </summary>
        /// <param name="profilerName">Name of the profiler instance.</param>
        /// <param name="busId">Identifier of the message bus.</param>
        /// <param name="busName">Name of the message bus.</param>
        /// <param name="configuration">Configuration details.</param>
        public MessageBusProfilerStartedMessage(string profilerName, Guid busId, string busName, string configuration = "")
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0x6001; // Unique type code for this message type
            ProfilerName = profilerName ?? "MessageBusProfiler";
            BusId = busId;
            BusName = busName ?? "Unknown";
            Configuration = configuration ?? string.Empty;
        }
    }
}