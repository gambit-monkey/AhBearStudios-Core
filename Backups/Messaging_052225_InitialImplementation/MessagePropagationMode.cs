using System;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Defines how messages propagate between parent and child buses in a hierarchical message bus system.
    /// Controls the direction and behavior of message propagation.
    /// </summary>
    [Flags]
    public enum MessagePropagationMode
    {
        /// <summary>
        /// Messages do not propagate between parent and child buses.
        /// Each bus processes messages independently.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Messages published to a child bus are also propagated to its parent bus.
        /// </summary>
        UpwardOnly = 1,
        
        /// <summary>
        /// Messages published to a parent bus are also propagated to all its child buses.
        /// </summary>
        DownwardOnly = 2,
        
        /// <summary>
        /// Messages propagate both upward from child to parent and downward from parent to child.
        /// Combines UpwardOnly and DownwardOnly modes.
        /// </summary>
        Bidirectional = UpwardOnly | DownwardOnly,
        
        /// <summary>
        /// Messages published to a child bus are propagated to its parent and to all its sibling buses.
        /// </summary>
        SiblingAware = 4,
        
        /// <summary>
        /// Propagates messages in all directions: upward, downward, and between siblings.
        /// Combines Bidirectional and SiblingAware modes.
        /// </summary>
        BroadcastAll = Bidirectional | SiblingAware
    }
}