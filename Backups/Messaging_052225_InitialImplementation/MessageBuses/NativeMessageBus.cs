using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Extensions;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Burst-compatible message bus implementation for use with Unity's DOTS/Jobs system.
    /// Provides thread-safe messaging for jobs and native code.
    /// </summary>
    /// <typeparam name="T">The type of message to handle.</typeparam>
    public class NativeMessageBus<T> : INativeMessageBus<T>, IDisposable where T : unmanaged, IMessage
    {
        private readonly NativeMessageQueue<T> _messageQueue;
        private readonly Dictionary<int, NativeList<SubscriptionHandle>> _typeSubscriptions;
        private readonly NativeList<SubscriptionHandle> _globalSubscriptions;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly int _capacity;
        private readonly Allocator _allocator;
        private readonly object _subscriptionLock = new object();
        private int _nextHandleId;
        private bool _isDisposed;

        /// <summary>
        /// Gets the number of pending messages in the bus.
        /// </summary>
        public int PendingMessageCount => _messageQueue.Count;

        /// <summary>
        /// Initializes a new instance of the NativeMessageBus class.
        /// </summary>
        /// <param name="capacity">The capacity of the message queue.</param>
        /// <param name="allocator">The allocator to use for native containers.</param>
        /// <param name="logger">Optional logger for message bus operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public NativeMessageBus(int capacity, Allocator allocator, IBurstLogger logger = null, IProfiler profiler = null)
        {
            if (allocator != Allocator.Persistent && allocator != Allocator.TempJob)
            {
                throw new ArgumentException("Only Persistent and TempJob allocators are supported for NativeMessageBus.", nameof(allocator));
            }

            _capacity = capacity;
            _allocator = allocator;
            _logger = logger;
            _profiler = profiler;
            
            _messageQueue = new NativeMessageQueue<T>(capacity, allocator);
            _typeSubscriptions = new Dictionary<int, NativeList<SubscriptionHandle>>();
            _globalSubscriptions = new NativeList<SubscriptionHandle>(16, allocator);
            _nextHandleId = 1;
            _isDisposed = false;
            
            if (_logger != null)
            {
                _logger.Info($"NativeMessageBus initialized with capacity {capacity}");
            }
        }

        /// <inheritdoc/>
        public SubscriptionHandle Subscribe(int messageTypeId, FunctionPointer<MessageHandler> handler)
        {
            using (_profiler?.BeginSample("NativeMessageBus.Subscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(NativeMessageBus<T>));
                }

                if (!handler.IsCreated)
                {
                    throw new ArgumentException("Handler function pointer is not valid.", nameof(handler));
                }

                lock (_subscriptionLock)
                {
                    // Create a new subscription handle
                    var handle = new SubscriptionHandle
                    {
                        Id = _nextHandleId++,
                        MessageTypeId = messageTypeId,
                        Handler = handler,
                        IsActive = true
                    };

                    // Add the subscription to the appropriate collection
                    if (messageTypeId > 0)
                    {
                        // Type-specific subscription
                        if (!_typeSubscriptions.TryGetValue(messageTypeId, out NativeList<SubscriptionHandle> subscriptions))
                        {
                            subscriptions = new NativeList<SubscriptionHandle>(16, _allocator);
                            _typeSubscriptions[messageTypeId] = subscriptions;
                        }

                        subscriptions.Add(handle);
                        
                        if (_logger != null)
                        {
                            _logger.Debug($"Subscribed to message type {messageTypeId} with handle {handle.Id}");
                        }
                    }
                    else
                    {
                        // Global subscription (receives all messages)
                        _globalSubscriptions.Add(handle);
                        
                        if (_logger != null)
                        {
                            _logger.Debug($"Subscribed to all messages with handle {handle.Id}");
                        }
                    }

                    return handle;
                }
            }
        }

        /// <inheritdoc/>
        public bool Unsubscribe(SubscriptionHandle handle)
        {
            using (_profiler?.BeginSample("NativeMessageBus.Unsubscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(NativeMessageBus<T>));
                }

                if (handle.Id <= 0)
                {
                    return false;
                }

                lock (_subscriptionLock)
                {
                    bool removed = false;

                    if (handle.MessageTypeId > 0)
                    {
                        // Type-specific subscription
                        if (_typeSubscriptions.TryGetValue(handle.MessageTypeId, out NativeList<SubscriptionHandle> subscriptions))
                        {
                            // Find and remove the subscription
                            for (int i = 0; i < subscriptions.Length; i++)
                            {
                                if (subscriptions[i].Id == handle.Id)
                                {
                                    subscriptions.RemoveAt(i);
                                    removed = true;
                                    
                                    if (_logger != null)
                                    {
                                        _logger.Debug($"Unsubscribed from message type {handle.MessageTypeId} with handle {handle.Id}");
                                    }
                                    
                                    break;
                                }
                            }

                            // If the list is empty, remove it from the dictionary
                            if (subscriptions.Length == 0)
                            {
                                subscriptions.Dispose();
                                _typeSubscriptions.Remove(handle.MessageTypeId);
                            }
                        }
                    }
                    else
                    {
                        // Global subscription
                        for (int i = 0; i < _globalSubscriptions.Length; i++)
                        {
                            if (_globalSubscriptions[i].Id == handle.Id)
                            {
                                _globalSubscriptions.RemoveAt(i);
                                removed = true;
                                
                                if (_logger != null)
                                {
                                    _logger.Debug($"Unsubscribed from all messages with handle {handle.Id}");
                                }
                                
                                break;
                            }
                        }
                    }

                    return removed;
                }
            }
        }

        /// <inheritdoc/>
        public bool Publish(T message)
        {
            using (_profiler?.BeginSample("NativeMessageBus.Publish"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(NativeMessageBus<T>));
                }

                bool success = _messageQueue.TryEnqueue(message);
                
                if (!success && _logger != null)
                {
                    _logger.Warning($"Failed to publish message: Queue is full (capacity: {_capacity})");
                }
                
                return success;
            }
        }

        /// <inheritdoc/>
        public int ProcessMessages(int maxMessages = 100)
        {
            using (_profiler?.BeginSample("NativeMessageBus.ProcessMessages"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(NativeMessageBus<T>));
                }

                int messagesProcessed = 0;
                
                // Process up to maxMessages messages
                for (int i = 0; i < maxMessages && !_messageQueue.IsEmpty; i++)
                {
                    if (_messageQueue.TryDequeue(out T message))
                    {
                        DeliverMessageToSubscribers(message);
                        messagesProcessed++;
                    }
                }
                
                if (_logger != null && messagesProcessed > 0)
                {
                    _logger.Debug($"Processed {messagesProcessed} messages");
                }
                
                return messagesProcessed;
            }
        }

        /// <inheritdoc/>
        public JobHandle ScheduleMessageProcessing(int maxMessages = 100, JobHandle dependsOn = default)
        {
            using (_profiler?.BeginSample("NativeMessageBus.ScheduleMessageProcessing"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(NativeMessageBus<T>));
                }

                // Create a temporary queue for the results (we'll process them on the main thread)
                var processedMessages = new NativeArray<T>(maxMessages, Allocator.TempJob);
                var processedCount = new NativeAtomic<int>(0, Allocator.TempJob);
                
                // Create a job to dequeue messages
                var job = new DequeueMessagesJob
                {
                    SourceQueue = _messageQueue,
                    ProcessedMessages = processedMessages,
                    ProcessedCount = processedCount,
                    MaxMessages = maxMessages
                };
                
                // Schedule the job
                var jobHandle = job.Schedule(dependsOn);
                
                // Create a continuation job to deliver the messages to subscribers
                var deliveryJob = new DeliverMessagesJob
                {
                    Bus = this,
                    ProcessedMessages = processedMessages,
                    ProcessedCount = processedCount
                };
                
                // Schedule the delivery job to run after the dequeue job
                var deliveryHandle = deliveryJob.Schedule(jobHandle);
                
                // Create a final job to cleanup resources
                var cleanupJob = new CleanupJob
                {
                    ProcessedMessages = processedMessages,
                    ProcessedCount = processedCount
                };
                
                // Schedule the cleanup job to run after the delivery job
                var cleanupHandle = cleanupJob.Schedule(deliveryHandle);
                
                return cleanupHandle;
            }
        }

        private void DeliverMessageToSubscribers(T message)
        {
            using (_profiler?.BeginSample("NativeMessageBus.DeliverMessageToSubscribers"))
            {
                int messageTypeId = message.GetTypeId();
                
                lock (_subscriptionLock)
                {
                    // Deliver to type-specific subscribers
                    if (_typeSubscriptions.TryGetValue(messageTypeId, out NativeList<SubscriptionHandle> typeSubscriptions))
                    {
                        foreach (var subscription in typeSubscriptions)
                        {
                            if (subscription.IsActive)
                            {
                                try
                                {
                                    subscription.Handler.Invoke(message);
                                }
                                catch (Exception ex)
                                {
                                    if (_logger != null)
                                    {
                                        _logger.Error($"Error in message handler for type {messageTypeId}: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                    
                    // Deliver to global subscribers
                    foreach (var subscription in _globalSubscriptions)
                    {
                        if (subscription.IsActive)
                        {
                            try
                            {
                                subscription.Handler.Invoke(message);
                            }
                            catch (Exception ex)
                            {
                                if (_logger != null)
                                {
                                    _logger.Error($"Error in global message handler: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            using (_profiler?.BeginSample("NativeMessageBus.Dispose"))
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Releases resources used by the message bus.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose the message queue
                if (_messageQueue.IsCreated)
                {
                    _messageQueue.Dispose();
                }
                
                // Dispose subscription lists
                lock (_subscriptionLock)
                {
                    foreach (var subscriptions in _typeSubscriptions.Values)
                    {
                        if (subscriptions.IsCreated)
                        {
                            subscriptions.Dispose();
                        }
                    }
                    
                    _typeSubscriptions.Clear();
                    
                    if (_globalSubscriptions.IsCreated)
                    {
                        _globalSubscriptions.Dispose();
                    }
                }
                
                if (_logger != null)
                {
                    _logger.Debug("NativeMessageBus disposed");
                }
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~NativeMessageBus()
        {
            Dispose(false);
        }

        /// <summary>
        /// Job for dequeuing messages from the source queue.
        /// </summary>
        [BurstCompile]
        private struct DequeueMessagesJob : IJob
        {
            public NativeMessageQueue<T> SourceQueue;
            public NativeArray<T> ProcessedMessages;
            public NativeAtomic<int> ProcessedCount;
            public int MaxMessages;

            [BurstCompile]
            public void Execute()
            {
                int count = 0;
                
                while (count < MaxMessages && SourceQueue.TryDequeue(out T message))
                {
                    ProcessedMessages[count] = message;
                    count++;
                }
                
                ProcessedCount.Value = count;
            }
        }

        /// <summary>
        /// Job for delivering messages to subscribers.
        /// </summary>
        private struct DeliverMessagesJob : IJob
        {
            [NativeDisableParallelForRestriction]
            public NativeMessageBus<T> Bus;
            
            [ReadOnly]
            public NativeArray<T> ProcessedMessages;
            
            [ReadOnly]
            public NativeAtomic<int> ProcessedCount;

            public void Execute()
            {
                int count = ProcessedCount.Value;
                
                for (int i = 0; i < count; i++)
                {
                    Bus.DeliverMessageToSubscribers(ProcessedMessages[i]);
                }
            }
        }

        /// <summary>
        /// Job for cleaning up temporary resources.
        /// </summary>
        [BurstCompile]
        private struct CleanupJob : IJob
        {
            [DeallocateOnJobCompletion]
            public NativeArray<T> ProcessedMessages;
            
            [DeallocateOnJobCompletion]
            public NativeAtomic<int> ProcessedCount;

            [BurstCompile]
            public void Execute()
            {
                // Resources will be automatically deallocated thanks to the DeallocateOnJobCompletion attribute
            }
        }
    }
}