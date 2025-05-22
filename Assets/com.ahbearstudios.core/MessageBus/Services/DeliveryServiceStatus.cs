namespace AhBearStudios.Core.Messaging.Services
{
    /// <summary>
    /// Enumeration of delivery service statuses.
    /// </summary>
    public enum DeliveryServiceStatus
    {
        /// <summary>
        /// The service is stopped.
        /// </summary>
        Stopped,
        
        /// <summary>
        /// The service is starting up.
        /// </summary>
        Starting,
        
        /// <summary>
        /// The service is running normally.
        /// </summary>
        Running,
        
        /// <summary>
        /// The service is stopping.
        /// </summary>
        Stopping,
        
        /// <summary>
        /// The service has encountered an error.
        /// </summary>
        Error,
        
        /// <summary>
        /// The service is paused.
        /// </summary>
        Paused
    }
}