using System;
using Unity.Collections;
using Unity.Jobs;

namespace AhBearStudios.Core.MessageBus.Processors
{
    /// <summary>
    /// Defines a Job System–based processor for batching and dispatching serialized message payloads.
    /// </summary>
    public interface IMessageBusJobProcessor : IDisposable
    {
        /// <summary>
        /// Enqueues a serialized message payload for later batch processing.
        /// Ownership of the <paramref name="payload"/> NativeArray is transferred to the processor.
        /// </summary>
        /// <param name="payload">A <see cref="NativeArray{T}"/> containing the serialized message.</param>
        void Enqueue(NativeArray<byte> payload);

        /// <summary>
        /// Schedules a Burst-compiled job to dequeue up to <paramref name="maxMessages"/>
        /// from the internal queue into an internal batch list.
        /// </summary>
        /// <param name="dependency">A <see cref="JobHandle"/> the scheduled job will depend on.</param>
        /// <param name="maxMessages">Maximum number of messages to process in this batch (>=1).</param>
        /// <returns>A new <see cref="JobHandle"/> representing the scheduled job.</returns>
        JobHandle ScheduleProcessing(JobHandle dependency, int maxMessages);

        /// <summary>
        /// Completes any scheduled job and invokes <paramref name="dispatcher"/>
        /// on each batched payload. Disposes the batch entries after dispatch.
        /// </summary>
        /// <param name="dispatcher">
        /// Callback invoked for every <see cref="NativeArray{Byte}"/> payload.
        /// The processor will dispose each array after dispatch.
        /// </param>
        void CompleteAndDispatch(Action<NativeArray<byte>> dispatcher);
    }
}