namespace AhBearStudios.Core.Messaging.Attributes
{
    /// <summary>
    /// Attribute for specifying a regular expression constraint on a property or field
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field, AllowMultiple = false)]
    public class RegularExpressionAttribute : System.Attribute
    {
        /// <summary>
        /// Gets the regular expression pattern
        /// </summary>
        public string Pattern { get; }
        
        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the RegularExpressionAttribute class
        /// </summary>
        /// <param name="pattern">The regular expression pattern</param>
        public RegularExpressionAttribute(string pattern)
        {
            Pattern = pattern;
            ErrorMessage = $"String must match pattern: {pattern}";
        }
        
        /// <summary>
        /// Initializes a new instance of the RegularExpressionAttribute class with a specified error message
        /// </summary>
        /// <param name="pattern">The regular expression pattern</param>
        /// <param name="errorMessage">The error message to use</param>
        public RegularExpressionAttribute(string pattern, string errorMessage)
        {
            Pattern = pattern;
            ErrorMessage = errorMessage;
        }
        
        /// <summary>
        /// Checks if a string value matches the regular expression pattern
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <returns>True if the value matches the pattern; otherwise, false</returns>
        public bool IsValid(object value)
        {
            if (value == null)
                return false;
                
            if (value is string stringValue)
            {
                var regex = new System.Text.RegularExpressions.Regex(Pattern);
                return regex.IsMatch(stringValue);
            }
            
            return false;
        }
    }
}