using System.Collections.Concurrent;
using System.Collections.Generic;
using AhBearStudios.Core.DependencyInjection.Configuration;
using AhBearStudios.Core.DependencyInjection.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Models
{
    /// <summary>
    /// Implementation of container performance metrics.
    /// Thread-safe and optimized for minimal allocation overhead.
    /// </summary>
    public sealed class ContainerMetrics : IContainerMetrics
    {
        private readonly string _containerName;
        private readonly ConcurrentDictionary<Type, ServiceMetrics> _serviceMetrics;
        private long _totalRegistrations;
        private long _totalResolutions;
        private long _failedResolutions;
        private long _totalResolutionTicks;
        private long _peakResolutionTicks;
        private TimeSpan _buildTime;
        private long _memoryUsageBytes;
        private DateTime _creationTime;
        private long _registrationFailures;
        
        /// <summary>
        /// Gets the total number of registrations.
        /// </summary>
        public int TotalRegistrations => (int)_totalRegistrations;
        
        /// <summary>
        /// Gets the total number of resolutions performed.
        /// </summary>
        public long TotalResolutions => _totalResolutions;
        
        /// <summary>
        /// Gets the total number of failed resolutions.
        /// </summary>
        public long FailedResolutions => _failedResolutions;
        
        /// <summary>
        /// Gets the average resolution time in milliseconds.
        /// </summary>
        public double AverageResolutionTimeMs
        {
            get
            {
                var totalResolutions = _totalResolutions;
                return totalResolutions > 0 
                    ? TimeSpan.FromTicks(_totalResolutionTicks).TotalMilliseconds / totalResolutions 
                    : 0.0;
            }
        }
        
        /// <summary>
        /// Gets the peak resolution time in milliseconds.
        /// </summary>
        public double PeakResolutionTimeMs => TimeSpan.FromTicks(_peakResolutionTicks).TotalMilliseconds;
        
        /// <summary>
        /// Gets the time taken to build the container.
        /// </summary>
        public TimeSpan BuildTime => _buildTime;
        
        /// <summary>
        /// Gets the memory usage of the container in bytes.
        /// </summary>
        public long MemoryUsageBytes => _memoryUsageBytes;
        
        /// <summary>
        /// Gets resolution performance by service type.
        /// </summary>
        public IReadOnlyDictionary<Type, ServiceMetrics> ServiceMetrics => _serviceMetrics;
        
        /// <summary>
        /// Initializes new container metrics.
        /// </summary>
        public ContainerMetrics(string containerName)
        {
            _containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
            _serviceMetrics = new ConcurrentDictionary<Type, ServiceMetrics>();
            _creationTime = DateTime.UtcNow;
            
            // Estimate initial memory usage
            UpdateMemoryUsage();
        }
        
        /// <summary>
        /// Records a service registration.
        /// </summary>
        internal void RecordRegistration(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            System.Threading.Interlocked.Increment(ref _totalRegistrations);
            
            // Ensure service metrics exist
            _serviceMetrics.GetOrAdd(serviceType, type => new ServiceMetrics(type));
            
            UpdateMemoryUsage();
        }
        
        /// <summary>
        /// Records a registration failure.
        /// </summary>
        internal void RecordRegistrationFailure(Type serviceType, Type implementationType)
        {
            System.Threading.Interlocked.Increment(ref _registrationFailures);
        }
        
        /// <summary>
        /// Records a successful service resolution.
        /// </summary>
        internal void RecordResolution(Type serviceType, TimeSpan resolutionTime)
        {
            System.Threading.Interlocked.Increment(ref _totalResolutions);
            
            var ticks = resolutionTime.Ticks;
            System.Threading.Interlocked.Add(ref _totalResolutionTicks, ticks);
            
            // Update peak resolution time
            long currentPeak, newPeak;
            do
            {
                currentPeak = _peakResolutionTicks;
                newPeak = Math.Max(currentPeak, ticks);
            } while (System.Threading.Interlocked.CompareExchange(ref _peakResolutionTicks, newPeak, currentPeak) != currentPeak);
            
            // Update service-specific metrics
            if (_serviceMetrics.TryGetValue(serviceType, out var serviceMetrics))
            {
                serviceMetrics.RecordResolution(resolutionTime);
            }
        }
        
        /// <summary>
        /// Records a failed service resolution.
        /// </summary>
        internal void RecordResolutionFailure(Type serviceType)
        {
            System.Threading.Interlocked.Increment(ref _failedResolutions);
            
            // Update service-specific metrics
            if (_serviceMetrics.TryGetValue(serviceType, out var serviceMetrics))
            {
                serviceMetrics.RecordFailedResolution();
            }
        }
        
        /// <summary>
        /// Records container build completion.
        /// </summary>
        internal void RecordBuild(TimeSpan buildTime)
        {
            _buildTime = buildTime;
            UpdateMemoryUsage();
        }
        
        /// <summary>
        /// Records container build failure.
        /// </summary>
        internal void RecordBuildFailure(TimeSpan attemptedBuildTime)
        {
            // Could track build failures if needed
        }
        
        /// <summary>
        /// Records container disposal.
        /// </summary>
        internal void RecordDisposal(TimeSpan lifetime)
        {
            // Could track disposal metrics if needed
        }
        
        /// <summary>
        /// Resets all metrics.
        /// </summary>
        public void Reset()
        {
            _totalRegistrations = 0;
            _totalResolutions = 0;
            _failedResolutions = 0;
            _totalResolutionTicks = 0;
            _peakResolutionTicks = 0;
            _registrationFailures = 0;
            _buildTime = TimeSpan.Zero;
            _serviceMetrics.Clear();
            _creationTime = DateTime.UtcNow;
            UpdateMemoryUsage();
        }
        
        /// <summary>
        /// Updates the memory usage estimate.
        /// </summary>
        private void UpdateMemoryUsage()
        {
            // Rough estimate of memory usage
            // In a real implementation, this might use more sophisticated memory tracking
            var baseSize = 1024; // Base container overhead
            var registrationSize = _totalRegistrations * 128; // Estimated per-registration overhead
            var metricsSize = _serviceMetrics.Count * 256; // Estimated per-service metrics overhead
            
            _memoryUsageBytes = baseSize + registrationSize + metricsSize;
        }
        
        /// <summary>
        /// Gets a summary of the metrics.
        /// </summary>
        public override string ToString()
        {
            var successRate = _totalResolutions > 0 
                ? (double)(_totalResolutions - _failedResolutions) / _totalResolutions * 100.0 
                : 100.0;
            
            return $"Container '{_containerName}': " +
                   $"{TotalRegistrations} registrations, " +
                   $"{TotalResolutions} resolutions " +
                   $"({successRate:F1}% success rate), " +
                   $"avg resolution time: {AverageResolutionTimeMs:F2}ms, " +
                   $"build time: {BuildTime.TotalMilliseconds:F1}ms, " +
                   $"memory usage: {MemoryUsageBytes / 1024.0:F1}KB";
        }
    }
}