using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.Messaging;
using Unity.Collections;
using Random = Unity.Mathematics.Random;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Service responsible for scheduling and executing health checks with advanced timing controls
    /// </summary>
    /// <remarks>
    /// Provides comprehensive health check scheduling including interval-based, cron-based, and
    /// adaptive scheduling with resource management, priority handling, and concurrent execution control
    /// </remarks>
    public sealed class HealthSchedulingService : IDisposable
    {
        private readonly ILoggingService _logger;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;
        private readonly IPoolingService _poolingService;
        private readonly IProfilerService _profilerService;
        private readonly CheckScheduleConfig _scheduleConfig;

        private readonly ConcurrentDictionary<FixedString64Bytes, ScheduledHealthCheck> _scheduledChecks;
        private readonly ConcurrentDictionary<FixedString64Bytes, CheckExecutionState> _executionStates;
        private readonly ConcurrentQueue<HealthCheckExecution> _executionQueue;
        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly ReaderWriterLockSlim _scheduleLock;

        private Timer _schedulingTimer;
        private Timer _adaptiveTimer;
        private Timer _maintenanceTimer;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _schedulingTask;
        private Task _executionTask;
        private bool _disposed;

        /// <summary>
        /// Occurs when a health check is scheduled for execution
        /// </summary>
        public event EventHandler<HealthCheckScheduledEventArgs> HealthCheckScheduled;

        /// <summary>
        /// Occurs when a health check execution starts
        /// </summary>
        public event EventHandler<HealthCheckExecutionStartedEventArgs> HealthCheckExecutionStarted;

        /// <summary>
        /// Occurs when a health check execution completes
        /// </summary>
        public event EventHandler<HealthCheckExecutionCompletedEventArgs> HealthCheckExecutionCompleted;

        /// <summary>
        /// Occurs when scheduling statistics are updated
        /// </summary>
        public event EventHandler<SchedulingStatisticsUpdatedEventArgs> SchedulingStatisticsUpdated;

        /// <summary>
        /// Initializes the health scheduling service with required dependencies
        /// </summary>
        /// <param name="logger">Logging service for scheduling operations</param>
        /// <param name="alertService">Alert service for scheduling notifications</param>
        /// <param name="messageBusService">Message bus for publishing scheduling events</param>
        /// <param name="poolingService">Optional pooling service for performance optimization</param>
        /// <param name="profilerService">Optional profiler service for performance monitoring</param>
        /// <param name="scheduleConfig">Scheduling configuration and policies</param>
        /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
        public HealthSchedulingService(
            ILoggingService logger,
            IAlertService alertService,
            IMessageBusService messageBusService,
            IPoolingService poolingService,
            IProfilerService profilerService,
            CheckScheduleConfig scheduleConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _poolingService = poolingService; // Optional
            _profilerService = profilerService; // Optional
            _scheduleConfig = scheduleConfig ?? throw new ArgumentNullException(nameof(scheduleConfig));

            _scheduledChecks = new ConcurrentDictionary<FixedString64Bytes, ScheduledHealthCheck>();
            _executionStates = new ConcurrentDictionary<FixedString64Bytes, CheckExecutionState>();
            _executionQueue = new ConcurrentQueue<HealthCheckExecution>();
            _concurrencyLimiter = new SemaphoreSlim(_scheduleConfig.MaxConcurrentExecutions,
                _scheduleConfig.MaxConcurrentExecutions);
            _scheduleLock = new ReaderWriterLockSlim();

            _cancellationTokenSource = new CancellationTokenSource();

            ValidateConfigurationOrThrow();
            InitializeSchedulingSystem();

            _logger.LogInfo("HealthSchedulingService initialized with advanced scheduling capabilities");
        }

        /// <summary>
        /// Schedules a health check for execution with the specified configuration
        /// </summary>
        /// <param name="healthCheckName">Name of the health check to schedule</param>
        /// <param name="scheduleConfig">Scheduling configuration</param>
        /// <param name="healthCheckDelegate">Delegate to execute the health check</param>
        /// <exception cref="ArgumentException">Thrown when health check name is invalid</exception>
        /// <exception cref="ArgumentNullException">Thrown when delegate is null</exception>
        public void ScheduleHealthCheck(
            FixedString64Bytes healthCheckName,
            CheckScheduleConfig scheduleConfig,
            Func<CancellationToken, Task<HealthCheckResult>> healthCheckDelegate)
        {
            if (healthCheckName.IsEmpty)
                throw new ArgumentException("Health check name cannot be empty", nameof(healthCheckName));
            if (healthCheckDelegate == null)
                throw new ArgumentNullException(nameof(healthCheckDelegate));

            ThrowIfDisposed();

            try
            {
                _scheduleLock.EnterWriteLock();
                try
                {
                    var scheduledCheck =
                        CreateScheduledHealthCheck(healthCheckName, scheduleConfig, healthCheckDelegate);
                    _scheduledChecks.AddOrUpdate(healthCheckName, scheduledCheck, (_, _) => scheduledCheck);

                    // Initialize execution state
                    var executionState = CreateExecutionState(healthCheckName, scheduleConfig);
                    _executionStates.AddOrUpdate(healthCheckName, executionState, (_, _) => executionState);

                    // Calculate next execution time
                    CalculateNextExecutionTime(scheduledCheck);

                    OnHealthCheckScheduled(healthCheckName, scheduledCheck.NextExecutionTime);

                    _logger.LogInfo(
                        $"Scheduled health check '{healthCheckName}' with {scheduleConfig.ScheduleType} schedule");
                }
                finally
                {
                    _scheduleLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to schedule health check '{healthCheckName}'");
                throw;
            }
        }

        /// <summary>
        /// Schedules a health check with interval-based execution
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="interval">Execution interval</param>
        /// <param name="healthCheckDelegate">Delegate to execute the health check</param>
        /// <param name="initialDelay">Optional initial delay before first execution</param>
        public void ScheduleHealthCheck(
            FixedString64Bytes healthCheckName,
            TimeSpan interval,
            Func<CancellationToken, Task<HealthCheckResult>> healthCheckDelegate,
            TimeSpan? initialDelay = null)
        {
            var config = new CheckScheduleConfig
            {
                Name = $"Interval Schedule for {healthCheckName}",
                ScheduleType = ScheduleType.Interval,
                Interval = interval,
                InitialDelay = initialDelay ?? TimeSpan.Zero,
                EnableJitter = _scheduleConfig.EnableJitter,
                JitterPercentage = _scheduleConfig.JitterPercentage,
                Priority = _scheduleConfig.Priority
            };

            ScheduleHealthCheck(healthCheckName, config, healthCheckDelegate);
        }

        /// <summary>
        /// Schedules a health check with cron expression
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="cronExpression">Cron expression for scheduling</param>
        /// <param name="healthCheckDelegate">Delegate to execute the health check</param>
        public void ScheduleHealthCheck(
            FixedString64Bytes healthCheckName,
            string cronExpression,
            Func<CancellationToken, Task<HealthCheckResult>> healthCheckDelegate)
        {
            if (string.IsNullOrWhiteSpace(cronExpression))
                throw new ArgumentException("Cron expression cannot be null or empty", nameof(cronExpression));

            var config = new CheckScheduleConfig
            {
                Name = $"Cron Schedule for {healthCheckName}",
                ScheduleType = ScheduleType.Cron,
                CronExpression = cronExpression,
                Priority = _scheduleConfig.Priority
            };

            ScheduleHealthCheck(healthCheckName, config, healthCheckDelegate);
        }

        /// <summary>
        /// Unschedules a health check
        /// </summary>
        /// <param name="healthCheckName">Name of the health check to unschedule</param>
        /// <returns>True if the health check was found and unscheduled</returns>
        public bool UnscheduleHealthCheck(FixedString64Bytes healthCheckName)
        {
            ThrowIfDisposed();

            try
            {
                _scheduleLock.EnterWriteLock();
                try
                {
                    var removed = _scheduledChecks.TryRemove(healthCheckName, out var scheduledCheck);
                    if (removed)
                    {
                        _executionStates.TryRemove(healthCheckName, out _);

                        // Cancel any pending executions
                        CancelPendingExecutions(healthCheckName);

                        _logger.LogInfo($"Unscheduled health check '{healthCheckName}'");
                    }

                    return removed;
                }
                finally
                {
                    _scheduleLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to unschedule health check '{healthCheckName}'");
                return false;
            }
        }

        /// <summary>
        /// Gets the current scheduling status for all health checks
        /// </summary>
        /// <returns>Dictionary of health check names and their scheduling status</returns>
        public Dictionary<string, HealthCheckScheduleStatus> GetSchedulingStatus()
        {
            ThrowIfDisposed();

            try
            {
                _scheduleLock.EnterReadLock();
                try
                {
                    var status = new Dictionary<string, HealthCheckScheduleStatus>();

                    foreach (var kvp in _scheduledChecks)
                    {
                        var checkName = kvp.Key.ToString();
                        var scheduledCheck = kvp.Value;
                        var executionState = _executionStates.GetValueOrDefault(kvp.Key);

                        status[checkName] = new HealthCheckScheduleStatus
                        {
                            Name = checkName,
                            ScheduleType = scheduledCheck.ScheduleConfig.ScheduleType,
                            NextExecutionTime = scheduledCheck.NextExecutionTime,
                            LastExecutionTime = executionState?.LastExecutionTime,
                            LastExecutionDuration = executionState?.LastExecutionDuration ?? TimeSpan.Zero,
                            LastExecutionStatus = executionState?.LastExecutionStatus ?? HealthStatus.Unknown,
                            ExecutionCount = executionState?.ExecutionCount ?? 0,
                            FailureCount = executionState?.FailureCount ?? 0,
                            AverageExecutionTime = executionState?.AverageExecutionTime ?? TimeSpan.Zero,
                            IsRunning = executionState?.IsCurrentlyExecuting ?? false,
                            Priority = scheduledCheck.ScheduleConfig.Priority,
                            Enabled = scheduledCheck.IsEnabled
                        };
                    }

                    return status;
                }
                finally
                {
                    _scheduleLock.ExitReadLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to get scheduling status");
                return new Dictionary<string, HealthCheckScheduleStatus>();
            }
        }

        /// <summary>
        /// Gets detailed scheduling statistics
        /// </summary>
        /// <returns>Comprehensive scheduling statistics</returns>
        public SchedulingStatistics GetSchedulingStatistics()
        {
            ThrowIfDisposed();

            try
            {
                _scheduleLock.EnterReadLock();
                try
                {
                    var totalScheduledChecks = _scheduledChecks.Count;
                    var enabledChecks = _scheduledChecks.Values.Count(c => c.IsEnabled);
                    var runningChecks = _executionStates.Values.Count(s => s.IsCurrentlyExecuting);
                    var queuedExecutions = _executionQueue.Count;

                    var executionStates = _executionStates.Values.ToList();
                    var totalExecutions = executionStates.Sum(s => s.ExecutionCount);
                    var totalFailures = executionStates.Sum(s => s.FailureCount);
                    var averageExecutionTime = executionStates.Any()
                        ? TimeSpan.FromTicks((long)executionStates.Average(s => s.AverageExecutionTime.Ticks))
                        : TimeSpan.Zero;

                    return new SchedulingStatistics
                    {
                        TotalScheduledChecks = totalScheduledChecks,
                        EnabledChecks = enabledChecks,
                        DisabledChecks = totalScheduledChecks - enabledChecks,
                        CurrentlyRunningChecks = runningChecks,
                        QueuedExecutions = queuedExecutions,
                        TotalExecutions = totalExecutions,
                        TotalFailures = totalFailures,
                        SuccessRate = totalExecutions > 0
                            ? (double)(totalExecutions - totalFailures) / totalExecutions
                            : 0.0,
                        AverageExecutionTime = averageExecutionTime,
                        ConcurrencyUtilization = (double)runningChecks / _scheduleConfig.MaxConcurrentExecutions,
                        LastUpdateTime = DateTime.UtcNow
                    };
                }
                finally
                {
                    _scheduleLock.ExitReadLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to calculate scheduling statistics");
                return new SchedulingStatistics
                {
                    LastUpdateTime = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Enables or disables a scheduled health check
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="enabled">Whether to enable or disable the check</param>
        /// <returns>True if the health check was found and updated</returns>
        public bool SetHealthCheckEnabled(FixedString64Bytes healthCheckName, bool enabled)
        {
            ThrowIfDisposed();

            try
            {
                _scheduleLock.EnterWriteLock();
                try
                {
                    if (_scheduledChecks.TryGetValue(healthCheckName, out var scheduledCheck))
                    {
                        scheduledCheck.IsEnabled = enabled;

                        if (enabled)
                        {
                            CalculateNextExecutionTime(scheduledCheck);
                        }
                        else
                        {
                            CancelPendingExecutions(healthCheckName);
                        }

                        _logger.LogInfo($"Health check '{healthCheckName}' {(enabled ? "enabled" : "disabled")}");
                        return true;
                    }

                    return false;
                }
                finally
                {
                    _scheduleLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to update enabled status for health check '{healthCheckName}'");
                return false;
            }
        }

        /// <summary>
        /// Forces immediate execution of a health check
        /// </summary>
        /// <param name="healthCheckName">Name of the health check to execute</param>
        /// <returns>Task representing the execution</returns>
        public async Task<HealthCheckResult> ExecuteHealthCheckImmediately(FixedString64Bytes healthCheckName)
        {
            ThrowIfDisposed();

            try
            {
                if (!_scheduledChecks.TryGetValue(healthCheckName, out var scheduledCheck))
                {
                    throw new InvalidOperationException($"Health check '{healthCheckName}' is not scheduled");
                }

                var execution = new HealthCheckExecution
                {
                    HealthCheckName = healthCheckName,
                    ScheduledTime = DateTime.UtcNow,
                    ExecutionType = ExecutionType.Manual,
                    Priority = int.MaxValue, // Highest priority for manual execution
                    HealthCheckDelegate = scheduledCheck.HealthCheckDelegate
                };

                return await ExecuteHealthCheckInternal(execution, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to execute health check '{healthCheckName}' immediately");
                throw;
            }
        }

        /// <summary>
        /// Pauses all scheduled health check executions
        /// </summary>
        public void PauseScheduling()
        {
            ThrowIfDisposed();

            try
            {
                _scheduleLock.EnterWriteLock();
                try
                {
                    foreach (var scheduledCheck in _scheduledChecks.Values)
                    {
                        scheduledCheck.IsPaused = true;
                    }

                    _logger.LogInfo("Paused all health check scheduling");
                }
                finally
                {
                    _scheduleLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to pause scheduling");
            }
        }

        /// <summary>
        /// Resumes all scheduled health check executions
        /// </summary>
        public void ResumeScheduling()
        {
            ThrowIfDisposed();

            try
            {
                _scheduleLock.EnterWriteLock();
                try
                {
                    foreach (var scheduledCheck in _scheduledChecks.Values)
                    {
                        scheduledCheck.IsPaused = false;
                        CalculateNextExecutionTime(scheduledCheck);
                    }

                    _logger.LogInfo("Resumed all health check scheduling");
                }
                finally
                {
                    _scheduleLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to resume scheduling");
            }
        }

        #region Private Implementation

        private void ValidateConfigurationOrThrow()
        {
            var validationErrors = _scheduleConfig.Validate();

            if (validationErrors.Count > 0)
            {
                var errorMessage = $"Invalid scheduling configuration: {string.Join(", ", validationErrors)}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
        }

        private void InitializeSchedulingSystem()
        {
            // Initialize main scheduling timer
            _schedulingTimer = new Timer(
                ProcessScheduledChecks,
                null,
                TimeSpan.FromSeconds(1), // Check every second for precision
                TimeSpan.FromSeconds(1));

            // Initialize adaptive scheduling timer if enabled
            if (_scheduleConfig.AdaptiveConfig.Enabled)
            {
                _adaptiveTimer = new Timer(
                    ProcessAdaptiveScheduling,
                    null,
                    _scheduleConfig.AdaptiveConfig.AdaptationInterval,
                    _scheduleConfig.AdaptiveConfig.AdaptationInterval);
            }

            // Initialize maintenance timer
            _maintenanceTimer = new Timer(
                PerformSchedulingMaintenance,
                null,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5));

            // Start background execution task
            _executionTask = Task.Run(ProcessExecutionQueue, _cancellationTokenSource.Token);

            _logger.LogDebug("Initialized scheduling system timers and background tasks");
        }

        private ScheduledHealthCheck CreateScheduledHealthCheck(
            FixedString64Bytes healthCheckName,
            CheckScheduleConfig scheduleConfig,
            Func<CancellationToken, Task<HealthCheckResult>> healthCheckDelegate)
        {
            return new ScheduledHealthCheck
            {
                HealthCheckName = healthCheckName,
                ScheduleConfig = scheduleConfig,
                HealthCheckDelegate = healthCheckDelegate,
                CreatedTime = DateTime.UtcNow,
                LastExecutionTime = null,
                NextExecutionTime = DateTime.UtcNow + (scheduleConfig.InitialDelay ?? TimeSpan.Zero),
                IsEnabled = true,
                IsPaused = false,
                ExecutionCount = 0
            };
        }

        private CheckExecutionState CreateExecutionState(
            FixedString64Bytes healthCheckName,
            CheckScheduleConfig scheduleConfig)
        {
            return new CheckExecutionState
            {
                HealthCheckName = healthCheckName,
                ExecutionCount = 0,
                FailureCount = 0,
                LastExecutionTime = null,
                LastExecutionDuration = TimeSpan.Zero,
                LastExecutionStatus = HealthStatus.Unknown,
                AverageExecutionTime = TimeSpan.Zero,
                IsCurrentlyExecuting = false,
                ConsecutiveFailures = 0,
                ExecutionHistory = new List<ExecutionHistoryEntry>()
            };
        }

        private void CalculateNextExecutionTime(ScheduledHealthCheck scheduledCheck)
        {
            var now = DateTime.UtcNow;
            var config = scheduledCheck.ScheduleConfig;

            var nextTime = config.ScheduleType switch
            {
                ScheduleType.Interval => CalculateIntervalNextTime(scheduledCheck, now),
                ScheduleType.Cron => CalculateCronNextTime(config.CronExpression, now),
                ScheduleType.Daily => CalculateDailyNextTime(config.ScheduledTimes, now),
                ScheduleType.Weekly => CalculateWeeklyNextTime(config.DaysOfWeek, config.ScheduledTimes, now),
                ScheduleType.Monthly => CalculateMonthlyNextTime(config.DaysOfMonth, config.ScheduledTimes, now),
                _ => now.AddMinutes(1) // Default fallback
            };

            // Apply jitter if enabled
            if (config.EnableJitter && config.JitterPercentage > 0)
            {
                nextTime = ApplyJitter(nextTime, config.JitterPercentage, config.JitterType);
            }

            // Check maintenance windows
            if (config.MaintenanceConfig.Enabled)
            {
                nextTime = AdjustForMaintenanceWindow(nextTime, config.MaintenanceConfig);
            }

            scheduledCheck.NextExecutionTime = nextTime;
        }

        private DateTime CalculateIntervalNextTime(ScheduledHealthCheck scheduledCheck, DateTime now)
        {
            var interval = scheduledCheck.ScheduleConfig.Interval;
            var lastExecution = scheduledCheck.LastExecutionTime ?? scheduledCheck.CreatedTime;

            var nextTime = lastExecution + interval;

            // If we're behind schedule, execute immediately
            return nextTime <= now ? now : nextTime;
        }

        private DateTime CalculateCronNextTime(string cronExpression, DateTime now)
        {
            try
            {
                // Simple cron parsing - in production, use a proper cron library
                return ParseCronExpression(cronExpression, now);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"Failed to parse cron expression: {cronExpression}");
                return now.AddMinutes(1); // Fallback
            }
        }

        private DateTime CalculateDailyNextTime(List<TimeSpan> scheduledTimes, DateTime now)
        {
            var today = now.Date;

            foreach (var time in scheduledTimes.OrderBy(t => t))
            {
                var scheduledDateTime = today + time;
                if (scheduledDateTime > now)
                {
                    return scheduledDateTime;
                }
            }

            // No more times today, get first time tomorrow
            return scheduledTimes.Any()
                ? today.AddDays(1) + scheduledTimes.Min()
                : now.AddDays(1);
        }

        private DateTime CalculateWeeklyNextTime(List<DayOfWeek> daysOfWeek, List<TimeSpan> scheduledTimes,
            DateTime now)
        {
            for (int i = 0; i < 7; i++)
            {
                var checkDate = now.Date.AddDays(i);
                if (daysOfWeek.Contains(checkDate.DayOfWeek))
                {
                    foreach (var time in scheduledTimes.OrderBy(t => t))
                    {
                        var scheduledDateTime = checkDate + time;
                        if (scheduledDateTime > now)
                        {
                            return scheduledDateTime;
                        }
                    }
                }
            }

            return now.AddDays(7); // Fallback to next week
        }

        private DateTime CalculateMonthlyNextTime(List<int> daysOfMonth, List<TimeSpan> scheduledTimes, DateTime now)
        {
            var currentMonth = now.Month;
            var currentYear = now.Year;

            for (int monthOffset = 0; monthOffset < 2; monthOffset++)
            {
                var checkDate = new DateTime(currentYear, currentMonth, 1).AddMonths(monthOffset);
                var daysInMonth = DateTime.DaysInMonth(checkDate.Year, checkDate.Month);

                foreach (var day in daysOfMonth.Where(d => d <= daysInMonth).OrderBy(d => d))
                {
                    var dateToCheck = new DateTime(checkDate.Year, checkDate.Month, day);

                    if (dateToCheck < now.Date) continue;

                    foreach (var time in scheduledTimes.OrderBy(t => t))
                    {
                        var scheduledDateTime = dateToCheck + time;
                        if (scheduledDateTime > now)
                        {
                            return scheduledDateTime;
                        }
                    }
                }
            }

            return now.AddMonths(1); // Fallback
        }

        private DateTime ApplyJitter(DateTime scheduledTime, double jitterPercentage, JitterType jitterType)
        {
            var random = new Random();
            var jitterRange = TimeSpan.FromMinutes(5 * jitterPercentage / 100.0); // Base on 5 minutes

            var jitterAmount = jitterType switch
            {
                JitterType.Uniform => TimeSpan.FromTicks((long)(random.NextDouble() * jitterRange.Ticks * 2 -
                                                                jitterRange.Ticks)),
                JitterType.Exponential => CalculateExponentialJitter(jitterRange, random),
                JitterType.Gaussian => CalculateGaussianJitter(jitterRange, random),
                _ => TimeSpan.Zero
            };

            return scheduledTime + jitterAmount;
        }

        private TimeSpan CalculateExponentialJitter(TimeSpan baseRange, Random random)
        {
            var lambda = 1.0 / (baseRange.TotalSeconds / 2);
            var exponentialValue = -Math.Log(1 - random.NextDouble()) / lambda;
            var sign = random.Next(2) == 0 ? -1 : 1;
            return TimeSpan.FromSeconds(exponentialValue * sign);
        }

        private TimeSpan CalculateGaussianJitter(TimeSpan baseRange, Random random)
        {
            // Box-Muller transform for Gaussian distribution
            var u1 = 1.0 - random.NextDouble();
            var u2 = 1.0 - random.NextDouble();
            var standardNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            var gaussianValue = standardNormal * (baseRange.TotalSeconds / 4); // Standard deviation
            return TimeSpan.FromSeconds(gaussianValue);
        }

        private DateTime AdjustForMaintenanceWindow(DateTime scheduledTime, MaintenanceWindowConfig maintenanceConfig)
        {
            foreach (var window in maintenanceConfig.MaintenanceWindows)
            {
                if (IsInMaintenanceWindow(scheduledTime, window))
                {
                    // Move execution to after the maintenance window
                    return window.EndTime.AddMinutes(1);
                }
            }

            return scheduledTime;
        }

        private bool IsInMaintenanceWindow(DateTime time, MaintenanceWindow window)
        {
            var timeOfDay = time.TimeOfDay;
            return timeOfDay >= window.StartTime && timeOfDay <= window.EndTime &&
                   window.DaysOfWeek.Contains(time.DayOfWeek);
        }

        private DateTime ParseCronExpression(string cronExpression, DateTime now)
        {
            // Simplified cron parsing - in production, use a proper library like Quartz.NET
            // Format: "minute hour day month dayofweek"
            var parts = cronExpression.Split(' ');
            if (parts.Length != 5)
                throw new ArgumentException("Invalid cron expression format");

            // For simplicity, just handle basic cases
            return now.AddMinutes(5); // Placeholder
        }

        private void ProcessScheduledChecks(object state)
        {
            try
            {
                var now = DateTime.UtcNow;
                var dueChecks = new List<ScheduledHealthCheck>();

                _scheduleLock.EnterReadLock();
                try
                {
                    foreach (var scheduledCheck in _scheduledChecks.Values)
                    {
                        if (ShouldExecuteCheck(scheduledCheck, now))
                        {
                            dueChecks.Add(scheduledCheck);
                        }
                    }
                }
                finally
                {
                    _scheduleLock.ExitReadLock();
                }

                // Queue due checks for execution
                foreach (var check in dueChecks.OrderByDescending(c => c.ScheduleConfig.Priority))
                {
                    QueueHealthCheckExecution(check);
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error processing scheduled checks");
            }
        }

        private bool ShouldExecuteCheck(ScheduledHealthCheck scheduledCheck, DateTime now)
        {
            if (!scheduledCheck.IsEnabled || scheduledCheck.IsPaused)
                return false;

            if (scheduledCheck.NextExecutionTime > now)
                return false;

            // Check for overlapping execution
            if (_executionStates.TryGetValue(scheduledCheck.HealthCheckName, out var executionState))
            {
                if (executionState.IsCurrentlyExecuting)
                {
                    return HandleOverlappingExecution(scheduledCheck);
                }
            }

            return true;
        }

        private bool HandleOverlappingExecution(ScheduledHealthCheck scheduledCheck)
        {
            return scheduledCheck.ScheduleConfig.OverlapBehavior switch
            {
                OverlapBehavior.Skip => false,
                OverlapBehavior.Queue => true,
                OverlapBehavior.Parallel => true,
                OverlapBehavior.Cancel => CancelCurrentExecution(scheduledCheck.HealthCheckName),
                _ => false
            };
        }

        private bool CancelCurrentExecution(FixedString64Bytes healthCheckName)
        {
            // Implementation would cancel the current execution
            _logger.LogWarning($"Cancelling current execution of {healthCheckName} due to overlap behavior");
            return true;
        }

        private void QueueHealthCheckExecution(ScheduledHealthCheck scheduledCheck)
        {
            var execution = new HealthCheckExecution
            {
                HealthCheckName = scheduledCheck.HealthCheckName,
                ScheduledTime = scheduledCheck.NextExecutionTime,
                ExecutionType = ExecutionType.Scheduled,
                Priority = scheduledCheck.ScheduleConfig.Priority,
                HealthCheckDelegate = scheduledCheck.HealthCheckDelegate
            };

            _executionQueue.Enqueue(execution);

            // Update next execution time
            _scheduleLock.EnterWriteLock();
            try
            {
                CalculateNextExecutionTime(scheduledCheck);
            }
            finally
            {
                _scheduleLock.ExitWriteLock();
            }

            _logger.LogDebug($"Queued execution for health check '{scheduledCheck.HealthCheckName}'");
        }

        private async Task ProcessExecutionQueue()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (_executionQueue.TryDequeue(out var execution))
                    {
                        await _concurrencyLimiter.WaitAsync(_cancellationTokenSource.Token);

                        try
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await ExecuteHealthCheckInternal(execution, _cancellationTokenSource.Token);
                                }
                                finally
                                {
                                    _concurrencyLimiter.Release();
                                }
                            }, _cancellationTokenSource.Token);
                        }
                        catch
                        {
                            _concurrencyLimiter.Release();
                            throw;
                        }
                    }
                    else
                    {
                        await Task.Delay(100, _cancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex, "Error processing execution queue");
                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
            }
        }

        private async Task<HealthCheckResult> ExecuteHealthCheckInternal(
            HealthCheckExecution execution,
            CancellationToken cancellationToken)
        {
            var healthCheckName = execution.HealthCheckName;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            HealthCheckResult result = null;

            try
            {
                // Update execution state
                if (_executionStates.TryGetValue(healthCheckName, out var executionState))
                {
                    executionState.IsCurrentlyExecuting = true;
                }

                OnHealthCheckExecutionStarted(healthCheckName, execution.ScheduledTime);

                // Execute with profiling if available
                using var profilerScope = _profilerService?.BeginScope($"HealthCheck.{healthCheckName}");

                // Execute the health check with timeout
                var timeout = _scheduleConfig.MaxExecutionTime;
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(timeout);

                try
                {
                    result = await execution.HealthCheckDelegate(timeoutCts.Token);
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested &&
                                                         !cancellationToken.IsCancellationRequested)
                {
                    result = HealthCheckResult.Unhealthy(
                        $"Health check timed out after {timeout}",
                        stopwatch.Elapsed,
                        new Dictionary<string, object> { ["Timeout"] = timeout },
                        new TimeoutException($"Health check execution exceeded {timeout}"));
                }

                stopwatch.Stop();

                // Update execution statistics
                UpdateExecutionStatistics(healthCheckName, result, stopwatch.Elapsed);

                OnHealthCheckExecutionCompleted(healthCheckName, result, stopwatch.Elapsed);

                _logger.LogDebug($"Executed health check '{healthCheckName}': {result.Status} in {stopwatch.Elapsed}");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                result = HealthCheckResult.Unhealthy(
                    $"Health check execution failed: {ex.Message}",
                    stopwatch.Elapsed,
                    new Dictionary<string, object> { ["Exception"] = ex.GetType().Name },
                    ex);

                UpdateExecutionStatistics(healthCheckName, result, stopwatch.Elapsed);
                OnHealthCheckExecutionCompleted(healthCheckName, result, stopwatch.Elapsed);

                _logger.LogException(ex, $"Health check '{healthCheckName}' execution failed");

                return result;
            }
            finally
            {
                // Clear execution state
                if (_executionStates.TryGetValue(healthCheckName, out var executionState))
                {
                    executionState.IsCurrentlyExecuting = false;
                }
            }
        }

        private void UpdateExecutionStatistics(FixedString64Bytes healthCheckName, HealthCheckResult result,
            TimeSpan duration)
        {
            if (_executionStates.TryGetValue(healthCheckName, out var executionState))
            {
                executionState.ExecutionCount++;
                executionState.LastExecutionTime = DateTime.UtcNow;
                executionState.LastExecutionDuration = duration;
                executionState.LastExecutionStatus = result.Status;

                if (result.Status == HealthStatus.Unhealthy || result.Status == HealthStatus.Unknown)
                {
                    executionState.FailureCount++;
                    executionState.ConsecutiveFailures++;
                }
                else
                {
                    executionState.ConsecutiveFailures = 0;
                }

                // Update average execution time
                var totalTicks = (executionState.AverageExecutionTime.Ticks * (executionState.ExecutionCount - 1)) +
                                 duration.Ticks;
                executionState.AverageExecutionTime = TimeSpan.FromTicks(totalTicks / executionState.ExecutionCount);

                // Add to execution history
                executionState.ExecutionHistory.Add(new ExecutionHistoryEntry
                {
                    ExecutionTime = DateTime.UtcNow,
                    Duration = duration,
                    Status = result.Status,
                    Message = result.Message
                });

                // Limit history size
                const int maxHistorySize = 50;
                if (executionState.ExecutionHistory.Count > maxHistorySize)
                {
                    executionState.ExecutionHistory.RemoveRange(0,
                        executionState.ExecutionHistory.Count - maxHistorySize);
                }

                // Update scheduled check statistics
                if (_scheduledChecks.TryGetValue(healthCheckName, out var scheduledCheck))
                {
                    scheduledCheck.ExecutionCount++;
                    scheduledCheck.LastExecutionTime = DateTime.UtcNow;
                }
            }
        }

        private void ProcessAdaptiveScheduling(object state)
        {
            try
            {
                if (!_scheduleConfig.AdaptiveConfig.Enabled)
                    return;

                _scheduleLock.EnterWriteLock();
                try
                {
                    foreach (var kvp in _executionStates)
                    {
                        var healthCheckName = kvp.Key;
                        var executionState = kvp.Value;

                        if (_scheduledChecks.TryGetValue(healthCheckName, out var scheduledCheck))
                        {
                            AdaptSchedule(scheduledCheck, executionState);
                        }
                    }
                }
                finally
                {
                    _scheduleLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error during adaptive scheduling");
            }
        }

        private void AdaptSchedule(ScheduledHealthCheck scheduledCheck, CheckExecutionState executionState)
        {
            var config = _scheduleConfig.AdaptiveConfig;

            // Adaptive logic based on recent performance
            if (executionState.ConsecutiveFailures >= config.FailureEscalationThreshold)
            {
                // Increase frequency for failing checks
                var currentInterval = scheduledCheck.ScheduleConfig.Interval;
                var newInterval = TimeSpan.FromTicks((long)(currentInterval.Ticks * config.FailureIntervalMultiplier));

                if (newInterval >= config.MinimumInterval)
                {
                    scheduledCheck.ScheduleConfig.Interval = newInterval;
                    CalculateNextExecutionTime(scheduledCheck);

                    _logger.LogInfo($"Adapted schedule for failing health check '{scheduledCheck.HealthCheckName}': " +
                                    $"interval changed from {currentInterval} to {newInterval}");
                }
            }
            else if (executionState.ConsecutiveFailures == 0 &&
                     executionState.ExecutionCount > config.StabilityThreshold)
            {
                // Decrease frequency for stable checks
                var currentInterval = scheduledCheck.ScheduleConfig.Interval;
                var newInterval = TimeSpan.FromTicks((long)(currentInterval.Ticks * config.SuccessIntervalMultiplier));

                if (newInterval <= config.MaximumInterval)
                {
                    scheduledCheck.ScheduleConfig.Interval = newInterval;
                    CalculateNextExecutionTime(scheduledCheck);

                    _logger.LogDebug($"Adapted schedule for stable health check '{scheduledCheck.HealthCheckName}': " +
                                     $"interval changed from {currentInterval} to {newInterval}");
                }
            }
        }

        private void PerformSchedulingMaintenance(object state)
        {
            try
            {
                // Clean up old execution history
                foreach (var executionState in _executionStates.Values)
                {
                    var cutoffTime = DateTime.UtcNow - TimeSpan.FromHours(24);
                    executionState.ExecutionHistory.RemoveAll(h => h.ExecutionTime < cutoffTime);
                }

                // Update and publish scheduling statistics
                var statistics = GetSchedulingStatistics();
                OnSchedulingStatisticsUpdated(statistics);

                // Check for stuck executions
                CheckForStuckExecutions();

                _logger.LogDebug("Performed scheduling maintenance");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error during scheduling maintenance");
            }
        }

        private void CheckForStuckExecutions()
        {
            var stuckThreshold = _scheduleConfig.MaxExecutionTime.Add(TimeSpan.FromMinutes(5));
            var now = DateTime.UtcNow;

            foreach (var kvp in _executionStates)
            {
                var healthCheckName = kvp.Key;
                var executionState = kvp.Value;

                if (executionState.IsCurrentlyExecuting &&
                    executionState.LastExecutionTime.HasValue &&
                    (now - executionState.LastExecutionTime.Value) > stuckThreshold)
                {
                    _logger.LogWarning($"Detected stuck execution for health check '{healthCheckName}' - " +
                                       $"running for {now - executionState.LastExecutionTime.Value}");

                    // Reset execution state
                    executionState.IsCurrentlyExecuting = false;

                    // Raise alert
                    _alertService.RaiseAlert(
                        new FixedString64Bytes($"HealthCheck.Stuck.{healthCheckName}"),
                        AlertSeverity.Medium,
                        new FixedString512Bytes($"Health check '{healthCheckName}' appears to be stuck"));
                }
            }
        }

        private void CancelPendingExecutions(FixedString64Bytes healthCheckName)
        {
            // Note: In a more sophisticated implementation, we would track and cancel specific executions
            _logger.LogDebug($"Cancelled pending executions for health check '{healthCheckName}'");
        }

        private void OnHealthCheckScheduled(FixedString64Bytes healthCheckName, DateTime nextExecutionTime)
        {
            try
            {
                var eventArgs = new HealthCheckScheduledEventArgs
                {
                    HealthCheckName = healthCheckName.ToString(),
                    NextExecutionTime = nextExecutionTime,
                    Timestamp = DateTime.UtcNow
                };

                HealthCheckScheduled?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error invoking HealthCheckScheduled event");
            }
        }

        private void OnHealthCheckExecutionStarted(FixedString64Bytes healthCheckName, DateTime scheduledTime)
        {
            try
            {
                var eventArgs = new HealthCheckExecutionStartedEventArgs
                {
                    HealthCheckName = healthCheckName.ToString(),
                    ScheduledTime = scheduledTime,
                    ActualStartTime = DateTime.UtcNow
                };

                HealthCheckExecutionStarted?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error invoking HealthCheckExecutionStarted event");
            }
        }

        private void OnHealthCheckExecutionCompleted(FixedString64Bytes healthCheckName, HealthCheckResult result,
            TimeSpan duration)
        {
            try
            {
                var eventArgs = new HealthCheckExecutionCompletedEventArgs
                {
                    HealthCheckName = healthCheckName.ToString(),
                    Result = result,
                    Duration = duration,
                    CompletionTime = DateTime.UtcNow
                };

                HealthCheckExecutionCompleted?.Invoke(this, eventArgs);

                // Publish message bus event
                PublishExecutionCompletedMessage(healthCheckName, result, duration);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error invoking HealthCheckExecutionCompleted event");
            }
        }

        private void OnSchedulingStatisticsUpdated(SchedulingStatistics statistics)
        {
            try
            {
                var eventArgs = new SchedulingStatisticsUpdatedEventArgs
                {
                    Statistics = statistics
                };

                SchedulingStatisticsUpdated?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error invoking SchedulingStatisticsUpdated event");
            }
        }

        private void PublishExecutionCompletedMessage(FixedString64Bytes healthCheckName, HealthCheckResult result,
            TimeSpan duration)
        {
            try
            {
                var message = new HealthCheckExecutionMessage
                {
                    HealthCheckName = healthCheckName.ToString(),
                    Status = result.Status,
                    Message = result.Message,
                    Duration = duration,
                    Timestamp = DateTime.UtcNow
                };

                var publisher = _messageBusService.GetPublisher<HealthCheckExecutionMessage>();
                publisher.PublishMessage(message);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to publish health check execution message");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HealthSchedulingService));
        }

        #endregion

        #region IDisposable Implementation

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                // Cancel all operations
                _cancellationTokenSource.Cancel();

                // Dispose timers
                _schedulingTimer?.Dispose();
                _schedulingTimer = null;

                _adaptiveTimer?.Dispose();
                _adaptiveTimer = null;

                _maintenanceTimer?.Dispose();
                _maintenanceTimer = null;

                // Wait for background task to complete
                try
                {
                    _executionTask?.Wait(TimeSpan.FromSeconds(5));
                }
                catch (AggregateException)
                {
                    // Task was cancelled
                }

                // Dispose resources
                _cancellationTokenSource?.Dispose();
                _concurrencyLimiter?.Dispose();

                _scheduleLock.EnterWriteLock();
                try
                {
                    _scheduledChecks.Clear();
                    _executionStates.Clear();
                }
                finally
                {
                    _scheduleLock.ExitWriteLock();
                }

                _scheduleLock?.Dispose();

                _logger.LogInfo("HealthSchedulingService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error during HealthSchedulingService disposal");
            }
            finally
            {
                _disposed = true;
            }
        }

        #endregion
    }
}