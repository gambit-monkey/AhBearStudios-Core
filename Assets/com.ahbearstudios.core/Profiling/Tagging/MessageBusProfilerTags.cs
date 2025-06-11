using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Tagging
{
    /// <summary>
    /// Predefined profiler tags for message bus operations
    /// </summary>
    public static class MessageBusProfilerTags
    {
        // MessageBus category
        private static readonly ProfilerCategory MessageBusCategory = ProfilerCategory.Scripts;
        
        // Common operation tags
        public static readonly ProfilerTag MessagePublish = new ProfilerTag(MessageBusCategory, "MessageBus.Publish");
        public static readonly ProfilerTag MessageDeliver = new ProfilerTag(MessageBusCategory, "MessageBus.Deliver");
        public static readonly ProfilerTag MessageSubscribe = new ProfilerTag(MessageBusCategory, "MessageBus.Subscribe");
        public static readonly ProfilerTag MessageUnsubscribe = new ProfilerTag(MessageBusCategory, "MessageBus.Unsubscribe");
        public static readonly ProfilerTag MessageProcess = new ProfilerTag(MessageBusCategory, "MessageBus.Process");
        public static readonly ProfilerTag MessageQueue = new ProfilerTag(MessageBusCategory, "MessageBus.Queue");
        public static readonly ProfilerTag MessageBatch = new ProfilerTag(MessageBusCategory, "MessageBus.Batch");
        public static readonly ProfilerTag MessageReliableDeliver = new ProfilerTag(MessageBusCategory, "MessageBus.ReliableDeliver");
        
        /// <summary>
        /// Creates a message bus-specific profiler tag
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="busId">Message bus identifier (shortened GUID)</param>
        /// <returns>A profiler tag for the specific message bus operation</returns>
        public static ProfilerTag ForBus(string operationType, System.Guid busId)
        {
            string guidPrefix = busId.ToString().Substring(0, 8);
            return new ProfilerTag(MessageBusCategory, $"MessageBus.{guidPrefix}.{operationType}");
        }
        
        /// <summary>
        /// Creates a message bus-specific profiler tag using only the bus name
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="busName">Name of the message bus</param>
        /// <returns>A profiler tag for the specific message bus operation</returns>
        public static ProfilerTag ForBusNamed(string operationType, string busName)
        {
            return new ProfilerTag(MessageBusCategory, $"MessageBus.{busName}.{operationType}");
        }
        
        /// <summary>
        /// Creates a message type-specific profiler tag
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="messageTypeName">Name of the message type</param>
        /// <returns>A profiler tag for the specific message type operation</returns>
        public static ProfilerTag ForMessageType(string operationType, string messageTypeName)
        {
            return new ProfilerTag(MessageBusCategory, $"MessageBus.{messageTypeName}.{operationType}");
        }
        
        /// <summary>
        /// Creates a delivery service-specific profiler tag
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="serviceName">Name of the delivery service</param>
        /// <returns>A profiler tag for the specific delivery service operation</returns>
        public static ProfilerTag ForDeliveryService(string operationType, string serviceName)
        {
            return new ProfilerTag(MessageBusCategory, $"MessageBus.{serviceName}.{operationType}");
        }
    }
}