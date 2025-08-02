namespace AhBearStudios.Core.Pooling.Models
{
    /// <summary>
    /// Statistics for network buffer pool usage.
    /// Provides comprehensive metrics for monitoring and performance analysis.
    /// </summary>
    public class NetworkBufferPoolStatistics
    {
        /// <summary>
        /// Number of times small buffers were requested.
        /// </summary>
        public long SmallBufferGets { get; init; }

        /// <summary>
        /// Number of times medium buffers were requested.
        /// </summary>
        public long MediumBufferGets { get; init; }

        /// <summary>
        /// Number of times large buffers were requested.
        /// </summary>
        public long LargeBufferGets { get; init; }

        /// <summary>
        /// Number of times compression buffers were requested.
        /// </summary>
        public long CompressionBufferGets { get; init; }

        /// <summary>
        /// Total number of buffer requests across all pools.
        /// </summary>
        public long TotalBufferGets { get; init; }

        /// <summary>
        /// Total number of buffers returned to pools.
        /// </summary>
        public long TotalBufferReturns { get; init; }

        /// <summary>
        /// Statistics for the small buffer pool.
        /// </summary>
        public PoolStatistics SmallBufferPoolStats { get; init; }

        /// <summary>
        /// Statistics for the medium buffer pool.
        /// </summary>
        public PoolStatistics MediumBufferPoolStats { get; init; }

        /// <summary>
        /// Statistics for the large buffer pool.
        /// </summary>
        public PoolStatistics LargeBufferPoolStats { get; init; }

        /// <summary>
        /// Statistics for the compression buffer pool.
        /// </summary>
        public PoolStatistics CompressionBufferPoolStats { get; init; }

        /// <summary>
        /// Calculated buffer return rate (returns / gets).
        /// Indicates how well buffers are being returned to pools.
        /// </summary>
        public double BufferReturnRate => TotalBufferGets > 0 ? (double)TotalBufferReturns / TotalBufferGets : 0.0;

        /// <summary>
        /// Usage rate of small buffers as percentage of total buffer requests.
        /// </summary>
        public double SmallBufferUsageRate => TotalBufferGets > 0 ? (double)SmallBufferGets / TotalBufferGets : 0.0;

        /// <summary>
        /// Usage rate of medium buffers as percentage of total buffer requests.
        /// </summary>
        public double MediumBufferUsageRate => TotalBufferGets > 0 ? (double)MediumBufferGets / TotalBufferGets : 0.0;

        /// <summary>
        /// Usage rate of large buffers as percentage of total buffer requests.
        /// </summary>
        public double LargeBufferUsageRate => TotalBufferGets > 0 ? (double)LargeBufferGets / TotalBufferGets : 0.0;

        /// <summary>
        /// Usage rate of compression buffers as percentage of total buffer requests.
        /// </summary>
        public double CompressionBufferUsageRate => TotalBufferGets > 0 ? (double)CompressionBufferGets / TotalBufferGets : 0.0;

        /// <summary>
        /// Gets the total memory usage across all buffer pools.
        /// Calculated based on buffer sizes and active counts.
        /// </summary>
        public long TotalMemoryUsage =>
            (SmallBufferPoolStats?.ActiveCount ?? 0) * 1024 +  // 1KB per small buffer
            (MediumBufferPoolStats?.ActiveCount ?? 0) * 16384 + // 16KB per medium buffer
            (LargeBufferPoolStats?.ActiveCount ?? 0) * 65536 +  // 64KB per large buffer
            (CompressionBufferPoolStats?.ActiveCount ?? 0) * 4096; // 4KB per compression buffer

        /// <summary>
        /// Gets the total number of active objects across all buffer pools.
        /// </summary>
        public int TotalActiveObjects =>
            (SmallBufferPoolStats?.ActiveCount ?? 0) +
            (MediumBufferPoolStats?.ActiveCount ?? 0) +
            (LargeBufferPoolStats?.ActiveCount ?? 0) +
            (CompressionBufferPoolStats?.ActiveCount ?? 0);

        /// <summary>
        /// Gets the efficiency score (0.0 to 1.0) based on buffer return rate and pool utilization.
        /// </summary>
        public double EfficiencyScore
        {
            get
            {
                var returnRateScore = BufferReturnRate;
                var utilizationScore = TotalActiveObjects > 0 ? 1.0 : 0.5; // Give partial credit if pools exist but not used
                return (returnRateScore + utilizationScore) / 2.0;
            }
        }
    }
}