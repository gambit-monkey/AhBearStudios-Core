using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Interfaces
{
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
        /// Applies a log level profile
        /// </summary>
        void ApplyProfile(LogLevelProfile profile);
    
        /// <summary>
        /// Resets all overrides to defaults
        /// </summary>
        void ResetToDefaults();
    }
}