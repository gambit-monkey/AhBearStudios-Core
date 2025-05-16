using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using Serilog;

namespace AhBearStudios.Core.Messaging.Serialization
{
    /// <summary>
    /// MemoryPack-based serializer implementation with compression support
    /// </summary>
    public class CompressibleMemoryPackSerializer : MemoryPackMessageSerializer, ICompressibleMessageSerializer
    {
        // Compression metadata for serialized data
        private const string COMPRESSION_HEADER = "CMPR:";
        private const string COMPRESSION_SEPARATOR = ":";
        private readonly IBurstLogger _compressorLogger;
        
        public IMessageCompressor Compressor { get; set; }
        
        public CompressibleMemoryPackSerializer(IMessageCompressor compressor, IBurstLogger logger = null)
            : base(logger)
        {
            Compressor = compressor ?? throw new ArgumentNullException(nameof(compressor));
            _compressorLogger = logger;
        }
        
        public string SerializeCompressed(object obj, int compressionThreshold = 1024)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            try
            {
                // Serialize the object using MemoryPack
                byte[] bytes = SerializeToBinary(obj);
                
                // Check if compression should be applied
                if (bytes.Length >= compressionThreshold)
                {
                    // Compress the data
                    byte[] compressedBytes = Compressor.Compress(bytes);
                    
                    // If compression actually reduced the size
                    if (compressedBytes.Length < bytes.Length)
                    {
                        // Create header with compression info
                        string header = $"{COMPRESSION_HEADER}{Compressor.Algorithm}{COMPRESSION_SEPARATOR}";
                        
                        // Combine header and compressed data
                        string base64 = Convert.ToBase64String(compressedBytes);
                        return header + base64;
                    }
                }
                
                // No compression applied or compression wasn't beneficial
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                _compressorLogger?.Log(LogLevel.Error, $"Error compressing object of type {obj.GetType().Name}: {ex.Message}", "Serializer");
                throw;
            }
        }
        
        public string SerializeCompressed<T>(T obj, int compressionThreshold = 1024)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            try
            {
                // Serialize the object using MemoryPack
                byte[] bytes = SerializeToBinary(obj);
                
                // Check if compression should be applied
                if (bytes.Length >= compressionThreshold)
                {
                    // Compress the data
                    byte[] compressedBytes = Compressor.Compress(bytes);
                    
                    // If compression actually reduced the size
                    if (compressedBytes.Length < bytes.Length)
                    {
                        // Create header with compression info
                        string header = $"{COMPRESSION_HEADER}{Compressor.Algorithm}{COMPRESSION_SEPARATOR}";
                        
                        // Combine header and compressed data
                        string base64 = Convert.ToBase64String(compressedBytes);
                        return header + base64;
                    }
                }
                
                // No compression applied or compression wasn't beneficial
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                _compressorLogger?.Log(LogLevel.Error, $"Error compressing object of type {typeof(T).Name}: {ex.Message}","Serializer");
                throw;
            }
        }
        
        public object DeserializeCompressed(string data, Type type)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentNullException(nameof(data));
                
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            try
            {
                // Check if data is compressed
                if (data.StartsWith(COMPRESSION_HEADER))
                {
                    // Extract algorithm from header
                    int endOfHeader = data.IndexOf(COMPRESSION_SEPARATOR, COMPRESSION_HEADER.Length);
                    if (endOfHeader == -1)
                    {
                        throw new FormatException("Invalid compression header format");
                    }
                    
                    string algorithm = data.Substring(COMPRESSION_HEADER.Length, endOfHeader - COMPRESSION_HEADER.Length);
                    
                    // Verify algorithm matches our compressor
                    if (algorithm != Compressor.Algorithm)
                    {
                        throw new InvalidOperationException($"Data was compressed with {algorithm}, but current compressor is {Compressor.Algorithm}");
                    }
                    
                    // Extract the compressed data
                    string compressedBase64 = data.Substring(endOfHeader + 1);
                    byte[] compressedBytes = Convert.FromBase64String(compressedBase64);
                    
                    // Decompress
                    byte[] bytes = Compressor.Decompress(compressedBytes);
                    
                    // Deserialize
                    return DeserializeFromBinary(bytes, type);
                }
                else
                {
                    // Not compressed, regular deserialization
                    byte[] bytes = Convert.FromBase64String(data);
                    return DeserializeFromBinary(bytes, type);
                }
            }
            catch (Exception ex)
            {
                _compressorLogger?.Log(LogLevel.Error, $"Error decompressing data to type {type.Name}: {ex.Message}", "Serializer");
                throw;
            }
        }
        
        public T DeserializeCompressed<T>(string data)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentNullException(nameof(data));

            try
            {
                // Check if data is compressed
                if (data.StartsWith(COMPRESSION_HEADER))
                {
                    // Extract algorithm from header
                    int endOfHeader = data.IndexOf(COMPRESSION_SEPARATOR, COMPRESSION_HEADER.Length, StringComparison.Ordinal);
                    if (endOfHeader == -1)
                    {
                        throw new FormatException("Invalid compression header format");
                    }
                    
                    string algorithm = data.Substring(COMPRESSION_HEADER.Length, endOfHeader - COMPRESSION_HEADER.Length);
                    
                    // Verify algorithm matches our compressor
                    if (algorithm != Compressor.Algorithm)
                    {
                        throw new InvalidOperationException($"Data was compressed with {algorithm}, but current compressor is {Compressor.Algorithm}");
                    }
                    
                    // Extract the compressed data
                    string compressedBase64 = data.Substring(endOfHeader + 1);
                    byte[] compressedBytes = Convert.FromBase64String(compressedBase64);
                    
                    // Decompress
                    byte[] bytes = Compressor.Decompress(compressedBytes);
                    
                    // Deserialize
                    return DeserializeFromBinary<T>(bytes);
                }
                else
                {
                    // Not compressed, regular deserialization
                    byte[] bytes = Convert.FromBase64String(data);
                    return DeserializeFromBinary<T>(bytes);
                }
            }
            catch (Exception ex)
            {
                _compressorLogger?.Log(LogLevel.Error, $"Error decompressing data to type {typeof(T).Name}: {ex.Message}","Serializer");
                throw;
            }
        }
    }
}