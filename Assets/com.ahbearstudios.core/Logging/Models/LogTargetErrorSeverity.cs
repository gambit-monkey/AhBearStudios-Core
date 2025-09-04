namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Defines the severity levels for log target errors.
    /// </summary>
    public enum LogTargetErrorSeverity : byte
    {
        /// <summary>
        /// Informational - target recovered or minor issue.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Warning - target had an issue but continues operating.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Error - target failed to process but may recover.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Critical - target is non-functional and requires intervention.
        /// </summary>
        Critical = 3
    }
}