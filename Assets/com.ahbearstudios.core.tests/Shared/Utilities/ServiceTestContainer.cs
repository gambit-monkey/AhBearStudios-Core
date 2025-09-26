using System;
using System.Collections.Generic;
using System.Linq;
using ZLinq;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Stubs;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Spies;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Fakes;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Tests.Shared.Utilities
{
    /// <summary>
    /// TDD-compliant service container for dependency injection in tests.
    /// Provides service registration and resolution with built-in test double support.
    /// Strictly follows CLAUDETESTS.md guidelines with correlation tracking and performance monitoring.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// </summary>
    public sealed class ServiceTestContainer : IDisposable
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();
        private readonly Dictionary<Guid, object> _correlatedServices = new Dictionary<Guid, object>();
        private bool _disposed = false;
        private StubLoggingService _loggingService;

        /// <summary>
        /// Initializes a new instance of ServiceTestContainer with optional logging integration.
        /// </summary>
        /// <param name="loggingService">Optional logging service for test double integration</param>
        public ServiceTestContainer(StubLoggingService loggingService = null)
        {
            _loggingService = loggingService;
            RegisterDefaultTestDoubles();
        }

        /// <summary>
        /// Gets the number of registered services.
        /// </summary>
        public int RegisteredServiceCount => _services.Count + _factories.Count;

        /// <summary>
        /// Registers a service instance in the container.
        /// </summary>
        /// <typeparam name="TService">The service type</typeparam>
        /// <param name="instance">The service instance</param>
        /// <exception cref="ArgumentNullException">Thrown when instance is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is already registered</exception>
        public void RegisterInstance<TService>(TService instance) where TService : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var serviceType = typeof(TService);

            if (_services.ContainsKey(serviceType) || _factories.ContainsKey(serviceType))
                throw new InvalidOperationException($"Service of type {serviceType.Name} is already registered");

            _services[serviceType] = instance;
        }

        /// <summary>
        /// Registers a service instance in the container, allowing re-registration.
        /// If the service is already registered, it will be replaced with the new instance.
        /// </summary>
        /// <typeparam name="TService">The service type</typeparam>
        /// <param name="instance">The service instance</param>
        /// <exception cref="ArgumentNullException">Thrown when instance is null</exception>
        public void RegisterOrReplaceInstance<TService>(TService instance) where TService : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var serviceType = typeof(TService);

            // Remove from factories if it exists there
            _factories.Remove(serviceType);

            // Register or replace the instance
            _services[serviceType] = instance;
        }

        /// <summary>
        /// Registers a service instance only if it's not already registered.
        /// Returns true if registration was successful, false if service was already registered.
        /// </summary>
        /// <typeparam name="TService">The service type</typeparam>
        /// <param name="instance">The service instance</param>
        /// <returns>True if service was registered, false if already registered</returns>
        /// <exception cref="ArgumentNullException">Thrown when instance is null</exception>
        public bool TryRegisterInstance<TService>(TService instance) where TService : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var serviceType = typeof(TService);

            if (_services.ContainsKey(serviceType) || _factories.ContainsKey(serviceType))
                return false;

            _services[serviceType] = instance;
            return true;
        }

        /// <summary>
        /// Registers a service factory in the container.
        /// </summary>
        /// <typeparam name="TService">The service type</typeparam>
        /// <param name="factory">The factory function</param>
        /// <exception cref="ArgumentNullException">Thrown when factory is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is already registered</exception>
        public void RegisterFactory<TService>(Func<TService> factory) where TService : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var serviceType = typeof(TService);

            if (_services.ContainsKey(serviceType) || _factories.ContainsKey(serviceType))
                throw new InvalidOperationException($"Service of type {serviceType.Name} is already registered");

            _factories[serviceType] = () => factory();
        }

        /// <summary>
        /// Registers a service with automatic interface resolution.
        /// </summary>
        /// <typeparam name="TInterface">The interface type</typeparam>
        /// <typeparam name="TImplementation">The implementation type</typeparam>
        /// <param name="instance">The service instance</param>
        public void RegisterAs<TInterface, TImplementation>(TImplementation instance)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var interfaceType = typeof(TInterface);

            if (_services.ContainsKey(interfaceType) || _factories.ContainsKey(interfaceType))
                throw new InvalidOperationException($"Service of type {interfaceType.Name} is already registered");

            _services[interfaceType] = instance;
        }

        /// <summary>
        /// Resolves a service from the container.
        /// </summary>
        /// <typeparam name="TService">The service type</typeparam>
        /// <returns>The service instance, or null if not found</returns>
        public TService Resolve<TService>() where TService : class
        {
            var serviceType = typeof(TService);

            // Try to resolve from instances first
            if (_services.TryGetValue(serviceType, out var instance))
            {
                return (TService)instance;
            }

            // Try to resolve from factories
            if (_factories.TryGetValue(serviceType, out var factory))
            {
                var createdInstance = factory();
                // Store created instance for future resolution (singleton behavior)
                _services[serviceType] = createdInstance;
                return (TService)createdInstance;
            }

            return null;
        }

        /// <summary>
        /// Resolves a service from the container, throwing an exception if not found.
        /// </summary>
        /// <typeparam name="TService">The service type</typeparam>
        /// <returns>The service instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when service is not registered</exception>
        public TService ResolveRequired<TService>() where TService : class
        {
            var service = Resolve<TService>();
            if (service == null)
                throw new InvalidOperationException($"Service of type {typeof(TService).Name} is not registered");

            return service;
        }

        /// <summary>
        /// Checks if a service is registered in the container.
        /// </summary>
        /// <typeparam name="TService">The service type</typeparam>
        /// <returns>True if the service is registered</returns>
        public bool IsRegistered<TService>()
        {
            var serviceType = typeof(TService);
            return _services.ContainsKey(serviceType) || _factories.ContainsKey(serviceType);
        }

        /// <summary>
        /// Unregisters a service from the container.
        /// </summary>
        /// <typeparam name="TService">The service type</typeparam>
        /// <returns>True if the service was unregistered</returns>
        public bool Unregister<TService>()
        {
            var serviceType = typeof(TService);
            var removedFromServices = _services.Remove(serviceType);
            var removedFromFactories = _factories.Remove(serviceType);

            return removedFromServices || removedFromFactories;
        }

        /// <summary>
        /// Gets all registered service types.
        /// </summary>
        /// <returns>Collection of registered service types</returns>
        public IEnumerable<Type> GetRegisteredTypes()
        {
            var allTypes = _services.Keys.Concat(_factories.Keys);
            return allTypes.Distinct().ToList();
        }

        /// <summary>
        /// Creates a scope that automatically disposes services when the scope is disposed.
        /// </summary>
        /// <returns>A disposable scope</returns>
        public ServiceScope CreateScope()
        {
            return new ServiceScope(this);
        }

        /// <summary>
        /// Clears all registered services and factories.
        /// </summary>
        public void Clear()
        {
            // Dispose any disposable services
            foreach (var service in _services.Values)
            {
                if (service is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch
                    {
                        // Ignore disposal exceptions during cleanup
                    }
                }
            }

            // Clear correlated services
            _correlatedServices.Clear();

            _services.Clear();
            _factories.Clear();
        }

        /// <summary>
        /// Registers default test doubles following CLAUDETESTS.md guidelines.
        /// Provides lightweight TDD-compliant test doubles for common services.
        /// </summary>
        private void RegisterDefaultTestDoubles()
        {
            try
            {
                // Register shared test doubles if not already registered
                if (!IsRegistered<StubLoggingService>())
                {
                    var stubLogging = _loggingService ?? new StubLoggingService();
                    RegisterInstance(stubLogging);
                    _loggingService = stubLogging;
                }

                if (!IsRegistered<SpyMessageBusService>())
                {
                    RegisterInstance(new SpyMessageBusService());
                }

                if (!IsRegistered<FakeSerializationService>())
                {
                    RegisterInstance(new FakeSerializationService());
                }

                if (!IsRegistered<NullProfilerService>())
                {
                    RegisterInstance(NullProfilerService.Instance);
                }

                if (!IsRegistered<StubHealthCheckService>())
                {
                    RegisterInstance(new StubHealthCheckService());
                }

                _loggingService?.LogInfo("ServiceTestContainer initialized with default test doubles");
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Failed to register default test doubles: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers a service with correlation tracking for distributed system debugging.
        /// Essential for CLAUDETESTS.md compliance and test correlation tracking.
        /// </summary>
        /// <typeparam name="TService">The service type</typeparam>
        /// <param name="instance">The service instance</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        public void RegisterWithCorrelation<TService>(TService instance, Guid correlationId) where TService : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            if (correlationId == Guid.Empty)
                throw new ArgumentException("Correlation ID cannot be empty", nameof(correlationId));

            RegisterInstance(instance);
            _correlatedServices[correlationId] = instance;

            _loggingService?.LogInfo($"Registered service {typeof(TService).Name} with correlation ID {correlationId}");
        }

        /// <summary>
        /// Resolves a service by correlation ID for distributed system testing.
        /// Validates correlation tracking across service boundaries.
        /// </summary>
        /// <typeparam name="TService">The service type</typeparam>
        /// <param name="correlationId">Correlation ID to resolve</param>
        /// <returns>The correlated service instance</returns>
        public TService ResolveByCorrelation<TService>(Guid correlationId) where TService : class
        {
            if (_correlatedServices.TryGetValue(correlationId, out var service) && service is TService typedService)
            {
                _loggingService?.LogInfo($"Resolved service {typeof(TService).Name} by correlation ID {correlationId}");
                return typedService;
            }

            _loggingService?.LogWarning($"Failed to resolve service {typeof(TService).Name} by correlation ID {correlationId}");
            return null;
        }

        /// <summary>
        /// Validates that all registered test doubles are healthy and properly configured.
        /// Essential for comprehensive test health checks following CLAUDETESTS.md guidelines.
        /// </summary>
        /// <returns>True if all services are healthy</returns>
        public bool ValidateTestDoubleHealth()
        {
            try
            {
                var issues = new List<string>();

                // Validate logging service
                var loggingService = Resolve<StubLoggingService>();
                if (loggingService == null || !loggingService.IsEnabled)
                {
                    issues.Add("StubLoggingService is not available or disabled");
                }

                // Validate message bus service
                var messageBusService = Resolve<SpyMessageBusService>();
                if (messageBusService == null || !messageBusService.IsEnabled)
                {
                    issues.Add("SpyMessageBusService is not available or disabled");
                }

                // Validate serialization service
                var serializationService = Resolve<FakeSerializationService>();
                if (serializationService == null || !serializationService.IsEnabled)
                {
                    issues.Add("FakeSerializationService is not available or disabled");
                }

                // Validate profiler service
                var profilerService = Resolve<NullProfilerService>();
                if (profilerService == null)
                {
                    issues.Add("NullProfilerService is not available");
                }

                // Validate health check service
                var healthCheckService = Resolve<StubHealthCheckService>();
                if (healthCheckService == null || !healthCheckService.IsEnabled)
                {
                    issues.Add("StubHealthCheckService is not available or disabled");
                }

                if (issues.Count > 0)
                {
                    var issueList = string.Join(", ", issues);
                    _loggingService?.LogError($"Test double health validation failed: {issueList}");
                    return false;
                }

                _loggingService?.LogInfo("All test doubles are healthy and properly configured");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Test double health validation error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets comprehensive container statistics for debugging and monitoring.
        /// Provides detailed information about registered services and their states.
        /// </summary>
        /// <returns>Container diagnostic information</returns>
        public ContainerDiagnostics GetDiagnostics()
        {
            return new ContainerDiagnostics
            {
                RegisteredServiceCount = RegisteredServiceCount,
                InstanceCount = _services.Count,
                FactoryCount = _factories.Count,
                CorrelatedServiceCount = _correlatedServices.Count,
                IsHealthy = ValidateTestDoubleHealth(),
                RegisteredTypes = GetRegisteredTypes().Select(t => t.Name).ToList()
            };
        }

        /// <summary>
        /// Validates frame budget compliance for service resolution operations.
        /// Ensures container operations don't impact Unity's 60 FPS performance target.
        /// </summary>
        /// <param name="operation">The container operation to validate</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <returns>True if operation completed within frame budget</returns>
        public bool ValidateFrameBudgetCompliance(Action operation, string operationName)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                operation();
            }
            finally
            {
                stopwatch.Stop();
            }

            var isCompliant = stopwatch.Elapsed < TestConstants.FrameBudget;

            if (!isCompliant)
            {
                _loggingService?.LogWarning($"Container operation '{operationName}' exceeded frame budget: {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
            }
            else
            {
                _loggingService?.LogInfo($"Container operation '{operationName}' completed within frame budget: {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
            }

            return isCompliant;
        }

        /// <summary>
        /// Bulk registers multiple test doubles with validation for performance testing.
        /// Optimized for stress testing scenarios with 1000+ service registrations.
        /// </summary>
        /// <param name="services">Dictionary of service types to instances</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>Number of successfully registered services</returns>
        public int BulkRegisterServices(Dictionary<Type, object> services, Guid correlationId = default)
        {
            if (services == null || services.Count == 0)
                return 0;

            var successCount = 0;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                foreach (var kvp in services)
                {
                    try
                    {
                        if (!_services.ContainsKey(kvp.Key) && !_factories.ContainsKey(kvp.Key))
                        {
                            _services[kvp.Key] = kvp.Value;

                            if (correlationId != Guid.Empty)
                            {
                                _correlatedServices[correlationId] = kvp.Value;
                            }

                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogError($"Failed to register service {kvp.Key.Name}: {ex.Message}");
                    }
                }

                stopwatch.Stop();

                // Validate frame budget compliance for bulk operations
                if (stopwatch.Elapsed > TestConstants.FrameBudget)
                {
                    _loggingService?.LogWarning($"Bulk service registration exceeded frame budget: {stopwatch.Elapsed.TotalMilliseconds:F2}ms for {services.Count} services");
                }

                _loggingService?.LogInfo($"Bulk registered {successCount}/{services.Count} services in {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
                return successCount;
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Bulk registration failed: {ex.Message}");
                return successCount;
            }
        }

        /// <summary>
        /// Disposes the container and all registered services.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            Clear();
            _disposed = true;
        }

        /// <summary>
        /// Represents a service scope that can automatically clean up services.
        /// </summary>
        public sealed class ServiceScope : IDisposable
        {
            private readonly ServiceTestContainer _container;
            private readonly List<Type> _scopedTypes = new List<Type>();
            private bool _disposed = false;

            internal ServiceScope(ServiceTestContainer container)
            {
                _container = container ?? throw new ArgumentNullException(nameof(container));
            }

            /// <summary>
            /// Registers a scoped service that will be disposed when the scope is disposed.
            /// </summary>
            /// <typeparam name="TService">The service type</typeparam>
            /// <param name="instance">The service instance</param>
            public void RegisterScoped<TService>(TService instance) where TService : class
            {
                _container.RegisterInstance(instance);
                _scopedTypes.Add(typeof(TService));
            }

            /// <summary>
            /// Resolves a service from the parent container.
            /// </summary>
            /// <typeparam name="TService">The service type</typeparam>
            /// <returns>The service instance</returns>
            public TService Resolve<TService>() where TService : class
            {
                return _container.Resolve<TService>();
            }

            /// <summary>
            /// Disposes the scope and unregisters scoped services.
            /// </summary>
            public void Dispose()
            {
                if (_disposed)
                    return;

                // Unregister scoped services
                foreach (var scopedType in _scopedTypes)
                {
                    if (_container._services.TryGetValue(scopedType, out var service))
                    {
                        if (service is IDisposable disposable)
                        {
                            try
                            {
                                disposable.Dispose();
                            }
                            catch
                            {
                                // Ignore disposal exceptions
                            }
                        }

                        _container._services.Remove(scopedType);
                    }
                }

                _scopedTypes.Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Diagnostic information about the ServiceTestContainer state.
    /// Provides comprehensive container health and configuration details.
    /// </summary>
    public sealed class ContainerDiagnostics
    {
        /// <summary>
        /// Gets the total number of registered services.
        /// </summary>
        public int RegisteredServiceCount { get; set; }

        /// <summary>
        /// Gets the number of registered service instances.
        /// </summary>
        public int InstanceCount { get; set; }

        /// <summary>
        /// Gets the number of registered service factories.
        /// </summary>
        public int FactoryCount { get; set; }

        /// <summary>
        /// Gets the number of services registered with correlation IDs.
        /// </summary>
        public int CorrelatedServiceCount { get; set; }

        /// <summary>
        /// Gets whether all test doubles are healthy and properly configured.
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Gets the list of registered service type names.
        /// </summary>
        public List<string> RegisteredTypes { get; set; } = new List<string>();

        /// <summary>
        /// Returns a comprehensive diagnostic summary.
        /// </summary>
        /// <returns>Formatted diagnostic information</returns>
        public override string ToString()
        {
            return $"ServiceTestContainer Diagnostics: " +
                   $"Services={RegisteredServiceCount}, " +
                   $"Instances={InstanceCount}, " +
                   $"Factories={FactoryCount}, " +
                   $"Correlated={CorrelatedServiceCount}, " +
                   $"Healthy={IsHealthy}";
        }
    }
}