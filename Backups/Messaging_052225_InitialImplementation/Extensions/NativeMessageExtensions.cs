using AhBearStudios.Core.Messaging.Interfaces;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Extensions
{
    /// <summary>
    /// Extension for the IMessage interface to add Burst-compatible type ID functionality.
    /// </summary>
    public static class NativeMessageExtensions
    {
        /// <summary>
        /// Gets the type ID for a message.
        /// </summary>
        /// <typeparam name="T">The type of message.</typeparam>
        /// <param name="message">The message to get the type ID for.</param>
        /// <returns>The type ID for the message.</returns>
        [GenerateTestsForBurstCompatibility]
        public static int GetTypeId<T>(this T message) where T : unmanaged, IMessage
        {
            // In a real implementation, this would get the type ID from the message
            // For now, we'll use a simple hash of the type name
            return typeof(T).GetHashCode();
        }

        /// <summary>
        /// Gets the type ID for a message type.
        /// </summary>
        /// <typeparam name="T">The type of message.</typeparam>
        /// <returns>The type ID for the message type.</returns>
        [GenerateTestsForBurstCompatibility]
        public static int GetTypeId<T>() where T : unmanaged, IMessage
        {
            // In a real implementation, this would get the type ID from the type
            // For now, we'll use a simple hash of the type name
            return typeof(T).GetHashCode();
        }
    }
}