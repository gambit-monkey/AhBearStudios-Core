using System;
using AhBearStudios.Core.Logging.Data;

namespace AhBearStudios.Core.Logging.Events
{
    /// <summary>
    /// Event arguments for log level changed events.
    /// </summary>
    public class LogLevelChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the previous log level.
        /// </summary>
        public byte OldLevel { get; }

        /// <summary>
        /// Gets the new log level.
        /// </summary>
        public byte NewLevel { get; }

        /// <summary>
        /// Creates new event arguments with the specified old and new levels.
        /// </summary>
        /// <param name="oldLevel">The previous log level.</param>
        /// <param name="newLevel">The new log level.</param>
        public LogLevelChangedEventArgs(byte oldLevel, byte newLevel)
        {
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }
}