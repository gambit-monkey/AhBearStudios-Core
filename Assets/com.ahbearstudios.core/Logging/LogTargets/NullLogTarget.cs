using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Targets
{
    /// <summary>
    /// A no-op implementation of <see cref="ILogTarget"/>, which silently discards all log messages.
    /// Useful for disabling output or in unit tests.
    /// </summary>
    public sealed class NullLogTarget : ILogTarget
    {
        /// <inheritdoc/>
        public string Name => "NullLogTarget";

        /// <inheritdoc/>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

        /// <inheritdoc/>
        public bool IsEnabled { get; set; } = true;

        // Tag filters (not really used since we drop everything, but kept for contract)
        private readonly HashSet<Tagging.TagCategory> _includedCategories = new();
        private readonly HashSet<Tagging.TagCategory> _excludedCategories = new();
        private readonly HashSet<Tagging.LogTag> _includedLogTags    = new();
        private readonly HashSet<Tagging.LogTag> _excludedLogTags    = new();
        private bool _processUntaggedMessages = true;

        /// <inheritdoc/>
        public void WriteBatch(NativeList<LogMessage> entries)
        {
            // no-op
        }

        /// <inheritdoc/>
        public void Write(in LogMessage entry)
        {
            // no-op
        }

        /// <inheritdoc/>
        public void Flush()
        {
            // no-op
        }

        /// <inheritdoc/>
        public bool IsLevelEnabled(LogLevel level)
        {
            if (!IsEnabled) return false;
            return level >= MinimumLevel;
        }

        /// <inheritdoc/>
        public void AddTagFilter(Tagging.TagCategory tagCategory)
        {
            _includedCategories.Add(tagCategory);
        }

        /// <inheritdoc/>
        public void RemoveTagFilter(Tagging.TagCategory tagCategory)
        {
            _includedCategories.Remove(tagCategory);
        }

        /// <inheritdoc/>
        public void ClearTagFilters()
        {
            _includedCategories.Clear();
            _excludedCategories.Clear();
            _includedLogTags.Clear();
            _excludedLogTags.Clear();
            _processUntaggedMessages = true;
        }

        /// <inheritdoc/>
        public void AddLogTagFilter(Tagging.LogTag logTag)
        {
            _includedLogTags.Add(logTag);
        }

        /// <inheritdoc/>
        public void RemoveLogTagFilter(Tagging.LogTag logTag)
        {
            _includedLogTags.Remove(logTag);
        }

        /// <inheritdoc/>
        public bool IsLogTagEnabled(Tagging.LogTag logTag)
        {
            if (!IsEnabled) return false;
            if (_includedLogTags.Count > 0) return _includedLogTags.Contains(logTag);
            if (_excludedLogTags.Count > 0) return !_excludedLogTags.Contains(logTag);
            return _processUntaggedMessages;
        }

        /// <inheritdoc/>
        public bool ShouldProcessMessage(in LogMessage logMessage)
        {
            if (!IsEnabled) 
                return false;

            if (!IsLevelEnabled(logMessage.Level)) 
                return false;

            // Map the LogTag → TagCategory once
            var category = Tagging.GetTagCategory(logMessage.Tag);

            // Category filters (by TagCategory)
            if (_includedCategories.Count > 0 && !_includedCategories.Contains(category))
                return false;
            if (_excludedCategories.Count > 0 && _excludedCategories.Contains(category))
                return false;

            // LogTag filters (by LogTag)
            if (_includedLogTags.Count > 0 && !_includedLogTags.Contains(logMessage.Tag))
                return false;
            if (_excludedLogTags.Count > 0 && _excludedLogTags.Contains(logMessage.Tag))
                return false;

            // Untagged messages fall through only if allowed
            if (category == Tagging.TagCategory.None && !_processUntaggedMessages)
                return false;

            return true;
        }

        /// <inheritdoc/>
        public void SetTagFilters(string[] includedTags, string[] excludedTags, bool processUntaggedMessages)
        {
            // For a null target we'll just clear existing filters
            ClearTagFilters();
            _processUntaggedMessages = processUntaggedMessages;
            // (Could parse included/excludedTags into LogTag or TagCategory here if desired)
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // nothing to dispose
        }
    }
}
