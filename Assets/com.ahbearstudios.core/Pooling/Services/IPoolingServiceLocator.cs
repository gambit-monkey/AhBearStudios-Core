using System.Collections.Generic;
using System;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Service locator for accessing pooling system components and services.
    /// </summary>
    public interface IPoolingServiceLocator
    {
        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <typeparam name="T">Type of service to retrieve</typeparam>
        /// <returns>The requested service, or null if not found</returns>
        T GetService<T>() where T : class;

        /// <summary>
        /// Checks if a service of the specified type is registered.
        /// </summary>
        /// <typeparam name="T">Type of service to check</typeparam>
        /// <returns>True if the service is registered, false otherwise</returns>
        bool HasService<T>() where T : class;

        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <param name="serviceType">Type of service to retrieve</param>
        /// <returns>The requested service, or null if not found</returns>
        object GetService(Type serviceType);

        /// <summary>
        /// Checks if a service of the specified type is registered.
        /// </summary>
        /// <param name="serviceType">Type of service to check</param>
        /// <returns>True if the service is registered, false otherwise</returns>
        bool HasService(Type serviceType);

        /// <summary>
        /// Registers a service implementation.
        /// </summary>
        /// <typeparam name="T">Type of service to register</typeparam>
        /// <param name="service">Service implementation</param>
        void RegisterService<T>(T service) where T : class;

        /// <summary>
        /// Registers a service implementation with a specific type.
        /// </summary>
        /// <param name="serviceType">Type to register the service as</param>
        /// <param name="service">Service implementation</param>
        void RegisterService(Type serviceType, object service);

        /// <summary>
        /// Unregisters a service of the specified type.
        /// </summary>
        /// <typeparam name="T">Type of service to unregister</typeparam>
        void UnregisterService<T>() where T : class;

        /// <summary>
        /// Unregisters a service of the specified type.
        /// </summary>
        /// <param name="serviceType">Type of service to unregister</param>
        void UnregisterService(Type serviceType);

        /// <summary>
        /// Gets all registered services of a specific type.
        /// </summary>
        /// <typeparam name="T">Type of services to retrieve</typeparam>
        /// <returns>Collection of registered services of the specified type</returns>
        IEnumerable<T> GetServices<T>() where T : class;

        /// <summary>
        /// Clears all registered services.
        /// </summary>
        void Clear();
    }
}