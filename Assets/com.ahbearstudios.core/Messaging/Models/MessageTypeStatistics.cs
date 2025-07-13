namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Statistics for a specific message type.
/// </summary>
public sealed class MessageTypeStatistics
{
    /// <summary>
    /// Gets the number of successfully processed messages.
    /// </summary>
    public long ProcessedCount { get; private set; }

    /// <summary>
    /// Gets the number of failed messages.
    /// </summary>
    public long FailedCount { get; private set; }

    /// <summary>
    /// Gets the total processing time in milliseconds.
    /// </summary>
    public double TotalProcessingTime { get; private set; }

    /// <summary>
    /// Gets the average processing time in milliseconds.
    /// </summary>
    public double AverageProcessingTime => ProcessedCount > 0 ? TotalProcessingTime / ProcessedCount : 0.0;

    /// <summary>
    /// Gets the peak processing time in milliseconds.
    /// </summary>
    public double PeakProcessingTime { get; private set; }

    /// <summary>
    /// Gets the error rate for this message type.
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
    /// Initializes a new instance of MessageTypeStatistics.
    /// </summary>
    /// <param name="processedCount">Initial processed count</param>
    /// <param name="failedCount">Initial failed count</param>
    /// <param name="totalProcessingTime">Initial total processing time</param>
    /// <param name="peakProcessingTime">Initial peak processing time</param>
    public MessageTypeStatistics(long processedCount, long failedCount, double totalProcessingTime,
        double peakProcessingTime)
    {
        ProcessedCount = processedCount;
        FailedCount = failedCount;
        TotalProcessingTime = totalProcessingTime;
        PeakProcessingTime = peakProcessingTime;
    }

    /// <summary>
    /// Updates the statistics with a new processing result.
    /// </summary>
    /// <param name="success">Whether the processing was successful</param>
    /// <param name="processingTime">The processing time in milliseconds</param>
    /// <returns>Updated statistics instance</returns>
    public MessageTypeStatistics Update(bool success, double processingTime)
    {
        if (success)
        {
            ProcessedCount++;
            TotalProcessingTime += processingTime;
            if (processingTime > PeakProcessingTime)
                PeakProcessingTime = processingTime;
        }
        else
        {
            FailedCount++;
        }

        return this;
    }
}