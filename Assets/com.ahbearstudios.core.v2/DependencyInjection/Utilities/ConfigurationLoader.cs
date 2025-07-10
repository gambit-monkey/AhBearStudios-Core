using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using AhBearStudios.Core.DependencyInjection.Configuration;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Utilities
{
    /// <summary>
    /// Implementation of configuration loader with JSON and XML support.
    /// Optimized for minimal allocations and fast loading.
    /// </summary>
    public sealed class ConfigurationLoader : IConfigurationLoader
    {
        private static readonly DataContractJsonSerializer JsonSerializer = 
            new DataContractJsonSerializer(typeof(DependencyInjectionConfigData));
        
        private static readonly System.Runtime.Serialization.DataContractSerializer XmlSerializer = 
            new System.Runtime.Serialization.DataContractSerializer(typeof(DependencyInjectionConfigData));
        
        /// <summary>
        /// Loads configuration from a file path.
        /// Returns default configuration if file doesn't exist or cannot be loaded.
        /// </summary>
        public IDependencyInjectionConfig LoadFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return GetDefault();
            
            try
            {
                if (!File.Exists(filePath))
                    return GetDefault();
                
                var content = File.ReadAllText(filePath, Encoding.UTF8);
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                
                return extension switch
                {
                    ".json" => LoadFromJson(content),
                    ".xml" => LoadFromXml(content),
                    _ => GetDefault()
                };
            }
            catch (Exception)
            {
                // Return default configuration if loading fails
                return GetDefault();
            }
        }
        
        /// <summary>
        /// Loads configuration from JSON string.
        /// </summary>
        public IDependencyInjectionConfig LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return GetDefault();
            
            try
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                var data = (DependencyInjectionConfigData)JsonSerializer.ReadObject(stream);
                return ConvertFromData(data);
            }
            catch (Exception)
            {
                return GetDefault();
            }
        }
        
        /// <summary>
        /// Loads configuration from XML string.
        /// </summary>
        public IDependencyInjectionConfig LoadFromXml(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return GetDefault();
            
            try
            {
                using var reader = XmlReader.Create(new StringReader(xml));
                var data = (DependencyInjectionConfigData)XmlSerializer.ReadObject(reader);
                return ConvertFromData(data);
            }
            catch (Exception)
            {
                return GetDefault();
            }
        }
        
        /// <summary>
        /// Gets the default configuration.
        /// </summary>
        public IDependencyInjectionConfig GetDefault()
        {
            return new DependencyInjectionConfig();
        }
        
        /// <summary>
        /// Converts configuration data to configuration instance.
        /// </summary>
        private IDependencyInjectionConfig ConvertFromData(DependencyInjectionConfigData data)
        {
            if (data == null)
                return GetDefault();
            
            // Parse framework enum safely
            if (!Enum.TryParse<ContainerFramework>(data.PreferredFramework, true, out var framework))
                framework = ContainerFramework.VContainer;
            
            // Build framework-specific options
            var frameworkOptions = new Dictionary<string, object>();
            
            switch (framework)
            {
                case ContainerFramework.VContainer when data.VContainerOptions != null:
                    frameworkOptions["EnableCodeGeneration"] = data.VContainerOptions.EnableCodeGeneration;
                    frameworkOptions["EnableDiagnostics"] = data.VContainerOptions.EnableDiagnostics;
                    frameworkOptions["ValidateDependencies"] = data.VContainerOptions.ValidateDependencies;
                    break;
                    
                case ContainerFramework.Reflex when data.ReflexOptions != null:
                    frameworkOptions["EnableProfiler"] = data.ReflexOptions.EnableProfiler;
                    frameworkOptions["LogRegistrations"] = data.ReflexOptions.LogRegistrations;
                    frameworkOptions["EnableResolverLogging"] = data.ReflexOptions.EnableResolverLogging;
                    break;
            }
            
            return new DependencyInjectionConfig(
                framework,
                data.EnableValidation,
                data.EnableDebugLogging,
                data.EnablePerformanceMetrics,
                data.ThrowOnValidationFailure,
                data.MaxBuildTimeWarningMs,
                frameworkOptions,
                data.EnableScoping,
                data.EnableNamedServices);
        }
    }
}