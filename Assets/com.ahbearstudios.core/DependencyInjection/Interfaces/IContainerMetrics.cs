using System.Collections.Generic;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Performance metrics for container operations.
    /// </summary>
    public interface IContainerMetrics
    {
        /// <summary>
        /// Gets the total number of registrations.
        /// </summary>
        int TotalRegistrations { get; }
        
        /// <summary>
        /// Gets the total number of resolutions performed.
        /// </summary>
        long TotalResolutions { get; }
        
        /// <summary>
        /// Gets the total number of failed resolutions.
        /// </summary>
        long FailedResolutions { get; }
        
        /// <summary>
        /// Gets the average resolution time in milliseconds.
        /// </summary>
        double AverageResolutionTimeMs { get; }
        
        /// <summary>
        /// Gets the peak resolution time in milliseconds.
        /// </summary>
        double PeakResolutionTimeMs { get; }
        
        /// <summary>
        /// Gets the time taken to build the container.
        /// </summary>
        TimeSpan BuildTime { get; }
        
        /// <summary>
        /// Gets the memory usage of the container in bytes.
        /// </summary>
        long MemoryUsageBytes { get; }
        
        /// <summary>
        /// Gets resolution performance by service type.
        /// </summary>
        IReadOnlyDictionary<Type, ServiceMetrics> ServiceMetrics { get; }
        
        /// <summary>
        /// Resets all metrics.
        /// </summary>
        void Reset();
    }
}