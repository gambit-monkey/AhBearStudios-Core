using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Profiling.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when a profiler scope exceeds its performance threshold.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    /// <remarks>
    /// This message is published by ProfilerScope when the elapsed time exceeds the configured
    /// threshold for performance monitoring. It enables decoupled notification of performance
    /// issues without requiring direct event coupling between profiling components.
    /// 
    /// The message includes comprehensive profiling context for analysis and alerting systems.
    /// </remarks>
    public readonly record struct ProfilerThresholdExceededMessage : IMessage
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
        /// Gets the profiler tag that exceeded its threshold.
        /// </summary>
        public ProfilerTag Tag { get; init; }

        /// <summary>
        /// Gets the actual elapsed time that exceeded the threshold, in milliseconds.
        /// </summary>
        public double ElapsedMs { get; init; }

        /// <summary>
        /// Gets the configured threshold that was exceeded, in milliseconds.
        /// </summary>
        public double ThresholdMs { get; init; }

        /// <summary>
        /// Gets the profiler scope ID that generated this threshold violation.
        /// </summary>
        public Guid ScopeId { get; init; }

        /// <summary>
        /// Gets the unit of measurement (typically "ms" for milliseconds).
        /// </summary>
        public FixedString32Bytes Unit { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the threshold violation percentage (how much the threshold was exceeded).
        /// </summary>
        public double ViolationPercentage => ThresholdMs > 0 ? ((ElapsedMs - ThresholdMs) / ThresholdMs) * 100.0 : 0.0;

        /// <summary>
        /// Gets a value indicating whether this represents a severe threshold violation (>50% over threshold).
        /// </summary>
        public bool IsSevereViolation => ViolationPercentage > 50.0;

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new ProfilerThresholdExceededMessage with proper validation and defaults.
        /// </summary>
        /// <param name="tag">The profiler tag that exceeded its threshold</param>
        /// <param name="elapsedMs">Actual elapsed time in milliseconds</param>
        /// <param name="thresholdMs">Configured threshold in milliseconds</param>
        /// <param name="scopeId">ID of the profiler scope that generated this violation</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <param name="priority">Message priority level</param>
        /// <returns>New ProfilerThresholdExceededMessage instance</returns>
        /// <exception cref="ArgumentException">Thrown when tag is empty or values are invalid</exception>
        public static ProfilerThresholdExceededMessage Create(
            ProfilerTag tag,
            double elapsedMs,
            double thresholdMs,
            Guid scopeId,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            MessagePriority priority = MessagePriority.High)
        {
            // Input validation
            if (tag.IsEmpty)
                throw new ArgumentException("Profiler tag cannot be empty", nameof(tag));

            if (elapsedMs < 0.0)
                throw new ArgumentException("Elapsed time cannot be negative", nameof(elapsedMs));

            if (thresholdMs < 0.0)
                throw new ArgumentException("Threshold cannot be negative", nameof(thresholdMs));

            if (scopeId == Guid.Empty)
                throw new ArgumentException("Scope ID cannot be empty", nameof(scopeId));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "ProfilerScope" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("ProfilerThresholdExceededMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("ProfilerThreshold", tag.Name.ToString())
                : correlationId;

            return new ProfilerThresholdExceededMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.ProfilingThresholdExceededMessage,
                Source = source.IsEmpty ? "ProfilerScope" : source,
                Priority = priority,
                CorrelationId = finalCorrelationId,
                Tag = tag,
                ElapsedMs = elapsedMs,
                ThresholdMs = thresholdMs,
                ScopeId = scopeId,
                Unit = "ms"
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Threshold exceeded message string representation</returns>
        public override string ToString()
        {
            return $"ProfilerThresholdExceeded: {Tag.Name} took {ElapsedMs:F2}ms (threshold: {ThresholdMs:F2}ms, {ViolationPercentage:F1}% over)";
        }

        #endregion
    }
}