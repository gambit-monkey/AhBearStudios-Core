using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Interface for health check scheduling services.
    /// Manages automatic execution of health checks at configured intervals.
    /// Scheduling events are published via IMessageBusService following CLAUDE.md patterns:
    /// - HealthCheckScheduledExecutionStartedMessage for execution start events
    /// - HealthCheckScheduledExecutionCompletedMessage for execution completion events
    /// </summary>
    public interface IHealthCheckSchedulerService : IDisposable
    {

        /// <summary>
        /// Gets whether automatic scheduling is currently active.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the current scheduling interval.
        /// </summary>
        TimeSpan CurrentInterval { get; }

        /// <summary>
        /// Gets the next scheduled execution time.
        /// </summary>
        DateTime NextExecutionTime { get; }

        /// <summary>
        /// Starts automatic health check scheduling.
        /// </summary>
        /// <param name="interval">Interval between scheduled executions</param>
        /// <param name="cancellationToken">Cancellation token</param>
        UniTask StartAsync(TimeSpan interval, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops automatic health check scheduling.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        UniTask StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the scheduling interval while running.
        /// </summary>
        /// <param name="newInterval">New scheduling interval</param>
        /// <param name="cancellationToken">Cancellation token</param>
        UniTask UpdateIntervalAsync(TimeSpan newInterval, CancellationToken cancellationToken = default);

        /// <summary>
        /// Triggers an immediate execution outside the normal schedule.
        /// </summary>
        /// <param name="reason">Reason for the immediate execution</param>
        /// <param name="cancellationToken">Cancellation token</param>
        UniTask TriggerImmediateExecutionAsync(string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pauses scheduling without stopping the service.
        /// </summary>
        void Pause();

        /// <summary>
        /// Resumes scheduling after being paused.
        /// </summary>
        void Resume();

        /// <summary>
        /// Gets scheduling statistics.
        /// </summary>
        /// <returns>Scheduler statistics</returns>
        HealthCheckSchedulerStatistics GetStatistics();
    }

    /// <summary>
    /// Event arguments for scheduled health check events.
    /// </summary>
    public sealed record ScheduledHealthCheckEventArgs
    {
        /// <summary>
        /// Gets the timestamp of the event.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets the reason for the execution.
        /// </summary>
        public string Reason { get; init; } = "Scheduled";

        /// <summary>
        /// Gets whether this was a manual trigger.
        /// </summary>
        public bool IsManualTrigger { get; init; }

        /// <summary>
        /// Gets the scheduled execution time.
        /// </summary>
        public DateTime ScheduledTime { get; init; }

        /// <summary>
        /// Gets the actual execution time.
        /// </summary>
        public DateTime ActualTime { get; init; }

        /// <summary>
        /// Gets the execution delay from scheduled time.
        /// </summary>
        public TimeSpan ExecutionDelay => ActualTime - ScheduledTime;
    }

    /// <summary>
    /// Statistics for the health check scheduler.
    /// </summary>
    public sealed record HealthCheckSchedulerStatistics
    {
        /// <summary>
        /// Gets the total number of scheduled executions.
        /// </summary>
        public long TotalScheduledExecutions { get; init; }

        /// <summary>
        /// Gets the total number of manual executions.
        /// </summary>
        public long TotalManualExecutions { get; init; }

        /// <summary>
        /// Gets the total number of missed executions.
        /// </summary>
        public long TotalMissedExecutions { get; init; }

        /// <summary>
        /// Gets the average execution delay.
        /// </summary>
        public TimeSpan AverageExecutionDelay { get; init; }

        /// <summary>
        /// Gets the maximum execution delay observed.
        /// </summary>
        public TimeSpan MaximumExecutionDelay { get; init; }

        /// <summary>
        /// Gets the current scheduling interval.
        /// </summary>
        public TimeSpan CurrentInterval { get; init; }

        /// <summary>
        /// Gets whether the scheduler is currently running.
        /// </summary>
        public bool IsRunning { get; init; }

        /// <summary>
        /// Gets whether the scheduler is currently paused.
        /// </summary>
        public bool IsPaused { get; init; }

        /// <summary>
        /// Gets the next scheduled execution time.
        /// </summary>
        public DateTime NextExecution { get; init; }

        /// <summary>
        /// Gets the last execution time.
        /// </summary>
        public DateTime LastExecution { get; init; }

        /// <summary>
        /// Gets the scheduler uptime.
        /// </summary>
        public TimeSpan Uptime { get; init; }
    }
}