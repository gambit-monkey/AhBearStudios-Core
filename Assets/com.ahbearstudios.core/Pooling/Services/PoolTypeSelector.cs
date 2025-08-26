using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Pools;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Implementation of pool type selector that follows CLAUDE.md Builder → Config → Factory → Service pattern.
    /// Analyzes object types and configurations to choose optimal pool types for factory creation.
    /// Does NOT create pool instances - that responsibility belongs to appropriate factories.
    /// </summary>
    public class PoolTypeSelector : IPoolTypeSelector
    {
        private readonly ILoggingService _loggingService;
        private readonly IMessageBusService _messageBusService;

        /// <summary>
        /// Initializes a new instance of the PoolTypeSelector.
        /// </summary>
        /// <param name="loggingService">Logging service for system integration</param>
        /// <param name="messageBusService">Message bus service for event publishing</param>
        public PoolTypeSelector(
            ILoggingService loggingService,
            IMessageBusService messageBusService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));

            _loggingService.LogInfo("PoolTypeSelector initialized with specialized pool integration");
        }

        /// <inheritdoc />
        public PoolType SelectPoolType<T>(PoolConfiguration configuration) where T : class, IPooledObject, new()
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var objectType = typeof(T);
            
            _loggingService.LogDebug($"Selecting pool type for {objectType.Name}");

            // Check for specific type matches first
            if (IsNetworkBufferType<T>())
            {
                return SelectNetworkBufferPoolType<T>(configuration);
            }

            if (IsLogDataType<T>())
            {
                _loggingService.LogDebug($"Selected ManagedLogData pool for {objectType.Name}");
                return PoolType.ManagedLogData;
            }

            // Check for size-based selection for generic types
            var estimatedSize = GetEstimatedObjectSize<T>();
            var poolType = SelectPoolTypeBySize(estimatedSize, configuration);

            _loggingService.LogDebug($"Selected {poolType} pool for {objectType.Name} (estimated size: {estimatedSize} bytes)");
            return poolType;
        }

        /// <inheritdoc />
        public bool CanUsePoolType<T>(PoolType poolType) where T : class, IPooledObject, new()
        {
            var objectType = typeof(T);

            return poolType switch
            {
                PoolType.SmallBuffer => IsNetworkBufferCompatible<T>(),
                PoolType.MediumBuffer => IsNetworkBufferCompatible<T>(),
                PoolType.LargeBuffer => IsNetworkBufferCompatible<T>(),
                PoolType.CompressionBuffer => IsNetworkBufferCompatible<T>(),
                PoolType.ManagedLogData => IsLogDataType<T>(),
                PoolType.Generic => true, // Generic can handle any IPooledObject
                _ => false
            };
        }

        /// <inheritdoc />
        public long GetEstimatedMemoryUsage<T>(PoolType poolType) where T : class, IPooledObject, new()
        {
            var baseObjectSize = GetEstimatedObjectSize<T>();

            return poolType switch
            {
                PoolType.SmallBuffer => baseObjectSize + 1024, // 1KB buffer overhead
                PoolType.MediumBuffer => baseObjectSize + 32768, // 32KB buffer overhead
                PoolType.LargeBuffer => baseObjectSize + 262144, // 256KB buffer overhead
                PoolType.CompressionBuffer => baseObjectSize + 65536, // 64KB compression overhead
                PoolType.ManagedLogData => baseObjectSize + 512, // 512B logging overhead
                PoolType.Generic => baseObjectSize,
                _ => baseObjectSize
            };
        }

        private PoolType SelectNetworkBufferPoolType<T>(PoolConfiguration configuration) where T : class, IPooledObject, new()
        {
            // Use configuration hints and performance budget to select buffer size
            var performanceBudget = configuration.PerformanceBudget;
            var maxCapacity = configuration.MaxCapacity;

            // High-performance configurations prefer smaller, more frequent allocations
            if (performanceBudget?.TargetFrameRate >= 60 && maxCapacity <= 100)
            {
                return PoolType.SmallBuffer;
            }

            // Network configurations with larger capacities use medium buffers
            if (configuration.StrategyType == PoolingStrategyType.AdaptiveNetwork)
            {
                return maxCapacity > 500 ? PoolType.LargeBuffer : PoolType.MediumBuffer;
            }

            // Default to medium buffers for balanced performance
            return PoolType.MediumBuffer;
        }

        private PoolType SelectPoolTypeBySize(long estimatedSize, PoolConfiguration configuration)
        {
            // Size-based selection with configuration hints
            return estimatedSize switch
            {
                <= 1024 => PoolType.SmallBuffer,
                <= 65536 => PoolType.MediumBuffer,
                > 65536 => PoolType.LargeBuffer
            };
        }

        private bool IsNetworkBufferType<T>() where T : class, IPooledObject, new()
        {
            var type = typeof(T);
            return type == typeof(PooledNetworkBuffer) ||
                   type.Name.Contains("Buffer") ||
                   type.Name.Contains("Network");
        }

        private bool IsNetworkBufferCompatible<T>() where T : class, IPooledObject, new()
        {
            // Check if type can work with network buffer pools
            var type = typeof(T);
            return typeof(PooledNetworkBuffer).IsAssignableFrom(type) ||
                   IsNetworkBufferType<T>();
        }

        private bool IsLogDataType<T>() where T : class, IPooledObject, new()
        {
            var type = typeof(T);
            return type == typeof(ManagedLogData) ||
                   type.Name.Contains("Log") ||
                   type.Name.Contains("Audit");
        }

        private long GetEstimatedObjectSize<T>() where T : class, IPooledObject, new()
        {
            var type = typeof(T);
            
            // Try to get size from the object if it implements the method
            try
            {
                var instance = new T();
                return instance.GetEstimatedMemoryUsage();
            }
            catch
            {
                // Fallback to basic size estimation
                return EstimateTypeSizeBasic(type);
            }
        }

        private long EstimateTypeSizeBasic(Type type)
        {
            // Basic size estimation based on type characteristics
            if (type.Name.Contains("Buffer"))
                return 32768; // 32KB default for buffers
            if (type.Name.Contains("Log"))
                return 512; // 512B for log entries
            if (type.Name.Contains("Message"))
                return 1024; // 1KB for messages

            return 256; // 256B default estimate
        }

    }
}