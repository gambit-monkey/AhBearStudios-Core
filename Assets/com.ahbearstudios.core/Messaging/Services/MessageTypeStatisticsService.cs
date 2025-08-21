using System;
using AhBearStudios.Core.Messaging.Models;
using Unity.Profiling;

namespace AhBearStudios.Core.Messaging.Services;

/// <summary>
/// Service for managing message type statistics updates.
/// Follows the Builder → Config → Factory → Service pattern.
/// </summary>
public interface IMessageTypeStatisticsService
{
    /// <summary>
    /// Updates statistics with a new processing result.
    /// </summary>
    /// <param name="current">The current statistics</param>
    /// <param name="success">Whether the processing was successful</param>
    /// <param name="processingTime">The processing time in milliseconds</param>
    /// <returns>Updated immutable statistics instance</returns>
    MessageTypeStatistics UpdateStatistics(MessageTypeStatistics current, bool success, double processingTime);

    /// <summary>
    /// Merges two statistics instances.
    /// </summary>
    /// <param name="first">The first statistics instance</param>
    /// <param name="second">The second statistics instance</param>
    /// <returns>Merged statistics instance</returns>
    MessageTypeStatistics MergeStatistics(MessageTypeStatistics first, MessageTypeStatistics second);

    /// <summary>
    /// Resets statistics to empty state.
    /// </summary>
    /// <returns>Empty statistics instance</returns>
    MessageTypeStatistics ResetStatistics();
}

/// <summary>
/// Implementation of message type statistics service.
/// Provides thread-safe, immutable statistics operations.
/// </summary>
public sealed class MessageTypeStatisticsService : IMessageTypeStatisticsService
{
    private readonly ProfilerMarker _updateMarker = new("MessageTypeStatisticsService.Update");
    private readonly ProfilerMarker _mergeMarker = new("MessageTypeStatisticsService.Merge");

    /// <summary>
    /// Updates statistics with a new processing result.
    /// Returns a new immutable instance with updated values.
    /// </summary>
    /// <param name="current">The current statistics</param>
    /// <param name="success">Whether the processing was successful</param>
    /// <param name="processingTime">The processing time in milliseconds</param>
    /// <returns>Updated immutable statistics instance</returns>
    public MessageTypeStatistics UpdateStatistics(MessageTypeStatistics current, bool success, double processingTime)
    {
        using (_updateMarker.Auto())
        {
            current ??= MessageTypeStatistics.Empty;

            if (success)
            {
                return current with
                {
                    ProcessedCount = current.ProcessedCount + 1,
                    TotalProcessingTime = current.TotalProcessingTime + Math.Max(0.0, processingTime),
                    PeakProcessingTime = Math.Max(current.PeakProcessingTime, Math.Max(0.0, processingTime)),
                    LastUpdated = DateTime.UtcNow
                };
            }
            else
            {
                return current with
                {
                    FailedCount = current.FailedCount + 1,
                    LastUpdated = DateTime.UtcNow
                };
            }
        }
    }

    /// <summary>
    /// Merges two statistics instances into a combined result.
    /// Useful for aggregating statistics from multiple sources.
    /// </summary>
    /// <param name="first">The first statistics instance</param>
    /// <param name="second">The second statistics instance</param>
    /// <returns>Merged statistics instance</returns>
    public MessageTypeStatistics MergeStatistics(MessageTypeStatistics first, MessageTypeStatistics second)
    {
        using (_mergeMarker.Auto())
        {
            first ??= MessageTypeStatistics.Empty;
            second ??= MessageTypeStatistics.Empty;

            return new MessageTypeStatistics(
                processedCount: first.ProcessedCount + second.ProcessedCount,
                failedCount: first.FailedCount + second.FailedCount,
                totalProcessingTime: first.TotalProcessingTime + second.TotalProcessingTime,
                peakProcessingTime: Math.Max(first.PeakProcessingTime, second.PeakProcessingTime),
                lastUpdated: DateTime.UtcNow
            );
        }
    }

    /// <summary>
    /// Resets statistics to empty state.
    /// </summary>
    /// <returns>Empty statistics instance</returns>
    public MessageTypeStatistics ResetStatistics()
    {
        return MessageTypeStatistics.Empty;
    }
}