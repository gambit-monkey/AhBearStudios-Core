using System;

namespace AhBearStudios.Core.Messaging.Attributes
{
    /// <summary>
    /// Attribute for specifying validation constraints on message properties
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class ValidationConstraintAttribute : Attribute
    {
        /// <summary>
        /// Gets the constraint type
        /// </summary>
        public string Type { get; }
       
        /// <summary>
        /// Gets the constraint value
        /// </summary>
        public string Value { get; }
       
        /// <summary>
        /// Gets the error message
        /// </summary>
        public string ErrorMessage { get; }
       
        public ValidationConstraintAttribute(string type, string value, string errorMessage = null)
        {
            Type = type;
            Value = value;
            ErrorMessage = errorMessage;
        }
    }
}