using Unity.Profiling;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// SystemMetric represents a single Unity ProfilerRecorder metric to track
    /// </summary>
    public class SystemMetric
    {
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

        /// <summary>
        /// Convert raw value to readable value based on marker type
        /// </summary>
        private double ConvertToReadableValue(long rawValue)
        {
            double value = rawValue;

            // Convert based on marker type
            switch (Recorder.ProfilerMarkerDataType)
            {
                case ProfilerMarkerDataType.TimeNanoseconds:
                    value = value / 1_000_000.0; // ns to ms
                    break;

                case ProfilerMarkerDataType.Bytes:
                    value = value / 1024.0; // bytes to KB
                    break;

                case ProfilerMarkerDataType.Frequency:
                case ProfilerMarkerDataType.Count:
                    // Keep as is
                    break;

                default:
                    // Keep as is for unknown types
                    break;
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