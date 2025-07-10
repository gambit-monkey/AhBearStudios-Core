// Assets/com.ahbearstudios.core/Logging/Scripts/Middleware/PropertyEnricherInterceptor.cs
using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Messages;
using Unity.Collections;

namespace AhBearStudios.Core.Logging.Middleware
{
    /// <summary>
    /// Interceptor that enriches log messages with additional properties.
    /// </summary>
    public class PropertyEnricherInterceptor : ILogInterceptor
    {
        private readonly Dictionary<FixedString32Bytes, Func<FixedString64Bytes>> _propertyProviders = 
            new Dictionary<FixedString32Bytes, Func<FixedString64Bytes>>();
        private readonly int _order;
        private bool _isEnabled = true;
        
        /// <summary>
        /// Creates a new property enricher with the specified execution order.
        /// </summary>
        /// <param name="order">Execution order of this interceptor (lower values run earlier).</param>
        public PropertyEnricherInterceptor(int order = 100)
        {
            _order = order;
        }
        
        /// <summary>
        /// Adds a property provider that will be called for each log message.
        /// </summary>
        /// <param name="propertyKey">The key for the property.</param>
        /// <param name="valueProvider">Function that provides the property value.</param>
        /// <returns>This interceptor (for fluent chaining).</returns>
        public PropertyEnricherInterceptor AddProperty(FixedString32Bytes propertyKey, Func<FixedString64Bytes> valueProvider)
        {
            _propertyProviders[propertyKey] = valueProvider;
            return this;
        }
        
        /// <summary>
        /// Adds a fixed property that will be added to each log message.
        /// </summary>
        /// <param name="propertyKey">The key for the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <returns>This interceptor (for fluent chaining).</returns>
        public PropertyEnricherInterceptor AddFixedProperty(FixedString32Bytes propertyKey, FixedString64Bytes value)
        {
            _propertyProviders[propertyKey] = () => value;
            return this;
        }
        
        /// <summary>
        /// Process the log message by adding the configured properties.
        /// </summary>
        /// <param name="message">The log message to enrich.</param>
        /// <returns>Always returns true to continue processing.</returns>
        public bool Process(ref LogMessage message)
        {
            if (!_isEnabled)
                return true;
                
            // Create properties collection if it doesn't exist
            if (!message.Properties.IsCreated)
            {
                message.InitializeProperties(_propertyProviders.Count);
            }
            
            // Add all property values from providers
            foreach (var provider in _propertyProviders)
            {
                try
                {
                    var value = provider.Value();
                    message.Properties.Add(provider.Key, value);
                }
                catch
                {
                    // Silently ignore errors in property providers
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Gets the execution order for this interceptor.
        /// </summary>
        public int Order => _order;
        
        /// <summary>
        /// Gets or sets whether this interceptor is enabled.
        /// </summary>
        public bool IsEnabled 
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }
    }
}