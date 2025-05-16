using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AhBearStudios.Core.Messaging.Data;
using UnityEngine; // This is the correct namespace for JsonIgnoreAttribute

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Represents a message schema
    /// </summary>
    [Serializable]
    public class MessageSchema
    {
        /// <summary>
        /// Gets or sets the message type
        /// </summary>
        [NonSerialized]
        public Type MessageType;
        
        /// <summary>
        /// Gets or sets the fully qualified name of the message type
        /// </summary>
        public string FullTypeName;
        
        /// <summary>
        /// Gets or sets the schema version
        /// </summary>
        public int Version;
        
        /// <summary>
        /// Gets or sets the schema name
        /// </summary>
        public string Name;
        
        /// <summary>
        /// Gets or sets the schema description
        /// </summary>
        public string Description;
        
        /// <summary>
        /// Gets or sets the category of the message
        /// </summary>
        public string Category;
        
        /// <summary>
        /// Gets or sets a value indicating whether this message is transient
        /// </summary>
        public bool IsTransient;
        
        /// <summary>
        /// Gets or sets the source of this message definition
        /// </summary>
        public string Source;
        
        /// <summary>
        /// Gets or sets the target(s) of this message
        /// </summary>
        public List<string> Targets = new List<string>();
        
        /// <summary>
        /// Gets or sets the creation date of this schema
        /// </summary>
        public DateTime CreationDate;
        
        /// <summary>
        /// Gets or sets the last modified date of this schema
        /// </summary>
        public DateTime LastModifiedDate;
        
        /// <summary>
        /// Gets or sets the expected typical size range of this message in bytes
        /// </summary>
        public string ExpectedSizeRange;
        
        /// <summary>
        /// Gets or sets custom properties for this schema
        /// </summary>
        public List<StringKeyValuePair> CustomProperties = new List<StringKeyValuePair>();
        
        /// <summary>
        /// Gets or sets the properties of the message
        /// </summary>
        public List<PropertySchema> Properties = new List<PropertySchema>();
        
        /// <summary>
        /// Initializes a new instance of the MessageSchema class
        /// </summary>
        public MessageSchema()
        {
            CreationDate = DateTime.UtcNow;
            LastModifiedDate = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Initializes a new instance of the MessageSchema class with a specific message type
        /// </summary>
        /// <param name="messageType">The message type</param>
        public MessageSchema(Type messageType) : this()
        {
            if (messageType == null)
                throw new ArgumentNullException(nameof(messageType));
                
            MessageType = messageType;
            FullTypeName = messageType.AssemblyQualifiedName;
            Name = messageType.Name;
            Category = "General"; // Default category
        }
        
        /// <summary>
        /// Add a custom property to the schema
        /// </summary>
        /// <param name="key">The property key</param>
        /// <param name="value">The property value</param>
        public void AddCustomProperty(string key, string value)
        {
            // Remove any existing property with the same key
            CustomProperties.RemoveAll(p => p.Key == key);
            
            // Add the new property
            CustomProperties.Add(new StringKeyValuePair { Key = key, Value = value });
        }
        
        /// <summary>
        /// Get a custom property value
        /// </summary>
        /// <param name="key">The property key</param>
        /// <returns>The property value, or null if not found</returns>
        public string GetCustomProperty(string key)
        {
            var property = CustomProperties.FirstOrDefault(p => p.Key == key);
            return property != null ? property.Value : null;
        }
        
        /// <summary>
        /// Exports the schema to JSON
        /// </summary>
        /// <returns>The JSON representation of the schema</returns>
        public string ToJson()
        {
            // Update last modified date
            LastModifiedDate = DateTime.UtcNow;
            
            return JsonUtility.ToJson(this, true);
        }
        
        /// <summary>
        /// Imports a schema from JSON
        /// </summary>
        /// <param name="json">The JSON representation of the schema</param>
        /// <returns>The imported schema</returns>
        public static MessageSchema FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));
                
            var schema = JsonUtility.FromJson<MessageSchema>(json);
            
            // Try to resolve the type from the full type name
            if (!string.IsNullOrEmpty(schema.FullTypeName))
            {
                try
                {
                    schema.MessageType = Type.GetType(schema.FullTypeName);
                }
                catch (Exception)
                {
                    // Type could not be resolved, but that's okay
                    // It might be from a different assembly or a type that doesn't exist anymore
                }
            }
            
            return schema;
        }
        
        /// <summary>
        /// Validates that a schema is properly formed
        /// </summary>
        /// <returns>True if the schema is valid; otherwise, false</returns>
        public bool Validate()
        {
            // A valid schema must have a name and at least one property
            return !string.IsNullOrEmpty(Name) && Properties.Count > 0;
        }
        
        /// <summary>
        /// Gets a property schema by name
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <returns>The property schema, or null if not found</returns>
        public PropertySchema GetProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));
                
            return Properties.Find(p => p.Name == propertyName);
        }
        
        /// <summary>
        /// Creates a deep copy of this schema
        /// </summary>
        /// <returns>A deep copy of this schema</returns>
        public MessageSchema Clone()
        {
            // Serialize to JSON and back for a deep copy
            return FromJson(ToJson());
        }
    }
}