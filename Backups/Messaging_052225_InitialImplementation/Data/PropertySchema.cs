using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging.Data
{
    /// <summary>
    /// Represents a property in a message schema
    /// </summary>
    public class PropertySchema
    {
        /// <summary>
        /// Gets or sets the name of the property
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the property
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Gets or sets the description of the property
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the property is required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets the default value of the property
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the validation constraints for the property
        /// </summary>
        public List<ValidationConstraint> Constraints { get; set; } = new List<ValidationConstraint>();

        public PropertySchema()
        {
        }

        public PropertySchema(string name, string typeName, string description = null, bool isRequired = false)
        {
            Name = name;
            TypeName = typeName;
            Description = description;
            IsRequired = isRequired;
        }
    }
}