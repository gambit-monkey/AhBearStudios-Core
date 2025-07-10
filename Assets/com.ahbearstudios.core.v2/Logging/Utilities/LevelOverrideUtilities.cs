using System;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Utilities
{
    /// <summary>
    /// Static utility class for working with level overrides.
    /// </summary>
    public static class LevelOverrideUtilities
    {
        /// <summary>
        /// Finds the most restrictive level from a collection of tag overrides for a specific tag.
        /// </summary>
        /// <param name="overrides">The collection of tag overrides to search.</param>
        /// <param name="tag">The tag to find overrides for.</param>
        /// <param name="defaultLevel">The default level to return if no override is found.</param>
        /// <returns>The most restrictive level found, or the default level.</returns>
        public static LogLevel GetMostRestrictiveTagLevel(
            System.Collections.Generic.IEnumerable<TagLevelOverride> overrides, 
            Tagging.LogTag tag, 
            LogLevel defaultLevel = LogLevel.Debug)
        {
            LogLevel mostRestrictive = defaultLevel;
            
            if (overrides != null)
            {
                foreach (var tagOverride in overrides)
                {
                    if (tagOverride.AppliesTo(tag))
                    {
                        if (tagOverride.Level > mostRestrictive)
                        {
                            mostRestrictive = tagOverride.Level;
                        }
                    }
                }
            }
            
            return mostRestrictive;
        }
        
        /// <summary>
        /// Finds the most restrictive level from a collection of category overrides for a specific category.
        /// </summary>
        /// <param name="overrides">The collection of category overrides to search.</param>
        /// <param name="category">The category to find overrides for.</param>
        /// <param name="defaultLevel">The default level to return if no override is found.</param>
        /// <returns>The most restrictive level found, or the default level.</returns>
        public static LogLevel GetMostRestrictiveCategoryLevel(
            System.Collections.Generic.IEnumerable<CategoryLevelOverride> overrides, 
            string category, 
            LogLevel defaultLevel = LogLevel.Debug)
        {
            if (string.IsNullOrWhiteSpace(category))
                return defaultLevel;
                
            LogLevel mostRestrictive = defaultLevel;
            
            if (overrides != null)
            {
                foreach (var categoryOverride in overrides)
                {
                    if (categoryOverride.AppliesTo(category))
                    {
                        if (categoryOverride.Level > mostRestrictive)
                        {
                            mostRestrictive = categoryOverride.Level;
                        }
                    }
                }
            }
            
            return mostRestrictive;
        }
        
        /// <summary>
        /// Validates a collection of tag overrides for duplicates.
        /// </summary>
        /// <param name="overrides">The overrides to validate.</param>
        /// <returns>True if all overrides are unique, false if duplicates exist.</returns>
        public static bool ValidateTagOverrides(System.Collections.Generic.IEnumerable<TagLevelOverride> overrides)
        {
            if (overrides == null)
                return true;
                
            var seenTags = new System.Collections.Generic.HashSet<Tagging.LogTag>();
            
            foreach (var tagOverride in overrides)
            {
                if (!seenTags.Add(tagOverride.Tag))
                {
                    return false; // Duplicate found
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Validates a collection of category overrides for duplicates.
        /// </summary>
        /// <param name="overrides">The overrides to validate.</param>
        /// <returns>True if all overrides are unique, false if duplicates exist.</returns>
        public static bool ValidateCategoryOverrides(System.Collections.Generic.IEnumerable<CategoryLevelOverride> overrides)
        {
            if (overrides == null)
                return true;
                
            var seenCategories = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var categoryOverride in overrides)
            {
                if (categoryOverride.IsValid && !seenCategories.Add(categoryOverride.Category))
                {
                    return false; // Duplicate found
                }
            }
            
            return true;
        }
    }
}