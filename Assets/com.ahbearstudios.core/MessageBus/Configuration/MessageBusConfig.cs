namespace AhBearStudios.Core.Messaging.Configuration
{
    /// <summary>
    /// Configuration for the message bus system.
    /// </summary>
    public sealed class MessageBusConfig
    {
        /// <summary>
        /// Gets or sets whether diagnostic logging is enabled.
        /// </summary>
        public bool EnableDiagnosticLogging { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether performance profiling is enabled.
        /// </summary>
        public bool EnablePerformanceProfiling { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to capture stack traces for debugging.
        /// When enabled, this can have a performance impact.
        /// </summary>
        public bool EnableCaptureStackTrace { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the maximum number of subscribers to a single message type.
        /// A value of 0 means unlimited.
        /// </summary>
        public int MaxSubscribersPerMessage { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets whether to validate message handlers at registration time.
        /// </summary>
        public bool ValidateOnRegistration { get; set; } = true;
    }
}