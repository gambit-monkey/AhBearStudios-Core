using System;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.MessageBus.Processors;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Profiling;

namespace AhBearStudios.Core.MessageBus.Processors
{
    /// <summary>
    /// Enterprise-ready batch processor that delegates unmanaged payload batching
    /// to <see cref="IMessageBusJobProcessor"/>, while handling managed messages inline.
    /// </summary>
    public sealed class MessageBatchProcessor : IDisposable
    {
        private readonly IMessageBusJobProcessor _jobProcessor;
        private readonly ILoggingService _logger;
        private readonly IProfilerService _profilerService;

        private readonly Dictionary<Type, Delegate> _unmanagedHandlers;
        private readonly Dictionary<Type, Delegate> _managedHandlers;
        private bool _disposed;

        /// <summary>
        /// Constructs a new MessageBatchProcessor.
        /// </summary>
        /// <param name="jobProcessor">Injected Burst-friendly job processor.</param>
        /// <param name="logger">Injected Burst-capable logger.</param>
        /// <param name="profilerService">Injected profiler for timing samples.</param>
        public MessageBatchProcessor(
            IMessageBusJobProcessor jobProcessor,
            ILoggingService logger,
            IProfilerService profilerService)
        {
            _jobProcessor = jobProcessor ?? throw new ArgumentNullException(nameof(jobProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _unmanagedHandlers = new Dictionary<Type, Delegate>();
            _managedHandlers = new Dictionary<Type, Delegate>();
            _disposed = false;
        }

        /// <summary>
        /// Registers a handler for unmanaged messages of type T.
        /// </summary>
        /// <typeparam name="T">Unmanaged struct type.</typeparam>
        /// <param name="handler">Delegate to invoke when messages are dispatched.</param>
        public void RegisterUnmanagedProcessor<T>(Action<T> handler) where T : unmanaged
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _unmanagedHandlers[typeof(T)] = handler;
        }

        /// <summary>
        /// Schedules a batch processing job for an array of unmanaged messages.
        /// </summary>
        /// <typeparam name="T">Unmanaged struct type.</typeparam>
        /// <param name="messages">NativeArray of messages to enqueue.</param>
        /// <param name="dependency">Optional prior JobHandle dependency.</param>
        /// <returns>JobHandle for the scheduled job.</returns>
        public JobHandle ProcessUnmanagedBatch<T>(NativeArray<T> messages, JobHandle dependency = default)
            where T : unmanaged
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MessageBatchProcessor));
            if (!messages.IsCreated) throw new ArgumentException("Messages must be a valid NativeArray", nameof(messages));

            var type = typeof(T);
            if (!_unmanagedHandlers.ContainsKey(type))
                throw new InvalidOperationException($"No handler registered for {type.Name}");

            using (var scope = _profilerService.BeginScope(ProfilerCategory.Scripts, $"ProcessUnmanagedBatch<{type.Name}>"))
            {
                _logger.LogInfo($"Enqueueing {messages.Length} messages of type {type.Name}");

                for (int i = 0; i < messages.Length; i++)
                {
                    var payload = SerializeUnmanaged(messages[i]);
                    _jobProcessor.Enqueue(payload);
                }

                return _jobProcessor.ScheduleProcessing(dependency, messages.Length);
            }
        }

        /// <summary>
        /// Completes the batch job and dispatches payloads to registered handlers.
        /// </summary>
        /// <typeparam name="T">Unmanaged struct type.</typeparam>
        public void CompleteAndDispatchUnmanaged<T>() where T : unmanaged
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MessageBatchProcessor));

            var type = typeof(T);
            if (!_unmanagedHandlers.TryGetValue(type, out var del))
                throw new InvalidOperationException($"No handler registered for {type.Name}");

            var handler = (Action<T>)del;

            using (var scope = _profilerService.BeginScope(ProfilerCategory.Scripts, $"CompleteAndDispatchUnmanaged<{type.Name}>") )
            {
                _jobProcessor.CompleteAndDispatch(payload =>
                {
                    var msg = DeserializeUnmanaged<T>(payload);
                    handler(msg);
                });
            }
        }

        /// <summary>
        /// Registers a handler for managed messages of type T.
        /// </summary>
        public void RegisterManagedProcessor<T>(Action<T> handler) where T : class
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _managedHandlers[typeof(T)] = handler;
        }

        /// <summary>
        /// Processes a batch of managed messages on the calling thread.
        /// </summary>
        public void ProcessManagedBatch<T>(IReadOnlyList<T> messages) where T : class
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MessageBatchProcessor));
            if (messages == null) throw new ArgumentNullException(nameof(messages));

            var type = typeof(T);
            if (!_managedHandlers.TryGetValue(type, out var del))
                throw new InvalidOperationException($"No handler registered for {type.Name}");

            var handler = (Action<T>)del;

            using (var scope = _profilerService.BeginScope(ProfilerCategory.Scripts, $"ProcessManagedBatch<{type.Name}>") )
            {
                _logger.LogInfo($"Processing {messages.Count} managed messages of type {type.Name}");

                for (int i = 0, len = messages.Count; i < len; i++)
                    handler(messages[i]);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;

            _logger.LogInfo("Disposing MessageBatchProcessor");
            _jobProcessor.Dispose();
            _unmanagedHandlers.Clear();
            _managedHandlers.Clear();
            _disposed = true;
        }

        #region Serialization Helpers
        private static unsafe NativeArray<byte> SerializeUnmanaged<T>(T msg) where T : unmanaged
        {
            var size = UnsafeUtility.SizeOf<T>();
            var arr = new NativeArray<byte>(size, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            void* src = UnsafeUtility.AddressOf(ref msg);
            void* dst = NativeArrayUnsafeUtility.GetUnsafePtr(arr);
            UnsafeUtility.MemCpy(dst, src, size);
            return arr;
        }

        private static unsafe T DeserializeUnmanaged<T>(NativeArray<byte> data) where T : unmanaged
        {
            var size = UnsafeUtility.SizeOf<T>();
            if (data.Length < size)
                throw new InvalidOperationException($"Payload length {data.Length} < size of {typeof(T).Name}");

            var msg = default(T);
            void* dst = UnsafeUtility.AddressOf(ref msg);
            void* src = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);
            UnsafeUtility.MemCpy(dst, src, size);
            return msg;
        }
        #endregion
    }
}