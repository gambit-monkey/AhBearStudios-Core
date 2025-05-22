using System;
using System.Threading;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Profiling.Data
{
        /// <summary>
        /// Metrics implementation for the serializer.
        /// </summary>
        public sealed class SerializerMetrics : ISerializerMetrics
        {
            private long _totalSerializations;
            private long _totalDeserializations;
            private long _failedSerializations;
            private long _failedDeserializations;
            private long _totalSerializationTimeNs;
            private long _totalDeserializationTimeNs;
            private long _totalBytesSeralized;
            private long _totalBytesDeserialized;
            
            /// <inheritdoc />
            public long TotalSerializations => _totalSerializations;
            
            /// <inheritdoc />
            public long TotalDeserializations => _totalDeserializations;
            
            /// <inheritdoc />
            public long FailedSerializations => _failedSerializations;
            
            /// <inheritdoc />
            public long FailedDeserializations => _failedDeserializations;
            
            /// <inheritdoc />
            public double AverageSerializationTimeMs => 
                _totalSerializations > 0 ? (_totalSerializationTimeNs / _totalSerializations) / 1_000_000.0 : 0.0;
            
            /// <inheritdoc />
            public double AverageDeserializationTimeMs => 
                _totalDeserializations > 0 ? (_totalDeserializationTimeNs / _totalDeserializations) / 1_000_000.0 : 0.0;
            
            /// <inheritdoc />
            public long TotalBytesSeralized => _totalBytesSeralized;
            
            /// <inheritdoc />
            public long TotalBytesDeserialized => _totalBytesDeserialized;
            
            /// <inheritdoc />
            public void Reset()
            {
                Interlocked.Exchange(ref _totalSerializations, 0);
                Interlocked.Exchange(ref _totalDeserializations, 0);
                Interlocked.Exchange(ref _failedSerializations, 0);
                Interlocked.Exchange(ref _failedDeserializations, 0);
                Interlocked.Exchange(ref _totalSerializationTimeNs, 0);
                Interlocked.Exchange(ref _totalDeserializationTimeNs, 0);
                Interlocked.Exchange(ref _totalBytesSeralized, 0);
                Interlocked.Exchange(ref _totalBytesDeserialized, 0);
            }
            
            public void RecordSerialization(TimeSpan duration, int byteCount, bool success)
            {
                Interlocked.Increment(ref _totalSerializations);
                Interlocked.Add(ref _totalSerializationTimeNs, duration.Ticks * 100); // Convert ticks to nanoseconds
                
                if (success)
                {
                    Interlocked.Add(ref _totalBytesSeralized, byteCount);
                }
                else
                {
                    Interlocked.Increment(ref _failedSerializations);
                }
            }
            
            public void RecordDeserialization(TimeSpan duration, int byteCount, bool success)
            {
                Interlocked.Increment(ref _totalDeserializations);
                Interlocked.Add(ref _totalDeserializationTimeNs, duration.Ticks * 100); // Convert ticks to nanoseconds
                
                if (success)
                {
                    Interlocked.Add(ref _totalBytesDeserialized, byteCount);
                }
                else
                {
                    Interlocked.Increment(ref _failedDeserializations);
                }
            }
        }
}