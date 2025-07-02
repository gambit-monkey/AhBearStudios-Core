using System;
using AhBearStudios.Core.Pooling.Configurations;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Pooling.Interfaces;
using AhBearStudios.Core.Pooling.Services;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Builder for asynchronous object pool configurations using the fluent pattern.
    /// Implements IPoolConfigBuilder for configuration consistency.
    /// </summary>
    public class AsyncObjectPoolConfigBuilder : IPoolConfigBuilder<AsyncObjectPoolConfig, AsyncObjectPoolConfigBuilder>
    {
        /// <summary>
        /// The configuration being built
        /// </summary>
        private readonly AsyncObjectPoolConfig _config;

        ///<summary>
        /// Service locator for accessing pooling-related services.
        /// </summary>
        private readonly IPoolingService _service;
        
        ///<summary>
        /// Logger instance for recording diagnostic information and warnings during pool configuration.
        /// </summary>
        private readonly IPoolLogger _logger;

        /// <summary>
        /// Gets or sets the configuration ID
        /// </summary>
        public string ConfigId
        {
            get => _config.ConfigId;
            set => _config.ConfigId = value;
        }

        /// <summary>
        /// Creates a new asynchronous object pool configuration builder with default settings
        /// </summary>
        public AsyncObjectPoolConfigBuilder()
        {
            _config = new AsyncObjectPoolConfig();
            _service = DefaultPoolingServices.Instance;
            // Get services from service locator
            if (_service.HasService<IPoolLogger>())
            {
                _logger = _service.GetService<IPoolLogger>();
            }
        }

        /// <summary>
        /// Builds and returns a new AsyncObjectPoolConfig instance with the configured settings.
        /// </summary>
        /// <returns>A new AsyncObjectPoolConfig instance</returns>
        public AsyncObjectPoolConfig Build()
        {
            ValidateConfiguration();
            return _config.Clone() as AsyncObjectPoolConfig;
        }

        /// <summary>
        /// Validates the configuration and applies necessary adjustments
        /// </summary>
        protected virtual void ValidateConfiguration()
        {
            // Force thread-safe mode for async pools
            if (_config.ThreadingMode != PoolThreadingMode.ThreadSafe)
            {
                if (_logger != null)
                {
                    _logger.LogWarningInstance(
                        "Async pools require thread-safe mode. Changing ThreadingMode to ThreadSafe.");
                }

                _config.ThreadingMode = PoolThreadingMode.ThreadSafe;
            }

            // Validate other configuration aspects
            ValidateThreadingSettings();
            ValidateTimeoutSettings();
            ValidateCapacitySettings();

            // Final validation of the entire configuration
            _config.Validate();
        }

        private void ValidateThreadingSettings()
        {
            if (_config.MaxConcurrentOperations > Environment.ProcessorCount * 2)
            {
                if (_logger != null)
                {
                    _logger.LogWarningInstance(
                        $"MaxConcurrentOperations ({_config.MaxConcurrentOperations}) exceeds optimal thread count " +
                        $"for the system ({Environment.ProcessorCount} cores). Consider reducing to avoid overhead.");
                }
            }
        }

        private void ValidateTimeoutSettings()
        {
            if (_config.AcquireTimeoutMs < 100 && !_config.ThrowOnAsyncFailure)
            {
                if (_logger != null)
                {
                    _logger.LogWarningInstance(
                        "Short acquire timeout configured without exception handling enabled. " +
                        "Consider enabling ThrowOnAsyncFailure for better error detection.");
                }
            }
        }

        private void ValidateCapacitySettings()
        {
            if (_config.UseBackgroundInitialization && _config.PrewarmOnInit && _config.InitialCapacity > 20)
            {
                if (_logger != null)
                {
                    _logger.LogWarningInstance(
                        "Large initial capacity with background initialization and prewarming may impact performance. " +
                        "Consider adjusting these settings based on your use case.");
                }
            }
        }

        /// <summary>
        /// Sets the initial capacity of the pool.
        /// </summary>
        /// <param name="capacity">The initial number of objects to allocate in the pool.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public AsyncObjectPoolConfigBuilder WithInitialCapacity(int capacity)
        {
            _config.InitialCapacity = capacity;
            return this;
        }
        
        /// <summary>
        /// Sets the initial capacity of the pool.
        /// </summary>
        /// <param name="capacity">The initial number of objects to allocate in the pool.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public AsyncObjectPoolConfigBuilder WithMinimumCapacity(int minSize)
        {
            _config.MinimumCapacity = minSize;
            return this;
        }

        /// <summary>
        /// Sets the maximum size limit for the pool.
        /// </summary>
        /// <param name="maxSize">The maximum number of objects allowed in the pool. Use 0 for unlimited.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public AsyncObjectPoolConfigBuilder WithMaximumCapacity(int maxSize)
        {
            _config.MaximumCapacity = maxSize;
            return this;
        }

        /// <summary>
        /// Sets the threading mode for the pool. Note that async pools always use thread-safe mode.
        /// </summary>
        /// <param name="mode">The desired threading mode (will be overridden to ThreadSafe).</param>
        /// <returns>This builder instance for method chaining.</returns>
        public AsyncObjectPoolConfigBuilder WithThreadingMode(PoolThreadingMode mode)
        {
            // Always use ThreadSafe mode for async pools
            _config.ThreadingMode = PoolThreadingMode.ThreadSafe;
            return this;
        }

        /// <summary>
        /// Enables or disables automatic pool shrinking when usage decreases.
        /// </summary>
        /// <param name="enable">True to enable automatic shrinking; otherwise, false.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public AsyncObjectPoolConfigBuilder WithAutoShrink(bool enable)
        {
            _config.EnableAutoShrink = enable;
            return this;
        }

        /// <summary>
        /// Enables or disables collection of pool performance metrics.
        /// </summary>
        /// <param name="enable">True to enable metrics collection; otherwise, false.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public AsyncObjectPoolConfigBuilder WithMetrics(bool enable)
        {
            _config.CollectMetrics = enable;
            return this;
        }

        /// <summary>
        /// Sets the maximum duration in milliseconds to wait for pool operations
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithAcquireTimeout(int timeoutMs)
        {
            _config.AcquireTimeoutMs = timeoutMs;
            return this;
        }

        /// <summary>
        /// Configures whether to report detailed progress of async operations.
        /// </summary>
        /// <param name="reportDetailedProgress">True to enable detailed progress reporting, false otherwise</param>
        /// <returns>This builder for method chaining</returns>
        public AsyncObjectPoolConfigBuilder WithReportDetailedProgress(bool reportDetailedProgress)
        {
            _config.ReportDetailedProgress = reportDetailedProgress;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of concurrent async operations
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithMaxConcurrentOperations(int maxOperations)
        {
            _config.MaxConcurrentOperations = maxOperations;
            return this;
        }

        /// <summary>
        /// Enables or disables background initialization of pool items
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithBackgroundInitialization(bool enable)
        {
            _config.UseBackgroundInitialization = enable;
            return this;
        }

        /// <summary>
        /// Enables or disables background cleanup of pool items
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithBackgroundCleanup(bool enable)
        {
            _config.UseBackgroundCleanup = enable;
            return this;
        }

        /// <summary>
        /// Enables or disables operation queueing
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithOperationQueue(bool enable)
        {
            _config.UseOperationQueue = enable;
            return this;
        }

        /// <summary>
        /// Sets the capacity of the operation queue
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithOperationQueueCapacity(int capacity)
        {
            _config.OperationQueueCapacity = capacity;
            return this;
        }

        /// <summary>
        /// Enables or disables batch processing of pool operations
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithBatchProcessing(bool enable)
        {
            _config.UseBatchProcessing = enable;
            return this;
        }

        /// <summary>
        /// Sets the size of operation batches
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithBatchSize(int size)
        {
            _config.BatchSize = size;
            return this;
        }

        /// <summary>
        /// Configures whether to cancel pending operations on pool disposal
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithCancelPendingOnDispose(bool enable)
        {
            _config.CancelPendingOnDispose = enable;
            return this;
        }

        /// <summary>
        /// Sets the timeout duration for cancellation operations
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithCancellationTimeout(int timeoutMs)
        {
            _config.CancellationTimeoutMs = timeoutMs;
            return this;
        }

        /// <summary>
        /// Configures whether to throw exceptions on async operation failures
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithThrowOnAsyncFailure(bool enable)
        {
            _config.ThrowOnAsyncFailure = enable;
            return this;
        }

        /// <summary>
        /// Enables or disables priority-based processing of pool operations
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithPriorityProcessing(bool enable)
        {
            _config.UsePriorityProcessing = enable;
            return this;
        }

        /// <summary>
        /// Configures whether to report detailed progress of async operations
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithDetailedProgress(bool enable)
        {
            _config.ReportDetailedProgress = enable;
            return this;
        }

        /// <summary>
        /// Sets the shrink threshold for the pool
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithShrinkThreshold(float threshold)
        {
            _config.ShrinkThreshold = threshold;
            return this;
        }

        /// <summary>
        /// Sets the interval for automatic pool shrinking
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithShrinkInterval(float intervalSeconds)
        {
            _config.ShrinkInterval = intervalSeconds;
            return this;
        }

        /// <summary>
        /// Configures whether to prewarm the pool on initialization
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithPrewarming(bool enable)
        {
            _config.PrewarmOnInit = enable;
            return this;
        }

        /// <summary>
        /// Configures whether to reset items on release
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithResetOnRelease(bool enable)
        {
            _config.ResetOnRelease = enable;
            return this;
        }

        /// <summary>
        /// Configures detailed logging
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithDetailedLogging(bool enable)
        {
            _config.DetailedLogging = enable;
            return this;
        }

        /// <summary>
        /// Configures warning logging
        /// </summary>
        public AsyncObjectPoolConfigBuilder WithWarningLogging(bool enable)
        {
            _config.LogWarnings = enable;
            return this;
        }

        /// <summary>
        /// Sets the native allocator type for pool memory management
        /// </summary>
        /// <param name="allocator">The Unity.Collections allocator to use</param>
        /// <returns>This builder instance for method chaining</returns>
        public AsyncObjectPoolConfigBuilder WithNativeAllocator(Allocator allocator)
        {
            _config.NativeAllocator = allocator;
            return this;
        }

        /// <summary>
        /// Enables or disables exponential growth for pool capacity
        /// </summary>
        /// <param name="useExponentialGrowth">True to use exponential growth, false for linear growth</param>
        /// <returns>This builder instance for method chaining</returns>
        public AsyncObjectPoolConfigBuilder WithExponentialGrowth(bool useExponentialGrowth)
        {
            _config.UseExponentialGrowth = useExponentialGrowth;
            return this;
        }

        /// <summary>
        /// Sets the growth factor for exponential pool expansion
        /// </summary>
        /// <param name="factor">The multiplier used when growing the pool capacity</param>
        /// <returns>This builder instance for method chaining</returns>
        public AsyncObjectPoolConfigBuilder WithGrowthFactor(float factor)
        {
            _config.GrowthFactor = factor;
            return this;
        }

        /// <summary>
        /// Sets the fixed increment size for linear pool growth
        /// </summary>
        /// <param name="increment">The number of items to add when growing the pool</param>
        /// <returns>This builder instance for method chaining</returns>
        public AsyncObjectPoolConfigBuilder WithGrowthIncrement(int increment)
        {
            _config.GrowthIncrement = increment;
            return this;
        }

        /// <summary>
        /// Configures whether to throw an exception when exceeding maximum pool size
        /// </summary>
        /// <param name="throwIfExceeding">True to throw exception, false to log warning</param>
        /// <returns>This builder instance for method chaining</returns>
        public AsyncObjectPoolConfigBuilder WithExceptionOnExceedingMaxCount(bool throwIfExceeding)
        {
            _config.ThrowIfExceedingMaxCount = throwIfExceeding;
            return this;
        }

        /// <summary>
        /// Configures metrics collection for the pool
        /// </summary>
        /// <param name="collectMetrics">True to enable metrics collection</param>
        /// <returns>This builder instance for method chaining</returns>
        public AsyncObjectPoolConfigBuilder WithMetricsCollection(bool collectMetrics)
        {
            _config.CollectMetrics = collectMetrics;
            return this;
        }

        /// <summary>
        /// Initializes a builder from an existing AsyncObjectPoolConfig.
        /// </summary>
        /// <param name="config">The source configuration.</param>
        /// <returns>The initialized builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
        public AsyncObjectPoolConfigBuilder FromExisting(AsyncObjectPoolConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            ConfigId = config.ConfigId;

            // Initialize builder with all configuration properties
            return this
                .WithInitialCapacity(config.InitialCapacity)
                .WithMinimumCapacity(config.MinimumCapacity)
                .WithMaximumCapacity(config.MaximumCapacity)
                .WithPrewarming(config.PrewarmOnInit)
                .WithMetricsCollection(config.CollectMetrics)
                .WithDetailedLogging(config.DetailedLogging)
                .WithWarningLogging(config.LogWarnings)
                .WithResetOnRelease(config.ResetOnRelease)
                .WithThreadingMode(PoolThreadingMode.ThreadSafe)
                .WithAutoShrink(config.EnableAutoShrink)
                .WithShrinkThreshold(config.ShrinkThreshold)
                .WithShrinkInterval(config.ShrinkInterval)
                .WithNativeAllocator(config.NativeAllocator)
                .WithExponentialGrowth(config.UseExponentialGrowth)
                .WithGrowthFactor(config.GrowthFactor)
                .WithGrowthIncrement(config.GrowthIncrement)
                .WithExceptionOnExceedingMaxCount(config.ThrowIfExceedingMaxCount)
                .WithAcquireTimeout(config.AcquireTimeoutMs)
                .WithMaxConcurrentOperations(config.MaxConcurrentOperations)
                .WithBackgroundInitialization(config.UseBackgroundInitialization)
                .WithBackgroundCleanup(config.UseBackgroundCleanup)
                .WithOperationQueue(config.UseOperationQueue)
                .WithOperationQueueCapacity(config.OperationQueueCapacity)
                .WithBatchProcessing(config.UseBatchProcessing)
                .WithBatchSize(config.BatchSize)
                .WithCancelPendingOnDispose(config.CancelPendingOnDispose)
                .WithCancellationTimeout(config.CancellationTimeoutMs)
                .WithThrowOnAsyncFailure(config.ThrowOnAsyncFailure)
                .WithPriorityProcessing(config.UsePriorityProcessing)
                .WithDetailedProgress(config.ReportDetailedProgress);
        }

        /// <summary>
        /// Configures the builder for high-performance mode with minimal overhead
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public AsyncObjectPoolConfigBuilder AsHighPerformance()
        {
            // Disable monitoring and diagnostics overhead
            _config.CollectMetrics = false;
            _config.DetailedLogging = false;
            _config.LogWarnings = false;
            _config.ReportDetailedProgress = false;

            // Configure for optimal performance
            _config.MaxConcurrentOperations = 8;
            _config.UseBatchProcessing = true;
            _config.BatchSize = 16;
            _config.UseOperationQueue = true;
            _config.OperationQueueCapacity = 32;
            _config.UseBackgroundInitialization = true;
            _config.UseBackgroundCleanup = true;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            _config.PrewarmOnInit = true;
            _config.EnableAutoShrink = false;

            return this;
        }

        /// <summary>
        /// Configures the builder for debug mode with extensive tracking and validation
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public AsyncObjectPoolConfigBuilder AsDebug()
        {
            // Enable monitoring and diagnostics
            _config.CollectMetrics = true;
            _config.DetailedLogging = true;
            _config.LogWarnings = true;
            _config.ReportDetailedProgress = true;

            // Configure for debugging
            _config.MaxConcurrentOperations = 2;
            _config.UseBatchProcessing = false;
            _config.UseOperationQueue = true;
            _config.OperationQueueCapacity = 8;
            _config.UseBackgroundInitialization = true;
            _config.UseBackgroundCleanup = true;
            _config.ThrowOnAsyncFailure = true;
            _config.AcquireTimeoutMs = 2000;

            return this;
        }

        /// <summary>
        /// Configures the builder with balanced settings between performance and monitoring
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public AsyncObjectPoolConfigBuilder AsBalanced()
        {
            // Configure balanced monitoring settings
            _config.CollectMetrics = true;
            _config.DetailedLogging = false;
            _config.LogWarnings = true;

            // Configure balanced performance settings
            _config.MaxConcurrentOperations = 4;
            _config.UseBatchProcessing = true;
            _config.BatchSize = 8;
            _config.UseOperationQueue = true;
            _config.OperationQueueCapacity = 16;
            _config.UseBackgroundInitialization = true;
            _config.UseBackgroundCleanup = true;
            _config.UseExponentialGrowth = true;

            return this;
        }

        /// <summary>
        /// Configures the builder optimized for UI operations with prioritized responsiveness
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public AsyncObjectPoolConfigBuilder AsResponsive()
        {
            // Configure for responsiveness
            _config.MaxConcurrentOperations = 2;
            _config.UseBatchProcessing = true;
            _config.BatchSize = 4;
            _config.UseOperationQueue = true;
            _config.UsePriorityProcessing = true;
            _config.UseBackgroundInitialization = true;
            _config.UseBackgroundCleanup = true;
            _config.AcquireTimeoutMs = 500;
            _config.CancellationTimeoutMs = 2000;

            return this;
        }

        /// <summary>
        /// Configures the builder for memory-efficient operation
        /// </summary>
        /// <returns>The builder instance for method chaining</returns>
        public AsyncObjectPoolConfigBuilder AsMemoryEfficient()
        {
            // Configure for memory efficiency
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.4f;
            _config.ShrinkInterval = 30f;
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 4;
            _config.BatchSize = 4;
            _config.OperationQueueCapacity = 8;
            _config.MaxConcurrentOperations = 2;

            return this;
        }
    }
}