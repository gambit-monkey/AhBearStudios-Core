using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Random = System.Random;

namespace AhBearStudios.Core.Messaging.Services
{
    /// <summary>
    /// Interface for message retry management and queue processing.
    /// Handles retry logic, exponential backoff, and retry queue management.
    /// Focused on retry responsibilities only, following single responsibility principle.
    /// </summary>
    public interface IMessageRetryService : IDisposable
    {
        #region Retry Operations

        /// <summary>
        /// Queues a message for retry after a failure.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="message">The message to retry</param>
        /// <param name="exception">The exception that caused the failure</param>
        /// <param name="context">Additional context about the failure</param>
        void QueueMessageForRetry<TMessage>(TMessage message, Exception exception, string context = null) where TMessage : IMessage;

        /// <summary>
        /// Queues multiple messages for retry after failures.
        /// </summary>
        /// <param name="pendingMessages">Collection of pending messages to retry</param>
        void QueueMessagesForRetry(IEnumerable<PendingMessage> pendingMessages);

        /// <summary>
        /// Manually retries a specific message by ID.
        /// </summary>
        /// <param name="messageId">ID of the message to retry</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if the retry was successful</returns>
        UniTask<bool> RetryMessageAsync(Guid messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes the retry queue and attempts to retry eligible messages.
        /// </summary>
        /// <param name="maxRetries">Maximum number of messages to retry in this batch</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Number of messages successfully retried</returns>
        UniTask<int> ProcessRetryQueueAsync(int maxRetries = 100, CancellationToken cancellationToken = default);

        #endregion

        #region Retry Queue Management

        /// <summary>
        /// Gets the current size of the retry queue.
        /// </summary>
        /// <returns>Number of messages waiting for retry</returns>
        int GetRetryQueueSize();

        /// <summary>
        /// Gets messages currently in the retry queue.
        /// </summary>
        /// <param name="maxCount">Maximum number of messages to return</param>
        /// <returns>Collection of pending messages</returns>
        IEnumerable<PendingMessage> GetRetryQueueMessages(int maxCount = 100);

        /// <summary>
        /// Gets messages in the retry queue for a specific message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="maxCount">Maximum number of messages to return</param>
        /// <returns>Collection of pending messages of the specified type</returns>
        IEnumerable<PendingMessage> GetRetryQueueMessages<TMessage>(int maxCount = 100) where TMessage : IMessage;

        /// <summary>
        /// Removes a message from the retry queue.
        /// </summary>
        /// <param name="messageId">ID of the message to remove</param>
        /// <returns>True if the message was removed</returns>
        bool RemoveFromRetryQueue(Guid messageId);

        /// <summary>
        /// Clears all messages from the retry queue.
        /// </summary>
        /// <returns>Number of messages that were cleared</returns>
        int ClearRetryQueue();

        /// <summary>
        /// Clears messages of a specific type from the retry queue.
        /// </summary>
        /// <typeparam name="TMessage">The message type to clear</typeparam>
        /// <returns>Number of messages that were cleared</returns>
        int ClearRetryQueue<TMessage>() where TMessage : IMessage;

        #endregion

        #region Retry Policy Configuration

        /// <summary>
        /// Sets the retry policy for a specific message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="policy">The retry policy to apply</param>
        void SetRetryPolicy<TMessage>(RetryPolicy policy) where TMessage : IMessage;

        /// <summary>
        /// Gets the retry policy for a specific message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <returns>The retry policy for the message type</returns>
        RetryPolicy GetRetryPolicy<TMessage>() where TMessage : IMessage;

        /// <summary>
        /// Sets the default retry policy for all message types.
        /// </summary>
        /// <param name="policy">The default retry policy</param>
        void SetDefaultRetryPolicy(RetryPolicy policy);

        /// <summary>
        /// Gets the default retry policy.
        /// </summary>
        /// <returns>The default retry policy</returns>
        RetryPolicy GetDefaultRetryPolicy();

        /// <summary>
        /// Removes the retry policy for a specific message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        void RemoveRetryPolicy<TMessage>() where TMessage : IMessage;

        #endregion

        #region Statistics and Diagnostics

        /// <summary>
        /// Gets retry service statistics.
        /// </summary>
        /// <returns>Current retry statistics</returns>
        MessageRetryStatistics GetStatistics();

        /// <summary>
        /// Gets retry statistics for a specific message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <returns>Retry statistics for the message type</returns>
        MessageTypeRetryStatistics GetStatistics<TMessage>() where TMessage : IMessage;

        /// <summary>
        /// Clears retry statistics.
        /// </summary>
        void ClearStatistics();

        /// <summary>
        /// Gets the retry success rate (0.0 to 1.0).
        /// </summary>
        /// <returns>Current retry success rate</returns>
        double GetRetrySuccessRate();

        /// <summary>
        /// Gets the average retry attempts per message.
        /// </summary>
        /// <returns>Average number of retry attempts</returns>
        double GetAverageRetryAttempts();

        #endregion

        #region Health and Status

        /// <summary>
        /// Gets the current health status of the retry service.
        /// </summary>
        /// <returns>Current health status</returns>
        HealthStatus GetHealthStatus();

        /// <summary>
        /// Forces a health check evaluation and returns the result.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Health check result</returns>
        UniTask<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);

        #endregion

        #region Events

        /// <summary>
        /// Event fired when a message is queued for retry.
        /// </summary>
        event Action<Guid, Type, int, TimeSpan> MessageQueuedForRetry;

        /// <summary>
        /// Event fired when a message retry succeeds.
        /// </summary>
        event Action<Guid, Type, int> MessageRetrySucceeded;

        /// <summary>
        /// Event fired when a message retry fails.
        /// </summary>
        event Action<Guid, Type, int, Exception> MessageRetryFailed;

        /// <summary>
        /// Event fired when a message exhausts all retry attempts.
        /// </summary>
        event Action<Guid, Type, int> MessageRetryExhausted;

        /// <summary>
        /// Event fired when the retry queue is processed.
        /// </summary>
        event Action<int, int, int> RetryQueueProcessed;

        #endregion
    }

    /// <summary>
    /// Represents retry policy configuration for messages.
    /// </summary>
    public sealed class RetryPolicy
    {
        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        public int MaxAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial delay between retry attempts.
        /// </summary>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the maximum delay between retry attempts.
        /// </summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the backoff strategy.
        /// </summary>
        public RetryBackoffStrategy BackoffStrategy { get; set; } = RetryBackoffStrategy.Exponential;

        /// <summary>
        /// Gets or sets the backoff multiplier for exponential backoff.
        /// </summary>
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Gets or sets whether to add jitter to retry delays.
        /// </summary>
        public bool UseJitter { get; set; } = true;

        /// <summary>
        /// Gets or sets the jitter factor (0.0 to 1.0).
        /// </summary>
        public double JitterFactor { get; set; } = 0.1;

        /// <summary>
        /// Gets or sets the exceptions that should trigger retries.
        /// </summary>
        public HashSet<Type> RetryableExceptions { get; set; } = new HashSet<Type>();

        /// <summary>
        /// Gets or sets whether retries are enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Calculates the delay for a specific retry attempt.
        /// </summary>
        /// <param name="attemptNumber">The retry attempt number (1-based)</param>
        /// <returns>Calculated delay for this attempt</returns>
        public TimeSpan CalculateDelay(int attemptNumber)
        {
            if (attemptNumber <= 0) return TimeSpan.Zero;

            TimeSpan delay = BackoffStrategy switch
            {
                RetryBackoffStrategy.Fixed => InitialDelay,
                RetryBackoffStrategy.Linear => TimeSpan.FromMilliseconds(InitialDelay.TotalMilliseconds * attemptNumber),
                RetryBackoffStrategy.Exponential => TimeSpan.FromMilliseconds(InitialDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, attemptNumber - 1)),
                _ => InitialDelay
            };

            // Apply maximum delay limit
            if (delay > MaxDelay) delay = MaxDelay;

            // Apply jitter if enabled
            if (UseJitter)
            {
                var jitterRange = delay.TotalMilliseconds * JitterFactor;
                var random = new Random();
                var jitter = (random.NextDouble() - 0.5) * 2 * jitterRange; // -jitterRange to +jitterRange
                delay = TimeSpan.FromMilliseconds(Math.Max(0, delay.TotalMilliseconds + jitter));
            }

            return delay;
        }

        /// <summary>
        /// Determines if an exception should trigger a retry.
        /// </summary>
        /// <param name="exception">The exception to check</param>
        /// <returns>True if the exception should trigger a retry</returns>
        public bool ShouldRetry(Exception exception)
        {
            if (!Enabled) return false;
            if (RetryableExceptions.Count == 0) return true; // Retry all exceptions if none specified
            
            return RetryableExceptions.Contains(exception.GetType()) || 
                   RetryableExceptions.AsEnumerable().Any(type => type.IsAssignableFrom(exception.GetType()));
        }

        /// <summary>
        /// Returns a string representation of the retry policy.
        /// </summary>
        /// <returns>Retry policy summary string</returns>
        public override string ToString()
        {
            return $"RetryPolicy: MaxAttempts={MaxAttempts}, InitialDelay={InitialDelay.TotalSeconds}s, " +
                   $"Strategy={BackoffStrategy}, Multiplier={BackoffMultiplier}, Jitter={UseJitter}, Enabled={Enabled}";
        }
    }

    /// <summary>
    /// Enum for retry backoff strategies.
    /// </summary>
    public enum RetryBackoffStrategy
    {
        /// <summary>
        /// Fixed delay between retries.
        /// </summary>
        Fixed,

        /// <summary>
        /// Linear increase in delay.
        /// </summary>
        Linear,

        /// <summary>
        /// Exponential increase in delay.
        /// </summary>
        Exponential
    }

    /// <summary>
    /// Statistics for the retry service.
    /// </summary>
    public sealed class MessageRetryStatistics
    {
        /// <summary>
        /// Gets or sets the total number of messages queued for retry.
        /// </summary>
        public long TotalMessagesQueuedForRetry { get; set; }

        /// <summary>
        /// Gets or sets the total number of successful retries.
        /// </summary>
        public long TotalSuccessfulRetries { get; set; }

        /// <summary>
        /// Gets or sets the total number of failed retries.
        /// </summary>
        public long TotalFailedRetries { get; set; }

        /// <summary>
        /// Gets or sets the total number of messages that exhausted all retry attempts.
        /// </summary>
        public long TotalExhaustedMessages { get; set; }

        /// <summary>
        /// Gets or sets the current size of the retry queue.
        /// </summary>
        public int CurrentRetryQueueSize { get; set; }

        /// <summary>
        /// Gets or sets the peak retry queue size.
        /// </summary>
        public int PeakRetryQueueSize { get; set; }

        /// <summary>
        /// Gets or sets the average number of retry attempts per message.
        /// </summary>
        public double AverageRetryAttempts { get; set; }

        /// <summary>
        /// Gets or sets the retry success rate (0.0 to 1.0).
        /// </summary>
        public double RetrySuccessRate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when these statistics were captured.
        /// </summary>
        public DateTime CapturedAt { get; set; }

        /// <summary>
        /// Gets or sets per-message-type retry statistics.
        /// </summary>
        public Dictionary<Type, MessageTypeRetryStatistics> MessageTypeStatistics { get; set; } = new Dictionary<Type, MessageTypeRetryStatistics>();
    }

    /// <summary>
    /// Retry statistics for a specific message type.
    /// </summary>
    public sealed class MessageTypeRetryStatistics
    {
        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        public Type MessageType { get; set; }

        /// <summary>
        /// Gets or sets the number of messages of this type queued for retry.
        /// </summary>
        public long MessagesQueuedForRetry { get; set; }

        /// <summary>
        /// Gets or sets the number of successful retries for this type.
        /// </summary>
        public long SuccessfulRetries { get; set; }

        /// <summary>
        /// Gets or sets the number of failed retries for this type.
        /// </summary>
        public long FailedRetries { get; set; }

        /// <summary>
        /// Gets or sets the number of exhausted messages for this type.
        /// </summary>
        public long ExhaustedMessages { get; set; }

        /// <summary>
        /// Gets or sets the average retry attempts for this message type.
        /// </summary>
        public double AverageRetryAttempts { get; set; }

        /// <summary>
        /// Gets or sets the success rate for this message type (0.0 to 1.0).
        /// </summary>
        public double SuccessRate { get; set; }
    }
}