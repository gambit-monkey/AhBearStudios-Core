using System;

namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Statistics for message registry performance and usage.
/// Immutable struct for thread-safe operations and performance.
/// </summary>
public readonly record struct MessageRegistryStatistics
{
    /// <summary>
    /// Gets the total number of registrations performed.
    /// </summary>
    public long TotalRegistrations { get; init; }

    /// <summary>
    /// Gets the total number of lookups performed.
    /// </summary>
    public long TotalLookups { get; init; }

    /// <summary>
    /// Gets the number of cache hits.
    /// </summary>
    public long CacheHits { get; init; }

    /// <summary>
    /// Gets the number of cache misses.
    /// </summary>
    public long CacheMisses { get; init; }

    /// <summary>
    /// Gets the cache hit rate as a percentage (0.0 to 1.0).
    /// </summary>
    public double CacheHitRate { get; init; }

    /// <summary>
    /// Gets the current lookup rate per second.
    /// </summary>
    public double LookupsPerSecond { get; init; }

    /// <summary>
    /// Gets the number of currently registered types.
    /// </summary>
    public int RegisteredTypeCount { get; init; }

    /// <summary>
    /// Gets the current cache size.
    /// </summary>
    public int CacheSize { get; init; }

    /// <summary>
    /// Gets the timestamp when statistics were collected.
    /// </summary>
    public long TimestampTicks { get; init; }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Gets the success rate for lookups (0.0 to 1.0).
    /// </summary>
    public double SuccessRate => TotalLookups > 0 ? CacheHitRate : 1.0;

    /// <summary>
    /// Gets the total cache operations performed.
    /// </summary>
    public long TotalCacheOperations => CacheHits + CacheMisses;

    /// <summary>
    /// Initializes a new instance of MessageRegistryStatistics.
    /// </summary>
    /// <param name="totalRegistrations">Total registrations performed</param>
    /// <param name="totalLookups">Total lookups performed</param>
    /// <param name="cacheHits">Number of cache hits</param>
    /// <param name="cacheMisses">Number of cache misses</param>
    /// <param name="cacheHitRate">Cache hit rate (0.0 to 1.0)</param>
    /// <param name="lookupsPerSecond">Current lookup rate per second</param>
    /// <param name="registeredTypeCount">Number of registered types</param>
    /// <param name="cacheSize">Current cache size</param>
    /// <param name="timestampTicks">Timestamp when collected</param>
    public MessageRegistryStatistics(
        long totalRegistrations,
        long totalLookups,
        long cacheHits,
        long cacheMisses,
        double cacheHitRate,
        double lookupsPerSecond,
        int registeredTypeCount,
        int cacheSize,
        long timestampTicks)
    {
        TotalRegistrations = Math.Max(0, totalRegistrations);
        TotalLookups = Math.Max(0, totalLookups);
        CacheHits = Math.Max(0, cacheHits);
        CacheMisses = Math.Max(0, cacheMisses);
        CacheHitRate = Math.Clamp(cacheHitRate, 0.0, 1.0);
        LookupsPerSecond = Math.Max(0.0, lookupsPerSecond);
        RegisteredTypeCount = Math.Max(0, registeredTypeCount);
        CacheSize = Math.Max(0, cacheSize);
        TimestampTicks = timestampTicks > 0 ? timestampTicks : DateTime.UtcNow.Ticks;
    }

    /// <summary>
    /// Gets an empty statistics instance.
    /// </summary>
    public static MessageRegistryStatistics Empty => new(0, 0, 0, 0, 0, 0, 0, 0, DateTime.UtcNow.Ticks);
}