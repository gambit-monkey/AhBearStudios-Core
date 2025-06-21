using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Tags;
using UnityEngine;

namespace AhBearStudios.Core.Logging.Editor
{
        /// <summary>
        /// Implementation of IColorManager that interacts with the ColorizedConsoleFormatter.
        /// </summary>
        public class ColorManager : IColorManager
        {
            private readonly ColorizedConsoleFormatter _formatter;
            private readonly Dictionary<LogLevel, Color> _levelColors = new Dictionary<LogLevel, Color>();
            private readonly Dictionary<Tagging.LogTag, Color> _tagColors = new Dictionary<Tagging.LogTag, Color>();
            
            /// <summary>
            /// Initializes a new instance of the ColorManager class.
            /// </summary>
            /// <param name="formatter">The formatter to manage colors for.</param>
            public ColorManager(ColorizedConsoleFormatter formatter)
            {
                _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            }
            
            /// <summary>
            /// Initialize colors from the formatter.
            /// </summary>
            public void InitializeColors()
            {
                // Initialize level colors
                _levelColors.Clear();
                foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
                {
                    // Skip None as it's typically not used for coloring
                    if (level == LogLevel.None)
                        continue;
                        
                    string colorHex = _formatter.GetColorForLevel(level);
                    _levelColors[level] = HexToColor(colorHex);
                }
                
                // Initialize tag colors
                _tagColors.Clear();
                foreach (Tagging.LogTag tag in Enum.GetValues(typeof(Tagging.LogTag)))
                {
                    string colorHex = _formatter.GetColorForTag(tag);
                    _tagColors[tag] = HexToColor(colorHex);
                }
            }
            
            /// <summary>
            /// Get color for a log level.
            /// </summary>
            /// <param name="level">The log level.</param>
            /// <returns>The color associated with the log level.</returns>
            public Color GetLevelColor(LogLevel level)
            {
                if (_levelColors.TryGetValue(level, out Color color))
                    return color;
                    
                // If not found, initialize it
                string colorHex = _formatter.GetColorForLevel(level);
                Color newColor = HexToColor(colorHex);
                _levelColors[level] = newColor;
                return newColor;
            }
            
            /// <summary>
            /// Set color for a log level.
            /// </summary>
            /// <param name="level">The log level.</param>
            /// <param name="color">The new color.</param>
            public void SetLevelColor(LogLevel level, Color color)
            {
                _levelColors[level] = color;
                _formatter.SetLevelColor(level, "#" + ColorUtility.ToHtmlStringRGB(color));
            }
            
            /// <summary>
            /// Get color for a log tag.
            /// </summary>
            /// <param name="tag">The log tag.</param>
            /// <returns>The color associated with the tag.</returns>
            public Color GetTagColor(Tagging.LogTag tag)
            {
                if (_tagColors.TryGetValue(tag, out Color color))
                    return color;
                    
                // If not found, initialize it
                string colorHex = _formatter.GetColorForTag(tag);
                Color newColor = HexToColor(colorHex);
                _tagColors[tag] = newColor;
                return newColor;
            }
            
            /// <summary>
            /// Set color for a log tag.
            /// </summary>
            /// <param name="tag">The log tag.</param>
            /// <param name="color">The new color.</param>
            public void SetTagColor(Tagging.LogTag tag, Color color)
            {
                _tagColors[tag] = color;
                _formatter.SetTagColor(tag, "#" + ColorUtility.ToHtmlStringRGB(color));
            }
            
            /// <summary>
            /// Get all tags in a category.
            /// </summary>
            /// <param name="category">The tag category.</param>
            /// <returns>An enumerable of tags in the category.</returns>
            public IEnumerable<Tagging.LogTag> GetTagsInCategory(Tagging.TagCategory category)
            {
                return Enum.GetValues(typeof(Tagging.LogTag))
                    .Cast<Tagging.LogTag>()
                    .Where(tag => Tagging.GetTagCategory(tag) == category);
            }
            
            /// <summary>
            /// Convert hex color string to Color.
            /// </summary>
            /// <param name="hex">Hex color string.</param>
            /// <returns>Color object.</returns>
            private static Color HexToColor(string hex)
            {
                // Remove # if present
                if (hex.StartsWith("#"))
                    hex = hex.Substring(1);
                    
                // Parse the hex string
                if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
                    return color;
                    
                return Color.white;
            }
        }
}