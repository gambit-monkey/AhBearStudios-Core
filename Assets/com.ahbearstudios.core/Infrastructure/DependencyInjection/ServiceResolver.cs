using System;
using System.Collections.Concurrent;

namespace AhBearStudios.Core.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Simple service resolver for scenarios where dependency injection is not available,
    /// such as static extension methods. This is a minimal implementation for specific use cases.
    /// </summary>
    public static class ServiceResolver
    {
        private static readonly ConcurrentDictionary<Type, object> _services = new();
        private static readonly object _lock = new object();

        /// <summary>
        /// Registers a service instance.
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <param name="instance">Service instance</param>
        public static void Register<T>(T instance) where T : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            _services.AddOrUpdate(typeof(T), instance, (key, oldValue) => instance);
        }

        /// <summary>
        /// Gets a registered service instance.
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <returns>Service instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when service is not registered</exception>
        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }

            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered. Call ServiceResolver.Register<{typeof(T).Name}>(instance) first.");
        }

        /// <summary>
        /// Tries to get a registered service instance.
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <param name="service">Output service instance</param>
        /// <returns>True if service was found</returns>
        public static bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var serviceObj))
            {
                service = (T)serviceObj;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Checks if a service is registered.
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <returns>True if service is registered</returns>
        public static bool IsRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Unregisters a service.
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <returns>True if service was removed</returns>
        public static bool Unregister<T>() where T : class
        {
            return _services.TryRemove(typeof(T), out _);
        }

        /// <summary>
        /// Clears all registered services.
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
        }

        /// <summary>
        /// Gets the count of registered services.
        /// </summary>
        public static int Count => _services.Count;
    }
}