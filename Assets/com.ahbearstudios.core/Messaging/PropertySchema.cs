using System;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Represents a property in a message schema
    /// </summary>
    public class PropertySchema
    {
        /// <summary>
        /// Gets the name of the property
        /// </summary>
        public string Name { get; }
    
        /// <summary>
        /// Gets the type of the property
        /// </summary>
        public Type Type { get; }
    
        public PropertySchema(string name, Type type)
        {
            Name = name;
            Type = type;
        }
    }
}