using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Tagging
{
    /// <summary>
    /// Predefined profiler tags for message bus operations
    /// </summary>
    public static class MessageBusProfilerTags
    {
        // MessageBusService category
        private static readonly ProfilerCategory MessageBusCategory = ProfilerCategory.Scripts;
        
        // Common operation tags
        public static readonly ProfilerTag MessagePublish = new ProfilerTag(MessageBusCategory, "MessageBusService.Publish");
        public static readonly ProfilerTag MessageDeliver = new ProfilerTag(MessageBusCategory, "MessageBusService.Deliver");
        public static readonly ProfilerTag MessageSubscribe = new ProfilerTag(MessageBusCategory, "MessageBusService.Subscribe");
        public static readonly ProfilerTag MessageUnsubscribe = new ProfilerTag(MessageBusCategory, "MessageBusService.Unsubscribe");
        public static readonly ProfilerTag MessageProcess = new ProfilerTag(MessageBusCategory, "MessageBusService.Process");
        public static readonly ProfilerTag MessageQueue = new ProfilerTag(MessageBusCategory, "MessageBusService.Queue");
        public static readonly ProfilerTag MessageBatch = new ProfilerTag(MessageBusCategory, "MessageBusService.Batch");
        public static readonly ProfilerTag MessageReliableDeliver = new ProfilerTag(MessageBusCategory, "MessageBusService.ReliableDeliver");
        
        /// <summary>
        /// Creates a message bus-specific profiler tag
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="busId">Message bus identifier (shortened GUID)</param>
        /// <returns>A profiler tag for the specific message bus operation</returns>
        public static ProfilerTag ForBus(string operationType, System.Guid busId)
        {
            string guidPrefix = busId.ToString().Substring(0, 8);
            return new ProfilerTag(MessageBusCategory, $"MessageBusService.{guidPrefix}.{operationType}");
        }
        
        /// <summary>
        /// Creates a message bus-specific profiler tag using only the bus name
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="busName">Name of the message bus</param>
        /// <returns>A profiler tag for the specific message bus operation</returns>
        public static ProfilerTag ForBusNamed(string operationType, string busName)
        {
            return new ProfilerTag(MessageBusCategory, $"MessageBusService.{busName}.{operationType}");
        }
        
        /// <summary>
        /// Creates a message type-specific profiler tag
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="messageTypeName">Name of the message type</param>
        /// <returns>A profiler tag for the specific message type operation</returns>
        public static ProfilerTag ForMessageType(string operationType, string messageTypeName)
        {
            return new ProfilerTag(MessageBusCategory, $"MessageBusService.{messageTypeName}.{operationType}");
        }
        
        /// <summary>
        /// Creates a delivery service-specific profiler tag
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="serviceName">Name of the delivery service</param>
        /// <returns>A profiler tag for the specific delivery service operation</returns>
        public static ProfilerTag ForDeliveryService(string operationType, string serviceName)
        {
            return new ProfilerTag(MessageBusCategory, $"MessageBusService.{serviceName}.{operationType}");
        }
    }
}