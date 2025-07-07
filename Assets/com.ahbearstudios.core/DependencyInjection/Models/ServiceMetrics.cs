using System;

namespace AhBearStudios.Core.DependencyInjection.Models
{
    /// <summary>
    /// Performance metrics for a specific service type.
    /// </summary>
    public sealed class ServiceMetrics
    {
        /// <summary>
        /// Gets the service type.
        /// </summary>
        public Type ServiceType { get; }
        
        /// <summary>
        /// Gets the number of times this service was resolved.
        /// </summary>
        public long ResolutionCount { get; internal set; }
        
        /// <summary>
        /// Gets the total time spent resolving this service.
        /// </summary>
        public TimeSpan TotalResolutionTime { get; internal set; }
        
        /// <summary>
        /// Gets the average resolution time for this service.
        /// </summary>
        public double AverageResolutionTimeMs => 
            ResolutionCount > 0 ? TotalResolutionTime.TotalMilliseconds / ResolutionCount : 0.0;
        
        /// <summary>
        /// Gets the peak resolution time for this service.
        /// </summary>
        public TimeSpan PeakResolutionTime { get; internal set; }
        
        /// <summary>
        /// Gets the number of failed resolutions for this service.
        /// </summary>
        public long FailedResolutions { get; internal set; }
        
        /// <summary>
        /// Initializes service metrics for the specified type.
        /// </summary>
        public ServiceMetrics(Type serviceType)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        }
        
        /// <summary>
        /// Records a successful resolution.
        /// </summary>
        internal void RecordResolution(TimeSpan resolutionTime)
        {
            ResolutionCount++;
            TotalResolutionTime = TotalResolutionTime.Add(resolutionTime);
            
            if (resolutionTime > PeakResolutionTime)
                PeakResolutionTime = resolutionTime;
        }
        
        /// <summary>
        /// Records a failed resolution.
        /// </summary>
        internal void RecordFailedResolution()
        {
            FailedResolutions++;
        }
    }
}