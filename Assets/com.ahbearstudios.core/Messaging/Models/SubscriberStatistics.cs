namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Statistics specific to a message subscriber.
/// </summary>
public readonly struct SubscriberStatistics
{
    /// <summary>
    /// Gets the total number of messages received.
    /// </summary>
    public readonly long TotalReceived;

    /// <summary>
    /// Gets the total number of messages successfully processed.
    /// </summary>
    public readonly long SuccessfullyProcessed;

    /// <summary>
    /// Gets the total number of messages that failed processing.
    /// </summary>
    public readonly long FailedProcessing;

    /// <summary>
    /// Gets the total number of messages filtered out.
    /// </summary>
    public readonly long FilteredOut;

    /// <summary>
    /// Gets the average processing time per message in milliseconds.
    /// </summary>
    public readonly double AverageProcessingTimeMs;

    /// <summary>
    /// Gets the current processing rate per second.
    /// </summary>
    public readonly double ProcessingRate;

    /// <summary>
    /// Gets the timestamp of the last processed message.
    /// </summary>
    public readonly long LastProcessedTicks;

    /// <summary>
    /// Gets the number of active subscriptions.
    /// </summary>
    public readonly int ActiveSubscriptions;

    /// <summary>
    /// Initializes a new instance of SubscriberStatistics.
    /// </summary>
    /// <param name="totalReceived">Total messages received</param>
    /// <param name="successfullyProcessed">Successfully processed messages</param>
    /// <param name="failedProcessing">Failed processing messages</param>
    /// <param name="filteredOut">Filtered out messages</param>
    /// <param name="averageProcessingTimeMs">Average processing time</param>
    /// <param name="processingRate">Current processing rate</param>
    /// <param name="lastProcessedTicks">Last processed timestamp</param>
    /// <param name="activeSubscriptions">Active subscriptions count</param>
    public SubscriberStatistics(
        long totalReceived,
        long successfullyProcessed,
        long failedProcessing,
        long filteredOut,
        double averageProcessingTimeMs,
        double processingRate,
        long lastProcessedTicks,
        int activeSubscriptions)
    {
        TotalReceived = totalReceived;
        SuccessfullyProcessed = successfullyProcessed;
        FailedProcessing = failedProcessing;
        FilteredOut = filteredOut;
        AverageProcessingTimeMs = averageProcessingTimeMs;
        ProcessingRate = processingRate;
        LastProcessedTicks = lastProcessedTicks;
        ActiveSubscriptions = activeSubscriptions;
    }

    /// <summary>
    /// Gets the success rate as a percentage (0.0 to 1.0).
    /// </summary>
    public double SuccessRate => TotalReceived > 0
        ? (double)SuccessfullyProcessed / TotalReceived
        : 1.0;

    /// <summary>
    /// Gets the DateTime representation of the last processed timestamp.
    /// </summary>
    public DateTime LastProcessed => new DateTime(LastProcessedTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates an empty statistics instance.
    /// </summary>
    /// <returns>Empty statistics</returns>
    public static SubscriberStatistics Empty => new(0, 0, 0, 0, 0, 0, 0, 0);
}