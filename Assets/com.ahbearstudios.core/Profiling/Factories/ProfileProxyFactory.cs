using System;
using System.Reflection;
using System.Collections.Generic;
using AhBearStudios.Core.Profiling.Attributes;
using AhBearStudios.Core.Profiling.Interfaces;
using UnityEngine;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Factories
{
    /// <summary>
    /// Factory for creating profile proxies
    /// </summary>
    public static class ProfileProxyFactory
    {
        // Dictionary to keep track of proxies we've created
        private static readonly Dictionary<MethodInfo, IProfileProxy> _methodProxies = 
            new Dictionary<MethodInfo, IProfileProxy>();
        
        /// <summary>
        /// Create a proxy for a method with a ProfileMethod attribute
        /// </summary>
        /// <param name="method">Method to create proxy for</param>
        public static IProfileProxy CreateProxy(MethodInfo method)
        {
            if (method == null)
                return null;
                
            // Check if we already have a proxy for this method
            if (_methodProxies.TryGetValue(method, out var existingProxy))
            {
                return existingProxy;
            }
            
            try
            {
                // Get the profiling attribute
                var attr = method.GetCustomAttribute<ProfileMethodAttribute>();
                if (attr == null)
                    return null;
                
                // Create the proxy
                var proxy = CreateProxyInternal(method, attr);
                if (proxy != null)
                {
                    _methodProxies[method] = proxy;
                }
                
                return proxy;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create proxy for {method.DeclaringType.Name}.{method.Name}: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Create a proxy for a method based on a class attribute
        /// </summary>
        /// <param name="method">Method to create proxy for</param>
        /// <param name="classAttr">Class attribute to use</param>
        public static IProfileProxy CreateProxy(MethodInfo method, ProfileClassAttribute classAttr)
        {
            if (method == null || classAttr == null)
                return null;
                
            // Check if we already have a proxy for this method
            if (_methodProxies.TryGetValue(method, out var existingProxy))
            {
                return existingProxy;
            }
            
            // Skip special methods like property accessors, operator overloads, etc.
            if (method.IsSpecialName || method.IsConstructor)
                return null;
                
            try
            {
                // Determine method name
                string prefix = string.IsNullOrEmpty(classAttr.Prefix) 
                    ? method.DeclaringType.Name 
                    : classAttr.Prefix;
                    
                // Create a synthetic attribute
                var syntheticAttr = new ProfileMethodAttribute(classAttr.Category, $"{prefix}.{method.Name}");
                
                // Create the proxy
                var proxy = CreateProxyInternal(method, syntheticAttr);
                if (proxy != null)
                {
                    _methodProxies[method] = proxy;
                }
                
                return proxy;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create proxy for {method.DeclaringType.Name}.{method.Name}: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Apply profiling to all methods in an instance
        /// </summary>
        /// <param name="instance">Object instance to profile</param>
        /// <param name="classAttr">Class attribute to use</param>
        public static void ApplyProfilingToInstance(object instance, ProfileClassAttribute classAttr)
        {
            if (instance == null || classAttr == null)
                return;
                
            var type = instance.GetType();
            
            // Determine which binding flags to use
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            
            if (classAttr.IncludePrivate)
            {
                flags |= BindingFlags.NonPublic;
            }
            
            if (classAttr.IncludeInherited)
            {
                // Get all methods including inherited
                var methods = type.GetMethods(flags);
                
                foreach (var method in methods)
                {
                    // Skip if method has DoNotProfile attribute
                    if (method.GetCustomAttribute<DoNotProfileAttribute>() != null)
                        continue;
                        
                    // Create or get proxy
                    IProfileProxy proxy;
                    
                    // Check if method has its own ProfileMethod attribute
                    var methodAttr = method.GetCustomAttribute<ProfileMethodAttribute>();
                    if (methodAttr != null)
                    {
                        proxy = CreateProxy(method);
                    }
                    else
                    {
                        proxy = CreateProxy(method, classAttr);
                    }
                    
                    // Apply proxy to instance
                    proxy?.ApplyToInstance(instance);
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
                        
                    // Create or get proxy
                    IProfileProxy proxy;
                    
                    // Check if method has its own ProfileMethod attribute
                    var methodAttr = method.GetCustomAttribute<ProfileMethodAttribute>();
                    if (methodAttr != null)
                    {
                        proxy = CreateProxy(method);
                    }
                    else
                    {
                        proxy = CreateProxy(method, classAttr);
                    }
                    
                    // Apply proxy to instance
                    proxy?.ApplyToInstance(instance);
                }
            }
        }
        
        /// <summary>
        /// Internal method to create a proxy
        /// </summary>
        private static IProfileProxy CreateProxyInternal(MethodInfo method, ProfileMethodAttribute attr)
        {
            // We can't create proxies for generic methods, abstract methods, or static methods yet
            if (method.IsGenericMethod || method.IsAbstract || method.IsStatic)
                return null;
                
            // Create appropriate proxy based on method signature
            // We use different types based on return type and parameter count
            
            Type proxyType;
            
            if (method.ReturnType == typeof(void))
            {
                // Void return type
                switch (method.GetParameters().Length)
                {
                    case 0:
                        proxyType = typeof(ActionProfileProxy);
                        break;
                    case 1:
                        proxyType = typeof(ActionProfileProxy<>).MakeGenericType(method.GetParameters()[0].ParameterType);
                        break;
                    case 2:
                        proxyType = typeof(ActionProfileProxy<,>).MakeGenericType(
                            method.GetParameters()[0].ParameterType,
                            method.GetParameters()[1].ParameterType);
                        break;
                    default:
                        // For methods with more than 2 parameters, we'll use a simple runtime wrapper
                        return new RuntimeMethodProxy(method, attr);
                }
            }
            else
            {
                // Function with return type
                Type returnType = method.ReturnType;
                
                switch (method.GetParameters().Length)
                {
                    case 0:
                        proxyType = typeof(FuncProfileProxy<>).MakeGenericType(returnType);
                        break;
                    case 1:
                        proxyType = typeof(FuncProfileProxy<,>).MakeGenericType(
                            method.GetParameters()[0].ParameterType,
                            returnType);
                        break;
                    case 2:
                        proxyType = typeof(FuncProfileProxy<,,>).MakeGenericType(
                            method.GetParameters()[0].ParameterType,
                            method.GetParameters()[1].ParameterType,
                            returnType);
                        break;
                    default:
                        // For methods with more than 2 parameters, we'll use a simple runtime wrapper
                        return new RuntimeMethodProxy(method, attr);
                }
            }
            
            // Create the proxy instance
            try
            {
                var proxy = (IProfileProxy)Activator.CreateInstance(proxyType, method, attr);
                return proxy;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create proxy for {method.DeclaringType.Name}.{method.Name}: {e.Message}");
                return null;
            }
        }
    }
    
    #region Proxy Implementations
    
    /// <summary>
    /// Base class for all profile proxies
    /// </summary>
    public abstract class ProfileProxyBase : IProfileProxy
    {
        /// <summary>
        /// Original method this proxy wraps
        /// </summary>
        public MethodInfo TargetMethod { get; }
        
        /// <summary>
        /// Profiler tag used for this method
        /// </summary>
        public ProfilerTag Tag { get; }
        
        /// <summary>
        /// ProfilerMarker for this method
        /// </summary>
        protected ProfilerMarker Marker { get; }
        
        /// <summary>
        /// Create a new profile proxy
        /// </summary>
        /// <param name="targetMethod">Method to profile</param>
        /// <param name="attr">Profile attribute</param>
        protected ProfileProxyBase(MethodInfo targetMethod, ProfileMethodAttribute attr)
        {
            TargetMethod = targetMethod;
            
            // Create the profiler tag
            string name = attr.Name;
            if (string.IsNullOrEmpty(name))
            {
                name = targetMethod.Name;
            }
            
            Tag = new ProfilerTag(attr.Category, $"{targetMethod.DeclaringType.Name}.{name}");
            
            // Create the profiler marker
            Marker = new ProfilerMarker(Tag.FullName);
        }
        
        /// <summary>
        /// Apply this proxy to an instance
        /// </summary>
        public abstract void ApplyToInstance(object instance);
    }
    
    /// <summary>
    /// Simple runtime method proxy that uses reflection
    /// </summary>
    public class RuntimeMethodProxy : ProfileProxyBase
    {
        /// <summary>
        /// Create a new runtime method proxy
        /// </summary>
        public RuntimeMethodProxy(MethodInfo targetMethod, ProfileMethodAttribute attr)
            : base(targetMethod, attr)
        {
        }
        
        /// <summary>
        /// Apply this proxy to an instance
        /// </summary>
        public override void ApplyToInstance(object instance)
        {
            // No-op, as this proxy doesn't support direct injection
            // It will be used at runtime via the AttributeProfilerWeaver
        }
    }
    
    /// <summary>
    /// Profile proxy for Action methods (void return, no params)
    /// </summary>
    public class ActionProfileProxy : ProfileProxyBase
    {
        private delegate void MethodDelegate(object target);
        private readonly MethodDelegate _originalMethod;
        
        /// <summary>
        /// Create a new action profile proxy
        /// </summary>
        public ActionProfileProxy(MethodInfo targetMethod, ProfileMethodAttribute attr)
            : base(targetMethod, attr)
        {
            // Create a delegate for faster invocation
            _originalMethod = (MethodDelegate)Delegate.CreateDelegate(
                typeof(MethodDelegate), targetMethod);
        }
        
        /// <summary>
        /// Apply this proxy to an instance
        /// </summary>
        public override void ApplyToInstance(object instance)
        {
            // Runtime interception not supported yet
        }
        
        /// <summary>
        /// Invoke the method with profiling
        /// </summary>
        public void Invoke(object target)
        {
            Marker.Begin();
            try
            {
                _originalMethod(target);
            }
            finally
            {
                Marker.End();
            }
        }
    }
    
    /// <summary>
    /// Profile proxy for Action methods (void return, 1 param)
    /// </summary>
    public class ActionProfileProxy<T1> : ProfileProxyBase
    {
        private delegate void MethodDelegate(object target, T1 arg1);
        private readonly MethodDelegate _originalMethod;
        
        /// <summary>
        /// Create a new action profile proxy
        /// </summary>
        public ActionProfileProxy(MethodInfo targetMethod, ProfileMethodAttribute attr)
            : base(targetMethod, attr)
        {
            // Create a delegate for faster invocation using DynamicInvoke
            _originalMethod = (target, arg1) => targetMethod.Invoke(target, new object[] { arg1 });
        }
        
        /// <summary>
        /// Apply this proxy to an instance
        /// </summary>
        public override void ApplyToInstance(object instance)
        {
            // Runtime interception not supported yet
        }
        
        /// <summary>
        /// Invoke the method with profiling
        /// </summary>
        public void Invoke(object target, T1 arg1)
        {
            Marker.Begin();
            try
            {
                _originalMethod(target, arg1);
            }
            finally
            {
                Marker.End();
            }
        }
    }
    
    /// <summary>
    /// Profile proxy for Action methods (void return, 2 params)
    /// </summary>
    public class ActionProfileProxy<T1, T2> : ProfileProxyBase
    {
        private delegate void MethodDelegate(object target, T1 arg1, T2 arg2);
        private readonly MethodDelegate _originalMethod;
        
        /// <summary>
        /// Create a new action profile proxy
        /// </summary>
        public ActionProfileProxy(MethodInfo targetMethod, ProfileMethodAttribute attr)
            : base(targetMethod, attr)
        {
            // Create a delegate for faster invocation using DynamicInvoke
            _originalMethod = (target, arg1, arg2) => targetMethod.Invoke(target, new object[] { arg1, arg2 });
        }
        
        /// <summary>
        /// Apply this proxy to an instance
        /// </summary>
        public override void ApplyToInstance(object instance)
        {
            // Runtime interception not supported yet
        }
        
        /// <summary>
        /// Invoke the method with profiling
        /// </summary>
        public void Invoke(object target, T1 arg1, T2 arg2)
        {
            Marker.Begin();
            try
            {
                _originalMethod(target, arg1, arg2);
            }
            finally
            {
                Marker.End();
            }
        }
    }
    
    /// <summary>
    /// Profile proxy for Func methods (with return, no params)
    /// </summary>
    public class FuncProfileProxy<TResult> : ProfileProxyBase
    {
        private delegate TResult MethodDelegate(object target);
        private readonly MethodDelegate _originalMethod;
        
        /// <summary>
        /// Create a new func profile proxy
        /// </summary>
        public FuncProfileProxy(MethodInfo targetMethod, ProfileMethodAttribute attr)
            : base(targetMethod, attr)
        {
            // Create a delegate for faster invocation using DynamicInvoke
            _originalMethod = target => (TResult)targetMethod.Invoke(target, null);
        }
        
        /// <summary>
        /// Apply this proxy to an instance
        /// </summary>
        public override void ApplyToInstance(object instance)
        {
            // Runtime interception not supported yet
        }
        
        /// <summary>
        /// Invoke the method with profiling
        /// </summary>
        public TResult Invoke(object target)
        {
            Marker.Begin();
            try
            {
                return _originalMethod(target);
            }
            finally
            {
                Marker.End();
            }
        }
    }
    
    /// <summary>
    /// Profile proxy for Func methods (with return, 1 param)
    /// </summary>
    public class FuncProfileProxy<T1, TResult> : ProfileProxyBase
    {
        private delegate TResult MethodDelegate(object target, T1 arg1);
        private readonly MethodDelegate _originalMethod;
        
        /// <summary>
        /// Create a new func profile proxy
        /// </summary>
        public FuncProfileProxy(MethodInfo targetMethod, ProfileMethodAttribute attr)
            : base(targetMethod, attr)
        {
            // Create a delegate for faster invocation using DynamicInvoke
            _originalMethod = (target, arg1) => (TResult)targetMethod.Invoke(target, new object[] { arg1 });
        }
        
        /// <summary>
        /// Apply this proxy to an instance
        /// </summary>
        public override void ApplyToInstance(object instance)
        {
            // Runtime interception not supported yet
        }
        
        /// <summary>
        /// Invoke the method with profiling
        /// </summary>
        public TResult Invoke(object target, T1 arg1)
        {
            Marker.Begin();
            try
            {
                return _originalMethod(target, arg1);
            }
            finally
            {
                Marker.End();
            }
        }
    }
    
    /// <summary>
    /// Profile proxy for Func methods (with return, 2 params)
    /// </summary>
    public class FuncProfileProxy<T1, T2, TResult> : ProfileProxyBase
    {
        private delegate TResult MethodDelegate(object target, T1 arg1, T2 arg2);
        private readonly MethodDelegate _originalMethod;
        
        /// <summary>
        /// Create a new func profile proxy
        /// </summary>
        public FuncProfileProxy(MethodInfo targetMethod, ProfileMethodAttribute attr)
            : base(targetMethod, attr)
        {
            // Create a delegate for faster invocation using DynamicInvoke
            _originalMethod = (target, arg1, arg2) => (TResult)targetMethod.Invoke(target, new object[] { arg1, arg2 });
        }
        
        /// <summary>
        /// Apply this proxy to an instance
        /// </summary>
        public override void ApplyToInstance(object instance)
        {
            // Runtime interception not supported yet
        }
        
        /// <summary>
        /// Invoke the method with profiling
        /// </summary>
        public TResult Invoke(object target, T1 arg1, T2 arg2)
        {
            Marker.Begin();
            try
            {
                return _originalMethod(target, arg1, arg2);
            }
            finally
            {
                Marker.End();
            }
        }
    }
    
    #endregion
}