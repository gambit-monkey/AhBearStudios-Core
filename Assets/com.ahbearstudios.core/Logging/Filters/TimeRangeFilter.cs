using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Configs;

namespace AhBearStudios.Core.Logging.Filters
{
    /// <summary>
    /// Filter that evaluates log entries based on their timestamp within specified time ranges.
    /// Provides time-based filtering for log entries with support for multiple time ranges and patterns.
    /// Supports both include and exclude filtering modes for flexible time-based management.
    /// </summary>
    public sealed class TimeRangeFilter : ILogFilter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly FilterStatistics _statistics;
        private readonly List<TimeRange> _timeRanges = new();
        private bool _isEnabled = true;
        private bool _includeMode = true;
        private TimeZoneInfo _timeZone = TimeZoneInfo.Utc;

        /// <inheritdoc />
        public FixedString64Bytes Name { get; }

        /// <inheritdoc />
        public bool IsEnabled 
        { 
            get => _isEnabled; 
            set => _isEnabled = value; 
        }

        /// <inheritdoc />
        public int Priority { get; }

        /// <summary>
        /// Gets the time ranges that this filter matches against.
        /// </summary>
        public IReadOnlyList<TimeRange> TimeRanges => _timeRanges.AsReadOnly();

        /// <summary>
        /// Gets whether the filter is in include mode (true) or exclude mode (false).
        /// </summary>
        public bool IncludeMode => _includeMode;

        /// <summary>
        /// Gets the time zone used for time range comparisons.
        /// </summary>
        public TimeZoneInfo TimeZone => _timeZone;

        /// <summary>
        /// Initializes a new instance of the TimeRangeFilter class.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="timeRanges">The time ranges to filter</param>
        /// <param name="includeMode">Whether to include (true) or exclude (false) entries within the time ranges</param>
        /// <param name="timeZone">The time zone for time range comparisons</param>
        /// <param name="priority">The filter priority (default: 600)</param>
        public TimeRangeFilter(
            string name = "TimeRangeFilter",
            IEnumerable<TimeRange> timeRanges = null,
            bool includeMode = true,
            TimeZoneInfo timeZone = null,
            int priority = 600)
        {
            Name = name ?? "TimeRangeFilter";
            Priority = priority;
            
            if (timeRanges != null)
            {
                foreach (var range in timeRanges)
                {
                    if (range.IsValid)
                        _timeRanges.Add(range);
                }
            }
            
            _includeMode = includeMode;
            _timeZone = timeZone ?? TimeZoneInfo.Utc;
            var description = _timeRanges.Count > 0 
                ? $"Ranges: {_timeRanges.Count} configured"
                : "No ranges configured";
            _statistics = FilterStatistics.ForCustom("TimeRange", description);
            
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["TimeRanges"] = _timeRanges,
                ["IncludeMode"] = _includeMode,
                ["TimeZone"] = _timeZone.Id,
                ["Priority"] = priority,
                ["IsEnabled"] = true
            };
        }

        /// <summary>
        /// Initializes a new instance of the TimeRangeFilter class with a single time range.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="startTime">The start time of the range</param>
        /// <param name="endTime">The end time of the range</param>
        /// <param name="includeMode">Whether to include (true) or exclude (false) entries within the time range</param>
        /// <param name="timeZone">The time zone for time range comparisons</param>
        /// <param name="priority">The filter priority (default: 600)</param>
        public TimeRangeFilter(
            string name,
            DateTime startTime,
            DateTime endTime,
            bool includeMode = true,
            TimeZoneInfo timeZone = null,
            int priority = 600)
            : this(name, new[] { new TimeRange(startTime, endTime) }, includeMode, timeZone, priority)
        {
        }

        /// <inheritdoc />
        public bool ShouldProcess(LogEntry entry, FixedString64Bytes correlationId = default)
        {
            if (!_isEnabled)
            {
                _statistics.RecordAllowed();
                return true;
            }

            // If no time ranges specified, allow all
            if (_timeRanges.Count == 0)
            {
                _statistics.RecordAllowed();
                return true;
            }

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var entryTime = entry.Timestamp;
                
                // Convert to the specified time zone if needed
                if (_timeZone != TimeZoneInfo.Utc)
                {
                    entryTime = TimeZoneInfo.ConvertTime(entryTime, TimeZoneInfo.Utc, _timeZone);
                }

                bool matches = false;
                
                // Check if entry time falls within any of the configured ranges
                foreach (var range in _timeRanges)
                {
                    if (range.Contains(entryTime))
                    {
                        matches = true;
                        break;
                    }
                }
                
                // Apply include/exclude logic
                var shouldProcess = _includeMode ? matches : !matches;
                
                stopwatch.Stop();
                
                if (shouldProcess)
                {
                    _statistics.RecordAllowed(stopwatch.Elapsed);
                }
                else
                {
                    _statistics.RecordBlocked(stopwatch.Elapsed);
                }
                
                return shouldProcess;
            }
            catch (Exception)
            {
                stopwatch.Stop();
                _statistics.RecordError(stopwatch.Elapsed);
                
                // On error, allow the entry to pass through to prevent log loss
                return true;
            }
        }

        /// <inheritdoc />
        public ValidationResult Validate(FixedString64Bytes correlationId = default)
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();

            if (string.IsNullOrEmpty(Name.ToString()))
            {
                errors.Add(new ValidationError("Filter name cannot be empty", nameof(Name)));
            }

            if (_timeRanges.Count == 0)
            {
                warnings.Add(new ValidationWarning("No time ranges specified - filter will allow all entries", nameof(TimeRanges)));
            }

            // Validate time ranges
            foreach (var range in _timeRanges)
            {
                if (!range.IsValid)
                {
                    errors.Add(new ValidationError($"Invalid time range: {range.StartTime} to {range.EndTime}", nameof(TimeRanges)));
                }
            }

            // Check for overlapping time ranges
            for (int i = 0; i < _timeRanges.Count; i++)
            {
                for (int j = i + 1; j < _timeRanges.Count; j++)
                {
                    if (TimeRangesOverlap(_timeRanges[i], _timeRanges[j]))
                    {
                        warnings.Add(new ValidationWarning($"Overlapping time ranges detected: {_timeRanges[i].StartTime}-{_timeRanges[i].EndTime} and {_timeRanges[j].StartTime}-{_timeRanges[j].EndTime}", nameof(TimeRanges)));
                    }
                }
            }

            // Check for very long time ranges
            foreach (var range in _timeRanges)
            {
                if (range.Duration.TotalDays > 365)
                {
                    warnings.Add(new ValidationWarning($"Very long time range detected: {range.Duration.TotalDays:F0} days", nameof(TimeRanges)));
                }
            }

            return errors.Count > 0 
                ? ValidationResult.Failure(errors, Name.ToString(), warnings)
                : ValidationResult.Success(Name.ToString(), warnings);
        }

        /// <inheritdoc />
        public FilterStatistics GetStatistics()
        {
            return _statistics.CreateSnapshot();
        }

        /// <inheritdoc />
        public void Reset(FixedString64Bytes correlationId = default)
        {
            _statistics.Reset();
        }

        /// <inheritdoc />
        public void Configure(IReadOnlyDictionary<FixedString32Bytes, object> settings, FixedString64Bytes correlationId = default)
        {
            if (settings == null) return;

            foreach (var setting in settings)
            {
                _settings[setting.Key] = setting.Value;
                
                // Apply settings to internal state
                switch (setting.Key.ToString())
                {
                    case "TimeRanges":
                        if (setting.Value is IEnumerable<TimeRange> ranges)
                        {
                            _timeRanges.Clear();
                            foreach (var range in ranges)
                            {
                                if (range.IsValid)
                                    _timeRanges.Add(range);
                            }
                        }
                        break;
                        
                    case "IncludeMode":
                        if (setting.Value is bool includeMode)
                            _includeMode = includeMode;
                        else if (bool.TryParse(setting.Value?.ToString(), out var parsedInclude))
                            _includeMode = parsedInclude;
                        break;
                        
                    case "TimeZone":
                        if (setting.Value is string timeZoneId)
                        {
                            try
                            {
                                _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                            }
                            catch (TimeZoneNotFoundException)
                            {
                                _timeZone = TimeZoneInfo.Utc;
                            }
                        }
                        else if (setting.Value is TimeZoneInfo timeZone)
                        {
                            _timeZone = timeZone;
                        }
                        break;
                        
                    case "IsEnabled":
                        if (setting.Value is bool isEnabled)
                            _isEnabled = isEnabled;
                        else if (bool.TryParse(setting.Value?.ToString(), out var parsedEnabled))
                            _isEnabled = parsedEnabled;
                        break;
                }
            }
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<FixedString32Bytes, object> GetSettings()
        {
            // Update current values in settings
            _settings["TimeRanges"] = _timeRanges;
            _settings["IncludeMode"] = _includeMode;
            _settings["TimeZone"] = _timeZone.Id;
            _settings["IsEnabled"] = _isEnabled;
            
            return _settings;
        }

        /// <summary>
        /// Adds a time range to the filter.
        /// </summary>
        /// <param name="startTime">The start time of the range</param>
        /// <param name="endTime">The end time of the range</param>
        /// <returns>True if the range was added successfully</returns>
        public bool AddTimeRange(DateTime startTime, DateTime endTime)
        {
            var range = new TimeRange(startTime, endTime);
            if (range.IsValid)
            {
                _timeRanges.Add(range);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a time range to the filter.
        /// </summary>
        /// <param name="range">The time range to add</param>
        /// <returns>True if the range was added successfully</returns>
        public bool AddTimeRange(TimeRange range)
        {
            if (range.IsValid)
            {
                _timeRanges.Add(range);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes all time ranges from the filter.
        /// </summary>
        public void ClearTimeRanges()
        {
            _timeRanges.Clear();
        }

        /// <summary>
        /// Checks if two time ranges overlap.
        /// </summary>
        /// <param name="range1">The first time range</param>
        /// <param name="range2">The second time range</param>
        /// <returns>True if the ranges overlap</returns>
        private bool TimeRangesOverlap(TimeRange range1, TimeRange range2)
        {
            return range1.StartTime < range2.EndTime && range2.StartTime < range1.EndTime;
        }

        /// <summary>
        /// Creates a TimeRangeFilter configured for a specific time range.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="startTime">The start time of the range</param>
        /// <param name="endTime">The end time of the range</param>
        /// <param name="includeMode">Whether to include or exclude entries within the range</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured TimeRangeFilter instance</returns>
        public static TimeRangeFilter ForRange(string name, DateTime startTime, DateTime endTime, bool includeMode = true, int priority = 600)
        {
            return new TimeRangeFilter(name, startTime, endTime, includeMode, TimeZoneInfo.Utc, priority);
        }

        /// <summary>
        /// Creates a TimeRangeFilter configured for business hours (9 AM to 5 PM).
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="includeMode">Whether to include or exclude entries during business hours</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured TimeRangeFilter instance</returns>
        public static TimeRangeFilter BusinessHours(string name = "BusinessHours", bool includeMode = true, int priority = 600)
        {
            var today = DateTime.Today;
            var startTime = today.AddHours(9);
            var endTime = today.AddHours(17);
            
            return new TimeRangeFilter(name, startTime, endTime, includeMode, TimeZoneInfo.Local, priority);
        }

        /// <summary>
        /// Creates a TimeRangeFilter configured for after hours (outside 9 AM to 5 PM).
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="includeMode">Whether to include or exclude entries during after hours</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured TimeRangeFilter instance</returns>
        public static TimeRangeFilter AfterHours(string name = "AfterHours", bool includeMode = true, int priority = 600)
        {
            var today = DateTime.Today;
            var ranges = new[]
            {
                new TimeRange(today, today.AddHours(9)),
                new TimeRange(today.AddHours(17), today.AddDays(1))
            };
            
            return new TimeRangeFilter(name, ranges, includeMode, TimeZoneInfo.Local, priority);
        }

        /// <summary>
        /// Creates a TimeRangeFilter configured for the last N hours.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="hours">The number of hours back from now</param>
        /// <param name="includeMode">Whether to include or exclude entries from the last N hours</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured TimeRangeFilter instance</returns>
        public static TimeRangeFilter LastHours(string name, int hours, bool includeMode = true, int priority = 600)
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddHours(-hours);
            
            return new TimeRangeFilter(name, startTime, endTime, includeMode, TimeZoneInfo.Utc, priority);
        }

        /// <summary>
        /// Creates a TimeRangeFilter configured for today only.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="includeMode">Whether to include or exclude entries from today</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured TimeRangeFilter instance</returns>
        public static TimeRangeFilter Today(string name = "Today", bool includeMode = true, int priority = 600)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            
            return new TimeRangeFilter(name, today, tomorrow, includeMode, TimeZoneInfo.Local, priority);
        }

        /// <summary>
        /// Creates a TimeRangeFilter configured for this week only.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="includeMode">Whether to include or exclude entries from this week</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured TimeRangeFilter instance</returns>
        public static TimeRangeFilter ThisWeek(string name = "ThisWeek", bool includeMode = true, int priority = 600)
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7);
            
            return new TimeRangeFilter(name, startOfWeek, endOfWeek, includeMode, TimeZoneInfo.Local, priority);
        }
    }
}