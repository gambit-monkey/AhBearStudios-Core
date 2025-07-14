using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Adaptive scheduling configuration for Unity game health checks
    /// </summary>
    public sealed record AdaptiveSchedulingConfig : IValidatable
    {
        #region Core Adaptive Settings

        /// <summary>
        /// Whether adaptive scheduling is enabled
        /// </summary>
        public bool Enabled { get; init; } = false;

        /// <summary>
        /// Minimum allowed interval between health check executions
        /// </summary>
        public TimeSpan MinInterval { get; init; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Maximum allowed interval between health check executions
        /// </summary>
        public TimeSpan MaxInterval { get; init; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Learning rate for adaptive algorithms (0.0 to 1.0)
        /// Lower values = more conservative adjustments
        /// </summary>
        [Range(0.0f, 1.0f)]
        public double LearningRate { get; init; } = 0.1;

        /// <summary>
        /// Base adjustment factor for interval changes (0.0 to 1.0)
        /// Determines how aggressively intervals are adjusted
        /// </summary>
        [Range(0.0f, 1.0f)]
        public double AdjustmentFactor { get; init; } = 0.2;

        #endregion

        #region Health Status-Based Adaptation

        /// <summary>
        /// Whether to adjust intervals based on health status changes
        /// </summary>
        public bool HealthBasedAdjustment { get; init; } = true;

        /// <summary>
        /// Number of consecutive healthy results before decreasing frequency
        /// </summary>
        [Range(1, 100)]
        public int HealthyConsecutiveCount { get; init; } = 5;

        /// <summary>
        /// Number of consecutive degraded/unhealthy results before increasing frequency
        /// </summary>
        [Range(1, 100)]
        public int DegradedConsecutiveCount { get; init; } = 2;

        /// <summary>
        /// Multiplier for interval adjustment when health is degraded
        /// Values < 1.0 increase frequency, > 1.0 decrease frequency
        /// </summary>
        [Range(0.1f, 2.0f)]
        public double DegradationAdjustmentMultiplier { get; init; } = 0.5;

        /// <summary>
        /// Multiplier for interval adjustment when health is stable
        /// Values < 1.0 increase frequency, > 1.0 decrease frequency
        /// </summary>
        [Range(0.5f, 5.0f)]
        public double StabilityAdjustmentMultiplier { get; init; } = 1.5;

        /// <summary>
        /// Custom health status weights for adaptive calculations
        /// </summary>
        public Dictionary<HealthStatus, double> HealthStatusWeights { get; init; } = new()
        {
            { HealthStatus.Healthy, 1.0 },
            { HealthStatus.Warning, 0.8 },
            { HealthStatus.Degraded, 0.6 },
            { HealthStatus.Unhealthy, 0.3 },
            { HealthStatus.Critical, 0.1 },
            { HealthStatus.Offline, 0.0 },
            { HealthStatus.Unknown, 0.5 }
        };

        #endregion

        #region Execution Time-Based Adaptation

        /// <summary>
        /// Whether to adjust intervals based on execution time performance
        /// </summary>
        public bool ExecutionTimeBasedAdjustment { get; init; } = true;

        /// <summary>
        /// Target execution time for optimal performance
        /// </summary>
        public TimeSpan TargetExecutionTime { get; init; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Threshold above target time to consider execution as slow
        /// </summary>
        [Range(1.0f, 10.0f)]
        public double SlowExecutionThreshold { get; init; } = 2.0;

        /// <summary>
        /// Threshold below target time to consider execution as fast
        /// </summary>
        [Range(0.1f, 1.0f)]
        public double FastExecutionThreshold { get; init; } = 0.5;

        /// <summary>
        /// Multiplier for interval adjustment when execution is consistently slow
        /// </summary>
        [Range(1.0f, 5.0f)]
        public double SlowExecutionMultiplier { get; init; } = 1.5;

        /// <summary>
        /// Multiplier for interval adjustment when execution is consistently fast
        /// </summary>
        [Range(0.1f, 1.0f)]
        public double FastExecutionMultiplier { get; init; } = 0.8;

        /// <summary>
        /// Number of consecutive slow executions before adjustment
        /// </summary>
        [Range(1, 20)]
        public int SlowExecutionCount { get; init; } = 3;

        /// <summary>
        /// Number of consecutive fast executions before adjustment
        /// </summary>
        [Range(1, 20)]
        public int FastExecutionCount { get; init; } = 5;

        #endregion

        #region Unity Game Performance Adaptation

        /// <summary>
        /// Whether to adjust intervals based on Unity performance metrics
        /// </summary>
        public bool UseUnityPerformanceMetrics { get; init; } = true;

        /// <summary>
        /// Target frame rate for the game
        /// </summary>
        [Range(1, 300)]
        public int TargetFrameRate { get; init; } = 60;

        /// <summary>
        /// Frame rate threshold below which to decrease health check frequency
        /// </summary>
        [Range(1, 300)]
        public int LowFrameRateThreshold { get; init; } = 30;

        /// <summary>
        /// Multiplier for interval adjustment when frame rate is low
        /// </summary>
        [Range(1.0f, 10.0f)]
        public double LowFrameRateMultiplier { get; init; } = 2.0;

        /// <summary>
        /// Whether to adjust based on Unity's Time.deltaTime spikes
        /// </summary>
        public bool AdjustForDeltaTimeSpikes { get; init; } = true;

        /// <summary>
        /// Delta time threshold in seconds to consider as a spike
        /// </summary>
        [Range(0.01f, 1.0f)]
        public double DeltaTimeSpikeThreshold { get; init; } = 0.1;

        /// <summary>
        /// Whether to adjust based on garbage collection pressure
        /// </summary>
        public bool AdjustForGCPressure { get; init; } = true;

        /// <summary>
        /// GC allocation threshold per frame (in KB) to reduce frequency
        /// </summary>
        [Range(1, 10000)]
        public int GCAllocationThreshold { get; init; } = 100;

        #endregion

        #region Game State Adaptation

        /// <summary>
        /// Whether to adjust based on game state (menu, gameplay, loading, etc.)
        /// </summary>
        public bool UseGameStateAdaptation { get; init; } = true;

        /// <summary>
        /// Game state frequency multipliers
        /// </summary>
        public Dictionary<string, double> GameStateMultipliers { get; init; } = new()
        {
            { "Menu", 2.0 },          // Less frequent in menus
            { "Gameplay", 1.0 },      // Normal frequency during gameplay
            { "Loading", 0.5 },       // More frequent during loading
            { "Paused", 5.0 },        // Much less frequent when paused
            { "Cutscene", 3.0 },      // Less frequent during cutscenes
            { "Multiplayer", 0.8 }    // Slightly more frequent in multiplayer
        };

        /// <summary>
        /// Whether to adjust based on player count in multiplayer scenarios
        /// </summary>
        public bool AdjustForPlayerCount { get; init; } = false;

        /// <summary>
        /// Player count threshold above which to increase frequency
        /// </summary>
        [Range(1, 1000)]
        public int HighPlayerCountThreshold { get; init; } = 10;

        /// <summary>
        /// Multiplier for high player count scenarios
        /// </summary>
        [Range(0.1f, 2.0f)]
        public double HighPlayerCountMultiplier { get; init; } = 0.7;

        #endregion

        #region Simple Smoothing and Stabilization

        /// <summary>
        /// Whether to use simple smoothing to prevent oscillation
        /// </summary>
        public bool EnableSmoothing { get; init; } = true;

        /// <summary>
        /// Simple smoothing algorithm to use
        /// </summary>
        public SmoothingAlgorithm SmoothingAlgorithm { get; init; } = SmoothingAlgorithm.SimpleMovingAverage;

        /// <summary>
        /// Window size for moving average calculations
        /// </summary>
        [Range(3, 20)]
        public int SmoothingWindowSize { get; init; } = 5;

        /// <summary>
        /// Minimum change threshold before applying adjustment (prevents tiny adjustments)
        /// </summary>
        [Range(0.0f, 1.0f)]
        public double MinimumChangeThreshold { get; init; } = 0.05;

        /// <summary>
        /// Maximum change per adjustment cycle (prevents dramatic changes)
        /// </summary>
        [Range(0.1f, 2.0f)]
        public double MaximumChangePerCycle { get; init; } = 0.5;

        #endregion

        #region Monitoring and Debugging

        /// <summary>
        /// Whether to enable detailed logging of adaptation decisions
        /// </summary>
        public bool EnableDetailedLogging { get; init; } = false;

        /// <summary>
        /// Whether to track basic adaptation metrics
        /// </summary>
        public bool TrackAdaptationMetrics { get; init; } = true;

        /// <summary>
        /// Whether to enable Unity Profiler integration
        /// </summary>
        public bool EnableUnityProfilerIntegration { get; init; } = true;

        #endregion

        #region Validation

        /// <summary>
        /// Validates the adaptive scheduling configuration
        /// </summary>
        /// <returns>List of validation errors</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            // Basic validation
            if (MinInterval <= TimeSpan.Zero)
                errors.Add("MinInterval must be greater than zero");

            if (MaxInterval <= MinInterval)
                errors.Add("MaxInterval must be greater than MinInterval");

            if (MaxInterval > TimeSpan.FromHours(24))
                errors.Add("MaxInterval should not exceed 24 hours");

            if (AdjustmentFactor <= 0 || AdjustmentFactor > 1)
                errors.Add("AdjustmentFactor must be between 0 and 1");

            if (LearningRate <= 0 || LearningRate > 1)
                errors.Add("LearningRate must be between 0 and 1");

            // Health-based validation
            if (HealthBasedAdjustment)
            {
                if (HealthyConsecutiveCount < 1)
                    errors.Add("HealthyConsecutiveCount must be at least 1");

                if (DegradedConsecutiveCount < 1)
                    errors.Add("DegradedConsecutiveCount must be at least 1");

                if (DegradationAdjustmentMultiplier <= 0)
                    errors.Add("DegradationAdjustmentMultiplier must be greater than zero");

                if (StabilityAdjustmentMultiplier <= 0)
                    errors.Add("StabilityAdjustmentMultiplier must be greater than zero");

                // Validate health status weights
                foreach (var weight in HealthStatusWeights.Values)
                {
                    if (weight < 0.0 || weight > 1.0)
                        errors.Add("Health status weights must be between 0.0 and 1.0");
                }
            }

            // Execution time validation
            if (ExecutionTimeBasedAdjustment)
            {
                if (TargetExecutionTime <= TimeSpan.Zero)
                    errors.Add("TargetExecutionTime must be greater than zero");

                if (SlowExecutionThreshold <= 1.0)
                    errors.Add("SlowExecutionThreshold must be greater than 1.0");

                if (FastExecutionThreshold <= 0.0 || FastExecutionThreshold >= 1.0)
                    errors.Add("FastExecutionThreshold must be between 0.0 and 1.0");

                if (SlowExecutionMultiplier <= 1.0)
                    errors.Add("SlowExecutionMultiplier must be greater than 1.0");

                if (FastExecutionMultiplier <= 0.0 || FastExecutionMultiplier >= 1.0)
                    errors.Add("FastExecutionMultiplier must be between 0.0 and 1.0");
            }

            // Unity performance validation
            if (UseUnityPerformanceMetrics)
            {
                if (TargetFrameRate <= 0)
                    errors.Add("TargetFrameRate must be greater than zero");

                if (LowFrameRateThreshold <= 0)
                    errors.Add("LowFrameRateThreshold must be greater than zero");

                if (LowFrameRateThreshold >= TargetFrameRate)
                    errors.Add("LowFrameRateThreshold must be less than TargetFrameRate");

                if (AdjustForDeltaTimeSpikes && DeltaTimeSpikeThreshold <= 0)
                    errors.Add("DeltaTimeSpikeThreshold must be greater than zero");

                if (AdjustForGCPressure && GCAllocationThreshold <= 0)
                    errors.Add("GCAllocationThreshold must be greater than zero");
            }

            // Game state validation
            if (UseGameStateAdaptation)
            {
                foreach (var multiplier in GameStateMultipliers.Values)
                {
                    if (multiplier <= 0.0)
                        errors.Add("Game state multipliers must be greater than zero");
                }
            }

            // Player count validation
            if (AdjustForPlayerCount)
            {
                if (HighPlayerCountThreshold <= 0)
                    errors.Add("HighPlayerCountThreshold must be greater than zero");

                if (HighPlayerCountMultiplier <= 0.0)
                    errors.Add("HighPlayerCountMultiplier must be greater than zero");
            }

            // Smoothing validation
            if (EnableSmoothing)
            {
                if (SmoothingWindowSize < 3)
                    errors.Add("SmoothingWindowSize must be at least 3");

                if (MinimumChangeThreshold < 0.0)
                    errors.Add("MinimumChangeThreshold must be non-negative");

                if (MaximumChangePerCycle <= 0.0)
                    errors.Add("MaximumChangePerCycle must be greater than zero");
            }

            return errors;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a basic adaptive configuration for simple health-based adjustments
        /// </summary>
        /// <param name="minInterval">Minimum interval</param>
        /// <param name="maxInterval">Maximum interval</param>
        /// <returns>Basic adaptive configuration</returns>
        public static AdaptiveSchedulingConfig CreateBasic(TimeSpan minInterval, TimeSpan maxInterval)
        {
            return new AdaptiveSchedulingConfig
            {
                Enabled = true,
                MinInterval = minInterval,
                MaxInterval = maxInterval,
                HealthBasedAdjustment = true,
                ExecutionTimeBasedAdjustment = false,
                UseUnityPerformanceMetrics = false,
                UseGameStateAdaptation = false,
                EnableSmoothing = true,
                LearningRate = 0.1,
                AdjustmentFactor = 0.2
            };
        }

        /// <summary>
        /// Creates a Unity-optimized configuration for game applications
        /// </summary>
        /// <param name="minInterval">Minimum interval</param>
        /// <param name="maxInterval">Maximum interval</param>
        /// <returns>Unity-optimized configuration</returns>
        public static AdaptiveSchedulingConfig CreateUnityOptimized(TimeSpan minInterval, TimeSpan maxInterval)
        {
            return new AdaptiveSchedulingConfig
            {
                Enabled = true,
                MinInterval = minInterval,
                MaxInterval = maxInterval,
                HealthBasedAdjustment = true,
                ExecutionTimeBasedAdjustment = true,
                UseUnityPerformanceMetrics = true,
                UseGameStateAdaptation = true,
                AdjustForDeltaTimeSpikes = true,
                AdjustForGCPressure = true,
                TargetFrameRate = 60,
                LowFrameRateThreshold = 30,
                EnableSmoothing = true,
                SmoothingAlgorithm = SmoothingAlgorithm.SimpleMovingAverage,
                SmoothingWindowSize = 5,
                TrackAdaptationMetrics = true,
                EnableUnityProfilerIntegration = true,
                LearningRate = 0.15,
                AdjustmentFactor = 0.25
            };
        }

        /// <summary>
        /// Creates a multiplayer-focused configuration
        /// </summary>
        /// <param name="minInterval">Minimum interval</param>
        /// <param name="maxInterval">Maximum interval</param>
        /// <returns>Multiplayer-focused configuration</returns>
        public static AdaptiveSchedulingConfig CreateMultiplayer(TimeSpan minInterval, TimeSpan maxInterval)
        {
            return new AdaptiveSchedulingConfig
            {
                Enabled = true,
                MinInterval = minInterval,
                MaxInterval = maxInterval,
                HealthBasedAdjustment = true,
                ExecutionTimeBasedAdjustment = true,
                UseUnityPerformanceMetrics = true,
                UseGameStateAdaptation = true,
                AdjustForPlayerCount = true,
                HighPlayerCountThreshold = 10,
                HighPlayerCountMultiplier = 0.7,
                GameStateMultipliers = new Dictionary<string, double>
                {
                    { "Menu", 3.0 },
                    { "Gameplay", 1.0 },
                    { "Loading", 0.5 },
                    { "Paused", 5.0 },
                    { "Multiplayer", 0.8 },
                    { "Lobby", 2.0 }
                },
                EnableSmoothing = true,
                TrackAdaptationMetrics = true,
                LearningRate = 0.2,
                AdjustmentFactor = 0.3
            };
        }

        /// <summary>
        /// Creates a conservative configuration with minimal adjustments
        /// </summary>
        /// <param name="minInterval">Minimum interval</param>
        /// <param name="maxInterval">Maximum interval</param>
        /// <returns>Conservative configuration</returns>
        public static AdaptiveSchedulingConfig CreateConservative(TimeSpan minInterval, TimeSpan maxInterval)
        {
            return new AdaptiveSchedulingConfig
            {
                Enabled = true,
                MinInterval = minInterval,
                MaxInterval = maxInterval,
                HealthBasedAdjustment = true,
                ExecutionTimeBasedAdjustment = false,
                UseUnityPerformanceMetrics = false,
                UseGameStateAdaptation = false,
                EnableSmoothing = true,
                LearningRate = 0.05,
                AdjustmentFactor = 0.1,
                HealthyConsecutiveCount = 10,
                DegradedConsecutiveCount = 5,
                DegradationAdjustmentMultiplier = 0.8,
                StabilityAdjustmentMultiplier = 1.2,
                MinimumChangeThreshold = 0.1,
                MaximumChangePerCycle = 0.3
            };
        }

        #endregion
    }

    #region Supporting Enums

    /// <summary>
    /// Simple smoothing algorithms for games
    /// </summary>
    public enum SmoothingAlgorithm
    {
        None,
        SimpleMovingAverage,
        ExponentialMovingAverage
    }

    #endregion
}