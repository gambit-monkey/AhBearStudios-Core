using System;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Implementation of ISubscriptionToken for managing message subscriptions.
    /// Provides automatic cleanup through IDisposable.
    /// </summary>
    public class SubscriptionToken : ISubscriptionToken
    {
        private readonly object _messageBus;
        private readonly Guid _tokenId;
        private readonly Type _messageType;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the SubscriptionToken class.
        /// </summary>
        /// <param name="messageBus">The message bus that created this token.</param>
        /// <param name="messageType">The type of message this subscription is for, or null for all messages.</param>
        public SubscriptionToken(object messageBus, Type messageType = null)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _tokenId = Guid.NewGuid();
            _messageType = messageType;
            IsActive = true;
            _isDisposed = false;
        }

        /// <inheritdoc/>
        public Guid Id => _tokenId;

        /// <inheritdoc/>
        public bool IsActive { get; private set; }

        /// <inheritdoc/>
        public Type MessageType => _messageType;

        /// <summary>
        /// Gets the message bus associated with this token.
        /// </summary>
        public object MessageBus => _messageBus;

        /// <summary>
        /// Deactivates this subscription token.
        /// </summary>
        internal void Deactivate()
        {
            IsActive = false;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources used by the subscription token.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing && IsActive)
            {
                // Only attempt to unsubscribe if we're being disposed explicitly
                try
                {
                    // Use pattern matching to determine the correct unsubscribe method
                    if (_messageBus is IMessageBus messageBus)
                    {
                        messageBus.Unsubscribe(this);
                    }
                    else if (_messageBus is IKeyedMessageBus keyedMessageBus)
                    {
                        keyedMessageBus.Unsubscribe(this);
                    }
                }
                catch (Exception)
                {
                    // Swallow exceptions during disposal to prevent finalization issues
                }

                IsActive = false;
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~SubscriptionToken()
        {
            Dispose(false);
        }

        /// <summary>
        /// Determines if this token is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns>True if equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is SubscriptionToken other)
            {
                return _tokenId.Equals(other._tokenId);
            }

            return false;
        }

        /// <summary>
        /// Gets a hash code for this token.
        /// </summary>
        /// <returns>A hash code.</returns>
        public override int GetHashCode()
        {
            return _tokenId.GetHashCode();
        }

        /// <summary>
        /// Converts the token to a string representation.
        /// </summary>
        /// <returns>A string representation of the token.</returns>
        public override string ToString()
        {
            string typeName = _messageType != null ? _messageType.Name : "All";
            return $"SubscriptionToken[{_tokenId}] Type: {typeName}, IsActive: {IsActive}";
        }
    }
}