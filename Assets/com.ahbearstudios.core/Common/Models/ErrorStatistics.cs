using System;
using Unity.Collections;

namespace AhBearStudios.Core.Common.Models;

/// <summary>
/// Error and exception statistics for system monitoring.
/// Provides comprehensive error tracking across multiple system components.
/// </summary>
public readonly record struct ErrorStatistics
{
    /// <summary>
    /// Gets the total number of errors encountered.
    /// </summary>
    public long TotalErrors { get; init; }

    /// <summary>
    /// Gets the number of delivery errors.
    /// </summary>
    public long DeliveryErrors { get; init; }

    /// <summary>
    /// Gets the number of processing errors.
    /// </summary>
    public long ProcessingErrors { get; init; }

    /// <summary>
    /// Gets the number of serialization errors.
    /// </summary>
    public long SerializationErrors { get; init; }

    /// <summary>
    /// Gets the last error timestamp.
    /// </summary>
    public DateTime? LastError { get; init; }

    /// <summary>
    /// Gets the most recent error message.
    /// </summary>
    public FixedString512Bytes LastErrorMessage { get; init; }

    /// <summary>
    /// Gets the error rate (errors per 1000 operations).
    /// </summary>
    public double ErrorRate => TotalErrors > 0 ? TotalErrors / 1000.0 : 0;

    /// <summary>
    /// Creates empty error statistics.
    /// </summary>
    public static ErrorStatistics Empty => new();

    /// <summary>
    /// Merges with other error statistics.
    /// </summary>
    /// <param name="other">Other statistics to merge</param>
    /// <returns>Merged statistics</returns>
    public ErrorStatistics Merge(ErrorStatistics other)
    {
        return new ErrorStatistics
        {
            TotalErrors = TotalErrors + other.TotalErrors,
            DeliveryErrors = DeliveryErrors + other.DeliveryErrors,
            ProcessingErrors = ProcessingErrors + other.ProcessingErrors,
            SerializationErrors = SerializationErrors + other.SerializationErrors,
            LastError = other.LastError ?? LastError,
            LastErrorMessage = other.LastErrorMessage.IsEmpty ? LastErrorMessage : other.LastErrorMessage
        };
    }
}