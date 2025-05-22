
using System;
using AhBearStudios.Core.Messaging.Interfaces;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace AhBearStudios.Core.Messaging.Jobs
{
    /// <summary>
    /// A queue for processing unmanaged messages in a Burst-compatible job.
    /// </summary>
    /// <typeparam name="TMessage">The type of unmanaged message to process.</typeparam>
    public struct MessageQueue<TMessage> : IDisposable 
        where TMessage : unmanaged, IUnmanagedMessage
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
        /// Tries to dequeue a message from the queue.
        /// </summary>
        /// <param name="message">The dequeued message if successful.</param>
        /// <returns>True if a message was dequeued; otherwise, false.</returns>
        public bool TryDequeue(out TMessage message)
        {
            return Queue.TryDequeue(out message);
        }
        
        /// <summary>
        /// Gets the number of messages in the queue.
        /// </summary>
        public int Count => Queue.Count;
        
        /// <summary>
        /// Gets whether the queue is empty.
        /// </summary>
        public bool IsEmpty => Queue.Count == 0;
        
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
    /// A job for processing unmanaged messages in a Burst-compatible way.
    /// </summary>
    /// <typeparam name="TMessage">The type of unmanaged message to process.</typeparam>
    [BurstCompile]
    public struct ProcessMessagesJob<TMessage> : IJob 
        where TMessage : unmanaged, IUnmanagedMessage
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
        /// Function pointer for processing messages in a Burst-compatible way.
        /// </summary>
        public FunctionPointer<MessageProcessor<TMessage>> ProcessorFunction;
        
        /// <summary>
        /// Executes the job.
        /// </summary>
        public void Execute()
        {
            int count = 0;
            
            while (Queue.TryDequeue(out TMessage message))
            {
                // Process the message using the function pointer
                if (ProcessorFunction.IsCreated)
                {
                    ProcessorFunction.Invoke(ref message);
                }
                count++;
            }
            
            ProcessedCount.Value = count;
        }
    }
    
    /// <summary>
    /// Delegate for processing messages in Burst-compatible code.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to process.</typeparam>
    /// <param name="message">The message to process.</param>
    public delegate void MessageProcessor<TMessage>(ref TMessage message) 
        where TMessage : unmanaged, IUnmanagedMessage;
}