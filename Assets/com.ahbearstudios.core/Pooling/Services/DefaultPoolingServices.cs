using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;
using AhBearStudios.Pooling.Factories;
using AhBearStudios.Pooling.Pools.Native;
using UnityEngine;

namespace AhBearStudios.Pooling.Services
{
    /// <summary>
    /// Default implementation of the pooling service locator following the singleton pattern.
    /// Provides centralized access to all pooling system services.
    /// </summary>
    public class DefaultPoolingServices : IPoolingServiceLocator
    {
        // Singleton instance with thread safety using double-check locking
        private static DefaultPoolingServices s_instance;
        private static readonly object s_lock = new object();
        
        /// <summary>
        /// Gets the singleton instance of the DefaultPoolingServices
        /// </summary>
        public static DefaultPoolingServices Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_lock)
                    {
                        if (s_instance == null)
                        {
                            s_instance = new DefaultPoolingServices();
                        }
                    }
                }
                return s_instance;
            }
        }
        
        // Dictionary to store registered services
        private readonly Dictionary<Type, object> _services;
        private readonly Dictionary<Type, List<object>> _multiServices;
        private bool _isInitialized;
        
        /// <summary>
        /// Gets whether the service locator has been initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Private constructor to prevent external instantiation
        /// </summary>
        private DefaultPoolingServices()
        {
            _services = new Dictionary<Type, object>();
            _multiServices = new Dictionary<Type, List<object>>();
            _isInitialized = false;
        }
        
        /// <summary>
        /// Initializes the service locator with default services
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;
                
            // Register default services
            RegisterDefaultServices();
            
            _isInitialized = true;
            Debug.Log("[DefaultPoolingServices] Services initialized");
        }
        
        /// <summary>
        /// Registers the default set of services needed by the pooling system
        /// </summary>
        private void RegisterDefaultServices()
        {
            // Register diagnostics and profiling services first since other services may depend on them
            if (!HasService<IPoolDiagnostics>())
            {
                var profiler = new PoolProfiler();
                var diagnostics = new PoolDiagnostics();
                RegisterService<IPoolDiagnostics>(diagnostics);
                RegisterService<IPoolProfiler>(profiler);
            }

            // Register health checker
            // if (!HasService<IPoolHealthChecker>())
            // {
            //     var healthChecker = new PoolHealthChecker(GetService<IPoolDiagnostics>());
            //     RegisterService<IPoolHealthChecker>(healthChecker);
            // }

            // Register logging service
            if (!HasService<IPoolLogger>())
            {
                var logger = new PoolLogger();
                RegisterService<IPoolLogger>(logger);
            }

            // Register factory
            if (!HasService<IPoolFactory>())
            {
                RegisterService<IPoolFactory>(new PoolFactory(null, GetService<IPoolDiagnostics>()));
            }

            // Register native pool registry
            if (!HasService<INativePoolRegistry>())
            {
                // Use the singleton instance to avoid multiple registry instances
                RegisterService<INativePoolRegistry>(NativePoolRegistry.Instance);
            }

            // Register managed pool registry
            if (!HasService<IPoolRegistry>())
            {
                var registry = new PoolRegistry("Default");
                RegisterService<IPoolRegistry>(registry);
            }

            // Register configuration registry
            if (!HasService<IPoolConfigRegistry>())
            {
                var configRegistry = DefaultPoolConfigRegistry.Instance;
                RegisterService<IPoolConfigRegistry>(configRegistry);
            }
        }
        
        /// <summary>
        /// Gets a service of the specified type
        /// </summary>
        /// <typeparam name="T">Type of service to retrieve</typeparam>
        /// <returns>The requested service, or null if not found</returns>
        public T GetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out object service))
            {
                return service as T;
            }
            return null;
        }
        
        /// <summary>
        /// Checks if a service of the specified type is registered
        /// </summary>
        /// <typeparam name="T">Type of service to check</typeparam>
        /// <returns>True if the service is registered, false otherwise</returns>
        public bool HasService<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// Gets a service of the specified type
        /// </summary>
        /// <param name="serviceType">Type of service to retrieve</param>
        /// <returns>The requested service, or null if not found</returns>
        public object GetService(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
                
            return _services.TryGetValue(serviceType, out object service) ? service : null;
        }
        
        /// <summary>
        /// Checks if a service of the specified type is registered
        /// </summary>
        /// <param name="serviceType">Type of service to check</param>
        /// <returns>True if the service is registered, false otherwise</returns>
        public bool HasService(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
                
            return _services.ContainsKey(serviceType);
        }
        
        /// <summary>
        /// Registers a service implementation
        /// </summary>
        /// <typeparam name="T">Type of service to register</typeparam>
        /// <param name="service">Service implementation</param>
        public void RegisterService<T>(T service) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));
                
            _services[typeof(T)] = service;
            
            // Also register in multi-services for GetServices<T>
            if (!_multiServices.TryGetValue(typeof(T), out var services))
            {
                services = new List<object>();
                _multiServices[typeof(T)] = services;
            }
            
            if (!services.Contains(service))
            {
                services.Add(service);
            }
        }
        
        /// <summary>
        /// Registers a service implementation with a specific type
        /// </summary>
        /// <param name="serviceType">Type to register the service as</param>
        /// <param name="service">Service implementation</param>
        public void RegisterService(Type serviceType, object service)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
                
            if (service == null)
                throw new ArgumentNullException(nameof(service));
                
            if (!serviceType.IsInstanceOfType(service))
                throw new ArgumentException($"Service does not implement {serviceType.Name}", nameof(service));
                
            _services[serviceType] = service;
            
            // Also register in multi-services
            if (!_multiServices.TryGetValue(serviceType, out var services))
            {
                services = new List<object>();
                _multiServices[serviceType] = services;
            }
            
            if (!services.Contains(service))
            {
                services.Add(service);
            }
        }
        
        /// <summary>
        /// Unregisters a service of the specified type
        /// </summary>
        /// <typeparam name="T">Type of service to unregister</typeparam>
        public void UnregisterService<T>() where T : class
        {
            _services.Remove(typeof(T));
            _multiServices.Remove(typeof(T));
        }
        
        /// <summary>
        /// Unregisters a service of the specified type
        /// </summary>
        /// <param name="serviceType">Type of service to unregister</param>
        public void UnregisterService(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
                
            _services.Remove(serviceType);
            _multiServices.Remove(serviceType);
        }
        
        /// <summary>
        /// Gets all registered services of a specific type
        /// </summary>
        /// <typeparam name="T">Type of services to retrieve</typeparam>
        /// <returns>Collection of registered services of the specified type</returns>
        public IEnumerable<T> GetServices<T>() where T : class
        {
            if (_multiServices.TryGetValue(typeof(T), out var services))
            {
                foreach (var service in services)
                {
                    yield return service as T;
                }
            }
        }
        
        /// <summary>
        /// Clears all registered services
        /// </summary>
        public void Clear()
        {
            _services.Clear();
            _multiServices.Clear();
            _isInitialized = false;
        }
        
        /// <summary>
        /// Resets the singleton instance
        /// </summary>
        public static void ResetInstance()
        {
            lock (s_lock)
            {
                if (s_instance != null)
                {
                    s_instance.Clear();
                    s_instance = null;
                }
            }
        }
    }
}