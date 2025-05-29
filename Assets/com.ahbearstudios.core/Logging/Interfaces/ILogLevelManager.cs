using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Interfaces
{
    /// <summary>
    /// Interface for managing dynamic log level configuration at runtime.
    /// Supports both Unity ScriptableObject profiles and runtime profiles.
    /// </summary>
    public interface ILogLevelManager
    {
        /// <summary>
        /// Gets or sets the global minimum log level
        /// </summary>
        LogLevel GlobalMinimumLevel { get; set; }
    
        /// <summary>
        /// Sets a log level override for a specific tag
        /// </summary>
        void SetTagLevelOverride(Tagging.LogTag tag, LogLevel level);
    
        /// <summary>
        /// Removes a tag-specific log level override
        /// </summary>
        bool RemoveTagLevelOverride(Tagging.LogTag tag);
    
        /// <summary>
        /// Sets a log level override for a category
        /// </summary>
        void SetCategoryLevelOverride(string category, LogLevel level);
    
        /// <summary>
        /// Removes a category-specific log level override
        /// </summary>
        bool RemoveCategoryLevelOverride(string category);
    
        /// <summary>
        /// Applies a Unity ScriptableObject log level profile
        /// </summary>
        void ApplyProfile(LogLevelProfile profile);
        
        /// <summary>
        /// Applies a runtime log level profile
        /// </summary>
        void ApplyProfile(RuntimeLogLevelProfile profile);
    
        /// <summary>
        /// Resets all overrides to defaults
        /// </summary>
        void ResetToDefaults();
        
        /// <summary>
        /// Checks if a message should be logged based on current level configuration
        /// </summary>
        /// <param name="level">The log level of the message</param>
        /// <param name="tag">The tag of the message</param>
        /// <param name="category">The category of the message (optional)</param>
        /// <returns>True if the message should be logged, false otherwise</returns>
        bool ShouldLog(LogLevel level, Tagging.LogTag tag, string category = null);
        
        /// <summary>
        /// Gets the effective minimum level for a specific tag and category combination
        /// </summary>
        /// <param name="tag">The tag to check</param>
        /// <param name="category">The category to check (optional)</param>
        /// <returns>The effective minimum log level</returns>
        LogLevel GetEffectiveLevel(Tagging.LogTag tag, string category = null);
    }
}