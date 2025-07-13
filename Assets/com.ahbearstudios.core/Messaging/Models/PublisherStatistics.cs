namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Statistics specific to a message publisher.
/// </summary>
public readonly struct PublisherStatistics
{
    /// <summary>
    /// Gets the total number of messages published.
    /// </summary>
    public readonly long TotalPublished;

    /// <summary>
    /// Gets the total number of successful publications.
    /// </summary>
    public readonly long SuccessfulPublications;

    /// <summary>
    /// Gets the total number of failed publications.
    /// </summary>
    public readonly long FailedPublications;

    /// <summary>
    /// Gets the average time to publish a message in milliseconds.
    /// </summary>
    public readonly double AveragePublishTimeMs;

    /// <summary>
    /// Gets the current publish rate per second.
    /// </summary>
    public readonly double PublishRate;

    /// <summary>
    /// Gets the timestamp of the last published message.
    /// </summary>
    public readonly long LastPublishedTicks;

    /// <summary>
    /// Initializes a new instance of PublisherStatistics.
    /// </summary>
    /// <param name="totalPublished">Total messages published</param>
    /// <param name="successfulPublications">Successful publications</param>
    /// <param name="failedPublications">Failed publications</param>
    /// <param name="averagePublishTimeMs">Average publish time</param>
    /// <param name="publishRate">Current publish rate</param>
    /// <param name="lastPublishedTicks">Last published timestamp</param>
    public PublisherStatistics(
        long totalPublished,
        long successfulPublications,
        long failedPublications,
        double averagePublishTimeMs,
        double publishRate,
        long lastPublishedTicks)
    {
        TotalPublished = totalPublished;
        SuccessfulPublications = successfulPublications;
        FailedPublications = failedPublications;
        AveragePublishTimeMs = averagePublishTimeMs;
        PublishRate = publishRate;
        LastPublishedTicks = lastPublishedTicks;
    }

    /// <summary>
    /// Gets the success rate as a percentage (0.0 to 1.0).
    /// </summary>
    public double SuccessRate => TotalPublished > 0
        ? (double)SuccessfulPublications / TotalPublished
        : 1.0;

    /// <summary>
    /// Gets the DateTime representation of the last published timestamp.
    /// </summary>
    public DateTime LastPublished => new DateTime(LastPublishedTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates an empty statistics instance.
    /// </summary>
    /// <returns>Empty statistics</returns>
    public static PublisherStatistics Empty => new(0, 0, 0, 0, 0, 0);
}