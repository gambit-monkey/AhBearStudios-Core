using System;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using Cysharp.Threading.Tasks;
using MessagePipe;

namespace AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe
{
    /// <summary>
    /// Implementation of IKeyedMessagePublisher using MessagePipe's keyed publisher.
    /// Provides efficient keyed message publishing with performance profiling and logging.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    public sealed class MessagePipeKeyedPublisher<TKey, TMessage> : IKeyedMessagePublisher<TKey, TMessage>, IDisposable
    {
        private readonly IAsyncPublisher<TKey, TMessage> _publisher;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly string _publisherName;
        private readonly object _syncLock = new object();

        private long _totalMessagesPublished;
        private long _totalAsyncPublishes;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the MessagePipeKeyedPublisher class.
        /// </summary>
        /// <param name="publisher">The underlying MessagePipe keyed publisher.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="profiler">The profiler for performance monitoring.</param>
        public MessagePipeKeyedPublisher(
            IAsyncPublisher<TKey, TMessage> publisher,
            IBurstLogger logger,
            IProfiler profiler)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));

            _publisherName = $"KeyedPublisher<{typeof(TKey).Name}, {typeof(TMessage).Name}>";

            _logger.Log(LogLevel.Debug,
                $"Created {_publisherName}",
                "MessagePipeKeyedPublisher");
        }

        /// <inheritdoc />
        public void Publish(TKey key, TMessage message)
        {
            if (_disposed)
                throw new ObjectDisposedException(_publisherName);

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (message == null && !typeof(TMessage).IsValueType)
                throw new ArgumentNullException(nameof(message));

            using (_profiler.BeginSample($"{_publisherName}.Publish"))
            {
                try
                {
                    _publisher.Publish(key, message);

                    lock (_syncLock)
                    {
                        _totalMessagesPublished++;
                    }

                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        _logger.Log(LogLevel.Trace,
                            $"Published message with key '{key}' of type {typeof(TMessage).Name}",
                            "MessagePipeKeyedPublisher");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"Error publishing keyed message: {ex.Message}",
                        "MessagePipeKeyedPublisher");
                    throw;
                }
            }
        }

        /// <inheritdoc />

        /// <inheritdoc />
        public IDisposable PublishAsync(TKey key, TMessage message)
        {
            if (_disposed)
                throw new ObjectDisposedException(_publisherName);

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (message == null && !typeof(TMessage).IsValueType)
                throw new ArgumentNullException(nameof(message));

            using (_profiler.BeginSample($"{_publisherName}.PublishAsync"))
            {
                try
                {
                    // Create the UniTask from MessagePipe's PublishAsync
                    var task = _publisher.PublishAsync(key, message);

                    lock (_syncLock)
                    {
                        _totalMessagesPublished++;
                        _totalAsyncPublishes++;
                    }

                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        _logger.Log(LogLevel.Trace,
                            $"Published async message with key '{key}' of type {typeof(TMessage).Name}",
                            "MessagePipeKeyedPublisher");
                    }

                    // Create a TaskCompletionSource to monitor the UniTask
                    var taskCompletionSource = new TaskCompletionSource<bool>();

                    // Convert UniTask to a traditional Task and set up completion
                    task.AsTask().ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            taskCompletionSource.SetResult(true);
                            if (_logger.IsEnabled(LogLevel.Trace))
                            {
                                _logger.Log(LogLevel.Trace,
                                    $"{_publisherName}: Async publish completed for key '{key}'",
                                    "MessagePipeKeyedPublisher");
                            }
                        }
                        else if (t.IsFaulted)
                        {
                            _logger.Log(LogLevel.Error,
                                $"{_publisherName}: Error completing async publish for key '{key}': {t.Exception.Message}",
                                "MessagePipeKeyedPublisher");
                            taskCompletionSource.SetException(t.Exception);
                        }
                    });

                    // Return a disposable that can be used to await or cancel the operation
                    return new TaskDisposable(taskCompletionSource);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"Error publishing async keyed message: {ex.Message}",
                        "MessagePipeKeyedPublisher");
                    throw;
                }
            }
        }
        private sealed class TaskDisposable : IDisposable
        {
            private readonly TaskCompletionSource<bool> _taskCompletionSource;
            private bool _disposed;

            public TaskDisposable(TaskCompletionSource<bool> taskCompletionSource)
            {
                _taskCompletionSource = taskCompletionSource;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                // If the task is still running, try to cancel it
                if (!_taskCompletionSource.Task.IsCompleted)
                {
                    try
                    {
                        _taskCompletionSource.TrySetCanceled();
                    }
                    catch
                    {
                        // Ignore any exceptions during cancellation
                    }
                }
            }
        }

        /// <summary>
        /// Gets the total number of messages published by this publisher.
        /// </summary>
        public long TotalMessagesPublished
        {
            get
            {
                lock (_syncLock)
                {
                    return _totalMessagesPublished;
                }
            }
        }

        /// <summary>
        /// Gets the total number of async publishes initiated by this publisher.
        /// </summary>
        public long TotalAsyncPublishes
        {
            get
            {
                lock (_syncLock)
                {
                    return _totalAsyncPublishes;
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_syncLock)
            {
                if (_disposed)
                    return;

                _logger.Log(LogLevel.Debug,
                    $"Disposing {_publisherName}. Total messages published: {_totalMessagesPublished}, Async publishes: {_totalAsyncPublishes}",
                    "MessagePipeKeyedPublisher");

                // MessagePipe publishers typically don't need explicit disposal,
                // but we mark ourselves as disposed to prevent further use
                _disposed = true;
            }
        }

        /// <summary>
        /// Handle for async publish operations that provides completion tracking.
        /// </summary>
        private sealed class PublishAsyncHandle : IDisposable
        {
            private readonly IDisposable _innerHandle;
            private readonly TKey _key;
            private readonly IBurstLogger _logger;
            private readonly string _publisherName;
            private bool _disposed;

            public PublishAsyncHandle(
                IDisposable innerHandle,
                TKey key,
                IBurstLogger logger,
                string publisherName)
            {
                _innerHandle = innerHandle ?? throw new ArgumentNullException(nameof(innerHandle));
                _key = key;
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _publisherName = publisherName;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                try
                {
                    _innerHandle.Dispose();

                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        _logger.Log(LogLevel.Trace,
                            $"{_publisherName}: Async publish completed for key '{_key}'",
                            "MessagePipeKeyedPublisher");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"{_publisherName}: Error completing async publish for key '{_key}': {ex.Message}",
                        "MessagePipeKeyedPublisher");
                    throw;
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}