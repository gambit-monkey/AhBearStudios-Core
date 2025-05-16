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
        private readonly IMessageBus _messageBus;
        private readonly Guid _tokenId;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the SubscriptionToken class.
        /// </summary>
        /// <param name="messageBus">The message bus that created this token.</param>
        public SubscriptionToken(IMessageBus messageBus)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _tokenId = Guid.NewGuid();
            IsActive = true;
            _isDisposed = false;
        }

        /// <inheritdoc/>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Gets the unique identifier for this subscription token.
        /// </summary>
        public Guid TokenId => _tokenId;

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
                    _messageBus.Unsubscribe(this);
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
            return $"SubscriptionToken[{_tokenId}] IsActive: {IsActive}";
        }
    }
}