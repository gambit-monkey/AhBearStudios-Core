using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when logging metrics are reset.
    /// </summary>
    public struct LoggingMetricsResetMessage : IMessage
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
        /// Gets the name of the profiler instance that was reset.
        /// </summary>
        public string ProfilerName { get; }
        
        /// <summary>
        /// Gets the reason for the reset.
        /// </summary>
        public string ResetReason { get; }
        
        /// <summary>
        /// Gets the number of metrics that were cleared.
        /// </summary>
        public int ClearedMetricsCount { get; }
        
        /// <summary>
        /// Creates a new logging metrics reset message.
        /// </summary>
        /// <param name="profilerName">Name of the profiler instance.</param>
        /// <param name="resetReason">Reason for the reset.</param>
        /// <param name="clearedMetricsCount">Number of metrics cleared.</param>
        public LoggingMetricsResetMessage(
            string profilerName, 
            string resetReason = "Manual", 
            int clearedMetricsCount = 0)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0x5005; // Unique type code for this message type
            ProfilerName = profilerName ?? "LoggingProfiler";
            ResetReason = resetReason ?? "Unknown";
            ClearedMetricsCount = clearedMetricsCount;
        }
    }
}