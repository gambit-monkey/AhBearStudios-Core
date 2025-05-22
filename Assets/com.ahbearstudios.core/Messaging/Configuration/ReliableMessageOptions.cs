namespace AhBearStudios.Core.Messaging.Configuration
{
    /// <summary>
    /// Options for configuring the behavior of a ReliableMessageBus.
    /// </summary>
    public class ReliableMessageOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether messages should be removed from storage
        /// after successful delivery. If false, messages will be kept for auditing.
        /// </summary>
        public bool RemoveOnSuccessfulDelivery { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the interval, in milliseconds, at which the processor checks for
        /// undelivered messages that need to be redelivered.
        /// </summary>
        public int ProcessingIntervalMs { get; set; } = 5000; // 5 seconds
        
        /// <summary>
        /// Gets or sets the maximum number of retry attempts for a message before it's
        /// considered permanently failed.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 5;
        
        /// <summary>
        /// Gets or sets the base delay, in milliseconds, between retry attempts.
        /// This delay increases with each retry attempt.
        /// </summary>
        public int RetryDelayBaseMs { get; set; } = 1000; // 1 second
        
        /// <summary>
        /// Gets or sets the maximum age, in minutes, of messages in the store.
        /// Messages older than this will be automatically removed.
        /// Set to 0 to disable automatic cleanup.
        /// </summary>
        public int MaxMessageAgeMinutes { get; set; } = 1440; // 24 hours
        
        /// <summary>
        /// Gets or sets a value indicating whether to remove messages from the store
        /// after exceeding the maximum delivery attempts.
        /// </summary>
        public bool RemoveFailedMessages { get; set; } = true;
        
        /// <summary>
        /// Gets or sets a value indicating whether to rethrow exceptions that occur
        /// during message publishing.
        /// </summary>
        public bool RethrowExceptions { get; set; } = false;
    }
}