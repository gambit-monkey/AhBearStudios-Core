using System;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// FIFO queue for messages using Unity.Collections v2.
    /// Thread-safe and Burst-compatible for use with the Jobs system.
    /// </summary>
    /// <typeparam name="T">The type of message to store in the queue.</typeparam>
    [BurstCompile]
    public struct NativeMessageQueue<T> : IDisposable where T : unmanaged, IMessage
    {
        /// <summary>
        /// Configuration parameters for the queue
        /// </summary>
        public readonly struct Config
        {
            public readonly int Capacity;
            public readonly Allocator Allocator;

            public Config(int capacity, Allocator allocator)
            {
                Capacity = capacity;
                Allocator = allocator;
            }
        }

        private readonly Config _config;
        private NativeArray<T> _items;
        private SharedStatic<int> _head;
        private SharedStatic<int> _tail;
        private SharedStatic<int> _count;
        private SharedStatic<int> _version;
        private struct HeadKey {}
        private struct TailKey {}
        private struct CountKey {}
        private struct VersionKey {}
        private volatile bool _isCreated;

        /// <summary>
        /// Gets the current number of messages in the queue.
        /// </summary>
        public readonly int Count => _count.Data;

        /// <summary>
        /// Gets whether the queue is full.
        /// </summary>
        public readonly bool IsFull => Count >= _config.Capacity;

        /// <summary>
        /// Gets whether the queue is empty.
        /// </summary>
        public readonly bool IsEmpty => Count <= 0;

        /// <summary>
        /// Gets whether the queue has been allocated.
        /// </summary>
        public readonly bool IsCreated => _isCreated && _items.IsCreated;

        /// <summary>
        /// Gets the capacity of the queue.
        /// </summary>
        public readonly int Capacity => _config.Capacity;

        /// <summary>
        /// Initializes a new instance of the NativeMessageQueue struct.
        /// </summary>
        public NativeMessageQueue(Config config)
        {
            ValidateConfig(config);

            _config = config;
            _items = new NativeArray<T>(config.Capacity, config.Allocator, NativeArrayOptions.ClearMemory);
    
            // Create unique hash values for each SharedStatic field
            _head = SharedStatic<int>.GetOrCreate<NativeMessageQueue<T>, HeadKey>();
            _tail = SharedStatic<int>.GetOrCreate<NativeMessageQueue<T>, TailKey>();
            _count = SharedStatic<int>.GetOrCreate<NativeMessageQueue<T>, CountKey>();
            _version = SharedStatic<int>.GetOrCreate<NativeMessageQueue<T>, VersionKey>();
            _isCreated = true;

            _head.Data = 0;
            _tail.Data = 0;
            _count.Data = 0;
            _version.Data = 0;
        }


        [BurstCompile]
        private static void ValidateConfig(Config config)
        {
            if (config.Capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(config.Capacity), "Capacity must be greater than zero.");

            if (config.Allocator <= Allocator.None)
                throw new ArgumentException("Invalid allocator type.", nameof(config.Allocator));
        }

        /// <summary>
        /// Attempts to add a message to the end of the queue.
        /// </summary>
        [BurstCompile]
        public bool TryEnqueue(in T message)
        {
            CheckDisposed();

            if (IsFull)
                return false;

            var tailIndex = _tail.Data % _config.Capacity;
            _items[tailIndex] = message;
            
            Interlocked.Increment(ref _tail.Data);
            Interlocked.Increment(ref _count.Data);
            Interlocked.Increment(ref _version.Data);

            return true;
        }

        /// <summary>
        /// Attempts to remove and return the message at the beginning of the queue.
        /// </summary>
        [BurstCompile]
        public bool TryDequeue(out T message)
        {
            CheckDisposed();

            if (IsEmpty)
            {
                message = default;
                return false;
            }

            var headIndex = _head.Data % _config.Capacity;
            message = _items[headIndex];

            Interlocked.Increment(ref _head.Data);
            Interlocked.Decrement(ref _count.Data);
            Interlocked.Increment(ref _version.Data);

            return true;
        }

        /// <summary>
        /// Attempts to peek at the message at the beginning of the queue without removing it.
        /// </summary>
        [BurstCompile]
        public bool TryPeek(out T message)
        {
            CheckDisposed();

            if (IsEmpty)
            {
                message = default;
                return false;
            }

            message = _items[_head.Data % _config.Capacity];
            return true;
        }

        /// <summary>
        /// Clears all messages from the queue.
        /// </summary>
        [BurstCompile]
        public void Clear()
        {
            CheckDisposed();
            _head.Data = 0;
            _tail.Data = 0;
            _count.Data = 0;
            Interlocked.Increment(ref _version.Data);
        }

        /// <summary>
        /// Disposes the queue and releases all resources.
        /// </summary>
        [BurstCompile]
        public void Dispose()
        {
            if (!_isCreated) return;
            
            if (_items.IsCreated)
                _items.Dispose();

            _isCreated = false;
        }

        /// <summary>
        /// Creates a job handle for the queue's disposal.
        /// </summary>
        public JobHandle Dispose(JobHandle inputDeps) => _items.Dispose(inputDeps);

        /// <summary>
        /// Copies the contents of the queue to an array.
        /// </summary>
        [BurstCompile]
        public void CopyTo(NativeArray<T> array, int arrayIndex)
        {
            if (!array.IsCreated)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Destination array is not large enough.");

            int count = Count;
            int head = _head.Data;

            for (int i = 0; i < count; i++)
                array[arrayIndex + i] = _items[(head + i) % Capacity];
        }

        private void CheckDisposed()
        {
            if (!IsCreated)
                throw new ObjectDisposedException(nameof(NativeMessageQueue<T>));
        }
    }

    /// <summary>
    /// Non-generic utility class for NativeMessageQueue operations.
    /// </summary>
    public static class NativeMessageQueue
    {
        /// <summary>
        /// Creates a new NativeMessageQueue of the specified type.
        /// </summary>
        public static NativeMessageQueue<T> Create<T>(int capacity, Allocator allocator) 
            where T : unmanaged, IMessage
        {
            var config = new NativeMessageQueue<T>.Config(capacity, allocator);
            return new NativeMessageQueue<T>(config);
        }
    }
}