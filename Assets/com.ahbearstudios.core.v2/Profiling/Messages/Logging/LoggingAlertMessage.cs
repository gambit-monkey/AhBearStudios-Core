using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Attributes;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when a logging alert is triggered.
    /// </summary>
    public struct LoggingAlertMessage : IMessage
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
        /// Gets the profiler tag associated with the alert.
        /// </summary>
        public ProfilerTag Tag { get; }

        /// <summary>
        /// Gets the log level associated with the alert.
        /// </summary>
        public LogLevel LogLevel { get; }

        /// <summary>
        /// Gets the log tag associated with the alert.
        /// </summary>
        public string LogTag { get; }

        /// <summary>
        /// Gets the actual value that triggered the alert.
        /// </summary>
        public double ActualValue { get; }

        /// <summary>
        /// Gets the threshold value that was exceeded.
        /// </summary>
        public double ThresholdValue { get; }

        /// <summary>
        /// Gets the type of alert (LogLevel, Target, etc.).
        /// </summary>
        public string AlertType { get; }

        /// <summary>
        /// Creates a new logging alert message.
        /// </summary>
        /// <param name="tag">Profiler tag for the alert.</param>
        /// <param name="logLevel">Log level associated with the alert.</param>
        /// <param name="logTag">Log tag associated with the alert.</param>
        /// <param name="actualValue">Actual value that triggered the alert.</param>
        /// <param name="thresholdValue">Threshold value that was exceeded.</param>
        /// <param name="alertType">Type of alert.</param>
        public LoggingAlertMessage(
            ProfilerTag tag,
            LogLevel logLevel,
            string logTag,
            double actualValue,
            double thresholdValue,
            string alertType)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0x5002; // Unique type code for this message type
            Tag = tag;
            LogLevel = logLevel;
            LogTag = logTag ?? string.Empty;
            ActualValue = actualValue;
            ThresholdValue = thresholdValue;
            AlertType = alertType ?? "Unknown";
        }
    }
}