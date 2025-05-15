using System;
using System.Reflection;
using AhBearStudios.Core.Profiling.Attributes;

namespace AhBearStudios.Core.Profiling.Factories
{
    /// <summary>
    /// Helper class to manually invoke profiled methods via attributes
    /// </summary>
    public static class ProfileInvoker
    {
        /// <summary>
        /// Invoke a method with profiling via attributes
        /// </summary>
        /// <param name="instance">Object instance</param>
        /// <param name="methodName">Method name to invoke</param>
        public static void InvokeMethod(object instance, string methodName)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
                
            if (string.IsNullOrEmpty(methodName))
                throw new ArgumentNullException(nameof(methodName));
                
            var type = instance.GetType();
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (method == null)
                throw new MissingMethodException(type.Name, methodName);
                
            InvokeMethod(instance, method);
        }
        
        /// <summary>
        /// Invoke a method with profiling via attributes
        /// </summary>
        /// <param name="instance">Object instance</param>
        /// <param name="method">Method to invoke</param>
        public static void InvokeMethod(object instance, MethodInfo method)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
                
            if (method == null)
                throw new ArgumentNullException(nameof(method));
                
            // Try to get profiler session
            using (AttributeProfilerWeaver.Instance.BeginMethodProfile(method))
            {
                method.Invoke(instance, null);
            }
        }
        
        /// <summary>
        /// Invoke a method with profiling via attributes
        /// </summary>
        /// <param name="instance">Object instance</param>
        /// <param name="methodName">Method name to invoke</param>
        /// <param name="arg1">First argument</param>
        public static void InvokeMethod<T1>(object instance, string methodName, T1 arg1)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
                
            if (string.IsNullOrEmpty(methodName))
                throw new ArgumentNullException(nameof(methodName));
                
            var type = instance.GetType();
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, 
                null, new[] { typeof(T1) }, null);
            
            if (method == null)
                throw new MissingMethodException(type.Name, methodName);
                
            InvokeMethod(instance, method, arg1);
        }
        
        /// <summary>
        /// Invoke a method with profiling via attributes
        /// </summary>
        /// <param name="instance">Object instance</param>
        /// <param name="method">Method to invoke</param>
        /// <param name="arg1">First argument</param>
        public static void InvokeMethod<T1>(object instance, MethodInfo method, T1 arg1)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
                
            if (method == null)
                throw new ArgumentNullException(nameof(method));
                
            // Try to get profiler session
            using (AttributeProfilerWeaver.Instance.BeginMethodProfile(method))
            {
                method.Invoke(instance, new object[] { arg1 });
            }
        }
        
        /// <summary>
        /// Invoke a method with profiling via attributes
        /// </summary>
        /// <param name="instance">Object instance</param>
        /// <param name="methodName">Method name to invoke</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        public static void InvokeMethod<T1, T2>(object instance, string methodName, T1 arg1, T2 arg2)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
                
            if (string.IsNullOrEmpty(methodName))
                throw new ArgumentNullException(nameof(methodName));
                
            var type = instance.GetType();
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, 
                null, new[] { typeof(T1), typeof(T2) }, null);
            
            if (method == null)
                throw new MissingMethodException(type.Name, methodName);
                
            InvokeMethod(instance, method, arg1, arg2);
        }
        
        /// <summary>
        /// Invoke a method with profiling via attributes
        /// </summary>
        /// <param name="instance">Object instance</param>
        /// <param name="method">Method to invoke</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        public static void InvokeMethod<T1, T2>(object instance, MethodInfo method, T1 arg1, T2 arg2)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
                
            if (method == null)
                throw new ArgumentNullException(nameof(method));
                
            // Try to get profiler session
            using (AttributeProfilerWeaver.Instance.BeginMethodProfile(method))
            {
                method.Invoke(instance, new object[] { arg1, arg2 });
            }
        }
        
        /// <summary>
        /// Invoke a method with profiling via attributes and return a result
        /// </summary>
        /// <param name="instance">Object instance</param>
        /// <param name="methodName">Method name to invoke</param>
        /// <returns>Method result</returns>
        public static TResult InvokeFunction<TResult>(object instance, string methodName)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
                
            if (string.IsNullOrEmpty(methodName))
                throw new ArgumentNullException(nameof(methodName));
                
            var type = instance.GetType();
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (method == null)
                throw new MissingMethodException(type.Name, methodName);
                
            return InvokeFunction<TResult>(instance, method);
        }
        
        /// <summary>
        /// Invoke a method with profiling via attributes and return a result
        /// </summary>
        /// <param name="instance">Object instance</param>
        /// <param name="method">Method to invoke</param>
        /// <returns>Method result</returns>
        public static TResult InvokeFunction<TResult>(object instance, MethodInfo method)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
                
            if (method == null)
                throw new ArgumentNullException(nameof(method));
                
            // Try to get profiler session
            using (AttributeProfilerWeaver.Instance.BeginMethodProfile(method))
            {
                return (TResult)method.Invoke(instance, null);
            }
        }
        
        /// <summary>
        /// Invoke a method with profiling via attributes and return a result
        /// </summary>
        /// <param name="instance">Object instance</param>
        /// <param name="methodName">Method name to invoke</param>
        /// <param name="arg1">First argument</param>
        /// <returns>Method result</returns>
        public static TResult InvokeFunction<T1, TResult>(object instance, string methodName, T1 arg1)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
                
            if (string.IsNullOrEmpty(methodName))
                throw new ArgumentNullException(nameof(methodName));
                
            var type = instance.GetType();
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, 
                null, new[] { typeof(T1) }, null);
            
            if (method == null)
                throw new MissingMethodException(type.Name, methodName);
                
            return InvokeFunction<T1, TResult>(instance, method, arg1);
        }
        
        /// <summary>
        /// Invoke a method with profiling via attributes and return a result
        /// </summary>
        /// <param name="instance">Object instance</param>
        /// <param name="method">Method to invoke</param>
        /// <param name="arg1">First argument</param>
        /// <returns>Method result</returns>
        public static TResult InvokeFunction<T1, TResult>(object instance, MethodInfo method, T1 arg1)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
                
            if (method == null)
                throw new ArgumentNullException(nameof(method));
                
            // Try to get profiler session
            using (AttributeProfilerWeaver.Instance.BeginMethodProfile(method))
            {
                return (TResult)method.Invoke(instance, new object[] { arg1 });
            }
        }
        
        /// <summary>
        /// Invoke a method with profiling via attributes and return a result
        /// </summary>
        /// <param name="instance">Object instance</param>
        /// <param name="methodName">Method name to invoke</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <returns>Method result</returns>
        public static TResult InvokeFunction<T1, T2, TResult>(object instance, string methodName, T1 arg1, T2 arg2)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
                
            if (string.IsNullOrEmpty(methodName))
                throw new ArgumentNullException(nameof(methodName));
                
            var type = instance.GetType();
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, 
                null, new[] { typeof(T1), typeof(T2) }, null);
            
            if (method == null)
                throw new MissingMethodException(type.Name, methodName);
                
            return InvokeFunction<T1, T2, TResult>(instance, method, arg1, arg2);
        }
        
        /// <summary>
        /// Invoke a method with profiling via attributes and return a result
        /// </summary>
        /// <param name="instance">Object instance</param>
        /// <param name="method">Method to invoke</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <returns>Method result</returns>
        public static TResult InvokeFunction<T1, T2, TResult>(object instance, MethodInfo method, T1 arg1, T2 arg2)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
                
            if (method == null)
                throw new ArgumentNullException(nameof(method));
                
            // Try to get profiler session
            using (AttributeProfilerWeaver.Instance.BeginMethodProfile(method))
            {
                return (TResult)method.Invoke(instance, new object[] { arg1, arg2 });
            }
        }
    }
}