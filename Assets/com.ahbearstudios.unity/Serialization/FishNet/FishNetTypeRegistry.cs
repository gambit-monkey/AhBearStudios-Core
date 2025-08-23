using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Logging;
using Unity.Collections;

namespace AhBearStudios.Unity.Serialization.FishNet
{
    /// <summary>
    /// Manages type registration for FishNet custom serialization.
    /// Tracks which types have FishNet serializers and facilitates generation of extension methods.
    /// </summary>
    public class FishNetTypeRegistry
    {
        private readonly ILoggingService _logger;
        private readonly Dictionary<Type, TypeSerializationInfo> _registeredTypes;
        private readonly HashSet<Assembly> _scannedAssemblies;
        private readonly object _lock = new object();
        
        /// <summary>
        /// Information about a registered type's serialization capabilities.
        /// </summary>
        public class TypeSerializationInfo
        {
            public Type Type { get; set; }
            public bool HasWriteMethod { get; set; }
            public bool HasReadMethod { get; set; }
            public bool IsGlobalSerializer { get; set; }
            public MethodInfo WriteMethod { get; set; }
            public MethodInfo ReadMethod { get; set; }
            public DateTime RegisteredAt { get; set; }
        }
        
        /// <summary>
        /// Initializes a new instance of FishNetTypeRegistry.
        /// </summary>
        /// <param name="logger">Logging service</param>
        public FishNetTypeRegistry(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registeredTypes = new Dictionary<Type, TypeSerializationInfo>();
            _scannedAssemblies = new HashSet<Assembly>();
        }
        
        /// <summary>
        /// Registers a type for FishNet serialization and checks for existing methods.
        /// </summary>
        /// <typeparam name="T">The type to register</typeparam>
        /// <returns>Registration info for the type</returns>
        public TypeSerializationInfo RegisterType<T>()
        {
            return RegisterType(typeof(T));
        }
        
        /// <summary>
        /// Registers a type for FishNet serialization and checks for existing methods.
        /// </summary>
        /// <param name="type">The type to register</param>
        /// <returns>Registration info for the type</returns>
        public TypeSerializationInfo RegisterType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            
            lock (_lock)
            {
                if (_registeredTypes.TryGetValue(type, out var existing))
                    return existing;
                
                var info = new TypeSerializationInfo
                {
                    Type = type,
                    RegisteredAt = DateTime.UtcNow
                };
                
                // Scan for existing FishNet serialization methods
                ScanForSerializationMethods(type, info);
                
                _registeredTypes[type] = info;
                
                _logger.LogInfo($"Registered type {type.Name} for FishNet serialization. " +
                               $"HasWrite: {info.HasWriteMethod}, HasRead: {info.HasReadMethod}");
                
                return info;
            }
        }
        
        /// <summary>
        /// Gets all registered types.
        /// </summary>
        /// <returns>Collection of registered type information</returns>
        public IReadOnlyCollection<TypeSerializationInfo> GetRegisteredTypes()
        {
            lock (_lock)
            {
                return _registeredTypes.Values.ToList();
            }
        }
        
        /// <summary>
        /// Gets types that need serialization methods generated.
        /// </summary>
        /// <returns>Types missing Write or Read methods</returns>
        public IReadOnlyCollection<Type> GetTypesNeedingGeneration()
        {
            lock (_lock)
            {
                return _registeredTypes
                    .Where(kvp => !kvp.Value.HasWriteMethod || !kvp.Value.HasReadMethod)
                    .Select(kvp => kvp.Key)
                    .ToList();
            }
        }
        
        /// <summary>
        /// Checks if a type is registered.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if registered</returns>
        public bool IsTypeRegistered(Type type)
        {
            lock (_lock)
            {
                return _registeredTypes.ContainsKey(type);
            }
        }
        
        /// <summary>
        /// Gets serialization info for a type.
        /// </summary>
        /// <param name="type">The type to query</param>
        /// <returns>Serialization info or null if not registered</returns>
        public TypeSerializationInfo GetTypeInfo(Type type)
        {
            lock (_lock)
            {
                return _registeredTypes.TryGetValue(type, out var info) ? info : null;
            }
        }
        
        /// <summary>
        /// Scans assemblies for types with existing FishNet serializers.
        /// </summary>
        /// <param name="assemblies">Assemblies to scan</param>
        public void ScanAssemblies(params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
                return;
            
            foreach (var assembly in assemblies)
            {
                if (assembly == null || _scannedAssemblies.Contains(assembly))
                    continue;
                
                try
                {
                    _logger.LogInfo($"Scanning assembly {assembly.GetName().Name} for FishNet serializers");
                    
                    // Look for static classes with extension methods
                    var extensionTypes = assembly.GetTypes()
                        .Where(t => t.IsSealed && t.IsAbstract && !t.IsNested); // Static classes
                    
                    foreach (var extensionType in extensionTypes)
                    {
                        ScanTypeForSerializationMethods(extensionType);
                    }
                    
                    _scannedAssemblies.Add(assembly);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to scan assembly {assembly.GetName().Name}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Generates a report of all registered types and their serialization status.
        /// </summary>
        /// <returns>Formatted report string</returns>
        public string GenerateReport()
        {
            lock (_lock)
            {
                var report = new System.Text.StringBuilder();
                report.AppendLine("FishNet Type Registry Report");
                report.AppendLine("============================");
                report.AppendLine($"Total Registered Types: {_registeredTypes.Count}");
                report.AppendLine($"Types with Complete Serialization: {_registeredTypes.Count(kvp => kvp.Value.HasWriteMethod && kvp.Value.HasReadMethod)}");
                report.AppendLine($"Types Needing Generation: {_registeredTypes.Count(kvp => !kvp.Value.HasWriteMethod || !kvp.Value.HasReadMethod)}");
                report.AppendLine();
                
                foreach (var kvp in _registeredTypes.OrderBy(k => k.Key.Name))
                {
                    var info = kvp.Value;
                    report.AppendLine($"  {info.Type.Name}:");
                    report.AppendLine($"    - Has Write: {info.HasWriteMethod}");
                    report.AppendLine($"    - Has Read: {info.HasReadMethod}");
                    report.AppendLine($"    - Is Global: {info.IsGlobalSerializer}");
                    report.AppendLine($"    - Registered: {info.RegisteredAt:yyyy-MM-dd HH:mm:ss}");
                }
                
                return report.ToString();
            }
        }
        
        private void ScanForSerializationMethods(Type targetType, TypeSerializationInfo info)
        {
            // Get all loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                if (_scannedAssemblies.Contains(assembly))
                    continue;
                
                try
                {
                    // Look for extension methods in static classes
                    var extensionTypes = assembly.GetTypes()
                        .Where(t => t.IsSealed && t.IsAbstract && !t.IsNested);
                    
                    foreach (var extensionType in extensionTypes)
                    {
                        // Look for Write method
                        var writeMethods = extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .Where(m => m.Name.StartsWith("Write") && 
                                       m.GetParameters().Length >= 2 &&
                                       m.GetParameters()[1].ParameterType == targetType);
                        
                        foreach (var writeMethod in writeMethods)
                        {
                            info.HasWriteMethod = true;
                            info.WriteMethod = writeMethod;
                            
                            // Check for [UseGlobalCustomSerializer] attribute
                            info.IsGlobalSerializer = writeMethod.GetCustomAttributes()
                                .Any(a => a.GetType().Name == "UseGlobalCustomSerializerAttribute");
                            
                            _logger.LogDebug($"Found Write method for {targetType.Name} in {extensionType.Name}");
                        }
                        
                        // Look for Read method
                        var readMethods = extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .Where(m => m.Name.StartsWith("Read") && 
                                       m.ReturnType == targetType);
                        
                        foreach (var readMethod in readMethods)
                        {
                            info.HasReadMethod = true;
                            info.ReadMethod = readMethod;
                            
                            _logger.LogDebug($"Found Read method for {targetType.Name} in {extensionType.Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"Error scanning assembly {assembly.GetName().Name}: {ex.Message}");
                }
            }
        }
        
        private void ScanTypeForSerializationMethods(Type extensionType)
        {
            // Scan all methods in the extension type
            var methods = extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            
            foreach (var method in methods)
            {
                // Check if it's a Write method
                if (method.Name.StartsWith("Write") && method.GetParameters().Length >= 2)
                {
                    var targetType = method.GetParameters()[1].ParameterType;
                    
                    if (!_registeredTypes.TryGetValue(targetType, out var info))
                    {
                        info = new TypeSerializationInfo
                        {
                            Type = targetType,
                            RegisteredAt = DateTime.UtcNow
                        };
                        _registeredTypes[targetType] = info;
                    }
                    
                    info.HasWriteMethod = true;
                    info.WriteMethod = method;
                    info.IsGlobalSerializer = method.GetCustomAttributes()
                        .Any(a => a.GetType().Name == "UseGlobalCustomSerializerAttribute");
                }
                // Check if it's a Read method
                else if (method.Name.StartsWith("Read") && method.ReturnType != typeof(void))
                {
                    var targetType = method.ReturnType;
                    
                    if (!_registeredTypes.TryGetValue(targetType, out var info))
                    {
                        info = new TypeSerializationInfo
                        {
                            Type = targetType,
                            RegisteredAt = DateTime.UtcNow
                        };
                        _registeredTypes[targetType] = info;
                    }
                    
                    info.HasReadMethod = true;
                    info.ReadMethod = method;
                }
            }
        }
    }
}