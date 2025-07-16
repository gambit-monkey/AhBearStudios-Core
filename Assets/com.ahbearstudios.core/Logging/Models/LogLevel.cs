namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Represents detailed tracing information within the application.
    /// This level is typically used for high-volume and fine-grained diagnostic messages.
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
        Critical = 4,

        /// <summary>
        /// Provides the most detailed and verbose logging level, used for tracing the exact flow and operations of the application.
        /// Typically helpful for diagnosing complex issues by capturing extensive diagnostic information.
        /// </summary>
        Trace = 5
    }
}