using System;

namespace AhBearStudios.Core.Messaging.Configuration
{
    /// <summary>
    /// Builder for configuring the message bus system.
    /// </summary>
    public sealed class MessageBusConfigBuilder
    {
        private readonly MessageBusConfig _config = new MessageBusConfig();
        
        /// <summary>
        /// Enables diagnostic logging.
        /// </summary>
        /// <returns>The builder for method chaining.</returns>
        public MessageBusConfigBuilder WithDiagnosticLogging()
        {
            _config.EnableDiagnosticLogging = true;
            return this;
        }
        
        /// <summary>
        /// Disables diagnostic logging.
        /// </summary>
        /// <returns>The builder for method chaining.</returns>
        public MessageBusConfigBuilder WithoutDiagnosticLogging()
        {
            _config.EnableDiagnosticLogging = false;
            return this;
        }
        
        /// <summary>
        /// Enables performance profiling.
        /// </summary>
        /// <returns>The builder for method chaining.</returns>
        public MessageBusConfigBuilder WithPerformanceProfiling()
        {
            _config.EnablePerformanceProfiling = true;
            return this;
        }
        
        /// <summary>
        /// Disables performance profiling.
        /// </summary>
        /// <returns>The builder for method chaining.</returns>
        public MessageBusConfigBuilder WithoutPerformanceProfiling()
        {
            _config.EnablePerformanceProfiling = false;
            return this;
        }
        
        /// <summary>
        /// Enables capturing stack traces for debugging.
        /// </summary>
        /// <returns>The builder for method chaining.</returns>
        public MessageBusConfigBuilder WithStackTraceCapture()
        {
            _config.EnableCaptureStackTrace = true;
            return this;
        }
        
        /// <summary>
        /// Disables capturing stack traces for debugging.
        /// </summary>
        /// <returns>The builder for method chaining.</returns>
        public MessageBusConfigBuilder WithoutStackTraceCapture()
        {
            _config.EnableCaptureStackTrace = false;
            return this;
        }
        
        /// <summary>
        /// Sets the maximum number of subscribers to a single message type.
        /// </summary>
        /// <param name="maxCount">The maximum number of subscribers.</param>
        /// <returns>The builder for method chaining.</returns>
        public MessageBusConfigBuilder WithMaxSubscribers(int maxCount)
        {
            if (maxCount < 0) throw new ArgumentOutOfRangeException(nameof(maxCount), "Maximum subscriber count cannot be negative.");
            _config.MaxSubscribersPerMessage = maxCount;
            return this;
        }
        
        /// <summary>
        /// Enables validation of message handlers at registration time.
        /// </summary>
        /// <returns>The builder for method chaining.</returns>
        public MessageBusConfigBuilder WithValidation()
        {
            _config.ValidateOnRegistration = true;
            return this;
        }
        
        /// <summary>
        /// Disables validation of message handlers at registration time.
        /// </summary>
        /// <returns>The builder for method chaining.</returns>
        public MessageBusConfigBuilder WithoutValidation()
        {
            _config.ValidateOnRegistration = false;
            return this;
        }
        
        /// <summary>
        /// Builds the message bus configuration.
        /// </summary>
        /// <returns>The configured MessageBusConfig instance.</returns>
        public MessageBusConfig Build()
        {
            return _config;
        }
    }
}