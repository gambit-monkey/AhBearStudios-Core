using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.HealthChecking.Models;
using Random = System.Random;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Advanced scheduling configuration for health checks with support for cron expressions,
    /// time windows, timezone handling, and complex scheduling patterns
    /// </summary>
    public sealed record ScheduleConfig : IValidatable
    {
        #region Core Scheduling Settings

        /// <summary>
        /// Whether advanced scheduling is enabled (overrides simple interval-based scheduling)
        /// </summary>
        public bool Enabled { get; init; } = false;

        /// <summary>
        /// Primary scheduling strategy to use
        /// </summary>
        public ScheduleStrategy Strategy { get; init; } = ScheduleStrategy.Interval;

        /// <summary>
        /// Cron expression for cron-based scheduling (e.g., "0 */15 * * * *" for every 15 minutes)
        /// </summary>
        public string CronExpression { get; init; } = string.Empty;

        /// <summary>
        /// Timezone for schedule calculations (defaults to UTC)
        /// </summary>
        public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Utc;

        /// <summary>
        /// Base interval for interval-based scheduling strategies
        /// </summary>
        public TimeSpan BaseInterval { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to enable schedule jitter to avoid thundering herd problems
        /// </summary>
        public bool EnableJitter { get; init; } = true;

        /// <summary>
        /// Maximum jitter variance as percentage of base interval (0.0 to 1.0)
        /// </summary>
        [Range(0.0f, 1.0f)]
        public double JitterVariance { get; init; } = 0.1;

        #endregion

        #region Execution Windows

        /// <summary>
        /// Whether to restrict execution to specific time windows
        /// </summary>
        public bool EnableExecutionWindows { get; init; } = false;

        /// <summary>
        /// Allowed execution time windows
        /// </summary>
        public List<TimeWindow> ExecutionWindows { get; init; } = new();

        /// <summary>
        /// Action to take when execution is attempted outside allowed windows
        /// </summary>
        public OutOfWindowAction OutOfWindowAction { get; init; } = OutOfWindowAction.Skip;

        /// <summary>
        /// Whether to queue executions that fall outside windows for later execution
        /// </summary>
        public bool QueueOutOfWindowExecutions { get; init; } = false;

        /// <summary>
        /// Maximum time to queue out-of-window executions
        /// </summary>
        public TimeSpan MaxQueueTime { get; init; } = TimeSpan.FromHours(1);

        #endregion

        #region Blackout Periods

        /// <summary>
        /// Whether to enable blackout periods where no execution occurs
        /// </summary>
        public bool EnableBlackoutPeriods { get; init; } = false;

        /// <summary>
        /// Blackout periods where health checks should not execute
        /// </summary>
        public List<BlackoutPeriod> BlackoutPeriods { get; init; } = new();

        /// <summary>
        /// Action to take when execution is attempted during blackout periods
        /// </summary>
        public BlackoutAction BlackoutAction { get; init; } = BlackoutAction.Skip;

        /// <summary>
        /// Whether to automatically reschedule after blackout periods end
        /// </summary>
        public bool AutoRescheduleAfterBlackout { get; init; } = true;

        #endregion

        #region Adaptive Scheduling

        /// <summary>
        /// Whether to enable adaptive scheduling based on health check results
        /// </summary>
        public bool EnableAdaptiveScheduling { get; init; } = false;

        /// <summary>
        /// Adaptive scheduling configuration
        /// </summary>
        public AdaptiveSchedulingConfig AdaptiveConfig { get; init; } = new();

        /// <summary>
        /// Whether to increase frequency when health status is degraded
        /// </summary>
        public bool IncreaseFrequencyOnDegradation { get; init; } = true;

        /// <summary>
        /// Multiplier for frequency increase during degradation (e.g., 0.5 = 2x frequency)
        /// </summary>
        [Range(0.1f, 1.0f)]
        public double DegradationFrequencyMultiplier { get; init; } = 0.5;

        /// <summary>
        /// Whether to decrease frequency when health status is consistently healthy
        /// </summary>
        public bool DecreaseFrequencyOnStability { get; init; } = true;

        /// <summary>
        /// Multiplier for frequency decrease during stability (e.g., 2.0 = 0.5x frequency)
        /// </summary>
        [Range(1.0f, 10.0f)]
        public double StabilityFrequencyMultiplier { get; init; } = 2.0;

        #endregion

        #region Dependency-Based Scheduling

        /// <summary>
        /// Whether to enable dependency-based scheduling
        /// </summary>
        public bool EnableDependencyScheduling { get; init; } = false;

        /// <summary>
        /// Whether to schedule this check after its dependencies complete
        /// </summary>
        public bool ScheduleAfterDependencies { get; init; } = false;

        /// <summary>
        /// Maximum delay to wait for dependencies before proceeding
        /// </summary>
        public TimeSpan MaxDependencyWaitTime { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to skip execution if dependencies are unhealthy
        /// </summary>
        public bool SkipOnUnhealthyDependencies { get; init; } = false;

        /// <summary>
        /// Whether to cascade schedule changes to dependent health checks
        /// </summary>
        public bool CascadeScheduleChanges { get; init; } = false;

        #endregion

        #region Load Balancing and Distribution

        /// <summary>
        /// Whether to enable load balancing across multiple instances
        /// </summary>
        public bool EnableLoadBalancing { get; init; } = false;

        /// <summary>
        /// Load balancing strategy for distributed environments
        /// </summary>
        public LoadBalancingStrategy LoadBalancingStrategy { get; init; } = LoadBalancingStrategy.RoundRobin;

        /// <summary>
        /// Instance identifier for load balancing
        /// </summary>
        public string InstanceId { get; init; } = Environment.MachineName;

        /// <summary>
        /// Whether to use consistent hashing for schedule distribution
        /// </summary>
        public bool UseConsistentHashing { get; init; } = true;

        /// <summary>
        /// Weight for this instance in load balancing (1.0 = normal weight)
        /// </summary>
        [Range(0.1f, 10.0f)]
        public double InstanceWeight { get; init; } = 1.0;

        #endregion

        #region Retry and Recovery

        /// <summary>
        /// Whether to enable schedule recovery after failures
        /// </summary>
        public bool EnableScheduleRecovery { get; init; } = true;

        /// <summary>
        /// Maximum number of missed executions before considering schedule broken
        /// </summary>
        [Range(1, 100)]
        public int MaxMissedExecutions { get; init; } = 3;

        /// <summary>
        /// Action to take when schedule recovery is triggered
        /// </summary>
        public RecoveryAction RecoveryAction { get; init; } = RecoveryAction.ImmediateExecution;

        /// <summary>
        /// Whether to catch up on missed executions
        /// </summary>
        public bool CatchUpMissedExecutions { get; init; } = false;

        /// <summary>
        /// Maximum number of catch-up executions to perform
        /// </summary>
        [Range(1, 10)]
        public int MaxCatchUpExecutions { get; init; } = 3;

        #endregion

        #region Advanced Patterns

        /// <summary>
        /// Whether to enable burst execution patterns
        /// </summary>
        public bool EnableBurstExecution { get; init; } = false;

        /// <summary>
        /// Burst execution configuration
        /// </summary>
        public BurstExecutionConfig BurstConfig { get; init; } = new();

        /// <summary>
        /// Whether to enable seasonal scheduling adjustments
        /// </summary>
        public bool EnableSeasonalAdjustments { get; init; } = false;

        /// <summary>
        /// Seasonal adjustment configurations
        /// </summary>
        public List<SeasonalAdjustment> SeasonalAdjustments { get; init; } = new();

        /// <summary>
        /// Whether to enable custom schedule expressions
        /// </summary>
        public bool EnableCustomExpressions { get; init; } = false;

        /// <summary>
        /// Custom schedule expression evaluator
        /// </summary>
        public string CustomExpression { get; init; } = string.Empty;

        #endregion

        #region Monitoring and Diagnostics

        /// <summary>
        /// Whether to enable schedule monitoring and diagnostics
        /// </summary>
        public bool EnableMonitoring { get; init; } = true;

        /// <summary>
        /// Whether to track schedule accuracy metrics
        /// </summary>
        public bool TrackScheduleAccuracy { get; init; } = true;

        /// <summary>
        /// Threshold for considering a schedule execution as delayed (milliseconds)
        /// </summary>
        [Range(100, 30000)]
        public int DelayThreshold { get; init; } = 1000;

        /// <summary>
        /// Whether to alert on schedule drift
        /// </summary>
        public bool AlertOnScheduleDrift { get; init; } = true;

        /// <summary>
        /// Maximum allowed schedule drift before alerting
        /// </summary>
        public TimeSpan MaxAllowedDrift { get; init; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Whether to enable detailed schedule logging
        /// </summary>
        public bool EnableDetailedLogging { get; init; } = false;

        #endregion

        #region Validation

        /// <summary>
        /// Validates the schedule configuration
        /// </summary>
        /// <returns>List of validation errors</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            // Basic validation
            if (Enabled)
            {
                if (Strategy == ScheduleStrategy.Cron && string.IsNullOrWhiteSpace(CronExpression))
                    errors.Add("CronExpression is required when using Cron scheduling strategy");

                if (Strategy == ScheduleStrategy.Interval && BaseInterval <= TimeSpan.Zero)
                    errors.Add("BaseInterval must be greater than zero for interval-based scheduling");

                if (BaseInterval > TimeSpan.FromDays(1))
                    errors.Add("BaseInterval should not exceed 1 day for practical scheduling");
            }

            // Cron expression validation
            if (!string.IsNullOrWhiteSpace(CronExpression))
            {
                if (!IsValidCronExpression(CronExpression))
                    errors.Add($"Invalid cron expression: {CronExpression}");
            }

            // Jitter validation
            if (EnableJitter)
            {
                if (JitterVariance < 0.0 || JitterVariance > 1.0)
                    errors.Add("JitterVariance must be between 0.0 and 1.0");
            }

            // Execution windows validation
            if (EnableExecutionWindows)
            {
                if (!ExecutionWindows.Any())
                    errors.Add("At least one execution window must be configured when EnableExecutionWindows is true");

                foreach (var window in ExecutionWindows)
                {
                    var windowErrors = window.Validate();
                    errors.AddRange(windowErrors.Select(e => $"Execution Window: {e}"));
                }

                if (QueueOutOfWindowExecutions && MaxQueueTime <= TimeSpan.Zero)
                    errors.Add("MaxQueueTime must be greater than zero when queuing out-of-window executions");
            }

            // Blackout periods validation
            if (EnableBlackoutPeriods)
            {
                if (!BlackoutPeriods.Any())
                    errors.Add("At least one blackout period must be configured when EnableBlackoutPeriods is true");

                foreach (var blackout in BlackoutPeriods)
                {
                    var blackoutErrors = blackout.Validate();
                    errors.AddRange(blackoutErrors.Select(e => $"Blackout Period: {e}"));
                }
            }

            // Adaptive scheduling validation
            if (EnableAdaptiveScheduling)
            {
                var adaptiveErrors = AdaptiveConfig.Validate();
                errors.AddRange(adaptiveErrors.Select(e => $"Adaptive ConfigSo: {e}"));

                if (DegradationFrequencyMultiplier < 0.1 || DegradationFrequencyMultiplier > 1.0)
                    errors.Add("DegradationFrequencyMultiplier must be between 0.1 and 1.0");

                if (StabilityFrequencyMultiplier < 1.0 || StabilityFrequencyMultiplier > 10.0)
                    errors.Add("StabilityFrequencyMultiplier must be between 1.0 and 10.0");
            }

            // Dependency scheduling validation
            if (EnableDependencyScheduling)
            {
                if (MaxDependencyWaitTime <= TimeSpan.Zero)
                    errors.Add("MaxDependencyWaitTime must be greater than zero");

                if (MaxDependencyWaitTime > TimeSpan.FromHours(1))
                    errors.Add("MaxDependencyWaitTime should not exceed 1 hour");
            }

            // Load balancing validation
            if (EnableLoadBalancing)
            {
                if (string.IsNullOrWhiteSpace(InstanceId))
                    errors.Add("InstanceId is required for load balancing");

                if (InstanceWeight < 0.1 || InstanceWeight > 10.0)
                    errors.Add("InstanceWeight must be between 0.1 and 10.0");
            }

            // Recovery validation
            if (EnableScheduleRecovery)
            {
                if (MaxMissedExecutions < 1)
                    errors.Add("MaxMissedExecutions must be at least 1");

                if (CatchUpMissedExecutions && MaxCatchUpExecutions < 1)
                    errors.Add("MaxCatchUpExecutions must be at least 1");
            }

            // Burst execution validation
            if (EnableBurstExecution)
            {
                var burstErrors = BurstConfig.Validate();
                errors.AddRange(burstErrors.Select(e => $"Burst ConfigSo: {e}"));
            }

            // Seasonal adjustments validation
            if (EnableSeasonalAdjustments)
            {
                foreach (var adjustment in SeasonalAdjustments)
                {
                    var adjustmentErrors = adjustment.Validate();
                    errors.AddRange(adjustmentErrors.Select(e => $"Seasonal Adjustment: {e}"));
                }
            }

            // Monitoring validation
            if (EnableMonitoring)
            {
                if (DelayThreshold < 100)
                    errors.Add("DelayThreshold must be at least 100 milliseconds");

                if (MaxAllowedDrift <= TimeSpan.Zero)
                    errors.Add("MaxAllowedDrift must be greater than zero");
            }

            return errors;
        }

        /// <summary>
        /// Validates a cron expression
        /// </summary>
        /// <param name="cronExpression">Cron expression to validate</param>
        /// <returns>True if valid</returns>
        private static bool IsValidCronExpression(string cronExpression)
        {
            if (string.IsNullOrWhiteSpace(cronExpression))
                return false;

            // Basic cron expression validation (simplified)
            var parts = cronExpression.Split(' ');
            if (parts.Length != 6) // Seconds, Minutes, Hours, Day of Month, Month, Day of Week
                return false;

            // Additional validation can be added here
            return true;
        }

        #endregion

        #region Calculation Methods

        /// <summary>
        /// Calculates the next execution time based on the schedule configuration
        /// </summary>
        /// <param name="lastExecution">Last execution time</param>
        /// <param name="currentTime">Current time</param>
        /// <returns>Next execution time</returns>
        public DateTime CalculateNextExecution(DateTime lastExecution, DateTime currentTime)
        {
            if (!Enabled)
                return currentTime.Add(BaseInterval);

            DateTime nextExecution = Strategy switch
            {
                ScheduleStrategy.Interval => CalculateIntervalBasedNext(lastExecution, currentTime),
                ScheduleStrategy.Cron => CalculateCronBasedNext(currentTime),
                ScheduleStrategy.Adaptive => CalculateAdaptiveNext(lastExecution, currentTime),
                ScheduleStrategy.Dependency => CalculateDependencyBasedNext(currentTime),
                ScheduleStrategy.Custom => CalculateCustomNext(currentTime),
                _ => currentTime.Add(BaseInterval)
            };

            // Apply jitter if enabled
            if (EnableJitter)
            {
                nextExecution = ApplyJitter(nextExecution);
            }

            // Check execution windows
            if (EnableExecutionWindows)
            {
                nextExecution = AdjustForExecutionWindows(nextExecution);
            }

            // Check blackout periods
            if (EnableBlackoutPeriods)
            {
                nextExecution = AdjustForBlackoutPeriods(nextExecution);
            }

            return nextExecution;
        }

        /// <summary>
        /// Determines if execution should occur at the given time
        /// </summary>
        /// <param name="executionTime">Time to check</param>
        /// <returns>True if execution should occur</returns>
        public bool ShouldExecuteAt(DateTime executionTime)
        {
            if (!Enabled)
                return true;

            // Check execution windows
            if (EnableExecutionWindows && !IsInExecutionWindow(executionTime))
                return OutOfWindowAction == OutOfWindowAction.Execute;

            // Check blackout periods
            if (EnableBlackoutPeriods && IsInBlackoutPeriod(executionTime))
                return BlackoutAction == BlackoutAction.Execute;

            return true;
        }

        private DateTime CalculateIntervalBasedNext(DateTime lastExecution, DateTime currentTime)
        {
            var nextExecution = lastExecution.Add(BaseInterval);
            return nextExecution < currentTime ? currentTime : nextExecution;
        }

        private DateTime CalculateCronBasedNext(DateTime currentTime)
        {
            // Simplified cron calculation - in production, use a proper cron library
            return currentTime.Add(BaseInterval);
        }

        private DateTime CalculateAdaptiveNext(DateTime lastExecution, DateTime currentTime)
        {
            // Implement adaptive scheduling logic based on recent health status
            return currentTime.Add(BaseInterval);
        }

        private DateTime CalculateDependencyBasedNext(DateTime currentTime)
        {
            // Implement dependency-based scheduling
            return currentTime.Add(BaseInterval);
        }

        private DateTime CalculateCustomNext(DateTime currentTime)
        {
            // Implement custom expression evaluation
            return currentTime.Add(BaseInterval);
        }

        private DateTime ApplyJitter(DateTime baseTime)
        {
            var random = new Random();
            var jitterMilliseconds = (int)(BaseInterval.TotalMilliseconds * JitterVariance);
            var jitterOffset = random.Next(-jitterMilliseconds, jitterMilliseconds);
            return baseTime.AddMilliseconds(jitterOffset);
        }

        private DateTime AdjustForExecutionWindows(DateTime proposedTime)
        {
            if (!IsInExecutionWindow(proposedTime))
            {
                var nextWindow = GetNextExecutionWindow(proposedTime);
                if (nextWindow != null)
                {
                    // Convert TimeSpan to DateTime for today, or next day if the time has passed
                    var today = proposedTime.Date;
                    var windowStart = today.Add(nextWindow.Start);
            
                    // If the window start time has already passed today, schedule for tomorrow
                    if (windowStart <= proposedTime)
                    {
                        windowStart = windowStart.AddDays(1);
                    }
            
                    return windowStart;
                }
                return proposedTime;
            }
            return proposedTime;
        }


        private DateTime AdjustForBlackoutPeriods(DateTime proposedTime)
        {
            if (IsInBlackoutPeriod(proposedTime))
            {
                var nextAvailableTime = GetNextAvailableTimeAfterBlackout(proposedTime);
                return nextAvailableTime;
            }
            return proposedTime;
        }

        private bool IsInExecutionWindow(DateTime time)
        {
            return ExecutionWindows.Any(window => window.Contains(time));
        }

        private bool IsInBlackoutPeriod(DateTime time)
        {
            return BlackoutPeriods.Any(blackout => blackout.Contains(time));
        }

        private TimeWindow GetNextExecutionWindow(DateTime fromTime)
        {
            return ExecutionWindows
                .Select(window => new { Window = window, NextStart = GetNextWindowStart(window, fromTime) })
                .Where(x => x.NextStart > fromTime)
                .OrderBy(x => x.NextStart)
                .FirstOrDefault()?.Window;
        }

        private DateTime GetNextWindowStart(TimeWindow window, DateTime fromTime)
        {
            var today = fromTime.Date;
            var windowStart = today.Add(window.Start);
    
            // If the window start time has already passed today, schedule for tomorrow
            if (windowStart <= fromTime)
            {
                windowStart = windowStart.AddDays(1);
            }
    
            // If specific day of week is required, find the next occurrence
            if (window.DayOfWeek.HasValue)
            {
                while (windowStart.DayOfWeek != window.DayOfWeek.Value)
                {
                    windowStart = windowStart.AddDays(1);
                }
            }
    
            return windowStart;
        }

        private DateTime GetNextAvailableTimeAfterBlackout(DateTime fromTime)
        {
            var blockingBlackout = BlackoutPeriods
                .Where(blackout => blackout.Contains(fromTime))
                .OrderBy(blackout => blackout.End)
                .FirstOrDefault();

            return blockingBlackout?.End ?? fromTime;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a simple interval-based schedule
        /// </summary>
        /// <param name="interval">Execution interval</param>
        /// <returns>Interval-based schedule configuration</returns>
        public static ScheduleConfig CreateInterval(TimeSpan interval)
        {
            return new ScheduleConfig
            {
                Enabled = true,
                Strategy = ScheduleStrategy.Interval,
                BaseInterval = interval,
                EnableJitter = true,
                JitterVariance = 0.1
            };
        }

        /// <summary>
        /// Creates a cron-based schedule
        /// </summary>
        /// <param name="cronExpression">Cron expression</param>
        /// <param name="timeZone">Timezone for cron evaluation</param>
        /// <returns>Cron-based schedule configuration</returns>
        public static ScheduleConfig CreateCron(string cronExpression, TimeZoneInfo timeZone = null)
        {
            return new ScheduleConfig
            {
                Enabled = true,
                Strategy = ScheduleStrategy.Cron,
                CronExpression = cronExpression,
                TimeZone = timeZone ?? TimeZoneInfo.Utc,
                EnableJitter = false
            };
        }

        /// <summary>
        /// Creates an adaptive schedule that adjusts based on health status
        /// </summary>
        /// <param name="baseInterval">Base execution interval</param>
        /// <returns>Adaptive schedule configuration</returns>
        public static ScheduleConfig CreateAdaptive(TimeSpan baseInterval)
        {
            return new ScheduleConfig
            {
                Enabled = true,
                Strategy = ScheduleStrategy.Adaptive,
                BaseInterval = baseInterval,
                EnableAdaptiveScheduling = true,
                IncreaseFrequencyOnDegradation = true,
                DecreaseFrequencyOnStability = true,
                DegradationFrequencyMultiplier = 0.5,
                StabilityFrequencyMultiplier = 2.0,
                EnableJitter = true
            };
        }

        /// <summary>
        /// Creates a schedule with execution windows
        /// </summary>
        /// <param name="interval">Base interval</param>
        /// <param name="windows">Allowed execution windows</param>
        /// <returns>Windowed schedule configuration</returns>
        public static ScheduleConfig CreateWindowed(TimeSpan interval, params TimeWindow[] windows)
        {
            return new ScheduleConfig
            {
                Enabled = true,
                Strategy = ScheduleStrategy.Interval,
                BaseInterval = interval,
                EnableExecutionWindows = true,
                ExecutionWindows = windows.ToList(),
                OutOfWindowAction = OutOfWindowAction.Skip,
                QueueOutOfWindowExecutions = true
            };
        }

        /// <summary>
        /// Creates a production-ready schedule with monitoring
        /// </summary>
        /// <param name="interval">Execution interval</param>
        /// <returns>Production schedule configuration</returns>
        public static ScheduleConfig CreateProduction(TimeSpan interval)
        {
            return new ScheduleConfig
            {
                Enabled = true,
                Strategy = ScheduleStrategy.Interval,
                BaseInterval = interval,
                EnableJitter = true,
                JitterVariance = 0.05,
                EnableScheduleRecovery = true,
                MaxMissedExecutions = 3,
                RecoveryAction = RecoveryAction.ImmediateExecution,
                EnableMonitoring = true,
                TrackScheduleAccuracy = true,
                AlertOnScheduleDrift = true,
                MaxAllowedDrift = TimeSpan.FromMinutes(1),
                DelayThreshold = 500
            };
        }

        #endregion
    }
}