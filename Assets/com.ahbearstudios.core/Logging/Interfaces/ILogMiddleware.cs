using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Messages;

namespace AhBearStudios.Core.Logging.Middleware
{
    /// <summary>
    /// Interface for log middleware components that can intercept and modify log messages
    /// before they are processed by log targets.
    /// </summary>
    public interface ILogMiddleware
    {
        /// <summary>
        /// Gets or sets the next middleware in the chain.
        /// </summary>
        ILogMiddleware Next { get; set; }
        
        /// <summary>
        /// Process a log message and optionally pass it to the next middleware in the chain.
        /// </summary>
        /// <param name="message">The log message to process.</param>
        /// <returns>True if the message should continue down the chain, false to stop processing.</returns>
        bool Process(ref LogMessage message);
    }
}