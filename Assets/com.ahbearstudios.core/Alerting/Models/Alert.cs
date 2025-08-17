using System;
using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Immutable alert data structure using Unity.Collections for high-performance, zero-allocation patterns.
    /// Serialization is handled through ISerializationService for efficient persistence and network transmission.
    /// Designed for Unity game development with Job System and Burst compatibility.
    /// Lifecycle management is handled by IPoolingService, not the Alert itself.
    /// </summary>
    public sealed record Alert : IDisposable
    {
        /// <summary>
        /// Unique identifier for this alert instance.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Alert message content using FixedString for zero allocation.
        /// </summary>
        public FixedString512Bytes Message { get; init; }

        /// <summary>
        /// Severity level of this alert.
        /// </summary>
        public AlertSeverity Severity { get; init; }

        /// <summary>
        /// Source system or component that raised this alert.
        /// </summary>
        public FixedString64Bytes Source { get; init; }

        /// <summary>
        /// Optional tag for alert categorization and filtering.
        /// </summary>
        public FixedString32Bytes Tag { get; init; }

        /// <summary>
        /// Timestamp when this alert was created (UTC ticks).
        /// </summary>
        public long TimestampTicks { get; init; } = DateTime.UtcNow.Ticks;

        /// <summary>
        /// Correlation ID for tracking alerts across system boundaries.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Operation ID for linking alerts to specific operations.
        /// </summary>
        public Guid OperationId { get; init; }

        /// <summary>
        /// Optional contextual information for this alert.
        /// </summary>
        public AlertContext Context { get; init; }

        /// <summary>
        /// Current state of this alert.
        /// </summary>
        public AlertState State { get; init; } = AlertState.Active;

        /// <summary>
        /// Timestamp when this alert was acknowledged (UTC ticks), null if not acknowledged.
        /// </summary>
        public long? AcknowledgedTimestampTicks { get; init; }

        /// <summary>
        /// Timestamp when this alert was resolved (UTC ticks), null if not resolved.
        /// </summary>
        public long? ResolvedTimestampTicks { get; init; }

        /// <summary>
        /// User or system that acknowledged this alert.
        /// </summary>
        public FixedString64Bytes AcknowledgedBy { get; init; }

        /// <summary>
        /// User or system that resolved this alert.
        /// </summary>
        public FixedString64Bytes ResolvedBy { get; init; }

        /// <summary>
        /// Number of times this alert has been raised (for duplicate suppression).
        /// </summary>
        public int Count { get; init; } = 1;

        /// <summary>
        /// Gets the DateTime representation of the timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the DateTime representation of acknowledged timestamp, null if not acknowledged.
        /// </summary>
        public DateTime? AcknowledgedTimestamp => AcknowledgedTimestampTicks.HasValue 
            ? new DateTime(AcknowledgedTimestampTicks.Value, DateTimeKind.Utc) 
            : null;

        /// <summary>
        /// Gets the DateTime representation of resolved timestamp, null if not resolved.
        /// </summary>
        public DateTime? ResolvedTimestamp => ResolvedTimestampTicks.HasValue 
            ? new DateTime(ResolvedTimestampTicks.Value, DateTimeKind.Utc) 
            : null;

        /// <summary>
        /// Gets whether this alert is currently active.
        /// </summary>
        public bool IsActive => State == AlertState.Active;

        /// <summary>
        /// Gets whether this alert has been acknowledged.
        /// </summary>
        public bool IsAcknowledged => State == AlertState.Acknowledged || State == AlertState.Resolved;

        /// <summary>
        /// Gets whether this alert has been resolved.
        /// </summary>
        public bool IsResolved => State == AlertState.Resolved;

        /// <summary>
        /// Gets whether this alert has a critical issue requiring immediate attention.
        /// </summary>
        public bool HasCriticalIssue()
        {
            return (Severity == AlertSeverity.Critical || Severity == AlertSeverity.Emergency) && 
                   State == AlertState.Active;
        }


        /// <summary>
        /// Creates a new alert with the specified parameters.
        /// </summary>
        /// <param name="message">Alert message</param>
        /// <param name="severity">Alert severity</param>
        /// <param name="source">Source system</param>
        /// <param name="tag">Optional tag</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <param name="operationId">Operation ID</param>
        /// <param name="context">Optional context</param>
        /// <returns>New alert instance</returns>
        public static Alert Create(
            string message,
            AlertSeverity severity,
            FixedString64Bytes source,
            FixedString32Bytes tag = default,
            Guid correlationId = default,
            Guid operationId = default,
            AlertContext context = default)
        {
            return new Alert
            {
                Id = Guid.NewGuid(),
                Message = message.Length <= 512 ? message : message[..512],
                Severity = severity,
                Source = source,
                Tag = tag,
                TimestampTicks = DateTime.UtcNow.Ticks,
                CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
                OperationId = operationId,
                Context = context,
                State = AlertState.Active
            };
        }

        /// <summary>
        /// Creates a new alert using Unity.Collections types for Burst compatibility.
        /// </summary>
        /// <param name="message">Alert message</param>
        /// <param name="severity">Alert severity</param>
        /// <param name="source">Source system</param>
        /// <param name="tag">Optional tag</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <param name="operationId">Operation ID</param>
        /// <param name="context">Optional context</param>
        /// <returns>New alert instance</returns>
        public static Alert Create(
            FixedString512Bytes message,
            AlertSeverity severity,
            FixedString64Bytes source,
            FixedString32Bytes tag = default,
            Guid correlationId = default,
            Guid operationId = default,
            AlertContext context = default)
        {
            return new Alert
            {
                Id = Guid.NewGuid(),
                Message = message,
                Severity = severity,
                Source = source,
                Tag = tag,
                TimestampTicks = DateTime.UtcNow.Ticks,
                CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
                OperationId = operationId,
                Context = context,
                State = AlertState.Active
            };
        }

        /// <summary>
        /// Creates a copy of this alert with acknowledgment information.
        /// </summary>
        /// <param name="acknowledgedBy">User or system acknowledging the alert</param>
        /// <returns>Acknowledged alert copy</returns>
        public Alert Acknowledge(FixedString64Bytes acknowledgedBy)
        {
            return this with
            {
                State = AlertState.Acknowledged,
                AcknowledgedTimestampTicks = DateTime.UtcNow.Ticks,
                AcknowledgedBy = acknowledgedBy
            };
        }

        /// <summary>
        /// Creates a copy of this alert with resolution information.
        /// </summary>
        /// <param name="resolvedBy">User or system resolving the alert</param>
        /// <returns>Resolved alert copy</returns>
        public Alert Resolve(FixedString64Bytes resolvedBy)
        {
            return this with
            {
                State = AlertState.Resolved,
                ResolvedTimestampTicks = DateTime.UtcNow.Ticks,
                ResolvedBy = resolvedBy,
                AcknowledgedTimestampTicks = AcknowledgedTimestampTicks ?? DateTime.UtcNow.Ticks,
                AcknowledgedBy = AcknowledgedBy.IsEmpty ? resolvedBy : AcknowledgedBy
            };
        }

        /// <summary>
        /// Creates a copy of this alert with incremented count for duplicate suppression.
        /// </summary>
        /// <returns>Alert copy with incremented count</returns>
        public Alert IncrementCount()
        {
            return this with { Count = Count + 1 };
        }


        /// <summary>
        /// Validates that the alert is in a valid state.
        /// Used by pooling containers for validation.
        /// </summary>
        /// <returns>True if the alert has valid required data</returns>
        public bool IsValid()
        {
            return !Message.IsEmpty && 
                   !Source.IsEmpty && 
                   Id != Guid.Empty;
        }

        /// <summary>
        /// Gets the estimated memory usage of this alert in bytes.
        /// Used by pooling system for memory management.
        /// </summary>
        /// <returns>Estimated memory footprint in bytes</returns>
        public long GetEstimatedMemoryUsage()
        {
            long size = 0;
            size += 16 * 2; // Two GUIDs (Id, CorrelationId)
            size += 512; // Message FixedString512Bytes
            size += 64; // Source FixedString64Bytes
            size += 32; // Tag FixedString32Bytes
            size += 8 * 3; // Three longs (timestamps)
            size += 64 * 2; // AcknowledgedBy, ResolvedBy
            size += 4; // Count int
            size += 1; // State byte
            size += Context != null ? 1024 : 0; // Estimate for context
            return size;
        }

        /// <summary>
        /// Disposes resources associated with this alert.
        /// </summary>
        public void Dispose()
        {
            Context?.Dispose();
        }

        /// <summary>
        /// Returns a string representation of this alert for debugging.
        /// </summary>
        /// <returns>Alert string representation</returns>
        public override string ToString()
        {
            return $"[{Severity}] {Source}: {Message} ({State}) - {Timestamp:yyyy-MM-dd HH:mm:ss UTC}";
        }
    }

    /// <summary>
    /// Alert state enumeration.
    /// </summary>
    public enum AlertState : byte
    {
        /// <summary>
        /// Alert is active and unacknowledged.
        /// </summary>
        Active = 0,

        /// <summary>
        /// Alert has been acknowledged but not resolved.
        /// </summary>
        Acknowledged = 1,

        /// <summary>
        /// Alert has been resolved and is no longer active.
        /// </summary>
        Resolved = 2,

        /// <summary>
        /// Alert has been suppressed by filtering rules.
        /// </summary>
        Suppressed = 3
    }
}