namespace AhBearStudios.Core.Messaging.Messages
{
    /// <summary>
    /// Base interface for all messages in the system.
    /// Provides core properties required for message routing and identification.
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// Gets the unique identifier for this message.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the timestamp when this message was created, represented as ticks.
        /// </summary>
        long TimestampTicks { get; }

        /// <summary>
        /// Gets the type code for efficient message routing and identification.
        /// </summary>
        ushort TypeCode { get; }
    }
}