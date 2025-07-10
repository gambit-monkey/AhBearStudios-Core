namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Interface for creating message serializer instances.
    /// </summary>
    public interface IMessageSerializerFactory
    {
        /// <summary>
        /// Creates a default message serializer.
        /// </summary>
        /// <returns>A configured message serializer.</returns>
        IMessageSerializer CreateDefaultSerializer();
        
        /// <summary>
        /// Creates a burst-compatible message serializer.
        /// </summary>
        /// <returns>A Burst-compatible message serializer.</returns>
        IMessageSerializer CreateBurstSerializer();
        
        /// <summary>
        /// Creates a network-optimized message serializer.
        /// </summary>
        /// <returns>A network-optimized message serializer.</returns>
        IMessageSerializer CreateNetworkSerializer();
        
        /// <summary>
        /// Creates a composite message serializer with multiple backends.
        /// </summary>
        /// <returns>A composite message serializer.</returns>
        IMessageSerializer CreateCompositeSerializer();
    }
}