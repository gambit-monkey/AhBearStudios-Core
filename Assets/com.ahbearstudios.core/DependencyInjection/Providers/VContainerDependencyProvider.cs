using System;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using VContainer;

namespace AhBearStudios.Core.Messaging.DI
{
    /// <summary>
    /// VContainer implementation of the IDependencyProvider interface.
    /// </summary>
    public sealed class VContainerDependencyProvider : IDependencyProvider
    {
        private readonly IObjectResolver _container;
        
        /// <summary>
        /// Initializes a new instance of the VContainerDependencyProvider class.
        /// </summary>
        /// <param name="container">The VContainer object resolver to use.</param>
        public VContainerDependencyProvider(IObjectResolver container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }
        
        /// <inheritdoc />
        public T Resolve<T>()
        {
            return _container.Resolve<T>();
        }
    }
}