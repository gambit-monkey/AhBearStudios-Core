namespace AhBearStudios.Core.Serialization.Models;

/// <summary>
/// Enumeration of supported serialization formats.
/// </summary>
public enum SerializationFormat
{
    /// <summary>
    /// MemoryPack binary format - High performance, zero-allocation serialization
    /// </summary>
    MemoryPack,
    
    /// <summary>
    /// Binary format using .NET BinaryFormatter - Legacy binary serialization
    /// </summary>
    Binary,
    
    /// <summary>
    /// JSON format using Newtonsoft.Json - Human-readable, debugging-friendly
    /// </summary>
    Json,
    
    /// <summary>
    /// XML format using System.Xml.Serialization - Legacy system compatibility
    /// </summary>
    Xml,
    
    /// <summary>
    /// MessagePack binary format - Alternative high-performance binary format
    /// </summary>
    MessagePack,
    
    /// <summary>
    /// Protocol Buffers format - Cross-platform compatibility
    /// </summary>
    Protobuf
}