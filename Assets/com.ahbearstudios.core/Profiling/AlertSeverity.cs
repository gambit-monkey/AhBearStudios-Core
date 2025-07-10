namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// Severity levels for metric alerts
    /// </summary>
    public enum AlertSeverity : byte
    {
        /// <summary>
        /// Informational alert
        /// </summary>
        Info = 0,
        
        /// <summary>
        /// Warning alert
        /// </summary>
        Warning = 1,
        
        /// <summary>
        /// Error alert
        /// </summary>
        Error = 2,
        
        /// <summary>
        /// Critical alert
        /// </summary>
        Critical = 3
    }
}