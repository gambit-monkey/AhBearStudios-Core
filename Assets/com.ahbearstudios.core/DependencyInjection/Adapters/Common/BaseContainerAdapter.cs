using System.Diagnostics;
using AhBearStudios.Core.DependencyInjection.Configuration;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;
using AhBearStudios.Core.DependencyInjection.Validation;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Adapters
{
    /// <summary>
    /// Base implementation of IContainerAdapter providing common functionality.
    /// Framework-specific adapters should inherit from this to get consistent behavior.
    /// </summary>
    public abstract class BaseContainerAdapter : IContainerAdapter
    {
        private readonly Stopwatch _lifetimeStopwatch;
        private readonly IContainerValidator _validator;
        private ContainerMetrics _metrics;
        private bool _disposed;
        private bool _isBuilt;
        
        /// <summary>
        /// Gets the framework this adapter represents.
        /// </summary>
        public abstract ContainerFramework Framework { get; }
        
        /// <summary>
        /// Gets the container name or identifier.
        /// </summary>
        public string ContainerName { get; }
        
        /// <summary>
        /// Gets whether this adapter has been disposed.
        /// </summary>
        public bool IsDisposed => _disposed;
        
        /// <summary>
        /// Gets whether the container has been built and is ready for resolution.
        /// </summary>
        public bool IsBuilt => _isBuilt && !_disposed;
        
        /// <summary>
        /// Gets the message bus used by this adapter.
        /// </summary>
        public IMessageBusService MessageBusService { get; }
        
        /// <summary>
        /// Gets the configuration used by this adapter.
        /// </summary>
        public IDependencyInjectionConfig Configuration { get; }
        
        /// <summary>
        /// Initializes a new base container adapter.
        /// </summary>
        protected BaseContainerAdapter(
            string containerName,
            IDependencyInjectionConfig configuration,
            IMessageBusService messageBusService = null,
            IContainerValidator validator = null)
        {
            ContainerName = containerName ?? $"{Framework}Container_{Guid.NewGuid():N}";
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            MessageBusService = messageBusService;
            _validator = validator ?? new ContainerValidator();
            _lifetimeStopwatch = Stopwatch.StartNew();
            
            if (Configuration.EnablePerformanceMetrics)
            {
                _metrics = new ContainerMetrics(ContainerName);
            }
        }
        
        /// <summary>
        /// Registers a singleton service with the container.
        /// </summary>
        public IContainerAdapter RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            ThrowIfDisposed();
            ThrowIfBuilt();
            
            try
            {
                DoRegisterSingleton<TInterface, TImplementation>();
                RecordRegistration(typeof(TInterface), typeof(TImplementation), ServiceLifetime.Singleton);
                return this;
            }
            catch (Exception ex)
            {
                HandleRegistrationError(typeof(TInterface), typeof(TImplementation), ex);
                throw;
            }
        }
        
        /// <summary>
        /// Registers a singleton service with a factory method.
        /// </summary>
        public IContainerAdapter RegisterSingleton<TInterface>(Func<IServiceResolver, TInterface> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            
            ThrowIfDisposed();
            ThrowIfBuilt();
            
            try
            {
                DoRegisterSingletonFactory<TInterface>(factory);
                RecordRegistration(typeof(TInterface), null, ServiceLifetime.Singleton, isFactory: true);
                return this;
            }
            catch (Exception ex)
            {
                HandleRegistrationError(typeof(TInterface), null, ex);
                throw;
            }
        }
        
        /// <summary>
        /// Registers a singleton instance directly.
        /// </summary>
        public IContainerAdapter RegisterInstance<TInterface>(TInterface instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            
            ThrowIfDisposed();
            ThrowIfBuilt();
            
            try
            {
                DoRegisterInstance<TInterface>(instance);
                RecordRegistration(typeof(TInterface), instance.GetType(), ServiceLifetime.Instance, isInstance: true);
                return this;
            }
            catch (Exception ex)
            {
                HandleRegistrationError(typeof(TInterface), instance.GetType(), ex);
                throw;
            }
        }
        
        /// <summary>
        /// Registers a transient service.
        /// </summary>
        public IContainerAdapter RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            ThrowIfDisposed();
            ThrowIfBuilt();
            
            try
            {
                DoRegisterTransient<TInterface, TImplementation>();
                RecordRegistration(typeof(TInterface), typeof(TImplementation), ServiceLifetime.Transient);
                return this;
            }
            catch (Exception ex)
            {
                HandleRegistrationError(typeof(TInterface), typeof(TImplementation), ex);
                throw;
            }
        }
        
        /// <summary>
        /// Registers a transient service with a factory method.
        /// </summary>
        public IContainerAdapter RegisterTransient<TInterface>(Func<IServiceResolver, TInterface> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            
            ThrowIfDisposed();
            ThrowIfBuilt();
            
            try
            {
                DoRegisterTransientFactory<TInterface>(factory);
                RecordRegistration(typeof(TInterface), null, ServiceLifetime.Transient, isFactory: true);
                return this;
            }
            catch (Exception ex)
            {
                HandleRegistrationError(typeof(TInterface), null, ex);
                throw;
            }
        }
        
        /// <summary>
        /// Builds the container and makes it ready for resolution.
        /// </summary>
        public IServiceResolver Build()
        {
            ThrowIfDisposed();
            
            if (_isBuilt)
                return GetExistingResolver();
            
            var buildStopwatch = Stopwatch.StartNew();
            
            try
            {
                var resolver = DoBuild();
                buildStopwatch.Stop();
                
                _isBuilt = true;
                _metrics?.RecordBuild(buildStopwatch.Elapsed);
                
                // Warn if build time exceeds threshold
                if (buildStopwatch.ElapsedMilliseconds > Configuration.MaxBuildTimeWarningMs)
                {
                    LogBuildTimeWarning(buildStopwatch.Elapsed);
                }
                
                return resolver;
            }
            catch (Exception ex)
            {
                buildStopwatch.Stop();
                _metrics?.RecordBuildFailure(buildStopwatch.Elapsed);
                HandleBuildError(ex);
                throw;
            }
        }
        
        /// <summary>
        /// Checks if a service type is registered.
        /// </summary>
        public abstract bool IsRegistered<T>();
        
        /// <summary>
        /// Checks if a service type is registered.
        /// </summary>
        public abstract bool IsRegistered(Type serviceType);
        
        /// <summary>
        /// Validates the container registrations.
        /// </summary>
        public IContainerValidationResult Validate()
        {
            ThrowIfDisposed();
            
            if (!Configuration.EnableValidation)
            {
                return ContainerValidationResult.Success(0, TimeSpan.Zero);
            }
            
            try
            {
                return _validator.Validate(this, Configuration);
            }
            catch (Exception ex)
            {
                var error = new ValidationError(
                    ValidationErrorType.RegistrationFailure,
                    $"Validation failed with exception: {ex.Message}",
                    null,
                    null,
                    ex);
                
                return ContainerValidationResult.Failure(0, 0, new[] { error });
            }
        }
        
        /// <summary>
        /// Creates a child container that inherits registrations.
        /// </summary>
        public abstract IContainerAdapter CreateChild(string childName = null);
        
        /// <summary>
        /// Gets performance metrics for this container.
        /// </summary>
        public IContainerMetrics GetMetrics()
        {
            return _metrics;
        }
        
        /// <summary>
        /// Disposes the adapter and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            
            try
            {
                _lifetimeStopwatch.Stop();
                
                // Record final metrics
                _metrics?.RecordDisposal(_lifetimeStopwatch.Elapsed);
                
                // Dispose framework-specific resources
                DisposeCore();
                
                _disposed = true;
            }
            catch (Exception ex)
            {
                // Log disposal error but don't throw
                LogDisposalError(ex);
                _disposed = true; // Mark as disposed even if disposal failed
            }
        }
        
        // Abstract methods that framework-specific adapters must implement
        
        /// <summary>
        /// Framework-specific singleton registration implementation.
        /// </summary>
        protected abstract void DoRegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface;
        
        /// <summary>
        /// Framework-specific singleton factory registration implementation.
        /// </summary>
        protected abstract void DoRegisterSingletonFactory<TInterface>(Func<IServiceResolver, TInterface> factory);
        
        /// <summary>
        /// Framework-specific instance registration implementation.
        /// </summary>
        protected abstract void DoRegisterInstance<TInterface>(TInterface instance);
        
        /// <summary>
        /// Framework-specific transient registration implementation.
        /// </summary>
        protected abstract void DoRegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface;
        
        /// <summary>
        /// Framework-specific transient factory registration implementation.
        /// </summary>
        protected abstract void DoRegisterTransientFactory<TInterface>(Func<IServiceResolver, TInterface> factory);
        
        /// <summary>
        /// Framework-specific build implementation.
        /// </summary>
        protected abstract IServiceResolver DoBuild();
        
        /// <summary>
        /// Gets the existing resolver if container is already built.
        /// </summary>
        protected abstract IServiceResolver GetExistingResolver();
        
        /// <summary>
        /// Framework-specific disposal implementation.
        /// </summary>
        protected abstract void DisposeCore();
        
        // Helper methods for subclasses
        
        /// <summary>
        /// Throws if the adapter has been disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BaseContainerAdapter));
        }
        
        /// <summary>
        /// Throws if the container has already been built.
        /// </summary>
        protected void ThrowIfBuilt()
        {
            if (_isBuilt)
                throw new InvalidOperationException("Cannot modify container after it has been built");
        }
        
        /// <summary>
        /// Records a service registration for metrics and validation.
        /// </summary>
        protected void RecordRegistration(
            Type serviceType, 
            Type implementationType, 
            ServiceLifetime lifetime,
            bool isFactory = false,
            bool isInstance = false)
        {
            _metrics?.RecordRegistration(serviceType, implementationType, lifetime);
            
            if (Configuration.EnableDebugLogging)
            {
                LogRegistration(serviceType, implementationType, lifetime, isFactory, isInstance);
            }
        }
        
        /// <summary>
        /// Handles registration errors with logging and metrics.
        /// </summary>
        protected void HandleRegistrationError(Type serviceType, Type implementationType, Exception exception)
        {
            _metrics?.RecordRegistrationFailure(serviceType, implementationType);
            
            if (Configuration.EnableDebugLogging)
            {
                LogRegistrationError(serviceType, implementationType, exception);
            }
        }
        
        /// <summary>
        /// Handles build errors with logging and metrics.
        /// </summary>
        protected void HandleBuildError(Exception exception)
        {
            if (Configuration.EnableDebugLogging)
            {
                LogBuildError(exception);
            }
        }
        
        /// <summary>
        /// Logs a service registration if debug logging is enabled.
        /// </summary>
        protected virtual void LogRegistration(
            Type serviceType, 
            Type implementationType, 
            ServiceLifetime lifetime,
            bool isFactory,
            bool isInstance)
        {
            var impl = implementationType?.Name ?? (isFactory ? "Factory" : isInstance ? "Instance" : "Unknown");
            Console.WriteLine($"[{Framework}] Registered {serviceType.Name} -> {impl} ({lifetime})");
        }
        
        /// <summary>
        /// Logs a registration error if debug logging is enabled.
        /// </summary>
        protected virtual void LogRegistrationError(Type serviceType, Type implementationType, Exception exception)
        {
            var impl = implementationType?.Name ?? "Unknown";
            Console.WriteLine($"[{Framework}] Failed to register {serviceType.Name} -> {impl}: {exception.Message}");
        }
        
        /// <summary>
        /// Logs a build error if debug logging is enabled.
        /// </summary>
        protected virtual void LogBuildError(Exception exception)
        {
            Console.WriteLine($"[{Framework}] Container build failed: {exception.Message}");
        }
        
        /// <summary>
        /// Logs a build time warning if build exceeds threshold.
        /// </summary>
        protected virtual void LogBuildTimeWarning(TimeSpan buildTime)
        {
            Console.WriteLine($"[{Framework}] Container build took {buildTime.TotalMilliseconds:F1}ms " +
                             $"(exceeds warning threshold of {Configuration.MaxBuildTimeWarningMs}ms)");
        }
        
        /// <summary>
        /// Logs disposal errors.
        /// </summary>
        protected virtual void LogDisposalError(Exception exception)
        {
            Console.WriteLine($"[{Framework}] Error disposing container '{ContainerName}': {exception.Message}");
        }
    }
}