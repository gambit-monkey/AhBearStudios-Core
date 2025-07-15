using System.Collections.Generic;

namespace AhBearStudios.Core.Profiling.Models;

/// <summary>
/// Represents a snapshot of performance metrics at a specific point in time.
/// </summary>
public readonly struct MetricSnapshot
{
    /// <summary>
    /// Gets the timestamp when the metric was recorded.
    /// </summary>
    public readonly DateTime Timestamp;

    /// <summary>
    /// Gets the metric name.
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// Gets the metric value.
    /// </summary>
    public readonly double Value;

    /// <summary>
    /// Gets the unit of measurement.
    /// </summary>
    public readonly string Unit;

    /// <summary>
    /// Gets additional tags associated with the metric.
    /// </summary>
    public readonly IReadOnlyDictionary<string, string> Tags;

    /// <summary>
    /// Initializes a new instance of the MetricSnapshot struct.
    /// </summary>
    /// <param name="timestamp">The timestamp</param>
    /// <param name="name">The metric name</param>
    /// <param name="value">The metric value</param>
    /// <param name="unit">The unit of measurement</param>
    /// <param name="tags">Additional tags</param>
    public MetricSnapshot(DateTime timestamp, string name, double value, string unit, IReadOnlyDictionary<string, string> tags)
    {
        Timestamp = timestamp;
        Name = name;
        Value = value;
        Unit = unit;
        Tags = tags;
    }
}