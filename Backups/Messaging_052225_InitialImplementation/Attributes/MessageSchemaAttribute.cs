using System;

namespace AhBearStudios.Core.Messaging.Attributes
{
    /// <summary>
    /// Attribute for marking a class or struct with schema information
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class MessageSchemaAttribute : Attribute
    {
        /// <summary>
        /// Gets the schema version
        /// </summary>
        public int Version { get; }

        /// <summary>
        /// Gets the schema description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets or sets the category of the message
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this message is transient
        /// </summary>
        public bool IsTransient { get; set; }

        /// <summary>
        /// Initializes a new instance of the MessageSchemaAttribute class
        /// </summary>
        /// <param name="version">The schema version</param>
        /// <param name="description">The schema description</param>
        public MessageSchemaAttribute(int version = 1, string description = null)
        {
            Version = version;
            Description = description;
            Category = "General"; // Default category
        }
    }
}