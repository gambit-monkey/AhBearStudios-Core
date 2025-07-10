using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Provides comprehensive statistics and performance metrics for health check factory operations.
/// Designed for thread-safe operation in production environments with high-frequency factory usage.
/// Tracks creation counts, timing metrics, error rates, and resource utilization patterns
/// to support monitoring, alerting, and performance optimization of the health check system.
/// </summary>
public sealed class HealthCheckFactoryStatistics : IDisposable
{
    #region Private Fields

    private readonly object _lockObject = new();
    private readonly ConcurrentDictionary<string, TypeStatistics> _typeStatistics = new();
    private readonly ConcurrentQueue<CreationEvent> _recentEvents = new();
    
    private volatile bool _isDisposed;
    private long _totalCreationsAttempted;
    private long _totalCreationsSucceeded;
    private long _totalCreationsFailed;
    private long _totalServicesCreated;
    private long _totalRegistriesCreated;
    private long _totalReportersCreated;
    private long _totalSchedulersCreated;
    private long _totalConfigurationsCreated;
    private long _totalCacheClears;
    private long _totalInstancesDisposed;
    
    private double _fastestCreationTimeMs = double.MaxValue;
    private double _slowestCreationTimeMs = double.MinValue;
    private double _totalCreationTimeMs;
    private DateTime _firstCreationTime = DateTime.MinValue;
    private DateTime _lastCreationTime = DateTime.MinValue;
    private DateTime _lastResetTime = DateTime.UtcNow;
    
    private readonly int _maxRecentEvents;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the HealthCheckFactoryStatistics class.
    /// </summary>
    /// <param name="maxRecentEvents">Maximum number of recent events to retain for analysis.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxRecentEvents is less than 1.</exception>
    public HealthCheckFactoryStatistics(int maxRecentEvents = 1000)
    {
        if (maxRecentEvents < 1)
            throw new ArgumentOutOfRangeException(nameof(maxRecentEvents), "Maximum recent events must be at least 1");
            
        _maxRecentEvents = maxRecentEvents;
    }

    #endregion

    #region Core Statistics Properties

    /// <summary>
    /// Gets the total number of health check creation attempts.
    /// </summary>
    public long TotalCreationsAttempted => _totalCreationsAttempted;

    /// <summary>
    /// Gets the total number of successful health check creations.
    /// </summary>
    public long TotalCreationsSucceeded => _totalCreationsSucceeded;

    /// <summary>
    /// Gets the total number of failed health check creations.
    /// </summary>
    public long TotalCreationsFailed => _totalCreationsFailed;

    /// <summary>
    /// Gets the success rate of health check creations as a percentage (0-100).
    /// </summary>
    public double CreationSuccessRate => _totalCreationsAttempted == 0 ? 0.0 : 
        (double)_totalCreationsSucceeded / _totalCreationsAttempted * 100.0;

    /// <summary>
    /// Gets the failure rate of health check creations as a percentage (0-100).
    /// </summary>
    public double CreationFailureRate => _totalCreationsAttempted == 0 ? 0.0 : 
        (double)_totalCreationsFailed / _totalCreationsAttempted * 100.0;

    /// <summary>
    /// Gets the total number of health check services created.
    /// </summary>
    public long TotalServicesCreated => _totalServicesCreated;

    /// <summary>
    /// Gets the total number of health check registries created.
    /// </summary>
    public long TotalRegistriesCreated => _totalRegistriesCreated;

    /// <summary>
    /// Gets the total number of health check reporters created.
    /// </summary>
    public long TotalReportersCreated => _totalReportersCreated;

    /// <summary>
    /// Gets the total number of health check schedulers created.
    /// </summary>
    public long TotalSchedulersCreated => _totalSchedulersCreated;

    /// <summary>
    /// Gets the total number of configurations created.
    /// </summary>
    public long TotalConfigurationsCreated => _totalConfigurationsCreated;

    /// <summary>
    /// Gets the total number of cache clear operations performed.
    /// </summary>
    public long TotalCacheClears => _totalCacheClears;

    /// <summary>
    /// Gets the total number of instances disposed during cache clears.
    /// </summary>
    public long TotalInstancesDisposed => _totalInstancesDisposed;

    #endregion

    #region Timing Statistics Properties

    /// <summary>
    /// Gets the fastest creation time recorded in milliseconds.
    /// Returns null if no creations have been recorded.
    /// </summary>
    public double? FastestCreationTimeMs => _fastestCreationTimeMs == double.MaxValue ? null : _fastestCreationTimeMs;

    /// <summary>
    /// Gets the slowest creation time recorded in milliseconds.
    /// Returns null if no creations have been recorded.
    /// </summary>
    public double? SlowestCreationTimeMs => _slowestCreationTimeMs == double.MinValue ? null : _slowestCreationTimeMs;

    /// <summary>
    /// Gets the average creation time in milliseconds.
    /// Returns null if no successful creations have been recorded.
    /// </summary>
    public double? AverageCreationTimeMs => _totalCreationsSucceeded == 0 ? null : 
        _totalCreationTimeMs / _totalCreationsSucceeded;

    /// <summary>
    /// Gets the total cumulative creation time in milliseconds.
    /// </summary>
    public double TotalCreationTimeMs => _totalCreationTimeMs;

    /// <summary>
    /// Gets the time of the first creation attempt.
    /// Returns null if no creations have been attempted.
    /// </summary>
    public DateTime? FirstCreationTime => _firstCreationTime == DateTime.MinValue ? null : _firstCreationTime;

    /// <summary>
    /// Gets the time of the most recent creation attempt.
    /// Returns null if no creations have been attempted.
    /// </summary>
    public DateTime? LastCreationTime => _lastCreationTime == DateTime.MinValue ? null : _lastCreationTime;

    /// <summary>
    /// Gets the time when statistics were last reset.
    /// </summary>
    public DateTime LastResetTime => _lastResetTime;

    /// <summary>
    /// Gets the total uptime since the first creation or last reset.
    /// </summary>
    public TimeSpan TotalUptime => DateTime.UtcNow - (_firstCreationTime == DateTime.MinValue ? _lastResetTime : _firstCreationTime);

    #endregion

    #region Performance Metrics Properties

    /// <summary>
    /// Gets the average creation rate (creations per second) over the total uptime.
    /// </summary>
    public double AverageCreationRate
    {
        get
        {
            var uptime = TotalUptime.TotalSeconds;
            return uptime <= 0 ? 0.0 : _totalCreationsAttempted / uptime;
        }
    }

    /// <summary>
    /// Gets the current creation rate (creations per second) based on recent activity.
    /// Calculated using events from the last 60 seconds.
    /// </summary>
    public double CurrentCreationRate
    {
        get
        {
            var cutoffTime = DateTime.UtcNow.AddSeconds(-60);
            var recentEvents = _recentEvents.Where(e => e.Timestamp >= cutoffTime).Count();
            return recentEvents / 60.0;
        }
    }

    /// <summary>
    /// Gets the peak creation rate (creations per second) observed in any 60-second window.
    /// </summary>
    public double PeakCreationRate => CalculatePeakCreationRate();

    /// <summary>
    /// Gets the number of unique health check types that have been created.
    /// </summary>
    public int UniqueHealthCheckTypes => _typeStatistics.Count;

    /// <summary>
    /// Gets the most frequently created health check type.
    /// Returns null if no creations have been recorded.
    /// </summary>
    public string MostCreatedType => _typeStatistics.IsEmpty ? null : 
        _typeStatistics.OrderByDescending(kvp => kvp.Value.TotalCreated).First().Key;

    /// <summary>
    /// Gets the health check type with the highest failure rate.
    /// Returns null if no failures have been recorded.
    /// </summary>
    public string HighestFailureRateType => _typeStatistics.IsEmpty ? null :
        _typeStatistics.Where(kvp => kvp.Value.TotalFailed > 0)
                      .OrderByDescending(kvp => kvp.Value.FailureRate)
                      .FirstOrDefault().Key;

    #endregion

    #region State Properties

    /// <summary>
    /// Gets whether this statistics instance has been disposed.
    /// </summary>
    public bool IsDisposed => _isDisposed;

    /// <summary>
    /// Gets the number of recent events currently retained.
    /// </summary>
    public int RecentEventCount => _recentEvents.Count;

    /// <summary>
    /// Gets the maximum number of recent events that will be retained.
    /// </summary>
    public int MaxRecentEvents => _maxRecentEvents;

    #endregion

    #region Recording Methods

    /// <summary>
    /// Records a successful health check creation.
    /// </summary>
    /// <param name="healthCheckType">The type of health check that was created.</param>
    /// <param name="creationDuration">The time taken to create the health check.</param>
    /// <param name="configurationId">Optional configuration identifier used.</param>
    /// <exception cref="ArgumentNullException">Thrown when healthCheckType is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when creationDuration is negative.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RecordSuccessfulCreation(string healthCheckType, TimeSpan creationDuration, string configurationId = null)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(healthCheckType))
            throw new ArgumentNullException(nameof(healthCheckType));
        if (creationDuration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(creationDuration), "Creation duration cannot be negative");

        var now = DateTime.UtcNow;
        var durationMs = creationDuration.TotalMilliseconds;

        // Update global counters
        System.Threading.Interlocked.Increment(ref _totalCreationsAttempted);
        System.Threading.Interlocked.Increment(ref _totalCreationsSucceeded);

        // Update timing statistics thread-safely
        lock (_lockObject)
        {
            _totalCreationTimeMs += durationMs;
            
            if (durationMs < _fastestCreationTimeMs)
                _fastestCreationTimeMs = durationMs;
            if (durationMs > _slowestCreationTimeMs)
                _slowestCreationTimeMs = durationMs;
                
            if (_firstCreationTime == DateTime.MinValue)
                _firstCreationTime = now;
            _lastCreationTime = now;
        }

        // Update type-specific statistics
        _typeStatistics.AddOrUpdate(healthCheckType,
            new TypeStatistics(healthCheckType, 1, 0, durationMs, durationMs, durationMs),
            (key, existing) => existing.RecordSuccess(durationMs));

        // Record event for rate calculations
        RecordEvent(new CreationEvent(now, healthCheckType, true, durationMs, configurationId));
    }

    /// <summary>
    /// Records a failed health check creation.
    /// </summary>
    /// <param name="healthCheckType">The type of health check that failed to be created.</param>
    /// <param name="failureReason">The reason for the creation failure.</param>
    /// <param name="configurationId">Optional configuration identifier used.</param>
    /// <exception cref="ArgumentNullException">Thrown when healthCheckType is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RecordFailedCreation(string healthCheckType, string failureReason = null, string configurationId = null)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(healthCheckType))
            throw new ArgumentNullException(nameof(healthCheckType));

        var now = DateTime.UtcNow;

        // Update global counters
        System.Threading.Interlocked.Increment(ref _totalCreationsAttempted);
        System.Threading.Interlocked.Increment(ref _totalCreationsFailed);

        // Update timing statistics
        lock (_lockObject)
        {
            if (_firstCreationTime == DateTime.MinValue)
                _firstCreationTime = now;
            _lastCreationTime = now;
        }

        // Update type-specific statistics
        _typeStatistics.AddOrUpdate(healthCheckType,
            new TypeStatistics(healthCheckType, 0, 1, 0, 0, 0),
            (key, existing) => existing.RecordFailure());

        // Record event for rate calculations
        RecordEvent(new CreationEvent(now, healthCheckType, false, 0, configurationId, failureReason));
    }

    /// <summary>
    /// Records the creation of a health check service.
    /// </summary>
    /// <param name="serviceType">The type of service that was created.</param>
    /// <param name="creationDuration">The time taken to create the service.</param>
    /// <exception cref="ArgumentNullException">Thrown when serviceType is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RecordServiceCreation(string serviceType, TimeSpan creationDuration)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(serviceType))
            throw new ArgumentNullException(nameof(serviceType));

        System.Threading.Interlocked.Increment(ref _totalServicesCreated);
        
        // Record as a general creation for timing statistics
        RecordSuccessfulCreation($"Service:{serviceType}", creationDuration);
    }

    /// <summary>
    /// Records the creation of a health check registry.
    /// </summary>
    /// <param name="allocator">The Unity allocator used for the registry.</param>
    /// <param name="initialCapacity">The initial capacity of the registry.</param>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RecordRegistryCreation(Allocator allocator, int initialCapacity)
    {
        ThrowIfDisposed();
        System.Threading.Interlocked.Increment(ref _totalRegistriesCreated);
    }

    /// <summary>
    /// Records the creation of a health check reporter.
    /// </summary>
    /// <param name="includeDefaultTargets">Whether default targets were included.</param>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RecordReporterCreation(bool includeDefaultTargets)
    {
        ThrowIfDisposed();
        System.Threading.Interlocked.Increment(ref _totalReportersCreated);
    }

    /// <summary>
    /// Records the creation of a health check scheduler.
    /// </summary>
    /// <param name="intervalSeconds">The scheduling interval in seconds.</param>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RecordSchedulerCreation(double intervalSeconds)
    {
        ThrowIfDisposed();
        System.Threading.Interlocked.Increment(ref _totalSchedulersCreated);
    }

    /// <summary>
    /// Records the creation of a configuration object.
    /// </summary>
    /// <param name="configurationType">The type of configuration that was created.</param>
    /// <exception cref="ArgumentNullException">Thrown when configurationType is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RecordConfigurationCreation(string configurationType)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(configurationType))
            throw new ArgumentNullException(nameof(configurationType));
            
        System.Threading.Interlocked.Increment(ref _totalConfigurationsCreated);
    }

    /// <summary>
    /// Records a cache clear operation.
    /// </summary>
    /// <param name="instancesDisposed">Whether instances were disposed during the clear.</param>
    /// <param name="clearedInstanceCount">The number of instances that were cleared.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when clearedInstanceCount is negative.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RecordCacheClear(bool instancesDisposed, int clearedInstanceCount)
    {
        ThrowIfDisposed();
        
        if (clearedInstanceCount < 0)
            throw new ArgumentOutOfRangeException(nameof(clearedInstanceCount), "Cleared instance count cannot be negative");

        System.Threading.Interlocked.Increment(ref _totalCacheClears);
        
        if (instancesDisposed)
        {
            System.Threading.Interlocked.Add(ref _totalInstancesDisposed, clearedInstanceCount);
        }
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Gets statistics for a specific health check type.
    /// </summary>
    /// <param name="healthCheckType">The health check type to get statistics for.</param>
    /// <returns>Statistics for the specified type, or null if the type has not been created or if no statistics are found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when healthCheckType is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    public TypeStatistics? GetTypeStatistics(string healthCheckType)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(healthCheckType))
            throw new ArgumentNullException(nameof(healthCheckType));

        return _typeStatistics.TryGetValue(healthCheckType, out var stats) ? stats : null;
    }

    /// <summary>
    /// Gets statistics for all health check types.
    /// </summary>
    /// <returns>A dictionary mapping health check types to their statistics.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    public IReadOnlyDictionary<string, TypeStatistics> GetAllTypeStatistics()
    {
        ThrowIfDisposed();
        return new Dictionary<string, TypeStatistics>(_typeStatistics);
    }

    /// <summary>
    /// Gets the top health check types by creation count.
    /// </summary>
    /// <param name="count">The number of top types to return.</param>
    /// <returns>The top health check types ordered by creation count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when count is less than 1.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    public IReadOnlyList<(string Type, long CreationCount)> GetTopTypesByCreationCount(int count = 10)
    {
        ThrowIfDisposed();
        
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1");

        return _typeStatistics
            .OrderByDescending(kvp => kvp.Value.TotalCreated)
            .Take(count)
            .Select(kvp => (kvp.Key, kvp.Value.TotalCreated))
            .ToList();
    }

    /// <summary>
    /// Gets the health check types with the highest failure rates.
    /// </summary>
    /// <param name="count">The number of types to return.</param>
    /// <param name="minimumAttempts">Minimum number of attempts required to be included.</param>
    /// <returns>The health check types with highest failure rates.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when count is less than 1 or minimumAttempts is negative.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    public IReadOnlyList<(string Type, double FailureRate)> GetTopTypesByFailureRate(int count = 10, int minimumAttempts = 5)
    {
        ThrowIfDisposed();
        
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1");
        if (minimumAttempts < 0)
            throw new ArgumentOutOfRangeException(nameof(minimumAttempts), "Minimum attempts cannot be negative");

        return _typeStatistics
            .Where(kvp => kvp.Value.TotalAttempted >= minimumAttempts)
            .OrderByDescending(kvp => kvp.Value.FailureRate)
            .Take(count)
            .Select(kvp => (kvp.Key, kvp.Value.FailureRate))
            .ToList();
    }

    /// <summary>
    /// Gets recent creation events for analysis.
    /// </summary>
    /// <param name="maxAge">Maximum age of events to include.</param>
    /// <returns>Recent creation events within the specified age.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxAge is negative.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    public IReadOnlyList<CreationEvent> GetRecentEvents(TimeSpan maxAge = default)
    {
        ThrowIfDisposed();
        
        if (maxAge < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maxAge), "Max age cannot be negative");

        var cutoffTime = maxAge == default ? DateTime.MinValue : DateTime.UtcNow - maxAge;
        return _recentEvents.Where(e => e.Timestamp >= cutoffTime).ToList();
    }

    /// <summary>
    /// Gets a comprehensive summary of all factory statistics.
    /// </summary>
    /// <returns>A dictionary containing all statistics in a structured format.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    public IReadOnlyDictionary<string, object> GetSummary()
    {
        ThrowIfDisposed();

        return new Dictionary<string, object>
        {
            ["TotalCreationsAttempted"] = TotalCreationsAttempted,
            ["TotalCreationsSucceeded"] = TotalCreationsSucceeded,
            ["TotalCreationsFailed"] = TotalCreationsFailed,
            ["CreationSuccessRate"] = CreationSuccessRate,
            ["CreationFailureRate"] = CreationFailureRate,
            ["TotalServicesCreated"] = TotalServicesCreated,
            ["TotalRegistriesCreated"] = TotalRegistriesCreated,
            ["TotalReportersCreated"] = TotalReportersCreated,
            ["TotalSchedulersCreated"] = TotalSchedulersCreated,
            ["TotalConfigurationsCreated"] = TotalConfigurationsCreated,
            ["TotalCacheClears"] = TotalCacheClears,
            ["TotalInstancesDisposed"] = TotalInstancesDisposed,
            ["FastestCreationTimeMs"] = FastestCreationTimeMs,
            ["SlowestCreationTimeMs"] = SlowestCreationTimeMs,
            ["AverageCreationTimeMs"] = AverageCreationTimeMs,
            ["TotalCreationTimeMs"] = TotalCreationTimeMs,
            ["FirstCreationTime"] = FirstCreationTime,
            ["LastCreationTime"] = LastCreationTime,
            ["LastResetTime"] = LastResetTime,
            ["TotalUptime"] = TotalUptime,
            ["AverageCreationRate"] = AverageCreationRate,
            ["CurrentCreationRate"] = CurrentCreationRate,
            ["PeakCreationRate"] = PeakCreationRate,
            ["UniqueHealthCheckTypes"] = UniqueHealthCheckTypes,
            ["MostCreatedType"] = MostCreatedType,
            ["HighestFailureRateType"] = HighestFailureRateType,
            ["RecentEventCount"] = RecentEventCount,
            ["MaxRecentEvents"] = MaxRecentEvents
        };
    }

    #endregion

    #region Management Methods

    /// <summary>
    /// Resets all statistics to their initial values.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    public void Reset()
    {
        ThrowIfDisposed();

        lock (_lockObject)
        {
            _totalCreationsAttempted = 0;
            _totalCreationsSucceeded = 0;
            _totalCreationsFailed = 0;
            _totalServicesCreated = 0;
            _totalRegistriesCreated = 0;
            _totalReportersCreated = 0;
            _totalSchedulersCreated = 0;
            _totalConfigurationsCreated = 0;
            _totalCacheClears = 0;
            _totalInstancesDisposed = 0;
            
            _fastestCreationTimeMs = double.MaxValue;
            _slowestCreationTimeMs = double.MinValue;
            _totalCreationTimeMs = 0;
            _firstCreationTime = DateTime.MinValue;
            _lastCreationTime = DateTime.MinValue;
            _lastResetTime = DateTime.UtcNow;
        }

        _typeStatistics.Clear();
        
        // Clear recent events
        while (_recentEvents.TryDequeue(out _)) { }
    }

    /// <summary>
    /// Clears recent events older than the specified age.
    /// </summary>
    /// <param name="maxAge">Maximum age of events to retain.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxAge is negative.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    public void ClearOldEvents(TimeSpan maxAge)
    {
        ThrowIfDisposed();
        
        if (maxAge < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maxAge), "Max age cannot be negative");

        var cutoffTime = DateTime.UtcNow - maxAge;
        var eventsToKeep = new List<CreationEvent>();
        
        while (_recentEvents.TryDequeue(out var eventItem))
        {
            if (eventItem.Timestamp >= cutoffTime)
                eventsToKeep.Add(eventItem);
        }
        
        foreach (var eventItem in eventsToKeep)
        {
            _recentEvents.Enqueue(eventItem);
        }
    }

    #endregion

    #region Private Methods

    private void RecordEvent(CreationEvent creationEvent)
    {
        _recentEvents.Enqueue(creationEvent);
        
        // Trim old events if we exceed the maximum
        while (_recentEvents.Count > _maxRecentEvents)
        {
            _recentEvents.TryDequeue(out _);
        }
    }

    private double CalculatePeakCreationRate()
    {
        if (_recentEvents.IsEmpty)
            return 0.0;

        var events = _recentEvents.ToArray();
        if (events.Length < 2)
            return 0.0;

        var maxRate = 0.0;
        var windowSize = TimeSpan.FromSeconds(60);
        
        for (int i = 0; i < events.Length; i++)
        {
            var windowStart = events[i].Timestamp;
            var windowEnd = windowStart + windowSize;
            var eventsInWindow = events.Count(e => e.Timestamp >= windowStart && e.Timestamp < windowEnd);
            var rate = eventsInWindow / windowSize.TotalSeconds;
            
            if (rate > maxRate)
                maxRate = rate;
        }
        
        return maxRate;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(HealthCheckFactoryStatistics));
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes the statistics instance and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _typeStatistics.Clear();
        
        while (_recentEvents.TryDequeue(out _)) { }
    }

    #endregion
}















