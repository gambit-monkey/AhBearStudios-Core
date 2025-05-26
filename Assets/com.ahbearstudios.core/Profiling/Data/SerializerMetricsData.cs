using System;
using Unity.Burst;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Profiling.Data
{
    /// <summary>
    /// Burst-compatible metrics implementation for serialization operations.
    /// </summary>
    [BurstCompile]
    public struct SerializerMetricsData : ISerializerMetrics
    {
        /// <summary>
        /// Total number of serialization operations performed
        /// </summary>
        private long _totalSerializations;

        /// <summary>
        /// Total number of deserialization operations performed
        /// </summary>
        private long _totalDeserializations;

        /// <summary>
        /// Count of failed serialization operations
        /// </summary>
        private long _failedSerializations;

        /// <summary>
        /// Count of failed deserialization operations
        /// </summary>
        private long _failedDeserializations;

        /// <summary>
        /// Total time spent on serialization operations (in nanoseconds)
        /// </summary>
        private long _totalSerializationTimeNs;

        /// <summary>
        /// Total time spent on deserialization operations (in nanoseconds)
        /// </summary>
        private long _totalDeserializationTimeNs;

        /// <summary>
        /// Total bytes processed during serialization operations
        /// </summary>
        private long _totalBytesSeralized;

        /// <summary>
        /// Total bytes processed during deserialization operations
        /// </summary>
        private long _totalBytesDeserialized;

        /// <summary>
        /// Gets the total number of serialization operations performed
        /// </summary>
        public long TotalSerializations => _totalSerializations;

        /// <summary>
        /// Gets the total number of deserialization operations performed
        /// </summary>
        public long TotalDeserializations => _totalDeserializations;

        /// <summary>
        /// Gets the count of failed serialization operations
        /// </summary>
        public long FailedSerializations => _failedSerializations;

        /// <summary>
        /// Gets the count of failed deserialization operations
        /// </summary>
        public long FailedDeserializations => _failedDeserializations;

        /// <summary>
        /// Gets the average time in milliseconds spent on serialization operations
        /// </summary>
        public double AverageSerializationTimeMs => 
            _totalSerializations > 0 ? (_totalSerializationTimeNs / (double)_totalSerializations) / 1_000_000.0 : 0.0;

        /// <summary>
        /// Gets the average time in milliseconds spent on deserialization operations
        /// </summary>
        public double AverageDeserializationTimeMs => 
            _totalDeserializations > 0 ? (_totalDeserializationTimeNs / (double)_totalDeserializations) / 1_000_000.0 : 0.0;

        /// <summary>
        /// Gets the total bytes processed during serialization operations
        /// </summary>
        public long TotalBytesSeralized => _totalBytesSeralized;

        /// <summary>
        /// Gets the total bytes processed during deserialization operations
        /// </summary>
        public long TotalBytesDeserialized => _totalBytesDeserialized;

        /// <summary>
        /// Resets all metrics to zero
        /// </summary>
        public void Reset()
        {
            _totalSerializations = 0;
            _totalDeserializations = 0;
            _failedSerializations = 0;
            _failedDeserializations = 0;
            _totalSerializationTimeNs = 0;
            _totalDeserializationTimeNs = 0;
            _totalBytesSeralized = 0;
            _totalBytesDeserialized = 0;
        }

        /// <summary>
        /// Records metrics for a serialization operation
        /// </summary>
        /// <param name="duration">Time taken to complete the operation</param>
        /// <param name="byteCount">Number of bytes processed</param>
        /// <param name="success">Whether the operation was successful</param>
        public void RecordSerialization(TimeSpan duration, int byteCount, bool success)
        {
            // We can't use Interlocked in a Burst-compatible struct, so we use direct operations
            // Note: This makes the operations non-thread-safe in Burst-compiled code
            _totalSerializations++;
            _totalSerializationTimeNs += duration.Ticks * 100; // Convert ticks to nanoseconds

            if (success)
            {
                _totalBytesSeralized += byteCount;
            }
            else
            {
                _failedSerializations++;
            }
        }

        /// <summary>
        /// Records metrics for a deserialization operation
        /// </summary>
        /// <param name="duration">Time taken to complete the operation</param>
        /// <param name="byteCount">Number of bytes processed</param>
        /// <param name="success">Whether the operation was successful</param>
        public void RecordDeserialization(TimeSpan duration, int byteCount, bool success)
        {
            // We can't use Interlocked in a Burst-compatible struct, so we use direct operations
            // Note: This makes the operations non-thread-safe in Burst-compiled code
            _totalDeserializations++;
            _totalDeserializationTimeNs += duration.Ticks * 100; // Convert ticks to nanoseconds

            if (success)
            {
                _totalBytesDeserialized += byteCount;
            }
            else
            {
                _failedDeserializations++;
            }
        }

        /// <summary>
        /// Thread-safe version of RecordSerialization for use in non-Burst contexts
        /// </summary>
        /// <param name="duration">Time taken to complete the operation</param>
        /// <param name="byteCount">Number of bytes processed</param>
        /// <param name="success">Whether the operation was successful</param>
        public void RecordSerializationThreadSafe(TimeSpan duration, int byteCount, bool success)
        {
            System.Threading.Interlocked.Increment(ref _totalSerializations);
            System.Threading.Interlocked.Add(ref _totalSerializationTimeNs, duration.Ticks * 100);
            
            if (success)
            {
                System.Threading.Interlocked.Add(ref _totalBytesSeralized, byteCount);
            }
            else
            {
                System.Threading.Interlocked.Increment(ref _failedSerializations);
            }
        }

        /// <summary>
        /// Thread-safe version of RecordDeserialization for use in non-Burst contexts
        /// </summary>
        /// <param name="duration">Time taken to complete the operation</param>
        /// <param name="byteCount">Number of bytes processed</param>
        /// <param name="success">Whether the operation was successful</param>
        public void RecordDeserializationThreadSafe(TimeSpan duration, int byteCount, bool success)
        {
            System.Threading.Interlocked.Increment(ref _totalDeserializations);
            System.Threading.Interlocked.Add(ref _totalDeserializationTimeNs, duration.Ticks * 100);
            
            if (success)
            {
                System.Threading.Interlocked.Add(ref _totalBytesDeserialized, byteCount);
            }
            else
            {
                System.Threading.Interlocked.Increment(ref _failedDeserializations);
            }
        }
    }
}