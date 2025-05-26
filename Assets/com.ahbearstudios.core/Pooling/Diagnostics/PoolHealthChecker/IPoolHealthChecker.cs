
using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Core;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Diagnostics
{
    /// <summary>
    /// Interface defining operations for monitoring and validating the health of object pools.
    /// Implementations will identify issues such as memory leaks, excessive allocations,
    /// and performance bottlenecks.
    /// </summary>
    public interface IPoolHealthChecker
    {
        /// <summary>
        /// Gets or sets the interval in seconds between automatic health checks.
        /// </summary>
        float CheckInterval { get; set; }

        /// <summary>
        /// Gets or sets whether warning messages should be logged.
        /// </summary>
        bool LogWarnings { get; set; }

        /// <summary>
        /// Gets or sets whether to alert on potential memory leaks.
        /// </summary>
        bool AlertOnLeaks { get; set; }

        /// <summary>
        /// Gets or sets whether to alert on high pool usage.
        /// </summary>
        bool AlertOnHighUsage { get; set; }

        /// <summary>
        /// Gets or sets the threshold (0-1) at which high pool usage alerts are triggered.
        /// </summary>
        float HighUsageThreshold { get; set; }

        /// <summary>
        /// Gets or sets whether to alert on performance issues.
        /// </summary>
        bool AlertOnPerformanceIssues { get; set; }

        /// <summary>
        /// Gets or sets the threshold in milliseconds for slow acquire operations.
        /// </summary>
        float SlowAcquireThreshold { get; set; }

        /// <summary>
        /// Gets or sets whether to alert on pool fragmentation.
        /// </summary>
        bool AlertOnFragmentation { get; set; }

        /// <summary>
        /// Gets or sets whether to alert on thread contention issues.
        /// </summary>
        bool AlertOnThreadContention { get; set; }

        /// <summary>
        /// Gets or sets whether to enable adaptive thresholds that adjust based on pool behavior.
        /// </summary>
        bool EnableAdaptiveThresholds { get; set; }

        /// <summary>
        /// Event triggered when a new health issue is detected.
        /// </summary>
        event Action<PoolHealthIssue> OnIssueDetected;

        /// <summary>
        /// Event triggered when the issue count for a pool changes.
        /// </summary>
        event Action<Guid, int> OnIssueCountChanged;

        /// <summary>
        /// Event triggered when a critical issue (severity >= 2) is detected.
        /// </summary>
        event Action<Guid> OnCriticalIssueDetected;

        /// <summary>
        /// Sets the time interval between automatic health checks.
        /// </summary>
        /// <param name="interval">Interval in seconds</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if interval is less than or equal to zero</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the health checker has been disposed</exception>
        void SetCheckInterval(float interval);

        /// <summary>
        /// Configures alert behaviors for the health checker.
        /// </summary>
        /// <param name="alertOnLeaks">Whether to alert on leaks</param>
        /// <param name="alertOnHighUsage">Whether to alert on high usage</param>
        /// <param name="logWarnings">Whether to log warnings</param>
        /// <exception cref="ObjectDisposedException">Thrown if the health checker has been disposed</exception>
        void SetAlertFlags(bool alertOnLeaks, bool alertOnHighUsage, bool logWarnings);

        /// <summary>
        /// Checks health of all registered pools.
        /// </summary>
        /// <returns>List of pool health issues found</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the health checker has been disposed</exception>
        List<PoolHealthIssue> CheckAllPools();

        /// <summary>
        /// Checks health of a specific pool and returns any issues found.
        /// </summary>
        /// <param name="pool">The pool to check</param>
        /// <returns>List of pool health issues detected</returns>
        /// <exception cref="ArgumentNullException">Thrown if pool is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the health checker has been disposed</exception>
        List<PoolHealthIssue> CheckPoolHealth(IPool pool);
        
        /// <summary>
        /// Gets the health data for a specific pool
        /// </summary>
        /// <param name="poolId">The pool ID to get health data for</param>
        /// <returns>Dictionary containing health data or null if not found</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the health checker has been disposed</exception>
        Dictionary<string, object> GetPoolHealth(Guid poolId);

        /// <summary>
        /// Updates the health checker and performs automatic checks if needed based on CheckInterval.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the health checker has been disposed</exception>
        void Update();

        /// <summary>
        /// Clears all health issues, both current and persistent.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the health checker has been disposed</exception>
        void ClearAllIssues();

        /// <summary>
        /// Clears all health issues for a specific pool.
        /// </summary>
        /// <param name="poolId">ID of the pool to clear issues for</param>
        /// <exception cref="ObjectDisposedException">Thrown if the health checker has been disposed</exception>
        void ClearIssuesForPool(Guid poolId);

        /// <summary>
        /// Gets all currently tracked health issues.
        /// </summary>
        /// <returns>List of all current health issues</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the health checker has been disposed</exception>
        List<PoolHealthIssue> GetCurrentIssues();

        /// <summary>
        /// Gets the count of issues for a specific pool.
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <returns>Number of current issues for the pool</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the health checker has been disposed</exception>
        int GetIssueCountForPool(Guid poolId);

        /// <summary>
        /// Tag a pool with a category for visualization grouping.
        /// </summary>
        /// <param name="poolId">ID of the pool to tag</param>
        /// <param name="tag">Tag to apply to the pool</param>
        /// <exception cref="ObjectDisposedException">Thrown if the health checker has been disposed</exception>
        void TagPool(Guid poolId, string tag);

        /// <summary>
        /// Gets all pools with a specific tag.
        /// </summary>
        /// <param name="tag">Tag to filter by</param>
        /// <returns>List of pool IDs with the specified tag</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the health checker has been disposed</exception>
        List<Guid> GetPoolsByTag(string tag);

        /// <summary>
        /// Gets historical health data for a specific pool.
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="maxPoints">Maximum number of data points to return</param>
        /// <returns>List of historical health data points</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the health checker has been disposed</exception>
        List<Dictionary<string, object>> GetHealthHistory(Guid poolId, int maxPoints = 100);

        /// <summary>
        /// Enables or disables adaptive thresholds for a specific pool.
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="enable">Whether to enable adaptive thresholds</param>
        /// <exception cref="ObjectDisposedException">Thrown if the health checker has been disposed</exception>
        void SetAdaptiveThresholds(Guid poolId, bool enable);

        /// <summary>
        /// Exports a complete health report for visualization.
        /// </summary>
        /// <param name="includeHistory">Whether to include historical data</param>
        /// <returns>A dictionary containing the complete health report</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the health checker has been disposed</exception>
        Dictionary<string, object> ExportHealthReport(bool includeHistory = false);
    }
}