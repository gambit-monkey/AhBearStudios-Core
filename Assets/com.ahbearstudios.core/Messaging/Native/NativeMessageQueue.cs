using System;
using Unity.Collections;
using Unity.Jobs;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// FIFO queue for messages using Unity.Collections v2.
    /// Thread-safe and Burst-compatible for use with the Jobs system.
    /// </summary>
    /// <typeparam name="T">The type of message to store in the queue.</typeparam>
    [GenerateTestsForBurstCompatibility]
    public struct NativeMessageQueue<T> : IDisposable where T : unmanaged, IMessage
    {
        /// <summary>
        /// The capacity of the queue.
        /// </summary>
        public readonly int Capacity;
        
        [NativeDisableParallelForRestriction]
        private NativeArray<T> _items;
        
        [NativeDisableParallelForRestriction]
        private NativeAtomic<int> _head;
        
        [NativeDisableParallelForRestriction]
        private NativeAtomic<int> _tail;
        
        [NativeDisableParallelForRestriction]
        private NativeAtomic<int> _count;
        
        [NativeDisableParallelForRestriction]
        private NativeAtomic<int> _version;
        
        private readonly Allocator _allocator;
        
        /// <summary>
        /// Gets the current number of messages in the queue.
        /// </summary>
        public int Count => _count.Value;
        
        /// <summary>
        /// Gets whether the queue is full.
        /// </summary>
        public bool IsFull => Count >= Capacity;
        
        /// <summary>
        /// Gets whether the queue is empty.
        /// </summary>
        public bool IsEmpty => Count <= 0;
        
        /// <summary>
        /// Gets whether the queue has been allocated.
        /// </summary>
        public bool IsCreated => _items.IsCreated;

        /// <summary>
        /// Initializes a new instance of the NativeMessageQueue struct.
        /// </summary>
        /// <param name="capacity">The maximum number of messages the queue can hold.</param>
        /// <param name="allocator">The allocation type to use for the queue.</param>
        public NativeMessageQueue(int capacity, Allocator allocator)
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
            
            // Allocate the array and atomic counters
            _items = new NativeArray<T>(capacity, allocator, NativeArrayOptions.UninitializedMemory);
            _head = new NativeAtomic<int>(0, allocator);
            _tail = new NativeAtomic<int>(0, allocator);
            _count = new NativeAtomic<int>(0, allocator);
            _version = new NativeAtomic<int>(0, allocator);
        }

        /// <summary>
        /// Attempts to add a message to the end of the queue.
        /// </summary>
        /// <param name="message">The message to add.</param>
        /// <returns>True if the message was added, false if the queue was full.</returns>
        [GenerateTestsForBurstCompatibility]
        public bool TryEnqueue(T message)
        {
            // If the queue is full, fail
            if (IsFull)
            {
                return false;
            }

            // Add the message at the tail position
            _items[_tail.Value] = message;
            
            // Advance the tail
            _tail.Value = (_tail.Value + 1) % Capacity;
            
            // Increment the count and version
            _count.Value++;
            _version.Value++;
            
            return true;
        }

        /// <summary>
        /// Attempts to remove and return the message at the beginning of the queue.
        /// </summary>
        /// <param name="message">When this method returns, contains the message that was removed, if any.</param>
        /// <returns>True if a message was retrieved, false if the queue was empty.</returns>
        [GenerateTestsForBurstCompatibility]
        public bool TryDequeue(out T message)
        {
            // If the queue is empty, fail
            if (IsEmpty)
            {
                message = default;
                return false;
            }

            // Get the message at the head position
            message = _items[_head.Value];
            
            // Advance the head
            _head.Value = (_head.Value + 1) % Capacity;
            
            // Decrement the count and increment the version
            _count.Value--;
            _version.Value++;
            
            return true;
        }

        /// <summary>
        /// Attempts to peek at the message at the beginning of the queue without removing it.
        /// </summary>
        /// <param name="message">When this method returns, contains the message at the beginning of the queue, if any.</param>
        /// <returns>True if a message was peeked, false if the queue was empty.</returns>
        [GenerateTestsForBurstCompatibility]
        public bool TryPeek(out T message)
        {
            // If the queue is empty, fail
            if (IsEmpty)
            {
                message = default;
                return false;
            }

            // Get the message at the head position without advancing the head
            message = _items[_head.Value];
            return true;
        }

        /// <summary>
        /// Clears all messages from the queue.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public void Clear()
        {
            _head.Value = 0;
            _tail.Value = 0;
            _count.Value = 0;
            _version.Value++;
        }

        /// <summary>
        /// Disposes the queue and releases all resources.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public void Dispose()
        {
            if (_items.IsCreated)
            {
                _items.Dispose();
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
            
            if (_version.IsCreated)
            {
                _version.Dispose();
            }
        }

        /// <summary>
        /// Creates a job handle for the queue's disposal.
        /// </summary>
        /// <param name="inputDeps">The JobHandle that represents already scheduled dependencies.</param>
        /// <returns>A new JobHandle that includes the disposal job.</returns>
        [GenerateTestsForBurstCompatibility]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = _items.Dispose(inputDeps);
            jobHandle = _head.Dispose(jobHandle);
            jobHandle = _tail.Dispose(jobHandle);
            jobHandle = _count.Dispose(jobHandle);
            jobHandle = _version.Dispose(jobHandle);
            return jobHandle;
        }

        /// <summary>
        /// Gets an enumerator for iterating through the queue's messages.
        /// This does not remove messages from the queue.
        /// </summary>
        /// <returns>An enumerator for the queue's messages.</returns>
        [GenerateTestsForBurstCompatibility]
        public QueueEnumerator GetEnumerator()
        {
            return new QueueEnumerator(this);
        }

        /// <summary>
        /// Copies the contents of the queue to an array.
        /// </summary>
        /// <param name="array">The array to copy to.</param>
        /// <param name="arrayIndex">The index in the array at which to start copying.</param>
        [GenerateTestsForBurstCompatibility]
        public void CopyTo(NativeArray<T> array, int arrayIndex)
        {
            if (!array.IsCreated)
            {
                throw new ArgumentNullException(nameof(array));
            }
            
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index cannot be negative.");
            }
            
            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException("Destination array is not large enough.");
            }

            int count = Count;
            int head = _head.Value;
            
            for (int i = 0; i < count; i++)
            {
                array[arrayIndex + i] = _items[(head + i) % Capacity];
            }
        }

        /// <summary>
        /// Enumerator for iterating through the queue's messages.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public struct QueueEnumerator
        {
            private readonly NativeMessageQueue<T> _queue;
            private readonly int _version;
            private int _index;
            private int _currentIndex;
            private int _remainingCount;

            internal QueueEnumerator(NativeMessageQueue<T> queue)
            {
                _queue = queue;
                _version = queue._version.Value;
                _index = -1;
                _currentIndex = queue._head.Value;
                _remainingCount = queue.Count;
                Current = default;
            }

            /// <summary>
            /// Gets the current message.
            /// </summary>
            public T Current { get; private set; }

            /// <summary>
            /// Advances the enumerator to the next message.
            /// </summary>
            /// <returns>True if another message is available, false if the end of the queue has been reached.</returns>
            [GenerateTestsForBurstCompatibility]
            public bool MoveNext()
            {
                if (_version != _queue._version.Value)
                {
                    throw new InvalidOperationException("Queue was modified during enumeration.");
                }
                
                if (_remainingCount <= 0)
                {
                    _index = -1;
                    Current = default;
                    return false;
                }

                _index++;
                Current = _queue._items[_currentIndex];
                _currentIndex = (_currentIndex + 1) % _queue.Capacity;
                _remainingCount--;
                return true;
            }

            /// <summary>
            /// Resets the enumerator to the beginning of the queue.
            /// </summary>
            [GenerateTestsForBurstCompatibility]
            public void Reset()
            {
                if (_version != _queue._version.Value)
                {
                    throw new InvalidOperationException("Queue was modified during enumeration.");
                }
                
                _index = -1;
                _currentIndex = _queue._head.Value;
                _remainingCount = _queue.Count;
                Current = default;
            }
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
        /// <typeparam name="T">The type of message to store in the queue.</typeparam>
        /// <param name="capacity">The maximum number of messages the queue can hold.</param>
        /// <param name="allocator">The allocation type to use for the queue.</param>
        /// <returns>A new NativeMessageQueue instance.</returns>
        public static NativeMessageQueue<T> Create<T>(int capacity, Allocator allocator) 
            where T : unmanaged, IMessage
        {
            return new NativeMessageQueue<T>(capacity, allocator);
        }
    }
}