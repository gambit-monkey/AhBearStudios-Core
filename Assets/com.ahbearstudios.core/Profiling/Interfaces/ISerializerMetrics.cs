using System;

namespace AhBearStudios.Core.Profiling.Interfaces
{
    /// <summary>
    /// Interface for serializer performance metrics.
    /// </summary>
    public interface ISerializerMetrics
    {
        /// <summary>
        /// Gets the total number of serialization operations performed.
        /// </summary>
        long TotalSerializations { get; }
        
        /// <summary>
        /// Gets the total number of deserialization operations performed.
        /// </summary>
        long TotalDeserializations { get; }
        
        /// <summary>
        /// Gets the number of failed serialization operations.
        /// </summary>
        long FailedSerializations { get; }
        
        /// <summary>
        /// Gets the number of failed deserialization operations.
        /// </summary>
        long FailedDeserializations { get; }
        
        /// <summary>
        /// Gets the average serialization time in milliseconds.
        /// </summary>
        double AverageSerializationTimeMs { get; }
        
        /// <summary>
        /// Gets the average deserialization time in milliseconds.
        /// </summary>
        double AverageDeserializationTimeMs { get; }
        
        /// <summary>
        /// Gets the total bytes serialized.
        /// </summary>
        long TotalBytesSeralized { get; }
        
        /// <summary>
        /// Gets the total bytes deserialized.
        /// </summary>
        long TotalBytesDeserialized { get; }

        /// <summary>
        /// Records metrics for a serialization operation.
        /// </summary>
        /// <param name="duration">The duration of the serialization operation.</param>
        /// <param name="dataSize">The size of the serialized data in bytes.</param>
        /// <param name="success">Whether the serialization was successful.</param>
        void RecordSerialization(TimeSpan duration, int dataSize, bool success);

        /// <summary>
        /// Records metrics for a deserialization operation.
        /// </summary>
        /// <param name="duration">The duration of the deserialization operation.</param>
        /// <param name="dataSize">The size of the data being deserialized in bytes.</param>
        /// <param name="success">Whether the deserialization was successful.</param>
        void RecordDeserialization(TimeSpan duration, int dataSize, bool success);
        
        /// <summary>
        /// Resets all metrics.
        /// </summary>
        void Reset();
    }
}