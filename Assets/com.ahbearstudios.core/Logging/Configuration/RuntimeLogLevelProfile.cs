using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Configuration
{
    /// <summary>
    /// Pure C# implementation of log level profile.
    /// Used for runtime configuration and non-Unity contexts.
    /// </summary>
    [Serializable]
    public class RuntimeLogLevelProfile : ILogLevelProfile
    {
        private string _profileName = "Default";
        private string _description = "";
        private LogLevel _globalMinimumLevel = LogLevel.Debug;
        private List<TagLevelOverride> _tagLevelOverrides = new List<TagLevelOverride>();
        private List<CategoryLevelOverride> _categoryLevelOverrides = new List<CategoryLevelOverride>();
        private bool _autoApplyOnStartup = false;
        private int _priority = 0;
        private List<string> _applicableEnvironments = new List<string>();

        #region Properties

        public string ProfileName
        {
            get => _profileName;
            set => _profileName = value ?? "Default";
        }

        public string Description
        {
            get => _description;
            set => _description = value ?? "";
        }

        public LogLevel GlobalMinimumLevel
        {
            get => _globalMinimumLevel;
            set => _globalMinimumLevel = value;
        }

        public IReadOnlyList<TagLevelOverride> TagLevelOverrides => _tagLevelOverrides;
        public IReadOnlyList<CategoryLevelOverride> CategoryLevelOverrides => _categoryLevelOverrides;
        public bool AutoApplyOnStartup { get; set; }
        public int Priority { get; set; }
        public IReadOnlyList<string> ApplicableEnvironments => _applicableEnvironments;

        #endregion

        #region Constructors

        public RuntimeLogLevelProfile()
        {
        }

        public RuntimeLogLevelProfile(string name, LogLevel globalLevel = LogLevel.Debug)
        {
            _profileName = name ?? "Default";
            _globalMinimumLevel = globalLevel;
        }

        #endregion

        #region Fluent Configuration API

        public RuntimeLogLevelProfile WithName(string name)
        {
            _profileName = name ?? "Default";
            return this;
        }

        public RuntimeLogLevelProfile WithDescription(string description)
        {
            _description = description ?? "";
            return this;
        }

        public RuntimeLogLevelProfile WithGlobalLevel(LogLevel level)
        {
            _globalMinimumLevel = level;
            return this;
        }

        public RuntimeLogLevelProfile WithTagOverride(Tagging.LogTag tag, LogLevel level)
        {
            // Remove existing override for this tag
            _tagLevelOverrides.RemoveAll(t => t.Tag == tag);
            _tagLevelOverrides.Add(new TagLevelOverride { Tag = tag, Level = level });
            return this;
        }

        public RuntimeLogLevelProfile WithCategoryOverride(string category, LogLevel level)
        {
            if (!string.IsNullOrEmpty(category))
            {
                // Remove existing override for this category
                _categoryLevelOverrides.RemoveAll(c =>
                    string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase));
                _categoryLevelOverrides.Add(new CategoryLevelOverride { Category = category, Level = level });
            }

            return this;
        }

        public RuntimeLogLevelProfile WithPriority(int priority)
        {
            Priority = priority;
            return this;
        }

        public RuntimeLogLevelProfile WithAutoApply(bool autoApply = true)
        {
            AutoApplyOnStartup = autoApply;
            return this;
        }

        public RuntimeLogLevelProfile WithEnvironments(params string[] environments)
        {
            _applicableEnvironments.Clear();
            if (environments != null)
            {
                _applicableEnvironments.AddRange(environments);
            }

            return this;
        }

        #endregion

        #region ILogLevelProfile Implementation

        public bool ShouldLog(LogLevel level, Tagging.LogTag tag, string category = null)
        {
            // Check global minimum level first
            if (level < _globalMinimumLevel)
                return false;

            // Check tag-specific overrides
            var tagOverride = _tagLevelOverrides.Find(t => t.Tag == tag);
            if (tagOverride != null && level < tagOverride.Level)
                return false;

            // Check category-specific overrides
            if (!string.IsNullOrEmpty(category))
            {
                var categoryOverride = _categoryLevelOverrides.Find(c =>
                    string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase));
                if (categoryOverride != null && level < categoryOverride.Level)
                    return false;
            }

            return true;
        }

        public LogLevel GetEffectiveLevel(Tagging.LogTag tag, string category = null)
        {
            LogLevel effectiveLevel = _globalMinimumLevel;

            // Check tag-specific override
            var tagOverride = _tagLevelOverrides.Find(t => t.Tag == tag);
            if (tagOverride != null)
            {
                effectiveLevel = (LogLevel)Math.Max((int)effectiveLevel, (int)tagOverride.Level);
            }

            // Check category-specific override
            if (!string.IsNullOrEmpty(category))
            {
                var categoryOverride = _categoryLevelOverrides.Find(c =>
                    string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase));
                if (categoryOverride != null)
                {
                    effectiveLevel = (LogLevel)Math.Max((int)effectiveLevel, (int)categoryOverride.Level);
                }
            }

            return effectiveLevel;
        }

        public bool IsApplicableForEnvironment(string environment)
        {
            if (string.IsNullOrEmpty(environment))
                return true;

            if (_applicableEnvironments == null || _applicableEnvironments.Count == 0)
                return true;

            return _applicableEnvironments.Exists(env =>
                string.Equals(env, environment, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Utility Methods

        public RuntimeLogLevelProfile Clone()
        {
            var clone = new RuntimeLogLevelProfile(_profileName, _globalMinimumLevel)
            {
                Description = _description,
                AutoApplyOnStartup = AutoApplyOnStartup,
                Priority = Priority
            };

            foreach (var tagOverride in _tagLevelOverrides)
            {
                clone._tagLevelOverrides.Add(new TagLevelOverride
                {
                    Tag = tagOverride.Tag,
                    Level = tagOverride.Level
                });
            }

            foreach (var categoryOverride in _categoryLevelOverrides)
            {
                clone._categoryLevelOverrides.Add(new CategoryLevelOverride
                {
                    Category = categoryOverride.Category,
                    Level = categoryOverride.Level
                });
            }

            clone._applicableEnvironments.AddRange(_applicableEnvironments);

            return clone;
        }

        public override string ToString()
        {
            return
                $"RuntimeLogLevelProfile '{_profileName}' (Global: {_globalMinimumLevel}, Tags: {_tagLevelOverrides.Count}, Categories: {_categoryLevelOverrides.Count})";
        }

        #endregion
    }
}