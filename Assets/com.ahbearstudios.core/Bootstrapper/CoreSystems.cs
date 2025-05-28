using System;
using UnityEngine;
using VContainer;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Pooling.Interfaces;

namespace AhBearStudios.Core.Bootstrap
{
    /// <summary>
    /// Static accessor for core systems that provides convenient access throughout the application.
    /// Uses service locator pattern backed by VContainer for dependency resolution.
    /// </summary>
    public static class CoreSystems
    {
        private static IObjectResolver resolver;
        private static bool isInitialized = false;
        
        /// <summary>
        /// Initializes the core systems accessor with the DI container resolver.
        /// Called automatically by the ApplicationBootstrapper.
        /// </summary>
        /// <param name="objectResolver">The VContainer object resolver.</param>
        internal static void Initialize(IObjectResolver objectResolver)
        {
            resolver = objectResolver ?? throw new ArgumentNullException(nameof(objectResolver));
            isInitialized = true;
            
            Debug.Log("[CoreSystems] Initialized with DI container");
        }
        
        /// <summary>
        /// Shuts down the core systems accessor.
        /// Called automatically when the application is shutting down.
        /// </summary>
        internal static void Shutdown()
        {
            resolver = null;
            isInitialized = false;
            
            Debug.Log("[CoreSystems] Shutdown completed");
        }
        
        /// <summary>
        /// Gets whether the core systems have been initialized.
        /// </summary>
        public static bool IsInitialized => isInitialized;
        
        /// <summary>
        /// Resolves a service from the DI container.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when core systems are not initialized.</exception>
        /// <exception cref="VContainerException">Thrown when the service cannot be resolved.</exception>
        public static T Resolve<T>()
        {
            ThrowIfNotInitialized();
            
            try
            {
                return resolver.Resolve<T>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to resolve service of type {typeof(T).Name}: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Tries to resolve a service from the DI container.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <param name="service">The resolved service instance, or default if resolution fails.</param>
        /// <returns>True if the service was successfully resolved; otherwise, false.</returns>
        public static bool TryResolve<T>(out T service)
        {
            service = default(T);
            
            if (!isInitialized)
                return false;
            
            try
            {
                service = resolver.Resolve<T>();
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets the main application logger.
        /// </summary>
        public static IBurstLogger Logger
        {
            get
            {
                ThrowIfNotInitialized();
                return resolver.Resolve<IBurstLogger>();
            }
        }
        
        /// <summary>
        /// Gets the message bus for inter-system communication.
        /// </summary>
        public static IMessageBus MessageBus
        {
            get
            {
                ThrowIfNotInitialized();
                return resolver.Resolve<IMessageBus>();
            }
        }
        
        /// <summary>
        /// Gets the profiler for performance monitoring.
        /// </summary>
        public static IProfiler Profiler
        {
            get
            {
                ThrowIfNotInitialized();
                return resolver.Resolve<IProfiler>();
            }
        }
        
        /// <summary>
        /// Gets the pool registry for object pooling.
        /// </summary>
        public static IPoolRegistry PoolRegistry
        {
            get
            {
                ThrowIfNotInitialized();
                return resolver.Resolve<IPoolRegistry>();
            }
        }
        
        /// <summary>
        /// Gets a specific pool by type.
        /// </summary>
        /// <typeparam name="T">The type of objects in the pool.</typeparam>
        /// <returns>The pool for the specified type, or null if not found.</returns>
        public static IPool<T> GetPool<T>()
        {
            ThrowIfNotInitialized();
            return PoolRegistry.GetPoolByType<T>();
        }
        
        /// <summary>
        /// Creates a logger with the specified tag.
        /// </summary>
        /// <param name="tag">The tag to identify the logger source.</param>
        /// <returns>A logger instance configured with the specified tag.</returns>
        public static IBurstLogger CreateLogger(string tag)
        {
            ThrowIfNotInitialized();
            
            try
            {
                var factory = resolver.Resolve<ILoggerFactory>();
                var config = resolver.Resolve<ILoggerConfig>();
                
                // Create a logger with custom tag
                return factory.CreateLogger(config);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CoreSystems] Failed to create logger with tag '{tag}': {ex.Message}");
                return Logger; // Fallback to main logger
            }
        }
        
        /// <summary>
        /// Publishes a message to the message bus.
        /// </summary>
        /// <typeparam name="T">The type of message to publish.</typeparam>
        /// <param name="message">The message to publish.</param>
        public static void PublishMessage<T>(T message) where T : IMessage
        {
            ThrowIfNotInitialized();
            MessageBus.PublishMessage(message);
        }
        
        /// <summary>
        /// Subscribes to messages of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of message to subscribe to.</typeparam>
        /// <param name="handler">The handler to invoke when a message is received.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        public static IDisposable SubscribeToMessage<T>(Action<T> handler) where T : IMessage
        {
            ThrowIfNotInitialized();
            return MessageBus.SubscribeToMessage(handler);
        }
        
        /// <summary>
        /// Begins a profiler scope with the specified name.
        /// </summary>
        /// <param name="sampleName">The name of the profiler sample.</param>
        /// <returns>A disposable that ends the profiler scope when disposed.</returns>
        public static IDisposable BeginProfilingScope(string sampleName)
        {
            if (!isInitialized)
                return EmptyDisposable.Instance;
            
            try
            {
                return Profiler.BeginSample(sampleName);
            }
            catch
            {
                return EmptyDisposable.Instance;
            }
        }
        
        /// <summary>
        /// Logs a message using the main application logger.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The tag to identify the source.</param>
        public static void Log(byte level, string message, string tag = "Application")
        {
            if (!isInitialized)
            {
                Debug.Log($"[{tag}] {message}");
                return;
            }
            
            try
            {
                Logger.Log(level, message, tag);
            }
            catch
            {
                // Fallback to Unity logging if core logger fails
                Debug.Log($"[{tag}] {message}");
            }
        }
        
        /// <summary>
        /// Logs an info message using the main application logger.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The tag to identify the source.</param>
        public static void LogInfo(string message, string tag = "Application")
        {
            Log(1, message, tag); // Info level
        }
        
        /// <summary>
        /// Logs a warning message using the main application logger.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The tag to identify the source.</param>
        public static void LogWarning(string message, string tag = "Application")
        {
            Log(2, message, tag); // Warning level
        }
        
        /// <summary>
        /// Logs an error message using the main application logger.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The tag to identify the source.</param>
        public static void LogError(string message, string tag = "Application")
        {
            Log(3, message, tag); // Error level
        }
        
        private static void ThrowIfNotInitialized()
        {
            if (!isInitialized)
                throw new InvalidOperationException("CoreSystems has not been initialized. Ensure ApplicationBootstrapper has completed startup.");
        }
        
        /// <summary>
        /// Empty disposable implementation for fallback scenarios.
        /// </summary>
        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new EmptyDisposable();
            
            private EmptyDisposable() { }
            
            public void Dispose() { }
        }
    }
}