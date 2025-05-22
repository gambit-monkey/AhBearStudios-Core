using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Burst;
using Unity.Profiling;

namespace AhBearStudios.Core.Messaging.Jobs
{
    /// <summary>
    /// Processor for handling batches of messages, supporting both managed and unmanaged types.
    /// </summary>
    public sealed class MessageBatchProcessor : IDisposable
    {
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly Dictionary<Type, object> _processors = new Dictionary<Type, object>();
        private bool _isDisposed;
        
        /// <summary>
        /// Initializes a new instance of the MessageBatchProcessor class.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        public MessageBatchProcessor(IBurstLogger logger, IProfiler profiler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
        }
        
        /// <summary>
        /// Registers a processor for unmanaged messages that can be processed in Burst-compiled jobs.
        /// </summary>
        /// <typeparam name="TMessage">The type of unmanaged message.</typeparam>
        /// <param name="processor">The processor function pointer.</param>
        public void RegisterUnmanagedProcessor<TMessage>(FunctionPointer<MessageProcessor<TMessage>> processor)
            where TMessage : unmanaged, IUnmanagedMessage
        {
            _processors[typeof(TMessage)] = processor;
            _logger.Log(LogLevel.Debug, 
                $"Registered unmanaged processor for message type {typeof(TMessage).Name}",
                "MessageBatchProcessor");
        }
        
        /// <summary>
        /// Registers a processor for managed messages.
        /// </summary>
        /// <typeparam name="TMessage">The type of managed message.</typeparam>
        /// <param name="processor">The processor action.</param>
        public void RegisterManagedProcessor<TMessage>(Action<TMessage> processor)
            where TMessage : IMessage
        {
            _processors[typeof(TMessage)] = processor;
            _logger.Log(LogLevel.Debug, 
                $"Registered managed processor for message type {typeof(TMessage).Name}",
                "MessageBatchProcessor");
        }
        
        /// <summary>
        /// Processes a batch of unmanaged messages using a Burst-compiled job.
        /// </summary>
        /// <typeparam name="TMessage">The type of unmanaged message.</typeparam>
        /// <param name="messages">The messages to process.</param>
        /// <param name="allocator">The allocator to use for temporary collections.</param>
        /// <returns>A JobHandle for the processing job.</returns>
        public JobHandle ProcessUnmanagedBatch<TMessage>(IEnumerable<TMessage> messages, Allocator allocator = Allocator.TempJob)
            where TMessage : unmanaged, IUnmanagedMessage
        {
            using var scope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("MessageBatchProcessor"), $"ProcessUnmanaged_{typeof(TMessage).Name}"));
            
            if (!_processors.TryGetValue(typeof(TMessage), out var processorObj) || 
                !(processorObj is FunctionPointer<MessageProcessor<TMessage>> processor))
            {
                _logger.Log(LogLevel.Warning, 
                    $"No processor registered for unmanaged message type {typeof(TMessage).Name}",
                    "MessageBatchProcessor");
                return default;
            }
            
            var messageList = new List<TMessage>(messages);
            if (messageList.Count == 0)
            {
                return default;
            }
            
            var messageArray = new NativeArray<TMessage>(messageList.ToArray(), allocator);
            var resultsArray = new NativeArray<int>(messageList.Count, allocator);
            
            var job = new ProcessMessagesParallelJob<TMessage>
            {
                Messages = messageArray,
                ProcessorFunction = processor,
                Results = resultsArray
            };
            
            var jobHandle = job.Schedule(messageList.Count, 32);
            
            // Schedule cleanup job
            var cleanupJob = new CleanupArraysJob<TMessage>
            {
                MessageArray = messageArray,
                ResultsArray = resultsArray
            };
            
            var cleanupHandle = cleanupJob.Schedule(jobHandle);
            
            _logger.Log(LogLevel.Debug, 
                $"Scheduled processing of {messageList.Count} unmanaged messages of type {typeof(TMessage).Name}",
                "MessageBatchProcessor");
            
            return cleanupHandle;
        }
        
        /// <summary>
        /// Processes a batch of managed messages synchronously.
        /// </summary>
        /// <typeparam name="TMessage">The type of managed message.</typeparam>
        /// <param name="messages">The messages to process.</param>
        public void ProcessManagedBatch<TMessage>(IEnumerable<TMessage> messages)
            where TMessage : IMessage
        {
            using var scope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("MessageBatchProcessor"), $"ProcessManaged_{typeof(TMessage).Name}"));
            
            if (!_processors.TryGetValue(typeof(TMessage), out var processorObj) || 
                !(processorObj is Action<TMessage> processor))
            {
                _logger.Log(LogLevel.Warning, 
                    $"No processor registered for managed message type {typeof(TMessage).Name}",
                    "MessageBatchProcessor");
                return;
            }
            
            int processedCount = 0;
            foreach (var message in messages)
            {
                try
                {
                    processor(message);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, 
                        $"Error processing managed message: {ex.Message}",
                        "MessageBatchProcessor");
                }
            }
            
            _logger.Log(LogLevel.Debug, 
                $"Processed {processedCount} managed messages of type {typeof(TMessage).Name}",
                "MessageBatchProcessor");
        }
        
        /// <summary>
        /// Processes a queue of unmanaged messages using a single-threaded job.
        /// </summary>
        /// <typeparam name="TMessage">The type of unmanaged message.</typeparam>
        /// <param name="messageQueue">The message queue to process.</param>
        /// <returns>A JobHandle for the processing job.</returns>
        public JobHandle ProcessUnmanagedQueue<TMessage>(MessageQueue<TMessage> messageQueue)
            where TMessage : unmanaged, IUnmanagedMessage
        {
            using var scope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("MessageBatchProcessor"), $"ProcessQueue_{typeof(TMessage).Name}"));
            
            if (!_processors.TryGetValue(typeof(TMessage), out var processorObj) || 
                !(processorObj is FunctionPointer<MessageProcessor<TMessage>> processor))
            {
                _logger.Log(LogLevel.Warning, 
                    $"No processor registered for unmanaged message type {typeof(TMessage).Name}",
                    "MessageBatchProcessor");
                return default;
            }
            
            var processedCount = new NativeReference<int>(Allocator.TempJob);
            
            var job = new ProcessMessagesJob<TMessage>
            {
                Queue = messageQueue.Queue,
                ProcessedCount = processedCount,
                ProcessorFunction = processor
            };
            
            var jobHandle = job.Schedule();
            
            // Schedule cleanup job
            var cleanupJob = new CleanupReferenceJob
            {
                Reference = processedCount
            };
            
            var cleanupHandle = cleanupJob.Schedule(jobHandle);
            
            _logger.Log(LogLevel.Debug, 
                $"Scheduled processing of queued unmanaged messages of type {typeof(TMessage).Name}",
                "MessageBatchProcessor");
            
            return cleanupHandle;
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed) return;
            
            _processors.Clear();
            _isDisposed = true;
            
            _logger.Log(LogLevel.Info, "MessageBatchProcessor disposed", "MessageBatchProcessor");
        }
    }
    
    /// <summary>
    /// Job for cleaning up native arrays after processing.
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    [BurstCompile]
    internal struct CleanupArraysJob<TMessage> : IJob
        where TMessage : unmanaged
    {
        public NativeArray<TMessage> MessageArray;
        public NativeArray<int> ResultsArray;
        
        public void Execute()
        {
            if (MessageArray.IsCreated)
                MessageArray.Dispose();
            if (ResultsArray.IsCreated)
                ResultsArray.Dispose();
        }
    }
    
    /// <summary>
    /// Job for cleaning up native references after processing.
    /// </summary>
    [BurstCompile]
    internal struct CleanupReferenceJob : IJob
    {
        public NativeReference<int> Reference;
        
        public void Execute()
        {
            if (Reference.IsCreated)
                Reference.Dispose();
        }
    }
}