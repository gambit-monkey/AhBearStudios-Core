using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for a hierarchical message bus that supports parent-child relationships.
    /// Allows for message propagation between related buses.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages this bus will handle.</typeparam>
    public interface IHierarchicalMessageBus<TMessage> : IMessageBus<TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Gets or sets the name of this message bus.
        /// </summary>
        string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the propagation mode for this message bus.
        /// </summary>
        MessagePropagationMode PropagationMode { get; set; }
        
        /// <summary>
        /// Gets the parent message bus, if any.
        /// </summary>
        IHierarchicalMessageBus<TMessage> Parent { get; }
        
        /// <summary>
        /// Gets a read-only list of child message buses.
        /// </summary>
        IReadOnlyList<IHierarchicalMessageBus<TMessage>> Children { get; }
        
        /// <summary>
        /// Adds a child bus to this bus.
        /// </summary>
        /// <param name="child">The child bus to add.</param>
        void AddChild(IHierarchicalMessageBus<TMessage> child);
        
        /// <summary>
        /// Removes a child bus from this bus.
        /// </summary>
        /// <param name="child">The child bus to remove.</param>
        void RemoveChild(IHierarchicalMessageBus<TMessage> child);
        
        /// <summary>
        /// Removes this bus from its parent, if any.
        /// </summary>
        void RemoveFromParent();
        
        /// <summary>
        /// Sets the propagation mode for this bus.
        /// </summary>
        /// <param name="mode">The new propagation mode.</param>
        void SetPropagationMode(MessagePropagationMode mode);
        
        /// <summary>
        /// Determines if this bus is a descendant of the specified bus.
        /// </summary>
        /// <param name="potentialAncestor">The potential ancestor bus.</param>
        /// <returns>True if this bus is a descendant of the specified bus, false otherwise.</returns>
        bool IsDescendantOf(IHierarchicalMessageBus<TMessage> potentialAncestor);
        
        /// <summary>
        /// Determines if this bus is an ancestor of the specified bus.
        /// </summary>
        /// <param name="potentialDescendant">The potential descendant bus.</param>
        /// <returns>True if this bus is an ancestor of the specified bus, false otherwise.</returns>
        bool IsAncestorOf(IHierarchicalMessageBus<TMessage> potentialDescendant);
    }
}