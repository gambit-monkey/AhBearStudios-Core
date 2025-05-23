using System;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.MessageBus.Handlers
{
    /// <summary>
    /// Handle for simple subscription operations that provides disposal tracking.
    /// </summary>
    internal sealed class SimpleSubscriptionHandle : IDisposable
    {
        private readonly IDisposable _innerSubscription;
        private readonly Action _onDispose;
        private readonly IBurstLogger _logger;
        private readonly string _subscriberName;
        private readonly bool _isFiltered;
        private readonly DateTime _createdAt;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the SimpleSubscriptionHandle class.
        /// </summary>
        /// <param name="innerSubscription">The underlying subscription to wrap.</param>
        /// <param name="onDispose">Action to execute when disposing.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="subscriberName">The name of the subscriber for logging purposes.</param>
        /// <param name="isFiltered">Whether this is a filtered subscription.</param>
        public SimpleSubscriptionHandle(
            IDisposable innerSubscription,
            Action onDispose,
            IBurstLogger logger,
            string subscriberName,
            bool isFiltered = false)
        {
            _innerSubscription = innerSubscription ?? throw new ArgumentNullException(nameof(innerSubscription));
            _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriberName = subscriberName;
            _isFiltered = isFiltered;
            _createdAt = DateTime.UtcNow;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _innerSubscription.Dispose();
                _onDispose();
                
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var duration = DateTime.UtcNow - _createdAt;
                    var subscriptionType = _isFiltered ? "filtered subscription" : "subscription";
                    _logger.Log(LogLevel.Debug,
                        $"{_subscriberName}: Disposed {subscriptionType} after {duration.TotalSeconds:F2} seconds",
                        "MessagePipeSubscriber");
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error,
                    $"{_subscriberName}: Error disposing subscription: {ex.Message}",
                    "MessagePipeSubscriber");
                throw;
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}