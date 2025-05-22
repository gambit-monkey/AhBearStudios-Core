using System;
using System.Threading;
using AhBearStudios.Core.Messaging.Interfaces;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Thread-safe, Burst-compatible buffer for messages using Unity.Collections v2.
    /// Provides a fixed-size ring buffer for message storage and retrieval.
    /// </summary>
    /// <typeparam name="T">The type of message to store in the buffer.</typeparam>
    [BurstCompile]
    public struct NativeMessageBuffer<T> : IDisposable where T : unmanaged, IMessage
    {
        /// <summary>
        /// Configuration parameters for the buffer
        /// </summary>
        public readonly struct Config
        {
            public readonly int Capacity;
            public readonly bool OverwriteWhenFull;
            public readonly Allocator Allocator;

            public Config(int capacity, Allocator allocator, bool overwriteWhenFull = false)
            {
                Capacity = capacity;
                Allocator = allocator;
                OverwriteWhenFull = overwriteWhenFull;
            }
        }

        private readonly Config _config;
        private NativeArray<T> _buffer;
        private SharedStatic<int> _head;
        private SharedStatic<int> _tail;
        private SharedStatic<int> _count;
        private volatile bool _isCreated;

        /// <summary>
        /// Gets the current number of messages in the buffer.
        /// </summary>
        public readonly int Count => _count.Data;

        /// <summary>
        /// Gets whether the buffer is full.
        /// </summary>
        public readonly bool IsFull => Count >= _config.Capacity;

        /// <summary>
        /// Gets whether the buffer is empty.
        /// </summary>
        public readonly bool IsEmpty => Count <= 0;

        /// <summary>
        /// Gets whether the buffer has been allocated.
        /// </summary>
        public readonly bool IsCreated => _isCreated && _buffer.IsCreated;

        /// <summary>
        /// Gets the capacity of the buffer.
        /// </summary>
        public readonly int Capacity => _config.Capacity;

        /// <summary>
        /// Initializes a new instance of the NativeMessageBuffer struct.
        /// </summary>
        public NativeMessageBuffer(Config config)
        {
            ValidateConfig(config);

            _config = config;
            _buffer = new NativeArray<T>(config.Capacity, config.Allocator, NativeArrayOptions.ClearMemory);
            _head = SharedStatic<int>.GetOrCreate<NativeMessageBuffer<T>>();
            _tail = SharedStatic<int>.GetOrCreate<NativeMessageBuffer<T>>();
            _count = SharedStatic<int>.GetOrCreate<NativeMessageBuffer<T>>();
            _isCreated = true;

            _head.Data = 0;
            _tail.Data = 0;
            _count.Data = 0;
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
        /// Attempts to add a message to the buffer.
        /// </summary>
        [BurstCompile]
        public bool TryEnqueue(in T message)
        {
            CheckDisposed();

            if (IsFull && !_config.OverwriteWhenFull)
                return false;

            if (IsFull)
            {
                Interlocked.Increment(ref _head.Data);
                Interlocked.Decrement(ref _count.Data);
            }

            var tailIndex = _tail.Data % _config.Capacity;
            _buffer[tailIndex] = message;
            Interlocked.Increment(ref _tail.Data);
            Interlocked.Increment(ref _count.Data);

            return true;
        }

        /// <summary>
        /// Attempts to remove and return the oldest message from the buffer.
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
            message = _buffer[headIndex];
            Interlocked.Increment(ref _head.Data);
            Interlocked.Decrement(ref _count.Data);

            return true;
        }

        /// <summary>
        /// Attempts to peek at the oldest message without removing it.
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

            message = _buffer[_head.Data % _config.Capacity];
            return true;
        }

        /// <summary>
        /// Clears all messages from the buffer.
        /// </summary>
        [BurstCompile]
        public void Clear()
        {
            CheckDisposed();
            _head.Data = 0;
            _tail.Data = 0;
            _count.Data = 0;
        }

        /// <summary>
        /// Disposes the buffer and releases all resources.
        /// </summary>
        [BurstCompile]
        public void Dispose()
        {
            if (!_isCreated) return;
            
            if (_buffer.IsCreated)
                _buffer.Dispose();

            _isCreated = false;
        }

        /// <summary>
        /// Creates a job handle for the buffer's disposal.
        /// </summary>
        public JobHandle Dispose(JobHandle inputDeps) => _buffer.Dispose(inputDeps);

        private void CheckDisposed()
        {
            if (!IsCreated)
                throw new ObjectDisposedException(nameof(NativeMessageBuffer<T>));
        }
    }
}