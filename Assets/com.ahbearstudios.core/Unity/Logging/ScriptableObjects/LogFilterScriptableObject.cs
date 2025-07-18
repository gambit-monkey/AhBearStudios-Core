using System;
using System.Collections.Generic;
using UnityEngine;

namespace AhBearStudios.Unity.Logging.ScriptableObjects
{
    /// <summary>
    /// Base ScriptableObject for log filter configurations.
    /// Provides Unity-serializable configuration for log filters.
    /// </summary>
    public abstract class LogFilterScriptableObject : LoggingScriptableObjectBase
    {
        [Header("Filter Settings")]
        [SerializeField] protected int _priority = 0;
        [SerializeField] protected bool _isInclusive = true;
        [SerializeField] protected bool _stopProcessingOnMatch = false;
        [SerializeField] protected bool _logFilterActions = false;

        [Header("Performance Settings")]
        [SerializeField] protected bool _enableCaching = true;
        [SerializeField] protected int _maxCacheSize = 1000;
        [SerializeField] protected bool _enableStatistics = true;
        [SerializeField] protected float _performanceThresholdMs = 1.0f;

        [Header("Channel Filtering")]
        [SerializeField] protected List<string> _targetChannels = new List<string>();
        [SerializeField] protected bool _applyToAllChannels = true;

        [Header("Time-based Filtering")]
        [SerializeField] protected bool _enableTimeFiltering = false;
        [SerializeField] protected string _startTime = "00:00:00";
        [SerializeField] protected string _endTime = "23:59:59";
        [SerializeField] protected bool _useUtcTime = false;

        /// <summary>
        /// Gets the filter priority (higher numbers = higher priority).
        /// </summary>
        public int Priority => _priority;

        /// <summary>
        /// Gets whether this is an inclusive filter (true) or exclusive filter (false).
        /// </summary>
        public bool IsInclusive => _isInclusive;

        /// <summary>
        /// Gets whether to stop processing other filters when this filter matches.
        /// </summary>
        public bool StopProcessingOnMatch => _stopProcessingOnMatch;

        /// <summary>
        /// Gets whether to log filter actions.
        /// </summary>
        public bool LogFilterActions => _logFilterActions;

        /// <summary>
        /// Gets whether caching is enabled.
        /// </summary>
        public bool EnableCaching => _enableCaching;

        /// <summary>
        /// Gets the maximum cache size.
        /// </summary>
        public int MaxCacheSize => _maxCacheSize;

        /// <summary>
        /// Gets whether statistics collection is enabled.
        /// </summary>
        public bool EnableStatistics => _enableStatistics;

        /// <summary>
        /// Gets the performance threshold in milliseconds.
        /// </summary>
        public float PerformanceThresholdMs => _performanceThresholdMs;

        /// <summary>
        /// Gets the list of target channels.
        /// </summary>
        public IReadOnlyList<string> TargetChannels => _targetChannels.AsReadOnly();

        /// <summary>
        /// Gets whether to apply to all channels.
        /// </summary>
        public bool ApplyToAllChannels => _applyToAllChannels;

        /// <summary>
        /// Gets whether time filtering is enabled.
        /// </summary>
        public bool EnableTimeFiltering => _enableTimeFiltering;

        /// <summary>
        /// Gets the start time for time-based filtering.
        /// </summary>
        public string StartTime => _startTime;

        /// <summary>
        /// Gets the end time for time-based filtering.
        /// </summary>
        public string EndTime => _endTime;

        /// <summary>
        /// Gets whether to use UTC time for time-based filtering.
        /// </summary>
        public bool UseUtcTime => _useUtcTime;

        /// <summary>
        /// Creates filter-specific properties dictionary.
        /// Override in derived classes to add specific properties.
        /// </summary>
        /// <returns>Dictionary of filter-specific properties</returns>
        public override Dictionary<string, object> ToProperties()
        {
            var properties = base.ToProperties();
            
            properties["Priority"] = _priority;
            properties["IsInclusive"] = _isInclusive;
            properties["StopProcessingOnMatch"] = _stopProcessingOnMatch;
            properties["LogFilterActions"] = _logFilterActions;
            properties["EnableCaching"] = _enableCaching;
            properties["MaxCacheSize"] = _maxCacheSize;
            properties["EnableStatistics"] = _enableStatistics;
            properties["PerformanceThresholdMs"] = _performanceThresholdMs;
            properties["TargetChannels"] = _targetChannels;
            properties["ApplyToAllChannels"] = _applyToAllChannels;
            properties["EnableTimeFilteringByRange"] = _enableTimeFiltering;
            properties["StartTime"] = _startTime;
            properties["EndTime"] = _endTime;
            properties["UseUtcTime"] = _useUtcTime;
            
            return properties;
        }

        /// <summary>
        /// Validates the filter configuration.
        /// </summary>
        /// <returns>List of validation errors</returns>
        public override List<string> ValidateConfiguration()
        {
            var errors = base.ValidateConfiguration();

            if (_maxCacheSize <= 0)
            {
                errors.Add("Max cache size must be greater than zero");
            }

            if (_performanceThresholdMs <= 0)
            {
                errors.Add("Performance threshold must be greater than zero");
            }

            if (_enableTimeFiltering)
            {
                if (string.IsNullOrWhiteSpace(_startTime))
                {
                    errors.Add("Start time cannot be empty when time filtering is enabled");
                }

                if (string.IsNullOrWhiteSpace(_endTime))
                {
                    errors.Add("End time cannot be empty when time filtering is enabled");
                }

                // Validate time format
                if (!TimeSpan.TryParse(_startTime, out _))
                {
                    errors.Add("Invalid start time format");
                }

                if (!TimeSpan.TryParse(_endTime, out _))
                {
                    errors.Add("Invalid end time format");
                }
            }

            return errors;
        }

        /// <summary>
        /// Resets the filter configuration to default values.
        /// </summary>
        public override void ResetToDefaults()
        {
            base.ResetToDefaults();
            
            _priority = 0;
            _isInclusive = true;
            _stopProcessingOnMatch = false;
            _logFilterActions = false;
            _enableCaching = true;
            _maxCacheSize = 1000;
            _enableStatistics = true;
            _performanceThresholdMs = 1.0f;
            _targetChannels.Clear();
            _applyToAllChannels = true;
            _enableTimeFiltering = false;
            _startTime = "00:00:00";
            _endTime = "23:59:59";
            _useUtcTime = false;
        }

        /// <summary>
        /// Performs filter-specific validation.
        /// </summary>
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();

            // Clamp numeric values
            _maxCacheSize = Mathf.Max(1, _maxCacheSize);
            _performanceThresholdMs = Mathf.Max(0.1f, _performanceThresholdMs);

            // Validate time strings
            if (string.IsNullOrWhiteSpace(_startTime))
            {
                _startTime = "00:00:00";
            }

            if (string.IsNullOrWhiteSpace(_endTime))
            {
                _endTime = "23:59:59";
            }

            // Validate time format
            if (!TimeSpan.TryParse(_startTime, out _))
            {
                _startTime = "00:00:00";
            }

            if (!TimeSpan.TryParse(_endTime, out _))
            {
                _endTime = "23:59:59";
            }
        }

        /// <summary>
        /// Adds a target channel to the filter.
        /// </summary>
        /// <param name="channelName">The channel name to add</param>
        public void AddTargetChannel(string channelName)
        {
            if (!string.IsNullOrWhiteSpace(channelName) && !_targetChannels.Contains(channelName))
            {
                _targetChannels.Add(channelName);
                _applyToAllChannels = false;
            }
        }

        /// <summary>
        /// Removes a target channel from the filter.
        /// </summary>
        /// <param name="channelName">The channel name to remove</param>
        public void RemoveTargetChannel(string channelName)
        {
            _targetChannels.Remove(channelName);
            
            if (_targetChannels.Count == 0)
            {
                _applyToAllChannels = true;
            }
        }

        /// <summary>
        /// Clears all target channels and sets to apply to all channels.
        /// </summary>
        [ContextMenu("Clear Target Channels")]
        public void ClearTargetChannels()
        {
            _targetChannels.Clear();
            _applyToAllChannels = true;
        }

        /// <summary>
        /// Checks if the filter should be applied to the specified channel.
        /// </summary>
        /// <param name="channelName">The channel name to check</param>
        /// <returns>True if the filter should be applied to the channel</returns>
        public bool ShouldApplyToChannel(string channelName)
        {
            if (_applyToAllChannels)
            {
                return true;
            }

            return _targetChannels.Contains(channelName);
        }

        /// <summary>
        /// Checks if the current time falls within the filter's time range.
        /// </summary>
        /// <returns>True if the current time is within the filter's time range</returns>
        public bool IsWithinTimeRange()
        {
            if (!_enableTimeFiltering)
            {
                return true;
            }

            var currentTime = _useUtcTime ? DateTime.UtcNow : DateTime.Now;
            var currentTimeOfDay = currentTime.TimeOfDay;

            if (TimeSpan.TryParse(_startTime, out var startTime) &&
                TimeSpan.TryParse(_endTime, out var endTime))
            {
                if (startTime <= endTime)
                {
                    // Same day range
                    return currentTimeOfDay >= startTime && currentTimeOfDay <= endTime;
                }
                else
                {
                    // Crosses midnight
                    return currentTimeOfDay >= startTime || currentTimeOfDay <= endTime;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the parsed start time as TimeSpan.
        /// </summary>
        /// <returns>Start time as TimeSpan</returns>
        public TimeSpan GetStartTimeSpan()
        {
            return TimeSpan.TryParse(_startTime, out var time) ? time : TimeSpan.Zero;
        }

        /// <summary>
        /// Gets the parsed end time as TimeSpan.
        /// </summary>
        /// <returns>End time as TimeSpan</returns>
        public TimeSpan GetEndTimeSpan()
        {
            return TimeSpan.TryParse(_endTime, out var time) ? time : TimeSpan.FromHours(23.99);
        }

        /// <summary>
        /// Sets the time range for filtering.
        /// </summary>
        /// <param name="startTime">Start time string (HH:mm:ss)</param>
        /// <param name="endTime">End time string (HH:mm:ss)</param>
        public void SetTimeRange(string startTime, string endTime)
        {
            if (TimeSpan.TryParse(startTime, out _))
            {
                _startTime = startTime;
            }

            if (TimeSpan.TryParse(endTime, out _))
            {
                _endTime = endTime;
            }

            ValidateInEditor();
        }

        /// <summary>
        /// Enables time-based filtering with the specified range.
        /// </summary>
        /// <param name="startTime">Start time string (HH:mm:ss)</param>
        /// <param name="endTime">End time string (HH:mm:ss)</param>
        /// <param name="useUtc">Whether to use UTC time</param>
        public void EnableTimeFilteringByRange(string startTime, string endTime, bool useUtc = false)
        {
            SetTimeRange(startTime, endTime);
            _enableTimeFiltering = true;
            _useUtcTime = useUtc;
        }

        /// <summary>
        /// Disables time-based filtering.
        /// </summary>
        public void DisableTimeFiltering()
        {
            _enableTimeFiltering = false;
        }

        /// <summary>
        /// Creates a test configuration for the filter.
        /// </summary>
        /// <returns>Test configuration description</returns>
        public virtual string CreateTestConfiguration()
        {
            var config = $"Filter: {Name}\n";
            config += $"Priority: {_priority}\n";
            config += $"Type: {(_isInclusive ? "Inclusive" : "Exclusive")}\n";
            config += $"Channels: {(_applyToAllChannels ? "All" : string.Join(", ", _targetChannels))}\n";
            
            if (_enableTimeFiltering)
            {
                config += $"Time Range: {_startTime} - {_endTime} ({(_useUtcTime ? "UTC" : "Local")})\n";
            }
            
            return config;
        }
    }
}