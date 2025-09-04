using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Profiling;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Production implementation of health check scheduling service.
    /// Manages automatic execution timing and coordination with configurable intervals.
    /// </summary>
    public sealed class HealthCheckScheduler : IHealthCheckScheduler
    {
        private readonly ILoggingService _logger;
        private readonly IMessageBusService _messageBus;
        private readonly IProfilerService _profilerService;
        private readonly HealthCheckServiceConfig _config;
        private readonly Func<CancellationToken, UniTask> _executeHealthChecks;
        
        private readonly ProfilerMarker _scheduleMarker = new ProfilerMarker("HealthCheckScheduler.Schedule");
        private readonly Guid _schedulerId;
        private readonly object _stateLock = new();
        
        // Scheduling state
        private CancellationTokenSource _scheduleCts;
        private UniTask _schedulingTask;
        private DateTime _nextExecutionTime;
        private DateTime _lastExecutionTime;
        private DateTime _startTime;
        private TimeSpan _currentInterval;
        private bool _isPaused;
        private bool _isRunning;
        private bool _disposed;
        
        // Statistics
        private long _totalScheduledExecutions;
        private long _totalManualExecutions;
        private long _totalMissedExecutions;
        private TimeSpan _totalExecutionDelay;
        private TimeSpan _maxExecutionDelay;

        /// <summary>
        /// Event triggered when a scheduled health check execution begins.
        /// </summary>
        public event EventHandler<ScheduledHealthCheckEventArgs> ScheduledExecutionStarted;
        
        /// <summary>
        /// Event triggered when a scheduled health check execution completes.
        /// </summary>
        public event EventHandler<ScheduledHealthCheckEventArgs> ScheduledExecutionCompleted;

        /// <summary>
        /// Gets whether automatic scheduling is currently active.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (_stateLock)
                {
                    return _isRunning;
                }
            }
        }

        /// <summary>
        /// Gets the current scheduling interval.
        /// </summary>
        public TimeSpan CurrentInterval
        {
            get
            {
                lock (_stateLock)
                {
                    return _currentInterval;
                }
            }
        }

        /// <summary>
        /// Gets the next scheduled execution time.
        /// </summary>
        public DateTime NextExecutionTime
        {
            get
            {
                lock (_stateLock)
                {
                    return _nextExecutionTime;
                }
            }
        }

        /// <summary>
        /// Initializes a new health check scheduler.
        /// </summary>
        /// <param name="config">Health check service configuration</param>
        /// <param name="logger">Logging service</param>
        /// <param name="messageBus">Message bus for events</param>
        /// <param name="profilerService">Profiler service</param>
        /// <param name="executeHealthChecks">Function to execute health checks</param>
        /// <exception cref="ArgumentNullException">Thrown when required dependencies are null</exception>
        public HealthCheckScheduler(
            HealthCheckServiceConfig config,
            ILoggingService logger,
            IMessageBusService messageBus,
            IProfilerService profilerService,
            Func<CancellationToken, UniTask> executeHealthChecks)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _executeHealthChecks = executeHealthChecks ?? throw new ArgumentNullException(nameof(executeHealthChecks));
            
            _schedulerId = DeterministicIdGenerator.GenerateHealthCheckId("HealthCheckScheduler", Environment.MachineName);
            _currentInterval = _config.AutomaticCheckInterval;
            _startTime = DateTime.UtcNow;
            
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("SchedulerInit", _schedulerId.ToString());
            _logger.LogInfo($"HealthCheckScheduler initialized with interval {_currentInterval}", correlationId);
        }

        /// <summary>
        /// Starts automatic health check scheduling.
        /// </summary>
        /// <param name="interval">Interval between scheduled executions</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async UniTask StartAsync(TimeSpan interval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than zero");

            lock (_stateLock)
            {
                if (_isRunning)
                {
                    _logger.LogWarning("Scheduler is already running");
                    return;
                }

                _currentInterval = interval;
                _scheduleCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _nextExecutionTime = DateTime.UtcNow.Add(_currentInterval);
                _isRunning = true;
                _isPaused = false;
            }

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("SchedulerStart", _schedulerId.ToString());
            _logger.LogInfo($"Starting health check scheduler with interval {interval}", correlationId);

            _schedulingTask = RunSchedulingLoopAsync(_scheduleCts.Token);
            await UniTask.Yield(); // Let the scheduling loop start
        }

        /// <summary>
        /// Stops automatic health check scheduling.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async UniTask StopAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            lock (_stateLock)
            {
                if (!_isRunning)
                {
                    return;
                }

                _isRunning = false;
                _isPaused = false;
                _scheduleCts?.Cancel();
            }

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("SchedulerStop", _schedulerId.ToString());
            _logger.LogInfo("Stopping health check scheduler", correlationId);

            if (_schedulingTask.Status == UniTaskStatus.Pending)
            {
                try
                {
                    await _schedulingTask.AttachExternalCancellation(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                }
            }

            _scheduleCts?.Dispose();
            _scheduleCts = null;
        }

        /// <summary>
        /// Updates the scheduling interval while running.
        /// </summary>
        /// <param name="newInterval">New scheduling interval</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async UniTask UpdateIntervalAsync(TimeSpan newInterval, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (newInterval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(newInterval), "Interval must be greater than zero");

            lock (_stateLock)
            {
                if (!_isRunning)
                {
                    _logger.LogWarning("Cannot update interval when scheduler is not running");
                    return;
                }

                _currentInterval = newInterval;
                _nextExecutionTime = DateTime.UtcNow.Add(_currentInterval);
            }

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("SchedulerIntervalUpdate", _schedulerId.ToString());
            _logger.LogInfo($"Updated scheduling interval to {newInterval}", correlationId);

            await UniTask.Yield();
        }

        /// <summary>
        /// Triggers an immediate execution outside the normal schedule.
        /// </summary>
        /// <param name="reason">Reason for the immediate execution</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async UniTask TriggerImmediateExecutionAsync(string reason, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ManualExecution", _schedulerId.ToString());
            
            var eventArgs = new ScheduledHealthCheckEventArgs
            {
                CorrelationId = correlationId,
                Reason = reason ?? "Manual trigger",
                IsManualTrigger = true,
                ScheduledTime = DateTime.UtcNow,
                ActualTime = DateTime.UtcNow
            };

            ScheduledExecutionStarted?.Invoke(this, eventArgs);

            try
            {
                using (_scheduleMarker.Auto())
                {
                    await _executeHealthChecks(cancellationToken);
                    Interlocked.Increment(ref _totalManualExecutions);
                }

                ScheduledExecutionCompleted?.Invoke(this, eventArgs);
                _logger.LogInfo($"Manual health check execution completed: {reason}", correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogException($"Manual health check execution failed: {reason}",ex, correlationId.ToString());
                throw;
            }
        }

        /// <summary>
        /// Pauses scheduling without stopping the service.
        /// </summary>
        public void Pause()
        {
            ThrowIfDisposed();

            lock (_stateLock)
            {
                if (!_isRunning || _isPaused)
                    return;

                _isPaused = true;
            }

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("SchedulerPause", _schedulerId.ToString());
            _logger.LogInfo("Health check scheduler paused", correlationId);
        }

        /// <summary>
        /// Resumes scheduling after being paused.
        /// </summary>
        public void Resume()
        {
            ThrowIfDisposed();

            lock (_stateLock)
            {
                if (!_isRunning || !_isPaused)
                    return;

                _isPaused = false;
                _nextExecutionTime = DateTime.UtcNow.Add(_currentInterval);
            }

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("SchedulerResume", _schedulerId.ToString());
            _logger.LogInfo("Health check scheduler resumed", correlationId);
        }

        /// <summary>
        /// Gets scheduling statistics.
        /// </summary>
        /// <returns>Scheduler statistics</returns>
        public HealthCheckSchedulerStatistics GetStatistics()
        {
            lock (_stateLock)
            {
                var avgDelay = _totalScheduledExecutions > 0 
                    ? new TimeSpan(_totalExecutionDelay.Ticks / _totalScheduledExecutions)
                    : TimeSpan.Zero;

                return new HealthCheckSchedulerStatistics
                {
                    TotalScheduledExecutions = _totalScheduledExecutions,
                    TotalManualExecutions = _totalManualExecutions,
                    TotalMissedExecutions = _totalMissedExecutions,
                    AverageExecutionDelay = avgDelay,
                    MaximumExecutionDelay = _maxExecutionDelay,
                    CurrentInterval = _currentInterval,
                    IsRunning = _isRunning,
                    IsPaused = _isPaused,
                    NextExecution = _nextExecutionTime,
                    LastExecution = _lastExecutionTime,
                    Uptime = DateTime.UtcNow - _startTime
                };
            }
        }

        private async UniTask RunSchedulingLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    bool isPaused;
                    TimeSpan? delayTime = null;
                    bool shouldExecute = false;
                    
                    lock (_stateLock)
                    {
                        isPaused = _isPaused;
                        
                        if (!isPaused)
                        {
                            if (now < _nextExecutionTime)
                            {
                                // Not time yet, wait until next execution
                                var delay = _nextExecutionTime - now;
                                if (delay > TimeSpan.Zero)
                                {
                                    delayTime = delay;
                                }
                            }
                            else
                            {
                                shouldExecute = true;
                            }
                        }
                    }

                    if (isPaused)
                    {
                        // When paused, just wait and check again
                        await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
                        continue;
                    }

                    if (delayTime.HasValue)
                    {
                        await UniTask.Delay(delayTime.Value, cancellationToken: cancellationToken);
                        continue;
                    }

                    if (shouldExecute)
                    {
                        // Time to execute
                        await ExecuteScheduledHealthCheckAsync(cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    var correlationId = DeterministicIdGenerator.GenerateCorrelationId("SchedulerError", _schedulerId.ToString());
                    _logger.LogException("Error in scheduling loop", ex,correlationId.ToString());
                    
                    // Wait a bit before retrying to prevent tight error loops
                    await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: cancellationToken);
                }
            }
        }

        private async UniTask ExecuteScheduledHealthCheckAsync(CancellationToken cancellationToken)
        {
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ScheduledExecution", _schedulerId.ToString());
            var actualTime = DateTime.UtcNow;
            var scheduledTime = _nextExecutionTime;
            var delay = actualTime - scheduledTime;

            var eventArgs = new ScheduledHealthCheckEventArgs
            {
                CorrelationId = correlationId,
                Reason = "Scheduled execution",
                IsManualTrigger = false,
                ScheduledTime = scheduledTime,
                ActualTime = actualTime
            };

            // Update statistics
            lock (_stateLock)
            {
                _totalExecutionDelay = _totalExecutionDelay.Add(delay);
                if (delay > _maxExecutionDelay)
                    _maxExecutionDelay = delay;

                // Update next execution time
                _nextExecutionTime = actualTime.Add(_currentInterval);
                _lastExecutionTime = actualTime;
                
                Interlocked.Increment(ref _totalScheduledExecutions);
            }

            ScheduledExecutionStarted?.Invoke(this, eventArgs);

            try
            {
                using (_scheduleMarker.Auto())
                {
                    await _executeHealthChecks(cancellationToken);
                }

                ScheduledExecutionCompleted?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogException("Scheduled health check execution failed", ex,correlationId.ToString());
                // Don't rethrow - we want the scheduler to continue running
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HealthCheckScheduler));
        }

        /// <summary>
        /// Disposes the health check scheduler.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogException("Error disposing HealthCheckScheduler",ex);
            }

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("SchedulerDispose", _schedulerId.ToString());
            _logger.LogInfo("HealthCheckScheduler disposed", correlationId);
        }
    }
}