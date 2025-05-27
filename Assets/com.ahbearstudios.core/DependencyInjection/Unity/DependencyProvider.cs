using System;
using UnityEngine;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Unity
{
    /// <summary>
    /// Unity component that provides dependency injection services.
    /// Acts as a bridge between Unity's component system and the DI framework.
    /// </summary>
    public class DependencyProvider : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool _persistBetweenScenes = true;
        [SerializeField] private bool _autoInitialize = true;
        
        private IDependencyProvider _dependencyProvider;
        private bool _isInitialized;
        
        /// <summary>
        /// Gets the dependency provider instance
        /// </summary>
        public IDependencyProvider DependencyProvider => _dependencyProvider;
        
        /// <summary>
        /// Gets whether the provider is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Event fired when the provider is initialized
        /// </summary>
        public event Action<DependencyProvider> Initialized;
        
        private void Awake()
        {
            if (_persistBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
            
            if (_autoInitialize)
            {
                Initialize();
            }
        }
        
        /// <summary>
        /// Initializes the dependency provider
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;
                
            try
            {
                // Create a basic dependency provider implementation
                // In a real implementation, this would be more sophisticated
                _dependencyProvider = new SimpleDependencyProvider();
                RegisterDefaultDependencies();
                
                _isInitialized = true;
                Initialized?.Invoke(this);
                
                Debug.Log("[DependencyProvider] Initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DependencyProvider] Failed to initialize: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Registers default dependencies
        /// </summary>
        private void RegisterDefaultDependencies()
        {
            // Register common dependencies here
            // This would typically include services like IMessageBus, ILogger, etc.
        }
        
        /// <summary>
        /// Resolves a dependency of the specified type
        /// </summary>
        /// <typeparam name="T">Type to resolve</typeparam>
        /// <returns>Instance of the requested type</returns>
        public T Resolve<T>()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[DependencyProvider] Attempting to resolve before initialization");
                return default;
            }
            
            return _dependencyProvider.Resolve<T>();
        }
        
        /// <summary>
        /// Registers a dependency instance
        /// </summary>
        /// <typeparam name="T">Type to register</typeparam>
        /// <param name="instance">Instance to register</param>
        public void Register<T>(T instance)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[DependencyProvider] Attempting to register before initialization");
                return;
            }
            
            if (_dependencyProvider is SimpleDependencyProvider simple)
            {
                simple.Register(instance);
            }
        }
        
        /// <summary>
        /// Gets the singleton instance (creates one if none exists)
        /// </summary>
        public static DependencyProvider Instance
        {
            get
            {
                var instance = FindObjectOfType<DependencyProvider>();
                if (instance == null)
                {
                    var go = new GameObject("[DependencyProvider]");
                    instance = go.AddComponent<DependencyProvider>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
    }
    
    
    
    /// <summary>
    /// Simple implementation of IDependencyProvider for basic dependency injection.
    /// This is a minimal implementation for demonstration purposes.
    /// </summary>
    internal class SimpleDependencyProvider : IDependencyProvider
    {
        private readonly System.Collections.Generic.Dictionary<Type, object> _dependencies = 
            new System.Collections.Generic.Dictionary<Type, object>();
        
        /// <summary>
        /// Resolves a dependency of the specified type
        /// </summary>
        /// <typeparam name="T">Type to resolve</typeparam>
        /// <returns>Instance of the requested type</returns>
        public T Resolve<T>()
        {
            var type = typeof(T);
            if (_dependencies.TryGetValue(type, out var instance))
            {
                return (T)instance;
            }
            
            // Try to create a default instance
            if (type.IsInterface)
            {
                Debug.LogWarning($"[SimpleDependencyProvider] No registration found for interface {type.Name}");
                return default;
            }
            
            try
            {
                var newInstance = Activator.CreateInstance<T>();
                _dependencies[type] = newInstance;
                return newInstance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SimpleDependencyProvider] Failed to create instance of {type.Name}: {ex.Message}");
                return default;
            }
        }
        
        /// <summary>
        /// Registers a dependency instance
        /// </summary>
        /// <typeparam name="T">Type to register</typeparam>
        /// <param name="instance">Instance to register</param>
        public void Register<T>(T instance)
        {
            var type = typeof(T);
            _dependencies[type] = instance;
        }
    }
    
    /// <summary>
    /// Simple implementation of IMessageBus for basic message passing.
    /// This is a minimal implementation for demonstration purposes.
    /// </summary>
    internal class SimpleMessageBus : IMessageBus, IDisposable
    {
        private readonly System.Collections.Generic.Dictionary<Type, System.Collections.Generic.List<Delegate>> _subscribers = 
            new System.Collections.Generic.Dictionary<Type, System.Collections.Generic.List<Delegate>>();
        private readonly object _lock = new object();
        private bool _disposed;
        
        /// <summary>
        /// Creates a new SimpleMessageBus
        /// </summary>
        /// <param name="initialCapacity">Initial capacity for subscribers</param>
        public SimpleMessageBus(int initialCapacity)
        {
            // Initialize with capacity hint
        }
        
        /// <summary>
        /// Gets a publisher for the specified message type
        /// </summary>
        /// <typeparam name="TMessage">The type of message to publish</typeparam>
        /// <returns>A publisher for the specified message type</returns>
        public IMessagePublisher<TMessage> GetPublisher<TMessage>()
        {
            return new SimpleMessagePublisher<TMessage>(this);
        }
        
        /// <summary>
        /// Gets a subscriber for the specified message type
        /// </summary>
        /// <typeparam name="TMessage">The type of message to subscribe to</typeparam>
        /// <returns>A subscriber for the specified message type</returns>
        public IMessageSubscriber<TMessage> GetSubscriber<TMessage>()
        {
            return new SimpleMessageSubscriber<TMessage>(this);
        }
        
        /// <summary>
        /// Gets a keyed publisher for the specified message type
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TMessage">The type of message to publish</typeparam>
        /// <returns>A keyed publisher for the specified message type</returns>
        public IKeyedMessagePublisher<TKey, TMessage> GetPublisher<TKey, TMessage>()
        {
            throw new NotImplementedException("Keyed publishers not implemented in SimpleMessageBus");
        }
        
        /// <summary>
        /// Gets a keyed subscriber for the specified message type
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TMessage">The type of message to subscribe to</typeparam>
        /// <returns>A keyed subscriber for the specified message type</returns>
        public IKeyedMessageSubscriber<TKey, TMessage> GetSubscriber<TKey, TMessage>()
        {
            throw new NotImplementedException("Keyed subscribers not implemented in SimpleMessageBus");
        }
        
        /// <summary>
        /// Clears all cached publishers and subscribers
        /// </summary>
        public void ClearCaches()
        {
            lock (_lock)
            {
                _subscribers.Clear();
            }
        }
        
        /// <summary>
        /// Publishes a message
        /// </summary>
        /// <typeparam name="TMessage">The type of message to publish</typeparam>
        /// <param name="message">The message to publish</param>
        public void PublishMessage<TMessage>(TMessage message) where TMessage : IMessage
        {
            if (_disposed)
                return;
                
            lock (_lock)
            {
                var messageType = typeof(TMessage);
                if (_subscribers.TryGetValue(messageType, out var subscribers))
                {
                    foreach (var subscriber in subscribers)
                    {
                        try
                        {
                            if (subscriber is Action<TMessage> action)
                            {
                                action.Invoke(message);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[SimpleMessageBus] Error invoking subscriber: {ex.Message}");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Subscribes to messages of the specified type
        /// </summary>
        /// <typeparam name="TMessage">The type of message to subscribe to</typeparam>
        /// <param name="handler">The handler to invoke when a message is received</param>
        /// <returns>A subscription token that can be disposed to unsubscribe</returns>
        public IDisposable SubscribeToMessage<TMessage>(Action<TMessage> handler) where TMessage : IMessage
        {
            if (_disposed || handler == null)
                return new EmptyDisposable();
                
            lock (_lock)
            {
                var messageType = typeof(TMessage);
                if (!_subscribers.TryGetValue(messageType, out var subscribers))
                {
                    subscribers = new System.Collections.Generic.List<Delegate>();
                    _subscribers[messageType] = subscribers;
                }
                
                subscribers.Add(handler);
                return new UnsubscribeDisposable(() => RemoveSubscriber(messageType, handler));
            }
        }
        
        /// <summary>
        /// Subscribes to all messages
        /// </summary>
        /// <param name="handler">The handler to invoke when any message is received</param>
        /// <returns>A subscription token that can be disposed to unsubscribe</returns>
        public IDisposable SubscribeToAllMessages(Action<IMessage> handler)
        {
            // Simple implementation - not fully implemented
            return new EmptyDisposable();
        }
        
        /// <summary>
        /// Gets the message registry
        /// </summary>
        /// <returns>The message registry</returns>
        public IMessageRegistry GetMessageRegistry()
        {
            throw new NotImplementedException("Message registry not implemented in SimpleMessageBus");
        }
        
        /// <summary>
        /// Removes a subscriber
        /// </summary>
        /// <param name="messageType">Type of message</param>
        /// <param name="handler">Handler to remove</param>
        private void RemoveSubscriber(Type messageType, Delegate handler)
        {
            if (_disposed)
                return;
                
            lock (_lock)
            {
                if (_subscribers.TryGetValue(messageType, out var subscribers))
                {
                    subscribers.Remove(handler);
                    if (subscribers.Count == 0)
                    {
                        _subscribers.Remove(messageType);
                    }
                }
            }
        }
        
        /// <summary>
        /// Disposes the message bus
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
                
            lock (_lock)
            {
                _subscribers.Clear();
                _disposed = true;
            }
        }
        
        /// <summary>
        /// Simple message publisher implementation
        /// </summary>
        private class SimpleMessagePublisher<TMessage> : IMessagePublisher<TMessage>
        {
            private readonly SimpleMessageBus _messageBus;
            
            public SimpleMessagePublisher(SimpleMessageBus messageBus)
            {
                _messageBus = messageBus;
            }
            
            public void Publish(TMessage message) where TMessage : IMessage
            {
                _messageBus.PublishMessage(message);
            }
        }
        
        /// <summary>
        /// Simple message subscriber implementation
        /// </summary>
        private class SimpleMessageSubscriber<TMessage> : IMessageSubscriber<TMessage>
        {
            private readonly SimpleMessageBus _messageBus;
            
            public SimpleMessageSubscriber(SimpleMessageBus messageBus)
            {
                _messageBus = messageBus;
            }
            
            public IDisposable Subscribe(Action<TMessage> handler) where TMessage : IMessage
            {
                return _messageBus.SubscribeToMessage(handler);
            }
        }
        
        /// <summary>
        /// Empty disposable for failed subscriptions
        /// </summary>
        private class EmptyDisposable : IDisposable
        {
            public void Dispose() { }
        }
        
        /// <summary>
        /// Disposable that calls an unsubscribe action
        /// </summary>
        private class UnsubscribeDisposable : IDisposable
        {
            private readonly Action _unsubscribe;
            private bool _disposed;
            
            public UnsubscribeDisposable(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }
            
            public void Dispose()
            {
                if (!_disposed)
                {
                    _unsubscribe?.Invoke();
                    _disposed = true;
                }
            }
        }
    }
}