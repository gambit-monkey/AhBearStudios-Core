using System;

namespace AhBearStudios.Core.MessageBus.Data
{
    /// <summary>
    /// Represents a throughput sample.
    /// </summary>
    public readonly struct ThroughputSample
    {
        public DateTime Timestamp { get; }
        public double Throughput { get; }
        public int BatchSize { get; }
            
        public ThroughputSample(DateTime timestamp, double throughput, int batchSize)
        {
            Timestamp = timestamp;
            Throughput = throughput;
            BatchSize = batchSize;
        }
    }
}