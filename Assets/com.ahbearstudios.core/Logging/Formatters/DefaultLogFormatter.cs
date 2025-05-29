using System;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Messages;
using Unity.Collections;

namespace AhBearStudios.Core.Logging.Formatters
{
    public class DefaultLogFormatter : ILogFormatter
    {
        public bool SupportsStructuredLogging => true;
        
        public FixedString512Bytes Format(LogMessage message)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var baseMessage = new FixedString512Bytes($"[{timestamp}] [{message.Level}] [{message.GetTagString()}] {message.Message}");
            
            // Add structured properties if available
            if (message.Properties.IsCreated)
            {
                // Create a buffer for properties
                var propertyBuffer = new FixedString512Bytes(" {");
                bool first = true;
                
                // Add each property
                foreach (var property in message.Properties)
                {
                    // Skip if we would exceed the buffer (conservative check)
                    if (propertyBuffer.Length > 450) 
                    {
                        propertyBuffer.Append("...");
                        break;
                    }
                    
                    if (!first)
                        propertyBuffer.Append(", ");
                    
                    propertyBuffer.Append(property.Key);
                    propertyBuffer.Append("=");
                    propertyBuffer.Append(property.Value);
                    first = false;
                }
                
                propertyBuffer.Append("}");
                
                // Ensure we don't exceed maximum length
                if (baseMessage.Length + propertyBuffer.Length <= 512)
                {
                    baseMessage.Append(propertyBuffer);
                }
                else
                {
                    baseMessage.Append(" {...}");
                }
            }
            
            return baseMessage;
        }
    }
}