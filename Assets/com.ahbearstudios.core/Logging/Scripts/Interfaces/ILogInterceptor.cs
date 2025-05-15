using AhBearStudios.Core.Logging.Data;

namespace AhBearStudios.Core.Logging.Middleware
{
    /// <summary>
    /// Interface for log interceptors that can modify or enrich log messages before they are processed.
    /// Implements the middleware pattern for the logging pipeline.
    /// </summary>
    public interface ILogInterceptor
    {
        /// <summary>
        /// Process a log message, optionally modifying it before it continues through the pipeline.
        /// </summary>
        /// <param name="message">The log message to process.</param>
        /// <returns>
        /// True if the message should continue through the pipeline, false to drop the message.
        /// </returns>
        bool Process(ref LogMessage message);
        
        /// <summary>
        /// Gets the execution order of this interceptor. Lower values run earlier.
        /// </summary>
        int Order { get; }
        
        /// <summary>
        /// Gets whether this interceptor is enabled.
        /// </summary>
        bool IsEnabled { get; }
    }
}