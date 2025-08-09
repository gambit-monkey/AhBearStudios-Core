namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Logical operator for combining filter results.
    /// </summary>
    public enum LogicalOperator
    {
        /// <summary>
        /// All filters must pass (logical AND).
        /// </summary>
        And = 0,

        /// <summary>
        /// At least one filter must pass (logical OR).
        /// </summary>
        Or = 1,

        /// <summary>
        /// Exactly one filter must pass (logical XOR).
        /// </summary>
        Xor = 2,

        /// <summary>
        /// No filters must pass (logical NOT).
        /// </summary>
        Not = 3
    }
}