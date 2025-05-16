using System;
using AhBearStudios.Core.Messaging.Data;

namespace AhBearStudios.Core.Messaging.MessageBuses
{
    /// <summary>
    /// Exception thrown when a message fails schema validation
    /// </summary>
    public class SchemaValidationException : Exception
    {
        /// <summary>
        /// Gets the validation result
        /// </summary>
        public SchemaValidationResult ValidationResult { get; }
        
        public SchemaValidationException(string message, SchemaValidationResult validationResult)
            : base(message)
        {
            ValidationResult = validationResult;
        }
    }
}