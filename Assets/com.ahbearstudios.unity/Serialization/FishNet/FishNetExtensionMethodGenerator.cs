using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Serialization.Configs;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Unity.Serialization.FishNet
{
    /// <summary>
    /// Generates FishNet custom serializer extension methods for registered types.
    /// Creates Write and Read methods following FishNet's naming conventions.
    /// </summary>
    public class FishNetExtensionMethodGenerator
    {
        private readonly ILoggingService _logger;
        private readonly FishNetTypeRegistry _typeRegistry;
        private readonly ISerializationService _serializationService;
        private readonly FishNetSerializationOptions _options;
        
        /// <summary>
        /// Initializes a new instance of FishNetExtensionMethodGenerator.
        /// </summary>
        /// <param name="logger">Logging service</param>
        /// <param name="typeRegistry">Type registry for tracking types</param>
        /// <param name="serializationService">Serialization service for data conversion</param>
        /// <param name="options">FishNet serialization options</param>
        public FishNetExtensionMethodGenerator(
            ILoggingService logger,
            FishNetTypeRegistry typeRegistry,
            ISerializationService serializationService,
            FishNetSerializationOptions options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _typeRegistry = typeRegistry ?? throw new ArgumentNullException(nameof(typeRegistry));
            _serializationService = serializationService ?? throw new ArgumentNullException(nameof(serializationService));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }
        
        /// <summary>
        /// Generates extension methods for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to generate methods for</typeparam>
        /// <returns>Generated C# code</returns>
        public string GenerateExtensionMethods<T>()
        {
            return GenerateExtensionMethods(typeof(T));
        }
        
        /// <summary>
        /// Generates extension methods for a specific type.
        /// </summary>
        /// <param name="type">The type to generate methods for</param>
        /// <returns>Generated C# code</returns>
        public string GenerateExtensionMethods(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            
            var sb = new StringBuilder();
            
            // Generate method names
            var typeName = GetSafeTypeName(type);
            var writeMethodName = $"Write{typeName}";
            var readMethodName = $"Read{typeName}";
            
            // Generate Write method
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Writes {type.Name} to the network stream.");
            sb.AppendLine($"        /// </summary>");
            
            if (_options.UseGlobalCustomSerializers)
            {
                sb.AppendLine($"        [UseGlobalCustomSerializer]");
            }
            
            sb.AppendLine($"        public static void {writeMethodName}(this Writer writer, {type.FullName} value)");
            sb.AppendLine($"        {{");
            
            if (_options.ValidateDataOrder)
            {
                sb.AppendLine($"            // Data order validation marker");
                sb.AppendLine($"            writer.WriteByte(0xFE); // Start marker");
            }
            
            // Generate serialization logic
            if (IsSimpleType(type))
            {
                GenerateSimpleTypeWrite(sb, type, "value");
            }
            else
            {
                // Use AhBearStudios serialization service
                sb.AppendLine($"            // Serialize using AhBearStudios serialization service");
                sb.AppendLine($"            var service = ServiceResolver.Get<ISerializationService>();");
                sb.AppendLine($"            var bytes = service.Serialize(value, default, SerializationFormat.FishNet);");
                sb.AppendLine($"            writer.WriteArraySegmentAndSize(bytes);");
            }
            
            if (_options.ValidateDataOrder)
            {
                sb.AppendLine($"            writer.WriteByte(0xFF); // End marker");
            }
            
            sb.AppendLine($"        }}");
            sb.AppendLine();
            
            // Generate Read method
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Reads {type.Name} from the network stream.");
            sb.AppendLine($"        /// </summary>");
            
            if (_options.UseGlobalCustomSerializers)
            {
                sb.AppendLine($"        [UseGlobalCustomSerializer]");
            }
            
            sb.AppendLine($"        public static {type.FullName} {readMethodName}(this Reader reader)");
            sb.AppendLine($"        {{");
            
            if (_options.ValidateDataOrder)
            {
                sb.AppendLine($"            // Data order validation");
                sb.AppendLine($"            var startMarker = reader.ReadByte();");
                sb.AppendLine($"            if (startMarker != 0xFE)");
                sb.AppendLine($"                throw new Exception(\"Invalid data order: expected start marker\");");
            }
            
            // Generate deserialization logic
            if (IsSimpleType(type))
            {
                GenerateSimpleTypeRead(sb, type);
            }
            else
            {
                // Use AhBearStudios serialization service
                sb.AppendLine($"            // Deserialize using AhBearStudios serialization service");
                sb.AppendLine($"            var service = ServiceResolver.Get<ISerializationService>();");
                sb.AppendLine($"            var bytes = reader.ReadArraySegmentAndSize();");
                sb.AppendLine($"            var result = service.Deserialize<{type.FullName}>(bytes.ToArray(), default, SerializationFormat.FishNet);");
            }
            
            if (_options.ValidateDataOrder)
            {
                sb.AppendLine($"            var endMarker = reader.ReadByte();");
                sb.AppendLine($"            if (endMarker != 0xFF)");
                sb.AppendLine($"                throw new Exception(\"Invalid data order: expected end marker\");");
            }
            
            sb.AppendLine($"            return result;");
            sb.AppendLine($"        }}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Generates pooled buffer extension methods for high-performance serialization.
        /// </summary>
        /// <param name="type">Type to generate methods for</param>
        /// <returns>Generated extension methods code</returns>
        public string GeneratePooledBufferExtensions(Type type)
        {
            var sb = new StringBuilder();
            var writeMethodName = $"Write{type.Name}Pooled";
            
            // Generate pooled Write method
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Writes {type.Name} to the network stream using pooled buffers for zero-allocation serialization.");
            sb.AppendLine($"        /// </summary>");
            
            if (_options.UseGlobalCustomSerializers)
            {
                sb.AppendLine($"        [UseGlobalCustomSerializer]");
            }
            
            sb.AppendLine($"        public static void {writeMethodName}(this Writer writer, {type.FullName} value)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            var bufferPool = ServiceResolver.Get<NetworkSerializationBufferPool>();");
            sb.AppendLine($"            var service = ServiceResolver.Get<ISerializationService>();");
            sb.AppendLine();
            
            // Estimate buffer size for different type categories
            var bufferSizeEstimate = EstimateTypeSize(type);
            var bufferMethod = bufferSizeEstimate switch
            {
                <= 1024 => "GetSmallBuffer()",
                <= 16384 => "GetMediumBuffer()",
                _ => "GetLargeBuffer()"
            };
            
            sb.AppendLine($"            // Get appropriately sized buffer from pool");
            sb.AppendLine($"            var buffer = bufferPool.{bufferMethod};");
            sb.AppendLine($"            try");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                // Serialize using MemoryPack via AhBearStudios service");
            sb.AppendLine($"                var bytes = service.Serialize(value, default, SerializationFormat.FishNet);");
            sb.AppendLine($"                buffer.SetData(bytes);");
            sb.AppendLine();
            sb.AppendLine($"                // Write to FishNet stream");
            sb.AppendLine($"                writer.WriteArraySegmentAndSize(buffer.GetData().ToArray());");
            sb.AppendLine($"            }}");
            sb.AppendLine($"            finally");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                // Always return buffer to pool");
            sb.AppendLine($"                bufferPool.ReturnBuffer(buffer);");
            sb.AppendLine($"            }}");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Estimates the serialized size of a type for buffer pool selection.
        /// </summary>
        /// <param name="type">Type to estimate</param>
        /// <returns>Estimated size in bytes</returns>
        private int EstimateTypeSize(Type type)
        {
            // Simple heuristics for common types
            if (type.IsPrimitive)
                return System.Runtime.InteropServices.Marshal.SizeOf(type) * 2;
            
            if (type == typeof(string))
                return 256; // Average string size estimate
            
            if (type.IsValueType)
            {
                try
                {
                    return System.Runtime.InteropServices.Marshal.SizeOf(type) * 2;
                }
                catch
                {
                    return 1024; // Default for complex value types
                }
            }
            
            // For reference types, use larger estimate
            return 4096;
        }
        
        /// <summary>
        /// Generates a complete extension class for all types needing serialization.
        /// </summary>
        /// <returns>Complete C# class code</returns>
        public string GenerateExtensionClass()
        {
            var sb = new StringBuilder();
            
            // File header
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("// This file was automatically generated by FishNetExtensionMethodGenerator");
            sb.AppendLine($"// Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine("// Do not modify this file directly");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine();
            
            // Using statements
            sb.AppendLine("using System;");
            sb.AppendLine("using FishNet.Serializing;");
            sb.AppendLine("using AhBearStudios.Core.Serialization;");
            sb.AppendLine("using AhBearStudios.Core.Serialization.Models;");
            sb.AppendLine("using AhBearStudios.Core.Infrastructure.DependencyInjection;");
            sb.AppendLine("using AhBearStudios.Core.Pooling.Services;");
            sb.AppendLine();
            
            // Namespace
            sb.AppendLine("namespace AhBearStudios.Unity.Serialization.FishNet.Generated");
            sb.AppendLine("{");
            
            // Class declaration
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Auto-generated FishNet custom serializers for AhBearStudios types.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class FishNetCustomSerializers");
            sb.AppendLine("    {");
            
            // Generate methods for each type needing generation
            var typesNeedingGeneration = _typeRegistry.GetTypesNeedingGeneration();
            
            foreach (var type in typesNeedingGeneration.OrderBy(t => t.Name))
            {
                try
                {
                    var methods = GenerateExtensionMethods(type);
                    sb.AppendLine(methods);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to generate methods for {type.Name}: {ex.Message}");
                    sb.AppendLine($"        // ERROR: Failed to generate methods for {type.Name}: {ex.Message}");
                    sb.AppendLine();
                }
            }
            
            // Close class and namespace
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Validates that generated methods will work correctly.
        /// </summary>
        /// <param name="type">Type to validate</param>
        /// <returns>Validation result with any issues found</returns>
        public (bool IsValid, List<string> Issues) ValidateType(Type type)
        {
            var issues = new List<string>();
            
            // Check if type is serializable
            if (!type.IsSerializable && !type.GetCustomAttributes(typeof(SerializableAttribute), true).Any())
            {
                issues.Add($"Type {type.Name} is not marked as [Serializable]");
            }
            
            // Check for circular references
            if (HasCircularReference(type, new HashSet<Type>()))
            {
                issues.Add($"Type {type.Name} has circular references which may cause issues");
            }
            
            // Check for unsupported types
            if (type.IsInterface)
            {
                issues.Add($"Type {type.Name} is an interface and cannot be directly serialized");
            }
            
            if (type.IsAbstract && !type.IsSealed)
            {
                issues.Add($"Type {type.Name} is abstract and may require custom handling");
            }
            
            return (issues.Count == 0, issues);
        }
        
        private string GetSafeTypeName(Type type)
        {
            // Remove generic parameters and special characters
            var name = type.Name;
            
            if (type.IsGenericType)
            {
                var index = name.IndexOf('`');
                if (index > 0)
                {
                    name = name.Substring(0, index);
                }
                
                // Add generic parameter names
                var genericArgs = type.GetGenericArguments();
                name += "_" + string.Join("_", genericArgs.Select(t => GetSafeTypeName(t)));
            }
            
            // Replace special characters
            name = name.Replace(".", "_")
                      .Replace("+", "_")
                      .Replace("<", "_")
                      .Replace(">", "_")
                      .Replace(",", "_")
                      .Replace(" ", "");
            
            return name;
        }
        
        private bool IsSimpleType(Type type)
        {
            return type.IsPrimitive || 
                   type == typeof(string) || 
                   type == typeof(decimal) || 
                   type == typeof(DateTime) || 
                   type == typeof(Guid);
        }
        
        private void GenerateSimpleTypeWrite(StringBuilder sb, Type type, string variableName)
        {
            if (type == typeof(bool))
                sb.AppendLine($"            writer.WriteBoolean({variableName});");
            else if (type == typeof(byte))
                sb.AppendLine($"            writer.WriteByte({variableName});");
            else if (type == typeof(sbyte))
                sb.AppendLine($"            writer.WriteSByte({variableName});");
            else if (type == typeof(short))
                sb.AppendLine($"            writer.WriteInt16({variableName});");
            else if (type == typeof(ushort))
                sb.AppendLine($"            writer.WriteUInt16({variableName});");
            else if (type == typeof(int))
                sb.AppendLine($"            writer.WriteInt32({variableName});");
            else if (type == typeof(uint))
                sb.AppendLine($"            writer.WriteUInt32({variableName});");
            else if (type == typeof(long))
                sb.AppendLine($"            writer.WriteInt64({variableName});");
            else if (type == typeof(ulong))
                sb.AppendLine($"            writer.WriteUInt64({variableName});");
            else if (type == typeof(float))
                sb.AppendLine($"            writer.WriteSingle({variableName});");
            else if (type == typeof(double))
                sb.AppendLine($"            writer.WriteDouble({variableName});");
            else if (type == typeof(string))
                sb.AppendLine($"            writer.WriteString({variableName});");
            else if (type == typeof(DateTime))
                sb.AppendLine($"            writer.WriteInt64({variableName}.Ticks);");
            else if (type == typeof(Guid))
                sb.AppendLine($"            writer.WriteString({variableName}.ToString());");
        }
        
        private void GenerateSimpleTypeRead(StringBuilder sb, Type type)
        {
            sb.Append("            var result = ");
            
            if (type == typeof(bool))
                sb.AppendLine("reader.ReadBoolean();");
            else if (type == typeof(byte))
                sb.AppendLine("reader.ReadByte();");
            else if (type == typeof(sbyte))
                sb.AppendLine("reader.ReadSByte();");
            else if (type == typeof(short))
                sb.AppendLine("reader.ReadInt16();");
            else if (type == typeof(ushort))
                sb.AppendLine("reader.ReadUInt16();");
            else if (type == typeof(int))
                sb.AppendLine("reader.ReadInt32();");
            else if (type == typeof(uint))
                sb.AppendLine("reader.ReadUInt32();");
            else if (type == typeof(long))
                sb.AppendLine("reader.ReadInt64();");
            else if (type == typeof(ulong))
                sb.AppendLine("reader.ReadUInt64();");
            else if (type == typeof(float))
                sb.AppendLine("reader.ReadSingle();");
            else if (type == typeof(double))
                sb.AppendLine("reader.ReadDouble();");
            else if (type == typeof(string))
                sb.AppendLine("reader.ReadString();");
            else if (type == typeof(DateTime))
                sb.AppendLine("new DateTime(reader.ReadInt64());");
            else if (type == typeof(Guid))
                sb.AppendLine("Guid.Parse(reader.ReadString());");
        }
        
        private bool HasCircularReference(Type type, HashSet<Type> visited)
        {
            if (visited.Contains(type))
                return true;
            
            visited.Add(type);
            
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                var propType = prop.PropertyType;
                
                // Skip primitive types and strings
                if (propType.IsPrimitive || propType == typeof(string))
                    continue;
                
                // Check collections
                if (propType.IsGenericType)
                {
                    var genericArgs = propType.GetGenericArguments();
                    foreach (var arg in genericArgs)
                    {
                        if (!arg.IsPrimitive && arg != typeof(string) && HasCircularReference(arg, new HashSet<Type>(visited)))
                            return true;
                    }
                }
                else if (!propType.IsValueType && HasCircularReference(propType, new HashSet<Type>(visited)))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}