using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for managing message modules
    /// </summary>
    public interface IMessageModuleManager
    {
        /// <summary>
        /// Registers a module
        /// </summary>
        /// <param name="module">The module to register</param>
        void RegisterModule(IMessageModule module);
    
        /// <summary>
        /// Gets a module by name
        /// </summary>
        /// <param name="moduleName">The name of the module</param>
        /// <returns>The module with the specified name</returns>
        IMessageModule GetModule(string moduleName);
    
        /// <summary>
        /// Gets all registered modules
        /// </summary>
        /// <returns>The registered modules</returns>
        IEnumerable<IMessageModule> GetAllModules();
    
        /// <summary>
        /// Initializes all modules
        /// </summary>
        /// <param name="provider">The service provider</param>
        void InitializeAllModules(IServiceProvider provider);
    
        /// <summary>
        /// Shuts down all modules
        /// </summary>
        void ShutdownAllModules();
    
        /// <summary>
        /// Registers all module message types
        /// </summary>
        /// <param name="registry">The registry to register with</param>
        void RegisterAllModuleMessageTypes(IMessageTypeRegistry registry);
    }
}