using System;
using AhBearStudios.Pooling.Configurations;
using AhBearStudios.Pooling.Core;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Pooling.Builders
{
    /// <summary>
    /// Builder for value type pool configurations implementing IPoolConfigBuilder.
    /// Provides specialized settings for value type pooling with a fluent API.
    /// </summary>
    public class ValueTypePoolConfigBuilder : IPoolConfigBuilder<ValueTypePoolConfig, ValueTypePoolConfigBuilder>
    {
        /// <summary>
        /// The configuration being built
        /// </summary>
        private readonly ValueTypePoolConfig _config;

        /// <summary>
        /// Creates a new builder with default settings
        /// </summary>
        public ValueTypePoolConfigBuilder()
        {
            _config = new ValueTypePoolConfig();
        }

        /// <summary>
        /// Creates a new builder initialized with an existing value type configuration
        /// </summary>
        /// <param name="config">The existing configuration to initialize with</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public ValueTypePoolConfigBuilder(ValueTypePoolConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
            }

            _config = config.Clone() as ValueTypePoolConfig
                      ?? throw new InvalidOperationException("Failed to clone configuration");
        }

        /// <summary>
        /// Creates a new builder initialized with a generic pool configuration
        /// </summary>
        /// <param name="config">The existing configuration to initialize with</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public ValueTypePoolConfigBuilder(IPoolConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
            }

            _config = new ValueTypePoolConfig(config);
        }

        /// <summary>
        /// Sets whether to zero memory when releasing elements
        /// </summary>
        public ValueTypePoolConfigBuilder WithZeroMemoryOnRelease(bool zeroMemory)
        {
            _config.ClearMemoryOnRelease = zeroMemory;
            return this;
        }

        /// <summary>
        /// Sets the struct layout type
        /// </summary>
        public ValueTypePoolConfigBuilder WithStructLayout(StructLayoutType layoutType)
        {
            _config.StructLayoutType = layoutType;
            return this;
        }

        /// <summary>
        /// Sets whether to use inline handling for elements
        /// </summary>
        public ValueTypePoolConfigBuilder WithInlineHandling(bool useInlineHandling)
        {
            _config.UseInlineHandling = useInlineHandling;
            return this;
        }

        /// <summary>
        /// Sets whether to optimize for blittable types
        /// </summary>
        public ValueTypePoolConfigBuilder WithBlittableOptimization(bool useBlittableOptimization)
        {
            _config.BlittableTypesOnly = useBlittableOptimization;
            return this;
        }

        /// <summary>
        /// Sets whether to use validation checks
        /// </summary>
        public ValueTypePoolConfigBuilder WithValidationChecks(bool useValidationChecks)
        {
            _config.UseValidationChecks = useValidationChecks;
            return this;
        }

        /// <summary>
        /// Sets the overflow handling behavior
        /// </summary>
        public ValueTypePoolConfigBuilder WithOverflowHandling(OverflowHandlingType handlingType)
        {
            _config.OverflowHandling = handlingType;
            return this;
        }

        /// <summary>
        /// Sets whether the pool should be automatically disposed
        /// </summary>
        public ValueTypePoolConfigBuilder WithAutomaticDisposal(bool useAutomaticDisposal)
        {
            _config.UseAutomaticDisposal = useAutomaticDisposal;
            return this;
        }

        /// <summary>
        /// Sets the timeout for automatic disposal in seconds
        /// </summary>
        public ValueTypePoolConfigBuilder WithDisposeTimeout(float timeoutSeconds)
        {
            _config.DisposeTimeoutSeconds = Mathf.Max(0f, timeoutSeconds);
            return this;
        }

        /// <summary>
        /// Sets whether the pool is compatible with Unity Jobs system
        /// </summary>
        public ValueTypePoolConfigBuilder WithJobCompatibility(bool isJobCompatible)
        {
            _config.IsJobCompatible = isJobCompatible;
            return this;
        }

        /// <summary>
        /// Sets the memory alignment size in bytes
        /// </summary>
        public ValueTypePoolConfigBuilder WithMemoryAlignment(int alignmentBytes)
        {
            _config.UseMemoryAlignment = true;
            _config.AlignmentSizeBytes = Math.Max(1, alignmentBytes);
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for initial capacity
        /// </summary>
        public ValueTypePoolConfigBuilder WithInitialCapacity(int capacity)
        {
            _config.InitialCapacity = Mathf.Max(0, capacity);
            return this;
        }

        /// <summary>
        /// Implements IPoolConfigBuilder interface method for maximum size
        /// </summary>
        public ValueTypePoolConfigBuilder WithMaxSize(int maxSize)
        {
            _config.MaximumCapacity = maxSize < 0 ? 0 : maxSize;
            return this;
        }

        /// <summary>
        /// Sets whether to use exponential growth when expanding
        /// </summary>
        public ValueTypePoolConfigBuilder WithExponentialGrowth(bool useExponentialGrowth)
        {
            _config.UseExponentialGrowth = useExponentialGrowth;
            return this;
        }

        /// <summary>
        /// Sets the growth factor for exponential expansion
        /// </summary>
        public ValueTypePoolConfigBuilder WithGrowthFactor(float growthFactor)
        {
            _config.GrowthFactor = Mathf.Max(1.1f, growthFactor);
            return this;
        }

        /// <summary>
        /// Sets whether to use memory alignment
        /// </summary>
        public ValueTypePoolConfigBuilder WithMemoryAlignment(bool useAlignment)
        {
            _config.UseMemoryAlignment = useAlignment;
            return this;
        }

        /// <summary>
        /// Sets the memory alignment size in bytes
        /// </summary>
        public ValueTypePoolConfigBuilder WithAlignmentSize(int sizeBytes)
        {
            _config.AlignmentSizeBytes = Mathf.Max(1, sizeBytes);
            return this;
        }

        /// <summary>
        /// Sets whether to clear memory on release
        /// </summary>
        public ValueTypePoolConfigBuilder WithClearMemoryOnRelease(bool clearOnRelease)
        {
            _config.ClearMemoryOnRelease = clearOnRelease;
            return this;
        }

        /// <summary>
        /// Sets whether to support only blittable types
        /// </summary>
        public ValueTypePoolConfigBuilder WithBlittableTypesOnly(bool blittableOnly)
        {
            _config.BlittableTypesOnly = blittableOnly;
            return this;
        }

        /// <summary>
        /// Sets whether to optimize for SIMD operations
        /// </summary>
        public ValueTypePoolConfigBuilder WithSimdOptimization(bool optimizeForSimd)
        {
            _config.UseSIMDAlignment = optimizeForSimd;
            if (optimizeForSimd)
            {
                _config.UseMemoryAlignment = true;
                _config.AlignmentSizeBytes = Math.Max(_config.AlignmentSizeBytes, 16);
                _config.BlittableTypesOnly = true;
            }

            return this;
        }

        /// <summary>
        /// Sets whether to use Burst-compatible allocation
        /// </summary>
        public ValueTypePoolConfigBuilder WithBurstAllocation(bool useBurst, Allocator allocator = Allocator.Persistent)
        {
            _config.UseBurstAllocation = useBurst;
            if (useBurst)
            {
                _config.BlittableTypesOnly = true;
                _config.NativeAllocator = allocator;
            }

            return this;
        }

        /// <summary>
        /// Sets whether to skip default initialization
        /// </summary>
        public ValueTypePoolConfigBuilder WithSkipDefaultInitialization(bool skipInit)
        {
            _config.SkipDefaultInitialization = skipInit;
            return this;
        }

        /// <summary>
        /// Sets whether to prewarm the pool on initialization
        /// </summary>
        public ValueTypePoolConfigBuilder WithPrewarming(bool prewarm)
        {
            _config.PrewarmOnInit = prewarm;
            return this;
        }

        /// <summary>
        /// Sets whether to use direct memory access for performance optimization
        /// </summary>
        public ValueTypePoolConfigBuilder WithDirectMemoryAccess(bool useDirectAccess)
        {
            _config.UseDirectMemoryAccess = useDirectAccess;
            return this;
        }

        /// <summary>
        /// Sets whether to collect metrics for this pool
        /// </summary>
        public ValueTypePoolConfigBuilder WithMetricsCollection(bool collectMetrics)
        {
            _config.CollectMetrics = collectMetrics;
            return this;
        }

        /// <summary>
        /// Sets whether to log detailed pool operations
        /// </summary>
        public ValueTypePoolConfigBuilder WithDetailedLogging(bool detailedLogging)
        {
            _config.DetailedLogging = detailedLogging;
            return this;
        }

        /// <summary>
        /// Sets whether to log warnings when the pool grows
        /// </summary>
        public ValueTypePoolConfigBuilder WithWarningLogging(bool logWarnings)
        {
            _config.LogWarnings = logWarnings;
            return this;
        }

        /// <summary>
        /// Sets whether to call Reset() on objects when they are released
        /// </summary>
        public ValueTypePoolConfigBuilder WithResetOnRelease(bool resetOnRelease)
        {
            _config.ResetOnRelease = resetOnRelease;
            return this;
        }

        /// <summary>
        /// Sets the threading mode for the pool
        /// </summary>
        public ValueTypePoolConfigBuilder WithThreadingMode(PoolThreadingMode threadingMode)
        {
            _config.ThreadingMode = threadingMode;
            return this;
        }

        /// <summary>
        /// Sets the threshold ratio of used/total items below which the pool will shrink
        /// </summary>
        public ValueTypePoolConfigBuilder WithShrinkThreshold(float threshold)
        {
            _config.ShrinkThreshold = Mathf.Clamp01(threshold);
            return this;
        }

        /// <summary>
        /// Sets the minimum time between auto-shrink operations in seconds
        /// </summary>
        public ValueTypePoolConfigBuilder WithShrinkInterval(float interval)
        {
            _config.ShrinkInterval = Mathf.Max(0f, interval);
            return this;
        }

        /// <summary>
        /// Sets the linear growth increment when expanding the pool
        /// </summary>
        public ValueTypePoolConfigBuilder WithGrowthIncrement(int increment)
        {
            _config.GrowthIncrement = Mathf.Max(1, increment);
            return this;
        }

        /// <summary>
        /// Sets whether to throw an exception when exceeding max count
        /// </summary>
        public ValueTypePoolConfigBuilder WithExceptionOnExceedingMaxCount(bool throwException)
        {
            _config.ThrowIfExceedingMaxCount = throwException;
            return this;
        }

        /// <summary>
        /// Sets the native allocator to use for native collections
        /// </summary>
        public ValueTypePoolConfigBuilder WithNativeAllocator(Allocator allocator)
        {
            _config.NativeAllocator = allocator;
            return this;
        }

        /// <summary>
        /// Sets whether the pool should automatically shrink when it has excess capacity.
        /// </summary>
        /// <param name="enableAutoShrink">True to enable automatic shrinking, false to disable</param>
        /// <returns>The builder instance for method chaining</returns>
        public ValueTypePoolConfigBuilder WithAutoShrink(bool enableAutoShrink)
        {
            _config.EnableAutoShrink = enableAutoShrink;
            return this;
        }

        /// <summary>
        /// Initializes the builder with settings from an existing pool configuration.
        /// Copies all configuration parameters, maintaining the fluent API for further customization.
        /// </summary>
        /// <param name="config">The configuration to initialize from</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public ValueTypePoolConfigBuilder FromExisting(IPoolConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");

            // Copy common IPoolConfig properties
            _config.ConfigId = config.ConfigId;
            _config.InitialCapacity = config.InitialCapacity;
            _config.MinimumCapacity = config.MinimumCapacity;
            _config.MaximumCapacity = config.MaximumCapacity;
            _config.PrewarmOnInit = config.PrewarmOnInit;
            _config.CollectMetrics = config.CollectMetrics;
            _config.DetailedLogging = config.DetailedLogging;
            _config.LogWarnings = config.LogWarnings;
            _config.ResetOnRelease = config.ResetOnRelease;
            _config.ThreadingMode = config.ThreadingMode;
            _config.EnableAutoShrink = config.EnableAutoShrink;
            _config.ShrinkThreshold = config.ShrinkThreshold;
            _config.ShrinkInterval = config.ShrinkInterval;
            _config.NativeAllocator = config.NativeAllocator;
            _config.UseExponentialGrowth = config.UseExponentialGrowth;
            _config.GrowthFactor = config.GrowthFactor;
            _config.GrowthIncrement = config.GrowthIncrement;
            _config.ThrowIfExceedingMaxCount = config.ThrowIfExceedingMaxCount;

            // If source is a ValueTypePoolConfig, also copy value-type specific properties
            if (config is ValueTypePoolConfig valueTypeConfig)
            {
                _config.UseMemoryAlignment = valueTypeConfig.UseMemoryAlignment;
                _config.AlignmentSizeBytes = valueTypeConfig.AlignmentSizeBytes;
                _config.ClearMemoryOnRelease = valueTypeConfig.ClearMemoryOnRelease;
                _config.BlittableTypesOnly = valueTypeConfig.BlittableTypesOnly;
                _config.UseSIMDAlignment = valueTypeConfig.UseSIMDAlignment;
                _config.UseDirectMemoryAccess = valueTypeConfig.UseDirectMemoryAccess;
                _config.UseBurstAllocation = valueTypeConfig.UseBurstAllocation;
                _config.SkipDefaultInitialization = valueTypeConfig.SkipDefaultInitialization;
                _config.StructLayoutType = valueTypeConfig.StructLayoutType;
                _config.UseInlineHandling = valueTypeConfig.UseInlineHandling;
                _config.UseValidationChecks = valueTypeConfig.UseValidationChecks;
                _config.OverflowHandling = valueTypeConfig.OverflowHandling;
                _config.UseAutomaticDisposal = valueTypeConfig.UseAutomaticDisposal;
                _config.DisposeTimeoutSeconds = valueTypeConfig.DisposeTimeoutSeconds;
                _config.IsJobCompatible = valueTypeConfig.IsJobCompatible;

                // Handle potential AutoShrinkEnabled property which exists both as EnableAutoShrink in IPoolConfig
                // and may be present in legacy implementations
                if (valueTypeConfig.GetType().GetProperty("AutoShrinkEnabled") != null)
                {
                    // Try to get the property through reflection
                    var autoShrinkEnabledProperty = _config.GetType().GetProperty("AutoShrinkEnabled");
                    if (autoShrinkEnabledProperty != null)
                    {
                        var value = valueTypeConfig.GetType().GetProperty("AutoShrinkEnabled")
                            .GetValue(valueTypeConfig);
                        autoShrinkEnabledProperty.SetValue(_config, value);
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Specialization of the FromExisting method for ValueTypePoolConfig instances.
        /// Provides a more direct and type-safe way to initialize from another value type pool configuration.
        /// </summary>
        /// <param name="config">The ValueTypePoolConfig to initialize from</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if config is null</exception>
        public ValueTypePoolConfigBuilder FromExisting(ValueTypePoolConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");

            // Use the generic version first for common properties
            FromExisting((IPoolConfig)config);

            // Directly copy all ValueTypePoolConfig specific properties
            _config.UseMemoryAlignment = config.UseMemoryAlignment;
            _config.AlignmentSizeBytes = config.AlignmentSizeBytes;
            _config.ClearMemoryOnRelease = config.ClearMemoryOnRelease;
            _config.BlittableTypesOnly = config.BlittableTypesOnly;
            _config.UseSIMDAlignment = config.UseSIMDAlignment;
            _config.UseDirectMemoryAccess = config.UseDirectMemoryAccess;
            _config.UseBurstAllocation = config.UseBurstAllocation;
            _config.SkipDefaultInitialization = config.SkipDefaultInitialization;
            _config.StructLayoutType = config.StructLayoutType;
            _config.UseInlineHandling = config.UseInlineHandling;
            _config.UseValidationChecks = config.UseValidationChecks;
            _config.OverflowHandling = config.OverflowHandling;
            _config.UseAutomaticDisposal = config.UseAutomaticDisposal;
            _config.DisposeTimeoutSeconds = config.DisposeTimeoutSeconds;
            _config.IsJobCompatible = config.IsJobCompatible;

            // Handle potential AutoShrinkEnabled property which exists both as EnableAutoShrink in IPoolConfig
            // and may be present in legacy implementations
            if (config.GetType().GetProperty("AutoShrinkEnabled") != null)
            {
                // Try to get the property through reflection
                var autoShrinkEnabledProperty = _config.GetType().GetProperty("AutoShrinkEnabled");
                if (autoShrinkEnabledProperty != null)
                {
                    var value = config.GetType().GetProperty("AutoShrinkEnabled").GetValue(config);
                    autoShrinkEnabledProperty.SetValue(_config, value);
                }
            }

            return this;
        }

        /// <summary>
        /// Configures for high-performance settings
        /// </summary>
        public ValueTypePoolConfigBuilder AsHighPerformance()
        {
            _config.UseMemoryAlignment = true;
            _config.AlignmentSizeBytes = 16;
            _config.ClearMemoryOnRelease = false;
            _config.UseSIMDAlignment = true;
            _config.UseDirectMemoryAccess = true;
            _config.SkipDefaultInitialization = true;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            _config.PrewarmOnInit = true;
            _config.DetailedLogging = false;
            _config.CollectMetrics = false;
            _config.EnableAutoShrink = false;
            return this;
        }

        /// <summary>
        /// Configures as Burst-compatible for use with Unity's Burst compiler
        /// </summary>
        public ValueTypePoolConfigBuilder AsBurstCompatible()
        {
            _config.BlittableTypesOnly = true;
            _config.UseBurstAllocation = true;
            _config.UseMemoryAlignment = true;
            _config.AlignmentSizeBytes = 16;
            _config.UseSIMDAlignment = true;
            _config.UseDirectMemoryAccess = true;
            _config.SkipDefaultInitialization = true;
            _config.ThreadingMode = PoolThreadingMode.JobCompatible;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 2.0f;
            _config.DetailedLogging = false;
            _config.CollectMetrics = false;
            return this;
        }

        /// <summary>
        /// Configures for memory-efficient settings
        /// </summary>
        public ValueTypePoolConfigBuilder AsMemoryEfficient()
        {
            _config.InitialCapacity = Math.Max(_config.InitialCapacity, 16);
            _config.MaximumCapacity = _config.InitialCapacity * 2;
            _config.UseExponentialGrowth = false;
            _config.GrowthIncrement = 4;
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.5f;
            _config.ShrinkInterval = 10.0f;
            _config.ClearMemoryOnRelease = true;
            _config.UseMemoryAlignment = false;
            _config.DetailedLogging = false;
            _config.CollectMetrics = false;
            return this;
        }

        /// <summary>
        /// Configures for debugging with extensive tracking
        /// </summary>
        public ValueTypePoolConfigBuilder AsDebug()
        {
            _config.DetailedLogging = true;
            _config.CollectMetrics = true;
            _config.LogWarnings = true;
            _config.ClearMemoryOnRelease = true;
            _config.SkipDefaultInitialization = false;
            _config.UseDirectMemoryAccess = false;
            _config.InitialCapacity = Math.Max(_config.InitialCapacity, 16);
            _config.MaximumCapacity = _config.InitialCapacity * 4;
            return this;
        }

        /// <summary>
        /// Configures for SIMD-optimized operations
        /// </summary>
        public ValueTypePoolConfigBuilder AsSimdOptimized()
        {
            _config.UseMemoryAlignment = true;
            _config.AlignmentSizeBytes = 16;
            _config.UseSIMDAlignment = true;
            _config.BlittableTypesOnly = true;
            _config.SkipDefaultInitialization = true;
            _config.UseDirectMemoryAccess = true;
            _config.UseExponentialGrowth = true;
            _config.PrewarmOnInit = true;
            return this;
        }

        /// <summary>
        /// Configures with balanced settings suitable for most use cases
        /// </summary>
        public ValueTypePoolConfigBuilder AsBalanced()
        {
            _config.InitialCapacity = Math.Max(_config.InitialCapacity, 32);
            _config.MaximumCapacity = _config.InitialCapacity * 4;
            _config.UseMemoryAlignment = true;
            _config.AlignmentSizeBytes = 16;
            _config.ClearMemoryOnRelease = true;
            _config.UseSIMDAlignment = false;
            _config.UseExponentialGrowth = true;
            _config.GrowthFactor = 1.5f;
            _config.EnableAutoShrink = true;
            _config.ShrinkThreshold = 0.4f;
            _config.CollectMetrics = true;
            return this;
        }

        /// <summary>
        /// Builds and validates the configuration
        /// </summary>
        /// <returns>The built configuration</returns>
        /// <exception cref="InvalidOperationException">Thrown if configuration validation fails</exception>
        public ValueTypePoolConfig Build()
        {
            ValidateConfiguration();
            ApplyConstraints();

            var clone = _config.Clone() as ValueTypePoolConfig;
            return clone ?? throw new InvalidOperationException("Failed to clone configuration");
        }

        /// <summary>
        /// Validates the configuration before building
        /// </summary>
        protected virtual void ValidateConfiguration()
        {
            if (_config.InitialCapacity < 0)
                throw new InvalidOperationException("Initial capacity cannot be negative");

            if (_config.MaximumCapacity > 0 && _config.InitialCapacity > _config.MaximumCapacity)
                throw new InvalidOperationException("Initial capacity cannot exceed maximum size");

            if (_config.AlignmentSizeBytes <= 0)
                throw new InvalidOperationException("Alignment size must be positive");

            if (_config.UseMemoryAlignment && (_config.AlignmentSizeBytes & (_config.AlignmentSizeBytes - 1)) != 0)
                throw new InvalidOperationException("Alignment size must be a power of 2");

            if (_config.UseBurstAllocation && !_config.BlittableTypesOnly)
                throw new InvalidOperationException("Burst allocation requires blittable types only");
        }

        /// <summary>
        /// Creates a value type pool builder with preset optimization settings
        /// </summary>
        public ValueTypePoolConfigBuilder ValueType(ValueTypePoolPreset preset, int initialCapacity = 32)
        {
            var builder = new ValueTypePoolConfigBuilder()
                .WithInitialCapacity(initialCapacity);

            switch (preset)
            {
                case ValueTypePoolPreset.HighPerformance:
                    return builder
                        .WithMemoryAlignment(true)
                        .WithAlignmentSize(16)
                        .WithBlittableTypesOnly(true)
                        .WithSimdOptimization(true)
                        .WithSkipDefaultInitialization(true)
                        .WithExponentialGrowth(true);
                case ValueTypePoolPreset.BurstCompatible:
                    return builder.AsBurstCompatible();
                case ValueTypePoolPreset.MemoryEfficient:
                    return builder
                        .WithMemoryAlignment(false)
                        .WithClearMemoryOnRelease(true)
                        .WithAutoShrink(true)
                        .WithExponentialGrowth(false);
                case ValueTypePoolPreset.Default:
                default:
                    return builder;
            }
        }

        /// <summary>
        /// Applies configuration constraints based on settings
        /// </summary>
        private void ApplyConstraints()
        {
            // SIMD optimization constraints
            if (_config.UseSIMDAlignment)
            {
                _config.UseMemoryAlignment = true;
                _config.AlignmentSizeBytes = Math.Max(_config.AlignmentSizeBytes, 16);
                _config.BlittableTypesOnly = true;
            }

            // Burst allocation constraints
            if (_config.UseBurstAllocation)
            {
                _config.BlittableTypesOnly = true;
                if (_config.NativeAllocator == Allocator.Invalid)
                {
                    _config.NativeAllocator = Allocator.Persistent;
                }
            }

            // Memory alignment constraints
            if (_config.UseMemoryAlignment && _config.AlignmentSizeBytes < 1)
            {
                _config.AlignmentSizeBytes = 16;
            }
        }
    }
}