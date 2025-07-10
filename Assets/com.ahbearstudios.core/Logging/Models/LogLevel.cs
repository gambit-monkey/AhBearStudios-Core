namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Defines the severity levels for log messages.
    /// Higher values indicate more severe issues.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Detailed diagnostic information, typically only useful during development.
        /// </summary>
        Debug = 0,

        /// <summary>
        /// General information about application execution.
        /// </summary>
        Info = 1,

        /// <summary>
        /// Potentially harmful situations that are not necessarily errors.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Error events that might still allow the application to continue running.
        /// </summary>
        Error = 3,

        /// <summary>
        /// Critical failures that may cause the application to terminate.
        /// </summary>
        Critical = 4
    }
}