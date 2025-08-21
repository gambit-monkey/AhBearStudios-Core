namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Immutable statistics for a specific message type.
/// Follows the Builder → Config → Factory → Service pattern for consistency.
/// </summary>
public sealed record MessageTypeStatistics
{
    /// <summary>
    /// Gets the number of successfully processed messages.
    /// </summary>
    public long ProcessedCount { get; init; }

    /// <summary>
    /// Gets the number of failed messages.
    /// </summary>
    public long FailedCount { get; init; }

    /// <summary>
    /// Gets the total processing time in milliseconds.
    /// </summary>
    public double TotalProcessingTime { get; init; }

    /// <summary>
    /// Gets the peak processing time in milliseconds.
    /// </summary>
    public double PeakProcessingTime { get; init; }

    /// <summary>
    /// Gets the timestamp when these statistics were last updated.
    /// </summary>
    public DateTime LastUpdated { get; init; }

    /// <summary>
    /// Gets the average processing time in milliseconds.
    /// </summary>
    public double AverageProcessingTime => ProcessedCount > 0 ? TotalProcessingTime / ProcessedCount : 0.0;

    /// <summary>
    /// Gets the error rate for this message type (0.0 to 1.0).
    /// </summary>
    public double ErrorRate
    {
        get
        {
            var total = ProcessedCount + FailedCount;
            return total > 0 ? (double)FailedCount / total : 0.0;
        }
    }

    /// <summary>
    /// Gets the success rate for this message type (0.0 to 1.0).
    /// </summary>
    public double SuccessRate
    {
        get
        {
            var total = ProcessedCount + FailedCount;
            return total > 0 ? (double)ProcessedCount / total : 1.0;
        }
    }

    /// <summary>
    /// Gets the total number of processed messages (successful + failed).
    /// </summary>
    public long TotalMessages => ProcessedCount + FailedCount;

    /// <summary>
    /// Initializes a new instance of MessageTypeStatistics.
    /// </summary>
    /// <param name="processedCount">The number of successfully processed messages</param>
    /// <param name="failedCount">The number of failed messages</param>
    /// <param name="totalProcessingTime">The total processing time in milliseconds</param>
    /// <param name="peakProcessingTime">The peak processing time in milliseconds</param>
    /// <param name="lastUpdated">The timestamp when statistics were last updated</param>
    public MessageTypeStatistics(
        long processedCount = 0,
        long failedCount = 0,
        double totalProcessingTime = 0.0,
        double peakProcessingTime = 0.0,
        DateTime lastUpdated = default)
    {
        ProcessedCount = Math.Max(0, processedCount);
        FailedCount = Math.Max(0, failedCount);
        TotalProcessingTime = Math.Max(0.0, totalProcessingTime);
        PeakProcessingTime = Math.Max(0.0, peakProcessingTime);
        LastUpdated = lastUpdated == default ? DateTime.UtcNow : lastUpdated;
    }

    /// <summary>
    /// Creates an empty statistics instance.
    /// </summary>
    /// <returns>Empty statistics</returns>
    public static MessageTypeStatistics Empty => new();
}