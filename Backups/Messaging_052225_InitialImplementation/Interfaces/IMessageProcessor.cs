namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for processing messages in a queue
    /// </summary>
    public interface IMessageProcessor
    {
        /// <summary>
        /// Processes all messages in the queue
        /// </summary>
        void ProcessQueue();
    
        /// <summary>
        /// Processes up to the specified number of messages in the queue
        /// </summary>
        /// <param name="maxCount">The maximum number of messages to process</param>
        /// <returns>The number of messages processed</returns>
        int ProcessQueue(int maxCount);
    
        /// <summary>
        /// Gets the number of messages in the queue
        /// </summary>
        int QueueCount { get; }
    
        /// <summary>
        /// Gets or sets the maximum size of the queue
        /// </summary>
        int MaxQueueSize { get; set; }
    
        /// <summary>
        /// Gets a value indicating whether the processor is enabled
        /// </summary>
        bool IsEnabled { get; }
    
        /// <summary>
        /// Enables message processing
        /// </summary>
        void Enable();
    
        /// <summary>
        /// Disables message processing
        /// </summary>
        void Disable();
    }
}