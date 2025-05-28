using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Pooling.Interfaces;

namespace AhBearStudios.Core.Extensions
{
    /// <summary>
    /// Extension methods for Unity objects that provide convenient access to core systems.
    /// Supports both VContainer field injection and service locator patterns.
    /// </summary>
    public static class UnityObjectExtensions
    {
        /// <summary>
        /// Resolves a service from the DI container using the nearest LifetimeScope.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <param name="behaviour">The MonoBehaviour to find the scope from.</param>
        /// <returns>The resolved service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no LifetimeScope is found or service cannot be resolved.</exception>
        public static T Resolve<T>(this MonoBehaviour behaviour)
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            // First try to find a LifetimeScope on this GameObject or its parents
            var scope = behaviour.GetComponentInParent<LifetimeScope>();
            if (scope != null)
            {
                return scope.Container.Resolve<T>();
            }
            
            // Fallback to global CoreSystems accessor
            return CoreSystems.Resolve<T>();
        }
        
        /// <summary>
        /// Tries to resolve a service from the DI container.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <param name="behaviour">The MonoBehaviour to find the scope from.</param>
        /// <param name="service">The resolved service instance, or default if resolution fails.</param>
        /// <returns>True if the service was successfully resolved; otherwise, false.</returns>
        public static bool TryResolve<T>(this MonoBehaviour behaviour, out T service)
        {
            service = default(T);
            
            if (behaviour == null)
                return false;
            
            try
            {
                service = behaviour.Resolve<T>();
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets a logger for this MonoBehaviour using its type name as the tag.
        /// </summary>
        /// <param name="behaviour">The MonoBehaviour to create a logger for.</param>
        /// <returns>A logger instance configured for this MonoBehaviour.</returns>
        public static IBurstLogger GetLogger(this MonoBehaviour behaviour)
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            return CoreSystems.CreateLogger(behaviour.GetType().Name);
        }
        
        /// <summary>
        /// Gets a logger for this MonoBehaviour with a custom tag.
        /// </summary>
        /// <param name="behaviour">The MonoBehaviour to create a logger for.</param>
        /// <param name="tag">The custom tag to use for the logger.</param>
        /// <returns>A logger instance configured with the specified tag.</returns>
        public static IBurstLogger GetLogger(this MonoBehaviour behaviour, string tag)
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            if (string.IsNullOrEmpty(tag))
                tag = behaviour.GetType().Name;
            
            return CoreSystems.CreateLogger(tag);
        }
        
        /// <summary>
        /// Gets a pool for the specified type.
        /// </summary>
        /// <typeparam name="T">The type of objects in the pool.</typeparam>
        /// <param name="behaviour">The MonoBehaviour requesting the pool.</param>
        /// <returns>The pool for the specified type, or null if not found.</returns>
        public static IPool<T> GetPool<T>(this MonoBehaviour behaviour)
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            return CoreSystems.GetPool<T>();
        }
        
        /// <summary>
        /// Gets a pool by name.
        /// </summary>
        /// <param name="behaviour">The MonoBehaviour requesting the pool.</param>
        /// <param name="poolName">The name of the pool to retrieve.</param>
        /// <returns>The named pool, or null if not found.</returns>
        public static IPool GetPool(this MonoBehaviour behaviour, string poolName)
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            if (string.IsNullOrEmpty(poolName))
                throw new ArgumentException("Pool name cannot be null or empty", nameof(poolName));
            
            return CoreSystems.PoolRegistry.GetPool(poolName);
        }
        
        /// <summary>
        /// Publishes a message to the message bus.
        /// </summary>
        /// <typeparam name="T">The type of message to publish.</typeparam>
        /// <param name="behaviour">The MonoBehaviour publishing the message.</param>
        /// <param name="message">The message to publish.</param>
        public static void PublishMessage<T>(this MonoBehaviour behaviour, T message) where T : IMessage
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            CoreSystems.PublishMessage(message);
        }
        
        /// <summary>
        /// Subscribes to messages of the specified type.
        /// The subscription will be automatically disposed when the MonoBehaviour is destroyed.
        /// </summary>
        /// <typeparam name="T">The type of message to subscribe to.</typeparam>
        /// <param name="behaviour">The MonoBehaviour subscribing to messages.</param>
        /// <param name="handler">The handler to invoke when a message is received.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        public static IDisposable SubscribeToMessage<T>(this MonoBehaviour behaviour, Action<T> handler) where T : IMessage
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            var subscription = CoreSystems.SubscribeToMessage(handler);
            
            // Ensure subscription is disposed when the MonoBehaviour is destroyed
            var disposer = behaviour.gameObject.GetComponent<SubscriptionDisposer>();
            if (disposer == null)
            {
                disposer = behaviour.gameObject.AddComponent<SubscriptionDisposer>();
            }
            
            disposer.AddSubscription(subscription);
            
            return subscription;
        }
        
        /// <summary>
        /// Begins a profiler scope for this MonoBehaviour.
        /// </summary>
        /// <param name="behaviour">The MonoBehaviour to profile.</param>
        /// <param name="sampleName">The name of the profiler sample. If null, uses the MonoBehaviour type name.</param>
        /// <returns>A disposable that ends the profiler scope when disposed.</returns>
        public static IDisposable BeginProfilingScope(this MonoBehaviour behaviour, string sampleName = null)
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            if (string.IsNullOrEmpty(sampleName))
                sampleName = behaviour.GetType().Name;
            
            return CoreSystems.BeginProfilingScope(sampleName);
        }
        
        /// <summary>
        /// Logs an info message using the MonoBehaviour's type name as the tag.
        /// </summary>
        /// <param name="behaviour">The MonoBehaviour logging the message.</param>
        /// <param name="message">The message to log.</param>
        public static void LogInfo(this MonoBehaviour behaviour, string message)
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            CoreSystems.LogInfo(message, behaviour.GetType().Name);
        }
        
        /// <summary>
        /// Logs a warning message using the MonoBehaviour's type name as the tag.
        /// </summary>
        /// <param name="behaviour">The MonoBehaviour logging the message.</param>
        /// <param name="message">The message to log.</param>
        public static void LogWarning(this MonoBehaviour behaviour, string message)
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            CoreSystems.LogWarning(message, behaviour.GetType().Name);
        }
        
        /// <summary>
        /// Logs an error message using the MonoBehaviour's type name as the tag.
        /// </summary>
        /// <param name="behaviour">The MonoBehaviour logging the message.</param>
        /// <param name="message">The message to log.</param>
        public static void LogError(this MonoBehaviour behaviour, string message)
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            CoreSystems.LogError(message, behaviour.GetType().Name);
        }
        
        /// <summary>
        /// Extension methods for GameObjects.
        /// </summary>
        public static class GameObjectExtensions
        {
            /// <summary>
            /// Resolves a service from the DI container using the nearest LifetimeScope.
            /// </summary>
            /// <typeparam name="T">The type of service to resolve.</typeparam>
            /// <param name="gameObject">The GameObject to find the scope from.</param>
            /// <returns>The resolved service instance.</returns>
            /// <summary>
            /// Resolves a service from the DI container using the nearest LifetimeScope.
            /// </summary>
            /// <typeparam name="T">The type of service to resolve.</typeparam>
            /// <param name="gameObject">The GameObject to find the scope from.</param>
            /// <returns>The resolved service instance.</returns>
            public static T Resolve<T>(this GameObject gameObject)
            {
                if (gameObject == null)
                    throw new ArgumentNullException(nameof(gameObject));
                
                // Try to find a LifetimeScope on this GameObject or its parents
                var scope = gameObject.GetComponentInParent<LifetimeScope>();
                if (scope != null)
                {
                    return scope.Container.Resolve<T>();
                }
                
                // Fallback to global CoreSystems accessor
                return CoreSystems.Resolve<T>();
            }
            
            /// <summary>
            /// Gets a logger for this GameObject using its name as the tag.
            /// </summary>
            /// <param name="gameObject">The GameObject to create a logger for.</param>
            /// <returns>A logger instance configured for this GameObject.</returns>
            public static IBurstLogger GetLogger(this GameObject gameObject)
            {
                if (gameObject == null)
                    throw new ArgumentNullException(nameof(gameObject));
                
                return CoreSystems.CreateLogger(gameObject.name);
            }
        }
    }
    
    /// <summary>
    /// Component that automatically disposes subscriptions when the GameObject is destroyed.
    /// Prevents memory leaks from message bus subscriptions.
    /// </summary>
    internal sealed class SubscriptionDisposer : MonoBehaviour
    {
        private readonly System.Collections.Generic.List<IDisposable> subscriptions = new System.Collections.Generic.List<IDisposable>();
        
        /// <summary>
        /// Adds a subscription to be disposed when this GameObject is destroyed.
        /// </summary>
        /// <param name="subscription">The subscription to dispose.</param>
        public void AddSubscription(IDisposable subscription)
        {
            if (subscription != null)
            {
                subscriptions.Add(subscription);
            }
        }
        
        /// <summary>
        /// Removes a subscription from automatic disposal.
        /// </summary>
        /// <param name="subscription">The subscription to remove.</param>
        public void RemoveSubscription(IDisposable subscription)
        {
            if (subscription != null)
            {
                subscriptions.Remove(subscription);
            }
        }
        
        private void OnDestroy()
        {
            // Dispose all tracked subscriptions
            foreach (var subscription in subscriptions)
            {
                try
                {
                    subscription?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SubscriptionDisposer] Failed to dispose subscription: {ex.Message}");
                }
            }
            
            subscriptions.Clear();
        }
        
        /// <summary>
        /// Gets the number of active subscriptions being tracked.
        /// </summary>
        public int ActiveSubscriptionCount => subscriptions.Count;
    }
}

namespace AhBearStudios.Core.Extensions.Pooling
{
    /// <summary>
    /// Extension methods specifically for pooling operations.
    /// </summary>
    public static class PoolingExtensions
    {
        /// <summary>
        /// Acquires an object from a pool and automatically returns it when the returned disposable is disposed.
        /// </summary>
        /// <typeparam name="T">The type of object to acquire.</typeparam>
        /// <param name="behaviour">The MonoBehaviour requesting the object.</param>
        /// <returns>A disposable wrapper that contains the acquired object.</returns>
        public static PooledObject<T> AcquirePooled<T>(this MonoBehaviour behaviour) where T : class
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            var pool = CoreSystems.GetPool<T>();
            if (pool == null)
                throw new InvalidOperationException($"No pool found for type {typeof(T).Name}");
            
            var obj = pool.Acquire();
            return new PooledObject<T>(obj, pool);
        }
        
        /// <summary>
        /// Acquires an object from a named pool and automatically returns it when the returned disposable is disposed.
        /// </summary>
        /// <typeparam name="T">The type of object to acquire.</typeparam>
        /// <param name="behaviour">The MonoBehaviour requesting the object.</param>
        /// <param name="poolName">The name of the pool to acquire from.</param>
        /// <returns>A disposable wrapper that contains the acquired object.</returns>
        public static PooledObject<T> AcquirePooled<T>(this MonoBehaviour behaviour, string poolName) where T : class
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            if (string.IsNullOrEmpty(poolName))
                throw new ArgumentException("Pool name cannot be null or empty", nameof(poolName));
            
            var pool = CoreSystems.PoolRegistry.GetPool<T>(poolName);
            if (pool == null)
                throw new InvalidOperationException($"No pool found with name '{poolName}' for type {typeof(T).Name}");
            
            var obj = pool.Acquire();
            return new PooledObject<T>(obj, pool);
        }
        
        /// <summary>
        /// Manually releases an object back to its appropriate pool.
        /// </summary>
        /// <typeparam name="T">The type of object to release.</typeparam>
        /// <param name="behaviour">The MonoBehaviour releasing the object.</param>
        /// <param name="obj">The object to release.</param>
        public static void ReleaseToPool<T>(this MonoBehaviour behaviour, T obj) where T : class
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            if (obj == null)
                return;
            
            var pool = CoreSystems.GetPool<T>();
            if (pool != null)
            {
                pool.Release(obj);
            }
            else
            {
                behaviour.LogWarning($"No pool found for type {typeof(T).Name}, object cannot be released");
            }
        }
    }
    
    /// <summary>
    /// A wrapper that automatically returns a pooled object when disposed.
    /// </summary>
    /// <typeparam name="T">The type of the pooled object.</typeparam>
    public sealed class PooledObject<T> : IDisposable where T : class
    {
        private readonly IPool<T> pool;
        private T obj;
        private bool isDisposed = false;
        
        internal PooledObject(T obj, IPool<T> pool)
        {
            this.obj = obj ?? throw new ArgumentNullException(nameof(obj));
            this.pool = pool ?? throw new ArgumentNullException(nameof(pool));
        }
        
        /// <summary>
        /// Gets the pooled object. Returns null if already disposed.
        /// </summary>
        public T Object => isDisposed ? null : obj;
        
        /// <summary>
        /// Gets whether this pooled object has been disposed.
        /// </summary>
        public bool IsDisposed => isDisposed;
        
        /// <summary>
        /// Implicitly converts to the underlying object type.
        /// </summary>
        /// <param name="pooledObject">The pooled object wrapper.</param>
        public static implicit operator T(PooledObject<T> pooledObject)
        {
            return pooledObject?.Object;
        }
        
        /// <summary>
        /// Returns the object to the pool.
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed && obj != null)
            {
                pool.Release(obj);
                obj = null;
                isDisposed = true;
            }
        }
    }
}

namespace AhBearStudios.Core.Extensions.Profiling
{
    /// <summary>
    /// Extension methods specifically for profiling operations.
    /// </summary>
    public static class ProfilingExtensions
    {
        /// <summary>
        /// Profiles the execution of an action.
        /// </summary>
        /// <param name="behaviour">The MonoBehaviour performing the action.</param>
        /// <param name="action">The action to profile.</param>
        /// <param name="sampleName">The name of the profiler sample.</param>
        public static void ProfileAction(this MonoBehaviour behaviour, Action action, string sampleName = null)
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            
            if (string.IsNullOrEmpty(sampleName))
                sampleName = $"{behaviour.GetType().Name}.Action";
            
            using (CoreSystems.BeginProfilingScope(sampleName))
            {
                action();
            }
        }
        
        /// <summary>
        /// Profiles the execution of a function and returns its result.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="behaviour">The MonoBehaviour performing the function.</param>
        /// <param name="function">The function to profile.</param>
        /// <param name="sampleName">The name of the profiler sample.</param>
        /// <returns>The result of the function execution.</returns>
        public static T ProfileFunction<T>(this MonoBehaviour behaviour, Func<T> function, string sampleName = null)
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            if (function == null)
                throw new ArgumentNullException(nameof(function));
            
            if (string.IsNullOrEmpty(sampleName))
                sampleName = $"{behaviour.GetType().Name}.Function";
            
            using (CoreSystems.BeginProfilingScope(sampleName))
            {
                return function();
            }
        }
        
        /// <summary>
        /// Creates a scoped profiler that automatically begins and ends profiling for a MonoBehaviour method.
        /// </summary>
        /// <param name="behaviour">The MonoBehaviour to profile.</param>
        /// <param name="methodName">The name of the method being profiled. If null, uses the calling method name.</param>
        /// <returns>A disposable profiler scope.</returns>
        public static IDisposable ProfileMethod(this MonoBehaviour behaviour, [System.Runtime.CompilerServices.CallerMemberName] string methodName = null)
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            var sampleName = $"{behaviour.GetType().Name}.{methodName ?? "UnknownMethod"}";
            return CoreSystems.BeginProfilingScope(sampleName);
        }
    }
}

namespace AhBearStudios.Core.Extensions.Messaging
{
    /// <summary>
    /// Extension methods specifically for message bus operations.
    /// </summary>
    public static class MessagingExtensions
    {
        /// <summary>
        /// Subscribes to a specific message type with automatic cleanup when the MonoBehaviour is destroyed.
        /// Includes filtering capabilities.
        /// </summary>
        /// <typeparam name="T">The type of message to subscribe to.</typeparam>
        /// <param name="behaviour">The MonoBehaviour subscribing to messages.</param>
        /// <param name="handler">The handler to invoke when a message is received.</param>
        /// <param name="filter">Optional filter function to determine which messages to handle.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        public static IDisposable SubscribeToMessageWithFilter<T>(this MonoBehaviour behaviour, Action<T> handler, Func<T, bool> filter = null) where T : IMessage
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            Action<T> filteredHandler = handler;
            
            if (filter != null)
            {
                filteredHandler = message =>
                {
                    try
                    {
                        if (filter(message))
                        {
                            handler(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        behaviour.LogError($"Error in message filter or handler: {ex.Message}");
                    }
                };
            }
            else
            {
                filteredHandler = message =>
                {
                    try
                    {
                        handler(message);
                    }
                    catch (Exception ex)
                    {
                        behaviour.LogError($"Error in message handler: {ex.Message}");
                    }
                };
            }
            
            return behaviour.SubscribeToMessage(filteredHandler);
        }
        
        /// <summary>
        /// Publishes a message and logs the operation for debugging.
        /// </summary>
        /// <typeparam name="T">The type of message to publish.</typeparam>
        /// <param name="behaviour">The MonoBehaviour publishing the message.</param>
        /// <param name="message">The message to publish.</param>
        /// <param name="logLevel">The log level to use for logging the operation.</param>
        public static void PublishMessageWithLogging<T>(this MonoBehaviour behaviour, T message, byte logLevel = 0) where T : IMessage
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));
            
            try
            {
                CoreSystems.Log(logLevel, $"Publishing message: {typeof(T).Name} (ID: {message.Id})", behaviour.GetType().Name);
                CoreSystems.PublishMessage(message);
            }
            catch (Exception ex)
            {
                behaviour.LogError($"Failed to publish message {typeof(T).Name}: {ex.Message}");
                throw;
            }
        }
    }
}