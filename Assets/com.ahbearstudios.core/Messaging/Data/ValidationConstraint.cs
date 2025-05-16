using System;

namespace AhBearStudios.Core.Messaging.Data
{
    /// <summary>
    /// Represents a validation constraint for a property
    /// </summary>
    [Serializable]
    public class ValidationConstraint
    {
        /// <summary>
        /// Gets or sets the type of constraint
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Gets or sets the value of the constraint
        /// </summary>
        public string Value { get; set; }
        
        /// <summary>
        /// Gets or sets the error message for the constraint
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the ValidationConstraint class
        /// </summary>
        public ValidationConstraint()
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the ValidationConstraint class with specific values
        /// </summary>
        /// <param name="type">The type of constraint</param>
        /// <param name="value">The value of the constraint</param>
        /// <param name="errorMessage">The error message for the constraint</param>
        public ValidationConstraint(string type, string value, string errorMessage = null)
        {
            Type = type;
            Value = value;
            ErrorMessage = errorMessage ?? $"Validation failed for constraint {type}";
        }
    }
}