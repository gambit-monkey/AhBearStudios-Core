namespace AhBearStudios.Core.Profiling.Models
{
    /// <summary>
    /// Specifies the severity level of profiler errors.
    /// Used to categorize and prioritize error handling in the profiling system.
    /// </summary>
    public enum ProfilerErrorSeverity : byte
    {
        /// <summary>
        /// Informational message, not an actual error.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Warning that may indicate potential issues.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Error that affects profiling functionality.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Critical error that may cause profiling system failure.
        /// </summary>
        Critical = 3
    }
}