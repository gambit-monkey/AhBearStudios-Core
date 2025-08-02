using System;

namespace AhBearStudios.Core.Pooling.Strategies.Models
{
    /// <summary>
    /// Network-specific metrics for pooling strategies.
    /// Provides insights into network buffer usage patterns and performance characteristics.
    /// </summary>
    public sealed class NetworkPoolingMetrics
    {
        /// <summary>
        /// Total number of network packets processed.
        /// </summary>
        public long PacketsProcessed { get; init; }

        /// <summary>
        /// Total bytes processed through the buffer pools.
        /// </summary>
        public long BytesProcessed { get; init; }

        /// <summary>
        /// Average bytes per packet processed.
        /// </summary>
        public double AverageBytesPerPacket => PacketsProcessed > 0 ? (double)BytesProcessed / PacketsProcessed : 0.0;

        /// <summary>
        /// Peak bytes per second throughput observed.
        /// </summary>
        public long PeakBytesPerSecond { get; init; }

        /// <summary>
        /// Current bytes per second throughput.
        /// </summary>
        public long CurrentBytesPerSecond { get; init; }

        /// <summary>
        /// Number of network spikes detected (sudden traffic increases).
        /// </summary>
        public int NetworkSpikesDetected { get; init; }

        /// <summary>
        /// Average network latency observed (if available).
        /// </summary>
        public TimeSpan AverageLatency { get; init; }

        /// <summary>
        /// Peak latency observed.
        /// </summary>
        public TimeSpan PeakLatency { get; init; }

        /// <summary>
        /// Number of buffer allocations that were triggered by network spikes.
        /// </summary>
        public long SpikeTriggeredAllocations { get; init; }

        /// <summary>
        /// Number of buffers that were pre-allocated for anticipated network load.
        /// </summary>
        public long PreemptiveAllocations { get; init; }

        /// <summary>
        /// Number of times buffers were exhausted during high network load.
        /// </summary>
        public int BufferExhaustionEvents { get; init; }

        /// <summary>
        /// Number of compression operations performed.
        /// </summary>
        public long CompressionOperations { get; init; }

        /// <summary>
        /// Total bytes saved through compression.
        /// </summary>
        public long CompressionBytesSaved { get; init; }

        /// <summary>
        /// Average compression ratio achieved.
        /// </summary>
        public double AverageCompressionRatio { get; init; }

        /// <summary>
        /// Number of serialization operations performed.
        /// </summary>
        public long SerializationOperations { get; init; }

        /// <summary>
        /// Number of deserialization operations performed.
        /// </summary>
        public long DeserializationOperations { get; init; }

        /// <summary>
        /// Average time taken for serialization operations.
        /// </summary>
        public TimeSpan AverageSerializationTime { get; init; }

        /// <summary>
        /// Average time taken for deserialization operations.
        /// </summary>
        public TimeSpan AverageDeserializationTime { get; init; }

        /// <summary>
        /// Number of FishNet-specific operations processed.
        /// </summary>
        public long FishNetOperations { get; init; }

        /// <summary>
        /// Number of MemoryPack-specific operations processed.
        /// </summary>
        public long MemoryPackOperations { get; init; }

        /// <summary>
        /// Peak concurrent network connections observed.
        /// </summary>
        public int PeakConcurrentConnections { get; init; }

        /// <summary>
        /// Current number of active network connections.
        /// </summary>
        public int CurrentActiveConnections { get; init; }

        /// <summary>
        /// Timestamp when these metrics were captured.
        /// </summary>
        public DateTime CapturedAt { get; init; }

        /// <summary>
        /// Duration over which these metrics were collected.
        /// </summary>
        public TimeSpan CollectionPeriod { get; init; }

        /// <summary>
        /// Creates empty network pooling metrics.
        /// </summary>
        /// <returns>Empty metrics instance</returns>
        public static NetworkPoolingMetrics Empty()
        {
            return new NetworkPoolingMetrics
            {
                CapturedAt = DateTime.UtcNow,
                CollectionPeriod = TimeSpan.Zero
            };
        }

        /// <summary>
        /// Calculates the network efficiency score based on various metrics.
        /// </summary>
        /// <returns>Efficiency score from 0.0 to 1.0</returns>
        public double CalculateEfficiencyScore()
        {
            var scores = new[]
            {
                // Compression efficiency (if compression is being used)
                CompressionOperations > 0 ? Math.Min(AverageCompressionRatio / 2.0, 1.0) : 1.0,
                
                // Buffer utilization efficiency (fewer exhaustion events is better)
                BufferExhaustionEvents == 0 ? 1.0 : Math.Max(0.0, 1.0 - (BufferExhaustionEvents / 100.0)),
                
                // Spike handling efficiency (fewer spikes relative to allocations is better)
                SpikeTriggeredAllocations > 0 ? Math.Max(0.0, 1.0 - (NetworkSpikesDetected / (double)SpikeTriggeredAllocations)) : 1.0,
                
                // Latency efficiency (lower latency is better, assuming 100ms is poor)
                AverageLatency.TotalMilliseconds > 0 ? Math.Max(0.0, 1.0 - (AverageLatency.TotalMilliseconds / 100.0)) : 1.0
            };

            double sum = 0.0;
            foreach (var score in scores)
                sum += score;
            return scores.Length > 0 ? sum / scores.Length : 1.0;
        }

        /// <summary>
        /// Gets the throughput ratio (current vs peak).
        /// </summary>
        public double ThroughputRatio => PeakBytesPerSecond > 0 ? (double)CurrentBytesPerSecond / PeakBytesPerSecond : 0.0;

        /// <summary>
        /// Gets the latency ratio (current vs peak).
        /// </summary>
        public double LatencyRatio => PeakLatency.TotalMilliseconds > 0 ? AverageLatency.TotalMilliseconds / PeakLatency.TotalMilliseconds : 0.0;

        /// <summary>
        /// Gets whether the network is currently experiencing high load.
        /// </summary>
        public bool IsHighLoad => ThroughputRatio > 0.8 || CurrentActiveConnections > PeakConcurrentConnections * 0.8;

        /// <summary>
        /// Gets whether network performance is currently degraded.
        /// </summary>
        public bool IsPerformanceDegraded => LatencyRatio > 0.5 || BufferExhaustionEvents > 0;
    }
}