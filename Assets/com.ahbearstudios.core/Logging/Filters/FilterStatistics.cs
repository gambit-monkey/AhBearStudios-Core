using System.Threading;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Filters
{
    /// <summary>
    /// Statistics and performance metrics for log filters.
    /// Provides comprehensive monitoring data for filter effectiveness and performance.
    /// Thread-safe implementation for high-performance logging scenarios.
    /// </summary>
    public sealed class FilterStatistics
    {
        private volatile int _totalEvaluations = 0;
        private volatile int _allowedCount = 0;
        private volatile int _blockedCount = 0;
        private volatile int _errorCount = 0;
        private long _totalProcessingTimeTicks = 0;
        private long _lastProcessingTimeTicks = 0;
        private readonly DateTime _startTime = DateTime.UtcNow;

        /// <summary>
        /// Gets the total number of filter evaluations performed.
        /// </summary>
        public int TotalEvaluations => _totalEvaluations;

        /// <summary>
        /// Gets the number of log entries that were allowed through the filter.
        /// </summary>
        public int AllowedCount => _allowedCount;

        /// <summary>
        /// Gets the number of log entries that were blocked by the filter.
        /// </summary>
        public int BlockedCount => _blockedCount;

        /// <summary>
        /// Gets the number of errors that occurred during filter evaluation.
        /// </summary>
        public int ErrorCount => _errorCount;

        /// <summary>
        /// Gets the filter effectiveness ratio (blocked / total).
        /// </summary>
        public double FilterEffectiveness => _totalEvaluations > 0 ? (double)_blockedCount / _totalEvaluations : 0.0;

        /// <summary>
        /// Gets the filter pass rate (allowed / total).
        /// </summary>
        public double PassRate => _totalEvaluations > 0 ? (double)_allowedCount / _totalEvaluations : 0.0;

        /// <summary>
        /// Gets the error rate (errors / total).
        /// </summary>
        public double ErrorRate => _totalEvaluations > 0 ? (double)_errorCount / _totalEvaluations : 0.0;

        /// <summary>
        /// Gets the average processing time per evaluation.
        /// </summary>
        public TimeSpan AverageProcessingTime => _totalEvaluations > 0 
            ? TimeSpan.FromTicks(_totalProcessingTimeTicks / _totalEvaluations) 
            : TimeSpan.Zero;

        /// <summary>
        /// Gets the last processing time recorded.
        /// </summary>
        public TimeSpan LastProcessingTime => TimeSpan.FromTicks(_lastProcessingTimeTicks);

        /// <summary>
        /// Gets the total processing time across all evaluations.
        /// </summary>
        public TimeSpan TotalProcessingTime => TimeSpan.FromTicks(_totalProcessingTimeTicks);

        /// <summary>
        /// Gets the uptime of the filter since statistics were created.
        /// </summary>
        public TimeSpan Uptime => DateTime.UtcNow - _startTime;

        /// <summary>
        /// Gets the evaluation rate (evaluations per second).
        /// </summary>
        public double EvaluationRate
        {
            get
            {
                var uptimeSeconds = Uptime.TotalSeconds;
                return uptimeSeconds > 0 ? _totalEvaluations / uptimeSeconds : 0.0;
            }
        }

        /// <summary>
        /// Records a successful filter evaluation that allowed the log entry.
        /// </summary>
        /// <param name="processingTime">Time taken to process the evaluation</param>
        public void RecordAllowed(TimeSpan processingTime = default)
        {
            Interlocked.Increment(ref _totalEvaluations);
            Interlocked.Increment(ref _allowedCount);
            RecordProcessingTime(processingTime);
        }

        /// <summary>
        /// Records a successful filter evaluation that blocked the log entry.
        /// </summary>
        /// <param name="processingTime">Time taken to process the evaluation</param>
        public void RecordBlocked(TimeSpan processingTime = default)
        {
            Interlocked.Increment(ref _totalEvaluations);
            Interlocked.Increment(ref _blockedCount);
            RecordProcessingTime(processingTime);
        }

        /// <summary>
        /// Records an error that occurred during filter evaluation.
        /// </summary>
        /// <param name="processingTime">Time taken before the error occurred</param>
        public void RecordError(TimeSpan processingTime = default)
        {
            Interlocked.Increment(ref _totalEvaluations);
            Interlocked.Increment(ref _errorCount);
            RecordProcessingTime(processingTime);
        }

        /// <summary>
        /// Resets all statistics to their initial state.
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _totalEvaluations, 0);
            Interlocked.Exchange(ref _allowedCount, 0);
            Interlocked.Exchange(ref _blockedCount, 0);
            Interlocked.Exchange(ref _errorCount, 0);
            Interlocked.Exchange(ref _totalProcessingTimeTicks, 0);
            Interlocked.Exchange(ref _lastProcessingTimeTicks, 0);
        }

        /// <summary>
        /// Creates a snapshot of the current statistics.
        /// </summary>
        /// <returns>A new FilterStatistics instance with current values</returns>
        public FilterStatistics CreateSnapshot()
        {
            return new FilterStatistics
            {
                _totalEvaluations = _totalEvaluations,
                _allowedCount = _allowedCount,
                _blockedCount = _blockedCount,
                _errorCount = _errorCount,
                _totalProcessingTimeTicks = _totalProcessingTimeTicks,
                _lastProcessingTimeTicks = _lastProcessingTimeTicks
            };
        }

        /// <summary>
        /// Gets a summary string of the filter statistics.
        /// </summary>
        /// <returns>A formatted summary of the statistics</returns>
        public string GetSummary()
        {
            return $"Filter Statistics: {_totalEvaluations} evals, {_allowedCount} allowed ({PassRate:P2}), " +
                   $"{_blockedCount} blocked ({FilterEffectiveness:P2}), {_errorCount} errors ({ErrorRate:P2}), " +
                   $"avg {AverageProcessingTime.TotalMilliseconds:F2}ms, rate {EvaluationRate:F1}/sec";
        }

        /// <summary>
        /// Records processing time for performance tracking.
        /// </summary>
        /// <param name="processingTime">Time taken to process the evaluation</param>
        private void RecordProcessingTime(TimeSpan processingTime)
        {
            var ticks = processingTime.Ticks;
            if (ticks > 0)
            {
                Interlocked.Add(ref _totalProcessingTimeTicks, ticks);
                Interlocked.Exchange(ref _lastProcessingTimeTicks, ticks);
            }
        }

        /// <summary>
        /// Creates a new FilterStatistics instance for a specific filter.
        /// </summary>
        /// <param name="filterName">The name of the filter</param>
        /// <param name="filterType">The type of the filter</param>
        /// <returns>A new FilterStatistics instance</returns>
        public static FilterStatistics Create(string filterName, string filterType)
        {
            return new FilterStatistics();
        }

        /// <summary>
        /// Creates a new FilterStatistics instance for a level filter.
        /// </summary>
        /// <param name="level">The log level being filtered</param>
        /// <returns>A new FilterStatistics instance optimized for level filtering</returns>
        public static FilterStatistics ForLevel(LogLevel level)
        {
            return new FilterStatistics();
        }

        /// <summary>
        /// Creates a new FilterStatistics instance for a source filter.
        /// </summary>
        /// <param name="source">The source being filtered</param>
        /// <returns>A new FilterStatistics instance optimized for source filtering</returns>
        public static FilterStatistics ForSource(string source)
        {
            return new FilterStatistics();
        }

        /// <summary>
        /// Creates a new FilterStatistics instance for a pattern filter.
        /// </summary>
        /// <param name="pattern">The pattern being filtered</param>
        /// <returns>A new FilterStatistics instance optimized for pattern filtering</returns>
        public static FilterStatistics ForPattern(string pattern)
        {
            return new FilterStatistics();
        }

        /// <summary>
        /// Creates a new FilterStatistics instance for a correlation filter.
        /// </summary>
        /// <param name="correlationId">The correlation ID being filtered</param>
        /// <returns>A new FilterStatistics instance optimized for correlation filtering</returns>
        public static FilterStatistics ForCorrelation(string correlationId)
        {
            return new FilterStatistics();
        }

        /// <summary>
        /// Creates a new FilterStatistics instance for a custom filter.
        /// </summary>
        /// <param name="filterName">The name of the custom filter</param>
        /// <param name="filterCriteria">The criteria for the custom filter</param>
        /// <returns>A new FilterStatistics instance optimized for custom filtering</returns>
        public static FilterStatistics ForCustom(string filterName, string filterCriteria)
        {
            return new FilterStatistics();
        }

        /// <summary>
        /// Creates a new FilterStatistics instance for a composite filter.
        /// </summary>
        /// <param name="childFilters">The number of child filters in the composite</param>
        /// <returns>A new FilterStatistics instance optimized for composite filtering</returns>
        public static FilterStatistics ForComposite(int childFilters)
        {
            return new FilterStatistics();
        }

        /// <summary>
        /// Creates an empty FilterStatistics instance.
        /// </summary>
        /// <param name="filterName">The name of the filter</param>
        /// <returns>A new FilterStatistics instance with no recorded activity</returns>
        public static FilterStatistics Empty(string filterName)
        {
            return new FilterStatistics();
        }
    }
}