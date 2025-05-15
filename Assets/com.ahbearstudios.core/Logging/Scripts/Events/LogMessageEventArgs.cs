using System;
using AhBearStudios.Core.Logging.Data;

namespace AhBearStudios.Core.Logging.Events
{
    /// <summary>
    /// Event arguments for log message events.
    /// </summary>
    public class LogMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the log message.
        /// </summary>
        public LogMessage Message { get; }

        /// <summary>
        /// Creates new event arguments with the specified message.
        /// </summary>
        /// <param name="message">The log message.</param>
        public LogMessageEventArgs(LogMessage message)
        {
            Message = message;
        }
    }
}