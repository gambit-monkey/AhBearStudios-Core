using System;
using AhBearStudios.Core.Logging;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe
{
    /// <summary>
    /// Handle for async publish operations that provides completion tracking.
    /// </summary>
    internal sealed class AsyncPublishTracker<T> : IDisposable
    {
        private readonly UniTask _task;
        private readonly T _message;
        private readonly IBurstLogger _logger;
        private readonly string _publisherName;
        private readonly DateTime _startTime;
        private bool _disposed;

        public AsyncPublishTracker(
            Cysharp.Threading.Tasks.UniTask task, 
            T message, 
            IBurstLogger logger,
            string publisherName)
        {
            _task = task;
            _message = message;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _publisherName = publisherName;
            _startTime = DateTime.UtcNow;
        
            // Start a fire-and-forget task to log when the operation completes
            LogTaskCompletionAsync().Forget();
        }

        private async Cysharp.Threading.Tasks.UniTask LogTaskCompletionAsync()
        {
            try
            {
                await _task;
            
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    var duration = DateTime.UtcNow - _startTime;
                    var messageType = typeof(T).Name;
                
                    _logger.Log(LogLevel.Trace,
                        $"{_publisherName}: Async publish completed for {messageType} in {duration.TotalMilliseconds:F2}ms",
                        "MessagePipePublisher");
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error,
                    $"{_publisherName}: Error in async publish: {ex.Message}",
                    "MessagePipePublisher");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            
            // We don't need to do anything with the task here
            // as we're already awaiting it in the LogTaskCompletionAsync method
            _disposed = true;
        }
    }
}