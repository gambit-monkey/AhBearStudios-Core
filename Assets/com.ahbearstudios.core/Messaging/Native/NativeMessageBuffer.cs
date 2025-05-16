using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Burst;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Thread-safe, Burst-compatible buffer for messages using Unity.Collections v2.
    /// Provides a fixed-size ring buffer for message storage and retrieval.
    /// </summary>
    /// <typeparam name="T">The type of message to store in the buffer.</typeparam>
    [GenerateTestsForBurstCompatibility]
    public struct NativeMessageBuffer<T> : IDisposable where T : unmanaged, IMessage
    {
        /// <summary>
        /// The capacity of the buffer.
        /// </summary>
        public readonly int Capacity;
        
        [NativeDisableParallelForRestriction]
        private NativeArray<T> _buffer;
        
        [NativeDisableParallelForRestriction]
        private NativeAtomic<int> _head;
        
        [NativeDisableParallelForRestriction]
        private NativeAtomic<int> _tail;
        
        [NativeDisableParallelForRestriction]
        private NativeAtomic<int> _count;
        
        private readonly Allocator _allocator;
        private readonly bool _overwriteWhenFull;
        
        /// <summary>
        /// Gets the current number of messages in the buffer.
        /// </summary>
        public int Count => _count.Value;
        
        /// <summary>
        /// Gets whether the buffer is full.
        /// </summary>
        public bool IsFull => Count >= Capacity;
        
        /// <summary>
        /// Gets whether the buffer is empty.
        /// </summary>
        public bool IsEmpty => Count <= 0;
        
        /// <summary>
        /// Gets whether the buffer has been allocated.
        /// </summary>
        public bool IsCreated => _buffer.IsCreated;

        /// <summary>
        /// Initializes a new instance of the NativeMessageBuffer struct.
        /// </summary>
        /// <param name="capacity">The maximum number of messages the buffer can hold.</param>
        /// <param name="allocator">The allocation type to use for the buffer.</param>
        /// <param name="overwriteWhenFull">Whether to overwrite oldest messages when the buffer is full.</param>
        public NativeMessageBuffer(int capacity, Allocator allocator, bool overwriteWhenFull = false)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
            }
            
            if (allocator <= Allocator.None)
            {
                throw new ArgumentException("Invalid allocator type.", nameof(allocator));
            }

            Capacity = capacity;
            _allocator = allocator;
            _overwriteWhenFull = overwriteWhenFull;
            
            // Allocate the buffer and atomic counters
            _buffer = new NativeArray<T>(capacity, allocator, NativeArrayOptions.UninitializedMemory);
            _head = new NativeAtomic<int>(0, allocator);
            _tail = new NativeAtomic<int>(0, allocator);
            _count = new NativeAtomic<int>(0, allocator);
        }

        /// <summary>
        /// Attempts to add a message to the buffer.
        /// </summary>
        /// <param name="message">The message to add.</param>
        /// <returns>True if the message was added, false if the buffer was full and overwriting is disabled.</returns>
        [GenerateTestsForBurstCompatibility]
        public bool TryEnqueue(T message)
        {
            // If the buffer is full and we're not overwriting, fail
            if (IsFull && !_overwriteWhenFull)
            {
                return false;
            }

            // If the buffer is full and we are overwriting, advance the head to remove the oldest message
            if (IsFull && _overwriteWhenFull)
            {
                _head.Value = (_head.Value + 1) % Capacity;
                _count.Value--;
            }

            // Add the message at the tail position
            _buffer[_tail.Value] = message;
            
            // Advance the tail
            _tail.Value = (_tail.Value + 1) % Capacity;
            
            // Increment the count
            _count.Value++;
            
            return true;
        }

        /// <summary>
        /// Attempts to remove and return the oldest message from the buffer.
        /// </summary>
        /// <param name="message">When this method returns, contains the message that was removed, if any.</param>
        /// <returns>True if a message was retrieved, false if the buffer was empty.</returns>
        [GenerateTestsForBurstCompatibility]
        public bool TryDequeue(out T message)
        {
            // If the buffer is empty, fail
            if (IsEmpty)
            {
                message = default;
                return false;
            }

            // Get the message at the head position
            message = _buffer[_head.Value];
            
            // Advance the head
            _head.Value = (_head.Value + 1) % Capacity;
            
            // Decrement the count
            _count.Value--;
            
            return true;
        }

        /// <summary>
        /// Attempts to peek at the oldest message without removing it.
        /// </summary>
        /// <param name="message">When this method returns, contains the oldest message, if any.</param>
        /// <returns>True if a message was peeked, false if the buffer was empty.</returns>
        [GenerateTestsForBurstCompatibility]
        public bool TryPeek(out T message)
        {
            // If the buffer is empty, fail
            if (IsEmpty)
            {
                message = default;
                return false;
            }

            // Get the message at the head position without advancing the head
            message = _buffer[_head.Value];
            return true;
        }

        /// <summary>
        /// Clears all messages from the buffer.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public void Clear()
        {
            _head.Value = 0;
            _tail.Value = 0;
            _count.Value = 0;
        }

        /// <summary>
        /// Disposes the buffer and releases all resources.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public void Dispose()
        {
            if (_buffer.IsCreated)
            {
                _buffer.Dispose();
            }
            
            if (_head.IsCreated)
            {
                _head.Dispose();
            }
            
            if (_tail.IsCreated)
            {
                _tail.Dispose();
            }
            
            if (_count.IsCreated)
            {
                _count.Dispose();
            }
        }

        /// <summary>
        /// Creates a job handle for the buffer's disposal.
        /// </summary>
        /// <param name="inputDeps">The JobHandle that represents already scheduled dependencies.</param>
        /// <returns>A new JobHandle that includes the disposal job.</returns>
        [GenerateTestsForBurstCompatibility]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = _buffer.Dispose(inputDeps);
            jobHandle = _head.Dispose(jobHandle);
            jobHandle = _tail.Dispose(jobHandle);
            jobHandle = _count.Dispose(jobHandle);
            return jobHandle;
        }

        /// <summary>
        /// Gets an enumerator for iterating through the buffer's messages.
        /// This does not remove messages from the buffer.
        /// </summary>
        /// <returns>An enumerator for the buffer's messages.</returns>
        [GenerateTestsForBurstCompatibility]
        public BufferEnumerator GetEnumerator()
        {
            return new BufferEnumerator(this);
        }

        /// <summary>
        /// Enumerator for iterating through the buffer's messages.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public struct BufferEnumerator
        {
            private readonly NativeMessageBuffer<T> _buffer;
            private int _currentIndex;
            private int _remainingCount;

            internal BufferEnumerator(NativeMessageBuffer<T> buffer)
            {
                _buffer = buffer;
                _currentIndex = buffer._head.Value;
                _remainingCount = buffer.Count;
                Current = default;
            }

            /// <summary>
            /// Gets the current message.
            /// </summary>
            public T Current { get; private set; }

            /// <summary>
            /// Advances the enumerator to the next message.
            /// </summary>
            /// <returns>True if another message is available, false if the end of the buffer has been reached.</returns>
            [GenerateTestsForBurstCompatibility]
            public bool MoveNext()
            {
                if (_remainingCount <= 0)
                {
                    Current = default;
                    return false;
                }

                Current = _buffer._buffer[_currentIndex];
                _currentIndex = (_currentIndex + 1) % _buffer.Capacity;
                _remainingCount--;
                return true;
            }

            /// <summary>
            /// Resets the enumerator to the beginning of the buffer.
            /// </summary>
            [GenerateTestsForBurstCompatibility]
            public void Reset()
            {
                _currentIndex = _buffer._head.Value;
                _remainingCount = _buffer.Count;
                Current = default;
            }
        }
    }

    /// <summary>
    /// Non-generic utility class for NativeMessageBuffer operations.
    /// </summary>
    public static class NativeMessageBuffer
    {
        /// <summary>
        /// Creates a new NativeMessageBuffer of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of message to store in the buffer.</typeparam>
        /// <param name="capacity">The maximum number of messages the buffer can hold.</param>
        /// <param name="allocator">The allocation type to use for the buffer.</param>
        /// <param name="overwriteWhenFull">Whether to overwrite oldest messages when the buffer is full.</param>
        /// <returns>A new NativeMessageBuffer instance.</returns>
        public static NativeMessageBuffer<T> Create<T>(int capacity, Allocator allocator, bool overwriteWhenFull = false) 
            where T : unmanaged, IMessage
        {
            return new NativeMessageBuffer<T>(capacity, allocator, overwriteWhenFull);
        }
    }
}