using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Token representing a message subscription that can be disposed to unsubscribe
    /// </summary>
    public interface ISubscriptionToken : IDisposable
    {
        /// <summary>
        /// Gets a unique identifier for this subscription
        /// </summary>
        Guid Id { get; }
    
        /// <summary>
        /// Gets a value indicating whether this subscription is active
        /// </summary>
        bool IsActive { get; }
    
        /// <summary>
        /// Gets the type of message this subscription is for
        /// </summary>
        Type MessageType { get; }
    }
}