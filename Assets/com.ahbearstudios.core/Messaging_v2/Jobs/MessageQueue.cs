using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging.Jobs
{
    /// <summary>
    /// A queue for processing messages in a Burst-compatible job.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to process. Must be a blittable struct.</typeparam>
    public struct MessageQueue<TMessage> : IDisposable where TMessage : struct, IMessage
    {
        /// <summary>
        /// The queue of messages to process.
        /// </summary>
        public NativeQueue<TMessage> Queue;
        
        /// <summary>
        /// Initializes a new instance of the MessageQueue struct.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        public MessageQueue(Allocator allocator)
        {
            Queue = new NativeQueue<TMessage>(allocator);
        }
        
        /// <summary>
        /// Enqueues a message to be processed.
        /// </summary>
        /// <param name="message">The message to enqueue.</param>
        public void Enqueue(TMessage message)
        {
            Queue.Enqueue(message);
        }
        
        /// <summary>
        /// Disposes the message queue.
        /// </summary>
        public void Dispose()
        {
            if (Queue.IsCreated)
            {
                Queue.Dispose();
            }
        }
    }
    
    /// <summary>
    /// A job for processing messages in a Burst-compatible way.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to process. Must be a blittable struct.</typeparam>
    [BurstCompile]
    public struct ProcessMessagesJob<TMessage> : IJob where TMessage : struct, IMessage
    {
        /// <summary>
        /// The queue of messages to process.
        /// </summary>
        public NativeQueue<TMessage> Queue;
        
        /// <summary>
        /// The count of processed messages.
        /// </summary>
        public NativeReference<int> ProcessedCount;
        
        /// <summary>
        /// Executes the job.
        /// </summary>
        public void Execute()
        {
            int count = 0;
            
            while (Queue.TryDequeue(out TMessage message))
            {
                // Process the message
                // In a real implementation, you would have a function pointer or equivalent
                // to process the message in a Burst-compatible way
                
                count++;
            }
            
            ProcessedCount.Value = count;
        }
    }
}