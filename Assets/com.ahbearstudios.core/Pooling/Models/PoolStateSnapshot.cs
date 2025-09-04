using System;
using System.Collections.Generic;
using Unity.Collections;
using MemoryPack;

namespace AhBearStudios.Core.Pooling.Models
{
    /// <summary>
    /// Represents a complete snapshot of pool state for persistence and recovery.
    /// Contains all essential pool information needed to restore pool operation.
    /// Optimized for serialization with MemoryPack for high performance.
    /// Follows CLAUDE.md patterns for pool state management and recovery.
    /// </summary>
    [MemoryPackable]
    public partial class PoolStateSnapshot
    {
        #region Core Pool Information

        /// <summary>
        /// Gets or sets the unique identifier for this pool.
        /// </summary>
        [MemoryPackOrder(0)]
        public Guid PoolId { get; set; }

        /// <summary>
        /// Gets or sets the pool name.
        /// </summary>
        [MemoryPackOrder(1)]
        public string PoolName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the pool type name (full type name of pooled objects).
        /// </summary>
        [MemoryPackOrder(2)]
        public string PoolType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current pool strategy name.
        /// </summary>
        [MemoryPackOrder(3)]
        public string StrategyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when this snapshot was created.
        /// </summary>
        [MemoryPackOrder(4)]
        public DateTime SnapshotTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the pool version for compatibility tracking.
        /// </summary>
        [MemoryPackOrder(5)]
        public int PoolVersion { get; set; }

        #endregion

        #region Pool Statistics

        /// <summary>
        /// Gets or sets the complete pool statistics at snapshot time.
        /// </summary>
        [MemoryPackOrder(6)]
        public PoolStatistics Statistics { get; set; } = new();

        #endregion

        #region Pool Configuration State

        /// <summary>
        /// Gets or sets the initial pool capacity.
        /// </summary>
        [MemoryPackOrder(7)]
        public int InitialCapacity { get; set; }

        /// <summary>
        /// Gets or sets the maximum pool capacity.
        /// </summary>
        [MemoryPackOrder(8)]
        public int MaxCapacity { get; set; }

        /// <summary>
        /// Gets or sets the minimum pool capacity.
        /// </summary>
        [MemoryPackOrder(9)]
        public int MinCapacity { get; set; }

        /// <summary>
        /// Gets or sets whether auto-scaling is enabled.
        /// </summary>
        [MemoryPackOrder(10)]
        public bool AutoScalingEnabled { get; set; }

        /// <summary>
        /// Gets or sets the expansion threshold percentage (0-100).
        /// </summary>
        [MemoryPackOrder(11)]
        public double ExpansionThreshold { get; set; }

        /// <summary>
        /// Gets or sets the contraction threshold percentage (0-100).
        /// </summary>
        [MemoryPackOrder(12)]
        public double ContractionThreshold { get; set; }

        #endregion

        #region Pool Health State

        /// <summary>
        /// Gets or sets the current pool health status.
        /// </summary>
        [MemoryPackOrder(13)]
        public string HealthStatus { get; set; } = "Unknown";

        /// <summary>
        /// Gets or sets the timestamp of the last health check.
        /// </summary>
        [MemoryPackOrder(14)]
        public DateTime LastHealthCheck { get; set; }

        /// <summary>
        /// Gets or sets whether the pool circuit breaker is open.
        /// </summary>
        [MemoryPackOrder(15)]
        public bool CircuitBreakerOpen { get; set; }

        /// <summary>
        /// Gets or sets the number of consecutive health check failures.
        /// </summary>
        [MemoryPackOrder(16)]
        public int ConsecutiveFailures { get; set; }

        #endregion

        #region Performance Metrics

        /// <summary>
        /// Gets or sets the average get operation duration in milliseconds.
        /// </summary>
        [MemoryPackOrder(17)]
        public double AverageGetDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the average return operation duration in milliseconds.
        /// </summary>
        [MemoryPackOrder(18)]
        public double AverageReturnDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the peak memory usage in bytes.
        /// </summary>
        [MemoryPackOrder(19)]
        public long PeakMemoryUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets the current memory usage in bytes.
        /// </summary>
        [MemoryPackOrder(20)]
        public long CurrentMemoryUsageBytes { get; set; }

        #endregion

        #region Validation and Recovery Information

        /// <summary>
        /// Gets or sets the number of validation failures since creation.
        /// </summary>
        [MemoryPackOrder(21)]
        public long ValidationFailures { get; set; }

        /// <summary>
        /// Gets or sets the number of recovery operations performed.
        /// </summary>
        [MemoryPackOrder(22)]
        public long RecoveryOperations { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last recovery operation.
        /// </summary>
        [MemoryPackOrder(23)]
        public DateTime LastRecoveryTimestamp { get; set; }

        /// <summary>
        /// Gets or sets custom properties for pool-specific data.
        /// </summary>
        [MemoryPackOrder(24)]
        public Dictionary<string, string> CustomProperties { get; set; } = new();

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a new pool state snapshot with current timestamp.
        /// </summary>
        /// <param name="poolId">The pool identifier</param>
        /// <param name="poolName">The pool name</param>
        /// <param name="poolType">The pool type</param>
        /// <param name="strategyName">The pool strategy name</param>
        /// <returns>A new PoolStateSnapshot instance</returns>
        public static PoolStateSnapshot Create(
            Guid poolId,
            string poolName,
            string poolType,
            string strategyName)
        {
            return new PoolStateSnapshot
            {
                PoolId = poolId,
                PoolName = poolName ?? string.Empty,
                PoolType = poolType ?? string.Empty,
                StrategyName = strategyName ?? string.Empty,
                SnapshotTimestamp = DateTime.UtcNow,
                PoolVersion = 1,
                Statistics = new PoolStatistics(),
                CustomProperties = new Dictionary<string, string>()
            };
        }

        /// <summary>
        /// Creates a pool state snapshot from pool statistics and configuration.
        /// </summary>
        /// <param name="poolId">The pool identifier</param>
        /// <param name="poolName">The pool name</param>
        /// <param name="poolType">The pool type</param>
        /// <param name="strategyName">The pool strategy name</param>
        /// <param name="statistics">The current pool statistics</param>
        /// <param name="initialCapacity">The initial pool capacity</param>
        /// <param name="maxCapacity">The maximum pool capacity</param>
        /// <param name="minCapacity">The minimum pool capacity</param>
        /// <returns>A new PoolStateSnapshot instance with provided data</returns>
        public static PoolStateSnapshot FromPoolData(
            Guid poolId,
            string poolName,
            string poolType,
            string strategyName,
            PoolStatistics statistics,
            int initialCapacity,
            int maxCapacity,
            int minCapacity)
        {
            return new PoolStateSnapshot
            {
                PoolId = poolId,
                PoolName = poolName ?? string.Empty,
                PoolType = poolType ?? string.Empty,
                StrategyName = strategyName ?? string.Empty,
                SnapshotTimestamp = DateTime.UtcNow,
                PoolVersion = 1,
                Statistics = statistics ?? new PoolStatistics(),
                InitialCapacity = initialCapacity,
                MaxCapacity = maxCapacity,
                MinCapacity = minCapacity,
                CustomProperties = new Dictionary<string, string>()
            };
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Validates that the snapshot contains valid data.
        /// </summary>
        /// <returns>True if the snapshot is valid</returns>
        public bool IsValid()
        {
            return PoolId != Guid.Empty
                && !string.IsNullOrWhiteSpace(PoolType)
                && Statistics != null
                && SnapshotTimestamp != default
                && MaxCapacity >= InitialCapacity
                && InitialCapacity >= MinCapacity
                && MinCapacity >= 0;
        }

        /// <summary>
        /// Gets the age of this snapshot.
        /// </summary>
        /// <returns>The age as a TimeSpan</returns>
        public TimeSpan Age => DateTime.UtcNow - SnapshotTimestamp;

        /// <summary>
        /// Determines if this snapshot is stale based on the provided threshold.
        /// </summary>
        /// <param name="staleThreshold">The threshold for considering a snapshot stale</param>
        /// <returns>True if the snapshot is stale</returns>
        public bool IsStale(TimeSpan staleThreshold)
        {
            return Age > staleThreshold;
        }

        /// <summary>
        /// Updates the snapshot timestamp to current time.
        /// </summary>
        public void RefreshTimestamp()
        {
            SnapshotTimestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a summary string for logging and debugging.
        /// </summary>
        /// <returns>A formatted summary of the snapshot</returns>
        public string GetSummary()
        {
            return $"Pool: {PoolName} ({PoolType}) | " +
                   $"Strategy: {StrategyName} | " +
                   $"Active: {Statistics.ActiveCount}/{Statistics.TotalCount} | " +
                   $"Hit Ratio: {Statistics.HitRatio:F1}% | " +
                   $"Health: {HealthStatus} | " +
                   $"Age: {Age.TotalMinutes:F1}m";
        }

        /// <summary>
        /// Adds or updates a custom property.
        /// </summary>
        /// <param name="key">The property key</param>
        /// <param name="value">The property value</param>
        public void SetCustomProperty(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                CustomProperties[key] = value ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets a custom property value.
        /// </summary>
        /// <param name="key">The property key</param>
        /// <param name="defaultValue">The default value if key not found</param>
        /// <returns>The property value or default</returns>
        public string GetCustomProperty(string key, string defaultValue = "")
        {
            return CustomProperties.TryGetValue(key, out var value) ? value : defaultValue;
        }

        #endregion
    }
}