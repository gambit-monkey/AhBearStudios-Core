namespace AhBearStudios.Core.Serialization.Models;

/// <summary>
/// Buffer pool usage statistics.
/// </summary>
public record BufferPoolStatistics
{
    /// <summary>
    /// Number of buffers currently in the pool.
    /// </summary>
    public int BuffersInPool { get; init; }

    /// <summary>
    /// Number of buffers currently rented out.
    /// </summary>
    public int BuffersRented { get; init; }

    /// <summary>
    /// Total number of buffer requests.
    /// </summary>
    public long TotalRequests { get; init; }

    /// <summary>
    /// Number of requests that were satisfied from the pool.
    /// </summary>
    public long PoolHits { get; init; }

    /// <summary>
    /// Number of requests that required new buffer allocation.
    /// </summary>
    public long PoolMisses { get; init; }

    /// <summary>
    /// Pool hit ratio (0.0 to 1.0).
    /// </summary>
    public double HitRatio => TotalRequests > 0 ? (double)PoolHits / TotalRequests : 0.0;

    /// <summary>
    /// Total memory currently allocated by the pool.
    /// </summary>
    public long TotalPoolMemory { get; init; }
}