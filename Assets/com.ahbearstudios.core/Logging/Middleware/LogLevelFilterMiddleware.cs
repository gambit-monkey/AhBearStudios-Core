using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Messages;

namespace AhBearStudios.Core.Logging.Middleware
{
    /// <summary>
    /// Log middleware that filters messages based on dynamic log level configuration.
    /// Uses an ILogLevelManager to determine which messages should be processed.
    /// </summary>
    public class LogLevelFilterMiddleware : ILogMiddleware
    {
        private readonly ILogLevelManager _levelManager;
        
        /// <summary>
        /// Gets or sets the next middleware in the chain.
        /// </summary>
        public ILogMiddleware Next { get; set; }
        
        /// <summary>
        /// Creates a new LogLevelFilterMiddleware.
        /// </summary>
        /// <param name="levelManager">The log level manager to use for filtering decisions.</param>
        public LogLevelFilterMiddleware(ILogLevelManager levelManager)
        {
            _levelManager = levelManager ?? throw new System.ArgumentNullException(nameof(levelManager));
        }
        
        /// <summary>
        /// Process a log message by filtering based on dynamic log levels.
        /// </summary>
        /// <param name="message">The log message to filter.</param>
        /// <returns>True to continue processing, false to stop.</returns>
        public bool Process(ref LogMessage message)
        {
            // Get category from message properties if available
            string category = null;
            if (message.Properties.IsCreated)
            {
                foreach (var prop in message.Properties)
                {
                    if (prop.Key == LogPropertyKeys.Category)
                    {
                        category = prop.Value.ToString();
                        break;
                    }
                }
            }
            
            // Use the level manager to determine if message should be logged
            bool shouldLog = _levelManager.ShouldLog(message.Level, message.Tag, category);
            
            if (!shouldLog)
                return false; // Filter out this message
            
            // Continue processing with next middleware
            return Next?.Process(ref message) ?? true;
        }
        
        /// <summary>
        /// Gets the log level manager used by this middleware.
        /// </summary>
        public ILogLevelManager LevelManager => _levelManager;
    }
}