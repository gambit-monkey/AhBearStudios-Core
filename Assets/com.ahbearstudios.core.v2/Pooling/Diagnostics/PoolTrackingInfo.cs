using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Diagnostics
{
    /// <summary>
    /// Structure containing tracking information for a pool.
    /// </summary>
    public struct PoolTrackingInfo
    {
        public FixedString64Bytes PoolId;  // Store GUID as FixedString
        public FixedString64Bytes PoolName;  // Use fixed size string instead
        public int TypeHash;  // Store type hash instead of Type reference
        public long RegistrationTicks;  // Store DateTime as ticks
        public Allocator Allocator;
        public bool IsCreated;
        public int TotalCreated;
        public int TotalDestroyed;
        public int TotalCapacity;
        public int TotalAcquired;
        public int TotalReleased;
        public int ActiveCount;
        public int PeakActiveCount;
        public float AcquireStartTime;
        public float LastAcquireTime;
        public float MaxAcquireTime;
        public float TotalAcquireTime;
        public float LastReleaseTimeMs;  // Fixed naming to match usage
        public float MaxReleaseTimeMs;  // Fixed naming to match usage
        public float TotalReleaseTime;
        public int EstimatedItemSizeBytes;
    }
}