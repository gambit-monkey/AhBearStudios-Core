using System.Collections.Generic;
using AhBearStudios.Core.Logging.Tags;
using UnityEngine;

namespace AhBearStudios.Core.Logging.Interfaces
{
    /// <summary>
    /// Interface for managing colors in the editor.
    /// </summary>
    public interface IColorManager
    {
        /// <summary>
        /// Initialize colors from the formatter.
        /// </summary>
        void InitializeColors();
            
        /// <summary>
        /// Get color for a log level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>The color associated with the log level.</returns>
        Color GetLevelColor(LogLevel level);
            
        /// <summary>
        /// Set color for a log level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="color">The new color.</param>
        void SetLevelColor(LogLevel level, Color color);
            
        /// <summary>
        /// Get color for a log tag.
        /// </summary>
        /// <param name="tag">The log tag.</param>
        /// <returns>The color associated with the tag.</returns>
        Color GetTagColor(Tagging.LogTag tag);
            
        /// <summary>
        /// Set color for a log tag.
        /// </summary>
        /// <param name="tag">The log tag.</param>
        /// <param name="color">The new color.</param>
        void SetTagColor(Tagging.LogTag tag, Color color);
            
        /// <summary>
        /// Get all tags in a category.
        /// </summary>
        /// <param name="category">The tag category.</param>
        /// <returns>An enumerable of tags in the category.</returns>
        IEnumerable<Tagging.LogTag> GetTagsInCategory(Tagging.TagCategory category);
    }
}