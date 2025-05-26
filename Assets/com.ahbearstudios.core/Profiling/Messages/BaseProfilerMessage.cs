using System;
using AhBearStudios.Core.MessageBus.Attributes;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Events;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Base abstract class for all profiler messages
    /// </summary>
    public abstract class BaseProfilerMessage : IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks)
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type
        /// </summary>
        public abstract ushort TypeCode { get; }
        
        /// <summary>
        /// Creates a new base profiler message
        /// </summary>
        protected BaseProfilerMessage()
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
        }
    }
    
    /// <summary>
    /// Message sent when a profiling session completes
    /// </summary>
    public struct ProfilerSessionCompletedMessage : IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks)
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type
        /// </summary>
        public ushort TypeCode => 1001;
        
        /// <summary>
        /// The tag that was being profiled
        /// </summary>
        public readonly ProfilerTag Tag;
        
        /// <summary>
        /// Duration of the profiling session in milliseconds
        /// </summary>
        public readonly double DurationMs;
        
        /// <summary>
        /// Creates a new session completed message
        /// </summary>
        public ProfilerSessionCompletedMessage(ProfilerTag tag, double durationMs)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            Tag = tag;
            DurationMs = durationMs;
        }
    }
    
    /// <summary>
    /// Message sent when profiling is started
    /// </summary>
    public struct ProfilingStartedMessage : IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks)
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type
        /// </summary>
        public ushort TypeCode => 1002;
        
        /// <summary>
        /// Creates a new profiling started message
        /// </summary>
        public ProfilingStartedMessage(Guid id)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
        }
    }
    
    /// <summary>
    /// Message sent when profiling is stopped
    /// </summary>
    public struct ProfilingStoppedMessage : IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks)
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type
        /// </summary>
        public ushort TypeCode => 1003;
        
        /// <summary>
        /// Creates a new profiling stopped message
        /// </summary>
        public ProfilingStoppedMessage(Guid id)
        {
            Id = id;
            TimestampTicks = DateTime.UtcNow.Ticks;
        }
    }
    
    /// <summary>
    /// Message sent when stats are reset
    /// </summary>
    public struct StatsResetMessage : IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks)
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type
        /// </summary>
        public ushort TypeCode => 1004;
        
        /// <summary>
        /// Creates a new stats reset message
        /// </summary>
        public StatsResetMessage(Guid id)
        {
            Id = id;
            TimestampTicks = DateTime.UtcNow.Ticks;
        }
    }
    
    /// <summary>
    /// Message sent when a metric alert is triggered
    /// </summary>
    public struct MetricAlertMessage : IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks)
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type
        /// </summary>
        public ushort TypeCode => 1005;
        
        /// <summary>
        /// The metric tag that triggered the alert
        /// </summary>
        public readonly ProfilerTag MetricTag;
        
        /// <summary>
        /// The value that triggered the alert
        /// </summary>
        public readonly double Value;
        
        /// <summary>
        /// The threshold that was exceeded
        /// </summary>
        public readonly double Threshold;
        
        /// <summary>
        /// The unit of the metric
        /// </summary>
        public readonly string Unit;
        
        /// <summary>
        /// Creates a new metric alert message
        /// </summary>
        public MetricAlertMessage(ProfilerTag metricTag, double value, double threshold, string unit)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            MetricTag = metricTag;
            Value = value;
            Threshold = threshold;
            Unit = unit;
        }
    }
    
    /// <summary>
    /// Message sent when a session alert is triggered
    /// </summary>
    public struct SessionAlertMessage : IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks)
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type
        /// </summary>
        public ushort TypeCode => 1006;
        
        /// <summary>
        /// The tag that was being profiled
        /// </summary>
        public readonly ProfilerTag Tag;
        
        /// <summary>
        /// Duration of the profiling session in milliseconds
        /// </summary>
        public readonly double DurationMs;
        
        /// <summary>
        /// The threshold that was exceeded
        /// </summary>
        public readonly double ThresholdMs;
        
        /// <summary>
        /// Creates a new session alert message
        /// </summary>
        public SessionAlertMessage(ProfilerTag tag, double durationMs, double thresholdMs)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            Tag = tag;
            DurationMs = durationMs;
            ThresholdMs = thresholdMs;
        }
        
        /// <summary>
        /// Creates a new session alert message from an event args
        /// </summary>
        public SessionAlertMessage(ProfilerSessionEventArgs args, double thresholdMs)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            Tag = args.Tag;
            DurationMs = args.DurationMs;
            ThresholdMs = thresholdMs;
        }
    }
}