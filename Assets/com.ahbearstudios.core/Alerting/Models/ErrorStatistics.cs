using System;
using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Error and exception statistics for the alert system.
    /// </summary>
    public readonly record struct ErrorStatistics
    {
        /// <summary>
        /// Gets the total number of errors encountered.
        /// </summary>
        public long TotalErrors { get; init; }

        /// <summary>
        /// Gets the number of channel delivery errors.
        /// </summary>
        public long ChannelErrors { get; init; }

        /// <summary>
        /// Gets the number of filter evaluation errors.
        /// </summary>
        public long FilterErrors { get; init; }

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
                ChannelErrors = ChannelErrors + other.ChannelErrors,
                FilterErrors = FilterErrors + other.FilterErrors,
                SerializationErrors = SerializationErrors + other.SerializationErrors,
                LastError = other.LastError ?? LastError,
                LastErrorMessage = other.LastErrorMessage.IsEmpty ? LastErrorMessage : other.LastErrorMessage
            };
        }
    }
}