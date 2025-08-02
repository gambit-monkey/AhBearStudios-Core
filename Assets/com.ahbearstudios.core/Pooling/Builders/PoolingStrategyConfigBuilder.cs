using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Models;
using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Pooling.Builders
{
    /// <summary>
    /// Builder for creating pooling strategy configurations.
    /// Provides fluent interface for constructing PoolingStrategyConfig instances.
    /// </summary>
    public class PoolingStrategyConfigBuilder : IPoolingStrategyConfigBuilder
    {
        private string _name;
        private PerformanceBudget _performanceBudget;
        private bool _enableCircuitBreaker;
        private int _circuitBreakerFailureThreshold;
        private TimeSpan _circuitBreakerRecoveryTime;
        private bool _enableHealthMonitoring;
        private TimeSpan _healthCheckInterval;
        private bool _enableDetailedMetrics;
        private int _maxMetricsSamples;
        private bool _enableNetworkOptimizations;
        private bool _enableUnityOptimizations;
        private bool _enableDebugLogging;
        private readonly Dictionary<string, object> _customParameters;
        private readonly HashSet<string> _tags;

        /// <summary>
        /// Initializes a new instance of the PoolingStrategyConfigBuilder.
        /// </summary>
        public PoolingStrategyConfigBuilder()
        {
            _customParameters = new Dictionary<string, object>();
            _tags = new HashSet<string>();
            SetDefaults();
        }

        /// <inheritdoc/>
        public IPoolingStrategyConfigBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        /// <inheritdoc/>
        public IPoolingStrategyConfigBuilder WithPerformanceBudget(PerformanceBudget performanceBudget)
        {
            _performanceBudget = performanceBudget ?? throw new ArgumentNullException(nameof(performanceBudget));
            return this;
        }

        /// <inheritdoc/>
        public IPoolingStrategyConfigBuilder WithCircuitBreaker(bool enabled, int failureThreshold, TimeSpan recoveryTime)
        {
            _enableCircuitBreaker = enabled;
            _circuitBreakerFailureThreshold = failureThreshold;
            _circuitBreakerRecoveryTime = recoveryTime;
            return this;
        }

        /// <inheritdoc/>
        public IPoolingStrategyConfigBuilder WithHealthMonitoring(bool enabled, TimeSpan checkInterval)
        {
            _enableHealthMonitoring = enabled;
            _healthCheckInterval = checkInterval;
            return this;
        }

        /// <inheritdoc/>
        public IPoolingStrategyConfigBuilder WithMetrics(bool enabled, int maxSamples)
        {
            _enableDetailedMetrics = enabled;
            _maxMetricsSamples = maxSamples;
            return this;
        }

        /// <inheritdoc/>
        public IPoolingStrategyConfigBuilder WithNetworkOptimizations(bool enabled)
        {
            _enableNetworkOptimizations = enabled;
            return this;
        }

        /// <inheritdoc/>
        public IPoolingStrategyConfigBuilder WithUnityOptimizations(bool enabled)
        {
            _enableUnityOptimizations = enabled;
            return this;
        }

        /// <inheritdoc/>
        public IPoolingStrategyConfigBuilder WithDebugLogging(bool enabled)
        {
            _enableDebugLogging = enabled;
            return this;
        }

        /// <inheritdoc/>
        public IPoolingStrategyConfigBuilder WithCustomParameter(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
                
            _customParameters[key] = value;
            return this;
        }

        /// <inheritdoc/>
        public IPoolingStrategyConfigBuilder WithTags(params string[] tags)
        {
            foreach (var tag in tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                    _tags.Add(tag);
            }
            return this;
        }

        /// <inheritdoc/>
        public IPoolingStrategyConfigBuilder Default()
        {
            SetDefaults();
            _name = "Default";
            _tags.Add("default");
            _tags.Add("production");
            return this;
        }

        /// <inheritdoc/>
        public IPoolingStrategyConfigBuilder HighPerformance()
        {
            _name = "HighPerformance";
            _performanceBudget = PerformanceBudget.For60FPS();
            _enableCircuitBreaker = true;
            _circuitBreakerFailureThreshold = 3;
            _circuitBreakerRecoveryTime = TimeSpan.FromSeconds(15);
            _enableHealthMonitoring = true;
            _healthCheckInterval = TimeSpan.FromSeconds(15);
            _enableDetailedMetrics = false; // Reduced overhead
            _maxMetricsSamples = 50;
            _enableNetworkOptimizations = true;
            _enableUnityOptimizations = true;
            _enableDebugLogging = false;
            _tags.Clear();
            _tags.Add("high-performance");
            _tags.Add("production");
            _tags.Add("60fps");
            return this;
        }

        /// <inheritdoc/>
        public IPoolingStrategyConfigBuilder MemoryOptimized()
        {
            _name = "MemoryOptimized";
            _performanceBudget = PerformanceBudget.For30FPS();
            _enableCircuitBreaker = true;
            _circuitBreakerFailureThreshold = 3;
            _circuitBreakerRecoveryTime = TimeSpan.FromSeconds(45);
            _enableHealthMonitoring = true;
            _healthCheckInterval = TimeSpan.FromMinutes(1);
            _enableDetailedMetrics = false; // Minimal memory usage
            _maxMetricsSamples = 20;
            _enableNetworkOptimizations = true;
            _enableUnityOptimizations = true;
            _enableDebugLogging = false;
            _tags.Clear();
            _tags.Add("memory-optimized");
            _tags.Add("mobile");
            _tags.Add("low-memory");
            return this;
        }

        /// <inheritdoc/>
        public IPoolingStrategyConfigBuilder Development()
        {
            _name = "Development";
            _performanceBudget = PerformanceBudget.ForDevelopment();
            _enableCircuitBreaker = false; // Disabled for easier debugging
            _circuitBreakerFailureThreshold = 10;
            _circuitBreakerRecoveryTime = TimeSpan.FromMinutes(1);
            _enableHealthMonitoring = true;
            _healthCheckInterval = TimeSpan.FromSeconds(10);
            _enableDetailedMetrics = true;
            _maxMetricsSamples = 500; // More samples for analysis
            _enableNetworkOptimizations = false; // Easier debugging
            _enableUnityOptimizations = false; // Easier debugging
            _enableDebugLogging = true;
            _tags.Clear();
            _tags.Add("development");
            _tags.Add("debug");
            _tags.Add("testing");
            return this;
        }

        /// <inheritdoc/>
        public IPoolingStrategyConfigBuilder NetworkOptimized()
        {
            _name = "NetworkOptimized";
            _performanceBudget = PerformanceBudget.For60FPS();
            _enableCircuitBreaker = true;
            _circuitBreakerFailureThreshold = 2; // More sensitive for network issues
            _circuitBreakerRecoveryTime = TimeSpan.FromSeconds(10);
            _enableHealthMonitoring = true;
            _healthCheckInterval = TimeSpan.FromSeconds(5); // More frequent monitoring
            _enableDetailedMetrics = true;
            _maxMetricsSamples = 200;
            _enableNetworkOptimizations = true;
            _enableUnityOptimizations = true;
            _enableDebugLogging = false;
            _tags.Clear();
            _tags.Add("network-optimized");
            _tags.Add("multiplayer");
            _tags.Add("fishnet");
            _customParameters.Clear();
            _customParameters["NetworkSpikeThreshold"] = 0.8;
            _customParameters["PreemptiveAllocationRatio"] = 0.2;
            _customParameters["LatencyThresholdMs"] = 50.0;
            _customParameters["ThroughputThresholdMbps"] = 10.0;
            return this;
        }

        /// <inheritdoc/>
        public PoolingStrategyConfig Build()
        {
            return new PoolingStrategyConfig
            {
                Name = _name,
                PerformanceBudget = _performanceBudget,
                EnableCircuitBreaker = _enableCircuitBreaker,
                CircuitBreakerFailureThreshold = _circuitBreakerFailureThreshold,
                CircuitBreakerRecoveryTime = _circuitBreakerRecoveryTime,
                EnableHealthMonitoring = _enableHealthMonitoring,
                HealthCheckInterval = _healthCheckInterval,
                EnableDetailedMetrics = _enableDetailedMetrics,
                MaxMetricsSamples = _maxMetricsSamples,
                EnableNetworkOptimizations = _enableNetworkOptimizations,
                EnableUnityOptimizations = _enableUnityOptimizations,
                EnableDebugLogging = _enableDebugLogging,
                CustomParameters = new Dictionary<string, object>(_customParameters),
                Tags = new HashSet<string>(_tags)
            };
        }

        /// <summary>
        /// Sets default values for the builder.
        /// </summary>
        private void SetDefaults()
        {
            _name = "Default";
            _performanceBudget = PerformanceBudget.For60FPS();
            _enableCircuitBreaker = true;
            _circuitBreakerFailureThreshold = 5;
            _circuitBreakerRecoveryTime = TimeSpan.FromSeconds(30);
            _enableHealthMonitoring = true;
            _healthCheckInterval = TimeSpan.FromSeconds(30);
            _enableDetailedMetrics = true;
            _maxMetricsSamples = 100;
            _enableNetworkOptimizations = true;
            _enableUnityOptimizations = true;
            _enableDebugLogging = false;
            _customParameters.Clear();
            _tags.Clear();
        }
    }
}