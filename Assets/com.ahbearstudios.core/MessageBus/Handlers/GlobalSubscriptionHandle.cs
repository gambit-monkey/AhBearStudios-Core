using System;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.MessageBus.Handlers
{
    /// <summary>
    /// Handle for global subscription operations that provides disposal tracking.
    /// </summary>
    internal sealed class GlobalSubscriptionHandle : IDisposable
    {
        private readonly IDisposable _innerSubscription;
        private readonly Action _onDispose;
        private readonly IBurstLogger _logger;
        private readonly string _subscriberName;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the GlobalSubscriptionHandle class.
        /// </summary>
        /// <param name="innerSubscription">The underlying subscription to wrap.</param>
        /// <param name="onDispose">Action to execute when disposing.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="subscriberName">The name of the subscriber for logging purposes.</param>
        public GlobalSubscriptionHandle(
            IDisposable innerSubscription,
            Action onDispose,
            IBurstLogger logger,
            string subscriberName)
        {
            _innerSubscription = innerSubscription ?? throw new ArgumentNullException(nameof(innerSubscription));
            _onDispose = onDispose;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriberName = subscriberName;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _innerSubscription.Dispose();
                _onDispose?.Invoke();
                
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.Log(LogLevel.Debug,
                        $"{_subscriberName}: Disposed subscription for all keys",
                        "MessagePipeKeyedSubscriber");
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error,
                    $"{_subscriberName}: Error disposing subscription: {ex.Message}",
                    "MessagePipeKeyedSubscriber");
                throw;
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}