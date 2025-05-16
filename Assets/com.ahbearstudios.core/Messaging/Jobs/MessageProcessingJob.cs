using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Burst-compatible job for processing messages in parallel.
    /// Processes messages from a source queue or buffer and can optionally
    /// output processed messages to a destination queue or buffer.
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
        /// Delegate for the message processing function.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <returns>The processed message, if any.</returns>
        public delegate T ProcessMessageDelegate(T message);
        
        // Since delegates are not directly Burst-compatible,
        // we use function pointers for the processing logic.
        // The client code will need to use BurstCompiler.CompileFunctionPointer
        // to create a Burst-compatible function pointer.
        
        /// <summary>
        /// Function pointer for the message processing function.
        /// </summary>
        [BurstDiscard]
        public FunctionPointer<ProcessMessageDelegate> ProcessFunction;
        
        /// <summary>
        /// Counter for the number of messages processed.
        /// </summary>
        public NativeAtomic<int> ProcessedCount;
        
        /// <summary>
        /// Counter for the number of messages that failed processing.
        /// </summary>
        public NativeAtomic<int> FailedCount;

        /// <summary>
        /// Executes the job, processing messages from the source queue.
        /// </summary>
        [BurstCompile]
        public void Execute()
        {
            // Initialize counters if they're not already created
            bool disposeProcessedCount = false;
            bool disposeFailedCount = false;
            
            if (!ProcessedCount.IsCreated)
            {
                ProcessedCount = new NativeAtomic<int>(0, Allocator.Temp);
                disposeProcessedCount = true;
            }
            
            if (!FailedCount.IsCreated)
            {
                FailedCount = new NativeAtomic<int>(0, Allocator.Temp);
                disposeFailedCount = true;
            }

            // Process messages
            int messagesProcessed = 0;
            
            try
            {
                while (SourceQueue.TryDequeue(out T message))
                {
                    try
                    {
                        // Process the message
                        T processedMessage = ProcessFunction.Invoke(message);
                        
                        // If a destination queue is provided, add the processed message to it
                        if (DestinationQueue.IsCreated)
                        {
                            if (!DestinationQueue.TryEnqueue(processedMessage))
                            {
                                // If the destination queue is full, increment the failed count
                                FailedCount.Value++;
                            }
                        }
                        
                        // Increment the processed count
                        ProcessedCount.Value++;
                        messagesProcessed++;
                        
                        // If we've reached the maximum number of messages to process and we're not processing all messages, stop
                        if (!ProcessAllMessages && messagesProcessed >= MaxMessagesToProcess)
                        {
                            break;
                        }
                    }
                    catch
                    {
                        // Increment the failed count
                        FailedCount.Value++;
                    }
                }
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
    }

    /// <summary>
    /// Utility class for creating and scheduling message processing jobs.
    /// </summary>
    public static class MessageProcessingJobs
    {
        /// <summary>
        /// Creates and schedules a message processing job.
        /// </summary>
        /// <typeparam name="T">The type of message to process.</typeparam>
        /// <param name="sourceQueue">The source message queue to read messages from.</param>
        /// <param name="processFunction">The function pointer for message processing.</param>
        /// <param name="dependsOn">The JobHandle to depend on.</param>
        /// <param name="destinationQueue">The destination message queue to write processed messages to (optional).</param>
        /// <param name="processAllMessages">Whether to process all available messages.</param>
        /// <param name="maxMessagesToProcess">The maximum number of messages to process.</param>
        /// <param name="processedCount">Counter for the number of messages processed (optional).</param>
        /// <param name="failedCount">Counter for the number of messages that failed processing (optional).</param>
        /// <returns>A JobHandle for the scheduled job.</returns>
        public static JobHandle Schedule<T>(
            NativeMessageQueue<T> sourceQueue,
            FunctionPointer<MessageProcessingJob<T>.ProcessMessageDelegate> processFunction,
            JobHandle dependsOn = default,
            NativeMessageQueue<T> destinationQueue = default,
            bool processAllMessages = true,
            int maxMessagesToProcess = 100,
            NativeAtomic<int> processedCount = default,
            NativeAtomic<int> failedCount = default)
            where T : unmanaged, IMessage
        {
            var job = new MessageProcessingJob<T>
            {
                SourceQueue = sourceQueue,
                DestinationQueue = destinationQueue,
                ProcessFunction = processFunction,
                ProcessAllMessages = processAllMessages,
                MaxMessagesToProcess = maxMessagesToProcess,
                ProcessedCount = processedCount,
                FailedCount = failedCount
            };
            
            return job.Schedule(dependsOn);
        }
    }
}