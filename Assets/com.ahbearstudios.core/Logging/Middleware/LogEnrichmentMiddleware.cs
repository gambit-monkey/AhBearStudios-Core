using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Messages;
using Unity.Collections;

namespace AhBearStudios.Core.Logging.Middleware
{
    /// <summary>
    /// Middleware that enriches log messages with additional properties.
    /// </summary>
    public class LogEnrichmentMiddleware : ILogMiddleware
    {
        private readonly Dictionary<FixedString32Bytes, FixedString64Bytes> _globalProperties = 
            new Dictionary<FixedString32Bytes, FixedString64Bytes>();
        private Func<LogMessage, LogProperties> _dynamicEnricher;
        
        /// <summary>
        /// Gets or sets the next middleware in the chain.
        /// </summary>
        public ILogMiddleware Next { get; set; }
        
        /// <summary>
        /// Adds a global property that will be added to all log messages.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        public void AddGlobalProperty(FixedString32Bytes key, FixedString64Bytes value)
        {
            _globalProperties[key] = value;
        }
        
        /// <summary>
        /// Removes a global property.
        /// </summary>
        /// <param name="key">The property key to remove.</param>
        /// <returns>True if the property was found and removed, false otherwise.</returns>
        public bool RemoveGlobalProperty(FixedString32Bytes key)
        {
            return _globalProperties.Remove(key);
        }
        
        /// <summary>
        /// Sets a dynamic enricher function that can add contextual properties to log messages.
        /// </summary>
        /// <param name="enricher">A function that takes a log message and returns properties to add.</param>
        public void SetDynamicEnricher(Func<LogMessage, LogProperties> enricher)
        {
            _dynamicEnricher = enricher;
        }
        
        /// <summary>
        /// Process a log message by enriching it with additional properties.
        /// </summary>
        /// <param name="message">The log message to enrich.</param>
        /// <returns>True to continue processing, false to stop.</returns>
        public bool Process(ref LogMessage message)
        {
            // Skip if no properties to add
            if (_globalProperties.Count == 0 && _dynamicEnricher == null)
            {
                return Next?.Process(ref message) ?? true;
            }
            
            // Create or use existing properties
            LogProperties properties;
            bool createdNewProperties = false;
            
            if (!message.Properties.IsCreated)
            {
                properties = new LogProperties(16);
                createdNewProperties = true;
            }
            else
            {
                properties = message.Properties;
            }
            
            // Add global properties
            foreach (var property in _globalProperties)
            {
                properties.Add(property.Key, property.Value);
            }
            
            // Add dynamic properties if an enricher is set
            if (_dynamicEnricher != null)
            {
                var dynamicProps = _dynamicEnricher(message);
                if (dynamicProps.IsCreated)
                {
                    foreach (var property in dynamicProps)
                    {
                        properties.Add(property.Key, property.Value);
                    }
                    
                    // Dispose dynamic properties after copying them
                    if (!dynamicProps.Equals(properties)) // Avoid disposing the same object
                    {
                        dynamicProps.Dispose();
                    }
                }
            }
            
            // Update the message with the enriched properties
            message.Properties = properties;
            
            // Continue processing
            return Next?.Process(ref message) ?? true;
        }
    }
}