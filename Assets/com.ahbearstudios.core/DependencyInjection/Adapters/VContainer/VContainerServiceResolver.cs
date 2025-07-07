using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AhBearStudios.Core.DependencyInjection.Exceptions;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Messages;
using AhBearStudios.Core.DependencyInjection.Models;
using AhBearStudios.Core.MessageBus.Interfaces;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Providers.VContainer
{
    /// <summary>
    /// Enhanced VContainer implementation of IServiceResolver that wraps an IObjectResolver.
    /// Provides comprehensive service resolution with metrics, logging, and message bus integration.
    /// Supports all advanced features including named services and scoping where available.
    /// </summary>
    public sealed class VContainerServiceResolver : IServiceResolver
    {
        private readonly IObjectResolver _resolver;
        private readonly string _containerName;
        private readonly IDependencyInjectionConfig _config;
        private readonly IMessageBusService _messageBusService;
        private bool _disposed;

        /// <summary>
        /// Gets the container framework this resolver uses.
        /// </summary>
        public ContainerFramework Framework => ContainerFramework.VContainer;

        /// <summary>
        /// Gets whether this resolver has been disposed.
        /// </summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// Initializes a new instance of the VContainerServiceResolver class.
        /// </summary>
        /// <param name="resolver">The VContainer object resolver to wrap.</param>
        /// <param name="containerName">Optional container name for logging and events.</param>
        /// <param name="config">Optional configuration for behavior control.</param>
        /// <param name="messageBusService">Optional message bus for publishing resolution events.</param>
        /// <exception cref="ArgumentNullException">Thrown when resolver is null.</exception>
        public VContainerServiceResolver(
            IObjectResolver resolver,
            string containerName = null,
            IDependencyInjectionConfig config = null,
            IMessageBusService messageBusService = null)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _containerName = containerName ?? "VContainer";
            _config = config;
            _messageBusService = messageBusService;
        }

        /// <summary>
        /// Resolves a service of the specified type from the container.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ServiceResolutionException">Thrown when the service cannot be resolved.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the resolver has been disposed.</exception>
        public T Resolve<T>()
        {
            ThrowIfDisposed();

            var serviceType = typeof(T);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = _resolver.Resolve<T>();
                stopwatch.Stop();

                // Publish successful resolution event
                PublishResolutionEvent(serviceType, result, stopwatch.Elapsed, true);

                if (_config?.EnableDebugLogging == true)
                {
                    LogResolution(serviceType, stopwatch.Elapsed, true);
                }

                return result;
            }
            catch (VContainerException ex)
            {
                stopwatch.Stop();

                // Publish failed resolution event
                PublishResolutionFailedEvent(serviceType, ex.Message, stopwatch.Elapsed);

                if (_config?.EnableDebugLogging == true)
                {
                    LogResolution(serviceType, stopwatch.Elapsed, false, ex.Message);
                }

                throw new ServiceResolutionException(
                    serviceType,
                    $"Failed to resolve service of type '{serviceType.FullName}' from VContainer: {ex.Message}",
                    ex,
                    Framework,
                    _containerName);
            }
            catch (Exception ex) when (!(ex is DependencyInjectionException))
            {
                stopwatch.Stop();

                // Publish failed resolution event
                PublishResolutionFailedEvent(serviceType, ex.Message, stopwatch.Elapsed);

                if (_config?.EnableDebugLogging == true)
                {
                    LogResolution(serviceType, stopwatch.Elapsed, false, ex.Message);
                }

                throw new ServiceResolutionException(
                    serviceType,
                    $"Unexpected error resolving service of type '{serviceType.FullName}' from VContainer: {ex.Message}",
                    ex,
                    Framework,
                    _containerName);
            }
        }

        /// <summary>
        /// Attempts to resolve a service, returning false if not found.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="service">The resolved service if successful.</param>
        /// <returns>True if resolution was successful, false otherwise.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the resolver has been disposed.</exception>
        public bool TryResolve<T>(out T service)
        {
            ThrowIfDisposed();

            var serviceType = typeof(T);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // VContainer's TryResolve method returns true if successful
                var result = _resolver.TryResolve<T>(out service);
                stopwatch.Stop();

                if (result)
                {
                    // Publish successful resolution event
                    PublishResolutionEvent(serviceType, service, stopwatch.Elapsed, true);
                }

                if (_config?.EnableDebugLogging == true)
                {
                    LogTryResolve(serviceType, stopwatch.Elapsed, result);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                if (_config?.EnableDebugLogging == true)
                {
                    LogTryResolve(serviceType, stopwatch.Elapsed, false, ex.Message);
                }

                // If any exception occurs during resolution, consider it a failure
                service = default;
                return false;
            }
        }

        /// <summary>
        /// Resolves a service or returns a default value if not found.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="defaultValue">The default value to return if resolution fails.</param>
        /// <returns>The resolved service or default value.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the resolver has been disposed.</exception>
        public T ResolveOrDefault<T>(T defaultValue = default)
        {
            ThrowIfDisposed();

            // Use TryResolve to attempt resolution safely
            if (TryResolve<T>(out T service))
            {
                return service;
            }

            return defaultValue;
        }

        /// <summary>
        /// Resolves all registered implementations of the specified type.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns>All registered implementations.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the resolver has been disposed.</exception>
        public IEnumerable<T> ResolveAll<T>()
        {
            ThrowIfDisposed();

            var serviceType = typeof(T);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // VContainer supports resolving collections via IEnumerable<T>
                var result = _resolver.Resolve<IEnumerable<T>>();
                stopwatch.Stop();

                var resultList = result.ToList();

                // Publish resolution event for collection
                PublishResolutionEvent(typeof(IEnumerable<T>), resultList, stopwatch.Elapsed, true);

                if (_config?.EnableDebugLogging == true)
                {
                    LogResolveAll(serviceType, resultList.Count, stopwatch.Elapsed);
                }

                return resultList;
            }
            catch (VContainerException)
            {
                stopwatch.Stop();

                if (_config?.EnableDebugLogging == true)
                {
                    LogResolveAll(serviceType, 0, stopwatch.Elapsed);
                }

                // Return empty collection if no registrations found
                return Enumerable.Empty<T>();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                if (_config?.EnableDebugLogging == true)
                {
                    Console.WriteLine(
                        $"[VContainer] Error resolving all services of type {serviceType.Name}: {ex.Message}");
                }

                // Return empty collection on error
                return Enumerable.Empty<T>();
            }
        }

        /// <summary>
        /// Resolves a named service if named services are enabled.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <param name="name">The service name identifier.</param>
        /// <returns>The named service instance.</returns>
        /// <exception cref="ServiceResolutionException">Thrown when named service cannot be resolved.</exception>
        /// <exception cref="NotSupportedException">Thrown when named services are not enabled.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the resolver has been disposed.</exception>
        public T ResolveNamed<T>(string name)
        {
            ThrowIfDisposed();

            if (_config?.EnableNamedServices != true)
            {
                throw new NotSupportedException(
                    "Named services are not enabled in the current configuration. " +
                    "Enable named services in the DI configuration to use this feature.");
            }

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Service name cannot be null or empty", nameof(name));

            // VContainer doesn't natively support named services like some other containers
            // This is a limitation we document and potentially work around with conventions
            throw new NotSupportedException(
                "VContainer does not natively support named service resolution. " +
                "Consider using factory patterns or keyed services for similar functionality.");
        }

        /// <summary>
        /// Attempts to resolve a named service.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <param name="name">The service name identifier.</param>
        /// <param name="service">The resolved service if successful.</param>
        /// <returns>True if resolution was successful, false otherwise.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the resolver has been disposed.</exception>
        public bool TryResolveNamed<T>(string name, out T service)
        {
            ThrowIfDisposed();

            service = default;

            if (_config?.EnableNamedServices != true)
            {
                return false;
            }

            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            // VContainer doesn't natively support named services
            // Return false to indicate this feature is not available
            return false;
        }

        /// <summary>
        /// Creates a scoped resolver if scoping is enabled.
        /// VContainer doesn't have traditional scoping, so this creates a child container scope.
        /// </summary>
        /// <param name="scopeName">Optional name for the scope.</param>
        /// <returns>A new scoped resolver.</returns>
        /// <exception cref="NotSupportedException">Thrown when scoping is not enabled or not supported.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the resolver has been disposed.</exception>
        public IServiceResolver CreateScope(string scopeName = null)
        {
            ThrowIfDisposed();

            if (_config?.EnableScoping != true)
            {
                throw new NotSupportedException(
                    "Scoping is not enabled in the current configuration. " +
                    "Enable scoping in the DI configuration to use this feature.");
            }

            // VContainer doesn't have traditional scoping like other DI containers
            // We can simulate scoping by creating a child container scope
            try
            {
                // Check if VContainer has CreateScope method on resolver
                var createScopeMethod = _resolver.GetType().GetMethod("CreateScope");
                if (createScopeMethod != null)
                {
                    // Use VContainer's native CreateScope if available
                    var scope = createScopeMethod.Invoke(_resolver, null);
                    if (scope is IObjectResolver scopedResolver)
                    {
                        return new VContainerServiceResolver(
                            scopedResolver,
                            scopeName ?? $"{_containerName}_Scope_{Guid.NewGuid():N}",
                            _config,
                            _messageBusService);
                    }
                }

                // Fallback: VContainer doesn't have traditional scoping
                // Return the same resolver but with a different name to simulate scoping
                // In VContainer, child containers are typically created at the builder level
                if (_config?.EnableDebugLogging == true)
                {
                    Console.WriteLine($"[VContainer] VContainer doesn't support traditional scoping. " +
                                      $"Returning same resolver with scope name '{scopeName ?? "unnamed"}'");
                }

                // Return a new resolver wrapper that delegates to the same underlying resolver
                // This maintains the scoping contract while acknowledging VContainer's limitations
                return new VContainerServiceResolver(
                    _resolver,
                    scopeName ?? $"{_containerName}_Scope_{Guid.NewGuid():N}",
                    _config,
                    _messageBusService);
            }
            catch (Exception ex)
            {
                if (_config?.EnableDebugLogging == true)
                {
                    Console.WriteLine($"[VContainer] Failed to create scoped resolver: {ex.Message}");
                }

                throw new NotSupportedException(
                    $"VContainer does not support traditional scoping. Failed to create scope: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disposes the service resolver and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                // VContainer's IObjectResolver doesn't typically implement IDisposable
                // But if it does, we should dispose it
                if (_resolver is IDisposable disposableResolver)
                {
                    disposableResolver.Dispose();
                }

                if (_config?.EnableDebugLogging == true)
                {
                    Console.WriteLine($"[VContainer] Disposed service resolver for container '{_containerName}'");
                }

                _disposed = true;
            }
            catch (Exception ex)
            {
                if (_config?.EnableDebugLogging == true)
                {
                    Console.WriteLine($"[VContainer] Error disposing service resolver: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Throws ObjectDisposedException if the resolver has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(VContainerServiceResolver));
        }

        /// <summary>
        /// Publishes a successful service resolution event to the message bus.
        /// </summary>
        private void PublishResolutionEvent(Type serviceType, object instance, TimeSpan resolutionTime,
            bool wasSuccessful)
        {
            if (_messageBusService == null) return;

            try
            {
                var message = new ServiceResolvedMessage(
                    _containerName,
                    serviceType,
                    instance,
                    resolutionTime,
                    wasSuccessful);

                _messageBusService.PublishMessage(message);
            }
            catch (Exception ex)
            {
                if (_config?.EnableDebugLogging == true)
                {
                    Console.WriteLine($"[VContainer] Failed to publish resolution event: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Publishes a failed service resolution event to the message bus.
        /// </summary>
        private void PublishResolutionFailedEvent(Type serviceType, string errorMessage,
            TimeSpan attemptedResolutionTime)
        {
            if (_messageBusService == null) return;

            try
            {
                var message = new ServiceResolutionFailedMessage(
                    _containerName,
                    serviceType,
                    errorMessage,
                    attemptedResolutionTime);

                _messageBusService.PublishMessage(message);
            }
            catch (Exception ex)
            {
                if (_config?.EnableDebugLogging == true)
                {
                    Console.WriteLine($"[VContainer] Failed to publish resolution failed event: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Logs a service resolution attempt.
        /// </summary>
        private void LogResolution(Type serviceType, TimeSpan resolutionTime, bool success, string errorMessage = null)
        {
            var status = success ? "SUCCESS" : "FAILED";
            var message =
                $"[VContainer] Resolution {status} for {serviceType.Name} in {resolutionTime.TotalMilliseconds:F2}ms";

            if (!success && !string.IsNullOrEmpty(errorMessage))
            {
                message += $" - {errorMessage}";
            }

            Console.WriteLine(message);
        }

        /// <summary>
        /// Logs a TryResolve attempt.
        /// </summary>
        private void LogTryResolve(Type serviceType, TimeSpan resolutionTime, bool success, string errorMessage = null)
        {
            var status = success ? "SUCCESS" : "NOT_FOUND";
            var message =
                $"[VContainer] TryResolve {status} for {serviceType.Name} in {resolutionTime.TotalMilliseconds:F2}ms";

            if (!success && !string.IsNullOrEmpty(errorMessage))
            {
                message += $" - {errorMessage}";
            }

            Console.WriteLine(message);
        }

        /// <summary>
        /// Logs a ResolveAll attempt.
        /// </summary>
        private void LogResolveAll(Type serviceType, int count, TimeSpan resolutionTime)
        {
            Console.WriteLine(
                $"[VContainer] ResolveAll found {count} implementations of {serviceType.Name} in {resolutionTime.TotalMilliseconds:F2}ms");
        }
    }
}