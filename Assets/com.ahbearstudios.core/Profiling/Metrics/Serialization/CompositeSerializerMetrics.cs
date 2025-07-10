using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Profiling.Metrics.Serialization
{
    /// <summary>
    /// Composite implementation of ISerializerMetrics that combines metrics from multiple serializers.
    /// </summary>
    public sealed class CompositeSerializerMetrics : ISerializerMetrics
    {
        private readonly List<ISerializerMetrics> _metrics;
            
        /// <summary>
        /// Initializes a new instance of the CompositeSerializerMetrics class.
        /// </summary>
        /// <param name="metrics">List of metrics implementations to combine.</param>
        public CompositeSerializerMetrics(List<ISerializerMetrics> metrics)
        {
            _metrics = metrics ?? new List<ISerializerMetrics>();
        }
            
        /// <inheritdoc />
        public long TotalSerializations => _metrics.Sum(m => m.TotalSerializations);

        /// <inheritdoc />
        public long TotalDeserializations => _metrics.Sum(m => m.TotalDeserializations);

        /// <inheritdoc />
        public long FailedSerializations => _metrics.Sum(m => m.FailedSerializations);

        /// <inheritdoc />
        public long FailedDeserializations => _metrics.Sum(m => m.FailedDeserializations);

        /// <inheritdoc />
        public double AverageSerializationTimeMs => 
            _metrics.Count > 0 ? _metrics.Average(m => m.AverageSerializationTimeMs) : 0.0;

        /// <inheritdoc />
        public double AverageDeserializationTimeMs => 
            _metrics.Count > 0 ? _metrics.Average(m => m.AverageDeserializationTimeMs) : 0.0;

        /// <inheritdoc />
        public long TotalBytesSeralized => _metrics.Sum(m => m.TotalBytesSeralized);

        /// <inheritdoc />
        public long TotalBytesDeserialized => _metrics.Sum(m => m.TotalBytesDeserialized);

        /// <inheritdoc />
        public void RecordSerialization(TimeSpan duration, int dataSize, bool success)
        {
            foreach (var metric in _metrics)
            {
                metric.RecordSerialization(duration, dataSize, success);
            }
        }

        /// <inheritdoc />
        public void RecordDeserialization(TimeSpan duration, int dataSize, bool success)
        {
            foreach (var metric in _metrics)
            {
                metric.RecordDeserialization(duration, dataSize, success);
            }
        }
            
        /// <inheritdoc />
        public void Reset()
        {
            foreach (var metric in _metrics)
            {
                metric.Reset();
            }
        }
    }
}