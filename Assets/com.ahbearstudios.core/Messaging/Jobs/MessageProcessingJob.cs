using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging.Jobs
{
    /// <summary>
    /// Burst-compatible job for processing messages in parallel.
    /// Processes messages from a source queue and can optionally
    /// output processed messages to a destination queue.
    /// </summary>
    /// <typeparam name="T">The type of message to process.</typeparam>
    [BurstCompile]
    public struct MessageProcessingJob<T> : IJob where T : unmanaged, IMessage
    {
        /// <summary>
        /// The source message queue to read messages from.
        /// </summary>
        [ReadOnly]
        public NativeMessageQueue<T> SourceQueue;
        
        /// <summary>
        /// The destination message queue to write processed messages to (optional).
        /// </summary>
        public NativeMessageQueue<T> DestinationQueue;
        
        /// <summary>
        /// Indicates whether the job should process all available messages.
        /// </summary>
        public bool ProcessAllMessages;
        
        /// <summary>
        /// The maximum number of messages to process.
        /// </summary>
        public int MaxMessagesToProcess;
        
        /// <summary>
        /// Function pointer for the message processing function.
        /// </summary>
        public FunctionPointer<ProcessMessageDelegate> ProcessFunction;
        
        /// <summary>
        /// Counter for the number of messages processed.
        /// </summary>
        public NativeReference<int> ProcessedCount;
        
        /// <summary>
        /// Counter for the number of messages that failed processing.
        /// </summary>
        public NativeReference<int> FailedCount;

        /// <summary>
        /// Delegate for the message processing function.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <returns>The processed message, if any.</returns>
        public delegate T ProcessMessageDelegate(T message);

        /// <summary>
        /// Executes the job, processing messages from the source queue.
        /// </summary>
        [BurstCompile]
        public void Execute()
        {
            // Safety checks for required resources
            if (!SourceQueue.IsCreated)
            {
                return;
            }
            
            if (!ProcessFunction.IsCreated)
            {
                return;
            }

            // Initialize counters if they're not already created
            bool disposeProcessedCount = false;
            bool disposeFailedCount = false;
            
            if (!ProcessedCount.IsCreated)
            {
                ProcessedCount = new NativeReference<int>(0, Allocator.Temp);
                disposeProcessedCount = true;
            }
            
            if (!FailedCount.IsCreated)
            {
                FailedCount = new NativeReference<int>(0, Allocator.Temp);
                disposeFailedCount = true;
            }

            // Process messages
            int messagesProcessed = 0;
            int processedValue = ProcessedCount.Value;
            int failedValue = FailedCount.Value;
            
            try
            {
                // Continue processing while we have messages and haven't reached the limit
                while (messagesProcessed < MaxMessagesToProcess || ProcessAllMessages)
                {
                    // Try to dequeue a message
                    if (!SourceQueue.TryDequeue(out T message))
                    {
                        // No more messages in the queue
                        break;
                    }

                    try
                    {
                        // Process the message using the function pointer
                        T processedMessage = ProcessMessage(message);
                        
                        // If a destination queue is provided and created, add the processed message to it
                        if (DestinationQueue.IsCreated)
                        {
                            if (!DestinationQueue.TryEnqueue(processedMessage))
                            {
                                // If the destination queue is full, increment the failed count
                                failedValue++;
                            }
                        }
                        
                        // Increment the processed count
                        processedValue++;
                        messagesProcessed++;
                    }
                    catch
                    {
                        // Increment the failed count in case of any exception
                        failedValue++;
                    }
                }
                
                // Update the counter values
                ProcessedCount.Value = processedValue;
                FailedCount.Value = failedValue;
            }
            finally
            {
                // Dispose the counters if we created them
                if (disposeProcessedCount && ProcessedCount.IsCreated)
                {
                    ProcessedCount.Dispose();
                }
                
                if (disposeFailedCount && FailedCount.IsCreated)
                {
                    FailedCount.Dispose();
                }
            }
        }

        /// <summary>
        /// Processes a message using the function pointer.
        /// This method allows the BurstDiscard attribute to be properly applied.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <returns>The processed message.</returns>
        [BurstDiscard]
        private T ProcessMessage(T message)
        {
            return ProcessFunction.Invoke(message);
        }
    }
}