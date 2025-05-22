using System;

namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Defines a mechanism for retrieving a service object.
    /// </summary>
    public interface IServiceProvider
    {
        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>
        /// A service object of the specified type, or null if there is no 
        /// service object of the specified type.
        /// </returns>
        object GetService(Type serviceType);
    }
}