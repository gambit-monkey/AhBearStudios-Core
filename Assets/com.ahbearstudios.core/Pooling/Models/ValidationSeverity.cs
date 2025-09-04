namespace AhBearStudios.Core.Pooling.Models
{
    /// <summary>
    /// Severity levels for pool validation issues.
    /// </summary>
    public enum ValidationSeverity : byte
    {
        /// <summary>
        /// Minor validation issues that don't affect functionality.
        /// </summary>
        Minor = 0,

        /// <summary>
        /// Moderate validation issues that may affect performance.
        /// </summary>
        Moderate = 1,

        /// <summary>
        /// Major validation issues that affect functionality.
        /// </summary>
        Major = 2,

        /// <summary>
        /// Critical validation issues requiring immediate action.
        /// </summary>
        Critical = 3
    }
}