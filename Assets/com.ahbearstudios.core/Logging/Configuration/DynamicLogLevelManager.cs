using System.Collections.Generic;
using AhBearStudios.Core.Logging.Middleware;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Tags;
using UnityEngine;

namespace AhBearStudios.Core.Logging.Config
{
    /// <summary>
    /// Manages dynamic log level configuration at runtime.
    /// </summary>
    public class DynamicLogLevelManager : ILogMiddleware
    {
        private readonly Dictionary<Tagging.LogTag, byte> _tagLevelOverrides = new Dictionary<Tagging.LogTag, byte>();
        private readonly Dictionary<string, byte> _categoryLevelOverrides = new Dictionary<string, byte>();
        private byte _globalMinimumLevel;
        
        /// <summary>
        /// The next middleware in the chain.
        /// </summary>
        public ILogMiddleware Next { get; set; }
        
        /// <summary>
        /// Gets or sets the global minimum log level. Messages below this level will be filtered out.
        /// </summary>
        public byte GlobalMinimumLevel
        {
            get => _globalMinimumLevel;
            set => _globalMinimumLevel = value;
        }
        
        /// <summary>
        /// Sets a log level override for a specific tag.
        /// </summary>
        /// <param name="tag">The tag to override.</param>
        /// <param name="level">The minimum level to log for this tag.</param>
        public void SetTagLevelOverride(Tagging.LogTag tag, byte level)
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
        public void SetCategoryLevelOverride(string category, byte level)
        {
            _categoryLevelOverrides[category] = level;
        }
        
        /// <summary>
        /// Removes a category-specific log level override.
        /// </summary>
        /// <param name="category">The category to remove the override for.</param>
        /// <returns>True if an override was removed, false otherwise.</returns>
        public bool RemoveCategoryLevelOverride(string category)
        {
            return _categoryLevelOverrides.Remove(category);
        }
        
        /// <summary>
        /// Applies a log level profile.
        /// </summary>
        /// <param name="profile">The profile to apply.</param>
        public void ApplyProfile(LogLevelProfile profile)
        {
            if (profile == null)
                return;
                
            _globalMinimumLevel = profile.GlobalMinimumLevel;
            
            // Apply tag overrides
            _tagLevelOverrides.Clear();
            foreach (var tagOverride in profile.TagLevelOverrides)
            {
                _tagLevelOverrides[tagOverride.Tag] = tagOverride.Level;
            }
            
            // Apply category overrides
            _categoryLevelOverrides.Clear();
            foreach (var categoryOverride in profile.CategoryLevelOverrides)
            {
                _categoryLevelOverrides[categoryOverride.Category] = categoryOverride.Level;
            }
        }
        
        /// <summary>
        /// Process a log message by filtering based on dynamic log levels.
        /// </summary>
        /// <param name="message">The log message to filter.</param>
        /// <returns>True to continue processing, false to stop.</returns>
        public bool Process(ref LogMessage message)
        {
            // Apply message filtering based on configured log levels
            if (message.Level < _globalMinimumLevel)
                return false;
                
            // Check tag-specific overrides
            if (_tagLevelOverrides.TryGetValue((Tagging.LogTag)message.Tag, out byte tagLevel) && 
                message.Level < tagLevel)
            {
                return false;
            }
            
            // Check category-specific overrides if properties contain a category
            if (message.Properties.IsCreated)
            {
                foreach (var prop in message.Properties)
                {
                    if (prop.Key == LogPropertyKeys.Category)
                    {
                        string category = prop.Value.ToString();
                        if (_categoryLevelOverrides.TryGetValue(category, out byte categoryLevel) &&
                            message.Level < categoryLevel)
                        {
                            return false;
                        }
                        break;
                    }
                }
            }
            
            // Continue processing
            return Next?.Process(ref message) ?? true;
        }
    }
}