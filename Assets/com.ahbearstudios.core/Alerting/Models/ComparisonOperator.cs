namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Comparison operators for rule evaluation.
    /// </summary>
    public enum ComparisonOperator : byte
    {
        /// <summary>
        /// Equal comparison.
        /// </summary>
        Equal = 0,

        /// <summary>
        /// Not equal comparison.
        /// </summary>
        NotEqual = 1,

        /// <summary>
        /// Greater than comparison.
        /// </summary>
        GreaterThan = 2,

        /// <summary>
        /// Greater than or equal comparison.
        /// </summary>
        GreaterThanOrEqual = 3,

        /// <summary>
        /// Less than comparison.
        /// </summary>
        LessThan = 4,

        /// <summary>
        /// Less than or equal comparison.
        /// </summary>
        LessThanOrEqual = 5
    }
}