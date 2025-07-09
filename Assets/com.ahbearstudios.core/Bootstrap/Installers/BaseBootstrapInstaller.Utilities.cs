using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using AhBearStudios.Core.Bootstrap.Interfaces;
using Reflex.Core;
using Unity.Collections;

namespace AhBearStudios.Core.Bootstrap.Installers
{
    public abstract partial class BaseBootstrapInstaller
    {
        #region Utility Methods

        /// <summary>
        /// Measures the execution time of an action and logs the result.
        /// </summary>
        protected TimeSpan MeasureExecutionTime(Action action, string operationName = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var stopwatch = Stopwatch.StartNew();
            try
            {
                action();
            }
            finally
            {
                stopwatch.Stop();
                if (!string.IsNullOrEmpty(operationName))
                {
                    _logger?.LogInfo($"{operationName} completed in {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
                }
            }
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Measures the execution time of an async action and logs the result.
        /// </summary>
        protected async Task<TimeSpan> MeasureExecutionTimeAsync(Func<Task> action, string operationName = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await action();
            }
            finally
            {
                stopwatch.Stop();
                if (!string.IsNullOrEmpty(operationName))
                {
                    _logger?.LogInfo($"{operationName} completed in {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
                }
            }
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Safely executes an action with exception handling and logging.
        /// </summary>
        protected bool TryExecute(Action action, string operationName = null, bool logErrors = true)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                if (logErrors)
                {
                    var operation = operationName ?? "Operation";
                    _logger?.LogException(ex, $"{operation} failed in {InstallerName}");
                    IncrementErrorCount();
                }
                return false;
            }
        }

        /// <summary>
        /// Safely executes an async action with exception handling and logging.
        /// </summary>
        protected async Task<bool> TryExecuteAsync(Func<Task> action, string operationName = null, bool logErrors = true)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            try
            {
                await action();
                return true;
            }
            catch (Exception ex)
            {
                if (logErrors)
                {
                    var operation = operationName ?? "Operation";
                    _logger?.LogException(ex, $"{operation} failed in {InstallerName}");
                    IncrementErrorCount();
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the current memory usage in bytes.
        /// </summary>
        protected long GetCurrentMemoryUsage()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return GC.GetTotalMemory(false);
        }

        /// <summary>
        /// Validates that a type implements the required interface.
        /// </summary>
        protected bool ValidateTypeImplementsInterface<TInterface>(Type type)
        {
            if (type == null)
                return false;

            return typeof(TInterface).IsAssignableFrom(type);
        }

        /// <summary>
        /// Validates that a type has a parameterless constructor.
        /// </summary>
        protected bool ValidateTypeHasParameterlessConstructor(Type type)
        {
            if (type == null)
                return false;

            return type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) != null;
        }

        /// <summary>
        /// Gets a user-friendly description of the installer for logging and diagnostics.
        /// </summary>
        protected string GetInstallerDescription()
        {
            return $"{InstallerName} (Priority: {Priority}, Category: {Category}, Enabled: {IsEnabled})";
        }

        /// <summary>
        /// Logs the start of an installation phase.
        /// </summary>
        protected void LogPhaseStart(string phaseName)
        {
            _logger?.LogInfo($"[{InstallerName}] Starting {phaseName} phase (CorrelationId: {_correlationId})");
        }

        /// <summary>
        /// Logs the end of an installation phase.
        /// </summary>
        protected void LogPhaseEnd(string phaseName, TimeSpan duration)
        {
            _logger?.LogInfo($"[{InstallerName}] Completed {phaseName} phase in {duration.TotalMilliseconds:F2}ms (CorrelationId: {_correlationId})");
        }

        /// <summary>
        /// Validates that all required dependencies are available in the Reflex container.
        /// </summary>
        protected bool ValidateDependencies(Container container)
        {
            if (container == null)
                return false;

            if (Dependencies == null || Dependencies.Length == 0)
                return true;

            foreach (var dependency in Dependencies)
            {
                if (dependency == null)
                {
                    _logger?.LogError($"Null dependency found in {InstallerName}");
                    return false;
                }

                // Use Reflex's proper way to check if a type is registered
                if (!container.HasBinding(dependency))
                {
                    _logger?.LogError($"Required dependency {dependency.Name} is not registered in container");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a correlation ID for this installation session.
        /// </summary>
        protected FixedString64Bytes CreateCorrelationId()
        {
            var id = $"{InstallerName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
            return new FixedString64Bytes(id.Length > 64 ? id.Substring(0, 64) : id);
        }

        /// <summary>
        /// Helper method to register a service with Reflex DI container.
        /// </summary>
        protected void RegisterService<TInterface, TImplementation>(Container container)
            where TImplementation : class, TInterface
        {
            try
            {
                container.Bind<TInterface>().To<TImplementation>();
                _logger?.LogDebug($"Registered {typeof(TInterface).Name} -> {typeof(TImplementation).Name}");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"Failed to register service {typeof(TInterface).Name}");
                throw;
            }
        }

        /// <summary>
        /// Helper method to register a singleton service with Reflex DI container.
        /// </summary>
        protected void RegisterSingleton<TInterface, TImplementation>(Container container)
            where TImplementation : class, TInterface
        {
            try
            {
                container.Bind<TInterface>().To<TImplementation>().AsSingleton();
                _logger?.LogDebug($"Registered singleton {typeof(TInterface).Name} -> {typeof(TImplementation).Name}");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"Failed to register singleton {typeof(TInterface).Name}");
                throw;
            }
        }

        /// <summary>
        /// Helper method to register a transient service with Reflex DI container.
        /// </summary>
        protected void RegisterTransient<TInterface, TImplementation>(Container container)
            where TImplementation : class, TInterface
        {
            try
            {
                container.Bind<TInterface>().To<TImplementation>().AsTransient();
                _logger?.LogDebug($"Registered transient {typeof(TInterface).Name} -> {typeof(TImplementation).Name}");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"Failed to register transient {typeof(TInterface).Name}");
                throw;
            }
        }

        /// <summary>
        /// Helper method to register an instance with Reflex DI container.
        /// </summary>
        protected void RegisterInstance<TInterface>(Container container, TInterface instance)
            where TInterface : class
        {
            try
            {
                container.Bind<TInterface>().FromInstance(instance);
                _logger?.LogDebug($"Registered instance {typeof(TInterface).Name}");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"Failed to register instance {typeof(TInterface).Name}");
                throw;
            }
        }

        /// <summary>
        /// Helper method to register a factory with Reflex DI container.
        /// </summary>
        protected void RegisterFactory<TInterface>(Container container, Func<Container, TInterface> factory)
            where TInterface : class
        {
            try
            {
                container.Bind<TInterface>().FromFunction(factory);
                _logger?.LogDebug($"Registered factory {typeof(TInterface).Name}");
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"Failed to register factory {typeof(TInterface).Name}");
                throw;
            }
        }

        /// <summary>
        /// Helper method to check if a service is registered in the container.
        /// </summary>
        protected bool IsServiceRegistered<TInterface>(Container container)
        {
            return container.HasBinding<TInterface>();
        }

        /// <summary>
        /// Helper method to resolve a service from the container.
        /// </summary>
        protected TInterface ResolveService<TInterface>(Container container)
        {
            try
            {
                return container.Resolve<TInterface>();
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, $"Failed to resolve service {typeof(TInterface).Name}");
                throw;
            }
        }

        /// <summary>
        /// Helper method to try resolve a service from the container.
        /// </summary>
        protected bool TryResolveService<TInterface>(Container container, out TInterface service)
        {
            try
            {
                service = container.Resolve<TInterface>();
                return true;
            }
            catch
            {
                service = default(TInterface);
                return false;
            }
        }

        #endregion
    }
}