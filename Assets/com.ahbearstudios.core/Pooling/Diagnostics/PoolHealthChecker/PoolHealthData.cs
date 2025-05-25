using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace AhBearStudios.Pooling.Diagnostics
{
    /// <summary>
    /// Represents a health issue detected in a pool.
    /// Fully unmanaged type for compatibility with Unity Collections v2 and Burst.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile]
    [GenerateTestsForBurstCompatibility]
    public readonly struct PoolHealthIssue : IEquatable<PoolHealthIssue>
    {
        /// <summary>
        /// Maximum length of strings in the issue
        /// </summary>
        private const int MaxStringLength = 128;

        /// <summary>
        /// Unique ID for the issue
        /// </summary>
        public readonly int IssueId;

        /// <summary>
        /// Fixed capacity string for pool name
        /// </summary>
        private readonly FixedString128Bytes _poolName;

        /// <summary>
        /// Fixed capacity string for issue type
        /// </summary>
        private readonly FixedString128Bytes _issueType;

        /// <summary>
        /// Fixed capacity string for description
        /// </summary>
        private readonly FixedString128Bytes _description;

        /// <summary>
        /// Severity of the issue (0-3, where 3 is most severe)
        /// </summary>
        public readonly int Severity;

        /// <summary>
        /// Whether this issue should persist between health checks
        /// </summary>
        public readonly bool IsPersistent;

        /// <summary>
        /// Timestamp when the issue was detected (stored as ticks)
        /// </summary>
        public readonly long TimestampTicks;

        /// <summary>
        /// Guid of the affected pool
        /// </summary>
        public readonly FixedBytes16 PoolIdBytes;

        /// <summary>
        /// Gets the name of the pool with the issue
        /// </summary>
        public string PoolName => _poolName.IsEmpty ? "Unknown" : _poolName.ToString();

        /// <summary>
        /// Gets the type of the issue (e.g., "Leak", "Fragmentation", "ThreadSafety")
        /// </summary>
        public string IssueType => _issueType.IsEmpty ? "Unknown" : _issueType.ToString();

        /// <summary>
        /// Gets the description of the issue
        /// </summary>
        public string Description => _description.ToString();

        /// <summary>
        /// Gets the timestamp when the issue was detected
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new health issue
        /// </summary>
        /// <param name="poolName">Name of the affected pool</param>
        /// <param name="issueType">Type of the issue</param>
        /// <param name="description">Description of the issue</param>
        /// <param name="isPersistent">Whether this issue should persist between health checks</param>
        /// <param name="severity">Severity level (0-3)</param>
        /// <param name="timestamp">Timestamp when the issue was detected</param>
        /// <param name="poolId">Optional Guid of the pool</param>
        public PoolHealthIssue(string poolName, string issueType, string description,
            bool isPersistent, int severity, DateTime timestamp, Guid? poolId = null)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(poolName)) poolName = "Unknown";
            if (string.IsNullOrEmpty(issueType)) issueType = "Unknown";
            if (description == null) description = string.Empty;

            // Clamp severity to valid range
            severity = Math.Max(0, Math.Min(3, severity));

            // Generate a consistent hash code for the issue
            IssueId = GenerateIssueId(poolName, issueType, timestamp);

            // Initialize fixed strings with safety checks to prevent overflows
            _poolName = new FixedString128Bytes();
            _issueType = new FixedString128Bytes();
            _description = new FixedString128Bytes();

            // Safely copy strings, truncating if too long
            _poolName.CopyFrom(poolName.Substring(0, Math.Min(poolName.Length, MaxStringLength)));
            _issueType.CopyFrom(issueType.Substring(0, Math.Min(issueType.Length, MaxStringLength)));
            _description.CopyFrom(description.Substring(0, Math.Min(description.Length, MaxStringLength)));

            IsPersistent = isPersistent;
            Severity = severity;
            TimestampTicks = timestamp.Ticks;

            if (poolId.HasValue && poolId.Value != Guid.Empty)
            {
                // Create a local copy of the value to use with ref
                Guid guidValue = poolId.Value;
                PoolIdBytes = UnsafeUtility.As<Guid, FixedBytes16>(ref guidValue);
            }
            else
            {
                PoolIdBytes = default;
            }
        }

        /// <summary>
        /// Creates a new health issue with default persistence setting (false)
        /// </summary>
        /// <param name="poolName">Name of the affected pool</param>
        /// <param name="issueType">Type of the issue</param>
        /// <param name="description">Description of the issue</param>
        /// <param name="severity">Severity level (0-3)</param>
        /// <param name="timestamp">Timestamp when the issue was detected</param>
        /// <param name="poolId">Optional Guid of the pool</param>
        public PoolHealthIssue(string poolName, string issueType, string description,
            int severity, DateTime timestamp, Guid? poolId = null)
            : this(poolName, issueType, description, false, severity, timestamp, poolId)
        {
        }

        /// <summary>
        /// Generates a consistent ID for an issue based on its key properties
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="issueType">Type of the issue</param>
        /// <param name="timestamp">Timestamp of the issue</param>
        /// <returns>A hash code that uniquely identifies this issue</returns>
        private static int GenerateIssueId(string poolName, string issueType, DateTime timestamp)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (poolName?.GetHashCode() ?? 0);
                hash = hash * 31 + (issueType?.GetHashCode() ?? 0);
                hash = hash * 31 + timestamp.Ticks.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Gets the pool ID if available
        /// </summary>
        /// <returns>Pool ID or Guid.Empty if not available</returns>
        public Guid GetPoolId()
        {
            try
            {
                // Check if PoolIdBytes is default (empty)
                bool isEmpty = true;
                unsafe
                {
                    fixed (FixedBytes16* ptr = &PoolIdBytes)
                    {
                        byte* bytePtr = (byte*)ptr;
                        for (int i = 0; i < sizeof(FixedBytes16); i++)
                        {
                            if (bytePtr[i] != 0)
                            {
                                isEmpty = false;
                                break;
                            }
                        }
                    }
                }

                if (isEmpty)
                    return Guid.Empty;

                // Create a copy of the PoolIdBytes to use
                FixedBytes16 bytes = PoolIdBytes;
                return UnsafeUtility.As<FixedBytes16, Guid>(ref bytes);
            }
            catch
            {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Returns a string representation of the health issue
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return $"[{Severity}] {PoolName} - {IssueType}: {Description} ({Timestamp:yyyy-MM-dd HH:mm:ss})";
        }

        /// <summary>
        /// Creates a dictionary containing the metrics for this issue
        /// </summary>
        /// <returns>Dictionary with issue metrics</returns>
        public System.Collections.Generic.Dictionary<string, object> CreateMetricsDictionary()
        {
            var dict = new System.Collections.Generic.Dictionary<string, object>
            {
                { "IssueId", IssueId },
                { "PoolName", PoolName },
                { "IssueType", IssueType },
                { "Description", Description },
                { "Severity", Severity },
                { "IsPersistent", IsPersistent },
                { "Timestamp", Timestamp }
            };

            var poolId = GetPoolId();
            if (poolId != Guid.Empty)
            {
                dict["PoolId"] = poolId;
            }

            return dict;
        }

        /// <summary>
        /// Determines whether this issue is equal to another
        /// </summary>
        /// <param name="other">The other issue to compare</param>
        /// <returns>True if equal, false otherwise</returns>
        public bool Equals(PoolHealthIssue other)
        {
            return _poolName.Equals(other._poolName) &&
                   _issueType.Equals(other._issueType) &&
                   Severity == other.Severity &&
                   IsPersistent == other.IsPersistent;
        }

        /// <summary>
        /// Determines whether this issue is equal to another object
        /// </summary>
        /// <param name="obj">The other object to compare</param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            return obj is PoolHealthIssue other && Equals(other);
        }

        /// <summary>
        /// Gets a hash code for this health issue
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _poolName.GetHashCode();
                hashCode = (hashCode * 397) ^ _issueType.GetHashCode();
                hashCode = (hashCode * 397) ^ Severity;
                hashCode = (hashCode * 397) ^ IsPersistent.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Creates a PoolHealthIssue for memory leak detection
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="leakedCount">Number of leaked objects</param>
        /// <param name="totalCreated">Total objects created</param>
        /// <param name="isPersistent">Whether this issue should persist</param>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>A new PoolHealthIssue instance</returns>
        public static PoolHealthIssue CreateLeakIssue(string poolName, int leakedCount, int totalCreated,
            bool isPersistent, Guid poolId)
        {
            float percentage = totalCreated > 0 ? (float)leakedCount / totalCreated * 100 : 0;
            string description =
                $"Detected {leakedCount} potentially leaked objects ({percentage:F1}% of total created)";
            int severity = leakedCount > totalCreated * 0.1f ? 2 : 1;

            return new PoolHealthIssue(
                poolName,
                "MemoryLeak",
                description,
                isPersistent,
                severity,
                DateTime.UtcNow,
                poolId);
        }

        /// <summary>
        /// Creates a PoolHealthIssue for high usage detection
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="usage">Current usage ratio</param>
        /// <param name="threshold">Usage threshold</param>
        /// <param name="capacity">Total capacity</param>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>A new PoolHealthIssue instance</returns>
        public static PoolHealthIssue CreateHighUsageIssue(string poolName, float usage, float threshold, int capacity,
            Guid poolId)
        {
            string description = $"Pool usage at {usage:P0} exceeds threshold of {threshold:P0} (capacity: {capacity})";
            int severity = usage > 0.95f ? 2 : 1;

            return new PoolHealthIssue(
                poolName,
                "HighUsage",
                description,
                false, // High usage is typically transient
                severity,
                DateTime.UtcNow,
                poolId);
        }

        /// <summary>
        /// Creates a PoolHealthIssue for fragmentation detection
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="fragmentation">Fragmentation ratio</param>
        /// <param name="fragmentCount">Number of fragments</param>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>A new PoolHealthIssue instance</returns>
        public static PoolHealthIssue CreateFragmentationIssue(string poolName, float fragmentation, int fragmentCount,
            Guid poolId)
        {
            string description = $"Pool fragmentation at {fragmentation:P0} with {fragmentCount} fragments";
            int severity = fragmentation > 0.5f ? 2 : 1;

            return new PoolHealthIssue(
                poolName,
                "Fragmentation",
                description,
                false,
                severity,
                DateTime.UtcNow,
                poolId);
        }

        /// <summary>
        /// Creates a PoolHealthIssue for performance detection
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="operationType">Type of operation</param>
        /// <param name="duration">Duration in milliseconds</param>
        /// <param name="threshold">Threshold in milliseconds</param>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>A new PoolHealthIssue instance</returns>
        public static PoolHealthIssue CreatePerformanceIssue(string poolName, string operationType, float duration,
            float threshold, Guid poolId)
        {
            string description = $"Slow {operationType} operation: {duration:F2}ms (threshold: {threshold:F2}ms)";
            int severity = duration > threshold * 2 ? 2 : 1;

            return new PoolHealthIssue(
                poolName,
                "SlowOperation",
                description,
                false,
                severity,
                DateTime.UtcNow,
                poolId);
        }
    }

    /// <summary>
    /// Internal struct for tracking pool health state
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile]
    [GenerateTestsForBurstCompatibility]
    public struct PoolHealthState
    {
        public float LeakProbability;
        public int LastLeakSize;
        public float PeakUsage;
        public float UsageVariance;
        public float LastCheckTimestamp;
        public int ConsecutiveLeakDetections;
        public int IssueCount;
        public int ThreadContentionCount;
        public float LastFragmentationRatio;
    }

    /// <summary>
    /// Internal struct for adaptive thresholds
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile]
    [GenerateTestsForBurstCompatibility]
    public struct AdaptiveThresholds
    {
        public float HighUsageThreshold;
        public float SlowOperationThreshold;
        public float LeakThresholdPercent;
        public int SampleCount;
        public bool Enabled;

        public void AdjustThresholds(float observedUsage, float observedOperationTime, float leakPercent)
        {
            if (!Enabled || SampleCount >= 100)
                return;

            // Only adjust during learning phase (first 100 samples)
            SampleCount++;

            // Gradually adjust thresholds based on observations
            if (SampleCount < 10)
            {
                // Initial learning phase, just record
                HighUsageThreshold = math.max(HighUsageThreshold, observedUsage * 1.2f);
                SlowOperationThreshold = math.max(SlowOperationThreshold, observedOperationTime * 1.5f);
                LeakThresholdPercent = math.max(LeakThresholdPercent, leakPercent * 2f);
            }
            else
            {
                // Refinement phase - weighted average
                float weight = math.min(0.1f, 1.0f / SampleCount);
                HighUsageThreshold = math.lerp(HighUsageThreshold, observedUsage * 1.2f, weight);
                SlowOperationThreshold = math.lerp(SlowOperationThreshold, observedOperationTime * 1.5f, weight);
                LeakThresholdPercent = math.lerp(LeakThresholdPercent, leakPercent * 2f, weight);

                // Ensure minimums
                HighUsageThreshold = math.max(0.5f, HighUsageThreshold);
                SlowOperationThreshold = math.max(0.5f, SlowOperationThreshold);
                LeakThresholdPercent = math.max(0.01f, LeakThresholdPercent);
            }
        }
    }

    /// <summary>
    /// Represents a historical health data point for a pool
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile]
    [GenerateTestsForBurstCompatibility]
    public readonly struct PoolHistoryPoint
    {
        /// <summary>
        /// Gets the timestamp in seconds since the start of the application
        /// </summary>
        public readonly float TimestampSeconds { get; }

        /// <summary>
        /// Gets the usage ratio (0-1) at the time of recording
        /// </summary>
        public readonly float UsageRatio { get; }

        /// <summary>
        /// Gets the number of active objects at the time of recording
        /// </summary>
        public readonly int ActiveCount { get; }

        /// <summary>
        /// Gets the number of leaked objects at the time of recording
        /// </summary>
        public readonly int LeakCount { get; }

        /// <summary>
        /// Gets the average acquire time (ms) at the time of recording
        /// </summary>
        public readonly float AcquireTime { get; }

        /// <summary>
        /// Gets the fragmentation ratio (0-1) at the time of recording
        /// </summary>
        public readonly float FragmentationRatio { get; }

        /// <summary>
        /// Initializes a new instance of the PoolHistoryPoint struct using a float timestamp
        /// </summary>
        public PoolHistoryPoint(
            float timestampSeconds,
            float usageRatio,
            int activeCount,
            int leakCount,
            float acquireTime,
            float fragmentationRatio)
        {
            TimestampSeconds = timestampSeconds;
            UsageRatio = usageRatio;
            ActiveCount = activeCount;
            LeakCount = leakCount;
            AcquireTime = acquireTime;
            FragmentationRatio = fragmentationRatio;
        }

        /// <summary>
        /// Initializes a new instance of the PoolHistoryPoint struct using the current time
        /// </summary>
        public PoolHistoryPoint(
            float usageRatio,
            int activeCount,
            int leakCount,
            float acquireTime,
            float fragmentationRatio)
        {
            TimestampSeconds = UnityEngine.Time.realtimeSinceStartup;
            UsageRatio = usageRatio;
            ActiveCount = activeCount;
            LeakCount = leakCount;
            AcquireTime = acquireTime;
            FragmentationRatio = fragmentationRatio;
        }

        /// <summary>
        /// Gets an approximate DateTime representation of the timestamp for display purposes
        /// Note: This method cannot be used in Burst-compiled code
        /// </summary>
        /// <returns>DateTime representation of the timestamp</returns>
        [BurstDiscard]
        public DateTime GetDateTimeForDisplay()
        {
            // This is a rough approximation - in a real implementation you might
            // track the start time of the application and add the timestamp to it
            return DateTime.Now.AddSeconds(-(UnityEngine.Time.realtimeSinceStartup - TimestampSeconds));
        }

        /// <summary>
        /// Returns a dictionary representation of this history point
        /// Note: This method cannot be used in Burst-compiled code
        /// </summary>
        /// <returns>Dictionary with history point data</returns>
        [BurstDiscard]
        public System.Collections.Generic.Dictionary<string, object> ToDictionary()
        {
            return new System.Collections.Generic.Dictionary<string, object>
            {
                { "TimestampSeconds", TimestampSeconds },
                { "DisplayTimestamp", GetDateTimeForDisplay() },
                { "UsageRatio", UsageRatio },
                { "ActiveCount", ActiveCount },
                { "LeakCount", LeakCount },
                { "AcquireTime", AcquireTime },
                { "FragmentationRatio", FragmentationRatio }
            };
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is PoolHistoryPoint other)
            {
                return Equals(other);
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified PoolHistoryPoint is equal to the current object
        /// </summary>
        public bool Equals(PoolHistoryPoint other)
        {
            return Math.Abs(TimestampSeconds - other.TimestampSeconds) < 0.001f &&
                   Math.Abs(UsageRatio - other.UsageRatio) < 0.001f &&
                   ActiveCount == other.ActiveCount &&
                   LeakCount == other.LeakCount &&
                   Math.Abs(AcquireTime - other.AcquireTime) < 0.001f &&
                   Math.Abs(FragmentationRatio - other.FragmentationRatio) < 0.001f;
        }

        /// <summary>
        /// Returns the hash code for this instance
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + TimestampSeconds.GetHashCode();
                hash = hash * 23 + UsageRatio.GetHashCode();
                hash = hash * 23 + ActiveCount.GetHashCode();
                hash = hash * 23 + LeakCount.GetHashCode();
                hash = hash * 23 + AcquireTime.GetHashCode();
                hash = hash * 23 + FragmentationRatio.GetHashCode();
                return hash;
            }
        }
    }
}