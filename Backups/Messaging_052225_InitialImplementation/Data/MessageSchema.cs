using System;
using System.Collections.Generic;
using UnityEngine;

namespace AhBearStudios.Core.Messaging.Data
{
    /// <summary>
    /// Represents a message schema
    /// </summary>
    public class MessageSchema
    {
        /// <summary>
        /// Gets the message type
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// Gets or sets the schema version
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the schema name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the schema description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets the properties of the message
        /// </summary>
        public List<PropertySchema> Properties { get; } = new List<PropertySchema>();

        public MessageSchema(Type messageType)
        {
            MessageType = messageType;
            Name = messageType.Name;
        }

        /// <summary>
        /// Exports the schema to JSON
        /// </summary>
        /// <returns>The JSON representation of the schema</returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>
        /// Imports a schema from JSON
        /// </summary>
        /// <param name="json">The JSON representation of the schema</param>
        /// <returns>The imported schema</returns>
        public static MessageSchema FromJson(string json)
        {
            return JsonUtility.FromJson<MessageSchema>(json);
        }
    }
}