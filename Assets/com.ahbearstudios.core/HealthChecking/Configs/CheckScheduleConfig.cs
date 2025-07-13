using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Advanced scheduling configuration for health check execution with flexible timing options
    /// </summary>
    public sealed record CheckScheduleConfig
    {
        /// <summary>
        /// Unique identifier for this schedule configuration
        /// </summary>
        public FixedString64Bytes Id { get; init; } = GenerateId();

        /// <summary>
        /// Display name for this schedule configuration
        /// </summary>
        public string Name { get; init; } = "Default Schedule";

        /// <summary>
        /// Whether this schedule is currently enabled
        /// </summary>
        public bool Enabled { get; init; } = true;

        /// <summary>
        /// Type of scheduling strategy to use
        /// </summary>
        public ScheduleType ScheduleType { get; init; } = ScheduleType.Interval;

        /// <summary>
        /// Fixed interval between health check executions
        /// </summary>
        public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Initial delay before first execution
        /// </summary>
        public TimeSpan InitialDelay { get; init; } = TimeSpan.Zero;

        /// <summary>
        /// Cron expression for cron-based scheduling
        /// </summary>
        public string CronExpression { get; init; } = string.Empty;

        /// <summary>
        /// Time zone for cron expression evaluation
        /// </summary>
        public string TimeZone { get; init; } = "UTC";

        /// <summary>
        /// Specific times of day when health checks should run
        /// </summary>
        public List<TimeSpan> ScheduledTimes { get; init; } = new();

        /// <summary>
        /// Days of week when health checks should run (for weekly scheduling)
        /// </summary>
        public HashSet<DayOfWeek> ScheduledDays { get; init; } = new();

        /// <summary>
        /// Specific dates when health checks should run
        /// </summary>
        public List<DateTime> ScheduledDates { get; init; } = new();

        /// <summary>
        /// Whether to use jitter to distribute load
        /// </summary>
        public bool EnableJitter { get; init; } = true;

        /// <summary>
        /// Maximum jitter amount as percentage of interval (0-100)
        /// </summary>
        public double JitterPercentage { get; init; } = 10.0;

        /// <summary>
        /// Type of jitter distribution to use
        /// </summary>
        public JitterType JitterType { get; init; } = JitterType.Uniform;

        /// <summary>
        /// Whether to use exponential backoff on failures
        /// </summary>
        public bool EnableBackoff { get; init; } = false;

        /// <summary>
        /// Base backoff interval for exponential backoff
        /// </summary>
        public TimeSpan BaseBackoffInterval { get; init; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Maximum backoff interval to prevent excessive delays
        /// </summary>
        public TimeSpan MaxBackoffInterval { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Backoff multiplier for exponential increase
        /// </summary>
        public double BackoffMultiplier { get; init; } = 2.0;

        /// <summary>
        /// Number of consecutive failures before applying backoff
        /// </summary>
        public int BackoffThreshold { get; init; } = 3;

        /// <summary>
        /// Maximum number of concurrent executions allowed
        /// </summary>
        public int MaxConcurrentExecutions { get; init; } = 1;

        /// <summary>
        /// Behavior when previous execution is still running
        /// </summary>
        public OverlapBehavior OverlapBehavior { get; init; } = OverlapBehavior.Skip;

        /// <summary>
        /// Maximum execution time before considering a check hung
        /// </summary>
        public TimeSpan MaxExecutionTime { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Priority of this scheduled check (higher numbers execute first)
        /// </summary>
        public int Priority { get; init; } = 100;

        /// <summary>
        /// Resource pool to use for execution
        /// </summary>
        public string ResourcePool { get; init; } = "Default";

        /// <summary>
        /// Execution context settings
        /// </summary>
        public ExecutionContextConfig ExecutionContext { get; init; } = new();

        /// <summary>
        /// Throttling configuration to limit execution rate
        /// </summary>
        public ThrottlingConfig ThrottlingConfig { get; init; } = new();

        /// <summary>
        /// Batch execution configuration for grouping checks
        /// </summary>
        public BatchExecutionConfig BatchConfig { get; init; } = new();

        /// <summary>
        /// Adaptive scheduling configuration for dynamic intervals
        /// </summary>
        public AdaptiveSchedulingConfig AdaptiveConfig { get; init; } = new();

        /// <summary>
        /// Maintenance window configuration
        /// </summary>
        public MaintenanceWindowConfig MaintenanceConfig { get; init; } = new();

        /// <summary>
        /// Holiday and exception date handling
        /// </summary>
        public HolidayConfig HolidayConfig { get; init; } = new();

        /// <summary>
        /// Custom tags for this schedule
        /// </summary>
        public HashSet<FixedString64Bytes> Tags { get; init; } = new();

        /// <summary>
        /// Custom metadata for this schedule
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();

        /// <summary>
        /// Validates the schedule configuration
        /// </summary>
        /// <returns>List of validation error messages, empty if valid</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Name cannot be null or empty");

            if (!Enum.IsDefined(typeof(ScheduleType), ScheduleType))
                errors.Add($"Invalid schedule type: {ScheduleType}");

            if (Interval <= TimeSpan.Zero && ScheduleType == ScheduleType.Interval)
                errors.Add("Interval must be greater than zero for interval-based scheduling");

            if (InitialDelay < TimeSpan.Zero)
                errors.Add("InitialDelay must be non-negative");

            if (ScheduleType == ScheduleType.Cron && string.IsNullOrWhiteSpace(CronExpression))
                errors.Add("CronExpression is required for cron-based scheduling");

            if (JitterPercentage < 0 || JitterPercentage > 100)
                errors.Add("JitterPercentage must be between 0 and 100");

            if (!Enum.IsDefined(typeof(JitterType), JitterType))
                errors.Add($"Invalid jitter type: {JitterType}");

            if (BaseBackoffInterval <= TimeSpan.Zero && EnableBackoff)
                errors.Add("BaseBackoffInterval must be greater than zero when backoff is enabled");

            if (MaxBackoffInterval < BaseBackoffInterval && EnableBackoff)
                errors.Add("MaxBackoffInterval must be greater than or equal to BaseBackoffInterval");

            if (BackoffMultiplier <= 1.0 && EnableBackoff)
                errors.Add("BackoffMultiplier must be greater than 1.0 when backoff is enabled");

            if (BackoffThreshold <= 0 && EnableBackoff)
                errors.Add("BackoffThreshold must be greater than zero when backoff is enabled");

            if (MaxConcurrentExecutions <= 0)
                errors.Add("MaxConcurrentExecutions must be greater than zero");

            if (!Enum.IsDefined(typeof(OverlapBehavior), OverlapBehavior))
                errors.Add($"Invalid overlap behavior: {OverlapBehavior}");

            if (MaxExecutionTime <= TimeSpan.Zero)
                errors.Add("MaxExecutionTime must be greater than zero");

            if (string.IsNullOrWhiteSpace(ResourcePool))
                errors.Add("ResourcePool cannot be null or empty");

            // Validate specific schedule types
            switch (ScheduleType)
            {
                case ScheduleType.Daily:
                    if (ScheduledTimes.Count == 0)
                        errors.Add("ScheduledTimes must be specified for daily scheduling");
                    break;

                case ScheduleType.Weekly:
                    if (ScheduledDays.Count == 0 || ScheduledTimes.Count == 0)
                        errors.Add("Both ScheduledDays and ScheduledTimes must be specified for weekly scheduling");
                    break;

                case ScheduleType.Monthly:
                    if (ScheduledDates.Count == 0)
                        errors.Add("ScheduledDates must be specified for monthly scheduling");
                    break;

                case ScheduleType.Cron:
                    if (!IsValidCronExpression(CronExpression))
                        errors.Add("Invalid cron expression format");
                    break;
            }

            // Validate time values
            foreach (var time in ScheduledTimes)
            {
                if (time < TimeSpan.Zero || time >= TimeSpan.FromDays(1))
                    errors.Add($"Invalid scheduled time: {time}. Must be between 00:00:00 and 23:59:59");
            }

            // Validate nested configurations
            errors.AddRange(ExecutionContext.Validate());
            errors.AddRange(ThrottlingConfig.Validate());
            errors.AddRange(BatchConfig.Validate());
            errors.AddRange(AdaptiveConfig.Validate());
            errors.AddRange(MaintenanceConfig.Validate());
            errors.AddRange(HolidayConfig.Validate());

            return errors;
        }

        /// <summary>
        /// Creates a schedule configuration for high-frequency monitoring
        /// </summary>
        /// <returns>High-frequency schedule configuration</returns>
        public static CheckScheduleConfig ForHighFrequency()
        {
            return new CheckScheduleConfig
            {
                Name = "High Frequency Schedule",
                ScheduleType = ScheduleType.Interval,
                Interval = TimeSpan.FromSeconds(10),
                EnableJitter = true,
                JitterPercentage = 20.0,
                JitterType = JitterType.Uniform,
                MaxConcurrentExecutions = 5,
                OverlapBehavior = OverlapBehavior.Queue,
                MaxExecutionTime = TimeSpan.FromSeconds(30),
                Priority = 1000,
                ThrottlingConfig = new ThrottlingConfig
                {
                    Enabled = true,
                    MaxExecutionsPerMinute = 100,
                    BurstAllowance = 20
                }
            };
        }

        /// <summary>
        /// Creates a schedule configuration for business hours monitoring
        /// </summary>
        /// <returns>Business hours schedule configuration</returns>
        public static CheckScheduleConfig ForBusinessHours()
        {
            return new CheckScheduleConfig
            {
                Name = "Business Hours Schedule",
                ScheduleType = ScheduleType.Daily,
                ScheduledTimes = new List<TimeSpan>
                {
                    new(9, 0, 0),   // 9:00 AM
                    new(12, 0, 0),  // 12:00 PM
                    new(15, 0, 0),  // 3:00 PM
                    new(17, 0, 0)   // 5:00 PM
                },
                EnableJitter = true,
                JitterPercentage = 5.0,
                MaxConcurrentExecutions = 10,
                OverlapBehavior = OverlapBehavior.Skip,
                MaintenanceConfig = new MaintenanceWindowConfig
                {
                    MaintenanceWindows = new List<MaintenanceWindow>
                    {
                        new()
                        {
                            StartTime = new TimeSpan(18, 0, 0),
                            EndTime = new TimeSpan(8, 30, 0),
                            DaysOfWeek = new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Creates a schedule configuration for critical system monitoring
        /// </summary>
        /// <returns>Critical system schedule configuration</returns>
        public static CheckScheduleConfig ForCriticalSystem()
        {
            return new CheckScheduleConfig
            {
                Name = "Critical System Schedule",
                ScheduleType = ScheduleType.Interval,
                Interval = TimeSpan.FromSeconds(15),
                EnableJitter = false,
                EnableBackoff = true,
                BaseBackoffInterval = TimeSpan.FromSeconds(2),
                MaxBackoffInterval = TimeSpan.FromMinutes(1),
                BackoffMultiplier = 1.5,
                BackoffThreshold = 2,
                MaxConcurrentExecutions = 1,
                OverlapBehavior = OverlapBehavior.Cancel,
                MaxExecutionTime = TimeSpan.FromSeconds(10),
                Priority = 2000,
                AdaptiveConfig = new AdaptiveSchedulingConfig
                {
                    Enabled = true,
                    MinInterval = TimeSpan.FromSeconds(5),
                    MaxInterval = TimeSpan.FromMinutes(2),
                    HealthBasedAdjustment = true
                }
            };
        }

        /// <summary>
        /// Creates a schedule configuration for development/testing
        /// </summary>
        /// <returns>Development schedule configuration</returns>
        public static CheckScheduleConfig ForDevelopment()
        {
            return new CheckScheduleConfig
            {
                Name = "Development Schedule",
                ScheduleType = ScheduleType.Interval,
                Interval = TimeSpan.FromSeconds(5),
                EnableJitter = false,
                EnableBackoff = false,
                MaxConcurrentExecutions = 1,
                OverlapBehavior = OverlapBehavior.Skip,
                MaxExecutionTime = TimeSpan.FromSeconds(30),
                Priority = 100
            };
        }

        /// <summary>
        /// Validates a cron expression format
        /// </summary>
        /// <param name="cronExpression">Cron expression to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        private static bool IsValidCronExpression(string cronExpression)
        {
            if (string.IsNullOrWhiteSpace(cronExpression))
                return false;

            var parts = cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 5 && parts.Length <= 7; // Basic validation - could be more sophisticated
        }

        /// <summary>
        /// Generates a unique identifier for configurations
        /// </summary>
        /// <returns>Unique configuration ID</returns>
        private static FixedString64Bytes GenerateId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);
        }
    }

    

    

    

    

    

    

    

    

    

    

    

    

    
}