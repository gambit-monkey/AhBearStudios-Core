using System;
using AhBearStudios.Core.Messaging.Interfaces;
using MessagePipe;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Adapter that converts between IMessageFilter and MessagePipe's ISubscriptionFilter.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages to filter.</typeparam>
    internal class MessageFilterAdapter<TMessage> : ISubscriptionFilter, IDisposable
        where TMessage : IMessage
    {
        private readonly IMessageFilter<TMessage> _filter;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the MessageFilterAdapter class.
        /// </summary>
        /// <param name="filter">The message filter to adapt.</param>
        public MessageFilterAdapter(IMessageFilter<TMessage> filter)
        {
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

        /// <summary>
        /// Applies the filter to a message.
        /// </summary>
        /// <param name="message">The message to filter.</param>
        /// <returns>True if the message passes the filter, false otherwise.</returns>
        public bool Apply(TMessage message)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(MessageFilterAdapter<TMessage>));
            }

            return _filter.PassesFilter(message);
        }

        /// <summary>
        /// Creates a MessagePipe filter from an IMessageFilter.
        /// </summary>
        /// <param name="filter">The filter to adapt.</param>
        /// <returns>A MessagePipe filter that uses the provided filter.</returns>
        public static ISubscriptionFilter Create(IMessageFilter<TMessage> filter)
        {
            return new MessageFilterAdapter<TMessage>(filter);
        }

        /// <summary>
        /// Disposes this adapter and the underlying filter.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _filter.Dispose();
            _isDisposed = true;
        }
    }
}