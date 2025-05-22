using AhBearStudios.Core.DependencyInjection.Interfaces;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for a module that registers message types
    /// </summary>
    public interface IMessageModule
    {
        /// <summary>
        /// Gets the name of the module
        /// </summary>
        string ModuleName { get; }
    
        /// <summary>
        /// Registers the module's message types
        /// </summary>
        /// <param name="registry">The registry to register with</param>
        void RegisterMessageTypes(IMessageTypeRegistry registry);
    
        /// <summary>
        /// Initializes the module
        /// </summary>
        /// <param name="provider">The service provider</param>
        void Initialize(IServiceProvider provider);
    
        /// <summary>
        /// Shuts down the module
        /// </summary>
        void Shutdown();
    }
}