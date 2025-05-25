using System;
using Unity.Collections;
using Unity.Mathematics;

namespace AhBearStudios.Core.Pooling.Diagnostics
{
    /// <summary>
    /// Burst-compatible structure containing pool metrics data
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct PoolMetricsData : IEquatable<PoolMetricsData>
    {
        // Pool identification
        public FixedString128Bytes Name;
        public FixedString64Bytes PoolId;
        public FixedString64Bytes PoolType;
        
        // Capacity and usage statistics
        public int Capacity;
        public int InitialCapacity;
        public int MinCapacity;
        public int MaxCapacity;
        public int ActiveCount;
        public int PeakActiveCount;
        public int TotalCreatedCount;
        public int TotalAcquiredCount;
        public int TotalReleasedCount;
        public int TotalResizeOperations;
        
        // Memory tracking
        public long TotalMemoryBytes;
        public long PeakMemoryBytes;
        public int EstimatedItemSizeBytes;
        
        // Performance metrics
        public float AverageAcquireTimeMs;
        public float MaxAcquireTimeMs;
        public float MinAcquireTimeMs;
        public float LastAcquireTimeMs;
        public float TotalAcquireTimeMs;
        public int AcquireSampleCount;
        
        public float AverageReleaseTimeMs;
        public float MaxReleaseTimeMs;
        public float MinReleaseTimeMs;
        public float LastReleaseTimeMs;
        public float TotalReleaseTimeMs;
        public int ReleaseSampleCount;
        
        public float TotalLifetimeSeconds;
        public float MaxItemLifetimeSeconds;
        public float MinItemLifetimeSeconds;
        public int LifetimeSampleCount;
        
        // Fragmentation metrics
        public int FragmentCount;
        public float FragmentationRatio;
        
        // Cache hit/miss tracking
        public int CacheHits;
        public int CacheMisses;
        
        // Time tracking
        public float LastOperationTime;
        public float LastResetTime;
        public float CreationTime;
        public float UpTimeSeconds;
        
        // Allocation pressure metrics
        public int OverflowAllocations;
        public int UnderflowDeallocations;
        
        /// <summary>
        /// Creates a new PoolMetricsData with default values
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="name">Pool name</param>
        public PoolMetricsData(FixedString64Bytes poolId, FixedString128Bytes name)
        {
            PoolId = poolId;
            Name = name;
            PoolType = default;
            
            Capacity = 0;
            InitialCapacity = 0;
            MinCapacity = 0;
            MaxCapacity = 0;
            ActiveCount = 0;
            PeakActiveCount = 0;
            TotalCreatedCount = 0;
            TotalAcquiredCount = 0;
            TotalReleasedCount = 0;
            TotalResizeOperations = 0;
            
            TotalMemoryBytes = 0;
            PeakMemoryBytes = 0;
            EstimatedItemSizeBytes = 0;
            
            AverageAcquireTimeMs = 0;
            MaxAcquireTimeMs = 0;
            MinAcquireTimeMs = float.MaxValue;
            LastAcquireTimeMs = 0;
            TotalAcquireTimeMs = 0;
            AcquireSampleCount = 0;
            
            AverageReleaseTimeMs = 0;
            MaxReleaseTimeMs = 0;
            MinReleaseTimeMs = float.MaxValue;
            LastReleaseTimeMs = 0;
            TotalReleaseTimeMs = 0;
            ReleaseSampleCount = 0;
            
            TotalLifetimeSeconds = 0;
            MaxItemLifetimeSeconds = 0;
            MinItemLifetimeSeconds = float.MaxValue;
            LifetimeSampleCount = 0;
            
            FragmentCount = 0;
            FragmentationRatio = 0;
            
            CacheHits = 0;
            CacheMisses = 0;
            
            LastOperationTime = 0;
            LastResetTime = 0;
            CreationTime = 0;
            UpTimeSeconds = 0;
            
            OverflowAllocations = 0;
            UnderflowDeallocations = 0;
        }
        
        /// <summary>
        /// Gets the number of free items in the pool
        /// </summary>
        public readonly int FreeCount => math.max(0, Capacity - ActiveCount);
        
        /// <summary>
        /// Gets the usage percentage of the pool (0-1)
        /// </summary>
        public readonly float UsageRatio => Capacity > 0 ? (float)ActiveCount / Capacity : 0f;
        
        /// <summary>
        /// Gets the number of potentially leaked items (acquired but not released)
        /// </summary>
        public readonly int LeakedItemCount => math.max(0, TotalAcquiredCount - TotalReleasedCount);
        
        /// <summary>
        /// Gets the average lifetime of released items in seconds
        /// </summary>
        public readonly float AverageLifetimeSeconds => LifetimeSampleCount > 0 
            ? TotalLifetimeSeconds / LifetimeSampleCount 
            : 0;
            
        /// <summary>
        /// Gets the cache hit ratio (0-1)
        /// </summary>
        public readonly float CacheHitRatio => (CacheHits + CacheMisses) > 0 
            ? (float)CacheHits / (CacheHits + CacheMisses) 
            : 0;
            
        /// <summary>
        /// Gets the estimated efficiency of the pool (0-1)
        /// Higher values indicate better resource utilization
        /// </summary>
        public readonly float PoolEfficiency => 
            UsageRatio > 0.8f ? 1.0f : // High utilization is good
            (TotalAcquiredCount > 0 ? 
                math.min(1.0f, 0.5f + 0.5f * ((float)TotalAcquiredCount / math.max(1, TotalCreatedCount))) : 
                0);
                
        /// <summary>
        /// Returns a new PoolMetricsData with updated capacity information
        /// </summary>
        public readonly PoolMetricsData WithCapacity(int newCapacity)
        {
            var result = this;
            result.Capacity = newCapacity;
            result.PeakActiveCount = math.max(result.PeakActiveCount, result.ActiveCount);
            
            // Update memory tracking
            result.TotalMemoryBytes = (long)newCapacity * EstimatedItemSizeBytes;
            result.PeakMemoryBytes = math.max(result.PeakMemoryBytes, result.TotalMemoryBytes);
            
            // Track resize operations
            if (newCapacity != Capacity)
                result.TotalResizeOperations++;
                
            return result;
        }
        
        /// <summary>
        /// Returns a new PoolMetricsData with updated item size information
        /// </summary>
        public readonly PoolMetricsData WithItemSize(int itemSizeBytes)
        {
            var result = this;
            result.EstimatedItemSizeBytes = itemSizeBytes;
            
            // Update memory tracking based on new size
            result.TotalMemoryBytes = (long)Capacity * itemSizeBytes;
            result.PeakMemoryBytes = math.max(result.PeakMemoryBytes, result.TotalMemoryBytes);
            
            return result;
        }
        
        /// <summary>
        /// Records an acquire operation and returns updated metrics
        /// </summary>
        public readonly PoolMetricsData RecordAcquire(int activeCount, float acquireTimeMs, float currentTime)
        {
            var result = this;
            
            // Update state
            result.ActiveCount = activeCount;
            result.PeakActiveCount = math.max(result.PeakActiveCount, activeCount);
            result.TotalAcquiredCount++;
            result.LastOperationTime = currentTime;
            
            // Track time
            if (result.CreationTime == 0)
                result.CreationTime = currentTime;
                
            result.UpTimeSeconds = currentTime - result.CreationTime;
            
            // Update acquire time metrics
            result.LastAcquireTimeMs = acquireTimeMs;
            result.TotalAcquireTimeMs += acquireTimeMs;
            result.AcquireSampleCount++;
            
            result.MaxAcquireTimeMs = math.max(result.MaxAcquireTimeMs, acquireTimeMs);
            result.MinAcquireTimeMs = math.min(result.MinAcquireTimeMs, acquireTimeMs);
            result.AverageAcquireTimeMs = result.TotalAcquireTimeMs / result.AcquireSampleCount;
            
            // Update cache hit/miss tracking
            if (activeCount >= Capacity)
            {
                result.CacheMisses++;
                result.OverflowAllocations++;
            }
            else
            {
                result.CacheHits++;
            }
            
            return result;
        }
        
        /// <summary>
        /// Records a release operation and returns updated metrics
        /// </summary>
        public readonly PoolMetricsData RecordRelease(int activeCount, float lifetimeSeconds, float releaseTimeMs, float currentTime)
        {
            var result = this;
            
            // Update state
            result.ActiveCount = activeCount;
            result.TotalReleasedCount++;
            result.LastOperationTime = currentTime;
            
            // Track time
            if (result.CreationTime == 0)
                result.CreationTime = currentTime;
                
            result.UpTimeSeconds = currentTime - result.CreationTime;
            
            // Update release time metrics
            result.LastReleaseTimeMs = releaseTimeMs;
            result.TotalReleaseTimeMs += releaseTimeMs;
            result.ReleaseSampleCount++;
            
            result.MaxReleaseTimeMs = math.max(result.MaxReleaseTimeMs, releaseTimeMs);
            result.MinReleaseTimeMs = math.min(result.MinReleaseTimeMs, releaseTimeMs);
            result.AverageReleaseTimeMs = result.TotalReleaseTimeMs / result.ReleaseSampleCount;
            
            // Update lifetime metrics if applicable
            if (lifetimeSeconds > 0)
            {
                result.TotalLifetimeSeconds += lifetimeSeconds;
                result.LifetimeSampleCount++;
                result.MaxItemLifetimeSeconds = math.max(result.MaxItemLifetimeSeconds, lifetimeSeconds);
                result.MinItemLifetimeSeconds = math.min(result.MinItemLifetimeSeconds, lifetimeSeconds);
            }
            
            // Track underflow deallocations (if actively shrinking pool)
            if (activeCount < MinCapacity && MinCapacity > 0)
            {
                result.UnderflowDeallocations++;
            }
            
            return result;
        }
        
        /// <summary>
        /// Returns a new PoolMetricsData with reset statistics
        /// </summary>
        public readonly PoolMetricsData Reset(float currentTime)
        {
            return new PoolMetricsData(PoolId, Name)
            {
                PoolType = PoolType,
                Capacity = Capacity,
                InitialCapacity = InitialCapacity,
                MinCapacity = MinCapacity,
                MaxCapacity = MaxCapacity,
                EstimatedItemSizeBytes = EstimatedItemSizeBytes,
                LastResetTime = currentTime,
                CreationTime = CreationTime
            };
        }
        
        /// <summary>
        /// Determines whether this instance is equal to another
        /// </summary>
        public bool Equals(PoolMetricsData other)
        {
            return PoolId.Equals(other.PoolId) &&
                   Name.Equals(other.Name) &&
                   Capacity == other.Capacity &&
                   ActiveCount == other.ActiveCount &&
                   TotalCreatedCount == other.TotalCreatedCount &&
                   TotalAcquiredCount == other.TotalAcquiredCount &&
                   TotalReleasedCount == other.TotalReleasedCount;
        }
        
        /// <summary>
        /// Gets the hash code for this instance
        /// </summary>
        public override int GetHashCode()
        {
            return PoolId.GetHashCode();
        }
    }
    
    /// <summary>
    /// Represents a key-value pair for pool metrics in native collections
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct PoolMetricsKeyValuePair
    {
        public FixedString64Bytes PoolId;
        public PoolMetricsData Metrics;
    }
}