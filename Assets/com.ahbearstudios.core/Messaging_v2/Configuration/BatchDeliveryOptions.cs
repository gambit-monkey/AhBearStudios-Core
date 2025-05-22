using System;

namespace AhBearStudios.Core.Messaging.Configuration
{
    /// <summary>
    /// Options for batch message delivery.
    /// </summary>
    public sealed class BatchDeliveryOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of messages to send in parallel.
        /// </summary>
        public int MaxConcurrency { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets whether to stop on the first error.
        /// </summary>
        public bool StopOnFirstError { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the timeout for the entire batch operation.
        /// </summary>
        public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromMinutes(5);
        
        /// <summary>
        /// Gets or sets the timeout for individual message delivery.
        /// </summary>
        public TimeSpan MessageTimeout { get; set; } = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// Gets or sets whether to require confirmation for all messages.
        /// </summary>
        public bool RequireConfirmation { get; set; } = false;
    }
}