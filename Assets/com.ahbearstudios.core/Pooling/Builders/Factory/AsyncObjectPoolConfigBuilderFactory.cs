using System;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;

namespace AhBearStudios.Pooling.Builders
{
    /// <summary>
    /// Factory for creating various asynchronous pool configuration builders.
    /// Part of the PoolConfigBuilderFactory that focuses on AsyncObjectPoolConfigBuilder creation.
    /// </summary>
    public static partial class PoolConfigBuilderFactory
    {
        /// <summary>
        /// Creates an asynchronous pool configuration builder with default settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new asynchronous pool configuration builder</returns>
        public static AsyncObjectPoolConfigBuilder Async(int initialCapacity = 32)
        {
            return new AsyncObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);
        }

        /// <summary>
        /// Creates an asynchronous pool configuration builder optimized for high performance
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new asynchronous pool configuration builder with high performance settings</returns>
        public static AsyncObjectPoolConfigBuilder AsyncHighPerformance(int initialCapacity = 64)
        {
            return new AsyncObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsHighPerformance()
                .WithMaximumCapacity(initialCapacity * 4);
        }

        /// <summary>
        /// Creates an asynchronous pool configuration builder with debug settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new asynchronous pool configuration builder with debug settings</returns>
        public static AsyncObjectPoolConfigBuilder AsyncDebug(int initialCapacity = 16)
        {
            return new AsyncObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsDebug();
        }

        /// <summary>
        /// Creates an asynchronous pool configuration builder optimized for memory efficiency
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new asynchronous pool configuration builder optimized for memory efficiency</returns>
        public static AsyncObjectPoolConfigBuilder AsyncMemoryEfficient(int initialCapacity = 16)
        {
            return new AsyncObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsMemoryEfficient();
        }

        /// <summary>
        /// Creates an asynchronous pool configuration builder optimized for UI responsiveness
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new asynchronous pool configuration builder optimized for UI responsiveness</returns>
        public static AsyncObjectPoolConfigBuilder AsyncResponsive(int initialCapacity = 32)
        {
            return new AsyncObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsResponsive()
                .WithMaxConcurrentOperations(2)
                .WithAcquireTimeout(250)
                .WithPriorityProcessing(true);
        }

        /// <summary>
        /// Creates an asynchronous pool configuration builder with balanced settings
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new asynchronous pool configuration builder with balanced settings</returns>
        public static AsyncObjectPoolConfigBuilder AsyncBalanced(int initialCapacity = 32)
        {
            return new AsyncObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .AsBalanced();
        }

        /// <summary>
        /// Creates an asynchronous pool configuration builder with monitoring capabilities
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new asynchronous pool configuration builder with monitoring settings</returns>
        public static AsyncObjectPoolConfigBuilder AsyncWithMonitoring(int initialCapacity = 32)
        {
            return new AsyncObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithMetricsCollection(true)
                .WithDetailedLogging(true)
                .WithDetailedProgress(true)
                .WithThreadingMode(PoolThreadingMode.ThreadSafe)
                .WithMaxConcurrentOperations(4)
                .WithOperationQueue(true)
                .WithOperationQueueCapacity(32);
        }

        /// <summary>
        /// Creates an asynchronous pool configuration builder with extensive error handling
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <returns>A new asynchronous pool configuration builder with error handling settings</returns>
        public static AsyncObjectPoolConfigBuilder AsyncWithErrorHandling(int initialCapacity = 16)
        {
            return new AsyncObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithThrowOnAsyncFailure(true)
                .WithAcquireTimeout(2000)
                .WithCancellationTimeout(1000)
                .WithDetailedProgress(true)
                .WithDetailedLogging(true)
                .WithWarningLogging(true)
                .WithCancelPendingOnDispose(true);
        }

        /// <summary>
        /// Creates an asynchronous pool configuration builder with batch processing capabilities
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="batchSize">Size of processing batches</param>
        /// <returns>A new asynchronous pool configuration builder with batch processing settings</returns>
        public static AsyncObjectPoolConfigBuilder AsyncBatchProcessing(int initialCapacity = 64, int batchSize = 16)
        {
            return new AsyncObjectPoolConfigBuilder()
                .WithInitialCapacity(initialCapacity)
                .WithBatchProcessing(true)
                .WithBatchSize(batchSize)
                .WithOperationQueue(true)
                .WithOperationQueueCapacity(batchSize * 4)
                .WithMaxConcurrentOperations(Environment.ProcessorCount);
        }

        /// <summary>
        /// Creates an asynchronous pool configuration builder from an existing configuration
        /// </summary>
        /// <param name="existingConfig">The existing configuration to copy settings from</param>
        /// <returns>A new asynchronous pool configuration builder initialized with existing settings</returns>
        public static AsyncObjectPoolConfigBuilder FromExistingAsyncConfig(IPoolConfig existingConfig)
        {
            if (existingConfig == null)
            {
                throw new ArgumentNullException(nameof(existingConfig), "Existing configuration cannot be null");
            }

            var builder = new AsyncObjectPoolConfigBuilder()
                .WithInitialCapacity(existingConfig.InitialCapacity)
                .WithMaximumCapacity(existingConfig.MaximumCapacity)
                .WithPrewarming(existingConfig.PrewarmOnInit)
                .WithExponentialGrowth(existingConfig.UseExponentialGrowth)
                .WithGrowthFactor(existingConfig.GrowthFactor)
                .WithGrowthIncrement(existingConfig.GrowthIncrement)
                .WithAutoShrink(existingConfig.EnableAutoShrink)
                .WithShrinkThreshold(existingConfig.ShrinkThreshold)
                .WithShrinkInterval(existingConfig.ShrinkInterval)
                .WithThreadingMode(existingConfig.ThreadingMode)
                .WithNativeAllocator(existingConfig.NativeAllocator)
                .WithWarningLogging(existingConfig.LogWarnings)
                .WithMetricsCollection(existingConfig.CollectMetrics)
                .WithDetailedLogging(existingConfig.DetailedLogging)
                .WithResetOnRelease(existingConfig.ResetOnRelease)
                .WithExceptionOnExceedingMaxCount(existingConfig.ThrowIfExceedingMaxCount);

            if (existingConfig is AsyncObjectPoolConfig asyncConfig)
            {
                // Copy async-specific properties if the existing config is an AsyncObjectPoolConfig
                builder
                    .WithAcquireTimeout(asyncConfig.AcquireTimeoutMs)
                    .WithCancellationTimeout(asyncConfig.CancellationTimeoutMs)
                    .WithMaxConcurrentOperations(asyncConfig.MaxConcurrentOperations)
                    .WithBackgroundInitialization(asyncConfig.UseBackgroundInitialization)
                    .WithBackgroundCleanup(asyncConfig.UseBackgroundCleanup)
                    .WithOperationQueue(asyncConfig.UseOperationQueue)
                    .WithOperationQueueCapacity(asyncConfig.OperationQueueCapacity)
                    .WithBatchProcessing(asyncConfig.UseBatchProcessing)
                    .WithBatchSize(asyncConfig.BatchSize)
                    .WithCancelPendingOnDispose(asyncConfig.CancelPendingOnDispose)
                    .WithThrowOnAsyncFailure(asyncConfig.ThrowOnAsyncFailure)
                    .WithPriorityProcessing(asyncConfig.UsePriorityProcessing)
                    .WithDetailedProgress(asyncConfig.ReportDetailedProgress);
            }

            return builder;
        }
    }
}