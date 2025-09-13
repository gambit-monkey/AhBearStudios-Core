using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Profiling.Models
{
    /// <summary>
    /// Represents a snapshot of performance metrics at a specific point in time.
    /// Designed for Unity game development with zero-allocation patterns and high-performance profiling.
    /// </summary>
    /// <remarks>
    /// The MetricSnapshot struct is optimized for Unity's performance requirements:
    /// - Uses readonly fields for immutability and performance
    /// - Supports both Unity and external profiling systems
    /// - Includes correlation tracking for distributed performance analysis
    /// - Designed for minimal allocation impact on 60 FPS frame budget
    /// </remarks>
    public readonly struct MetricSnapshot : IEquatable<MetricSnapshot>
    {
        #region Public Properties

        /// <summary>
        /// Gets the unique identifier for this metric snapshot.
        /// Used for correlation tracking and debugging across profiling systems.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets the timestamp when the metric was recorded, in UTC ticks.
        /// Provides high precision timing for performance analysis.
        /// </summary>
        public long TimestampTicks { get; init; }

        /// <summary>
        /// Gets the profiler tag associated with this metric.
        /// Provides consistent naming and categorization across profiling operations.
        /// </summary>
        public ProfilerTag Tag { get; init; }

        /// <summary>
        /// Gets the metric name for human-readable identification.
        /// Used for logging, debugging, and external system integration.
        /// </summary>
        public FixedString64Bytes Name { get; init; }

        /// <summary>
        /// Gets the numeric value of the metric.
        /// Supports both integer and floating-point measurements.
        /// </summary>
        public double Value { get; init; }

        /// <summary>
        /// Gets the unit of measurement for this metric.
        /// Common values: "ms", "fps", "MB", "count", "percent".
        /// </summary>
        public FixedString32Bytes Unit { get; init; }

        /// <summary>
        /// Gets the source system or component that recorded this metric.
        /// Useful for filtering and categorizing metrics by origin.
        /// </summary>
        public FixedString64Bytes Source { get; init; }

        /// <summary>
        /// Gets the correlation ID linking this metric to related operations.
        /// Enables tracing performance impacts across distributed systems.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets additional tags associated with the metric for categorization.
        /// Enables flexible querying and filtering of metric data.
        /// </summary>
        public IReadOnlyDictionary<string, string> Tags { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the metric timestamp.
        /// Converts high-precision ticks to standard DateTime format.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets a value indicating whether this metric snapshot is valid.
        /// Checks for required fields and reasonable values.
        /// </summary>
        public bool IsValid => !Tag.IsEmpty && TimestampTicks > 0 && !double.IsNaN(Value) && !double.IsInfinity(Value);

        /// <summary>
        /// Gets a value indicating whether this metric represents a time-based measurement.
        /// Determined by checking if the unit contains time-related indicators.
        /// </summary>
        public bool IsTimeBased
        {
            get
            {
                var unitStr = Unit.ToString().ToLowerInvariant();
                return unitStr.Contains("ms") || unitStr.Contains("sec") || unitStr.Contains("min") || 
                       unitStr.Contains("hour") || unitStr.Contains("tick");
            }
        }

        /// <summary>
        /// Gets a value indicating whether this metric represents a performance issue.
        /// Time-based metrics over 16.67ms (60 FPS frame budget) are considered performance issues.
        /// </summary>
        public bool IsPerformanceIssue
        {
            get
            {
                if (!IsTimeBased) return false;
                
                var unitStr = Unit.ToString().ToLowerInvariant();
                if (unitStr.Contains("ms"))
                    return Value > 16.67; // 60 FPS frame budget
                
                if (unitStr.Contains("sec"))
                    return Value > 0.01667; // 60 FPS frame budget in seconds
                
                return false;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the MetricSnapshot struct with comprehensive metric data.
        /// </summary>
        /// <param name="id">Unique identifier for this snapshot</param>
        /// <param name="timestampTicks">Timestamp in UTC ticks</param>
        /// <param name="tag">Profiler tag for categorization</param>
        /// <param name="name">Human-readable metric name</param>
        /// <param name="value">Numeric metric value</param>
        /// <param name="unit">Unit of measurement</param>
        /// <param name="source">Source system or component</param>
        /// <param name="correlationId">Correlation ID for operation tracking</param>
        /// <param name="tags">Additional categorization tags</param>
        /// <exception cref="ArgumentException">Thrown when required parameters are invalid</exception>
        public MetricSnapshot(
            Guid id,
            long timestampTicks,
            ProfilerTag tag,
            FixedString64Bytes name,
            double value,
            FixedString32Bytes unit,
            FixedString64Bytes source,
            Guid correlationId,
            IReadOnlyDictionary<string, string> tags = null)
        {
            if (tag.IsEmpty)
                throw new ArgumentException("Profiler tag cannot be empty", nameof(tag));
            
            if (timestampTicks <= 0)
                throw new ArgumentException("Timestamp ticks must be positive", nameof(timestampTicks));
            
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("Value cannot be NaN or Infinity", nameof(value));

            Id = id;
            TimestampTicks = timestampTicks;
            Tag = tag;
            Name = name;
            Value = value;
            Unit = unit;
            Source = source;
            CorrelationId = correlationId;
            Tags = tags ?? new Dictionary<string, string>();
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new MetricSnapshot for a performance measurement with automatic ID generation.
        /// </summary>
        /// <param name="tag">Profiler tag for the measurement</param>
        /// <param name="value">Performance value (typically in milliseconds)</param>
        /// <param name="unit">Unit of measurement (defaults to "ms")</param>
        /// <param name="source">Source system (optional)</param>
        /// <param name="correlationId">Correlation ID (auto-generated if not provided)</param>
        /// <param name="tags">Additional tags (optional)</param>
        /// <returns>New MetricSnapshot instance for performance data</returns>
        public static MetricSnapshot CreatePerformanceSnapshot(
            ProfilerTag tag,
            double value,
            string unit = "ms",
            FixedString64Bytes source = default,
            Guid correlationId = default,
            IReadOnlyDictionary<string, string> tags = null)
        {
            var sourceStr = source.IsEmpty ? "ProfilerService" : source.ToString();
            var id = DeterministicIdGenerator.GenerateCoreId($"MetricSnapshot:{tag.Name}-{value}");
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("Performance", tag.Name.ToString())
                : correlationId;

            return new MetricSnapshot(
                id: id,
                timestampTicks: DateTime.UtcNow.Ticks,
                tag: tag,
                name: tag.Name,
                value: value,
                unit: unit?.Length <= 32 ? unit : unit?[..32] ?? "unknown",
                source: source.IsEmpty ? "ProfilerService" : source,
                correlationId: finalCorrelationId,
                tags: tags);
        }

        /// <summary>
        /// Creates a new MetricSnapshot for a custom metric with automatic ID generation.
        /// </summary>
        /// <param name="metricName">Name of the custom metric</param>
        /// <param name="value">Metric value</param>
        /// <param name="unit">Unit of measurement</param>
        /// <param name="source">Source system (optional)</param>
        /// <param name="correlationId">Correlation ID (auto-generated if not provided)</param>
        /// <param name="tags">Additional tags (optional)</param>
        /// <returns>New MetricSnapshot instance for custom metric data</returns>
        /// <exception cref="ArgumentException">Thrown when metric name is null or empty</exception>
        public static MetricSnapshot CreateCustomMetric(
            string metricName,
            double value,
            string unit = null,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            IReadOnlyDictionary<string, string> tags = null)
        {
            if (string.IsNullOrEmpty(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            var tag = new ProfilerTag(metricName);
            var sourceStr = source.IsEmpty ? "ProfilerService" : source.ToString();
            var id = DeterministicIdGenerator.GenerateCoreId($"MetricSnapshot:{metricName}-{value}");
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("CustomMetric", metricName)
                : correlationId;

            return new MetricSnapshot(
                id: id,
                timestampTicks: DateTime.UtcNow.Ticks,
                tag: tag,
                name: metricName?.Length <= 64 ? metricName : metricName?[..64] ?? "unknown",
                value: value,
                unit: unit?.Length <= 32 ? unit : unit?[..32] ?? "unknown",
                source: source.IsEmpty ? "ProfilerService" : source,
                correlationId: finalCorrelationId,
                tags: tags);
        }

        /// <summary>
        /// Creates a new MetricSnapshot for a counter increment/decrement operation.
        /// </summary>
        /// <param name="counterName">Name of the counter</param>
        /// <param name="increment">Increment value (can be negative for decrement)</param>
        /// <param name="source">Source system (optional)</param>
        /// <param name="correlationId">Correlation ID (auto-generated if not provided)</param>
        /// <param name="tags">Additional tags (optional)</param>
        /// <returns>New MetricSnapshot instance for counter operation</returns>
        /// <exception cref="ArgumentException">Thrown when counter name is null or empty</exception>
        public static MetricSnapshot CreateCounterSnapshot(
            string counterName,
            long increment,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            IReadOnlyDictionary<string, string> tags = null)
        {
            if (string.IsNullOrEmpty(counterName))
                throw new ArgumentException("Counter name cannot be null or empty", nameof(counterName));

            var tag = ProfilerTag.CreateSystemTag("Counter", counterName);
            var sourceStr = source.IsEmpty ? "ProfilerService" : source.ToString();
            var id = DeterministicIdGenerator.GenerateCoreId($"MetricSnapshot:{counterName}-counter-{increment}");
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("Counter", counterName)
                : correlationId;

            return new MetricSnapshot(
                id: id,
                timestampTicks: DateTime.UtcNow.Ticks,
                tag: tag,
                name: counterName?.Length <= 64 ? counterName : counterName?[..64] ?? "unknown",
                value: increment,
                unit: "count",
                source: source.IsEmpty ? "ProfilerService" : source,
                correlationId: finalCorrelationId,
                tags: tags);
        }

        #endregion

        #region IEquatable Implementation

        /// <summary>
        /// Determines whether the specified MetricSnapshot is equal to this instance.
        /// Equality is based on the snapshot ID.
        /// </summary>
        /// <param name="other">The MetricSnapshot to compare</param>
        /// <returns>True if the snapshots are equal, false otherwise</returns>
        public bool Equals(MetricSnapshot other)
        {
            return Id.Equals(other.Id);
        }

        /// <summary>
        /// Determines whether the specified object is equal to this MetricSnapshot.
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>True if the objects are equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            return obj is MetricSnapshot other && Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this MetricSnapshot.
        /// Based on the snapshot ID for consistent hashing.
        /// </summary>
        /// <returns>Hash code for this snapshot</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Determines whether two MetricSnapshot instances are equal.
        /// </summary>
        /// <param name="left">First MetricSnapshot to compare</param>
        /// <param name="right">Second MetricSnapshot to compare</param>
        /// <returns>True if the snapshots are equal, false otherwise</returns>
        public static bool operator ==(MetricSnapshot left, MetricSnapshot right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two MetricSnapshot instances are not equal.
        /// </summary>
        /// <param name="left">First MetricSnapshot to compare</param>
        /// <param name="right">Second MetricSnapshot to compare</param>
        /// <returns>True if the snapshots are not equal, false otherwise</returns>
        public static bool operator !=(MetricSnapshot left, MetricSnapshot right)
        {
            return !(left == right);
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this metric snapshot for debugging purposes.
        /// </summary>
        /// <returns>String representation including key metric information</returns>
        public override string ToString()
        {
            return $"MetricSnapshot [Tag={Tag.Name}, Value={Value:F2}{Unit}, Source={Source}, Time={Timestamp:HH:mm:ss.fff}]";
        }

        /// <summary>
        /// Returns a detailed string representation including all properties.
        /// Useful for comprehensive logging and debugging scenarios.
        /// </summary>
        /// <returns>Detailed string representation of the metric snapshot</returns>
        public string ToDetailedString()
        {
            return $"MetricSnapshot [Id={Id:D}, Tag={Tag.Name}, Name={Name}, Value={Value:F6}, " +
                   $"Unit={Unit}, Source={Source}, CorrelationId={CorrelationId:D}, " +
                   $"Timestamp={Timestamp:yyyy-MM-dd HH:mm:ss.fff} UTC, Valid={IsValid}, " +
                   $"TimeBased={IsTimeBased}, PerformanceIssue={IsPerformanceIssue}]";
        }

        #endregion
    }
}