using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Serialization.Services
{
    /// <summary>
    /// Interface for type registration and management in serialization.
    /// </summary>
    public interface ISerializationRegistry
    {
        /// <summary>
        /// Registers a type for serialization optimization.
        /// </summary>
        /// <param name="type">Type to register</param>
        void RegisterType(Type type);

        /// <summary>
        /// Registers a type with custom metadata.
        /// </summary>
        /// <param name="type">Type to register</param>
        /// <param name="descriptor">Type descriptor with metadata</param>
        void RegisterType(Type type, TypeDescriptor descriptor);

        /// <summary>
        /// Checks if a type is registered.
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if registered</returns>
        bool IsRegistered(Type type);

        /// <summary>
        /// Gets the descriptor for a registered type.
        /// </summary>
        /// <param name="type">Type to get descriptor for</param>
        /// <returns>Type descriptor or null if not found</returns>
        TypeDescriptor GetTypeDescriptor(Type type);

        /// <summary>
        /// Gets all registered types.
        /// </summary>
        /// <returns>Collection of registered types</returns>
        IReadOnlyCollection<Type> GetRegisteredTypes();

        /// <summary>
        /// Unregisters a type.
        /// </summary>
        /// <param name="type">Type to unregister</param>
        /// <returns>True if type was registered and removed</returns>
        bool UnregisterType(Type type);

        /// <summary>
        /// Clears all registered types.
        /// </summary>
        void Clear();
    }
}