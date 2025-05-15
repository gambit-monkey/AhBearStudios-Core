using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Standard log severity levels as byte constants.
    /// Maps directly to corresponding Tagging.LogTag values for consistency.
    /// </summary>
    public static class LogLevel
    {
        /// <summary>
        /// Debug level - least severe, used for detailed troubleshooting information.
        /// </summary>
        public const byte Debug = (byte)Tagging.LogTag.Debug;
        
        /// <summary>
        /// Information level - general operational information.
        /// </summary>
        public const byte Info = (byte)Tagging.LogTag.Info;
        
        /// <summary>
        /// Warning level - non-critical issues that might need attention.
        /// </summary>
        public const byte Warning = (byte)Tagging.LogTag.Warning;
        
        /// <summary>
        /// Error level - issues that prevent normal operation but don't crash the application.
        /// </summary>
        public const byte Error = (byte)Tagging.LogTag.Error;
        
        /// <summary>
        /// Critical level - severe errors that may cause the application to terminate.
        /// </summary>
        public const byte Critical = (byte)Tagging.LogTag.Critical;
        
        /// <summary>
        /// Determines if a log level meets or exceeds a minimum threshold.
        /// </summary>
        /// <param name="level">The log level to check.</param>
        /// <param name="minimumLevel">The minimum acceptable level.</param>
        /// <returns>True if the level meets or exceeds the minimum threshold.</returns>
        public static bool MeetsMinimumLevel(byte level, byte minimumLevel)
        {
            return level >= minimumLevel;
        }
    }
}