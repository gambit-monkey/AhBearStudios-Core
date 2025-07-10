using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Logging.Data
{
    /// <summary>
    /// Profile for log visualization settings, including which levels are visible.
    /// </summary>
    public class LogVisualizationProfile : IDisposable
    {
        private readonly string _name;
        private readonly Dictionary<LogLevel, bool> _levelVisibility = new Dictionary<LogLevel, bool>();
        
        /// <summary>
        /// Gets the name of this profile.
        /// </summary>
        public string Name => _name;
        
        /// <summary>
        /// Creates a new log visualization profile with the given name.
        /// </summary>
        /// <param name="name">The name for this profile.</param>
        public LogVisualizationProfile(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            
            // By default, all levels are visible
            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
            {
                if (level != LogLevel.None)
                    _levelVisibility[level] = true;
            }
        }
        
        /// <summary>
        /// Checks if a log level is visible in this profile.
        /// </summary>
        /// <param name="level">The log level to check.</param>
        /// <returns>True if the level is visible, false otherwise.</returns>
        public bool IsLevelVisible(LogLevel level)
        {
            return _levelVisibility.TryGetValue(level, out bool isVisible) && isVisible;
        }
        
        /// <summary>
        /// Sets the visibility of a log level in this profile.
        /// </summary>
        /// <param name="level">The log level to update.</param>
        /// <param name="isVisible">Whether the level should be visible.</param>
        public void SetLevelVisibility(LogLevel level, bool isVisible)
        {
            _levelVisibility[level] = isVisible;
        }
        
        /// <summary>
        /// Creates a copy of this profile with a new name.
        /// </summary>
        /// <param name="newName">The name for the copied profile.</param>
        /// <returns>A new profile with the same settings as this one.</returns>
        public LogVisualizationProfile Clone(string newName)
        {
            var clone = new LogVisualizationProfile(newName);
            
            foreach (var kvp in _levelVisibility)
            {
                clone.SetLevelVisibility(kvp.Key, kvp.Value);
            }
            
            return clone;
        }
        
        /// <summary>
        /// Disposes of any resources used by the profile.
        /// </summary>
        public void Dispose()
        {
            _levelVisibility.Clear();
        }
    }
}