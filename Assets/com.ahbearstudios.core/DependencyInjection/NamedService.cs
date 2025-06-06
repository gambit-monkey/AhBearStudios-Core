using System;

namespace AhBearStudios.Core.DependencyInjection
{
    /// <summary>
    /// Wrapper for named service instances.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    public sealed class NamedService<T>
    {
        /// <summary>
        /// Gets the name identifier for this service.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the service instance.
        /// </summary>
        public T Instance { get; }

        /// <summary>
        /// Initializes a new instance of the NamedService class.
        /// </summary>
        /// <param name="name">The name identifier.</param>
        /// <param name="instance">The service instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when name or instance is null.</exception>
        public NamedService(string name, T instance)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }
    }
}