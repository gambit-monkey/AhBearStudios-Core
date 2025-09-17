using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Profiling.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when profiling data is recorded.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    /// <remarks>
    /// This message is published by the profiler service when performance data is recorded,
    /// replacing the direct event-based approach to maintain compliance with the messaging
    /// architecture guidelines.
    /// </remarks>
    public readonly record struct ProfilerDataRecordedMessage : IMessage
    {
        #region IMessage Implementation

        /// <summary>
        /// Gets the unique identifier for this message instance.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets the timestamp when this message was created, in UTC ticks.
        /// </summary>
        public long TimestampTicks { get; init; }

        /// <summary>
        /// Gets the message type code for efficient routing and filtering.
        /// </summary>
        public ushort TypeCode { get; init; }

        /// <summary>
        /// Gets the source system or component that created this message.
        /// </summary>
        public FixedString64Bytes Source { get; init; }

        /// <summary>
        /// Gets the priority level for message processing.
        /// </summary>
        public MessagePriority Priority { get; init; }

        /// <summary>
        /// Gets optional correlation ID for message tracing across systems.
        /// </summary>
        public Guid CorrelationId { get; init; }

        #endregion

        #region Message-Specific Properties

        /// <summary>
        /// Gets the profiler tag associated with the recorded data.
        /// </summary>
        public ProfilerTag Tag { get; init; }

        /// <summary>
        /// Gets the recorded performance value.
        /// </summary>
        public double Value { get; init; }

        /// <summary>
        /// Gets the unit of measurement for the recorded value.
        /// </summary>
        public FixedString32Bytes Unit { get; init; }

        /// <summary>
        /// Gets the type of profiling data recorded.
        /// </summary>
        public ProfilingDataType DataType { get; init; }

        /// <summary>
        /// Gets the optional scope ID if this data was recorded within a specific scope.
        /// </summary>
        public Guid ScopeId { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new ProfilerDataRecordedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="tag">The profiler tag associated with the data</param>
        /// <param name="value">The recorded performance value</param>
        /// <param name="unit">Unit of measurement (default: "ms")</param>
        /// <param name="dataType">Type of profiling data</param>
        /// <param name="scopeId">Optional scope ID</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <param name="priority">Message priority level</param>
        /// <returns>New ProfilerDataRecordedMessage instance</returns>
        /// <exception cref="ArgumentException">Thrown when tag is empty or value is invalid</exception>
        public static ProfilerDataRecordedMessage Create(
            ProfilerTag tag,
            double value,
            string unit = "ms",
            ProfilingDataType dataType = ProfilingDataType.Sample,
            Guid scopeId = default,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            MessagePriority priority = MessagePriority.Normal)
        {
            // Input validation
            if (tag.IsEmpty)
                throw new ArgumentException("Profiler tag cannot be empty", nameof(tag));

            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("Value must be a valid number", nameof(value));

            if (string.IsNullOrEmpty(unit))
                throw new ArgumentException("Unit cannot be null or empty", nameof(unit));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "ProfilerService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("ProfilerDataRecordedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("ProfilerData", tag.Name.ToString())
                : correlationId;

            return new ProfilerDataRecordedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.ProfilingDataRecordedMessage,
                Source = source.IsEmpty ? "ProfilerService" : source,
                Priority = priority,
                CorrelationId = finalCorrelationId,
                Tag = tag,
                Value = value,
                Unit = unit.Length <= 32 ? unit : unit[..32],
                DataType = dataType,
                ScopeId = scopeId
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Data recorded message string representation</returns>
        public override string ToString()
        {
            return $"ProfilerDataRecorded: {Tag.Name} = {Value:F2} {Unit} (Type: {DataType})";
        }

        #endregion
    }
}