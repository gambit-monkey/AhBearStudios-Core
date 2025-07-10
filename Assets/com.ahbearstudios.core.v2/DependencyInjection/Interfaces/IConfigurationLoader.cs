using System;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Interface for loading configuration from different sources.
    /// </summary>
    public interface IConfigurationLoader
    {
        /// <summary>
        /// Loads configuration from a file path.
        /// </summary>
        /// <param name="filePath">Path to the configuration file.</param>
        /// <returns>The loaded configuration or default if file doesn't exist.</returns>
        IDependencyInjectionConfig LoadFromFile(string filePath);
        
        /// <summary>
        /// Loads configuration from JSON string.
        /// </summary>
        /// <param name="json">JSON configuration string.</param>
        /// <returns>The loaded configuration.</returns>
        IDependencyInjectionConfig LoadFromJson(string json);
        
        /// <summary>
        /// Loads configuration from XML string.
        /// </summary>
        /// <param name="xml">XML configuration string.</param>
        /// <returns>The loaded configuration.</returns>
        IDependencyInjectionConfig LoadFromXml(string xml);
        
        /// <summary>
        /// Gets the default configuration.
        /// </summary>
        /// <returns>Default configuration instance.</returns>
        IDependencyInjectionConfig GetDefault();
    }
}