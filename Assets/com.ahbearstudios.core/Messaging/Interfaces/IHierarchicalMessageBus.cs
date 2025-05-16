using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for a hierarchical message bus that supports parent-child relationships
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish or subscribe to</typeparam>
    public interface IHierarchicalMessageBus<TMessage> : IMessageBus<TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Gets the parent message bus, or null if this is a root bus
        /// </summary>
        IHierarchicalMessageBus<TMessage> Parent { get; }
    
        /// <summary>
        /// Gets the child message buses
        /// </summary>
        IReadOnlyList<IHierarchicalMessageBus<TMessage>> Children { get; }
    
        /// <summary>
        /// Gets the propagation mode for this bus
        /// </summary>
        MessagePropagationMode PropagationMode { get; }
    
        /// <summary>
        /// Adds a child message bus
        /// </summary>
        /// <param name="child">The child bus to add</param>
        void AddChild(IHierarchicalMessageBus<TMessage> child);
    
        /// <summary>
        /// Removes a child message bus
        /// </summary>
        /// <param name="child">The child bus to remove</param>
        /// <returns>True if the child was removed; otherwise, false</returns>
        bool RemoveChild(IHierarchicalMessageBus<TMessage> child);
    
        /// <summary>
        /// Sets the parent message bus
        /// </summary>
        /// <param name="parent">The parent bus</param>
        void SetParent(IHierarchicalMessageBus<TMessage> parent);
    
        /// <summary>
        /// Clears the parent reference
        /// </summary>
        void ClearParent();
    
        /// <summary>
        /// Subscribes to messages locally (without propagation)
        /// </summary>
        /// <param name="handler">The handler to be called when a message is published</param>
        /// <returns>A token that can be disposed to unsubscribe</returns>
        ISubscriptionToken SubscribeLocal(Action<TMessage> handler);
    }
}