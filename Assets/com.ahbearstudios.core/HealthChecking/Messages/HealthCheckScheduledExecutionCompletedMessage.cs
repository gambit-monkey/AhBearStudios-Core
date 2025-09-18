using System;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Message published when a scheduled health check execution completes.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct HealthCheckScheduledExecutionCompletedMessage : IMessage
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
        /// Gets the name of the health check that was executed.
        /// </summary>
        public FixedString64Bytes HealthCheckName { get; init; }

        /// <summary>
        /// Gets the schedule name or identifier.
        /// </summary>
        public FixedString64Bytes ScheduleName { get; init; }

        /// <summary>
        /// Gets the resulting health status.
        /// </summary>
        public HealthStatus ResultStatus { get; init; }

        /// <summary>
        /// Gets the execution duration in milliseconds.
        /// </summary>
        public double ExecutionDurationMs { get; init; }

        /// <summary>
        /// Gets whether the execution succeeded without errors.
        /// </summary>
        public bool IsSuccessful { get; init; }

        /// <summary>
        /// Gets any error message if the execution failed.
        /// </summary>
        public FixedString512Bytes ErrorMessage { get; init; }

        /// <summary>
        /// Gets whether this was a batch execution.
        /// </summary>
        public bool IsBatchExecution { get; init; }

        /// <summary>
        /// Gets the number of successful checks in batch.
        /// </summary>
        public int SuccessfulChecks { get; init; }

        /// <summary>
        /// Gets the total number of checks in batch.
        /// </summary>
        public int TotalChecks { get; init; }

        /// <summary>
        /// Gets the next scheduled execution time (UTC ticks).
        /// </summary>
        public long NextScheduledTimeTicks { get; init; }

        /// <summary>
        /// Gets the execution number.
        /// </summary>
        public int ExecutionNumber { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the next scheduled execution time as DateTime.
        /// </summary>
        public DateTime NextScheduledTime => NextScheduledTimeTicks > 0
            ? new DateTime(NextScheduledTimeTicks, DateTimeKind.Utc)
            : DateTime.MinValue;

        /// <summary>
        /// Gets the success rate for batch executions.
        /// </summary>
        public double BatchSuccessRate => TotalChecks > 0
            ? (double)SuccessfulChecks / TotalChecks
            : 0.0;

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new HealthCheckScheduledExecutionCompletedMessage following CLAUDE.md patterns.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="scheduleName">Schedule name or identifier</param>
        /// <param name="resultStatus">Resulting health status</param>
        /// <param name="executionDurationMs">Execution duration in milliseconds</param>
        /// <param name="isSuccessful">Whether execution succeeded</param>
        /// <param name="errorMessage">Error message if failed</param>
        /// <param name="isBatchExecution">Whether this was a batch execution</param>
        /// <param name="successfulChecks">Number of successful checks in batch</param>
        /// <param name="totalChecks">Total number of checks in batch</param>
        /// <param name="nextScheduledTime">Next scheduled execution time</param>
        /// <param name="executionNumber">Execution number</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>New HealthCheckScheduledExecutionCompletedMessage instance</returns>
        public static HealthCheckScheduledExecutionCompletedMessage Create(
            string healthCheckName,
            string scheduleName = "Default",
            HealthStatus resultStatus = HealthStatus.Healthy,
            double executionDurationMs = 0,
            bool isSuccessful = true,
            string errorMessage = null,
            bool isBatchExecution = false,
            int successfulChecks = 1,
            int totalChecks = 1,
            DateTime? nextScheduledTime = null,
            int executionNumber = 1,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            // Input validation
            if (string.IsNullOrEmpty(healthCheckName))
                throw new ArgumentException("Health check name cannot be null or empty", nameof(healthCheckName));

            // Determine priority based on result
            var priority = !isSuccessful || resultStatus == HealthStatus.Unhealthy
                ? MessagePriority.High
                : MessagePriority.Low;

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "HealthCheckScheduler" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckScheduledExecutionCompletedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("ScheduledExecutionComplete", $"{healthCheckName}-{executionNumber}")
                : correlationId;

            return new HealthCheckScheduledExecutionCompletedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckScheduledExecutionCompletedMessage,
                Source = source.IsEmpty ? "HealthCheckScheduler" : source,
                Priority = priority,
                CorrelationId = finalCorrelationId,
                HealthCheckName = healthCheckName.Length <= 64 ? healthCheckName : healthCheckName[..64],
                ScheduleName = scheduleName?.Length <= 64 ? scheduleName : scheduleName?[..64] ?? "Default",
                ResultStatus = resultStatus,
                ExecutionDurationMs = executionDurationMs,
                IsSuccessful = isSuccessful,
                ErrorMessage = errorMessage?.Length <= 512 ? errorMessage : errorMessage?[..512] ?? string.Empty,
                IsBatchExecution = isBatchExecution,
                SuccessfulChecks = successfulChecks,
                TotalChecks = totalChecks,
                NextScheduledTimeTicks = nextScheduledTime?.Ticks ?? 0,
                ExecutionNumber = executionNumber
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Scheduled execution completed message string representation</returns>
        public override string ToString()
        {
            var batchInfo = IsBatchExecution ? $" (Batch: {SuccessfulChecks}/{TotalChecks})" : "";
            var statusText = IsSuccessful ? $"Status: {ResultStatus}" : $"Failed: {ErrorMessage}";
            return $"ScheduledExecutionCompleted: {HealthCheckName} #{ExecutionNumber}{batchInfo} - {statusText} ({ExecutionDurationMs:F1}ms)";
        }

        #endregion
    }
}