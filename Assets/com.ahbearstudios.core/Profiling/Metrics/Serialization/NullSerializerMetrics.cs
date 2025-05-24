using System;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Profiling.Metrics.Serialization
{
    /// <summary>
    /// Null implementation of ISerializerMetrics for when no metrics are needed.
    /// All operations are no-op and all values return zero.
    /// </summary>
    public sealed class NullSerializerMetrics : ISerializerMetrics
    {
        /// <inheritdoc />
        public long TotalSerializations => 0;

        /// <inheritdoc />
        public long TotalDeserializations => 0;

        /// <inheritdoc />
        public long FailedSerializations => 0;

        /// <inheritdoc />
        public long FailedDeserializations => 0;

        /// <inheritdoc />
        public double AverageSerializationTimeMs => 0.0;

        /// <inheritdoc />
        public double AverageDeserializationTimeMs => 0.0;

        /// <inheritdoc />
        public long TotalBytesSeralized => 0;

        /// <inheritdoc />
        public long TotalBytesDeserialized => 0;

        /// <inheritdoc />
        public void RecordSerialization(TimeSpan duration, int dataSize, bool success)
        {
            // No-op for null implementation
        }

        /// <inheritdoc />
        public void RecordDeserialization(TimeSpan duration, int dataSize, bool success)
        {
            // No-op for null implementation
        }

        /// <inheritdoc />
        public void Reset()
        {
            // No-op for null implementation
        }
    }
}