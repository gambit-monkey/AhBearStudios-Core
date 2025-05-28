using UnityEngine;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Configuration
{
    /// <summary>
    /// Configuration for the message bus system that implements IMessageBusConfig.
    /// Provides ScriptableObject-based configuration with validation and platform optimization.
    /// </summary>
    [CreateAssetMenu(menuName = "AhBear/Core/MessageBus Config", fileName = "MessageBusConfig", order = 3)]
    public sealed class MessageBusConfig : ScriptableObject, IMessageBusConfig
    {
        [Header("Configuration")]
        [SerializeField]
        private string configId = "DefaultMessageBusConfig";
        
        [Header("Performance")]
        [SerializeField, Range(10, 10000)]
        private int maxMessagesPerFrame = 100;
        
        [SerializeField, Range(1, 1000)]
        private int initialMessageQueueCapacity = 50;
        
        [SerializeField, Range(0.001f, 0.1f)]
        private float messageProcessingTimeSliceMs = 0.016f; // 16ms = 60fps
        
        [Header("Memory Management")]
        [SerializeField]
        private bool enableMessagePooling = true;
        
        [SerializeField, Range(10, 1000)]
        private int messagePoolInitialSize = 100;
        
        [SerializeField, Range(100, 10000)]
        private int messagePoolMaxSize = 1000;
        
        [Header("Serialization")]
        [SerializeField]
        private bool enableBurstSerialization = true;
        
        [SerializeField]
        private bool enableNetworkSerialization = false;
        
        [SerializeField]
        private bool enableCompressionForNetwork = true;
        
        [Header("Delivery Options")]
        [SerializeField]
        private bool enableReliableDelivery = true;
        
        [SerializeField, Range(1, 10)]
        private int maxDeliveryRetries = 3;
        
        [SerializeField, Range(0.1f, 10f)]
        private float deliveryTimeoutSeconds = 5f;
        
        [SerializeField, Range(0.01f, 1f)]
        private float retryBackoffMultiplier = 2f;
        
        [Header("Statistics")]
        [SerializeField]
        private bool enableStatisticsCollection = true;
        
        [SerializeField]
        private bool enableDeliveryTracking = true;
        
        [SerializeField]
        private bool enablePerformanceMetrics = true;
        
        [Header("Debugging")]
        [SerializeField]
        private bool enableMessageLogging = false;
        
        [SerializeField]
        private bool enableVerboseLogging = false;
        
        [SerializeField]
        private bool logFailedDeliveries = true;
        
        [Header("Threading")]
        [SerializeField]
        private bool enableMultithreading = true;
        
        [SerializeField, Range(1, 8)]
        private int workerThreadCount = 2;
        
        [SerializeField]
        private bool useJobSystemForProcessing = true;
        
        [Header("Platform Optimizations")]
        [SerializeField]
        private bool enableMobileOptimizations = true;
        
        [SerializeField]
        private bool enableConsoleOptimizations = true;
        
        [SerializeField]
        private bool enableEditorDebugging = true;
        
        // IMessageBusConfig implementation
        public string ConfigId 
        { 
            get => configId; 
            set => configId = value; 
        }
        
        public int MaxMessagesPerFrame 
        { 
            get => maxMessagesPerFrame; 
            set => maxMessagesPerFrame = Mathf.Max(1, value); 
        }
        
        public int InitialMessageQueueCapacity 
        { 
            get => initialMessageQueueCapacity; 
            set => initialMessageQueueCapacity = Mathf.Max(1, value); 
        }
        
        public float MessageProcessingTimeSliceMs 
        { 
            get => messageProcessingTimeSliceMs; 
            set => messageProcessingTimeSliceMs = Mathf.Max(0.001f, value); 
        }
        
        public bool EnableMessagePooling 
        { 
            get => enableMessagePooling; 
            set => enableMessagePooling = value; 
        }
        
        public int MessagePoolInitialSize 
        { 
            get => messagePoolInitialSize; 
            set => messagePoolInitialSize = Mathf.Max(1, value); 
        }
        
        public int MessagePoolMaxSize 
        { 
            get => messagePoolMaxSize; 
            set => messagePoolMaxSize = Mathf.Max(1, value); 
        }
        
        public bool EnableBurstSerialization 
        { 
            get => enableBurstSerialization; 
            set => enableBurstSerialization = value; 
        }
        
        public bool EnableNetworkSerialization 
        { 
            get => enableNetworkSerialization; 
            set => enableNetworkSerialization = value; 
        }
        
        public bool EnableCompressionForNetwork 
        { 
            get => enableCompressionForNetwork; 
            set => enableCompressionForNetwork = value; 
        }
        
        public bool EnableReliableDelivery 
        { 
            get => enableReliableDelivery; 
            set => enableReliableDelivery = value; 
        }
        
        public int MaxDeliveryRetries 
        { 
            get => maxDeliveryRetries; 
            set => maxDeliveryRetries = Mathf.Max(1, value); 
        }
        
        public float DeliveryTimeoutSeconds 
        { 
            get => deliveryTimeoutSeconds; 
            set => deliveryTimeoutSeconds = Mathf.Max(0.1f, value); 
        }
        
        public float RetryBackoffMultiplier 
        { 
            get => retryBackoffMultiplier; 
            set => retryBackoffMultiplier = Mathf.Max(0.1f, value); 
        }
        
        public bool EnableStatisticsCollection 
        { 
            get => enableStatisticsCollection; 
            set => enableStatisticsCollection = value; 
        }
        
        public bool EnableDeliveryTracking 
        { 
            get => enableDeliveryTracking; 
            set => enableDeliveryTracking = value; 
        }
        
        public bool EnablePerformanceMetrics 
        { 
            get => enablePerformanceMetrics; 
            set => enablePerformanceMetrics = value; 
        }
        
        public bool EnableMessageLogging 
        { 
            get => enableMessageLogging; 
            set => enableMessageLogging = value; 
        }
        
        public bool EnableVerboseLogging 
        { 
            get => enableVerboseLogging; 
            set => enableVerboseLogging = value; 
        }
        
        public bool LogFailedDeliveries 
        { 
            get => logFailedDeliveries; 
            set => logFailedDeliveries = value; 
        }
        
        public bool EnableMultithreading 
        { 
            get => enableMultithreading; 
            set => enableMultithreading = value; 
        }
        
        public int WorkerThreadCount 
        { 
            get => workerThreadCount; 
            set => workerThreadCount = Mathf.Max(1, value); 
        }
        
        public bool UseJobSystemForProcessing 
        { 
            get => useJobSystemForProcessing; 
            set => useJobSystemForProcessing = value; 
        }
        
        // Additional properties for bootstrapping
        public bool EnableMobileOptimizations => enableMobileOptimizations;
        public bool EnableConsoleOptimizations => enableConsoleOptimizations;
        public bool EnableEditorDebugging => enableEditorDebugging;
        
        public IMessageBusConfig Clone()
        {
            var clone = CreateInstance<MessageBusConfig>();
            
            clone.configId = configId;
            clone.maxMessagesPerFrame = maxMessagesPerFrame;
            clone.initialMessageQueueCapacity = initialMessageQueueCapacity;
            clone.messageProcessingTimeSliceMs = messageProcessingTimeSliceMs;
            clone.enableMessagePooling = enableMessagePooling;
            clone.messagePoolInitialSize = messagePoolInitialSize;
            clone.messagePoolMaxSize = messagePoolMaxSize;
            clone.enableBurstSerialization = enableBurstSerialization;
            clone.enableNetworkSerialization = enableNetworkSerialization;
            clone.enableCompressionForNetwork = enableCompressionForNetwork;
            clone.enableReliableDelivery = enableReliableDelivery;
            clone.maxDeliveryRetries = maxDeliveryRetries;
            clone.deliveryTimeoutSeconds = deliveryTimeoutSeconds;
            clone.retryBackoffMultiplier = retryBackoffMultiplier;
            clone.enableStatisticsCollection = enableStatisticsCollection;
            clone.enableDeliveryTracking = enableDeliveryTracking;
            clone.enablePerformanceMetrics = enablePerformanceMetrics;
            clone.enableMessageLogging = enableMessageLogging;
            clone.enableVerboseLogging = enableVerboseLogging;
            clone.logFailedDeliveries = logFailedDeliveries;
            clone.enableMultithreading = enableMultithreading;
            clone.workerThreadCount = workerThreadCount;
            clone.useJobSystemForProcessing = useJobSystemForProcessing;
            clone.enableMobileOptimizations = enableMobileOptimizations;
            clone.enableConsoleOptimizations = enableConsoleOptimizations;
            clone.enableEditorDebugging = enableEditorDebugging;
            
            return clone;
        }
        
        private void OnValidate()
        {
            ValidateConfiguration();
        }
        
        private void ValidateConfiguration()
        {
            if (maxMessagesPerFrame < 1)
            {
                maxMessagesPerFrame = 1;
                Debug.LogWarning("MaxMessagesPerFrame cannot be less than 1. Reset to 1.");
            }
            
            if (initialMessageQueueCapacity < 1)
            {
                initialMessageQueueCapacity = 1;
                Debug.LogWarning("InitialMessageQueueCapacity cannot be less than 1. Reset to 1.");
            }
            
            if (messagePoolMaxSize < messagePoolInitialSize)
            {
                messagePoolMaxSize = messagePoolInitialSize;
                Debug.LogWarning("MessagePoolMaxSize cannot be less than MessagePoolInitialSize. Adjusted automatically.");
            }
            
            if (maxDeliveryRetries < 1)
            {
                maxDeliveryRetries = 1;
                Debug.LogWarning("MaxDeliveryRetries cannot be less than 1. Reset to 1.");
            }
            
            if (deliveryTimeoutSeconds < 0.1f)
            {
                deliveryTimeoutSeconds = 0.1f;
                Debug.LogWarning("DeliveryTimeoutSeconds cannot be less than 0.1. Reset to 0.1.");
            }
            
            if (workerThreadCount < 1)
            {
                workerThreadCount = 1;
                Debug.LogWarning("WorkerThreadCount cannot be less than 1. Reset to 1.");
            }
            
            if (string.IsNullOrEmpty(configId))
            {
                configId = "DefaultMessageBusConfig";
                Debug.LogWarning("ConfigId cannot be empty. Reset to 'DefaultMessageBusConfig'.");
            }
            
            // Auto-adjust thread count based on platform
#if UNITY_ANDROID || UNITY_IOS
            if (workerThreadCount > 2)
            {
                workerThreadCount = 2;
                Debug.LogWarning("WorkerThreadCount limited to 2 on mobile platforms.");
            }
#endif
        }
        
        /// <summary>
        /// Creates a platform-optimized version of this configuration.
        /// </summary>
        public MessageBusConfig GetPlatformOptimizedConfig()
        {
            var optimized = (MessageBusConfig)Clone();
            
#if UNITY_EDITOR
            if (enableEditorDebugging)
            {
                optimized.enableMessageLogging = true;
                optimized.enableVerboseLogging = true;
                optimized.enableStatisticsCollection = true;
                optimized.enablePerformanceMetrics = true;
            }
#elif UNITY_ANDROID || UNITY_IOS
            if (enableMobileOptimizations)
            {
                // Mobile optimizations for performance and battery life
                optimized.maxMessagesPerFrame = Mathf.Min(maxMessagesPerFrame, 50);
                optimized.messageProcessingTimeSliceMs = Mathf.Max(messageProcessingTimeSliceMs, 0.020f);
                optimized.enableStatisticsCollection = false;
                optimized.enablePerformanceMetrics = false;
                optimized.enableVerboseLogging = false;
                optimized.workerThreadCount = Mathf.Min(workerThreadCount, 1);
                optimized.messagePoolMaxSize = Mathf.Min(messagePoolMaxSize, 500);
                optimized.enableMultithreading = false; // Reduce mobile complexity
            }
#elif UNITY_GAMECORE || UNITY_PS4 || UNITY_PS5 || UNITY_SWITCH
            if (enableConsoleOptimizations)
            {
                // Console optimizations for performance
                optimized.maxMessagesPerFrame = Mathf.Max(maxMessagesPerFrame, 200);
                optimized.enableMultithreading = true;
                optimized.workerThreadCount = Mathf.Min(workerThreadCount, 4);
                optimized.useJobSystemForProcessing = true;
                optimized.messagePoolMaxSize = Mathf.Max(messagePoolMaxSize, 2000);
            }
#elif UNITY_STANDALONE
            // Desktop can handle more processing
            optimized.maxMessagesPerFrame = Mathf.Max(maxMessagesPerFrame, 200);
            optimized.workerThreadCount = Mathf.Min(workerThreadCount, System.Environment.ProcessorCount);
#endif
            
            return optimized;
        }
    }
}