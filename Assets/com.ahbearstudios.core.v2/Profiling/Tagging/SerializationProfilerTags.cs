using System;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Tagging
{
    /// <summary>
    /// Predefined profiler tags for serialization operations
    /// </summary>
    public static class SerializationProfilerTags
    {
        // Serialization category
        private static readonly ProfilerCategory SerializationCategory = ProfilerCategory.Scripts;
        
        // Common serialization operation tags
        public static readonly ProfilerTag SerializeMessage = new ProfilerTag(SerializationCategory, "Serialization.SerializeMessage");
        public static readonly ProfilerTag DeserializeMessage = new ProfilerTag(SerializationCategory, "Serialization.DeserializeMessage");
        public static readonly ProfilerTag SerializePayload = new ProfilerTag(SerializationCategory, "Serialization.SerializePayload");
        public static readonly ProfilerTag DeserializePayload = new ProfilerTag(SerializationCategory, "Serialization.DeserializePayload");
        public static readonly ProfilerTag SerializeBatch = new ProfilerTag(SerializationCategory, "Serialization.SerializeBatch");
        public static readonly ProfilerTag DeserializeBatch = new ProfilerTag(SerializationCategory, "Serialization.DeserializeBatch");
        
        /// <summary>
        /// Creates a serializer-specific profiler tag
        /// </summary>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="serializerId">Serializer identifier</param>
        /// <returns>A profiler tag for the specific serialization operation</returns>
        public static ProfilerTag ForSerializer(string operationType, Guid serializerId)
        {
            string guidPrefix = serializerId.ToString().Substring(0, 8);
            return new ProfilerTag(SerializationCategory, $"Serialization.{guidPrefix}.{operationType}");
        }
        
        /// <summary>
        /// Creates a serializer-specific profiler tag using only the serializer name
        /// </summary>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="serializerName">Name of the serializer</param>
        /// <returns>A profiler tag for the specific serialization operation</returns>
        public static ProfilerTag ForSerializerName(string operationType, string serializerName)
        {
            return new ProfilerTag(SerializationCategory, $"Serialization.{serializerName}.{operationType}");
        }
        
        /// <summary>
        /// Creates a message-type-specific profiler tag
        /// </summary>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="messageTypeCode">Message type code</param>
        /// <returns>A profiler tag for the specific message type operation</returns>
        public static ProfilerTag ForMessageType(string operationType, ushort messageTypeCode)
        {
            return new ProfilerTag(SerializationCategory, $"Serialization.MsgType{messageTypeCode}.{operationType}");
        }
        
        /// <summary>
        /// Creates a message-specific profiler tag
        /// </summary>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="messageId">Message identifier</param>
        /// <returns>A profiler tag for the specific message operation</returns>
        public static ProfilerTag ForMessage(string operationType, Guid messageId)
        {
            string guidPrefix = messageId.ToString().Substring(0, 8);
            return new ProfilerTag(SerializationCategory, $"Serialization.Msg.{guidPrefix}.{operationType}");
        }
        
        /// <summary>
        /// Creates a profiler tag for message batch operations
        /// </summary>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="batchSize">Size of the batch</param>
        /// <returns>A profiler tag for the batch operation</returns>
        public static ProfilerTag ForBatch(string operationType, int batchSize)
        {
            return new ProfilerTag(SerializationCategory, $"Serialization.Batch{batchSize}.{operationType}");
        }
        
        /// <summary>
        /// Gets the appropriate tag for the given serialization operation type
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <returns>A profiler tag for the operation</returns>
        public static ProfilerTag ForOperation(string operationType)
        {
            switch (operationType.ToLowerInvariant())
            {
                case "serialize":
                case "serializemessage":
                    return SerializeMessage;
                case "deserialize":
                case "deserializemessage":
                    return DeserializeMessage;
                case "serializepayload":
                    return SerializePayload;
                case "deserializepayload":
                    return DeserializePayload;
                case "serializebatch":
                    return SerializeBatch;
                case "deserializebatch":
                    return DeserializeBatch;
                default:
                    return new ProfilerTag(SerializationCategory, $"Serialization.{operationType}");
            }
        }
        
        /// <summary>
        /// Creates a combined profiler tag for both a serializer and message type
        /// </summary>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="serializerName">Name of the serializer</param>
        /// <param name="messageTypeCode">Message type code</param>
        /// <returns>A profiler tag for the specific serializer/message type operation</returns>
        public static ProfilerTag ForSerializerAndMessageType(string operationType, string serializerName, ushort messageTypeCode)
        {
            return new ProfilerTag(SerializationCategory, $"Serialization.{serializerName}.Type{messageTypeCode}.{operationType}");
        }
        
        /// <summary>
        /// Creates a detailed profiler tag with serializer metrics context
        /// </summary>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="serializerName">Name of the serializer</param>
        /// <param name="messageSize">Size of the message in bytes</param>
        /// <returns>A profiler tag with size details</returns>
        public static ProfilerTag ForSizedOperation(string operationType, string serializerName, int messageSize)
        {
            string sizeCategory = messageSize < 1024 ? $"{messageSize}B" : 
                                 messageSize < 1048576 ? $"{messageSize / 1024}KB" : 
                                 $"{messageSize / 1048576}MB";
            
            return new ProfilerTag(SerializationCategory, $"Serialization.{serializerName}.{sizeCategory}.{operationType}");
        }
    }
}