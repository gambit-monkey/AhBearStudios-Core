using System;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Tags;
using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace AhBearStudios.Core.Logging.Formatters
{
    /// <summary>
    /// A formatter that adds colorization to console output using Unity's rich text tag.
    /// This formatter is specifically designed for the Unity Editor Console and provides
    /// customizable color coding for different log levels and tag.
    /// </summary>
    [CreateAssetMenu(fileName = "ColorizedConsoleFormatter", menuName = "AhBearStudios/Logging/Formatters/Colorized Console Formatter")]
    public class ColorizedConsoleFormatter : ScriptableObject, ILogFormatter
    {
        // Serialized color data (stored as JSON strings to avoid serialization issues)
        [SerializeField, HideInInspector]
        private string _levelColorsJson = string.Empty;
        
        [SerializeField, HideInInspector]
        private string _tagColorsJson = string.Empty;
        
        // Color dictionaries used at runtime (non-serialized)
        private Dictionary<byte, string> _levelColors = new Dictionary<byte, string>();
        private Dictionary<int, string> _tagColors = new Dictionary<int, string>();
        
        // Format template options
        [SerializeField, Tooltip("Show timestamp in formatted log messages")]
        private bool _showTimestamp = true;
        
        [SerializeField, Tooltip("Show log level in formatted log messages")]
        private bool _showLogLevel = true;
        
        [SerializeField, Tooltip("Show tag in formatted log messages")]
        private bool _showTag = true;
        
        [SerializeField, Tooltip("Format for timestamps (e.g., yyyy-MM-ddTHH:mm:ssZ)")]
        private string _timestampFormat = "yyyy-MM-ddTHH:mm:ssZ";
        
        /// <summary>
        /// Initialize default colors when the ScriptableObject is created or loaded
        /// </summary>
        private void OnEnable()
        {
            InitializeIfNeeded();
        }
        
        /// <summary>
        /// Initialize color dictionaries if they haven't been initialized yet
        /// </summary>
        private void InitializeIfNeeded()
        {
            if (_levelColors.Count == 0 || _tagColors.Count == 0)
            {
                LoadColorsFromJson();
                
                // If still empty, reset to defaults
                if (_levelColors.Count == 0 || _tagColors.Count == 0)
                {
                    ResetColorsToDefaults();
                }
            }
        }
        
        /// <summary>
        /// Loads color dictionaries from serialized JSON strings
        /// </summary>
        private void LoadColorsFromJson()
        {
            // Don't deserialize if the JSON strings are empty
            if (string.IsNullOrEmpty(_levelColorsJson) || string.IsNullOrEmpty(_tagColorsJson))
                return;
                
            try
            {
                // Deserialize level colors
                Dictionary<string, string> levelColorDict = 
                    JsonUtility.FromJson<SerializableDictionary>(_levelColorsJson).ToDictionary();
                    
                _levelColors.Clear();
                foreach (var pair in levelColorDict)
                {
                    if (byte.TryParse(pair.Key, out byte level))
                    {
                        _levelColors[level] = pair.Value;
                    }
                }
                
                // Deserialize tag colors
                Dictionary<string, string> tagColorDict = 
                    JsonUtility.FromJson<SerializableDictionary>(_tagColorsJson).ToDictionary();
                    
                _tagColors.Clear();
                foreach (var pair in tagColorDict)
                {
                    if (int.TryParse(pair.Key, out int tag))
                    {
                        _tagColors[tag] = pair.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error deserializing colors for ColorizedConsoleFormatter: {ex.Message}");
                // If deserialization fails, clear dictionaries to trigger default initialization
                _levelColors.Clear();
                _tagColors.Clear();
            }
        }
        
        /// <summary>
        /// Saves color dictionaries to serialized JSON strings
        /// </summary>
        private void SaveColorsToJson()
        {
            try
            {
                // Convert level colors to string dictionary for serialization
                Dictionary<string, string> levelColorDict = new Dictionary<string, string>();
                foreach (var pair in _levelColors)
                {
                    levelColorDict[pair.Key.ToString()] = pair.Value;
                }
                
                // Convert tag colors to string dictionary for serialization
                Dictionary<string, string> tagColorDict = new Dictionary<string, string>();
                foreach (var pair in _tagColors)
                {
                    tagColorDict[pair.Key.ToString()] = pair.Value;
                }
                
                // Serialize to JSON
                _levelColorsJson = JsonUtility.ToJson(new SerializableDictionary(levelColorDict));
                _tagColorsJson = JsonUtility.ToJson(new SerializableDictionary(tagColorDict));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error serializing colors for ColorizedConsoleFormatter: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Reset all colors to their default values
        /// </summary>
        public void ResetColorsToDefaults()
        {
            // Initialize level colors
            _levelColors.Clear();
            _levelColors[LogLevel.Debug] = "#888888";    // Gray
            _levelColors[LogLevel.Info] = "#00AAFF";     // Light Blue
            _levelColors[LogLevel.Warning] = "#FFFF00";  // Yellow
            _levelColors[LogLevel.Error] = "#FF3300";    // Orange/Red
            _levelColors[LogLevel.Critical] = "#FF0000"; // Bright Red
            
            // Initialize tag colors
            _tagColors.Clear();
            
            // System categories with blue hues
            _tagColors[(int)Tagging.LogTag.System] = "#4169E1";     // Royal Blue
            _tagColors[(int)Tagging.LogTag.Network] = "#1E90FF";    // Dodger Blue
            _tagColors[(int)Tagging.LogTag.Physics] = "#00BFFF";    // Deep Sky Blue
            _tagColors[(int)Tagging.LogTag.Audio] = "#87CEEB";      // Sky Blue
            _tagColors[(int)Tagging.LogTag.Input] = "#B0E0E6";      // Powder Blue
            _tagColors[(int)Tagging.LogTag.Database] = "#B0C4DE";   // Light Steel Blue
            _tagColors[(int)Tagging.LogTag.IO] = "#4682B4";         // Steel Blue
            _tagColors[(int)Tagging.LogTag.Memory] = "#5F9EA0";     // Cadet Blue
            _tagColors[(int)Tagging.LogTag.Job] = "#6495ED";        // Cornflower Blue
            _tagColors[(int)Tagging.LogTag.Unity] = "#7B68EE";      // Medium Slate Blue
            
            // Application layers with green hues
            _tagColors[(int)Tagging.LogTag.UI] = "#32CD32";         // Lime Green
            _tagColors[(int)Tagging.LogTag.Gameplay] = "#3CB371";   // Medium Sea Green
            _tagColors[(int)Tagging.LogTag.AI] = "#2E8B57";         // Sea Green
            _tagColors[(int)Tagging.LogTag.Animation] = "#00FF7F";  // Spring Green
            _tagColors[(int)Tagging.LogTag.Rendering] = "#66CDAA";  // Medium Aquamarine
            _tagColors[(int)Tagging.LogTag.Particles] = "#20B2AA";  // Light Sea Green
            
            // Cross-cutting concerns with purplish hues
            _tagColors[(int)Tagging.LogTag.Loading] = "#DA70D6";    // Orchid
            _tagColors[(int)Tagging.LogTag.Performance] = "#BA55D3"; // Medium Orchid
            _tagColors[(int)Tagging.LogTag.Analytics] = "#9370DB";  // Medium Purple
            
            // Severity tag with warm colors
            _tagColors[(int)Tagging.LogTag.Debug] = "#888888";      // Gray
            _tagColors[(int)Tagging.LogTag.Info] = "#00AAFF";       // Light Blue
            _tagColors[(int)Tagging.LogTag.Warning] = "#FFFF00";    // Yellow
            _tagColors[(int)Tagging.LogTag.Error] = "#FF3300";      // Orange/Red
            _tagColors[(int)Tagging.LogTag.Critical] = "#FF0000";   // Bright Red
            _tagColors[(int)Tagging.LogTag.Exception] = "#FF1493";  // Deep Pink
            _tagColors[(int)Tagging.LogTag.Assert] = "#FF00FF";     // Magenta
            
            // Special tag
            _tagColors[(int)Tagging.LogTag.Custom] = "#FFA500";     // Orange
            _tagColors[(int)Tagging.LogTag.Default] = "#FFFFFF";    // White
            _tagColors[(int)Tagging.LogTag.Undefined] = "#FFFFFF";  // White
            _tagColors[(int)Tagging.LogTag.None] = "#FFFFFF";       // White
            
            // Save to JSON for serialization
            SaveColorsToJson();
        }
        
        /// <summary>
        /// Formats a log message with colorization based on log level and tag.
        /// This is the main method implementing the ILogFormatter interface.
        /// </summary>
        /// <param name="message">The log message to format.</param>
        /// <returns>A formatted log message string with color tag.</returns>
        public FixedString512Bytes Format(LogMessage message)
        {
            InitializeIfNeeded();
            
            // Build the message
            FixedString512Bytes formatted = new FixedString512Bytes();
            
            // Add timestamp if enabled
            if (_showTimestamp)
            {
                DateTime dt = new DateTime(message.TimestampTicks);
                string timestamp = dt.ToString(_timestampFormat);
                formatted.Append("[");
                formatted.Append(timestamp);
                formatted.Append("] ");
            }
            
            // Add log level if enabled
            if (_showLogLevel)
            {
                string levelColor = GetColorForLevel(message.Level);
                string levelName = GetLevelName(message.Level);
                
                formatted.Append("[<color=");
                formatted.Append(levelColor);
                formatted.Append(">");
                formatted.Append(levelName);
                formatted.Append("</color>] ");
            }
            
            // Add tag if enabled
            if (_showTag)
            {
                string tagColor = GetColorForTag(message.Tag);
                string tagName = message.GetTagString().ToString();
                
                formatted.Append("[<color=");
                formatted.Append(tagColor);
                formatted.Append("><b>");
                formatted.Append(tagName);
                formatted.Append("</b></color>] ");
            }
            
            // Add the message content
            formatted.Append(message.Message);
            
            return formatted;
        }

        public bool SupportsStructuredLogging { get; }

        /// <summary>
        /// Gets a color code based on the log level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>A hexadecimal color code.</returns>
        public string GetColorForLevel(byte level)
        {
            // Check if we have a specific color for this level
            if (_levelColors.TryGetValue(level, out string color))
            {
                return color;
            }
            
            // Fallback to default colors based on level
            if (level == LogLevel.Debug)
                return "#888888";  // Gray
            else if (level == LogLevel.Info)
                return "#00AAFF";  // Light Blue
            else if (level == LogLevel.Warning)
                return "#FFFF00";  // Yellow
            else if (level == LogLevel.Error)
                return "#FF3300";  // Orange/Red
            else if (level == LogLevel.Critical)
                return "#FF0000";  // Bright Red
            else
                return "#FFFFFF";  // White fallback
        }
        
        /// <summary>
        /// Gets a color code based on the tag.
        /// </summary>
        /// <param name="tag">The log tag.</param>
        /// <returns>A hexadecimal color code.</returns>
        public string GetColorForTag(Tagging.LogTag tag)
        {
            // Check if we have a specific color for this tag
            if (_tagColors.TryGetValue((int)tag, out string color))
            {
                return color;
            }
            
            // Check tag categories if specific tag doesn't have a direct color
            Tagging.TagCategory category = Tagging.GetTagCategory(tag);
            return GetColorForTagCategory(category);
        }
        
        /// <summary>
        /// Sets a custom color for a log level
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="colorHex">Hexadecimal color code (e.g., "#FF0000")</param>
        public void SetLevelColor(byte level, string colorHex)
        {
            InitializeIfNeeded();
            _levelColors[level] = colorHex;
            SaveColorsToJson();
        }
        
        /// <summary>
        /// Sets a custom color for a log tag
        /// </summary>
        /// <param name="tag">The log tag</param>
        /// <param name="colorHex">Hexadecimal color code (e.g., "#FF0000")</param>
        public void SetTagColor(Tagging.LogTag tag, string colorHex)
        {
            InitializeIfNeeded();
            _tagColors[(int)tag] = colorHex;
            SaveColorsToJson();
        }
        
        /// <summary>
        /// Gets a color code based on the tag category.
        /// </summary>
        /// <param name="category">The tag category.</param>
        /// <returns>A hexadecimal color code.</returns>
        private string GetColorForTagCategory(Tagging.TagCategory category)
        {
            switch (category)
            {
                case Tagging.TagCategory.System:
                    return "#4682B4";  // Steel Blue
                case Tagging.TagCategory.UI:
                    return "#32CD32";  // Lime Green
                case Tagging.TagCategory.Gameplay:
                    return "#3CB371";  // Medium Sea Green
                case Tagging.TagCategory.Debug:
                    return "#888888";  // Gray
                case Tagging.TagCategory.Error:
                    return "#FF3300";  // Orange/Red
                case Tagging.TagCategory.Custom:
                    return "#FFA500";  // Orange
                case Tagging.TagCategory.All:
                case Tagging.TagCategory.None:
                default:
                    return "#FFFFFF";  // White
            }
        }
        
        /// <summary>
        /// Gets a human-readable name for a log level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>A string representation of the log level.</returns>
        private string GetLevelName(byte level)
        {
            if (level == LogLevel.Debug)
                return "DEBUG";
            else if (level == LogLevel.Info)
                return "INFO";
            else if (level == LogLevel.Warning)
                return "WARNING";
            else if (level == LogLevel.Error)
                return "ERROR";
            else if (level == LogLevel.Critical)
                return "CRITICAL";
            else
                return $"LEVEL({level})";
        }
    }

    /// <summary>
    /// Helper class for serializing dictionaries with Unity's JsonUtility
    /// </summary>
    [Serializable]
    public class SerializableDictionary
    {
        [Serializable]
        public class KeyValuePair
        {
            public string Key;
            public string Value;
            
            public KeyValuePair(string key, string value)
            {
                Key = key;
                Value = value;
            }
        }
        
        public List<KeyValuePair> Items = new List<KeyValuePair>();
        
        public SerializableDictionary() { }
        
        public SerializableDictionary(Dictionary<string, string> dictionary)
        {
            foreach (var pair in dictionary)
            {
                Items.Add(new KeyValuePair(pair.Key, pair.Value));
            }
        }
        
        public Dictionary<string, string> ToDictionary()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (var item in Items)
            {
                result[item.Key] = item.Value;
            }
            return result;
        }
    }
}