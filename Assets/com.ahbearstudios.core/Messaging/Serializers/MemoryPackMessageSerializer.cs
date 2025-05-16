using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AhBearStudios.Core.com.ahbearstudios.core.Messaging.Serializers.Formatters;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Messaging.Serializers.Formatters;
using MemoryPack;

namespace AhBearStudios.Core.Messaging.Serialization
{
    /// <summary>
    /// MemoryPack-based serializer implementation for high-performance message serialization
    /// </summary>
    public class MemoryPackMessageSerializer : IMessageSerializer
    {
        private readonly Dictionary<Type, byte[]> _typeCache = new Dictionary<Type, byte[]>();
        private readonly IBurstLogger _logger;

        public MemoryPackMessageSerializer(IBurstLogger logger = null)
        {
            _logger = logger;
            
            // Register converters for Unity types
            RegisterUnityTypeConverters();
        }

        /// <summary>
        /// Serializes an object to a string
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>The serialized string (Base64 encoded binary data)</returns>
        public string Serialize(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            try
            {
                // Serialize the object using MemoryPack
                byte[] bytes = MemoryPackSerializer.Serialize(obj.GetType(), obj);
                
                // Convert to Base64 string for storage in text files
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, $"Error serializing object of type {obj.GetType().Name}: {ex.Message}", "Serialization");
                throw;
            }
        }

        /// <summary>
        /// Serializes an object to a string
        /// </summary>
        /// <typeparam name="T">The type of object</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <returns>The serialized string (Base64 encoded binary data)</returns>
        public string Serialize<T>(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            try
            {
                // Serialize the object using MemoryPack
                byte[] bytes = MemoryPackSerializer.Serialize(obj);
                
                // Convert to Base64 string for storage in text files
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, $"Error serializing object of type {typeof(T).Name}: {ex.Message}", "Serialization");

                throw;
            }
        }

        /// <summary>
        /// Deserializes a string to an object
        /// </summary>
        /// <param name="data">The serialized string (Base64 encoded binary data)</param>
        /// <param name="type">The type of object</param>
        /// <returns>The deserialized object</returns>
        public object Deserialize(string data, Type type)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentNullException(nameof(data));
                
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            try
            {
                // Convert from Base64 string to binary
                byte[] bytes = Convert.FromBase64String(data);
                
                // Deserialize using MemoryPack
                return MemoryPackSerializer.Deserialize(type, bytes);
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, $"Error deserializing data to type {type.Name}: {ex.Message}", "Serialization");

                throw;
            }
        }

        /// <summary>
        /// Deserializes a string to an object
        /// </summary>
        /// <typeparam name="T">The type of object</typeparam>
        /// <param name="data">The serialized string (Base64 encoded binary data)</param>
        /// <returns>The deserialized object</returns>
        public T Deserialize<T>(string data)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentNullException(nameof(data));

            try
            {
                // Convert from Base64 string to binary
                byte[] bytes = Convert.FromBase64String(data);
                
                // Deserialize using MemoryPack
                return MemoryPackSerializer.Deserialize<T>(bytes);
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, $"Error deserializing data to type {typeof(T).Name}: {ex.Message}", "Serialization");

                throw;
            }
        }

        /// <summary>
        /// Directly serialize an object to binary
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>The serialized binary data</returns>
        public byte[] SerializeToBinary(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            try
            {
                // Serialize the object using MemoryPack
                return MemoryPackSerializer.Serialize(obj.GetType(), obj);
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, $"Error serializing object of type {obj.GetType().Name} to binary: {ex.Message}", "Serialization" );
                throw;
            }
        }

        /// <summary>
        /// Directly serialize an object to binary
        /// </summary>
        /// <typeparam name="T">The type of object</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <returns>The serialized binary data</returns>
        public byte[] SerializeToBinary<T>(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            try
            {
                // Serialize the object using MemoryPack
                return MemoryPackSerializer.Serialize(obj);
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, $"Error serializing object of type {typeof(T).Name} to binary: {ex.Message}", "Serialization");
                throw;
            }
        }

        /// <summary>
        /// Directly deserialize an object from binary
        /// </summary>
        /// <param name="bytes">The serialized binary data</param>
        /// <param name="type">The type of object</param>
        /// <returns>The deserialized object</returns>
        public object DeserializeFromBinary(byte[] bytes, Type type)
        {
            if (bytes == null || bytes.Length == 0)
                throw new ArgumentNullException(nameof(bytes));
                
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            try
            {
                // Deserialize using MemoryPack
                return MemoryPackSerializer.Deserialize(type, bytes);
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error,$"Error deserializing binary data to type {type.Name}: {ex.Message}", "Serialization" );
                throw;
            }
        }

        /// <summary>
        /// Directly deserialize an object from binary
        /// </summary>
        /// <typeparam name="T">The type of object</typeparam>
        /// <param name="bytes">The serialized binary data</param>
        /// <returns>The deserialized object</returns>
        public T DeserializeFromBinary<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                throw new ArgumentNullException(nameof(bytes));

            try
            {
                // Deserialize using MemoryPack
                return MemoryPackSerializer.Deserialize<T>(bytes);
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, $"Error deserializing binary data to type {typeof(T).Name}: {ex.Message}", "Serialization");
                throw;
            }
        }

        /// <summary>
        /// Serializes a message to a file
        /// </summary>
        /// <typeparam name="T">The type of message</typeparam>
        /// <param name="message">The message to serialize</param>
        /// <param name="filePath">The file path to save to</param>
        public async Task SerializeToFileAsync<T>(T message, string filePath)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await MemoryPackSerializer.SerializeAsync(fileStream, message);
                }
        
                _logger?.Log(LogLevel.Debug, $"Serialized message of type {typeof(T).Name} to file {filePath}","Serialization");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, $"Error serializing message of type {typeof(T).Name} to file {filePath}: {ex.Message}", "Serialization");
                throw;
            }
        }

        /// <summary>
        /// Deserializes a message from a file asynchronously
        /// </summary>
        /// <typeparam name="T">The type of message</typeparam>
        /// <param name="filePath">The file path to read from</param>
        /// <returns>The deserialized message</returns>
        public async Task<T> DeserializeFromFileAsync<T>(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
        
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            try
            {
                // Read all bytes from the file asynchronously
                byte[] data = await File.ReadAllBytesAsync(filePath);
        
                // Deserialize from the byte array
                return MemoryPackSerializer.Deserialize<T>(data);
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, $"Error deserializing message of type {typeof(T).Name} from file {filePath}: {ex.Message}", "Serialization");
                throw;
            }
        }

        /// <summary>
        /// Registers custom formatters for Unity types that MemoryPack doesn't handle by default
        /// </summary>
        private void RegisterUnityTypeConverters()
        {
            try
            {
                // Register Unity-specific type formatters
                // Note: These registrations need to be defined using MemoryPack's custom formatter APIs
                
                // Example: Vector3 formatter
                MemoryPackFormatterProvider.Register(new Vector3Formatter());
                
                // Example: Quaternion formatter
                MemoryPackFormatterProvider.Register(new QuaternionFormatter());
                
                // Example: Color formatter
                MemoryPackFormatterProvider.Register(new ColorFormatter());
                
                // Add more Unity type formatters as needed
                
                _logger?.Log(LogLevel.Debug, "Registered Unity type converters for MemoryPack", "Serialization");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, $"Error registering Unity type converters: {ex.Message}", "Serialization");
                // Continue without failing - default serialization might still work for some types
            }
        }
    }
}