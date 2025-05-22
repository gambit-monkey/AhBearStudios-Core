using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging.Messages
{
    /// <summary>
    /// Base class for message modules that provides common functionality
    /// </summary>
    public abstract class MessageModuleBase : IMessageModule
    {
        /// <summary>
        /// Gets the name of the module
        /// </summary>
        public abstract string ModuleName { get; }
    
        /// <summary>
        /// Registers the module's message types
        /// </summary>
        /// <param name="registry">The registry to register with</param>
        public abstract void RegisterMessageTypes(IMessageTypeRegistry registry);

        /// <summary>
        /// Initializes the module
        /// </summary>
        /// <param name="provider">The service provider</param>
        public virtual void Initialize(IServiceProvider provider)
        {
            // Default implementation does nothing
        }
    
        /// <summary>
        /// Shuts down the module
        /// </summary>
        public virtual void Shutdown()
        {
            // Default implementation does nothing
        }
    
        /// <summary>
        /// Gets a service from the provider
        /// </summary>
        /// <typeparam name="T">The type of service to get</typeparam>
        /// <param name="provider">The service provider</param>
        /// <returns>The service</returns>
        protected T GetService<T>(IServiceProvider provider)
        {
            return (T)provider.GetService(typeof(T));
        }
    }
}