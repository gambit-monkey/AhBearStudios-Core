using System;

namespace AhBearStudios.Core.Pooling.Models
{
    /// <summary>
    /// Statistics for object pool usage and performance monitoring.
    /// Provides metrics for pool sizing decisions and health monitoring.
    /// </summary>
    public class PoolStatistics
    {
        /// <summary>
        /// Gets or sets the total number of objects in the pool.
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// Gets or sets the number of objects available for use.
        /// </summary>
        public int AvailableCount { get; set; }
        
        /// <summary>
        /// Gets or sets the number of objects currently in use.
        /// </summary>
        public int ActiveCount { get; set; }
        
        /// <summary>
        /// Gets or sets the peak number of active objects.
        /// </summary>
        public int PeakActiveCount { get; set; }
        
        /// <summary>
        /// Gets or sets the total number of objects created.
        /// </summary>
        public long TotalCreated { get; set; }
        
        /// <summary>
        /// Gets or sets the total number of objects destroyed.
        /// </summary>
        public long TotalDestroyed { get; set; }
        
        /// <summary>
        /// Gets or sets the total number of get operations.
        /// </summary>
        public long TotalGets { get; set; }
        
        /// <summary>
        /// Gets or sets the total number of return operations.
        /// </summary>
        public long TotalReturns { get; set; }
        
        /// <summary>
        /// Gets or sets the number of cache hits (objects reused from pool).
        /// </summary>
        public long CacheHits { get; set; }
        
        /// <summary>
        /// Gets or sets the number of cache misses (new objects created).
        /// </summary>
        public long CacheMisses { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when statistics were last updated.
        /// </summary>
        public DateTime LastUpdated { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the pool was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the initial capacity of the pool when it was created.
        /// </summary>
        public int InitialCapacity { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum allowed capacity of the pool.
        /// </summary>
        public int MaxCapacity { get; set; }
        
        /// <summary>
        /// Gets or sets the total number of requests made to the pool.
        /// </summary>
        public long TotalRequestCount { get; set; }
        
        /// <summary>
        /// Gets the cache hit ratio as a percentage.
        /// </summary>
        public double HitRatio
        {
            get
            {
                var totalRequests = CacheHits + CacheMisses;
                return totalRequests > 0 ? (double)CacheHits / totalRequests * 100.0 : 0.0;
            }
        }
        
        /// <summary>
        /// Gets the pool utilization as a percentage.
        /// </summary>
        public double Utilization
        {
            get
            {
                return TotalCount > 0 ? (double)ActiveCount / TotalCount * 100.0 : 0.0;
            }
        }
        
        /// <summary>
        /// Gets the pool efficiency score (0-100).
        /// </summary>
        public double EfficiencyScore
        {
            get
            {
                var hitRatio = HitRatio / 100.0;
                var utilization = Math.Min(Utilization / 100.0, 1.0);
                return (hitRatio * 0.7 + utilization * 0.3) * 100.0;
            }
        }
        
        /// <summary>
        /// Gets the average idle time in minutes.
        /// </summary>
        public double AverageIdleTimeMinutes
        {
            get
            {
                var totalTime = (LastUpdated - CreatedAt).TotalMinutes;
                var activeTime = TotalGets > 0 ? totalTime * (Utilization / 100.0) : 0;
                return totalTime - activeTime;
            }
        }
        
        /// <summary>
        /// Resets all statistics to initial values.
        /// </summary>
        public void Reset()
        {
            TotalCount = 0;
            AvailableCount = 0;
            ActiveCount = 0;
            PeakActiveCount = 0;
            TotalCreated = 0;
            TotalDestroyed = 0;
            TotalGets = 0;
            TotalReturns = 0;
            CacheHits = 0;
            CacheMisses = 0;
            TotalRequestCount = 0;
            LastUpdated = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Updates the statistics with a new get operation.
        /// </summary>
        /// <param name="wasFromPool">True if the object was reused from the pool</param>
        public void RecordGet(bool wasFromPool)
        {
            TotalGets++;
            TotalRequestCount++;
            if (wasFromPool)
            {
                CacheHits++;
                AvailableCount--;
            }
            else
            {
                CacheMisses++;
                TotalCreated++;
                TotalCount++;
            }
            
            ActiveCount++;
            if (ActiveCount > PeakActiveCount)
                PeakActiveCount = ActiveCount;
                
            LastUpdated = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Updates the statistics with a new return operation.
        /// </summary>
        public void RecordReturn()
        {
            TotalReturns++;
            ActiveCount--;
            AvailableCount++;
            LastUpdated = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Updates the statistics with an object destruction.
        /// </summary>
        public void RecordDestruction()
        {
            TotalDestroyed++;
            TotalCount--;
            AvailableCount--;
            LastUpdated = DateTime.UtcNow;
        }
    }
}