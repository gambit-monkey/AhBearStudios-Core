namespace AhBearStudios.Core.Messaging.Models
{
    /// <summary>
    /// Enum for threshold comparison types.
    /// </summary>
    public enum ThresholdComparisonType
    {
        /// <summary>
        /// Trigger when value is greater than threshold.
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Trigger when value is less than threshold.
        /// </summary>
        LessThan,

        /// <summary>
        /// Trigger when value equals threshold.
        /// </summary>
        Equals,

        /// <summary>
        /// Trigger when value is greater than or equal to threshold.
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// Trigger when value is less than or equal to threshold.
        /// </summary>
        LessThanOrEqual
    }
}