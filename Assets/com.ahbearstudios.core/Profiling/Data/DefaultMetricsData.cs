namespace AhBearStudios.Core.Profiling.Data
{
    /// <summary>
    /// Core metrics for profiled code sections
    /// </summary>
    public struct DefaultMetricsData
    {
        /// <summary>
        /// Last sample value
        /// </summary>
        public double LastValue { get; private set; }
        
        /// <summary>
        /// Average value
        /// </summary>
        public double AverageValue { get; private set; }
        
        /// <summary>
        /// Minimum value
        /// </summary>
        public double MinValue { get; private set; }
        
        /// <summary>
        /// Maximum value
        /// </summary>
        public double MaxValue { get; private set; }
        
        /// <summary>
        /// Number of samples
        /// </summary>
        public int SampleCount { get; private set; }
        
        /// <summary>
        /// Total cumulative value
        /// </summary>
        public double TotalValue { get; private set; }
        
        // Weight for moving average calculation
        private const float MovingAverageWeight = 0.05f;
        
        /// <summary>
        /// Add a new sample
        /// </summary>
        public void AddSample(double value)
        {
            LastValue = value;
            TotalValue += value;
            
            // Update min/max
            if (SampleCount == 0 || value < MinValue)
            {
                MinValue = value;
            }
            
            if (SampleCount == 0 || value > MaxValue)
            {
                MaxValue = value;
            }
            
            // Update average
            if (SampleCount == 0)
            {
                AverageValue = value;
            }
            else
            {
                AverageValue = AverageValue * (1f - MovingAverageWeight) + value * MovingAverageWeight;
            }
            
            SampleCount++;
        }
        
        /// <summary>
        /// Reset stats
        /// </summary>
        public void Reset()
        {
            LastValue = 0;
            AverageValue = 0;
            MinValue = 0;
            MaxValue = 0;
            SampleCount = 0;
            TotalValue = 0;
        }
    }
}