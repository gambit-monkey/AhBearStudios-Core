namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Defines the severity levels for log channel errors.
    /// </summary>
    public enum LogChannelErrorSeverity : byte
    {
        /// <summary>
        /// Informational - channel recovered or minor issue.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Warning - channel had an issue but continues operating.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Error - channel failed to process but may recover.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Critical - channel is non-functional and requires intervention.
        /// </summary>
        Critical = 3
    }
}