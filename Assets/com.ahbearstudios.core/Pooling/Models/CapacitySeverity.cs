namespace AhBearStudios.Core.Pooling.Models
{
    /// <summary>
    /// Severity levels for pool capacity issues.
    /// </summary>
    public enum CapacitySeverity : byte
    {
        /// <summary>
        /// Information about normal capacity usage.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Warning about approaching capacity limits.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Critical capacity situation requiring immediate attention.
        /// </summary>
        Critical = 2,

        /// <summary>
        /// Emergency situation - pool exhausted.
        /// </summary>
        Emergency = 3
    }
}