using System;
using AhBearStudios.Core.Logging.Data;

namespace AhBearStudios.Core.Logging.Editor
{
    /// <summary>
    /// Interface for managing log visualization profiles.
    /// </summary>
    public interface ILogVisualizationProfileManager : IDisposable
    {
        /// <summary>
        /// Gets a profile by name, or creates it if it doesn't exist.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <returns>The profile with the specified name.</returns>
        LogVisualizationProfile GetOrCreateProfile(string name);
        
        /// <summary>
        /// Creates a new profile with the specified name.
        /// </summary>
        /// <param name="name">The name for the new profile.</param>
        /// <returns>The newly created profile.</returns>
        LogVisualizationProfile CreateProfile(string name);
        
        /// <summary>
        /// Checks if a profile with the specified name exists.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>True if a profile with the name exists, false otherwise.</returns>
        bool ProfileExists(string name);
        
        /// <summary>
        /// Gets the names of all available profiles.
        /// </summary>
        /// <returns>An array of profile names.</returns>
        string[] GetProfileNames();
    }
}