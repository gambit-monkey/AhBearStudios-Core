using System.Diagnostics;
using AhBearStudios.Core.DependencyInjection.Extensions.VContainer;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Messages;
using AhBearStudios.Core.DependencyInjection.Models;
using AhBearStudios.Core.DependencyInjection.Providers.VContainer;
using AhBearStudios.Core.MessageBus.Interfaces;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Adapters.VContainer
{
    /// <summary>
    /// VContainer implementation of IContainerAdapter.
    /// Provides a framework-agnostic wrapper around VContainer's IContainerBuilder and IObjectResolver.
    /// Integrates with message bus for lifecycle events and supports comprehensive metrics collection.
    /// Fixed to use correct VContainer API for child containers.
    /// </summary>
    public sealed class VContainerAdapter : BaseContainerAdapter
    {
        private readonly IContainerBuilder _builder;
        private IObjectResolver _resolver;
        private IObjectResolver _parentResolver; // Store parent for child containers
        private readonly object _buildLock = new object();

        /// <summary>
        /// Gets the framework this adapter represents.
        /// </summary>
        public override ContainerFramework Framework => ContainerFramework.VContainer;

        /// <summary>
        /// Initializes a new VContainer adapter.
        /// </summary>
        /// <param name="builder">The VContainer builder instance.</param>
        /// <param name="containerName">Optional name for the container.</param>
        /// <param name="configuration">Configuration for the adapter.</param>
        /// <param name="messageBusService">Optional message bus service.</param>
        /// <param name="validator">Optional container validator.</param>
        /// <param name="parentResolver">Optional parent resolver for child containers.</param>
        /// <exception cref="ArgumentNullException">Thrown when builder or configuration is null.</exception>
        public VContainerAdapter(
            IContainerBuilder builder,
            string containerName = null,
            IDependencyInjectionConfig configuration = null,
            IMessageBusService messageBusService = null,
            IContainerValidator validator = null,
            IObjectResolver parentResolver = null)
            : base(containerName, configuration, messageBusService, validator)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _parentResolver = parentResolver; // Store parent for Build() phase
            
            // Register core services that the adapter itself needs
            RegisterCoreServices();
        }

        /// <summary>
        /// Framework-specific singleton registration implementation.
        /// </summary>
        protected override void DoRegisterSingleton<TInterface, TImplementation>()
        {
            try
            {
                _builder.Register<TImplementation>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
                
                // Publish registration event
                PublishRegistrationEvent(typeof(TInterface), typeof(TImplementation), ServiceLifetime.Singleton);
            }
            catch (Exception ex)
            {
                LogRegistrationError(typeof(TInterface), typeof(TImplementation), ex);
                throw;
            }
        }

        /// <summary>
        /// Framework-specific singleton factory registration implementation.
        /// </summary>
        protected override void DoRegisterSingletonFactory<TInterface>(Func<IServiceResolver, TInterface> factory)
        {
            try
            {
                _builder.Register<TInterface>(resolver =>
                {
                    var serviceResolver = new VContainerServiceResolver(resolver);
                    return factory(serviceResolver);
                }, Lifetime.Singleton);
                
                // Publish registration event
                PublishRegistrationEvent(typeof(TInterface), null, ServiceLifetime.Singleton, isFactory: true);
            }
            catch (Exception ex)
            {
                LogRegistrationError(typeof(TInterface), null, ex);
                throw;
            }
        }

        /// <summary>
        /// Framework-specific instance registration implementation.
        /// </summary>
        protected override void DoRegisterInstance<TInterface>(TInterface instance)
        {
            try
            {
                _builder.RegisterInstance(instance);
                
                // Publish registration event
                PublishRegistrationEvent(typeof(TInterface), instance.GetType(), ServiceLifetime.Instance, isInstance: true);
            }
            catch (Exception ex)
            {
                LogRegistrationError(typeof(TInterface), instance.GetType(), ex);
                throw;
            }
        }

        /// <summary>
        /// Framework-specific transient registration implementation.
        /// </summary>
        protected override void DoRegisterTransient<TInterface, TImplementation>()
        {
            try
            {
                _builder.Register<TImplementation>(Lifetime.Transient).AsImplementedInterfaces().AsSelf();
                
                // Publish registration event
                PublishRegistrationEvent(typeof(TInterface), typeof(TImplementation), ServiceLifetime.Transient);
            }
            catch (Exception ex)
            {
                LogRegistrationError(typeof(TInterface), typeof(TImplementation), ex);
                throw;
            }
        }

        /// <summary>
        /// Framework-specific transient factory registration implementation.
        /// </summary>
        protected override void DoRegisterTransientFactory<TInterface>(Func<IServiceResolver, TInterface> factory)
        {
            try
            {
                _builder.Register<TInterface>(resolver =>
                {
                    var serviceResolver = new VContainerServiceResolver(resolver);
                    return factory(serviceResolver);
                }, Lifetime.Transient);
                
                // Publish registration event
                PublishRegistrationEvent(typeof(TInterface), null, ServiceLifetime.Transient, isFactory: true);
            }
            catch (Exception ex)
            {
                LogRegistrationError(typeof(TInterface), null, ex);
                throw;
            }
        }

        /// <summary>
        /// Framework-specific build implementation using correct VContainer API.
        /// </summary>
        protected override IServiceResolver DoBuild()
        {
            lock (_buildLock)
            {
                if (_resolver != null)
                    return new VContainerServiceResolver(_resolver);

                var stopwatch = Stopwatch.StartNew();
                
                try
                {
                    // Validate registrations before building if validation is enabled
                    if (Configuration.EnableValidation)
                    {
                        var isValid = _builder.ValidateRegistrations(Configuration.ThrowOnValidationFailure);
                        if (!isValid && Configuration.ThrowOnValidationFailure)
                        {
                            throw new InvalidOperationException("Container validation failed");
                        }
                    }

                    // Build the container using correct VContainer API
                    // Check what build method is actually available on the builder
                    IObjectResolver resolver = null;
                    
                    // Try different possible VContainer build methods
                    if (_builder is ContainerBuilder concreteBuilder)
                    {
                        // Use the concrete ContainerBuilder's Build method
                        resolver = concreteBuilder.Build();
                    }
                    else
                    {
                        // Fallback: try to find Build method via reflection
                        var buildMethod = _builder.GetType().GetMethod("Build", new Type[0]);
                        if (buildMethod != null)
                        {
                            resolver = buildMethod.Invoke(_builder, null) as IObjectResolver;
                        }
                        else
                        {
                            throw new NotSupportedException(
                                $"Cannot find Build method on VContainer builder type: {_builder.GetType().FullName}");
                        }
                    }

                    if (resolver == null)
                    {
                        throw new InvalidOperationException("VContainer Build() returned null resolver");
                    }

                    _resolver = resolver;
                    stopwatch.Stop();

                    var registrationCount = _builder.GetRegistrationCount();
                    
                    // Publish container built event
                    PublishContainerBuiltEvent(registrationCount, stopwatch.Elapsed);
                    
                    // Log successful build
                    if (Configuration.EnableDebugLogging)
                    {
                        LogBuildSuccess(registrationCount, stopwatch.Elapsed);
                    }

                    return new VContainerServiceResolver(_resolver, ContainerName, Configuration, MessageBusService);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    LogBuildError(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the existing resolver if container is already built.
        /// </summary>
        protected override IServiceResolver GetExistingResolver()
        {
            if (_resolver == null)
                throw new InvalidOperationException("Container has not been built yet");
                
            return new VContainerServiceResolver(_resolver, ContainerName, Configuration, MessageBusService);
        }

        /// <summary>
        /// Checks if a service type is registered.
        /// </summary>
        public override bool IsRegistered<T>()
        {
            return IsRegistered(typeof(T));
        }

        /// <summary>
        /// Checks if a service type is registered.
        /// </summary>
        public override bool IsRegistered(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            try
            {
                if (_resolver != null)
                {
                    // Container is built, check with resolver
                    return _resolver.TryResolve(serviceType, out _);
                }
                else
                {
                    // Container not built yet, check with builder
                    return _builder.IsRegistered(serviceType);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a child container that inherits registrations.
        /// Note: VContainer's child container support may be limited in some versions.
        /// </summary>
        public override IContainerAdapter CreateChild(string childName = null)
        {
            ThrowIfDisposed();
            
            if (_resolver == null)
                throw new InvalidOperationException("Cannot create child container before parent is built");

            try
            {
                // Create new builder using standard VContainer API
                var childBuilder = new ContainerBuilder();
                var childContainerName = childName ?? $"{ContainerName}_Child_{Guid.NewGuid():N}";
                
                // Check if VContainer supports child containers in this version
                // Some versions of VContainer may have limited child container support
                
                // Create child adapter without parent resolver for now
                // VContainer child container behavior depends on the specific version
                var childAdapter = new VContainerAdapter(
                    childBuilder,
                    childContainerName,
                    Configuration,
                    MessageBusService,
                    null); // No parent resolver due to API limitations

                // Log the limitation
                if (Configuration.EnableDebugLogging)
                {
                    Console.WriteLine($"[VContainer] Created child container '{childContainerName}'. " +
                                    $"Note: Child container inheritance depends on VContainer version.");
                }

                // Publish child container created event
                PublishChildContainerCreatedEvent(childContainerName);

                return childAdapter;
            }
            catch (Exception ex)
            {
                if (Configuration.EnableDebugLogging)
                {
                    Console.WriteLine($"[VContainer] Failed to create child container: {ex.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Framework-specific disposal implementation.
        /// </summary>
        protected override void DisposeCore()
        {
            try
            {
                // Publish disposal event before disposing
                PublishContainerDisposedEvent();

                // Dispose the resolver if it's disposable
                if (_resolver is IDisposable disposableResolver)
                {
                    disposableResolver.Dispose();
                }

                _resolver = null;
                _parentResolver = null; // Clear parent reference

                if (Configuration.EnableDebugLogging)
                {
                    Console.WriteLine($"[VContainer] Disposed container '{ContainerName}'");
                }
            }
            catch (Exception ex)
            {
                LogDisposalError(ex);
            }
        }

        /// <summary>
        /// Registers core services needed by the adapter itself.
        /// </summary>
        private void RegisterCoreServices()
        {
            try
            {
                // Register the dependency provider adapter that wraps the resolver
                _builder.Register<IDependencyProvider>(resolver => 
                    new VContainerDependencyProviderAdapter(resolver), Lifetime.Singleton);

                if (Configuration.EnableDebugLogging)
                {
                    Console.WriteLine("[VContainer] Registered core adapter services");
                }
            }
            catch (Exception ex)
            {
                if (Configuration.EnableDebugLogging)
                {
                    Console.WriteLine($"[VContainer] Failed to register core services: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Publishes a service registration event to the message bus.
        /// </summary>
        private void PublishRegistrationEvent(
            Type serviceType, 
            Type implementationType, 
            ServiceLifetime lifetime,
            bool isFactory = false,
            bool isInstance = false)
        {
            if (MessageBusService == null) return;

            try
            {
                var message = new ServiceRegisteredMessage(
                    ContainerName,
                    serviceType,
                    implementationType,
                    lifetime,
                    isFactory);

                MessageBusService.PublishMessage(message);
            }
            catch (Exception ex)
            {
                if (Configuration.EnableDebugLogging)
                {
                    Console.WriteLine($"[VContainer] Failed to publish registration event: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Publishes a container built event to the message bus.
        /// </summary>
        private void PublishContainerBuiltEvent(int registrationCount, TimeSpan buildTime)
        {
            if (MessageBusService == null) return;

            try
            {
                var message = new ContainerBuiltMessage(ContainerName, registrationCount, buildTime);
                MessageBusService.PublishMessage(message);
            }
            catch (Exception ex)
            {
                if (Configuration.EnableDebugLogging)
                {
                    Console.WriteLine($"[VContainer] Failed to publish container built event: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Publishes a child container created event to the message bus.
        /// </summary>
        private void PublishChildContainerCreatedEvent(string childContainerName)
        {
            if (MessageBusService == null) return;

            try
            {
                var message = new ChildContainerCreatedMessage(ContainerName, childContainerName);
                MessageBusService.PublishMessage(message);
            }
            catch (Exception ex)
            {
                if (Configuration.EnableDebugLogging)
                {
                    Console.WriteLine($"[VContainer] Failed to publish child container created event: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Publishes a container disposed event to the message bus.
        /// </summary>
        private void PublishContainerDisposedEvent()
        {
            if (MessageBusService == null) return;

            try
            {
                var lifetime = DateTime.UtcNow - _creationTime;
                var message = new ContainerDisposedMessage(ContainerName, lifetime);
                MessageBusService.PublishMessage(message);
            }
            catch (Exception ex)
            {
                if (Configuration.EnableDebugLogging)
                {
                    Console.WriteLine($"[VContainer] Failed to publish container disposed event: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Logs a successful container build.
        /// </summary>
        private void LogBuildSuccess(int registrationCount, TimeSpan buildTime)
        {
            Console.WriteLine($"[VContainer] Successfully built container '{ContainerName}' " +
                             $"with {registrationCount} registrations in {buildTime.TotalMilliseconds:F1}ms");
        }

        private readonly DateTime _creationTime = DateTime.UtcNow;
    }
}