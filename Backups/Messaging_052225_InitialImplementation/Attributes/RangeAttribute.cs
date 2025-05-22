namespace AhBearStudios.Core.Messaging.Attributes
{
    /// <summary>
    /// Attribute for specifying a range constraint on a property or field
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field, AllowMultiple = false)]
    public class RangeAttribute : System.Attribute
    {
        /// <summary>
        /// Gets the minimum allowable value
        /// </summary>
        public object Minimum { get; }
        
        /// <summary>
        /// Gets the maximum allowable value
        /// </summary>
        public object Maximum { get; }
        
        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the RangeAttribute class with integer range
        /// </summary>
        /// <param name="minimum">The minimum allowable value</param>
        /// <param name="maximum">The maximum allowable value</param>
        public RangeAttribute(int minimum, int maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
            ErrorMessage = $"Value must be between {minimum} and {maximum}";
        }
        
        /// <summary>
        /// Initializes a new instance of the RangeAttribute class with double range
        /// </summary>
        /// <param name="minimum">The minimum allowable value</param>
        /// <param name="maximum">The maximum allowable value</param>
        public RangeAttribute(double minimum, double maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
            ErrorMessage = $"Value must be between {minimum} and {maximum}";
        }
        
        /// <summary>
        /// Initializes a new instance of the RangeAttribute class with type-specified range
        /// </summary>
        /// <param name="type">The type of the range values</param>
        /// <param name="minimum">The minimum allowable value as a string</param>
        /// <param name="maximum">The maximum allowable value as a string</param>
        public RangeAttribute(System.Type type, string minimum, string maximum)
        {
            // Convert the string values to the appropriate type
            Minimum = System.Convert.ChangeType(minimum, type);
            Maximum = System.Convert.ChangeType(maximum, type);
            ErrorMessage = $"Value must be between {minimum} and {maximum}";
        }
        
        /// <summary>
        /// Checks if a value is within the specified range
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <returns>True if the value is within range; otherwise, false</returns>
        public bool IsValid(object value)
        {
            if (value == null)
                return false;
                
            var valueType = value.GetType();
            
            if (Minimum is int minimumInt && Maximum is int maximumInt && value is int valueInt)
            {
                return valueInt >= minimumInt && valueInt <= maximumInt;
            }
            else if (Minimum is double minimumDouble && Maximum is double maximumDouble)
            {
                if (value is float valueFloat)
                {
                    return valueFloat >= minimumDouble && valueFloat <= maximumDouble;
                }
                else if (value is double valueDouble)
                {
                    return valueDouble >= minimumDouble && valueDouble <= maximumDouble;
                }
                else if (value is decimal valueDecimal)
                {
                    var min = (decimal)minimumDouble;
                    var max = (decimal)maximumDouble;
                    return valueDecimal >= min && valueDecimal <= max;
                }
            }
            else if (Minimum is System.DateTime minimumDate && Maximum is System.DateTime maximumDate && value is System.DateTime valueDate)
            {
                return valueDate >= minimumDate && valueDate <= maximumDate;
            }
            else
            {
                // Try to use IComparable for comparison
                if (Minimum is System.IComparable comparableMin && 
                    Maximum is System.IComparable comparableMax &&
                    value is System.IComparable comparableValue)
                {
                    // Try to convert value to the type of Minimum/Maximum
                    var minType = Minimum.GetType();
                    var convertedValue = System.Convert.ChangeType(value, minType);
                    
                    if (convertedValue is System.IComparable convertedComparable)
                    {
                        return comparableMin.CompareTo(convertedValue) <= 0 && 
                               comparableMax.CompareTo(convertedValue) >= 0;
                    }
                }
            }
            
            return false;
        }
    }
}