namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Enum defining message log levels
    /// </summary>
    public enum MessageLogLevel
    {
        /// <summary>
        /// No logging
        /// </summary>
        None,
    
        /// <summary>
        /// Error logging only
        /// </summary>
        Error,
    
        /// <summary>
        /// Warning and error logging
        /// </summary>
        Warning,
    
        /// <summary>
        /// Information, warning, and error logging
        /// </summary>
        Info,
    
        /// <summary>
        /// Debug, information, warning, and error logging
        /// </summary>
        Debug,
    
        /// <summary>
        /// Trace, debug, information, warning, and error logging
        /// </summary>
        Trace
    }
}