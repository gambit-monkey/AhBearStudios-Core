using System;
using AhBearStudios.Core.DependencyInjection.Unity;
using AhBearStudios.Core.MessageBus.Interfaces;
using UnityEngine;

namespace AhBearStudios.Core.MessageBus.Unity
{
    /// <summary>
    /// Unity component that provides message bus services.
    /// Manages the message bus lifecycle and provides access to it.
    /// </summary>
    public class MessageBusProvider : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool _persistBetweenScenes = true;
        [SerializeField] private bool _autoInitialize = true;
        [SerializeField] private int _initialCapacity = 100;
        
        private IMessageBus _messageBus;
        private bool _isInitialized;
        
        /// <summary>
        /// Gets the message bus instance
        /// </summary>
        public IMessageBus MessageBus => _messageBus;
        
        /// <summary>
        /// Gets whether the message bus is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Event fired when the message bus is initialized
        /// </summary>
        public event Action<MessageBusProvider> Initialized;
        
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
        /// Initializes the message bus
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;
                
            try
            {
                // Create a basic message bus implementation
                // In a real implementation, this would create the actual message bus
                _messageBus = new SimpleMessageBus(_initialCapacity);
                
                _isInitialized = true;
                Initialized?.Invoke(this);
                
                Debug.Log("[MessageBusProvider] Initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MessageBusProvider] Failed to initialize: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets the singleton instance (creates one if none exists)
        /// </summary>
        public static MessageBusProvider Instance
        {
            get
            {
                var instance = FindObjectOfType<MessageBusProvider>();
                if (instance == null)
                {
                    var go = new GameObject("[MessageBusProvider]");
                    instance = go.AddComponent<MessageBusProvider>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
    }
}