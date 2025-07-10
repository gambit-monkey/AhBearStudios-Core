using System.Collections.Generic;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Tags;

    /// <summary>
    /// Manages global and per-<see cref="Tagging.LogTag"/> minimum levels and implements <see cref="ILogLevelManager"/>.
    /// </summary>
    public sealed class DynamicLogLevelManager : ILogLevelManager
    {
        private readonly Dictionary<Tagging.LogTag, LogLevel> _tagLevelOverrides = new();
        private readonly Dictionary<string, LogLevel> _categoryLevelOverrides = new();
        private LogLevel _globalMinimumLevel;
        private readonly LogLevel _defaultGlobalLevel;

        /// <summary>
        /// Creates a new DynamicLogLevelManager with default level <see cref="LogLevel.Debug"/>.
        /// </summary>
        public DynamicLogLevelManager() : this(LogLevel.Debug) { }

        /// <summary>
        /// Creates a new DynamicLogLevelManager with the specified default global level.
        /// </summary>
        public DynamicLogLevelManager(LogLevel defaultLevel)
        {
            _defaultGlobalLevel = defaultLevel;
            _globalMinimumLevel = defaultLevel;
        }

        /// <inheritdoc/>
        public LogLevel GlobalMinimumLevel
        {
            get => _globalMinimumLevel;
            set => _globalMinimumLevel = value;
        }

        /// <inheritdoc/>
        public void SetTagLevelOverride(Tagging.LogTag tag, LogLevel level)
            => _tagLevelOverrides[tag] = level;

        /// <inheritdoc/>
        public bool RemoveTagLevelOverride(Tagging.LogTag tag)
            => _tagLevelOverrides.Remove(tag);

        /// <inheritdoc/>
        public void SetCategoryLevelOverride(string category, LogLevel level)
        {
            if (!string.IsNullOrEmpty(category))
                _categoryLevelOverrides[category] = level;
        }

        /// <inheritdoc/>
        public bool RemoveCategoryLevelOverride(string category)
            => string.IsNullOrEmpty(category) ? false : _categoryLevelOverrides.Remove(category);

        /// <inheritdoc/>
        public void ApplyProfile(LogLevelProfile profile)
        {
            if (profile == null) return;
            var runtime = profile.ToRuntimeProfile();
            ApplyProfile(runtime);
        }

        /// <inheritdoc/>
        public void ApplyProfile(RuntimeLogLevelProfile profile)
        {
            if (profile == null) return;
            _globalMinimumLevel = profile.GlobalMinimumLevel;

            _tagLevelOverrides.Clear();
            if (profile.TagLevelOverrides != null)
            {
                foreach (var kv in profile.TagLevelOverrides)
                    _tagLevelOverrides[kv.Tag] = kv.Level;
            }

            _categoryLevelOverrides.Clear();
            if (profile.CategoryLevelOverrides != null)
            {
                foreach (var kv in profile.CategoryLevelOverrides)
                    if (!string.IsNullOrEmpty(kv.Category))
                        _categoryLevelOverrides[kv.Category] = kv.Level;
            }
        }

        /// <inheritdoc/>
        public void ResetToDefaults()
        {
            _globalMinimumLevel = _defaultGlobalLevel;
            _tagLevelOverrides.Clear();
            _categoryLevelOverrides.Clear();
        }

        /// <inheritdoc/>
        public bool ShouldLog(LogLevel level, Tagging.LogTag tag, string category = null)
        {
            if (level < _globalMinimumLevel)
                return false;
            if (_tagLevelOverrides.TryGetValue(tag, out var tagLevel) && level < tagLevel)
                return false;
            if (!string.IsNullOrEmpty(category)
                && _categoryLevelOverrides.TryGetValue(category, out var catLevel)
                && level < catLevel)
                return false;
            return true;
        }

        /// <inheritdoc/>
        public LogLevel GetEffectiveLevel(Tagging.LogTag tag, string category = null)
        {
            var effective = _globalMinimumLevel;
            if (_tagLevelOverrides.TryGetValue(tag, out var tagLevel) && tagLevel > effective)
                effective = tagLevel;
            if (!string.IsNullOrEmpty(category)
                && _categoryLevelOverrides.TryGetValue(category, out var catLevel)
                && catLevel > effective)
                effective = catLevel;
            return effective;
        }
    }