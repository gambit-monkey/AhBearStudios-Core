using AhBearStudios.Core.MessageBus.Interfaces;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace AhBearStudios.Core.MessageBus.Jobs
{
    /// <summary>
    /// A parallel job for processing unmanaged messages in a Burst-compatible way.
    /// </summary>
    /// <typeparam name="TMessage">The type of unmanaged message to process.</typeparam>
    [BurstCompile]
    public struct ProcessMessagesParallelJob<TMessage> : IJobParallelFor 
        where TMessage : unmanaged, IUnmanagedMessage
    {
        /// <summary>
        /// The array of messages to process.
        /// </summary>
        [ReadOnly]
        public NativeArray<TMessage> Messages;
        
        /// <summary>
        /// Function pointer for processing messages in a Burst-compatible way.
        /// </summary>
        [ReadOnly]
        public FunctionPointer<MessageProcessor<TMessage>> ProcessorFunction;
        
        /// <summary>
        /// Results array for storing processing results.
        /// </summary>
        [WriteOnly]
        public NativeArray<int> Results;
        
        /// <summary>
        /// Executes the job for a specific index.
        /// </summary>
        /// <param name="index">The index of the message to process.</param>
        public void Execute(int index)
        {
            var message = Messages[index];
            
            if (ProcessorFunction.IsCreated)
            {
                ProcessorFunction.Invoke(ref message);
                Results[index] = 1; // Mark as processed
            }
            else
            {
                Results[index] = 0; // Mark as not processed
            }
        }
    }
}