using System;
using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Data;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Represents a property in a message schema
    /// </summary>
    [Serializable]
    public class PropertySchema
    {
        /// <summary>
        /// Gets or sets the name of the property
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the type name of the property
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
        /// Gets or sets a value indicating whether the property is a collection
        /// </summary>
        public bool IsCollection { get; set; }
        
        /// <summary>
        /// Gets or sets the element type name if this property is a collection
        /// </summary>
        public string ElementTypeName { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the property represents a complex type
        /// </summary>
        public bool IsComplexType { get; set; }
        
        /// <summary>
        /// Gets or sets the schema reference if this property is a complex type
        /// </summary>
        public string SchemaReference { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the property is nullable
        /// </summary>
        public bool IsNullable { get; set; }
        
        /// <summary>
        /// Gets or sets the order of the property
        /// </summary>
        public int Order { get; set; }
        
        /// <summary>
        /// Gets or sets custom properties for this property
        /// </summary>
        public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Gets or sets the validation constraints for the property
        /// </summary>
        public List<ValidationConstraint> Constraints { get; set; } = new List<ValidationConstraint>();
        
        /// <summary>
        /// Initializes a new instance of the PropertySchema class
        /// </summary>
        public PropertySchema()
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the PropertySchema class with specific values
        /// </summary>
        /// <param name="name">The name of the property</param>
        /// <param name="typeName">The type name of the property</param>
        /// <param name="description">The description of the property</param>
        /// <param name="isRequired">Whether the property is required</param>
        public PropertySchema(string name, string typeName, string description = null, bool isRequired = false)
        {
            Name = name;
            TypeName = typeName;
            Description = description;
            IsRequired = isRequired;
        }
        
        /// <summary>
        /// Adds a constraint to the property
        /// </summary>
        /// <param name="constraint">The constraint to add</param>
        public void AddConstraint(ValidationConstraint constraint)
        {
            if (constraint == null)
                throw new ArgumentNullException(nameof(constraint));
                
            Constraints.Add(constraint);
        }
        
        /// <summary>
        /// Gets a constraint by type
        /// </summary>
        /// <param name="constraintType">The type of constraint</param>
        /// <returns>The constraint, or null if not found</returns>
        public ValidationConstraint GetConstraint(string constraintType)
        {
            if (string.IsNullOrEmpty(constraintType))
                throw new ArgumentNullException(nameof(constraintType));
                
            return Constraints.Find(c => c.Type == constraintType);
        }
    }
}