using System.Collections.Generic;

namespace AhBearStudios.Core.Serialization.Models;

/// <summary>
/// Performance and usage statistics for serialization operations.
/// </summary>
public record SerializationStatistics
{
    /// <summary>
    /// Total number of serialization operations performed.
    /// </summary>
    public long TotalSerializations { get; init; }

    /// <summary>
    /// Total number of deserialization operations performed.
    /// </summary>
    public long TotalDeserializations { get; init; }

    /// <summary>
    /// Number of failed operations.
    /// </summary>
    public long FailedOperations { get; init; }

    /// <summary>
    /// Total bytes serialized.
    /// </summary>
    public long TotalBytesProcessed { get; init; }

    /// <summary>
    /// Average serialization time in milliseconds.
    /// </summary>
    public double AverageSerializationTimeMs { get; init; }

    /// <summary>
    /// Average deserialization time in milliseconds.
    /// </summary>
    public double AverageDeserializationTimeMs { get; init; }

    /// <summary>
    /// Peak memory usage during operations.
    /// </summary>
    public long PeakMemoryUsage { get; init; }

    /// <summary>
    /// Number of registered types.
    /// </summary>
    public int RegisteredTypeCount { get; init; }

    /// <summary>
    /// Buffer pool statistics.
    /// </summary>
    public BufferPoolStatistics BufferPoolStats { get; init; }

    /// <summary>
    /// Most frequently serialized types.
    /// </summary>
    public IReadOnlyDictionary<string, long> TypeUsageStats { get; init; } = 
        new Dictionary<string, long>();

    /// <summary>
    /// When statistics were last reset.
    /// </summary>
    public DateTime LastResetTime { get; init; } = DateTime.UtcNow;
}