using System;
using AhBearStudios.Pooling.Builders;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;
using AhBearStudios.Pooling.Services;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Pooling.Configurations
{
    /// <summary>
    /// Configuration for semaphore pools that provide thread synchronization capabilities
    /// along with standard pool features like auto-scaling, metrics collection, and lifecycle management.
    /// Fully compatible with Unity Collections v2, Burst, and Unity Jobs system.
    /// Uses composition rather than inheritance for better modularity.
    /// </summary>
    [Serializable]
    public sealed class SemaphorePoolConfig : IPoolConfig, IDisposable
    {
        #region IPoolConfig Implementation
        
        /// <summary>
        /// Gets or sets the unique identifier for this configuration.
        /// </summary>
        public string ConfigId { get; set; } = Guid.NewGuid().ToString("N");
        
        /// <summary>
        /// Gets or sets the initial capacity of the pool.
        /// </summary>
        public int InitialCapacity { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets the minimum capacity the pool should maintain.
        /// </summary>
        public int MinimumCapacity { get; set; } = 5;
        
        /// <summary>
        /// Gets or sets the maximum size of the pool. Set to 0 for unlimited.
        /// </summary>
        public int MaximumCapacity { get; set; } = 100;
        
        /// <summary>
        /// Gets or sets whether to prewarm the pool on initialization.
        /// </summary>
        public bool PrewarmOnInit { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to collect metrics for this pool.
        /// </summary>
        public bool CollectMetrics { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to log detailed pool operations.
        /// </summary>
        public bool DetailedLogging { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to log warnings when the pool grows.
        /// </summary>
        public bool LogWarnings { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to call Reset() on objects when they are released.
        /// </summary>
        public bool ResetOnRelease { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the threading mode for the pool. For semaphore pools, this defaults to ThreadSafe.
        /// </summary>
        public PoolThreadingMode ThreadingMode { get; set; } = PoolThreadingMode.ThreadSafe;
        
        /// <summary>
        /// Gets or sets whether to automatically shrink the pool when usage drops.
        /// </summary>
        public bool EnableAutoShrink { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the threshold ratio of used/total items below which the pool will shrink.
        /// </summary>
        public float ShrinkThreshold { get; set; } = 0.25f;
        
        /// <summary>
        /// Gets or sets the minimum time between auto-shrink operations in seconds.
        /// </summary>
        public float ShrinkInterval { get; set; } = 60f;
        
        /// <summary>
        /// Gets or sets the allocator to use for native containers.
        /// Uses Unity.Collections v2 Allocator enum.
        /// </summary>
        public Allocator NativeAllocator { get; set; } = Allocator.Persistent;
        
        /// <summary>
        /// Gets or sets whether to use exponential growth when expanding the pool.
        /// </summary>
        public bool UseExponentialGrowth { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the growth factor when expanding the pool (for exponential growth).
        /// </summary>
        public float GrowthFactor { get; set; } = 2.0f;
        
        /// <summary>
        /// Gets or sets the linear growth increment when expanding the pool (for linear growth).
        /// </summary>
        public int GrowthIncrement { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets whether to throw an exception when attempting to get an object 
        /// that would exceed the maximum pool size.
        /// </summary>
        public bool ThrowIfExceedingMaxCount { get; set; } = false;
        
        #endregion
        
        #region Semaphore Pool Specific Properties
        
        /// <summary>
        /// Gets or sets the initial count of available semaphore resources.
        /// </summary>
        public int InitialCount { get; set; } = 1;
        
        /// <summary>
        /// Gets or sets the maximum number of concurrent wait operations allowed.
        /// </summary>
        public int MaxConcurrentWaits { get; set; } = 100;
        
        /// <summary>
        /// Gets or sets whether to track which threads hold semaphore resources.
        /// </summary>
        public bool TrackOwnership { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the default timeout in milliseconds for wait operations (0 = no timeout).
        /// </summary>
        public int DefaultTimeoutMs { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets whether to allow a thread to request multiple semaphore resources.
        /// </summary>
        public bool AllowRecursiveAcquisition { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to force release on thread abort/exit.
        /// </summary>
        public bool AutoReleaseOnThreadExit { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to validate semaphore counts on operations.
        /// </summary>
        public bool ValidateCounts { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to track timing metrics for semaphore operations.
        /// </summary>
        public bool TrackOperationTimings { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to report detailed contention metrics.
        /// </summary>
        public bool TrackContentionMetrics { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to support priority-based waiting.
        /// </summary>
        public bool EnablePriorityWaiting { get; set; } = false;
        
        #endregion
        
        #region Private Fields
        
        private readonly IPoolLogger _logger;
        private readonly IPoolingServiceLocator _serviceLocator;
        private bool _isDisposed;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Creates a new instance of the semaphore pool configuration.
        /// </summary>
        public SemaphorePoolConfig()
        {
            _logger = null;
            _serviceLocator = null;
        }
        
        /// <summary>
        /// Creates a new instance of the semaphore pool configuration with dependency injection.
        /// </summary>
        /// <param name="serviceLocator">Service locator for pool services</param>
        public SemaphorePoolConfig(IPoolingServiceLocator serviceLocator = null)
        {
            _serviceLocator = serviceLocator ?? DefaultPoolingServices.Instance;
            _logger = _serviceLocator.GetService<IPoolLogger>();
            
            // Initialize with default GUID in efficient format
            ConfigId = Guid.NewGuid().ToString("N");
        }
        
        /// <summary>
        /// Creates a new semaphore pool configuration with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="serviceLocator">Service locator for pool services</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is negative</exception>
        public SemaphorePoolConfig(int initialCapacity, IPoolingServiceLocator serviceLocator = null) 
            : this(serviceLocator)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative");
                
            InitialCapacity = initialCapacity;
            MinimumCapacity = Math.Max(1, initialCapacity / 2);
        }
        
        /// <summary>
        /// Creates a new semaphore pool configuration with the specified initial and maximum capacities.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool</param>
        /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
        /// <param name="serviceLocator">Service locator for pool services</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity values are invalid</exception>
        public SemaphorePoolConfig(int initialCapacity, int maxSize, IPoolingServiceLocator serviceLocator = null) 
            : this(initialCapacity, serviceLocator)
        {
            if (maxSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Maximum size cannot be negative");
                
            if (maxSize > 0 && maxSize < initialCapacity)
                throw new ArgumentOutOfRangeException(nameof(maxSize), 
                    "Maximum size must be greater than or equal to initial capacity, or 0 for unlimited");
                
            MaximumCapacity = maxSize;
        }
        
        /// <summary>
        /// Creates a new semaphore pool configuration with the specified ID.
        /// </summary>
        /// <param name="configId">Unique identifier for this configuration</param>
        /// <param name="serviceLocator">Service locator for pool services</param>
        /// <exception cref="ArgumentException">Thrown if configId is null or empty</exception>
        public SemaphorePoolConfig(string configId, IPoolingServiceLocator serviceLocator = null) 
            : this(serviceLocator)
        {
            if (string.IsNullOrEmpty(configId))
                throw new ArgumentException("Config ID cannot be null or empty", nameof(configId));
                
            ConfigId = configId;
        }
        
        /// <summary>
        /// Creates a new semaphore pool configuration by copying settings from another configuration.
        /// </summary>
        /// <param name="source">Source configuration to copy from</param>
        /// <param name="serviceLocator">Service locator for pool services</param>
        /// <exception cref="ArgumentNullException">Thrown if source is null</exception>
        public SemaphorePoolConfig(IPoolConfig source, IPoolingServiceLocator serviceLocator = null) 
            : this(serviceLocator)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "Source configuration cannot be null");
                
            ConfigId = source.ConfigId;
            InitialCapacity = source.InitialCapacity;
            MinimumCapacity = source.MinimumCapacity; 
            MaximumCapacity = source.MaximumCapacity;
            PrewarmOnInit = source.PrewarmOnInit;
            CollectMetrics = source.CollectMetrics;
            DetailedLogging = source.DetailedLogging;
            LogWarnings = source.LogWarnings;
            ResetOnRelease = source.ResetOnRelease;
            ThreadingMode = source.ThreadingMode;
            EnableAutoShrink = source.EnableAutoShrink;
            ShrinkThreshold = source.ShrinkThreshold;
            ShrinkInterval = source.ShrinkInterval;
            NativeAllocator = source.NativeAllocator;
            UseExponentialGrowth = source.UseExponentialGrowth;
            GrowthFactor = source.GrowthFactor;
            GrowthIncrement = source.GrowthIncrement;
            ThrowIfExceedingMaxCount = source.ThrowIfExceedingMaxCount;
            
            // Copy semaphore specific properties if source is also a SemaphorePoolConfig
            if (source is SemaphorePoolConfig semaphoreConfig)
            {
                CopySemaphoreSpecificProperties(semaphoreConfig);
            }
            
            // Force thread-safe mode for semaphore pools regardless of source setting
            ThreadingMode = PoolThreadingMode.ThreadSafe;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Copies semaphore specific properties from another semaphore pool configuration.
        /// </summary>
        /// <param name="semaphoreConfig">Source semaphore configuration to copy from</param>
        /// <exception cref="ArgumentNullException">Thrown if semaphoreConfig is null</exception>
        public void CopySemaphoreSpecificProperties(SemaphorePoolConfig semaphoreConfig)
        {
            if (semaphoreConfig == null)
                throw new ArgumentNullException(nameof(semaphoreConfig), "Source semaphore config cannot be null");
                
            InitialCount = semaphoreConfig.InitialCount;
            MaxConcurrentWaits = semaphoreConfig.MaxConcurrentWaits;
            TrackOwnership = semaphoreConfig.TrackOwnership;
            DefaultTimeoutMs = semaphoreConfig.DefaultTimeoutMs;
            AllowRecursiveAcquisition = semaphoreConfig.AllowRecursiveAcquisition;
            AutoReleaseOnThreadExit = semaphoreConfig.AutoReleaseOnThreadExit;
            ValidateCounts = semaphoreConfig.ValidateCounts;
            TrackOperationTimings = semaphoreConfig.TrackOperationTimings;
            TrackContentionMetrics = semaphoreConfig.TrackContentionMetrics;
            EnablePriorityWaiting = semaphoreConfig.EnablePriorityWaiting;
        }
        
        /// <summary>
        /// Creates a deep clone of this configuration.
        /// </summary>
        /// <returns>A new instance of SemaphorePoolConfig with the same settings</returns>
        public IPoolConfig Clone()
        {
            var clone = new SemaphorePoolConfig(_serviceLocator)
            {
                // Clone base IPoolConfig properties
                ConfigId = ConfigId,
                InitialCapacity = InitialCapacity,
                MinimumCapacity = MinimumCapacity,
                MaximumCapacity = MaximumCapacity,
                PrewarmOnInit = PrewarmOnInit,
                CollectMetrics = CollectMetrics,
                DetailedLogging = DetailedLogging,
                LogWarnings = LogWarnings,
                ResetOnRelease = ResetOnRelease,
                ThreadingMode = ThreadingMode,
                EnableAutoShrink = EnableAutoShrink,
                ShrinkThreshold = ShrinkThreshold,
                ShrinkInterval = ShrinkInterval,
                NativeAllocator = NativeAllocator,
                UseExponentialGrowth = UseExponentialGrowth,
                GrowthFactor = GrowthFactor,
                GrowthIncrement = GrowthIncrement,
                ThrowIfExceedingMaxCount = ThrowIfExceedingMaxCount
            };
            
            // Clone semaphore specific properties
            clone.CopySemaphoreSpecificProperties(this);
            
            return clone;
        }
        
        /// <summary>
        /// Converts this configuration to a builder for further modification.
        /// </summary>
        /// <returns>A builder initialized with this configuration's values.</returns>
        public SemaphorePoolConfigBuilder ToBuilder()
        {
            return new SemaphorePoolConfigBuilder().FromExisting(this);
        }
        
        /// <summary>
        /// Registers this configuration with the specified registry.
        /// </summary>
        /// <param name="registry">The registry to register with</param>
        /// <param name="configId">Optional config ID to register with (defaults to this config's ID)</param>
        /// <returns>This configuration instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if registry is null</exception>
        /// <exception cref="ArgumentException">Thrown if configId is null or empty when specified</exception>
        public SemaphorePoolConfig Register(IPoolConfigRegistry registry, string configId = null)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry), "Registry cannot be null");
                
            var id = configId ?? ConfigId;
            
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Config ID cannot be null or empty", nameof(configId));
                
            registry.RegisterConfig(id, this);
            return this;
        }
        
        /// <summary>
        /// Validates the configuration settings.
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise</returns>
        public bool Validate()
        {
            // Basic validation
            if (InitialCapacity < 0)
            {
                LogWarning($"Invalid initial capacity: {InitialCapacity}. Must be >= 0.");
                return false;
            }
            
            if (MaximumCapacity < 0)
            {
                LogWarning($"Invalid maximum capacity: {MaximumCapacity}. Must be >= 0.");
                return false;
            }
            
            if (MaximumCapacity > 0 && InitialCapacity > MaximumCapacity)
            {
                LogWarning($"Initial capacity ({InitialCapacity}) exceeds maximum capacity ({MaximumCapacity}).");
                return false;
            }
            
            if (ShrinkThreshold <= 0 || ShrinkThreshold >= 1)
            {
                LogWarning($"Invalid shrink threshold: {ShrinkThreshold}. Must be between 0 and 1.");
                return false;
            }
            
            if (ShrinkInterval <= 0)
            {
                LogWarning($"Invalid shrink interval: {ShrinkInterval}. Must be > 0.");
                return false;
            }
            
            if (GrowthFactor <= 1 && UseExponentialGrowth)
            {
                LogWarning($"Invalid growth factor: {GrowthFactor}. Must be > 1 for exponential growth.");
                return false;
            }
            
            if (GrowthIncrement <= 0 && !UseExponentialGrowth)
            {
                LogWarning($"Invalid growth increment: {GrowthIncrement}. Must be > 0 for linear growth.");
                return false;
            }
            
            // Semaphore-specific validation
            if (InitialCount < 0)
            {
                LogWarning($"Invalid initial count: {InitialCount}. Must be >= 0.");
                return false;
            }
            
            if (MaxConcurrentWaits <= 0)
            {
                LogWarning($"Invalid max concurrent waits: {MaxConcurrentWaits}. Must be > 0.");
                return false;
            }
            
            if (DefaultTimeoutMs < 0)
            {
                LogWarning($"Invalid default timeout: {DefaultTimeoutMs}. Must be >= 0.");
                return false;
            }
            
            // Validate that we're using thread-safe mode
            if (ThreadingMode != PoolThreadingMode.ThreadSafe)
            {
                // Auto-correct instead of failing
                ThreadingMode = PoolThreadingMode.ThreadSafe;
                LogWarning("ThreadingMode changed to ThreadSafe for semaphore pool");
            }
            
            return true;
        }
        
        /// <summary>
        /// Logs a warning message if warning logging is enabled.
        /// </summary>
        /// <param name="message">Warning message to log</param>
        public void LogWarning(string message)
        {
            if (!LogWarnings)
                return;
                
            if (_logger != null)
            {
                _logger.LogWarningInstance($"[SemaphorePoolConfig] {message}");
            }
            else
            {
                Debug.LogWarning($"[SemaphorePoolConfig] {message}");
            }
        }
        
        /// <summary>
        /// Disposes any unmanaged resources held by this configuration.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            // Release any native resources
            
            _isDisposed = true;
        }
        
        #endregion
    }
}