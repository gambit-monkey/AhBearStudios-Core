using System;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message sent when a session alert is triggered
    /// </summary>
    public struct SessionAlertMessage : IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance.
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks).
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type.
        /// </summary>
        public ushort TypeCode => 10006; // You would use your message registry to assign appropriate codes

        /// <summary>
        /// The profiler tag associated with this session
        /// </summary>
        public ProfilerTag SessionTag { get; }
        
        /// <summary>
        /// The duration of the session in milliseconds
        /// </summary>
        public double DurationMs { get; }
        
        /// <summary>
        /// The threshold that was exceeded
        /// </summary>
        public double ThresholdMs { get; }
        
        /// <summary>
        /// The percentage by which the threshold was exceeded
        /// </summary>
        public double ExceedancePercentage { get; }

        /// <summary>
        /// Creates a new SessionAlertMessage
        /// </summary>
        /// <param name="sessionTag">The session tag</param>
        /// <param name="durationMs">Duration in milliseconds</param>
        /// <param name="thresholdMs">Threshold in milliseconds</param>
        public SessionAlertMessage(ProfilerTag sessionTag, double durationMs, double thresholdMs)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            SessionTag = sessionTag;
            DurationMs = durationMs;
            ThresholdMs = thresholdMs;
            ExceedancePercentage = thresholdMs > 0 ? ((durationMs / thresholdMs) - 1.0) * 100.0 : 0;
        }
    }
}