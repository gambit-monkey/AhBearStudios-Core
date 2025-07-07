namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Statistics for a specific health check type.
/// Immutable record that tracks creation counts, timing, and failure rates.
/// </summary>
public readonly record struct TypeStatistics(
    string TypeName,
    long TotalCreated,
    long TotalFailed,
    double FastestCreationMs,
    double SlowestCreationMs,
    double TotalCreationTimeMs)
{
    /// <summary>
    /// Gets the total number of creation attempts for this type.
    /// </summary>
    public long TotalAttempted => TotalCreated + TotalFailed;

    /// <summary>
    /// Gets the success rate as a percentage (0-100).
    /// </summary>
    public double SuccessRate => TotalAttempted == 0 ? 0.0 : (double)TotalCreated / TotalAttempted * 100.0;

    /// <summary>
    /// Gets the failure rate as a percentage (0-100).
    /// </summary>
    public double FailureRate => TotalAttempted == 0 ? 0.0 : (double)TotalFailed / TotalAttempted * 100.0;

    /// <summary>
    /// Gets the average creation time in milliseconds.
    /// </summary>
    public double AverageCreationTimeMs => TotalCreated == 0 ? 0.0 : TotalCreationTimeMs / TotalCreated;

    /// <summary>
    /// Records a successful creation and returns updated statistics.
    /// </summary>
    /// <param name="creationTimeMs">The creation time in milliseconds.</param>
    /// <returns>Updated statistics including the new successful creation.</returns>
    public TypeStatistics RecordSuccess(double creationTimeMs)
    {
        var newFastest = TotalCreated == 0 ? creationTimeMs : Math.Min(FastestCreationMs, creationTimeMs);
        var newSlowest = TotalCreated == 0 ? creationTimeMs : Math.Max(SlowestCreationMs, creationTimeMs);
        
        return this with
        {
            TotalCreated = TotalCreated + 1,
            FastestCreationMs = newFastest,
            SlowestCreationMs = newSlowest,
            TotalCreationTimeMs = TotalCreationTimeMs + creationTimeMs
        };
    }

    /// <summary>
    /// Records a failed creation and returns updated statistics.
    /// </summary>
    /// <returns>Updated statistics including the new failed creation.</returns>
    public TypeStatistics RecordFailure()
    {
        return this with { TotalFailed = TotalFailed + 1 };
    }

    /// <summary>
    /// Gets a formatted string representation of the statistics.
    /// </summary>
    /// <returns>A human-readable summary of the type statistics.</returns>
    public override string ToString()
    {
        return $"{TypeName}: {TotalCreated} created, {TotalFailed} failed " +
               $"({SuccessRate:F1}% success), avg {AverageCreationTimeMs:F1}ms";
    }
}