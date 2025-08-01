using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AhBearStudios.Core.Common.Models;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Filters
{
    /// <summary>
    /// Filter that evaluates log entries based on their log level.
    /// Provides level-based filtering with configurable minimum and maximum levels.
    /// Supports both include and exclude filtering modes for flexible log management.
    /// </summary>
    public sealed class LevelFilter : ILogFilter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly FilterStatistics _statistics;
        private bool _isEnabled = true;
        private LogLevel _minimumLevel = LogLevel.Debug;
        private LogLevel _maximumLevel = LogLevel.Critical;
        private bool _includeMode = true; // true = include, false = exclude

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
        /// Gets the minimum log level for filtering.
        /// </summary>
        public LogLevel MinimumLevel => _minimumLevel;

        /// <summary>
        /// Gets the maximum log level for filtering.
        /// </summary>
        public LogLevel MaximumLevel => _maximumLevel;

        /// <summary>
        /// Gets whether the filter is in include mode (true) or exclude mode (false).
        /// </summary>
        public bool IncludeMode => _includeMode;

        /// <summary>
        /// Initializes a new instance of the LevelFilter class.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="minimumLevel">The minimum log level (default: Debug)</param>
        /// <param name="maximumLevel">The maximum log level (default: Critical)</param>
        /// <param name="includeMode">Whether to include (true) or exclude (false) matching entries</param>
        /// <param name="priority">The filter priority (default: 1000)</param>
        public LevelFilter(
            string name = "LevelFilter",
            LogLevel minimumLevel = LogLevel.Debug,
            LogLevel maximumLevel = LogLevel.Critical,
            bool includeMode = true,
            int priority = 1000)
        {
            Name = name ?? "LevelFilter";
            Priority = priority;
            _minimumLevel = minimumLevel;
            _maximumLevel = maximumLevel;
            _includeMode = includeMode;
            _statistics = FilterStatistics.ForLevel(minimumLevel);
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["MinimumLevel"] = minimumLevel,
                ["MaximumLevel"] = maximumLevel,
                ["IncludeMode"] = includeMode,
                ["Priority"] = priority,
                ["IsEnabled"] = true
            };
        }

        /// <inheritdoc />
        public bool ShouldProcess(LogEntry entry, FixedString64Bytes correlationId = default)
        {
            if (!_isEnabled)
            {
                _statistics.RecordAllowed();
                return true;
            }

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Check if the entry's level is within the configured range
                var isInRange = entry.Level >= _minimumLevel && entry.Level <= _maximumLevel;
                
                // Apply include/exclude logic
                var shouldProcess = _includeMode ? isInRange : !isInRange;
                
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

            if (_minimumLevel > _maximumLevel)
            {
                errors.Add(new ValidationError("Minimum level cannot be greater than maximum level", nameof(MinimumLevel)));
            }

            if (_minimumLevel == LogLevel.Critical && _maximumLevel == LogLevel.Critical && _includeMode)
            {
                warnings.Add(new ValidationWarning("Filter only allows Critical level messages", nameof(MinimumLevel)));
            }

            if (_minimumLevel == LogLevel.Debug && _maximumLevel == LogLevel.Critical && !_includeMode)
            {
                warnings.Add(new ValidationWarning("Filter excludes all log levels", nameof(IncludeMode)));
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
                    case "MinimumLevel":
                        if (setting.Value is LogLevel minLevel)
                            _minimumLevel = minLevel;
                        else if (Enum.TryParse<LogLevel>(setting.Value?.ToString(), out var parsedMin))
                            _minimumLevel = parsedMin;
                        break;
                        
                    case "MaximumLevel":
                        if (setting.Value is LogLevel maxLevel)
                            _maximumLevel = maxLevel;
                        else if (Enum.TryParse<LogLevel>(setting.Value?.ToString(), out var parsedMax))
                            _maximumLevel = parsedMax;
                        break;
                        
                    case "IncludeMode":
                        if (setting.Value is bool includeMode)
                            _includeMode = includeMode;
                        else if (bool.TryParse(setting.Value?.ToString(), out var parsedInclude))
                            _includeMode = parsedInclude;
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
            _settings["MinimumLevel"] = _minimumLevel;
            _settings["MaximumLevel"] = _maximumLevel;
            _settings["IncludeMode"] = _includeMode;
            _settings["IsEnabled"] = _isEnabled;
            
            return _settings;
        }

        /// <summary>
        /// Creates a LevelFilter configured for specific log levels.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="levels">The log levels to filter</param>
        /// <param name="includeMode">Whether to include or exclude the specified levels</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured LevelFilter instance</returns>
        public static LevelFilter ForLevels(string name, LogLevel[] levels, bool includeMode = true, int priority = 1000)
        {
            if (levels == null || levels.Length == 0)
            {
                throw new ArgumentException("At least one level must be specified", nameof(levels));
            }

            var minLevel = levels.Min();
            var maxLevel = levels.Max();
            
            return new LevelFilter(name, minLevel, maxLevel, includeMode, priority);
        }

        /// <summary>
        /// Creates a LevelFilter configured for minimum level filtering.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="minimumLevel">The minimum log level</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured LevelFilter instance</returns>
        public static LevelFilter ForMinimumLevel(string name, LogLevel minimumLevel, int priority = 1000)
        {
            return new LevelFilter(name, minimumLevel, LogLevel.Critical, true, priority);
        }

        /// <summary>
        /// Creates a LevelFilter configured to exclude debug messages.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured LevelFilter instance</returns>
        public static LevelFilter ExcludeDebug(string name = "ExcludeDebug", int priority = 1000)
        {
            return new LevelFilter(name, LogLevel.Info, LogLevel.Critical, true, priority);
        }

        /// <summary>
        /// Creates a LevelFilter configured to only allow errors and critical messages.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured LevelFilter instance</returns>
        public static LevelFilter ErrorsOnly(string name = "ErrorsOnly", int priority = 1000)
        {
            return new LevelFilter(name, LogLevel.Error, LogLevel.Critical, true, priority);
        }
    }
}