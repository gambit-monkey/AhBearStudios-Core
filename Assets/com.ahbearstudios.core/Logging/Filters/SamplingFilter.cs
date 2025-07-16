using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using Random = System.Random;

namespace AhBearStudios.Core.Logging.Filters
{
    /// <summary>
    /// Filter that evaluates log entries based on statistical sampling.
    /// Provides sampling-based filtering to reduce log volume while maintaining representative data.
    /// Uses various sampling strategies including uniform, adaptive, and burst sampling.
    /// </summary>
    public sealed class SamplingFilter : ILogFilter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly FilterStatistics _statistics;
        private readonly Random _random;
        private bool _isEnabled = true;
        private double _samplingRate = 1.0;
        private SamplingStrategy _strategy = SamplingStrategy.Uniform;
        private long _totalSeen = 0;
        private long _totalSampled = 0;

        /// <inheritdoc />
        public FixedString64Bytes Name { get; }

        /// <inheritdoc />
        public bool IsEnabled 
        { 
            get => _isEnabled; 
            set => _isEnabled = value; 
        }

        /// <inheritdoc />
        public int Priority { get; }

        /// <summary>
        /// Gets the sampling rate (0.0 to 1.0).
        /// </summary>
        public double SamplingRate => _samplingRate;

        /// <summary>
        /// Gets the sampling strategy.
        /// </summary>
        public SamplingStrategy Strategy => _strategy;

        /// <summary>
        /// Gets the total number of entries seen by this filter.
        /// </summary>
        public long TotalSeen => _totalSeen;

        /// <summary>
        /// Gets the total number of entries sampled by this filter.
        /// </summary>
        public long TotalSampled => _totalSampled;

        /// <summary>
        /// Gets the actual sampling rate achieved.
        /// </summary>
        public double ActualSamplingRate => _totalSeen > 0 ? (double)_totalSampled / _totalSeen : 0.0;

        /// <summary>
        /// Initializes a new instance of the SamplingFilter class.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="samplingRate">The sampling rate (0.0 to 1.0)</param>
        /// <param name="strategy">The sampling strategy</param>
        /// <param name="priority">The filter priority (default: 100)</param>
        public SamplingFilter(
            string name = "SamplingFilter",
            double samplingRate = 1.0,
            SamplingStrategy strategy = SamplingStrategy.Uniform,
            int priority = 100)
        {
            Name = name ?? "SamplingFilter";
            Priority = priority;
            
            _samplingRate = Math.Max(0.0, Math.Min(1.0, samplingRate));
            _strategy = strategy;
            _statistics = new FilterStatistics();
            _random = new Random();
            
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["SamplingRate"] = _samplingRate,
                ["Strategy"] = _strategy.ToString(),
                ["Priority"] = priority,
                ["IsEnabled"] = true
            };
        }

        /// <inheritdoc />
        public bool ShouldProcess(LogEntry entry, FixedString64Bytes correlationId = default)
        {
            if (!_isEnabled)
            {
                _statistics.RecordAllowed();
                return true;
            }

            // If sampling rate is 1.0, always allow
            if (_samplingRate >= 1.0)
            {
                _statistics.RecordAllowed();
                return true;
            }

            // If sampling rate is 0.0, never allow
            if (_samplingRate <= 0.0)
            {
                _statistics.RecordBlocked();
                return false;
            }

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                Interlocked.Increment(ref _totalSeen);
                
                bool shouldProcess = false;
                
                switch (_strategy)
                {
                    case SamplingStrategy.Uniform:
                        shouldProcess = UniformSampling();
                        break;
                        
                    case SamplingStrategy.Systematic:
                        shouldProcess = SystematicSampling();
                        break;
                        
                    case SamplingStrategy.LevelWeighted:
                        shouldProcess = LevelWeightedSampling(entry.Level);
                        break;
                        
                    case SamplingStrategy.Adaptive:
                        shouldProcess = AdaptiveSampling();
                        break;
                        
                    default:
                        shouldProcess = UniformSampling();
                        break;
                }
                
                if (shouldProcess)
                {
                    Interlocked.Increment(ref _totalSampled);
                }
                
                stopwatch.Stop();
                
                if (shouldProcess)
                {
                    _statistics.RecordAllowed(stopwatch.Elapsed);
                }
                else
                {
                    _statistics.RecordBlocked(stopwatch.Elapsed);
                }
                
                return shouldProcess;
            }
            catch (Exception)
            {
                stopwatch.Stop();
                _statistics.RecordError(stopwatch.Elapsed);
                
                // On error, allow the entry to pass through to prevent log loss
                return true;
            }
        }

        /// <inheritdoc />
        public ValidationResult Validate(FixedString64Bytes correlationId = default)
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();

            if (string.IsNullOrEmpty(Name.ToString()))
            {
                errors.Add(new ValidationError("Filter name cannot be empty", nameof(Name)));
            }

            if (_samplingRate < 0.0 || _samplingRate > 1.0)
            {
                errors.Add(new ValidationError("Sampling rate must be between 0.0 and 1.0", nameof(SamplingRate)));
            }

            if (_samplingRate < 1.0)
            {
                warnings.Add(new ValidationWarning($"Sampling rate {_samplingRate:P1} will drop some messages", nameof(SamplingRate)));
            }

            if (_samplingRate < 0.1)
            {
                warnings.Add(new ValidationWarning("Very low sampling rate may cause significant data loss", nameof(SamplingRate)));
            }

            return errors.Count > 0 
                ? ValidationResult.Failure(errors, Name.ToString(), warnings)
                : ValidationResult.Success(Name.ToString(), warnings);
        }

        /// <inheritdoc />
        public FilterStatistics GetStatistics()
        {
            return _statistics.CreateSnapshot();
        }

        /// <inheritdoc />
        public void Reset(FixedString64Bytes correlationId = default)
        {
            _statistics.Reset();
            Interlocked.Exchange(ref _totalSeen, 0);
            Interlocked.Exchange(ref _totalSampled, 0);
        }

        /// <inheritdoc />
        public void Configure(IReadOnlyDictionary<FixedString32Bytes, object> settings, FixedString64Bytes correlationId = default)
        {
            if (settings == null) return;

            foreach (var setting in settings)
            {
                _settings[setting.Key] = setting.Value;
                
                // Apply settings to internal state
                switch (setting.Key.ToString())
                {
                    case "SamplingRate":
                        if (setting.Value is double samplingRate)
                            _samplingRate = Math.Max(0.0, Math.Min(1.0, samplingRate));
                        else if (double.TryParse(setting.Value?.ToString(), out var parsedRate))
                            _samplingRate = Math.Max(0.0, Math.Min(1.0, parsedRate));
                        break;
                        
                    case "Strategy":
                        if (setting.Value is SamplingStrategy strategy)
                            _strategy = strategy;
                        else if (Enum.TryParse<SamplingStrategy>(setting.Value?.ToString(), out var parsedStrategy))
                            _strategy = parsedStrategy;
                        break;
                        
                    case "IsEnabled":
                        if (setting.Value is bool isEnabled)
                            _isEnabled = isEnabled;
                        else if (bool.TryParse(setting.Value?.ToString(), out var parsedEnabled))
                            _isEnabled = parsedEnabled;
                        break;
                }
            }
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<FixedString32Bytes, object> GetSettings()
        {
            // Update current values in settings
            _settings["SamplingRate"] = _samplingRate;
            _settings["Strategy"] = _strategy.ToString();
            _settings["IsEnabled"] = _isEnabled;
            
            return _settings;
        }

        /// <summary>
        /// Performs uniform random sampling.
        /// </summary>
        /// <returns>True if the entry should be sampled</returns>
        private bool UniformSampling()
        {
            lock (_random)
            {
                return _random.NextDouble() < _samplingRate;
            }
        }

        /// <summary>
        /// Performs systematic sampling (every Nth entry).
        /// </summary>
        /// <returns>True if the entry should be sampled</returns>
        private bool SystematicSampling()
        {
            if (_samplingRate <= 0.0) return false;
            if (_samplingRate >= 1.0) return true;
            
            var interval = (long)(1.0 / _samplingRate);
            return (_totalSeen % interval) == 0;
        }

        /// <summary>
        /// Performs level-weighted sampling (higher priority for more severe levels).
        /// </summary>
        /// <param name="level">The log level</param>
        /// <returns>True if the entry should be sampled</returns>
        private bool LevelWeightedSampling(LogLevel level)
        {
            // Calculate weight based on log level
            var weight = level switch
            {
                LogLevel.Critical => 1.0,
                LogLevel.Error => 0.9,
                LogLevel.Warning => 0.7,
                LogLevel.Info => 0.5,
                LogLevel.Debug => 0.3,
                LogLevel.Trace => 0.1,
                _ => 0.5
            };
            
            var adjustedRate = _samplingRate * weight;
            
            lock (_random)
            {
                return _random.NextDouble() < adjustedRate;
            }
        }

        /// <summary>
        /// Performs adaptive sampling based on current sampling rate performance.
        /// </summary>
        /// <returns>True if the entry should be sampled</returns>
        private bool AdaptiveSampling()
        {
            // If we're sampling too much, reduce the rate slightly
            // If we're sampling too little, increase the rate slightly
            var actualRate = ActualSamplingRate;
            var adaptiveRate = _samplingRate;
            
            if (actualRate > _samplingRate * 1.1)
            {
                adaptiveRate = _samplingRate * 0.95;
            }
            else if (actualRate < _samplingRate * 0.9)
            {
                adaptiveRate = _samplingRate * 1.05;
            }
            
            adaptiveRate = Math.Max(0.0, Math.Min(1.0, adaptiveRate));
            
            lock (_random)
            {
                return _random.NextDouble() < adaptiveRate;
            }
        }

        /// <summary>
        /// Creates a SamplingFilter configured for uniform sampling.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="samplingRate">The sampling rate (0.0 to 1.0)</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured SamplingFilter instance</returns>
        public static SamplingFilter Uniform(string name, double samplingRate, int priority = 100)
        {
            return new SamplingFilter(name, samplingRate, SamplingStrategy.Uniform, priority);
        }

        /// <summary>
        /// Creates a SamplingFilter configured for systematic sampling.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="samplingRate">The sampling rate (0.0 to 1.0)</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured SamplingFilter instance</returns>
        public static SamplingFilter Systematic(string name, double samplingRate, int priority = 100)
        {
            return new SamplingFilter(name, samplingRate, SamplingStrategy.Systematic, priority);
        }

        /// <summary>
        /// Creates a SamplingFilter configured for level-weighted sampling.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="samplingRate">The sampling rate (0.0 to 1.0)</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured SamplingFilter instance</returns>
        public static SamplingFilter LevelWeighted(string name, double samplingRate, int priority = 100)
        {
            return new SamplingFilter(name, samplingRate, SamplingStrategy.LevelWeighted, priority);
        }

        /// <summary>
        /// Creates a SamplingFilter configured for adaptive sampling.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="samplingRate">The sampling rate (0.0 to 1.0)</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured SamplingFilter instance</returns>
        public static SamplingFilter Adaptive(string name, double samplingRate, int priority = 100)
        {
            return new SamplingFilter(name, samplingRate, SamplingStrategy.Adaptive, priority);
        }

        /// <summary>
        /// Creates a SamplingFilter configured to sample 10% of entries.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured SamplingFilter instance</returns>
        public static SamplingFilter TenPercent(string name = "TenPercentSampling", int priority = 100)
        {
            return new SamplingFilter(name, 0.1, SamplingStrategy.Uniform, priority);
        }

        /// <summary>
        /// Creates a SamplingFilter configured to sample 1% of entries.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured SamplingFilter instance</returns>
        public static SamplingFilter OnePercent(string name = "OnePercentSampling", int priority = 100)
        {
            return new SamplingFilter(name, 0.01, SamplingStrategy.Uniform, priority);
        }
    }

    /// <summary>
    /// Defines the sampling strategy for the SamplingFilter.
    /// </summary>
    public enum SamplingStrategy
    {
        /// <summary>
        /// Uniform random sampling - each entry has an equal probability of being sampled.
        /// </summary>
        Uniform,

        /// <summary>
        /// Systematic sampling - samples every Nth entry based on the sampling rate.
        /// </summary>
        Systematic,

        /// <summary>
        /// Level-weighted sampling - higher priority for more severe log levels.
        /// </summary>
        LevelWeighted,

        /// <summary>
        /// Adaptive sampling - adjusts sampling rate based on current performance.
        /// </summary>
        Adaptive
    }
}