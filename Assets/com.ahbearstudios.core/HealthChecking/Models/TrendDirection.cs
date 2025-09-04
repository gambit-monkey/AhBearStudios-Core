namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Trend direction enumeration.
    /// </summary>
    public enum TrendDirection
    {
        /// <summary>
        /// Strongly improving trend.
        /// </summary>
        StronglyImproving,

        /// <summary>
        /// Improving trend.
        /// </summary>
        Improving,

        /// <summary>
        /// Stable trend.
        /// </summary>
        Stable,

        /// <summary>
        /// Degrading trend.
        /// </summary>
        Degrading,

        /// <summary>
        /// Strongly degrading trend.
        /// </summary>
        StronglyDegrading,

        /// <summary>
        /// Insufficient data for trend analysis.
        /// </summary>
        InsufficientData
    }
}