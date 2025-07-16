using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Filters
{
    /// <summary>
    /// Service for managing and applying multiple log filters.
    /// Provides centralized filter management with priority-based execution and performance monitoring.
    /// Supports dynamic filter configuration and real-time filter management.
    /// </summary>
    public sealed class LogFilterService : IDisposable
    {
        private readonly List<ILogFilter> _filters = new();
        private readonly ReaderWriterLockSlim _filtersLock = new();
        private readonly FilterStatistics _globalStatistics = new();
        private volatile bool _isEnabled = true;
        private volatile bool _disposed = false;

        /// <summary>
        /// Gets whether the filter service is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// Gets the number of registered filters.
        /// </summary>
        public int FilterCount
        {
            get
            {
                _filtersLock.EnterReadLock();
                try
                {
                    return _filters.Count;
                }
                finally
                {
                    _filtersLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the global filter statistics.
        /// </summary>
        public FilterStatistics GlobalStatistics => _globalStatistics.CreateSnapshot();

        /// <summary>
        /// Gets a read-only list of all registered filters.
        /// </summary>
        public IReadOnlyList<ILogFilter> Filters
        {
            get
            {
                _filtersLock.EnterReadLock();
                try
                {
                    return _filters.ToArray();
                }
                finally
                {
                    _filtersLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the LogFilterService class.
        /// </summary>
        public LogFilterService()
        {
        }

        /// <summary>
        /// Initializes a new instance of the LogFilterService class with initial filters.
        /// </summary>
        /// <param name="filters">The initial filters to add</param>
        public LogFilterService(IEnumerable<ILogFilter> filters)
        {
            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    AddFilter(filter);
                }
            }
        }

        /// <summary>
        /// Adds a filter to the service.
        /// </summary>
        /// <param name="filter">The filter to add</param>
        /// <exception cref="ArgumentNullException">Thrown when filter is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the service is disposed</exception>
        public void AddFilter(ILogFilter filter)
        {
            ThrowIfDisposed();
            
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            _filtersLock.EnterWriteLock();
            try
            {
                // Check if filter with same name already exists
                if (_filters.Any(f => f.Name.Equals(filter.Name)))
                {
                    throw new InvalidOperationException($"Filter with name '{filter.Name}' already exists");
                }

                _filters.Add(filter);
                
                // Sort filters by priority (higher priority first)
                _filters.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
            finally
            {
                _filtersLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes a filter from the service.
        /// </summary>
        /// <param name="filterName">The name of the filter to remove</param>
        /// <returns>True if the filter was found and removed</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the service is disposed</exception>
        public bool RemoveFilter(FixedString64Bytes filterName)
        {
            ThrowIfDisposed();

            _filtersLock.EnterWriteLock();
            try
            {
                var index = _filters.FindIndex(f => f.Name.Equals(filterName));
                if (index >= 0)
                {
                    _filters.RemoveAt(index);
                    return true;
                }
                return false;
            }
            finally
            {
                _filtersLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes a filter from the service.
        /// </summary>
        /// <param name="filter">The filter to remove</param>
        /// <returns>True if the filter was found and removed</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the service is disposed</exception>
        public bool RemoveFilter(ILogFilter filter)
        {
            ThrowIfDisposed();
            
            if (filter == null)
                return false;

            _filtersLock.EnterWriteLock();
            try
            {
                return _filters.Remove(filter);
            }
            finally
            {
                _filtersLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets a filter by name.
        /// </summary>
        /// <param name="filterName">The name of the filter to get</param>
        /// <returns>The filter if found, otherwise null</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the service is disposed</exception>
        public ILogFilter GetFilter(FixedString64Bytes filterName)
        {
            ThrowIfDisposed();

            _filtersLock.EnterReadLock();
            try
            {
                return _filters.FirstOrDefault(f => f.Name.Equals(filterName));
            }
            finally
            {
                _filtersLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Clears all filters from the service.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the service is disposed</exception>
        public void ClearFilters()
        {
            ThrowIfDisposed();

            _filtersLock.EnterWriteLock();
            try
            {
                _filters.Clear();
            }
            finally
            {
                _filtersLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Determines if a log entry should be processed by evaluating all filters.
        /// </summary>
        /// <param name="entry">The log entry to evaluate</param>
        /// <param name="correlationId">The correlation ID for tracking</param>
        /// <returns>True if the entry should be processed, false otherwise</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the service is disposed</exception>
        public bool ShouldProcess(LogEntry entry, FixedString64Bytes correlationId = default)
        {
            ThrowIfDisposed();

            if (!_isEnabled)
            {
                _globalStatistics.RecordAllowed();
                return true;
            }

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _filtersLock.EnterReadLock();
                try
                {
                    // If no filters, allow all entries
                    if (_filters.Count == 0)
                    {
                        _globalStatistics.RecordAllowed(stopwatch.Elapsed);
                        return true;
                    }

                    // Apply filters in priority order
                    foreach (var filter in _filters)
                    {
                        if (!filter.IsEnabled)
                            continue;

                        try
                        {
                            if (!filter.ShouldProcess(entry, correlationId))
                            {
                                stopwatch.Stop();
                                _globalStatistics.RecordBlocked(stopwatch.Elapsed);
                                return false;
                            }
                        }
                        catch (Exception)
                        {
                            // If a filter throws an exception, continue with the next filter
                            // This prevents one bad filter from breaking the entire pipeline
                            continue;
                        }
                    }

                    // If all filters allow the entry, it should be processed
                    stopwatch.Stop();
                    _globalStatistics.RecordAllowed(stopwatch.Elapsed);
                    return true;
                }
                finally
                {
                    _filtersLock.ExitReadLock();
                }
            }
            catch (Exception)
            {
                stopwatch.Stop();
                _globalStatistics.RecordError(stopwatch.Elapsed);
                
                // On error, allow the entry to pass through to prevent log loss
                return true;
            }
        }

        /// <summary>
        /// Gets statistics for all filters.
        /// </summary>
        /// <returns>A dictionary of filter names to their statistics</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the service is disposed</exception>
        public IReadOnlyDictionary<string, FilterStatistics> GetFilterStatistics()
        {
            ThrowIfDisposed();

            _filtersLock.EnterReadLock();
            try
            {
                var statistics = new Dictionary<string, FilterStatistics>();
                
                foreach (var filter in _filters)
                {
                    try
                    {
                        statistics[filter.Name.ToString()] = filter.GetStatistics();
                    }
                    catch (Exception)
                    {
                        // If getting statistics fails, skip this filter
                        continue;
                    }
                }

                return statistics;
            }
            finally
            {
                _filtersLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Validates all filters in the service.
        /// </summary>
        /// <param name="correlationId">The correlation ID for tracking</param>
        /// <returns>A dictionary of filter names to their validation results</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the service is disposed</exception>
        public IReadOnlyDictionary<string, ValidationResult> ValidateFilters(FixedString64Bytes correlationId = default)
        {
            ThrowIfDisposed();

            _filtersLock.EnterReadLock();
            try
            {
                var results = new Dictionary<string, ValidationResult>();
                
                foreach (var filter in _filters)
                {
                    try
                    {
                        results[filter.Name.ToString()] = filter.Validate(correlationId);
                    }
                    catch (Exception ex)
                    {
                        var errors = new List<ValidationError> 
                        { 
                            new ValidationError($"Exception during validation: {ex.Message}", "Filter") 
                        };
                        results[filter.Name.ToString()] = ValidationResult.Failure(errors, filter.Name.ToString());
                    }
                }

                return results;
            }
            finally
            {
                _filtersLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Resets all filters in the service.
        /// </summary>
        /// <param name="correlationId">The correlation ID for tracking</param>
        /// <exception cref="ObjectDisposedException">Thrown when the service is disposed</exception>
        public void ResetFilters(FixedString64Bytes correlationId = default)
        {
            ThrowIfDisposed();

            _filtersLock.EnterReadLock();
            try
            {
                foreach (var filter in _filters)
                {
                    try
                    {
                        filter.Reset(correlationId);
                    }
                    catch (Exception)
                    {
                        // If reset fails, continue with other filters
                        continue;
                    }
                }
            }
            finally
            {
                _filtersLock.ExitReadLock();
            }

            _globalStatistics.Reset();
        }

        /// <summary>
        /// Enables or disables a specific filter.
        /// </summary>
        /// <param name="filterName">The name of the filter</param>
        /// <param name="enabled">Whether to enable or disable the filter</param>
        /// <returns>True if the filter was found and updated</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the service is disposed</exception>
        public bool SetFilterEnabled(FixedString64Bytes filterName, bool enabled)
        {
            ThrowIfDisposed();

            _filtersLock.EnterReadLock();
            try
            {
                var filter = _filters.FirstOrDefault(f => f.Name.Equals(filterName));
                if (filter != null)
                {
                    filter.IsEnabled = enabled;
                    return true;
                }
                return false;
            }
            finally
            {
                _filtersLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets the enabled state of a specific filter.
        /// </summary>
        /// <param name="filterName">The name of the filter</param>
        /// <returns>True if the filter is enabled, false if disabled or not found</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the service is disposed</exception>
        public bool IsFilterEnabled(FixedString64Bytes filterName)
        {
            ThrowIfDisposed();

            _filtersLock.EnterReadLock();
            try
            {
                var filter = _filters.FirstOrDefault(f => f.Name.Equals(filterName));
                return filter?.IsEnabled ?? false;
            }
            finally
            {
                _filtersLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets a summary of the filter service state.
        /// </summary>
        /// <returns>A formatted summary string</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the service is disposed</exception>
        public string GetSummary()
        {
            ThrowIfDisposed();

            _filtersLock.EnterReadLock();
            try
            {
                var enabledCount = _filters.Count(f => f.IsEnabled);
                var disabledCount = _filters.Count - enabledCount;
                var globalStats = _globalStatistics.GetSummary();
                
                return $"LogFilterService: {_filters.Count} filters ({enabledCount} enabled, {disabledCount} disabled), " +
                       $"Service enabled: {_isEnabled}, Global {globalStats}";
            }
            finally
            {
                _filtersLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Throws an ObjectDisposedException if the service is disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LogFilterService));
        }

        /// <summary>
        /// Disposes the filter service and its resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _filtersLock.EnterWriteLock();
            try
            {
                _filters.Clear();
                _disposed = true;
            }
            finally
            {
                _filtersLock.ExitWriteLock();
            }

            _filtersLock.Dispose();
        }

        /// <summary>
        /// Creates a LogFilterService with common default filters.
        /// </summary>
        /// <returns>A configured LogFilterService instance</returns>
        public static LogFilterService CreateDefault()
        {
            var service = new LogFilterService();
            
            // Add common default filters
            service.AddFilter(LevelFilter.ForMinimumLevel("DefaultLevel", LogLevel.Debug));
            service.AddFilter(SamplingFilter.Uniform("DefaultSampling", 1.0));
            
            return service;
        }

        /// <summary>
        /// Creates a LogFilterService with performance-optimized filters.
        /// </summary>
        /// <returns>A configured LogFilterService instance for performance scenarios</returns>
        public static LogFilterService CreatePerformanceOptimized()
        {
            var service = new LogFilterService();
            
            // Add performance-focused filters
            service.AddFilter(LevelFilter.ForMinimumLevel("PerformanceLevel", LogLevel.Info));
            service.AddFilter(SamplingFilter.Uniform("PerformanceSampling", 0.1));
            service.AddFilter(RateLimitFilter.BurstLimit("PerformanceRateLimit"));
            
            return service;
        }

        /// <summary>
        /// Creates a LogFilterService with debugging-focused filters.
        /// </summary>
        /// <returns>A configured LogFilterService instance for debugging scenarios</returns>
        public static LogFilterService CreateDebugFocused()
        {
            var service = new LogFilterService();
            
            // Add debugging-focused filters
            service.AddFilter(LevelFilter.ForMinimumLevel("DebugLevel", LogLevel.Debug));
            service.AddFilter(PatternFilter.ExcludeErrors("DebugExcludeErrors"));
            
            return service;
        }
    }
}