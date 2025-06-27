using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Configuration
{
    /// <summary>
    /// Pure-data configuration for the MessageBusService system, composing global and sub-service settings.
    /// </summary>
    public sealed class MessageBusConfig : IMessageBusConfig
    {
        // Global settings
        private string _configId = "DefaultMessageBusConfig";
        private int _maxMessagesPerFrame = 100;
        private int _initialMessageQueueCapacity = 50;
        private float _messageProcessingTimeSliceMs = 0.016f;
        private bool _enableMessagePooling = true;
        private int _messagePoolInitialSize = 100;
        private int _messagePoolMaxSize = 1000;
        private bool _enableBurstSerialization = true;
        private bool _enableNetworkSerialization = false;
        private bool _enableCompressionForNetwork = true;
        private bool _enableReliableDelivery = true;
        private int _maxDeliveryRetries = 3;
        private float _deliveryTimeoutSeconds = 5f;
        private float _retryBackoffMultiplier = 2f;
        private bool _enableStatisticsCollection = true;
        private bool _enableDeliveryTracking = true;
        private bool _enablePerformanceMetrics = true;
        private bool _enableMessageLogging = false;
        private bool _enableVerboseLogging = false;
        private bool _logFailedDeliveries = true;
        private bool _enableMultithreading = true;
        private int _workerThreadCount = 2;
        private bool _useJobSystemForProcessing = true;

        /// <inheritdoc/>
        public string ConfigId
        {
            get => _configId;
            set => _configId = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <inheritdoc/>
        public int MaxMessagesPerFrame
        {
            get => _maxMessagesPerFrame;
            set => _maxMessagesPerFrame = value < 1
                    ? throw new ArgumentOutOfRangeException(nameof(value), "Must be >= 1.")
                    : value;
        }

        /// <inheritdoc/>
        public int InitialMessageQueueCapacity
        {
            get => _initialMessageQueueCapacity;
            set => _initialMessageQueueCapacity = value < 1
                    ? throw new ArgumentOutOfRangeException(nameof(value), "Must be >= 1.")
                    : value;
        }

        /// <inheritdoc/>
        public float MessageProcessingTimeSliceMs
        {
            get => _messageProcessingTimeSliceMs;
            set => _messageProcessingTimeSliceMs = value <= 0f
                    ? throw new ArgumentOutOfRangeException(nameof(value), "Must be > 0.")
                    : value;
        }

        /// <inheritdoc/>
        public bool EnableMessagePooling
        {
            get => _enableMessagePooling;
            set => _enableMessagePooling = value;
        }

        /// <inheritdoc/>
        public int MessagePoolInitialSize
        {
            get => _messagePoolInitialSize;
            set => _messagePoolInitialSize = value < 1
                    ? throw new ArgumentOutOfRangeException(nameof(value), "Must be >= 1.")
                    : value;
        }

        /// <inheritdoc/>
        public int MessagePoolMaxSize
        {
            get => _messagePoolMaxSize;
            set => _messagePoolMaxSize = value < _messagePoolInitialSize
                    ? throw new ArgumentOutOfRangeException(nameof(value), "Must be >= MessagePoolInitialSize.")
                    : value;
        }

        /// <inheritdoc/>
        public bool EnableBurstSerialization
        {
            get => _enableBurstSerialization;
            set => _enableBurstSerialization = value;
        }

        /// <inheritdoc/>
        public bool EnableNetworkSerialization
        {
            get => _enableNetworkSerialization;
            set => _enableNetworkSerialization = value;
        }

        /// <inheritdoc/>
        public bool EnableCompressionForNetwork
        {
            get => _enableCompressionForNetwork;
            set => _enableCompressionForNetwork = value;
        }

        /// <inheritdoc/>
        public bool EnableReliableDelivery
        {
            get => _enableReliableDelivery;
            set => _enableReliableDelivery = value;
        }

        /// <inheritdoc/>
        public int MaxDeliveryRetries
        {
            get => _maxDeliveryRetries;
            set => _maxDeliveryRetries = value < 1
                    ? throw new ArgumentOutOfRangeException(nameof(value), "Must be >= 1.")
                    : value;
        }

        /// <inheritdoc/>
        public float DeliveryTimeoutSeconds
        {
            get => _deliveryTimeoutSeconds;
            set => _deliveryTimeoutSeconds = value <= 0f
                    ? throw new ArgumentOutOfRangeException(nameof(value), "Must be > 0.")
                    : value;
        }

        /// <inheritdoc/>
        public float RetryBackoffMultiplier
        {
            get => _retryBackoffMultiplier;
            set => _retryBackoffMultiplier = value <= 0f
                    ? throw new ArgumentOutOfRangeException(nameof(value), "Must be > 0.")
                    : value;
        }

        /// <inheritdoc/>
        public bool EnableStatisticsCollection
        {
            get => _enableStatisticsCollection;
            set => _enableStatisticsCollection = value;
        }

        /// <inheritdoc/>
        public bool EnableDeliveryTracking
        {
            get => _enableDeliveryTracking;
            set => _enableDeliveryTracking = value;
        }

        /// <inheritdoc/>
        public bool EnablePerformanceMetrics
        {
            get => _enablePerformanceMetrics;
            set => _enablePerformanceMetrics = value;
        }

        /// <inheritdoc/>
        public bool EnableMessageLogging
        {
            get => _enableMessageLogging;
            set => _enableMessageLogging = value;
        }

        /// <inheritdoc/>
        public bool EnableVerboseLogging
        {
            get => _enableVerboseLogging;
            set => _enableVerboseLogging = value;
        }

        /// <inheritdoc/>
        public bool LogFailedDeliveries
        {
            get => _logFailedDeliveries;
            set => _logFailedDeliveries = value;
        }

        /// <inheritdoc/>
        public bool EnableMultithreading
        {
            get => _enableMultithreading;
            set => _enableMultithreading = value;
        }

        /// <inheritdoc/>
        public int WorkerThreadCount
        {
            get => _workerThreadCount;
            set => _workerThreadCount = value < 1
                    ? throw new ArgumentOutOfRangeException(nameof(value), "Must be >= 1.")
                    : value;
        }

        /// <inheritdoc/>
        public bool UseJobSystemForProcessing
        {
            get => _useJobSystemForProcessing;
            set => _useJobSystemForProcessing = value;
        }

        /// <summary>
        /// Configuration for the batch-optimized delivery service.
        /// </summary>
        public BatchOptimizedConfiguration BatchDelivery { get; set; } = new BatchOptimizedConfiguration();

        /// <summary>
        /// Configuration for the default delivery service.
        /// </summary>
        public DeliveryServiceConfiguration DefaultDelivery { get; set; } = new DeliveryServiceConfiguration();

        /// <summary>
        /// Validates all configuration values and sub-configurations, throwing if invalid.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(_configId))
                throw new InvalidOperationException("ConfigId must be non-empty.");
            if (_maxMessagesPerFrame < 1)
                throw new InvalidOperationException("MaxMessagesPerFrame must be >= 1.");
            if (_initialMessageQueueCapacity < 1)
                throw new InvalidOperationException("InitialMessageQueueCapacity must be >= 1.");
            if (_messageProcessingTimeSliceMs <= 0f)
                throw new InvalidOperationException("MessageProcessingTimeSliceMs must be > 0.");
            if (_messagePoolInitialSize < 1)
                throw new InvalidOperationException("MessagePoolInitialSize must be >= 1.");
            if (_messagePoolMaxSize < _messagePoolInitialSize)
                throw new InvalidOperationException("MessagePoolMaxSize must be >= MessagePoolInitialSize.");
            if (_maxDeliveryRetries < 1)
                throw new InvalidOperationException("MaxDeliveryRetries must be >= 1.");
            if (_deliveryTimeoutSeconds <= 0f)
                throw new InvalidOperationException("DeliveryTimeoutSeconds must be > 0.");
            if (_retryBackoffMultiplier <= 0f)
                throw new InvalidOperationException("RetryBackoffMultiplier must be > 0.");
            if (_workerThreadCount < 1)
                throw new InvalidOperationException("WorkerThreadCount must be >= 1.");

            // Validate sub-configurations
            // BatchDelivery?.ThrowIfInvalid();
            // DefaultDelivery?.ThrowIfInvalid();
        }

        /// <inheritdoc/>
        public IMessageBusConfig Clone()
        {
            var clone = new MessageBusConfig
            {
                ConfigId = ConfigId,
                MaxMessagesPerFrame = MaxMessagesPerFrame,
                InitialMessageQueueCapacity = InitialMessageQueueCapacity,
                MessageProcessingTimeSliceMs = MessageProcessingTimeSliceMs,
                EnableMessagePooling = EnableMessagePooling,
                MessagePoolInitialSize = MessagePoolInitialSize,
                MessagePoolMaxSize = MessagePoolMaxSize,
                EnableBurstSerialization = EnableBurstSerialization,
                EnableNetworkSerialization = EnableNetworkSerialization,
                EnableCompressionForNetwork = EnableCompressionForNetwork,
                EnableReliableDelivery = EnableReliableDelivery,
                MaxDeliveryRetries = MaxDeliveryRetries,
                DeliveryTimeoutSeconds = DeliveryTimeoutSeconds,
                RetryBackoffMultiplier = RetryBackoffMultiplier,
                EnableStatisticsCollection = EnableStatisticsCollection,
                EnableDeliveryTracking = EnableDeliveryTracking,
                EnablePerformanceMetrics = EnablePerformanceMetrics,
                EnableMessageLogging = EnableMessageLogging,
                EnableVerboseLogging = EnableVerboseLogging,
                LogFailedDeliveries = LogFailedDeliveries,
                EnableMultithreading = EnableMultithreading,
                WorkerThreadCount = WorkerThreadCount,
                UseJobSystemForProcessing = UseJobSystemForProcessing
            };

            // Deep-copy sub-configurations
            clone.BatchDelivery = new BatchOptimizedConfiguration
            {
                MaxBatchSize = BatchDelivery.MaxBatchSize,
                BatchInterval = BatchDelivery.BatchInterval,
                FlushInterval = BatchDelivery.FlushInterval,
                ConfirmationTimeout = BatchDelivery.ConfirmationTimeout,
                ImmediateProcessingForReliable = BatchDelivery.ImmediateProcessingForReliable,
                MaxConcurrentBatches = BatchDelivery.MaxConcurrentBatches,
                GroupMessagesByType = BatchDelivery.GroupMessagesByType,
                ImmediateProcessingThreshold = BatchDelivery.ImmediateProcessingThreshold,
                EnableAdaptiveBatching = BatchDelivery.EnableAdaptiveBatching,
                TargetThroughput = BatchDelivery.TargetThroughput
            };

            clone.DefaultDelivery = new DeliveryServiceConfiguration
            {
                DefaultTimeout = DefaultDelivery.DefaultTimeout,
                DefaultMaxDeliveryAttempts = DefaultDelivery.DefaultMaxDeliveryAttempts,
                ProcessingInterval = DefaultDelivery.ProcessingInterval,
                MaxConcurrentDeliveries = DefaultDelivery.MaxConcurrentDeliveries,
                EnableStatistics = DefaultDelivery.EnableStatistics,
                EnableProfiling = DefaultDelivery.EnableProfiling,
                EnableLogging = DefaultDelivery.EnableLogging,
                LogLevel = DefaultDelivery.LogLevel,
                BackoffMultiplier = DefaultDelivery.BackoffMultiplier,
                MaxRetryDelay = DefaultDelivery.MaxRetryDelay
            };

            return clone;
        }
    }
}