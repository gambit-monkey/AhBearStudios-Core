using System;
using System.Collections.Generic;
using System.Reflection;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for discovering message types
    /// </summary>
    public interface IMessageTypeDiscovery
    {
        /// <summary>
        /// Discovers message types in the specified assemblies
        /// </summary>
        /// <param name="assemblies">The assemblies to search</param>
        /// <returns>The discovered message types</returns>
        IEnumerable<Type> DiscoverMessageTypes(params Assembly[] assemblies);
    
        /// <summary>
        /// Discovers message types that match the specified predicate
        /// </summary>
        /// <param name="predicate">The predicate to match</param>
        /// <param name="assemblies">The assemblies to search</param>
        /// <returns>The discovered message types</returns>
        IEnumerable<Type> DiscoverMessageTypes(Func<Type, bool> predicate, params Assembly[] assemblies);
    }
}