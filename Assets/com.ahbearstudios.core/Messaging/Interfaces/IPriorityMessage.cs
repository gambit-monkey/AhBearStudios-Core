namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for a message with priority
    /// </summary>
    public interface IPriorityMessage : IMessage
    {
        /// <summary>
        /// Gets the priority of the message
        /// </summary>
        MessagePriority Priority { get; }
    }
}