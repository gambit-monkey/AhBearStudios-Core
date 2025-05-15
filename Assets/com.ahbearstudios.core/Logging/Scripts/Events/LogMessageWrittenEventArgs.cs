using AhBearStudios.Core.Logging.Data;

namespace AhBearStudios.Core.Logging.Events
{
    /// <summary>
    /// Event arguments for message written events.
    /// </summary>
    public class LogMessageWrittenEventArgs : LogMessageEventArgs
    {
        /// <summary>
        /// Gets the number of targets the message was written to.
        /// </summary>
        public int TargetCount { get; }

        /// <summary>
        /// Creates new event arguments with the specified message and target count.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="targetCount">The number of targets the message was written to.</param>
        public LogMessageWrittenEventArgs(LogMessage message, int targetCount) : base(message)
        {
            TargetCount = targetCount;
        }
    }
}