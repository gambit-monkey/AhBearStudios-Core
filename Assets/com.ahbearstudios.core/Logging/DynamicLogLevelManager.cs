using System.Collections.Generic;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Manages dynamic log level configuration at runtime.
    /// Provides pure log level management functionality without mixing middleware concerns.
    /// </summary>
    public class DynamicLogLevelManager : ILogLevelManager
    {
        private readonly Dictionary<Tagging.LogTag, LogLevel> _tagLevelOverrides = new Dictionary<Tagging.LogTag, LogLevel>();
        private readonly Dictionary<string, LogLevel> _categoryLevelOverrides = new Dictionary<string, LogLevel>();
        private LogLevel _globalMinimumLevel = LogLevel.Debug; // Default minimum level
        private LogLevel _defaultGlobalLevel = LogLevel.Debug; // Store default for reset
        
        /// <summary>
        /// Gets or sets the global minimum log level. Messages below this level will be filtered out.
        /// </summary>
        public LogLevel GlobalMinimumLevel
        {
            get => _globalMinimumLevel;
            set => _globalMinimumLevel = value;
        }
        
        /// <summary>
        /// Creates a new DynamicLogLevelManager with default settings.
        /// </summary>
        public DynamicLogLevelManager() : this(LogLevel.Debug)
        {
        }
        
        /// <summary>
        /// Creates a new DynamicLogLevelManager with the specified default level.
        /// </summary>
        /// <param name="defaultLevel">The default global minimum log level.</param>
        public DynamicLogLevelManager(LogLevel defaultLevel)
        {
            _defaultGlobalLevel = defaultLevel;
            _globalMinimumLevel = defaultLevel;
        }
        
        /// <summary>
        /// Sets a log level override for a specific tag.
        /// </summary>
        /// <param name="tag">The tag to override.</param>
        /// <param name="level">The minimum level to log for this tag.</param>
        public void SetTagLevelOverride(Tagging.LogTag tag, LogLevel level)
        {
            _tagLevelOverrides[tag] = level;
        }
        
        /// <summary>
        /// Removes a tag-specific log level override.
        /// </summary>
        /// <param name="tag">The tag to remove the override for.</param>
        /// <returns>True if an override was removed, false otherwise.</returns>
        public bool RemoveTagLevelOverride(Tagging.LogTag tag)
        {
            return _tagLevelOverrides.Remove(tag);
        }
        
        /// <summary>
        /// Sets a log level override for a category (usually a subsystem or component name).
        /// </summary>
        /// <param name="category">The category to override.</param>
        /// <param name="level">The minimum level to log for this category.</param>
        public void SetCategoryLevelOverride(string category, LogLevel level)
        {
            if (!string.IsNullOrEmpty(category))
            {
                _categoryLevelOverrides[category] = level;
            }
        }
        
        /// <summary>
        /// Removes a category-specific log level override.
        /// </summary>
        /// <param name="category">The category to remove the override for.</param>
        /// <returns>True if an override was removed, false otherwise.</returns>
        public bool RemoveCategoryLevelOverride(string category)
        {
            if (string.IsNullOrEmpty(category))
                return false;
                
            return _categoryLevelOverrides.Remove(category);
        }
        
        /// <summary>
        /// Applies a Unity ScriptableObject log level profile.
        /// </summary>
        /// <param name="profile">The ScriptableObject profile to apply.</param>
        public void ApplyProfile(LogLevelProfile profile)
        {
            if (profile == null)
                return;
                
            // Convert to runtime profile and apply
            var runtimeProfile = profile.ToRuntimeProfile();
            ApplyProfile(runtimeProfile);
        }
        
        /// <summary>
        /// Applies a runtime log level profile.
        /// </summary>
        /// <param name="profile">The runtime profile to apply.</param>
        public void ApplyProfile(RuntimeLogLevelProfile profile)
        {
            if (profile == null)
                return;
                
            _globalMinimumLevel = profile.GlobalMinimumLevel;
            
            // Apply tag overrides
            _tagLevelOverrides.Clear();
            if (profile.TagLevelOverrides != null)
            {
                foreach (var tagOverride in profile.TagLevelOverrides)
                {
                    _tagLevelOverrides[tagOverride.Tag] = tagOverride.Level;
                }
            }
            
            // Apply category overrides
            _categoryLevelOverrides.Clear();
            if (profile.CategoryLevelOverrides != null)
            {
                foreach (var categoryOverride in profile.CategoryLevelOverrides)
                {
                    if (!string.IsNullOrEmpty(categoryOverride.Category))
                    {
                        _categoryLevelOverrides[categoryOverride.Category] = categoryOverride.Level;
                    }
                }
            }
        }
        
        /// <summary>
        /// Resets all overrides to defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            _globalMinimumLevel = _defaultGlobalLevel;
            _tagLevelOverrides.Clear();
            _categoryLevelOverrides.Clear();
        }
        
        /// <summary>
        /// Sets the default global level (used when resetting).
        /// </summary>
        /// <param name="defaultLevel">The default level to use for resets.</param>
        public void SetDefaultLevel(LogLevel defaultLevel)
        {
            _defaultGlobalLevel = defaultLevel;
        }
        
        /// <summary>
        /// Checks if a message should be logged based on current level configuration.
        /// </summary>
        /// <param name="level">The log level of the message.</param>
        /// <param name="tag">The tag of the message.</param>
        /// <param name="category">The category of the message (optional).</param>
        /// <returns>True if the message should be logged, false otherwise.</returns>
        public bool ShouldLog(LogLevel level, Tagging.LogTag tag, string category = null)
        {
            // Check global minimum level first
            if (level < _globalMinimumLevel)
                return false;
                
            // Check tag-specific overrides
            if (_tagLevelOverrides.TryGetValue(tag, out LogLevel tagLevel))
            {
                if (level < tagLevel)
                    return false;
            }
            
            // Check category-specific overrides
            if (!string.IsNullOrEmpty(category) && 
                _categoryLevelOverrides.TryGetValue(category, out LogLevel categoryLevel))
            {
                if (level < categoryLevel)
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Gets the effective minimum level for a specific tag and category combination.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <param name="category">The category to check (optional).</param>
        /// <returns>The effective minimum log level.</returns>
        public LogLevel GetEffectiveLevel(Tagging.LogTag tag, string category = null)
        {
            LogLevel effectiveLevel = _globalMinimumLevel;
            
            // Check tag-specific override
            if (_tagLevelOverrides.TryGetValue(tag, out LogLevel tagLevel))
            {
                effectiveLevel = (LogLevel)System.Math.Max((int)effectiveLevel, (int)tagLevel);
            }
            
            // Check category-specific override
            if (!string.IsNullOrEmpty(category) && 
                _categoryLevelOverrides.TryGetValue(category, out LogLevel categoryLevel))
            {
                effectiveLevel = (LogLevel)System.Math.Max((int)effectiveLevel, (int)categoryLevel);
            }
            
            return effectiveLevel;
        }
        
        /// <summary>
        /// Gets all currently configured tag overrides.
        /// </summary>
        /// <returns>A read-only dictionary of tag overrides.</returns>
        public IReadOnlyDictionary<Tagging.LogTag, LogLevel> GetTagOverrides()
        {
            return _tagLevelOverrides;
        }
        
        /// <summary>
        /// Gets all currently configured category overrides.
        /// </summary>
        /// <returns>A read-only dictionary of category overrides.</returns>
        public IReadOnlyDictionary<string, LogLevel> GetCategoryOverrides()
        {
            return _categoryLevelOverrides;
        }
        
        /// <summary>
        /// Gets the number of configured tag overrides.
        /// </summary>
        public int TagOverrideCount => _tagLevelOverrides.Count;
        
        /// <summary>
        /// Gets the number of configured category overrides.
        /// </summary>
        public int CategoryOverrideCount => _categoryLevelOverrides.Count;
        
        /// <summary>
        /// Creates a snapshot of the current configuration.
        /// </summary>
        /// <returns>A RuntimeLogLevelProfile representing the current state.</returns>
        public RuntimeLogLevelProfile CreateSnapshot()
        {
            var profile = new RuntimeLogLevelProfile("Snapshot", _globalMinimumLevel);
    
            // Add tag overrides using the fluent API
            foreach (var kvp in _tagLevelOverrides)
            {
                profile.WithTagOverride(kvp.Key, kvp.Value);
            }
    
            // Add category overrides using the fluent API
            foreach (var kvp in _categoryLevelOverrides)
            {
                profile.WithCategoryOverride(kvp.Key, kvp.Value);
            }
    
            return profile;
        }
    }
}