using System.Collections.Generic;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Factory interface for resolving named services.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    public interface INamedServiceFactory<T>
    {
        /// <summary>
        /// Resolves a named service by identifier.
        /// </summary>
        /// <param name="name">The name identifier.</param>
        /// <returns>The service instance if found.</returns>
        /// <exception cref="ServiceResolutionException">Thrown when the named service cannot be found.</exception>
        T Resolve(string name);

        /// <summary>
        /// Attempts to resolve a named service by identifier.
        /// </summary>
        /// <param name="name">The name identifier.</param>
        /// <param name="service">The service instance if found.</param>
        /// <returns>True if the service was found, false otherwise.</returns>
        bool TryResolve(string name, out T service);

        /// <summary>
        /// Gets all available named services.
        /// </summary>
        /// <returns>A dictionary of all named services.</returns>
        IReadOnlyDictionary<string, T> GetAllNamed();
    }
}