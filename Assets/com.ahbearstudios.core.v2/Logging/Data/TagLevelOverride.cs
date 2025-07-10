using System;
using AhBearStudios.Core.Logging.Tags;
using UnityEngine;

namespace AhBearStudios.Core.Logging.Data
{
    /// <summary>
    /// Represents a tag-specific log level override.
    /// Allows specific log tags to have different minimum levels than the global setting.
    /// </summary>
    [Serializable]
    public class TagLevelOverride : IEquatable<TagLevelOverride>
    {
        [SerializeField, Tooltip("The log tag to apply this override to")]
        private Tagging.LogTag _tag = Tagging.LogTag.Default;
        
        [SerializeField, Tooltip("The minimum log level for this tag")]
        private LogLevel _level = LogLevel.Debug;
        
        /// <summary>
        /// Gets or sets the log tag to override.
        /// </summary>
        public Tagging.LogTag Tag 
        { 
            get => _tag; 
            set => _tag = value; 
        }
        
        /// <summary>
        /// Gets or sets the minimum log level for this tag.
        /// </summary>
        public LogLevel Level 
        { 
            get => _level; 
            set => _level = value; 
        }
        
        /// <summary>
        /// Creates a new empty tag level override.
        /// </summary>
        public TagLevelOverride()
        {
        }
        
        /// <summary>
        /// Creates a new tag level override with the specified values.
        /// </summary>
        /// <param name="tag">The tag to override.</param>
        /// <param name="level">The minimum log level for this tag.</param>
        public TagLevelOverride(Tagging.LogTag tag, LogLevel level)
        {
            _tag = tag;
            _level = level;
        }
        
        /// <summary>
        /// Checks if this override applies to the specified tag.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if this override applies to the tag.</returns>
        public bool AppliesTo(Tagging.LogTag tag)
        {
            return _tag == tag;
        }
        
        /// <summary>
        /// Checks if the specified log level meets this override's requirements.
        /// </summary>
        /// <param name="level">The log level to check.</param>
        /// <returns>True if the level meets or exceeds the override level.</returns>
        public bool IsLevelSufficient(LogLevel level)
        {
            return level >= _level;
        }
        
        /// <summary>
        /// Creates a copy of this override.
        /// </summary>
        /// <returns>A new TagLevelOverride with the same values.</returns>
        public TagLevelOverride Clone()
        {
            return new TagLevelOverride(_tag, _level);
        }
        
        /// <summary>
        /// Determines whether the specified TagLevelOverride is equal to this instance.
        /// </summary>
        /// <param name="other">The TagLevelOverride to compare with this instance.</param>
        /// <returns>True if the specified TagLevelOverride is equal to this instance; otherwise, false.</returns>
        public bool Equals(TagLevelOverride other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _tag == other._tag && _level == other._level;
        }
        
        /// <summary>
        /// Determines whether the specified object is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns>True if the specified object is equal to this instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as TagLevelOverride);
        }
        
        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)_tag * 397) ^ (int)_level;
            }
        }
        
        /// <summary>
        /// Implements the equality operator.
        /// </summary>
        public static bool operator ==(TagLevelOverride left, TagLevelOverride right)
        {
            return Equals(left, right);
        }
        
        /// <summary>
        /// Implements the inequality operator.
        /// </summary>
        public static bool operator !=(TagLevelOverride left, TagLevelOverride right)
        {
            return !Equals(left, right);
        }
        
        /// <summary>
        /// Returns a string representation of this override.
        /// </summary>
        /// <returns>A string that represents this override.</returns>
        public override string ToString()
        {
            return $"Tag '{_tag}' -> {_level}";
        }
        
        /// <summary>
        /// Returns a detailed description of this override.
        /// </summary>
        /// <returns>A detailed string description.</returns>
        public string GetDetailedDescription()
        {
            var tagCategory = Tagging.GetTagCategory(_tag);
            return $"Tag Override: {_tag} (Category: {tagCategory}) requires minimum level {_level}";
        }
    }
}