using System;
using System.Collections.Generic;
using UnityEngine;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Messaging.Unity
{
    /// <summary>
    /// MonoBehaviour component that integrates the message bus system with Unity's lifecycle.
    /// Provides automatic initialization and cleanup of message buses within Unity context.
    /// </summary>
    public class MessageBusComponent : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Enable debug logging for the message bus system")]
        private bool _enableDebugLogging = false;

        [SerializeField]
        [Tooltip("Enable profiling for the message bus system")]
        private bool _enableProfiling = false;

        [SerializeField]
        [Tooltip("Don't destroy this GameObject when loading a new scene")]
        private bool _dontDestroyOnLoad = true;

        private IBurstLogger _logger;
        private IProfiler _profiler;
        private MessageBusProvider _provider;
        private readonly List<IDisposable> _trackedResources = new List<IDisposable>();
        private static MessageBusComponent _instance;
        private bool _isInitialized;

        /// <summary>
        /// Gets the singleton instance of the MessageBusComponent.
        /// </summary>
        public static MessageBusComponent Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find an existing instance in the scene
                    _instance = FindObjectOfType<MessageBusComponent>();
                    
                    // If no instance exists, create one
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("MessageBusComponent");
                        _instance = go.AddComponent<MessageBusComponent>();
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Gets the MessageBusProvider used by this component.
        /// </summary>
        public MessageBusProvider Provider => _provider;

        /// <summary>
        /// Gets a message bus for the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The type of messages the bus will handle.</typeparam>
        /// <returns>A message bus for the specified message type.</returns>
        public IMessageBus<TMessage> GetMessageBus<TMessage>() where TMessage : IMessage
        {
            EnsureInitialized();
            return _provider.GetMessageBus<TMessage>();
        }

        /// <summary>
        /// Gets a keyed message bus for the specified key and message types.
        /// </summary>
        /// <typeparam name="TKey">The type of keys used for message filtering.</typeparam>
        /// <typeparam name="TMessage">The type of messages the bus will handle.</typeparam>
        /// <returns>A keyed message bus for the specified key and message types.</returns>
        public IKeyedMessageBus<TKey, TMessage> GetKeyedMessageBus<TKey, TMessage>() 
            where TMessage : IMessage 
            where TKey : IEquatable<TKey>
        {
            EnsureInitialized();
            return _provider.GetKeyedMessageBus<TKey, TMessage>();
        }

        /// <summary>
        /// Creates and tracks a new subscription manager.
        /// The manager will be automatically disposed when this component is destroyed.
        /// </summary>
        /// <returns>A new subscription manager.</returns>
        public SubscriptionManager CreateSubscriptionManager()
        {
            EnsureInitialized();
            
            var manager = new SubscriptionManager(_logger, _profiler);
            _trackedResources.Add(manager);
            
            if (_logger != null && _enableDebugLogging)
            {
                _logger.Debug("Created and tracking new SubscriptionManager");
            }
            
            return manager;
        }

        /// <summary>
        /// Registers a disposable resource to be automatically disposed when this component is destroyed.
        /// </summary>
        /// <param name="resource">The resource to track.</param>
        /// <typeparam name="T">Type of the resource, must implement IDisposable.</typeparam>
        /// <returns>The same resource for chaining.</returns>
        public T TrackResource<T>(T resource) where T : IDisposable
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }
            
            _trackedResources.Add(resource);
            
            if (_logger != null && _enableDebugLogging)
            {
                _logger.Debug($"Tracking new resource of type {typeof(T).Name}");
            }
            
            return resource;
        }

        /// <summary>
        /// Stops tracking a disposable resource.
        /// </summary>
        /// <param name="resource">The resource to stop tracking.</param>
        /// <returns>True if the resource was found and removed, false otherwise.</returns>
        public bool UntrackResource(IDisposable resource)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }
            
            bool removed = _trackedResources.Remove(resource);
            
            if (removed && _logger != null && _enableDebugLogging)
            {
                _logger.Debug($"Stopped tracking resource of type {resource.GetType().Name}");
            }
            
            return removed;
        }

        /// <summary>
        /// Resets the message bus system, clearing all cached buses.
        /// </summary>
        public void ResetMessageBusSystem()
        {
            if (_provider != null)
            {
                _provider.Reset();
                
                if (_logger != null)
                {
                    _logger.Info("Message bus system reset");
                }
            }
        }

        private void Awake()
        {
            // Enforce singleton pattern
            if (_instance != null && _instance != this)
            {
                if (_logger != null)
                {
                    _logger.Warning("Duplicate MessageBusComponent detected, destroying duplicate");
                }
                
                Destroy(gameObject);
                return;
            }

            _instance = this;
            
            if (_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
            
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            // Set up logger and profiler based on settings
            if (_enableDebugLogging)
            {
                // Use the logger implementation from your project
                _logger = BurstLoggerFactory.GetLoggerForContext("MessageBusSystem");
            }
            
            if (_enableProfiling)
            {
                // Use the profiler implementation from your project
                _profiler = ProfilerFactory.GetProfilerForContext("MessageBusSystem");
            }
            
            // Create the provider
            _provider = new MessageBusProvider(null, _logger, _profiler);
            
            _isInitialized = true;
            
            if (_logger != null)
            {
                _logger.Info("MessageBusComponent initialized");
            }
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        private void OnDestroy()
        {
            if (_logger != null)
            {
                _logger.Info("MessageBusComponent being destroyed, cleaning up resources");
            }
            
            // Dispose all tracked resources
            foreach (var resource in _trackedResources)
            {
                try
                {
                    resource.Dispose();
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error disposing resource: {ex.Message}");
                    }
                }
            }
            
            _trackedResources.Clear();
            
            // Dispose the provider
            if (_provider != null)
            {
                _provider.Dispose();
                _provider = null;
            }
            
            _isInitialized = false;
            
            // Clear the instance reference if this is the singleton
            if (_instance == this)
            {
                _instance = null;
            }
            
            if (_logger != null)
            {
                _logger.Info("MessageBusComponent cleanup complete");
                _logger = null;
            }
            
            _profiler = null;
        }
    }
}