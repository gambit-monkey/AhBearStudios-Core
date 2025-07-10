using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Tags;
using UnityEngine;

namespace AhBearStudios.Core.Logging.Configuration
{
    /// <summary>
    /// Represents a log level profile that can be saved and loaded as a Unity asset.
    /// Provides configuration for global log levels and specific overrides for tags and categories.
    /// </summary>
    [CreateAssetMenu(fileName = "LogLevelProfile", menuName = "AhBearStudios/Logging/Log Level Profile", order = 1)]
    public class LogLevelProfile : ScriptableObject
    {
        [Header("Global Settings")]
        [SerializeField, Tooltip("Global minimum log level - messages below this level will be filtered out")]
        private LogLevel _globalMinimumLevel = LogLevel.Debug;

        [SerializeField, Tooltip("Description of this profile's purpose")] [TextArea(2, 4)]
        private string _description = "";

        [Header("Tag Overrides")]
        [SerializeField, Tooltip("Tag-specific level overrides that take precedence over global level")]
        private TagLevelOverride[] _tagLevelOverrides = new TagLevelOverride[0];

        [Header("Category Overrides")]
        [SerializeField, Tooltip("Category-specific level overrides for custom logging categories")]
        private CategoryLevelOverride[] _categoryLevelOverrides = new CategoryLevelOverride[0];

        [Header("Advanced Settings")]
        [SerializeField, Tooltip("Whether this profile should be applied automatically on startup")]
        private bool _autoApplyOnStartup = false;

        [SerializeField, Tooltip("Priority when multiple profiles are available (higher = applied first)")]
        private int _priority = 0;

        [SerializeField, Tooltip("Environment tags where this profile is applicable")]
        private string[] _applicableEnvironments = new string[] { "Development", "Staging", "Production" };

        /// <summary>
        /// Global minimum log level.
        /// </summary>
        public LogLevel GlobalMinimumLevel => _globalMinimumLevel;

        /// <summary>
        /// Description of this profile.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Tag-specific level overrides.
        /// </summary>
        public TagLevelOverride[] TagLevelOverrides => _tagLevelOverrides;

        /// <summary>
        /// Category-specific level overrides.
        /// </summary>
        public CategoryLevelOverride[] CategoryLevelOverrides => _categoryLevelOverrides;

        /// <summary>
        /// Whether this profile should be applied automatically on startup.
        /// </summary>
        public bool AutoApplyOnStartup => _autoApplyOnStartup;

        /// <summary>
        /// Priority when multiple profiles are available (higher = applied first).
        /// </summary>
        public int Priority => _priority;

        /// <summary>
        /// Environment tags where this profile is applicable.
        /// </summary>
        public string[] ApplicableEnvironments => _applicableEnvironments;

        /// <summary>
        /// Gets the profile name (uses the asset name).
        /// </summary>
        public string ProfileName => name;

        /// <summary>
        /// Checks if a log level should be processed based on this profile's configuration.
        /// </summary>
        /// <param name="level">The log level to check.</param>
        /// <param name="tag">The log tag to check.</param>
        /// <param name="category">The log category to check (optional).</param>
        /// <returns>True if the message should be logged, false otherwise.</returns>
        public bool ShouldLog(LogLevel level, Tagging.LogTag tag, string category = null)
        {
            // Check global minimum level first
            if (level < _globalMinimumLevel)
                return false;

            // Check tag-specific overrides
            var tagOverride = Array.Find(_tagLevelOverrides, t => t.Tag == tag);
            if (!tagOverride.Equals(default(TagLevelOverride)) && level < tagOverride.Level)
                return false;

            // Check category-specific overrides
            if (!string.IsNullOrEmpty(category))
            {
                var categoryOverride = Array.Find(_categoryLevelOverrides, c =>
                    string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase));
                if (!categoryOverride.Equals(default(CategoryLevelOverride)) && level < categoryOverride.Level)
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
            var tagOverride = Array.Find(_tagLevelOverrides, t => t.Tag == tag);
            if (tagOverride != null)
            {
                effectiveLevel = (LogLevel)Math.Max((int)effectiveLevel, (int)tagOverride.Level);
            }

            // Check category-specific override
            if (!string.IsNullOrEmpty(category))
            {
                var categoryOverride = Array.Find(_categoryLevelOverrides, c =>
                    string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase));
                if (categoryOverride != null)
                {
                    effectiveLevel = (LogLevel)Math.Max((int)effectiveLevel, (int)categoryOverride.Level);
                }
            }

            return effectiveLevel;
        }

        /// <summary>
        /// Checks if this profile is applicable for the given environment.
        /// </summary>
        /// <param name="environment">The environment to check (e.g., "Development", "Production").</param>
        /// <returns>True if this profile is applicable for the environment.</returns>
        public bool IsApplicableForEnvironment(string environment)
        {
            if (string.IsNullOrEmpty(environment))
                return true;

            if (_applicableEnvironments == null || _applicableEnvironments.Length == 0)
                return true;

            return Array.Exists(_applicableEnvironments, env =>
                string.Equals(env, environment, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a summary of this profile's configuration.
        /// </summary>
        /// <returns>A formatted string describing the profile.</returns>
        public string GetConfigurationSummary()
        {
            var summary = $"Profile: {ProfileName}\n";
            summary += $"Global Level: {_globalMinimumLevel}\n";

            if (_tagLevelOverrides != null && _tagLevelOverrides.Length > 0)
            {
                summary += $"Tag Overrides: {_tagLevelOverrides.Length}\n";
                foreach (var tagOverride in _tagLevelOverrides.Take(3)) // Show first 3
                {
                    summary += $"  {tagOverride.Tag} -> {tagOverride.Level}\n";
                }

                if (_tagLevelOverrides.Length > 3)
                    summary += $"  ... and {_tagLevelOverrides.Length - 3} more\n";
            }

            if (_categoryLevelOverrides != null && _categoryLevelOverrides.Length > 0)
            {
                summary += $"Category Overrides: {_categoryLevelOverrides.Length}\n";
                foreach (var categoryOverride in _categoryLevelOverrides.Take(3)) // Show first 3
                {
                    summary += $"  {categoryOverride.Category} -> {categoryOverride.Level}\n";
                }

                if (_categoryLevelOverrides.Length > 3)
                    summary += $"  ... and {_categoryLevelOverrides.Length - 3} more\n";
            }

            return summary;
        }


        /// <summary>
        /// Creates a runtime-compatible copy of this profile.
        /// Converts the ScriptableObject to a simple data structure.
        /// </summary>
        /// <returns>A RuntimeLogLevelProfile that can be used with ILogLevelManager.</returns>
        public RuntimeLogLevelProfile ToRuntimeProfile()
        {
            var profile = new RuntimeLogLevelProfile(ProfileName, _globalMinimumLevel)
                .WithDescription(_description)
                .WithPriority(_priority)
                .WithAutoApply(_autoApplyOnStartup);

            // Add tag overrides
            if (_tagLevelOverrides != null)
            {
                foreach (var tagOverride in _tagLevelOverrides)
                {
                    profile.WithTagOverride(tagOverride.Tag, tagOverride.Level);
                }
            }

            // Add category overrides
            if (_categoryLevelOverrides != null)
            {
                foreach (var categoryOverride in _categoryLevelOverrides)
                {
                    profile.WithCategoryOverride(categoryOverride.Category, categoryOverride.Level);
                }
            }

            // Add environments
            if (_applicableEnvironments != null && _applicableEnvironments.Length > 0)
            {
                profile.WithEnvironments(_applicableEnvironments);
            }

            return profile;
        }

        /// <summary>
        /// Validates this profile's configuration.
        /// </summary>
        /// <returns>True if the profile is valid, false otherwise.</returns>
        public bool Validate()
        {
            // Check for duplicate tag overrides
            if (_tagLevelOverrides != null && _tagLevelOverrides.Length > 0)
            {
                var tags = new HashSet<Tagging.LogTag>();
                foreach (var tagOverride in _tagLevelOverrides)
                {
                    if (!tags.Add(tagOverride.Tag))
                    {
                        Debug.LogWarning($"LogLevelProfile '{name}' has duplicate tag override for {tagOverride.Tag}");
                        return false;
                    }
                }
            }

            // Check for duplicate category overrides
            if (_categoryLevelOverrides != null && _categoryLevelOverrides.Length > 0)
            {
                var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var categoryOverride in _categoryLevelOverrides)
                {
                    if (!string.IsNullOrEmpty(categoryOverride.Category))
                    {
                        if (!categories.Add(categoryOverride.Category))
                        {
                            Debug.LogWarning(
                                $"LogLevelProfile '{name}' has duplicate category override for '{categoryOverride.Category}'");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only method to add a tag override.
        /// </summary>
        /// <param name="tag">The tag to override.</param>
        /// <param name="level">The minimum level for this tag.</param>
        public void AddTagOverride(Tagging.LogTag tag, LogLevel level)
        {
            var overridesList = new List<TagLevelOverride>(_tagLevelOverrides ?? new TagLevelOverride[0]);

            // Remove existing override for this tag
            overridesList.RemoveAll(t => t.Tag == tag);

            // Add new override using constructor
            overridesList.Add(new TagLevelOverride(tag, level));

            _tagLevelOverrides = overridesList.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Editor-only method to add a category override.
        /// </summary>
        /// <param name="category">The category to override.</param>
        /// <param name="level">The minimum level for this category.</param>
        public void AddCategoryOverride(string category, LogLevel level)
        {
            if (string.IsNullOrEmpty(category))
                return;

            var overridesList =
                new List<CategoryLevelOverride>(_categoryLevelOverrides ?? new CategoryLevelOverride[0]);

            // Remove existing override for this category
            overridesList.RemoveAll(c => string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase));

            // Add new override using constructor
            overridesList.Add(new CategoryLevelOverride(category, level));

            _categoryLevelOverrides = overridesList.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Editor-only method to set global minimum level.
        /// </summary>
        /// <param name="level">The new global minimum level.</param>
        public void SetGlobalMinimumLevel(LogLevel level)
        {
            _globalMinimumLevel = level;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        /// <summary>
        /// Provides a string representation of the profile.
        /// </summary>
        public override string ToString()
        {
            return
                $"LogLevelProfile '{ProfileName}' (Global: {_globalMinimumLevel}, Tags: {_tagLevelOverrides?.Length ?? 0}, Categories: {_categoryLevelOverrides?.Length ?? 0})";
        }
    }
}