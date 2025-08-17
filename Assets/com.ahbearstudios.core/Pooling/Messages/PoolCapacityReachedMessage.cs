using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Pooling.Models;

namespace AhBearStudios.Core.Pooling.Messages
{
    /// <summary>
    /// Message published when a pool reaches its capacity limits.
    /// Follows the IMessage pattern from CLAUDE.md guidelines.
    /// Used for monitoring pool health and triggering capacity management.
    /// </summary>
    public record struct PoolCapacityReachedMessage : IMessage
    {
        /// <summary>
        /// Gets the unique identifier for this message instance.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets the message type code for efficient routing and filtering.
        /// </summary>
        public ushort TypeCode => MessageTypeCodes.PoolCapacityReached;

        /// <summary>
        /// Gets the name of the pool that reached capacity.
        /// </summary>
        public FixedString64Bytes PoolName { get; init; }

        /// <summary>
        /// Gets the type name of the pooled objects.
        /// </summary>
        public FixedString64Bytes ObjectTypeName { get; init; }

        /// <summary>
        /// Gets the unique identifier of the pool.
        /// </summary>
        public Guid PoolId { get; init; }

        /// <summary>
        /// Gets the timestamp when capacity was reached (UTC ticks).
        /// </summary>
        public long TimestampTicks { get; init; }

        /// <summary>
        /// Gets the current capacity of the pool.
        /// </summary>
        public int CurrentCapacity { get; init; }

        /// <summary>
        /// Gets the maximum allowed capacity of the pool.
        /// </summary>
        public int MaxCapacity { get; init; }

        /// <summary>
        /// Gets the number of objects currently active.
        /// </summary>
        public int ActiveObjects { get; init; }

        /// <summary>
        /// Gets the capacity utilization percentage (0.0 to 1.0).
        /// </summary>
        public float UtilizationPercentage { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking related operations.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets the source that triggered the capacity check.
        /// </summary>
        public FixedString64Bytes Source { get; init; }

        /// <summary>
        /// Gets the severity level of the capacity issue.
        /// </summary>
        public CapacitySeverity Severity { get; init; }

        /// <summary>
        /// Gets additional context about the capacity situation.
        /// </summary>
        public FixedString128Bytes Context { get; init; }

        /// <summary>
        /// Gets the DateTime representation of the timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new PoolCapacityReachedMessage.
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="objectTypeName">Type name of pooled objects</param>
        /// <param name="poolId">Pool unique identifier</param>
        /// <param name="currentCapacity">Current pool capacity</param>
        /// <param name="maxCapacity">Maximum pool capacity</param>
        /// <param name="activeObjects">Number of active objects</param>
        /// <param name="severity">Severity of the capacity issue</param>
        /// <param name="context">Additional context about the situation</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="source">Source that triggered the check</param>
        /// <returns>New message instance</returns>
        public static PoolCapacityReachedMessage Create(
            FixedString64Bytes poolName,
            FixedString64Bytes objectTypeName,
            Guid poolId,
            int currentCapacity,
            int maxCapacity,
            int activeObjects,
            CapacitySeverity severity,
            FixedString128Bytes context = default,
            Guid correlationId = default,
            FixedString64Bytes source = default)
        {
            var utilizationPercentage = maxCapacity > 0 ? (float)currentCapacity / maxCapacity : 0f;

            return new PoolCapacityReachedMessage
            {
                Id = Guid.NewGuid(),
                PoolName = poolName,
                ObjectTypeName = objectTypeName,
                PoolId = poolId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                CurrentCapacity = currentCapacity,
                MaxCapacity = maxCapacity,
                ActiveObjects = activeObjects,
                UtilizationPercentage = utilizationPercentage,
                Severity = severity,
                Context = context.IsEmpty ? new FixedString128Bytes("Pool capacity limit reached") : context,
                CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
                Source = source.IsEmpty ? new FixedString64Bytes("PoolingService") : source
            };
        }
    }

    /// <summary>
    /// Severity levels for pool capacity issues.
    /// </summary>
    public enum CapacitySeverity : byte
    {
        /// <summary>
        /// Information about normal capacity usage.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Warning about approaching capacity limits.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Critical capacity situation requiring immediate attention.
        /// </summary>
        Critical = 2,

        /// <summary>
        /// Emergency situation - pool exhausted.
        /// </summary>
        Emergency = 3
    }
}