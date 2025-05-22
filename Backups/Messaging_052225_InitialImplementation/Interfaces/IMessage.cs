using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Base interface for all messages
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance
        /// </summary>
        Guid Id { get; }
    
        /// <summary>
        /// Gets the timestamp when this message was created
        /// </summary>
        DateTime Timestamp { get; }
    }
}