namespace AhBearStudios.Core.Messaging.Data
{
    /// <summary>
    /// Represents a validation constraint for a property
    /// </summary>
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

        public ValidationConstraint()
        {
        }

        public ValidationConstraint(string type, string value, string errorMessage = null)
        {
            Type = type;
            Value = value;
            ErrorMessage = errorMessage;
        }
    }
}