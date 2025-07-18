using System.Collections.Generic;
using UnityEngine;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Unity.Logging.ScriptableObjects.Filters
{
    /// <summary>
    /// ScriptableObject configuration for Level log filter.
    /// Provides Unity-serializable configuration for log level based filtering.
    /// </summary>
    [CreateAssetMenu(
        menuName = "AhBearStudios/Core/Logging/Filters/Level Filter", 
        fileName = "LevelFilterConfig", 
        order = 1)]
    public class LevelFilterConfig : LogFilterScriptableObject
    {
        [Header("Level Filter Settings")]
        [SerializeField] private LogLevel _filterLevel = LogLevel.Debug;
        [SerializeField] private bool _allowHigherLevels = true;
        [SerializeField] private bool _allowLowerLevels = false;
        [SerializeField] private bool _allowExactLevel = true;

        [Header("Level Range Settings")]
        [SerializeField] private bool _useRangeFiltering = false;
        [SerializeField] private LogLevel _minLevel = LogLevel.Debug;
        [SerializeField] private LogLevel _maxLevel = LogLevel.Critical;

        [Header("Specific Level Settings")]
        [SerializeField] private List<LogLevel> _allowedLevels = new List<LogLevel>();
        [SerializeField] private List<LogLevel> _blockedLevels = new List<LogLevel>();
        [SerializeField] private bool _useSpecificLevels = false;

        [Header("Dynamic Level Settings")]
        [SerializeField] private bool _enableDynamicLevels = false;
        [SerializeField] private string _dynamicLevelSource = "Environment";
        [SerializeField] private string _dynamicLevelKey = "LOG_LEVEL";
        [SerializeField] private LogLevel _fallbackLevel = LogLevel.Info;

        [Header("Level Mapping")]
        [SerializeField] private bool _enableLevelMapping = false;
        [SerializeField] private List<LevelMapping> _levelMappings = new List<LevelMapping>();

        /// <summary>
        /// Gets the filter level.
        /// </summary>
        public LogLevel FilterLevel => _filterLevel;

        /// <summary>
        /// Gets whether to allow higher levels.
        /// </summary>
        public bool AllowHigherLevels => _allowHigherLevels;

        /// <summary>
        /// Gets whether to allow lower levels.
        /// </summary>
        public bool AllowLowerLevels => _allowLowerLevels;

        /// <summary>
        /// Gets whether to allow exact level.
        /// </summary>
        public bool AllowExactLevel => _allowExactLevel;

        /// <summary>
        /// Gets whether to use range filtering.
        /// </summary>
        public bool UseRangeFiltering => _useRangeFiltering;

        /// <summary>
        /// Gets the minimum level for range filtering.
        /// </summary>
        public LogLevel MinLevel => _minLevel;

        /// <summary>
        /// Gets the maximum level for range filtering.
        /// </summary>
        public LogLevel MaxLevel => _maxLevel;

        /// <summary>
        /// Gets the list of allowed levels.
        /// </summary>
        public IReadOnlyList<LogLevel> AllowedLevels => _allowedLevels.AsReadOnly();

        /// <summary>
        /// Gets the list of blocked levels.
        /// </summary>
        public IReadOnlyList<LogLevel> BlockedLevels => _blockedLevels.AsReadOnly();

        /// <summary>
        /// Gets whether to use specific levels.
        /// </summary>
        public bool UseSpecificLevels => _useSpecificLevels;

        /// <summary>
        /// Gets whether dynamic levels are enabled.
        /// </summary>
        public bool EnableDynamicLevels => _enableDynamicLevels;

        /// <summary>
        /// Gets the dynamic level source.
        /// </summary>
        public string DynamicLevelSource => _dynamicLevelSource;

        /// <summary>
        /// Gets the dynamic level key.
        /// </summary>
        public string DynamicLevelKey => _dynamicLevelKey;

        /// <summary>
        /// Gets the fallback level.
        /// </summary>
        public LogLevel FallbackLevel => _fallbackLevel;

        /// <summary>
        /// Gets whether level mapping is enabled.
        /// </summary>
        public bool EnableLevelMapping => _enableLevelMapping;

        /// <summary>
        /// Gets the level mappings.
        /// </summary>
        public IReadOnlyList<LevelMapping> LevelMappings => _levelMappings.AsReadOnly();

        /// <summary>
        /// Creates level filter specific properties.
        /// </summary>
        /// <returns>Dictionary of level filter properties</returns>
        public override Dictionary<string, object> ToProperties()
        {
            var properties = base.ToProperties();
            
            properties["FilterLevel"] = _filterLevel.ToString();
            properties["AllowHigherLevels"] = _allowHigherLevels;
            properties["AllowLowerLevels"] = _allowLowerLevels;
            properties["AllowExactLevel"] = _allowExactLevel;
            properties["UseRangeFiltering"] = _useRangeFiltering;
            properties["MinLevel"] = _minLevel.ToString();
            properties["MaxLevel"] = _maxLevel.ToString();
            properties["UseSpecificLevels"] = _useSpecificLevels;
            properties["EnableDynamicLevels"] = _enableDynamicLevels;
            properties["DynamicLevelSource"] = _dynamicLevelSource;
            properties["DynamicLevelKey"] = _dynamicLevelKey;
            properties["FallbackLevel"] = _fallbackLevel.ToString();
            properties["EnableLevelMapping"] = _enableLevelMapping;
            
            // Convert lists to arrays for serialization
            var allowedLevelsArray = new string[_allowedLevels.Count];
            for (int i = 0; i < _allowedLevels.Count; i++)
            {
                allowedLevelsArray[i] = _allowedLevels[i].ToString();
            }
            properties["AllowedLevels"] = allowedLevelsArray;
            
            var blockedLevelsArray = new string[_blockedLevels.Count];
            for (int i = 0; i < _blockedLevels.Count; i++)
            {
                blockedLevelsArray[i] = _blockedLevels[i].ToString();
            }
            properties["BlockedLevels"] = blockedLevelsArray;
            
            return properties;
        }

        /// <summary>
        /// Validates level filter specific configuration.
        /// </summary>
        /// <returns>List of validation errors</returns>
        public override List<string> ValidateConfiguration()
        {
            var errors = base.ValidateConfiguration();

            if (_useRangeFiltering && _minLevel > _maxLevel)
            {
                errors.Add("Minimum level cannot be higher than maximum level");
            }

            if (_useSpecificLevels && _allowedLevels.Count == 0 && _blockedLevels.Count == 0)
            {
                errors.Add("At least one allowed or blocked level must be specified when using specific levels");
            }

            if (_enableDynamicLevels)
            {
                if (string.IsNullOrWhiteSpace(_dynamicLevelSource))
                {
                    errors.Add("Dynamic level source cannot be empty");
                }

                if (string.IsNullOrWhiteSpace(_dynamicLevelKey))
                {
                    errors.Add("Dynamic level key cannot be empty");
                }
            }

            if (_enableLevelMapping && _levelMappings.Count == 0)
            {
                errors.Add("At least one level mapping must be specified when level mapping is enabled");
            }

            return errors;
        }

        /// <summary>
        /// Resets to level filter specific defaults.
        /// </summary>
        public override void ResetToDefaults()
        {
            base.ResetToDefaults();
            
            _name = "Level Filter";
            _description = "Filters log messages based on log level";
            _filterLevel = LogLevel.Debug;
            _allowHigherLevels = true;
            _allowLowerLevels = false;
            _allowExactLevel = true;
            _useRangeFiltering = false;
            _minLevel = LogLevel.Debug;
            _maxLevel = LogLevel.Critical;
            _allowedLevels.Clear();
            _blockedLevels.Clear();
            _useSpecificLevels = false;
            _enableDynamicLevels = false;
            _dynamicLevelSource = "Environment";
            _dynamicLevelKey = "LOG_LEVEL";
            _fallbackLevel = LogLevel.Info;
            _enableLevelMapping = false;
            _levelMappings.Clear();
        }

        /// <summary>
        /// Performs level filter specific validation.
        /// </summary>
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();

            // Validate strings
            if (string.IsNullOrWhiteSpace(_dynamicLevelSource))
            {
                _dynamicLevelSource = "Environment";
            }

            if (string.IsNullOrWhiteSpace(_dynamicLevelKey))
            {
                _dynamicLevelKey = "LOG_LEVEL";
            }

            // Ensure range is valid
            if (_useRangeFiltering && _minLevel > _maxLevel)
            {
                var temp = _minLevel;
                _minLevel = _maxLevel;
                _maxLevel = temp;
            }

            // Remove duplicates from lists
            _allowedLevels = new List<LogLevel>(_allowedLevels);
            _blockedLevels = new List<LogLevel>(_blockedLevels);
        }

        /// <summary>
        /// Checks if a log level should be allowed through the filter.
        /// </summary>
        /// <param name="level">The log level to check</param>
        /// <returns>True if the level should be allowed</returns>
        public bool ShouldAllowLevel(LogLevel level)
        {
            // Check specific levels first
            if (_useSpecificLevels)
            {
                if (_blockedLevels.Contains(level))
                {
                    return false;
                }

                if (_allowedLevels.Count > 0)
                {
                    return _allowedLevels.Contains(level);
                }
            }

            // Check range filtering
            if (_useRangeFiltering)
            {
                return level >= _minLevel && level <= _maxLevel;
            }

            // Check standard level filtering
            var comparison = level.CompareTo(_filterLevel);
            
            if (comparison == 0)
            {
                return _allowExactLevel;
            }
            else if (comparison > 0)
            {
                return _allowHigherLevels;
            }
            else
            {
                return _allowLowerLevels;
            }
        }

        /// <summary>
        /// Gets the effective filter level, considering dynamic levels.
        /// </summary>
        /// <returns>The effective filter level</returns>
        public LogLevel GetEffectiveFilterLevel()
        {
            if (_enableDynamicLevels)
            {
                return GetDynamicLevel();
            }

            return _filterLevel;
        }

        /// <summary>
        /// Gets the dynamic level from the specified source.
        /// </summary>
        /// <returns>The dynamic level or fallback level</returns>
        private LogLevel GetDynamicLevel()
        {
            string levelValue = null;

            switch (_dynamicLevelSource.ToLower())
            {
                case "environment":
                    levelValue = System.Environment.GetEnvironmentVariable(_dynamicLevelKey);
                    break;
                case "playerprefs":
                    levelValue = PlayerPrefs.GetString(_dynamicLevelKey, "");
                    break;
                case "commandline":
                    levelValue = GetCommandLineArgument(_dynamicLevelKey);
                    break;
            }

            if (string.IsNullOrWhiteSpace(levelValue))
            {
                return _fallbackLevel;
            }

            if (System.Enum.TryParse<LogLevel>(levelValue, true, out var level))
            {
                return level;
            }

            return _fallbackLevel;
        }

        /// <summary>
        /// Gets a command line argument value.
        /// </summary>
        /// <param name="key">The argument key</param>
        /// <returns>The argument value or null</returns>
        private string GetCommandLineArgument(string key)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals($"-{key}", System.StringComparison.OrdinalIgnoreCase) ||
                    args[i].Equals($"--{key}", System.StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        /// <summary>
        /// Maps a level using the configured level mappings.
        /// </summary>
        /// <param name="level">The original level</param>
        /// <returns>The mapped level</returns>
        public LogLevel MapLevel(LogLevel level)
        {
            if (!_enableLevelMapping)
            {
                return level;
            }

            foreach (var mapping in _levelMappings)
            {
                if (mapping.From == level)
                {
                    return mapping.To;
                }
            }

            return level;
        }

        /// <summary>
        /// Adds an allowed level.
        /// </summary>
        /// <param name="level">The level to allow</param>
        public void AddAllowedLevel(LogLevel level)
        {
            if (!_allowedLevels.Contains(level))
            {
                _allowedLevels.Add(level);
                _useSpecificLevels = true;
            }
        }

        /// <summary>
        /// Removes an allowed level.
        /// </summary>
        /// <param name="level">The level to remove</param>
        public void RemoveAllowedLevel(LogLevel level)
        {
            _allowedLevels.Remove(level);
        }

        /// <summary>
        /// Adds a blocked level.
        /// </summary>
        /// <param name="level">The level to block</param>
        public void AddBlockedLevel(LogLevel level)
        {
            if (!_blockedLevels.Contains(level))
            {
                _blockedLevels.Add(level);
                _useSpecificLevels = true;
            }
        }

        /// <summary>
        /// Removes a blocked level.
        /// </summary>
        /// <param name="level">The level to remove</param>
        public void RemoveBlockedLevel(LogLevel level)
        {
            _blockedLevels.Remove(level);
        }

        /// <summary>
        /// Adds a level mapping.
        /// </summary>
        /// <param name="from">The source level</param>
        /// <param name="to">The target level</param>
        public void AddLevelMapping(LogLevel from, LogLevel to)
        {
            var mapping = new LevelMapping { From = from, To = to };
            
            // Remove existing mapping for the same source level
            _levelMappings.RemoveAll(m => m.From == from);
            
            _levelMappings.Add(mapping);
            _enableLevelMapping = true;
        }

        /// <summary>
        /// Removes a level mapping.
        /// </summary>
        /// <param name="from">The source level</param>
        public void RemoveLevelMapping(LogLevel from)
        {
            _levelMappings.RemoveAll(m => m.From == from);
            
            if (_levelMappings.Count == 0)
            {
                _enableLevelMapping = false;
            }
        }

        /// <summary>
        /// Gets a summary of the filter configuration.
        /// </summary>
        /// <returns>Configuration summary</returns>
        public override string CreateTestConfiguration()
        {
            var config = base.CreateTestConfiguration();
            
            if (_useRangeFiltering)
            {
                config += $"Range: {_minLevel} - {_maxLevel}\n";
            }
            else if (_useSpecificLevels)
            {
                config += $"Allowed: {string.Join(", ", _allowedLevels)}\n";
                config += $"Blocked: {string.Join(", ", _blockedLevels)}\n";
            }
            else
            {
                config += $"Filter Level: {_filterLevel}\n";
                config += $"Allow Higher: {_allowHigherLevels}, Lower: {_allowLowerLevels}, Exact: {_allowExactLevel}\n";
            }
            
            if (_enableDynamicLevels)
            {
                config += $"Dynamic Source: {_dynamicLevelSource}[{_dynamicLevelKey}]\n";
            }
            
            return config;
        }
    }

    /// <summary>
    /// Represents a level mapping from one log level to another.
    /// </summary>
    [System.Serializable]
    public class LevelMapping
    {
        [SerializeField] public LogLevel From;
        [SerializeField] public LogLevel To;
    }
}