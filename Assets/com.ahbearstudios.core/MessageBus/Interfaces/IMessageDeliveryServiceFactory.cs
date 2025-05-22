namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for creating message delivery service instances.
    /// </summary>
    public interface IMessageDeliveryServiceFactory
    {
        /// <summary>
        /// Creates a reliable message delivery service.
        /// </summary>
        /// <returns>A configured reliable message delivery service.</returns>
        IMessageDeliveryService CreateReliableDeliveryService();
        
        /// <summary>
        /// Creates a fire-and-forget message delivery service.
        /// </summary>
        /// <returns>A configured fire-and-forget message delivery service.</returns>
        IMessageDeliveryService CreateFireAndForgetService();
        
        /// <summary>
        /// Creates a batch-optimized message delivery service.
        /// </summary>
        /// <returns>A configured batch-optimized message delivery service.</returns>
        IMessageDeliveryService CreateBatchOptimizedService();
    }
}