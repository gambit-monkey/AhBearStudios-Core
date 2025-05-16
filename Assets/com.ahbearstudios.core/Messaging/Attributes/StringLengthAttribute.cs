namespace AhBearStudios.Core.Messaging.Attributes
{
    /// <summary>
    /// Attribute for specifying a string length constraint on a property or field
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field, AllowMultiple = false)]
    public class StringLengthAttribute : System.Attribute
    {
        /// <summary>
        /// Gets the maximum allowable length of the string
        /// </summary>
        public int MaximumLength { get; }
        
        /// <summary>
        /// Gets the minimum allowable length of the string
        /// </summary>
        public int MinimumLength { get; set; }
        
        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the StringLengthAttribute class
        /// </summary>
        /// <param name="maximumLength">The maximum allowable length of the string</param>
        public StringLengthAttribute(int maximumLength)
        {
            MaximumLength = maximumLength;
            MinimumLength = 0;
            ErrorMessage = $"String must be at most {maximumLength} characters long";
        }
        
        /// <summary>
        /// Initializes a new instance of the StringLengthAttribute class with a specified minimum and maximum length
        /// </summary>
        /// <param name="maximumLength">The maximum allowable length of the string</param>
        /// <param name="minimumLength">The minimum allowable length of the string</param>
        public StringLengthAttribute(int maximumLength, int minimumLength)
        {
            MaximumLength = maximumLength;
            MinimumLength = minimumLength;
            ErrorMessage = $"String must be between {minimumLength} and {maximumLength} characters long";
        }
        
        /// <summary>
        /// Checks if a string value is within the specified length range
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <returns>True if the value is within range; otherwise, false</returns>
        public bool IsValid(object value)
        {
            if (value == null)
                return MinimumLength == 0;
                
            if (value is string stringValue)
            {
                return stringValue.Length >= MinimumLength && stringValue.Length <= MaximumLength;
            }
            
            return false;
        }
    }
}