using System;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Message published when a scheduled health check execution starts.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct HealthCheckScheduledExecutionStartedMessage : IMessage
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
        /// Gets the name of the health check being executed.
        /// </summary>
        public FixedString64Bytes HealthCheckName { get; init; }

        /// <summary>
        /// Gets the schedule name or identifier.
        /// </summary>
        public FixedString64Bytes ScheduleName { get; init; }

        /// <summary>
        /// Gets the execution interval in seconds.
        /// </summary>
        public double IntervalSeconds { get; init; }

        /// <summary>
        /// Gets the scheduled execution time (UTC ticks).
        /// </summary>
        public long ScheduledTimeTicks { get; init; }

        /// <summary>
        /// Gets the actual start time (UTC ticks).
        /// </summary>
        public long ActualStartTimeTicks { get; init; }

        /// <summary>
        /// Gets the delay from scheduled time in milliseconds.
        /// </summary>
        public double ScheduleDelayMs { get; init; }

        /// <summary>
        /// Gets whether this is a batch execution (multiple checks).
        /// </summary>
        public bool IsBatchExecution { get; init; }

        /// <summary>
        /// Gets the number of checks in the batch (1 if not batch).
        /// </summary>
        public int BatchSize { get; init; }

        /// <summary>
        /// Gets the execution attempt number.
        /// </summary>
        public int ExecutionNumber { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the scheduled execution time as DateTime.
        /// </summary>
        public DateTime ScheduledTime => new DateTime(ScheduledTimeTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the actual start time as DateTime.
        /// </summary>
        public DateTime ActualStartTime => new DateTime(ActualStartTimeTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new HealthCheckScheduledExecutionStartedMessage following CLAUDE.md patterns.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="scheduleName">Schedule name or identifier</param>
        /// <param name="intervalSeconds">Execution interval in seconds</param>
        /// <param name="scheduledTime">Scheduled execution time</param>
        /// <param name="actualStartTime">Actual start time</param>
        /// <param name="isBatchExecution">Whether this is a batch execution</param>
        /// <param name="batchSize">Number of checks in batch</param>
        /// <param name="executionNumber">Execution attempt number</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>New HealthCheckScheduledExecutionStartedMessage instance</returns>
        public static HealthCheckScheduledExecutionStartedMessage Create(
            string healthCheckName,
            string scheduleName = "Default",
            double intervalSeconds = 60,
            DateTime? scheduledTime = null,
            DateTime? actualStartTime = null,
            bool isBatchExecution = false,
            int batchSize = 1,
            int executionNumber = 1,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            // Input validation
            if (string.IsNullOrEmpty(healthCheckName))
                throw new ArgumentException("Health check name cannot be null or empty", nameof(healthCheckName));

            var scheduled = scheduledTime ?? DateTime.UtcNow;
            var actual = actualStartTime ?? DateTime.UtcNow;
            var delayMs = (actual - scheduled).TotalMilliseconds;

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "HealthCheckScheduler" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckScheduledExecutionStartedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("ScheduledExecution", $"{healthCheckName}-{executionNumber}")
                : correlationId;

            return new HealthCheckScheduledExecutionStartedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckScheduledExecutionStartedMessage,
                Source = source.IsEmpty ? "HealthCheckScheduler" : source,
                Priority = MessagePriority.Low,
                CorrelationId = finalCorrelationId,
                HealthCheckName = healthCheckName.Length <= 64 ? healthCheckName : healthCheckName[..64],
                ScheduleName = scheduleName?.Length <= 64 ? scheduleName : scheduleName?[..64] ?? "Default",
                IntervalSeconds = intervalSeconds,
                ScheduledTimeTicks = scheduled.Ticks,
                ActualStartTimeTicks = actual.Ticks,
                ScheduleDelayMs = delayMs,
                IsBatchExecution = isBatchExecution,
                BatchSize = batchSize,
                ExecutionNumber = executionNumber
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Scheduled execution started message string representation</returns>
        public override string ToString()
        {
            var batchInfo = IsBatchExecution ? $" (Batch: {BatchSize})" : "";
            var delayInfo = Math.Abs(ScheduleDelayMs) > 1 ? $" Delay: {ScheduleDelayMs:F1}ms" : "";
            return $"ScheduledExecutionStarted: {HealthCheckName} #{ExecutionNumber}{batchInfo} - Interval: {IntervalSeconds}s{delayInfo}";
        }

        #endregion
    }
}