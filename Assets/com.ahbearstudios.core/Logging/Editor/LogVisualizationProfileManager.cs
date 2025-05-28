using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging.Data;

namespace AhBearStudios.Core.Logging.Editor
{
    /// <summary>
    /// Manages log visualization profiles.
    /// </summary>
    public class LogVisualizationProfileManager : ILogVisualizationProfileManager
    {
        private readonly Dictionary<string, LogVisualizationProfile> _profiles = new Dictionary<string, LogVisualizationProfile>();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the LogVisualizationProfileManager class.
        /// </summary>
        public LogVisualizationProfileManager()
        {
            // Create default profile
            CreateProfile("Default");
        }

        /// <summary>
        /// Gets a profile by name, or creates it if it doesn't exist.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <returns>The profile with the specified name.</returns>
        public LogVisualizationProfile GetOrCreateProfile(string name)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LogVisualizationProfileManager));

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Profile name cannot be null or empty", nameof(name));

            if (_profiles.TryGetValue(name, out var profile))
                return profile;

            return CreateProfile(name);
        }

        /// <summary>
        /// Creates a new profile with the specified name.
        /// </summary>
        /// <param name="name">The name for the new profile.</param>
        /// <returns>The newly created profile.</returns>
        public LogVisualizationProfile CreateProfile(string name)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LogVisualizationProfileManager));

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Profile name cannot be null or empty", nameof(name));

            var profile = new LogVisualizationProfile(name);
            _profiles[name] = profile;
            return profile;
        }

        /// <summary>
        /// Checks if a profile with the specified name exists.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>True if a profile with the name exists, false otherwise.</returns>
        public bool ProfileExists(string name)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LogVisualizationProfileManager));

            return !string.IsNullOrEmpty(name) && _profiles.ContainsKey(name);
        }

        /// <summary>
        /// Gets the names of all available profiles.
        /// </summary>
        /// <returns>An array of profile names.</returns>
        public string[] GetProfileNames()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LogVisualizationProfileManager));

            return _profiles.Keys.ToArray();
        }

        /// <summary>
        /// Disposes of resources used by the profile manager.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (var profile in _profiles.Values)
            {
                profile.Dispose();
            }

            _profiles.Clear();
            _disposed = true;
        }
    }
}