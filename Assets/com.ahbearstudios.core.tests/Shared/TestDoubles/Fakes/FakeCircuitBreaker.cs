using System;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Tests.Shared.TestDoubles.Fakes
{
    /// <summary>
    /// Fake implementation of ICircuitBreaker for TDD testing.
    /// Provides simplified working implementation without actual circuit breaker logic.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// </summary>
    public sealed class FakeCircuitBreaker : ICircuitBreaker
    {
        private static readonly CircuitBreakerConfig DefaultConfig = CircuitBreakerConfig.Default;
        private const string DefaultName = "FakeCircuitBreaker";

        #region ICircuitBreaker Implementation

        public FixedString64Bytes Name { get; } = DefaultName;
        public CircuitBreakerState State => CircuitBreakerState.Closed;
        public CircuitBreakerConfig Configuration => DefaultConfig;
        public int FailureCount => 0;
        public DateTime? LastFailureTime => null;
        public DateTime LastStateChangeTime => DateTime.UtcNow;

        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Fake implementation - just execute the operation directly
            return await operation(cancellationToken);
        }

        public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Fake implementation - just execute the operation directly
            await operation(cancellationToken);
        }

        public void Open(string reason = null)
        {
            // No-op for fake circuit breaker
        }

        public void Close(string reason = null)
        {
            // No-op for fake circuit breaker
        }

        public void HalfOpen(string reason = null)
        {
            // No-op for fake circuit breaker
        }

        public void Reset(string reason = null)
        {
            // No-op for fake circuit breaker
        }

        public void RecordSuccess()
        {
            // No-op for fake circuit breaker
        }

        public void RecordFailure(Exception exception)
        {
            // No-op for fake circuit breaker
        }

        public bool AllowsRequests()
        {
            // Fake implementation - always allow requests
            return true;
        }

        public CircuitBreakerStatistics GetStatistics()
        {
            return new CircuitBreakerStatistics
            {
                Name = Name,
                State = State,
                TotalExecutions = 0,
                TotalFailures = 0,
                TotalSuccesses = 0,
                LastStateChange = LastStateChangeTime
            };
        }

        public string GetLastStateChangeReason()
        {
            // Fake implementation - return null as no state changes occur
            return null;
        }

        public void Dispose()
        {
            // No-op for fake circuit breaker - no resources to dispose
        }

        #endregion
    }
}