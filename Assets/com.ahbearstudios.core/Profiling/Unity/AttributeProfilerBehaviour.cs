using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace AhBearStudios.Core.Profiling.Unity
{
    /// <summary>
    /// Component that handles runtime attribute-based profiling
    /// </summary>
    public class AttributeProfilerBehaviour : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool _autoScanAssemblies = true;
        [SerializeField] private bool _scanOnAwake = true;
        [SerializeField] private bool _enableRuntimeProxy = true;
        [SerializeField] private bool _logScannedTypes = false;
        
        [Header("Assembly Filtering")]
        [SerializeField] private string[] _assemblyPrefixWhitelist = { "Assembly-CSharp" };
        [SerializeField] private string[] _assemblyPrefixBlacklist = { "Unity.", "System.", "mscorlib" };
        
        // Types and methods found with profiling attributes
        private readonly List<Type> _profiledTypes = new List<Type>();
        private readonly List<MethodInfo> _profiledMethods = new List<MethodInfo>();
        
        // List of proxies created for runtime monitoring
        private readonly List<IProfileProxy> _proxies = new List<IProfileProxy>();
        
        private void Awake()
        {
            if (_scanOnAwake)
            {
                ScanForProfiledMembers();
            }
        }
        
        /// <summary>
        /// Scan loaded assemblies for profiled types and methods
        /// </summary>
        public void ScanForProfiledMembers()
        {
            if (!_autoScanAssemblies)
                return;
                
            _profiledTypes.Clear();
            _profiledMethods.Clear();
            
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                // Skip assemblies that don't match our filters
                if (!ShouldProcessAssembly(assembly))
                    continue;
                    
                try
                {
                    // Find types with ProfileClass attribute
                    var profiledTypesInAssembly = assembly.GetTypes()
                        .Where(t => t.GetCustomAttribute<ProfileClassAttribute>() != null)
                        .ToList();
                        
                    _profiledTypes.AddRange(profiledTypesInAssembly);
                    
                    // Find methods with ProfileMethod attribute
                    var profiledMethodsInAssembly = assembly.GetTypes()
                        .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                        .Where(m => m.GetCustomAttribute<ProfileMethodAttribute>() != null)
                        .ToList();
                        
                    _profiledMethods.AddRange(profiledMethodsInAssembly);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error scanning assembly {assembly.FullName}: {e.Message}");
                }
            }
            
            if (_logScannedTypes)
            {
                Debug.Log($"Found {_profiledTypes.Count} types and {_profiledMethods.Count} methods with profiling attributes");
                
                foreach (var type in _profiledTypes)
                {
                    Debug.Log($"Profiled type: {type.FullName}");
                }
                
                foreach (var method in _profiledMethods)
                {
                    Debug.Log($"Profiled method: {method.DeclaringType.FullName}.{method.Name}");
                }
            }
            
            // Create proxies if enabled
            if (_enableRuntimeProxy)
            {
                CreateProxies();
            }
        }
        
        /// <summary>
        /// Create runtime proxies for profiled methods
        /// </summary>
        private void CreateProxies()
        {
            _proxies.Clear();
            
            // For standalone methods
            foreach (var method in _profiledMethods)
            {
                var proxy = ProfileProxyFactory.CreateProxy(method);
                if (proxy != null)
                {
                    _proxies.Add(proxy);
                }
            }
            
            // For methods in profiled classes
            foreach (var type in _profiledTypes)
            {
                var classAttr = type.GetCustomAttribute<ProfileClassAttribute>();
                
                // Determine which binding flags to use
                BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
                
                if (classAttr.IncludePrivate)
                {
                    flags |= BindingFlags.NonPublic;
                }
                
                if (classAttr.IncludeInherited)
                {
                    // For inherited methods, we need to get methods from base types too
                    var methods = type.GetMethods(flags);
                    
                    foreach (var method in methods)
                    {
                        // Skip if method has DoNotProfile attribute
                        if (method.GetCustomAttribute<DoNotProfileAttribute>() != null)
                            continue;
                            
                        // Skip if method already has ProfileMethod attribute (it will be handled separately)
                        if (method.GetCustomAttribute<ProfileMethodAttribute>() != null)
                            continue;
                            
                        var proxy = ProfileProxyFactory.CreateProxy(method, classAttr);
                        if (proxy != null)
                        {
                            _proxies.Add(proxy);
                        }
                    }
                }
                else
                {
                    // Only get methods declared in this type
                    flags |= BindingFlags.DeclaredOnly;
                    var methods = type.GetMethods(flags);
                    
                    foreach (var method in methods)
                    {
                        // Skip if method has DoNotProfile attribute
                        if (method.GetCustomAttribute<DoNotProfileAttribute>() != null)
                            continue;
                            
                        // Skip if method already has ProfileMethod attribute (it will be handled separately)
                        if (method.GetCustomAttribute<ProfileMethodAttribute>() != null)
                            continue;
                            
                        var proxy = ProfileProxyFactory.CreateProxy(method, classAttr);
                        if (proxy != null)
                        {
                            _proxies.Add(proxy);
                        }
                    }
                }
            }
            
            if (_logScannedTypes)
            {
                Debug.Log($"Created {_proxies.Count} proxy wrappers");
            }
        }
        
        /// <summary>
        /// Apply profiling to a specific instance of a type
        /// </summary>
        /// <param name="instance">Object instance to profile</param>
        public void ProfileInstance(object instance)
        {
            if (instance == null)
                return;
                
            var type = instance.GetType();
            
            // Check if type has ProfileClass attribute
            var classAttr = type.GetCustomAttribute<ProfileClassAttribute>();
            if (classAttr == null)
                return;
                
            // Hook methods
            ProfileProxyFactory.ApplyProfilingToInstance(instance, classAttr);
        }
        
        /// <summary>
        /// Should the assembly be processed
        /// </summary>
        private bool ShouldProcessAssembly(Assembly assembly)
        {
            string assemblyName = assembly.GetName().Name;
            
            // Check blacklist first
            foreach (var prefix in _assemblyPrefixBlacklist)
            {
                if (assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            
            // Then check whitelist
            foreach (var prefix in _assemblyPrefixWhitelist)
            {
                if (assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            // If whitelist is empty, accept all non-blacklisted
            return _assemblyPrefixWhitelist.Length == 0;
        }
    }
}