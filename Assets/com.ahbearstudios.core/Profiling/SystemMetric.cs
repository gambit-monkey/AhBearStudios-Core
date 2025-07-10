using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Profiling.LowLevel;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// SystemMetric represents a single Unity ProfilerRecorder metric to track
    /// </summary>
    public class SystemMetric
    {
        private readonly string _statName;
        
        /// <summary>
        /// The profiler tag for this metric
        /// </summary>
        public ProfilerTag Tag { get; }

        /// <summary>
        /// The underlying ProfilerRecorder
        /// </summary>
        public ProfilerRecorder Recorder { get; }

        /// <summary>
        /// Last recorded value
        /// </summary>
        public double LastValue { get; private set; }

        /// <summary>
        /// Moving average value over time
        /// </summary>
        public double AverageValue { get; private set; }

        /// <summary>
        /// Maximum value recorded
        /// </summary>
        public double MaxValue { get; private set; }

        /// <summary>
        /// Unit for this metric (ms, count, bytes, etc.)
        /// </summary>
        public string Unit { get; }

        /// <summary>
        /// Number of samples taken
        /// </summary>
        public int SampleCount { get; private set; }

        // Weight for moving average calculation
        private const float MovingAverageWeight = 0.05f;

        /// <summary>
        /// Create a new system metric
        /// </summary>
        internal SystemMetric(ProfilerTag tag, ProfilerRecorder recorder, string unit)
        {
            Tag = tag;
            Recorder = recorder;
            Unit = unit;
            LastValue = 0;
            AverageValue = 0;
            MaxValue = 0;
            SampleCount = 0;
        }

        /// <summary>
        /// Update the metric with the latest value from the recorder
        /// </summary>
        public void Update()
        {
            if (!Recorder.Valid)
                return;

            long rawValue = Recorder.CurrentValue;
            double convertedValue = ConvertToReadableValue(rawValue);

            LastValue = convertedValue;

            // Update moving average
            if (SampleCount == 0)
            {
                AverageValue = LastValue;
            }
            else
            {
                AverageValue = AverageValue * (1 - MovingAverageWeight) + LastValue * MovingAverageWeight;
            }

            // Update max
            if (LastValue > MaxValue || SampleCount == 0)
            {
                MaxValue = LastValue;
            }

            SampleCount++;
        }

        /// <summary>
        /// Reset statistics for this metric
        /// </summary>
        public void Reset()
        {
            LastValue = 0;
            AverageValue = 0;
            MaxValue = 0;
            SampleCount = 0;
        }
        
        // Dictionary of known stat names and their data types
        private static readonly Dictionary<string, MetricDataType> _knownStatTypes = new Dictionary<string, MetricDataType>(StringComparer.OrdinalIgnoreCase)
        {
            // Time values (in nanoseconds)
            { "Main Thread", MetricDataType.TimeNanoseconds },
            { "FrameTime", MetricDataType.TimeNanoseconds },
            { "Physics.Step", MetricDataType.TimeNanoseconds },
    
            // Byte values
            { "GC.Alloc.Size", MetricDataType.Bytes },
            { "System Used Memory", MetricDataType.Bytes },
    
            // Count values
            { "GC.Alloc.Count", MetricDataType.Count },
            { "Batches Count", MetricDataType.Count }
    
            // Add other known stats as needed
        };
        internal SystemMetric(ProfilerTag tag, ProfilerRecorder recorder, string statName, string unit)
        {
            Tag = tag;
            Recorder = recorder;
            _statName = statName;
            Unit = unit;
            LastValue = 0;
            AverageValue = 0;
            MaxValue = 0;
            SampleCount = 0;
        }

        /// <summary>
        /// Convert raw value to readable value based on known stat type
        /// </summary>
        private double ConvertToReadableValue(long rawValue)
        {
            double value = rawValue;

            // Look up the type for this stat name
            if (_knownStatTypes.TryGetValue(_statName, out var dataType))
            {
                switch (dataType)
                {
                    case MetricDataType.TimeNanoseconds:
                        value = value / 1_000_000.0; // ns to ms
                        break;

                    case MetricDataType.Bytes:
                        value = value / 1024.0; // bytes to KB
                        break;

                    // Handle other types as needed
                }
            }
            else
            {
                // Fallback - try to infer from the unit as before
                if (Unit.Equals("ms", StringComparison.OrdinalIgnoreCase))
                    value = value / 1_000_000.0;
                else if (Unit.Equals("KB", StringComparison.OrdinalIgnoreCase))
                    value = value / 1024.0;
            }

            return value;
        }

        /// <summary>
        /// Get formatted value with unit
        /// </summary>
        public string GetFormattedValue(double value)
        {
            return $"{value:F2} {Unit}";
        }

        /// <summary>
        /// Get formatted last value with unit
        /// </summary>
        public string GetFormattedLastValue()
        {
            return GetFormattedValue(LastValue);
        }

        /// <summary>
        /// Get formatted average value with unit
        /// </summary>
        public string GetFormattedAverageValue()
        {
            return GetFormattedValue(AverageValue);
        }

        /// <summary>
        /// Get formatted max value with unit
        /// </summary>
        public string GetFormattedMaxValue()
        {
            return GetFormattedValue(MaxValue);
        }
    }
}