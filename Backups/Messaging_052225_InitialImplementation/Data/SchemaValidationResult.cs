using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging.Data
{
    /// <summary>
    /// Represents the result of a schema validation
    /// </summary>
    public class SchemaValidationResult
    {
        /// <summary>
        /// Gets a value indicating whether the validation succeeded
        /// </summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>
        /// Gets the validation errors
        /// </summary>
        public List<ValidationError> Errors { get; } = new List<ValidationError>();

        /// <summary>
        /// Adds an error to the validation result
        /// </summary>
        /// <param name="propertyName">The name of the property with the error</param>
        /// <param name="errorMessage">The error message</param>
        public void AddError(string propertyName, string errorMessage)
        {
            Errors.Add(new ValidationError(propertyName, errorMessage));
        }
    }
}