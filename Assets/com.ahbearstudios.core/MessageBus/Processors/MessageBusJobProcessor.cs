using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace AhBearStudios.Core.MessageBus.Processors
{
    /// <summary>
    /// Enterprise-grade, Burst-optimized job processor for the MessageBusService.
    /// Batches serialized message payloads off the main thread,
    /// then dispatches them via <see cref="CompleteAndDispatch"/>.
    /// </summary>
    public sealed class MessageBusJobProcessor : IMessageBusJobProcessor
    {
        private readonly NativeQueue<NativeArray<byte>> _queue;
        private readonly NativeList<NativeArray<byte>> _batchList;
        private JobHandle _jobHandle;
        private bool _disposed;

        /// <summary>
        /// Creates a new <see cref="MessageBusJobProcessor"/>.
        /// </summary>
        /// <param name="initialCapacity">
        /// Initial capacity for the internal queue and batch list. Must be >= 1.
        /// </param>
        public MessageBusJobProcessor(int initialCapacity)
        {
            if (initialCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be >= 1.");

            _queue = new NativeQueue<NativeArray<byte>>(Allocator.Persistent);
            _batchList = new NativeList<NativeArray<byte>>(initialCapacity, Allocator.Persistent);
            _jobHandle = default;
            _disposed = false;
        }

        /// <inheritdoc/>
        public void Enqueue(NativeArray<byte> payload)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MessageBusJobProcessor));
            if (!payload.IsCreated)
                throw new ArgumentException("Payload must be a valid NativeArray<byte>.", nameof(payload));

            _queue.Enqueue(payload);
        }

        /// <inheritdoc/>
        public JobHandle ScheduleProcessing(JobHandle dependency, int maxMessages)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MessageBusJobProcessor));
            if (maxMessages < 1)
                throw new ArgumentOutOfRangeException(nameof(maxMessages), "maxMessages must be >= 1.");

            // Complete any prior work to safely reuse the batch list
            dependency.Complete();
            _batchList.Clear();

            var job = new BatchMessageJob
            {
                Queue = _queue,
                BatchList = _batchList,
                MaxMessages = maxMessages
            };

            _jobHandle = job.Schedule(dependency);
            return _jobHandle;
        }

        /// <inheritdoc/>
        public void CompleteAndDispatch(Action<NativeArray<byte>> dispatcher)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MessageBusJobProcessor));
            if (dispatcher == null)
                throw new ArgumentNullException(nameof(dispatcher));

            // Wait for the job to finish
            _jobHandle.Complete();

            // Invoke dispatcher on each batched payload
            for (int i = 0, count = _batchList.Length; i < count; i++)
            {
                var arr = _batchList[i];
                dispatcher(arr);
                arr.Dispose();
            }

            _batchList.Clear();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;

            // Ensure any running job completes
            _jobHandle.Complete();

            // Dispose remaining queued arrays
            while (_queue.Count > 0)
            {
                var arr = _queue.Dequeue();
                arr.Dispose();
            }

            // Dispose any leftovers in the batch list
            for (int i = 0, count = _batchList.Length; i < count; i++)
                _batchList[i].Dispose();

            _batchList.Dispose();
            _queue.Dispose();
            _disposed = true;
        }

        [BurstCompile]
        private struct BatchMessageJob : IJob
        {
            public NativeQueue<NativeArray<byte>> Queue;
            public NativeList<NativeArray<byte>> BatchList;
            public int MaxMessages;

            public void Execute()
            {
                for (int i = 0; i < MaxMessages && Queue.Count > 0; i++)
                {
                    if (Queue.TryDequeue(out var msg))
                        BatchList.Add(msg);
                    else
                        break;
                }
            }
        }
    }
}