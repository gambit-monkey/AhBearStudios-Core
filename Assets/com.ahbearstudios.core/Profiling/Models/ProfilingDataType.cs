namespace AhBearStudios.Core.Profiling.Models
{
    /// <summary>
    /// Specifies the type of profiling data being recorded.
    /// Used to categorize different measurement types in the profiling system.
    /// </summary>
    public enum ProfilingDataType : byte
    {
        /// <summary>
        /// A one-time sample measurement.
        /// </summary>
        Sample = 0,

        /// <summary>
        /// A metric value tracked over time.
        /// </summary>
        Metric = 1,

        /// <summary>
        /// A counter increment or decrement.
        /// </summary>
        Counter = 2,

        /// <summary>
        /// Scope execution timing data.
        /// </summary>
        ScopeTiming = 3,

        /// <summary>
        /// Custom user-defined measurement.
        /// </summary>
        Custom = 4
    }
}